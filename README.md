
# JWT
![image](https://github.com/alicanyilmazz/JWT/assets/49749125/f06bf886-ae77-439e-81d9-e26660d60a8e)

### In our project, we have a total of 4 APIs: AuthServer.API, MiniAPI1, MiniAPI2, MiniAPI3, and MiniAPI4. Each API has its own Core, Data, and Service layers. In addition, there are common structures shared among all APIs, such as JWT implementation, which are utilized through the SharedLibrary project.

### The responsibilities of AuthServer.API are as follows:
### A) It performs token distribution. It provides service support for SignIn, SignUp, SignOut, Policy-Based Authorization, Claim-Based Authorization, Role-Based Authorization, and other operations using its own database. AuthServer API can also distribute two types of tokens: one that requires membership and one that does not.

B.) This allows you to protect your services that can be accessed without requiring users to sign up, as well as services that require membership, using the token service for non-membership. Through the appsettings.json file under AuthServerAPI, you can make settings to allow MiniAPIs to be accessed using only a token that requires membership, only a token that does not require membership, or both.

C.) Additionally, MiniAPI3 is a service that involves real-life examples of image saving and resizing. If you explore the API, you will see that it can process and resize an uploaded image in multiple dimensions based on the configurations provided. It also allows adjusting the image quality according to the specified values and provides options to save the image either in the database or on the server. These operations are performed asynchronously and in a multithreaded manner, while also managing caching.

D.) On the other hand, MiniAPI4 is a simple image saving service that everyone can use. Although MiniAPIs are primarily designed as examples for JWT, I made extra developments for MiniAPI3 to make it highly capable in terms of image processing. Currently, it saves the provided image in three different sizes and resolutions by default, but you can modify or customize these parameters to increase or decrease the variations as per your requirements.

E.) Furthermore, in the MiniAPI3 project, in addition to the Generic Repository, I created Command and Query Repositories using Stored Procedures to perform operations. I believe it serves as a good example of using Stored Procedures in EF Core in terms of both performance and usage. However, I do not find it appropriate to store and retrieve images in the database, but for the sake of demonstration, I implemented services that both write and read images from the database. For the services that save images to the server, I improved performance by directly saving them as byte arrays without converting them from streams. Below the documentation, I will provide basic code structures related to the multi-threaded image saving process and the creation scripts of the Stored Procedures used in the project. In essence, we can say that within this project, which is primarily a JWT example, there is also a lovely image API.

### SAMPLE POSTMAN COLLECTION 

[JWT_TUTORIAL.postman_collection.json](https://github.com/alicanyilmazz/JWT/files/14551567/JWT_TUTORIAL.postman_collection.json)

[JWT_TUTURIAL_MinAPIs.postman_collection.json](https://github.com/alicanyilmazz/JWT/files/14551568/JWT_TUTURIAL_MinAPIs.postman_collection.json)

#### Version 1 
```c#

 public static bool CheckCdmCassettesForDispense(AtmDeviceType deviceType, decimal amount, string currencyCode)
{
    if (amount <= 0) return false;
    if (amount != Math.Truncate(amount)) return false; // banknotlar tam sayı varsayımı

    // CDM cihazlarındaki kasetleri topla
    var cdmCassettes = Diagnostic.Diagnostics.Values
        .OfType<CdmDevice>()
        .SelectMany(d => d.Cassettes.Values)
        .Where(c =>
            c.CassetteType.Equals(deviceType.ToString(), StringComparison.OrdinalIgnoreCase) &&
            string.Equals(c.CurrencyCode, currencyCode, StringComparison.OrdinalIgnoreCase) &&
            c.Status < 4 &&
            c.BanknoteType > 0 &&
            c.CurrentCount > 0);

    // Aynı kupürü (BanknoteType) birleştir → toplam adet
    var groups = cdmCassettes
        .GroupBy(c => c.BanknoteType)
        .Select(g => new
        {
            Denom = (int)g.Key,                 // 10, 20, 50, 100, ...
            Count = g.Sum(x => x.CurrentCount)  // o kupürden toplam kaç adet var
        })
        .Where(x => x.Denom > 0)
        .OrderByDescending(x => x.Denom)       // sıralama şart değil ama zarar da vermez
        .ToList();

    // Hedefi int'e çevir (tam-sayı banknot varsayımı)
    int target = (int)amount;

    // Bounded coin change (O(denom_sayısı * amount)):
    // dp[s] >= 0  ise s tutarına ulaşılabiliyor ve dp[s], şu anki kupürden kalan kullanma hakkı
    // dp[s] == -1 ise s tutarına şu ana kadar ulaşılamıyor
    var dp = Enumerable.Repeat(-1, target + 1).ToArray();
    dp[0] = 0;

    foreach (var g in groups)
    {
        int w = g.Denom;
        int c = Math.Min(g.Count, target / w); // gereksiz fazla adedi kırp

        for (int s = 0; s <= target; s++)
        {
            if (dp[s] >= 0)
            {
                // s zaten yapılabiliyor; bu kupürden c adet hakkımız var
                dp[s] = c;
            }
            else if (s >= w && dp[s - w] > 0)
            {
                // s-w yapılabiliyorsa ve oradan bu kupürden en az 1 hakkımız kalmışsa
                dp[s] = dp[s - w] - 1;
            }
            else
            {
                dp[s] = -1;
            }
        }
    }

    return dp[target] >= 0;
}

```

#### Version 2

```c#
  public class MultistagedTransactionImageSaveService : IImageServerSaveService
    {
        private const int ThumbnailWidth = 300;
        private const int FullScreenWidth = 1000;

        private readonly IUnitOfWork _unitOfWork;
        private readonly IRepository<ImageFile> _repository;
        public MultistagedTransactionImageSaveService(IUnitOfWork unitOfWork, IRepository<ImageFile> repository)
        {
            _unitOfWork = unitOfWork;
            _repository = repository;
        }
        public async Task<Response<NoDataDto>> SaveAsync(IEnumerable<ImageDbServiceRequest> images)
        {
            var imageStorage = new ConcurrentDictionary<string, ImageFile>();
            var totalImages = await _repository.CountAsync();
            var tasks = images.Select(image => Task.Run(async () =>
            {
                try
                {
                    using var imageResult = await Image.LoadAsync(image.Content);

                    var id = Guid.NewGuid();
                    var path = $"/images/{totalImages % 1000}/";
                    var name = $"{id}.jpg";

                    var storagePath = Path.Combine(Directory.GetCurrentDirectory(), $"wwwroot{path}".Replace("/", "\\"));

                    if (!Directory.Exists(storagePath))
                    {
                        Directory.CreateDirectory(storagePath);
                    }

                    await SaveImageAsync(imageResult, $"Original_{image.Name}", storagePath, imageResult.Width);
                    await SaveImageAsync(imageResult, $"FullScreen_{image.Name}", storagePath, FullScreenWidth);
                    await SaveImageAsync(imageResult, $"Thumbnail_{image.Name}", storagePath, ThumbnailWidth);

                    imageStorage.TryAdd(image.Name, new ImageFile()
                    {
                        Id = id,
                        Folder= path
                    });
                }
                catch (Exception)
                {
                    // Log
                    throw;
                }
            })).ToList();

            try
            {
                await Task.WhenAll(tasks);
                foreach (var image in imageStorage)
                {
                    await _repository.AddAsync(image.Value);
                }
                await _repository.CommitAsync();
            }
            catch (Exception e)
            {
                return Response<NoDataDto>.Fail(e.Message, 404, true);
            }
            return Response<NoDataDto>.Success(200);
        }

        private async Task SaveImageAsync(Image image, string name, string path, int resizeWidth)
        {
            var width = image.Width;
            var height = image.Height;
            if (width > resizeWidth)
            {
                height = (int)(double)(resizeWidth / width * height);
                width = resizeWidth;
            }
            image.Mutate(x => x.Resize(new Size(width, height)));
            image.Metadata.ExifProfile = null;
            await image.SaveAsJpegAsync($"{path}/{name}", new JpegEncoder
            {
                Quality = 75
            });
        }
    }
```
### GET_IMAGE
```SQL
USE [ADVANCEPHOTODB]
GO
/****** Object:  StoredProcedure [dbo].[GET_IMAGE]    Script Date: 7/8/2023 2:42:28 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[GET_IMAGE]
@ImageId UNIQUEIDENTIFIER = NULL
AS
BEGIN

SELECT IMF.Folder + IMFD.Type + '_' + LOWER(IMFD.ImageId) + '.' + IMF.Extension AS Path FROM [dbo].[ImageFile] IMF WITH (NOLOCK)  JOIN [dbo].[ImageFileDetail] IMFD
ON IMF.ImageId = IMFD.ImageId WHERE IMF.ImageId = @ImageId

END
```
### GET_IMAGES
```SQL
USE [ADVANCEPHOTODB]
GO
/****** Object:  StoredProcedure [dbo].[GET_IMAGES]    Script Date: 7/8/2023 2:43:21 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[GET_IMAGES]
AS
BEGIN

SELECT IMF.Folder + IMFD.Type + '_' + LOWER(IMFD.ImageId) + '.' + IMF.Extension AS Path FROM [dbo].[ImageFile] IMF WITH (NOLOCK)  JOIN [dbo].[ImageFileDetail] IMFD
ON IMF.ImageId = IMFD.ImageId 

END
```
### GET_IMAGE_QUALITY
```SQL
USE [ADVANCEPHOTODB]
GO
/****** Object:  StoredProcedure [dbo].[GET_IMAGE_QUALITY]    Script Date: 7/8/2023 2:43:56 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[GET_IMAGE_QUALITY]
AS
BEGIN
SELECT [Name],[Rate],[ResizeWidth],[IsOriginal] FROM [dbo].[ImageQuality]
END
```
### GET_NUMBER_OF_IMAGE_FILE
```SQL
USE [ADVANCEPHOTODB]
GO
/****** Object:  StoredProcedure [dbo].[GET_NUMBER_OF_IMAGE_FILE]    Script Date: 7/8/2023 2:44:32 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[GET_NUMBER_OF_IMAGE_FILE]
 @RecordCount INT OUTPUT
AS
BEGIN
SELECT @RecordCount = COUNT(*) FROM [dbo].[ImageFile]
END
```
### IMAGE_FILE_DETAIL_INSERT
```SQL
USE [ADVANCEPHOTODB]
GO
/****** Object:  StoredProcedure [dbo].[IMAGE_FILE_DETAIL_INSERT]    Script Date: 7/8/2023 2:45:35 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[IMAGE_FILE_DETAIL_INSERT]
@ImageId UNIQUEIDENTIFIER,
@Type NVARCHAR(MAX),
@QualityRate NVARCHAR(MAX)
AS
BEGIN
INSERT INTO [dbo].[ImageFileDetail]
           ([ImageId]
           ,[Type]
           ,[QualityRate])
     VALUES (@ImageId,@Type,@QualityRate)
END
```
### IMAGE_FILE_INSERT
```SQL
USE [ADVANCEPHOTODB]
GO
/****** Object:  StoredProcedure [dbo].[IMAGE_FILE_INSERT]    Script Date: 7/8/2023 2:46:10 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[IMAGE_FILE_INSERT]
@ImageId UNIQUEIDENTIFIER,
@Folder NVARCHAR(MAX),
@Extension NVARCHAR(MAX)
AS
BEGIN
INSERT INTO [dbo].[ImageFile] ([ImageId]
           ,[Folder]
           ,[Extension])
     VALUES (@ImageId,@Folder,@Extension)
END
```

```c#
   public async Task<List<string>> ReadPhotoInfoDirectlyFromDatabase()
        {
            try
            {
                var database = _context.Database;
                var dbConnection = (SqlConnection)database.GetDbConnection();

                var command = new SqlCommand($"SELECT [Folder] + '/' + CAST([Id] AS nvarchar(36)) FROM [ADVANCEPHOTODB].[dbo].[ImageFile]", dbConnection);
                //command.Parameters.Add(new SqlParameter("@id", id));
                dbConnection.Open();
                var reader = await command.ExecuteReaderAsync();
                var result = new List<string>();
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        result.Add(reader.GetString(0));
                    }
                }
                reader.Close();
                return result;
            }
            catch (Exception)
            {
                // Log
            }
            return null;
        }
        public async Task<Stream> ReadPhotoDirectlyFromDatabase(string id, string content)
        {
            try
            {
                var database = _context.Database;
                var dbConnection = (SqlConnection)database.GetDbConnection();

                var command = new SqlCommand($"SELECT {content} FROM [ADVANCEPHOTODB].[dbo].[ImageData] WHERE Id = @id", dbConnection);
                command.Parameters.Add(new SqlParameter("@id", id));
                dbConnection.Open();
                var reader = await command.ExecuteReaderAsync();
                Stream result = null;
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        result = reader.GetStream(0);
                    }
                }
                reader.Close();
                return result;
            }
            catch (Exception)
            {
                // Log
            }
            return null;
        }
```
```SQL
DECLARE @SearchValue NVARCHAR(100) = 'X';
DECLARE @SQL NVARCHAR(MAX) = '';
 
SELECT @SQL = STRING_AGG('
IF EXISTS (SELECT 1 FROM [' + TABLE_SCHEMA + '].[' + TABLE_NAME + '] 
            WHERE TRY_CAST([' + COLUMN_NAME + '] AS NVARCHAR(MAX)) = ''' + @SearchValue + ''')
    PRINT ''' + TABLE_SCHEMA + '.' + TABLE_NAME + '''',
    CHAR(13) + CHAR(10))
FROM INFORMATION_SCHEMA.COLUMNS
WHERE DATA_TYPE IN ('char', 'nchar', 'varchar', 'nvarchar', 'text', 'ntext');
 
EXEC sp_executesql @SQL;


# Admin olarak çalıştır
%windir%\Microsoft.NET\Framework64\v4.0.30319\aspnet_regiis.exe -lk
# listede keyContainerName gör --> diyelim "NetFrameworkConfigurationKey"
%windir%\Microsoft.NET\Framework64\v4.0.30319\aspnet_regiis.exe -px "NetFrameworkConfigurationKey" "C:\temp\rsa-key.xml" -pri

# Import
%windir%\Microsoft.NET\Framework64\v4.0.30319\aspnet_regiis.exe -pi "NetFrameworkConfigurationKey" "C:\temp\rsa-key.xml"

# AppPool'a izin ver (örnek MyAppPool)
%windir%\Microsoft.NET\Framework64\v4.0.30319\aspnet_regiis.exe -pa "NetFrameworkConfigurationKey" "IIS APPPOOL\MyAppPool"

# IIS restart veya apppool recycle
iisreset

```
BU
```c#
using System;
using System.Collections.Generic;
using System.Linq;

public static class AtmButtonPlanner
{
    // (opsiyonel) Flag helper
    public static void FlagDispensibility<T>(IEnumerable<T> items) where T : BaseAmount
    {
        foreach (var x in items ?? Enumerable.Empty<T>())
            x.IsDispensible = Manager.IsAmountDispensible(x.Amount, x.CurrencyCode);
    }

    // Baseline (pre_i -> 100/200/500/1000/…)
    public static IReadOnlyDictionary<string, decimal> SnapshotBaselineByIndex(IList<PredefinedAmount> pre) =>
        pre.ToDictionary(p => p.Index, p => p.Amount, StringComparer.Ordinal);

    // En yakın baseline’a sahip slot index’i
    private static int? PickNearestIndex(
        IList<PredefinedAmount> slots,
        IEnumerable<int> candidates,
        decimal target,
        IReadOnlyDictionary<string, decimal> baselineByIndex)
    {
        return candidates
            .Select(i => new { i, b = baselineByIndex[slots[i].Index] })
            .OrderBy(x => Math.Abs(x.b - target))
            .ThenBy(x => x.b)
            .ThenBy(x => x.i)
            .Select(x => (int?)x.i)
            .FirstOrDefault();
    }

    /// API (dispensible) → önce ENABLE pre’ler, sonra DISABLE pre’ler (en yakın baseline)
    public static void Replace_EnabledThenDisabled(
        IList<PredefinedAmount> slots,
        IEnumerable<ApiAmount> apiInOrder,
        IReadOnlyDictionary<string, decimal> baselineByIndex)
    {
        if (slots == null || slots.Count == 0) return;

        var apis = (apiInOrder ?? Enumerable.Empty<ApiAmount>())
                   .Where(a => a.IsDispensible)
                   .ToList();
        if (apis.Count == 0) return;

        // 1) ENABLE & unreplaced
        var pass1 = Enumerable.Range(0, slots.Count)
            .Where(i =>  slots[i].IsDispensible && string.IsNullOrEmpty(slots[i].ReplacedIndex))
            .ToList();

        foreach (var api in apis.ToList())
        {
            if (pass1.Count == 0) break;
            var best = PickNearestIndex(slots, pass1, api.Amount, baselineByIndex);
            if (best is null) continue;

            var s = slots[best.Value];
            s.Amount        = api.Amount;
            s.AmountSource  = AmountSource.Api;
            s.ReplacedIndex = api.Index;
            s.IsDispensible = true;

            pass1.Remove(best.Value);
            apis.Remove(api);
        }

        if (apis.Count == 0) return;

        // 2) DISABLE & unreplaced
        var pass2 = Enumerable.Range(0, slots.Count)
            .Where(i => !slots[i].IsDispensible && string.IsNullOrEmpty(slots[i].ReplacedIndex))
            .ToList();

        foreach (var api in apis.ToList())
        {
            if (pass2.Count == 0) break;
            var best = PickNearestIndex(slots, pass2, api.Amount, baselineByIndex);
            if (best is null) continue;

            var s = slots[best.Value];
            s.Amount        = api.Amount;
            s.AmountSource  = AmountSource.Api;
            s.ReplacedIndex = api.Index;
            s.IsDispensible = true;

            pass2.Remove(best.Value);
            apis.Remove(api);
        }
    }

    /// Fill: Replacement sonrası listedeki **en küçük ödenebilir** tutarı base al;
    /// base * k adaylarını (cap altında, çakışmayan ve dispensible) disabled slotlara sırayla dağıt.
    public static void FillDisabledWithMultiplesFromCurrentMinDispensible(
        IList<PredefinedAmount> slots,
        IReadOnlyDictionary<string, decimal> baselineByIndex,
        decimal maxAtmPayout,
        string calcPrefix = "calc_")
    {
        if (slots == null || slots.Count == 0) return;

        var cur = slots[0].CurrencyCode;

        // BASE = post-replacement en küçük ödenebilir
        var baseUnit = slots
            .Where(s => s.IsDispensible)
            .Select(s => s.Amount)
            .DefaultIfEmpty(0m)
            .Min();

        if (baseUnit <= 0 || maxAtmPayout < baseUnit) return;

        var used = new HashSet<decimal>(slots.Select(s => s.Amount));

        // Cap altında, çakışmayan ve dispensible aday katlar
        int kMax = (int)Math.Floor(maxAtmPayout / baseUnit);
        var candidates = Enumerable.Range(1, kMax)
            .Select(k => baseUnit * k)
            .Where(v => !used.Contains(v) && Manager.IsAmountDispensible(v, cur))
            .ToList();

        int calcNo = 1;
        foreach (var slot in slots
            .Where(s => !s.IsDispensible && string.IsNullOrEmpty(s.ReplacedIndex))
            .OrderBy(s => baselineByIndex[s.Index]))
        {
            if (candidates.Count == 0) break; // aday kalmadı → kalanlar disabled

            var cand = candidates[0];
            candidates.RemoveAt(0);

            slot.Amount        = cand;
            slot.AmountSource  = AmountSource.Calculated;
            slot.ReplacedIndex = $"{calcPrefix}{calcNo++}";
            slot.IsDispensible = true;

            used.Add(cand);
        }
    }

    /// Tek çağrıda: Replace(Enabled→Disabled) → Fill(CurrentMinDispensible)
    public static IList<PredefinedAmount> Plan_ReplaceThenFill(
        IList<PredefinedAmount> predefinedN,
        IList<ApiAmount> apiInOrder,
        decimal maxAtmPayout,
        bool runFlagStep = false)
    {
        if (predefinedN == null || predefinedN.Count == 0) return predefinedN ?? new List<PredefinedAmount>();

        var baseline = SnapshotBaselineByIndex(predefinedN.ToList());

        if (runFlagStep)
        {
            FlagDispensibility(predefinedN);
            FlagDispensibility(apiInOrder);
        }

        Replace_EnabledThenDisabled(predefinedN.ToList(), apiInOrder, baseline);
        FillDisabledWithMultiplesFromCurrentMinDispensible(predefinedN, baseline, maxAtmPayout);

        return predefinedN;
    }
}



```
------------------------------------------------------------
```c#
using System;
using System.Collections.Generic;
using System.Linq;

public enum AmountSource { Predefined, Api, Calculated }

// ----------------- Yardımcılar -----------------
public static class AtmButtonPlanner
{
    private static readonly decimal[] Units = { 100m, 200m, 500m, 1000m };

    // Flag
    public static void FlagDispensibility<T>(IEnumerable<T> items) where T : BaseAmount
    {
        foreach (var x in items ?? Enumerable.Empty<T>())
            x.IsDispensible = Manager.IsAmountDispensible(x.Amount, x.CurrencyCode);
    }

    // Pre baseline snapshot (pre_i -> 100/200/500/1000)
    public static IReadOnlyDictionary<string, decimal> SnapshotBaselineByIndex(List<PredefinedAmount> pre) =>
        pre.ToDictionary(p => p.Index, p => p.Amount, StringComparer.Ordinal);

    // İsteğe bağlı: başlangıç pre snapshot (flag’lenebilir)
    public static List<PredefinedAmount> BuildPreSnapshot(List<PredefinedAmount> pre) =>
        pre.Select(p => new PredefinedAmount(p.Index, p.Amount, p.CurrencyCode)).ToList();

    // Ortak: belirli aday indekslerden target’a en yakın slotu seç
    private static int? PickNearestIndex(
        List<PredefinedAmount> slots,
        IEnumerable<int> candidates,
        decimal target,
        IReadOnlyDictionary<string, decimal> baseline)
    {
        return candidates
            .Select(i => new { i, b = baseline[slots[i].Index] })
            .OrderBy(x => Math.Abs(x.b - target))
            .ThenBy(x => x.b)
            .ThenBy(x => x.i)
            .Select(x => (int?)x.i)
            .FirstOrDefault();
    }

    // ----------------- REPLACEMENT STRATEJİLERİ -----------------
    // A) Non-dispensible-first: önce ödenemeyen pre’lere, sonra ödenebilenlere
    public static void Replace_NonDispensibleFirst(
        List<PredefinedAmount> slots,
        IEnumerable<ApiAmount> apiInOrder,
        IReadOnlyDictionary<string, decimal> baselineByIndex)
    {
        if (slots == null || slots.Count != 4) return;
        var apiList = (apiInOrder ?? Enumerable.Empty<ApiAmount>()).Where(a => a.IsDispensible).ToList();
        if (apiList.Count == 0) return;

        // Pass 1: disabled & unreplaced
        var pass1 = Enumerable.Range(0, slots.Count)
            .Where(i => !slots[i].IsDispensible && string.IsNullOrEmpty(slots[i].ReplacedIndex))
            .ToList();

        // Pass 2: enabled & unreplaced
        var pass2 = Enumerable.Range(0, slots.Count)
            .Where(i =>  slots[i].IsDispensible && string.IsNullOrEmpty(slots[i].ReplacedIndex))
            .ToList();

        foreach (var pass in new[] { pass1, pass2 })
        {
            foreach (var api in apiList.ToList())
            {
                if (pass.Count == 0) break;
                var best = PickNearestIndex(slots, pass, api.Amount, baselineByIndex);
                if (best is null) continue;

                var s = slots[best.Value];
                s.Amount        = api.Amount;
                s.AmountSource  = AmountSource.Api;
                s.ReplacedIndex = api.Index;
                s.IsDispensible = true;

                pass.Remove(best.Value);
                apiList.Remove(api);
                if (apiList.Count == 0) break;
            }
            if (apiList.Count == 0) break;
        }
    }

    // B) Enabled-first: önce ödenebilen pre’lere, sonra ödenemeyenlere
    public static void Replace_EnabledFirst(
        List<PredefinedAmount> slots,
        IEnumerable<ApiAmount> apiInOrder,
        IReadOnlyDictionary<string, decimal> baselineByIndex)
    {
        if (slots == null || slots.Count != 4) return;
        var apiList = (apiInOrder ?? Enumerable.Empty<ApiAmount>()).Where(a => a.IsDispensible).ToList();
        if (apiList.Count == 0) return;

        var pass1 = Enumerable.Range(0, slots.Count)
            .Where(i =>  slots[i].IsDispensible && string.IsNullOrEmpty(slots[i].ReplacedIndex))
            .ToList();
        var pass2 = Enumerable.Range(0, slots.Count)
            .Where(i => !slots[i].IsDispensible && string.IsNullOrEmpty(slots[i].ReplacedIndex))
            .ToList();

        foreach (var pass in new[] { pass1, pass2 })
        {
            foreach (var api in apiList.ToList())
            {
                if (pass.Count == 0) break;
                var best = PickNearestIndex(slots, pass, api.Amount, baselineByIndex);
                if (best is null) continue;

                var s = slots[best.Value];
                s.Amount        = api.Amount;
                s.AmountSource  = AmountSource.Api;
                s.ReplacedIndex = api.Index;
                s.IsDispensible = true;

                pass.Remove(best.Value);
                apiList.Remove(api);
                if (apiList.Count == 0) break;
            }
            if (apiList.Count == 0) break;
        }
    }

    // C) Closest-overall: durumuna bakmadan 4 slot içinden en yakın (unreplaced)
    public static void Replace_ClosestOverall(
        List<PredefinedAmount> slots,
        IEnumerable<ApiAmount> apiInOrder,
        IReadOnlyDictionary<string, decimal> baselineByIndex)
    {
        if (slots == null || slots.Count != 4) return;

        var candidates = Enumerable.Range(0, slots.Count)
            .Where(i => string.IsNullOrEmpty(slots[i].ReplacedIndex))
            .ToList();

        foreach (var api in (apiInOrder ?? Enumerable.Empty<ApiAmount>()).Where(a => a.IsDispensible))
        {
            if (candidates.Count == 0) break;
            var best = PickNearestIndex(slots, candidates, api.Amount, baselineByIndex);
            if (best is null) continue;

            var s = slots[best.Value];
            s.Amount        = api.Amount;
            s.AmountSource  = AmountSource.Api;
            s.ReplacedIndex = api.Index;
            s.IsDispensible = true;

            candidates.Remove(best.Value);
        }
    }

    // ----------------- FILL STRATEJİLERİ -----------------
    // Ortak doldurma çekirdeği (belirli baseUnit ile)
    private static void FillDisabledWithGivenBaseUnit(
        List<PredefinedAmount> slots,
        IReadOnlyDictionary<string, decimal> baselineByIndex,
        decimal baseUnit,
        string calcPrefix = "calc_")
    {
        if (slots == null || slots.Count != 4) return;
        var cur = slots[0].CurrencyCode;
        if (baseUnit <= 0 || !Manager.IsAmountDispensible(baseUnit, cur)) return;

        var used = new HashSet<decimal>(slots.Select(s => s.Amount));
        int calcNo = 1;

        foreach (var slot in slots
            .Where(s => !s.IsDispensible && string.IsNullOrEmpty(s.ReplacedIndex))
            .OrderBy(s => baselineByIndex[s.Index]))
        {
            var cand = Enumerable.Range(1, 300)
                .Select(k => baseUnit * k)
                .FirstOrDefault(v => !used.Contains(v) && Manager.IsAmountDispensible(v, cur));

            if (cand == 0m) continue;

            slot.Amount        = cand;
            slot.AmountSource  = AmountSource.Calculated;
            slot.ReplacedIndex = $"{calcPrefix}{calcNo++}";
            slot.IsDispensible = true;
            used.Add(cand);
        }
    }

    // FA) FromReplacedMin: replace edilen API’lerin en küçüğü taban
    public static void Fill_FromReplacedMin(
        List<PredefinedAmount> slots,
        IReadOnlyDictionary<string, decimal> baselineByIndex,
        string calcPrefix = "calc_")
    {
        var cur = slots[0].CurrencyCode;

        var replaced = slots
            .Where(s => s.AmountSource == AmountSource.Api && s.IsDispensible)
            .Select(s => s.Amount)
            .ToList();

        decimal baseUnit;
        if (replaced.Count > 0)
        {
            var min = replaced.Min();
            baseUnit = Manager.IsAmountDispensible(min, cur) ? min
                      : Units.FirstOrDefault(u => Manager.IsAmountDispensible(u, cur));
        }
        else
        {
            baseUnit = Units.FirstOrDefault(u => Manager.IsAmountDispensible(u, cur));
        }

        FillDisabledWithGivenBaseUnit(slots, baselineByIndex, baseUnit, calcPrefix);
    }

    // FB) FromInitialGlobalMin: başlangıç (pre baseline + api) içinden ödenebilir en küçük taban
    public static void Fill_FromInitialGlobalMin(
        List<PredefinedAmount> slots,
        IReadOnlyDictionary<string, decimal> baselineByIndex,
        IEnumerable<PredefinedAmount> initialPreSnapshot, // flag’li olmalı (baseline 100/200/500/1000)
        IEnumerable<ApiAmount> initialApi,                // flag’li olmalı
        string calcPrefix = "calc_")
    {
        var cur = slots[0].CurrencyCode;

        var preUsable = (initialPreSnapshot ?? Enumerable.Empty<PredefinedAmount>())
            .Where(p => p.IsDispensible)
            .Select(p => p.Amount);

        var apiUsable = (initialApi ?? Enumerable.Empty<ApiAmount>())
            .Where(a => a.IsDispensible)
            .Select(a => a.Amount);

        var all = preUsable.Concat(apiUsable).ToList();

        decimal baseUnit;
        if (all.Count > 0)
        {
            var min = all.Min();
            baseUnit = Manager.IsAmountDispensible(min, cur) ? min
                      : Units.FirstOrDefault(u => Manager.IsAmountDispensible(u, cur));
        }
        else
        {
            baseUnit = Units.FirstOrDefault(u => Manager.IsAmountDispensible(u, cur));
        }

        FillDisabledWithGivenBaseUnit(slots, baselineByIndex, baseUnit, calcPrefix);
    }
}


```
------------------NIKAHI-----------------------------
```c#
using System;
using System.Collections.Generic;
using System.Linq;

public static class AtmButtonPlanner
{
    // ---------------------------
    // Yardımcı / Ortak
    // ---------------------------

    public static void FlagDispensibility<T>(IEnumerable<T> items) where T : BaseAmount
    {
        foreach (var x in items ?? Enumerable.Empty<T>())
            x.IsDispensible = Manager.IsAmountDispensible(x.Amount, x.CurrencyCode);
    }

    public static IReadOnlyDictionary<string, decimal> SnapshotBaselineByIndex(IList<PredefinedAmount> pre) =>
        pre.ToDictionary(p => p.Index, p => p.Amount, StringComparer.Ordinal);

    private static void ValidateBaselineCoverage(
        IList<PredefinedAmount> slots,
        IReadOnlyDictionary<string, decimal> baselineByIndex)
    {
        if (slots == null) throw new ArgumentNullException(nameof(slots));
        if (baselineByIndex == null) throw new ArgumentNullException(nameof(baselineByIndex));
        if (!slots.All(s => baselineByIndex.ContainsKey(s.Index)))
            throw new ArgumentException("Baseline sözlüğü, tüm slot Index değerlerini içermiyor.");
    }

    // ---------------------------
    // Replacement (tek para birimi, benzersiz değer garantisi)
    // ---------------------------

    private sealed class SlotReplacer
    {
        private readonly IList<PredefinedAmount> _slots;
        private readonly IReadOnlyDictionary<string, decimal> _baselineByIndex;
        private readonly List<ApiAmount> _remainingApiAmounts;
        private readonly HashSet<decimal> _currentAmounts; // mevcut buton tutarları (benzersizlik için)

        public SlotReplacer(
            IList<PredefinedAmount> slots,
            IEnumerable<ApiAmount> apiAmounts,
            IReadOnlyDictionary<string, decimal> baselineByIndex)
        {
            _slots = slots ?? throw new ArgumentNullException(nameof(slots));
            _baselineByIndex = baselineByIndex ?? throw new ArgumentNullException(nameof(baselineByIndex));
            _remainingApiAmounts = apiAmounts?.Where(a => a.IsDispensible).ToList() ?? new List<ApiAmount>();
            _currentAmounts = new HashSet<decimal>(_slots.Select(s => s.Amount));
        }

        public void ReplaceSlots()
        {
            if (_slots.Count == 0 || _remainingApiAmounts.Count == 0) return;
            ValidateBaselineCoverage((IList<PredefinedAmount>)_slots, _baselineByIndex);

            // 1) ENABLE → 2) DISABLE
            ReplaceInPhase(isDispensible: true);
            ReplaceInPhase(isDispensible: false);
        }

        private void ReplaceInPhase(bool isDispensible)
        {
            foreach (var api in _remainingApiAmounts.ToList())
            {
                // ⛔ Aynı tutarı ikinci kez yerleştirme — benzersizlik garantisi
                if (_currentAmounts.Contains(api.Amount))
                    continue;

                var candidateIdx = Enumerable.Range(0, _slots.Count)
                    .Where(i =>
                        _slots[i].IsDispensible == isDispensible &&
                        string.IsNullOrEmpty(_slots[i].ReplacedIndex))
                    .ToList();

                if (candidateIdx.Count == 0) continue;

                var best = FindNearestSlotIndex(candidateIdx, api.Amount);
                if (!best.HasValue) continue;

                ReplaceSlot(best.Value, api);
                _remainingApiAmounts.Remove(api);
            }
        }

        private int? FindNearestSlotIndex(IEnumerable<int> candidateIndices, decimal targetAmount) =>
            candidateIndices
                .Select(i => new { Index = i, BaselineAmount = _baselineByIndex[_slots[i].Index] })
                .OrderBy(x => Math.Abs(x.BaselineAmount - targetAmount))
                .ThenBy(x => x.BaselineAmount)
                .ThenBy(x => x.Index)
                .Select(x => (int?)x.Index)
                .FirstOrDefault();

        private void ReplaceSlot(int slotIndex, ApiAmount apiAmount)
        {
            var slot = _slots[slotIndex];
            slot.Amount        = apiAmount.Amount;
            slot.AmountSource  = AmountSource.Api;
            slot.ReplacedIndex = apiAmount.Index;
            slot.IsDispensible = true;

            _currentAmounts.Add(slot.Amount); // benzersizlik set’ine ekle
        }
    }

    // ---------------------------
    // Fill (tek para birimi, cap ve benzersizlik korunur)
    // ---------------------------

    private sealed class DisabledSlotFiller
    {
        private readonly IList<PredefinedAmount> _slots;
        private readonly IReadOnlyDictionary<string, decimal> _baselineByIndex;
        private readonly decimal _maxAtmPayout;
        private readonly string _calcPrefix;

        public DisabledSlotFiller(
            IList<PredefinedAmount> slots,
            IReadOnlyDictionary<string, decimal> baselineByIndex,
            decimal maxAtmPayout,
            string calcPrefix = "calc_")
        {
            _slots = slots ?? throw new ArgumentNullException(nameof(slots));
            _baselineByIndex = baselineByIndex ?? throw new ArgumentNullException(nameof(baselineByIndex));
            _maxAtmPayout = maxAtmPayout;
            _calcPrefix = calcPrefix;
        }

        public void FillSlots()
        {
            if (_slots.Count == 0) return;
            ValidateBaselineCoverage((IList<PredefinedAmount>)_slots, _baselineByIndex);

            var currency = _slots[0].CurrencyCode;

            // BASE = listede en küçük ödenebilir
            var baseUnit = _slots.Where(s => s.IsDispensible)
                                 .Select(s => s.Amount)
                                 .DefaultIfEmpty(0m)
                                 .Min();

            if (baseUnit <= 0 || _maxAtmPayout < baseUnit) return;

            var used = new HashSet<decimal>(_slots.Select(s => s.Amount));
            int kMax = (int)Math.Floor(_maxAtmPayout / baseUnit);

            // Cap altında, çakışmayan ve dispensible olan adaylar
            var candidates = Enumerable.Range(1, kMax)
                .Select(k => baseUnit * k)
                .Where(v => !used.Contains(v) && Manager.IsAmountDispensible(v, currency))
                .ToList();

            int calcNo = 1;
            foreach (var slot in _slots
                .Where(s => !s.IsDispensible && string.IsNullOrEmpty(s.ReplacedIndex))
                .OrderBy(s => _baselineByIndex[s.Index]))
            {
                if (candidates.Count == 0) break; // aday bitti → kalanlar disabled

                var cand = candidates[0];
                candidates.RemoveAt(0);

                slot.Amount        = cand;
                slot.AmountSource  = AmountSource.Calculated;
                slot.ReplacedIndex = $"{_calcPrefix}{calcNo++}";
                slot.IsDispensible = true;

                used.Add(cand);
            }
        }
    }

    // ---------------------------
    // Public API
    // ---------------------------

    public static void Replace_EnabledThenDisabled(
        IList<PredefinedAmount> slots,
        IEnumerable<ApiAmount> apiInOrder,
        IReadOnlyDictionary<string, decimal> baselineByIndex)
    {
        ValidateBaselineCoverage(slots as IList<PredefinedAmount> ?? throw new ArgumentNullException(nameof(slots)),
                                 baselineByIndex);

        new SlotReplacer(slots, apiInOrder, baselineByIndex).ReplaceSlots();
    }

    public static void FillDisabledWithMultiplesFromCurrentMinDispensible(
        IList<PredefinedAmount> slots,
        IReadOnlyDictionary<string, decimal> baselineByIndex,
        decimal maxAtmPayout,
        string calcPrefix = "calc_")
    {
        ValidateBaselineCoverage(slots as IList<PredefinedAmount> ?? throw new ArgumentNullException(nameof(slots)),
                                 baselineByIndex);

        new DisabledSlotFiller(slots, baselineByIndex, maxAtmPayout, calcPrefix).FillSlots();
    }

    public static IList<PredefinedAmount> Plan_ReplaceThenFill(
        IList<PredefinedAmount> predefinedN,
        IList<ApiAmount> apiInOrder,
        decimal maxAtmPayout,
        bool runFlagStep = false)
    {
        if (predefinedN == null || predefinedN.Count == 0) return predefinedN ?? new List<PredefinedAmount>();

        var baseline = SnapshotBaselineByIndex(predefinedN.ToList());

        if (runFlagStep)
        {
            FlagDispensibility(predefinedN);
            FlagDispensibility(apiInOrder);
        }

        Replace_EnabledThenDisabled(predefinedN, apiInOrder, baseline);
        FillDisabledWithMultiplesFromCurrentMinDispensible(predefinedN, baseline, maxAtmPayout);

        return predefinedN;
    }
}

```
------------------DISPENSE ALOOTITHMS-----------------------------
```c#
public enum DispenseAlgorithm
{
    Greedy,      // Hızlı, basit, %95+ başarı oranı
    SubsetSum    // Yavaş ama %100 matematiksel kesinlik
}

public static class Manager
{
    // Varsayılan algoritma (istediğin zaman değiştirebilirsin)
    private static DispenseAlgorithm _currentAlgorithm = DispenseAlgorithm.SubsetSum;

    /// <summary>
    /// Kullanılacak algoritmayı ayarla
    /// </summary>
    public static void SetAlgorithm(DispenseAlgorithm algorithm)
    {
        _currentAlgorithm = algorithm;
    }

    /// <summary>
    /// amount tutarı, currencyCode para biriminde ÖDENEBİLİR mi?
    /// </summary>
    public static bool IsAmountDispensible(decimal amount, string currencyCode)
    {
        if (amount <= 0m || string.IsNullOrWhiteSpace(currencyCode))
            return false;

        return CanDispenseFromDevice(AtmDeviceType.CDM, amount, currencyCode)
            || CanDispenseFromDevice(AtmDeviceType.REC, amount, currencyCode);
    }

    private static bool CanDispenseFromDevice(
        AtmDeviceType deviceType,
        decimal amount,
        string currencyCode)
    {
        // Kasetleri filtrele ve grupla
        var groups = Diagnostic.Diagnostics?.Values?
            .OfType<CdmDevice>()
            .SelectMany(d => d.Cassettes.Values)
            .Where(c =>
                c.CassetteType == deviceType.ToString() &&
                c.CurrencyCode == currencyCode &&
                c.Status < 4 &&
                c.CurrentCount > 0 &&
                c.BanknoteType > 0m)
            .GroupBy(c => c.BanknoteType)
            .Select(g => new { Denom = g.Key, Count = g.Sum(x => x.CurrentCount) })
            .OrderByDescending(g => g.Denom)
            .ToList();

        if (!groups.Any()) return false;

        // Hızlı kapasite kontrolü
        if (groups.Sum(g => g.Denom * g.Count) < amount)
            return false;

        // Seçilen algoritmaya göre kontrol et
        return _currentAlgorithm == DispenseAlgorithm.Greedy
            ? CanDispenseGreedy(amount, groups)
            : CanDispenseSubsetSum(amount, groups);
    }

    #region Greedy Algorithm (Hızlı)

    /// <summary>
    /// Greedy algoritma: En büyük kupürden başlayarak küçüğe doğru ilerler.
    /// Avantaj: Çok hızlı O(n), az bellek
    /// Dezavantaj: Bazı nadir durumlarda yanlış negatif verebilir
    /// </summary>
    private static bool CanDispenseGreedy(decimal amount, List<dynamic> cassettes)
    {
        decimal remaining = amount;

        foreach (var cassette in cassettes)
        {
            decimal banknoteType = cassette.Denom;
            int totalCount = cassette.Count;

            // Bu banknottan kaç tane kullanılabilir
            int notesToUse = (int)Math.Min(remaining / banknoteType, totalCount);
            remaining -= notesToUse * banknoteType;

            if (remaining == 0)
                return true;
        }

        return remaining == 0;
    }

    #endregion

    #region Subset-Sum Algorithm (Kesin)

    /// <summary>
    /// Subset-Sum algoritması (Binary Splitting ile optimize edilmiş)
    /// Avantaj: %100 matematiksel kesinlik, tüm kombinasyonları kontrol eder
    /// Dezavantaj: Daha fazla bellek ve işlem gerektirir
    /// </summary>
    private static bool CanDispenseSubsetSum(decimal amount, List<dynamic> cassettes)
    {
        var reachable = new HashSet<decimal> { 0m };

        foreach (var g in cassettes)
        {
            decimal denom = g.Denom;
            int remaining = g.Count;
            int pack = 1;

            // Binary splitting: count'u 1,2,4,8... parçalarına böl
            while (remaining > 0)
            {
                int take = Math.Min(pack, remaining);
                decimal chunk = denom * take;

                // Mevcut toplamları kopyala ve yeni toplamlar ekle
                foreach (var sum in reachable.ToArray())
                {
                    decimal newSum = sum + chunk;
                    if (newSum == amount) return true;  // Erken çıkış
                    if (newSum < amount) reachable.Add(newSum);
                }

                remaining -= take;
                pack <<= 1;  // 1,2,4,8,16... şeklinde ilerle
            }
        }

        return false;
    }

    #endregion
}

// ============ KULLANIM ÖRNEĞİ ============

internal class Program
{
    static void Main(string[] args)
    {
        // Diagnostic dictionary'yi başlat
        Diagnostic.Diagnostics = new Dictionary<AtmDeviceType, BaseDevice>();

        var cdmDevice = new CdmDevice
        {
            DeviceClass = AtmDeviceType.CDM,
            Status = 0,
            Cassettes = new Dictionary<int, CdmCassette>
            {
                {1, GenerateData.GetCdmCassette("CDM001",1,"REJ",0,"USD",0,0) },
                {2, GenerateData.GetCdmCassette("CDM001",2,"REJ",0,"USD",0,0) },
                {3, GenerateData.GetCdmCassette("CDM001",3,"CDM",50,"USD",5,0) },
                {4, GenerateData.GetCdmCassette("CDM001",4,"CDM",20,"USD",6,0) },
                {5, GenerateData.GetCdmCassette("CDM001",5,"CDM",100,"USD",4,0) },
                {6, GenerateData.GetCdmCassette("CDM001",6,"CDM",10,"USD",7,0) },
                {7, GenerateData.GetCdmCassette("CDM001",7,"REC",50,"USD",3,0) },
                {8, GenerateData.GetCdmCassette("CDM001",8,"REC",200,"USD",7,0) },
            }
        };

        // ÖNEMLİ: Device'ı dictionary'ye ekle!
        Diagnostic.Diagnostics.Add(AtmDeviceType.CDM, cdmDevice);

        // Test senaryoları
        Console.WriteLine("=== SUBSET-SUM ALGORITHM (Varsayılan) ===");
        Manager.SetAlgorithm(DispenseAlgorithm.SubsetSum);
        TestDispense();

        Console.WriteLine("\n=== GREEDY ALGORITHM ===");
        Manager.SetAlgorithm(DispenseAlgorithm.Greedy);
        TestDispense();
    }

    static void TestDispense()
    {
        // Test 1: Basit tutar
        Test(150, "USD", "150 USD"); // 100 + 2x20 + 10

        // Test 2: Büyük tutar
        Test(800, "USD", "800 USD"); // Toplam: 400+120+70 = 590, yetmez

        // Test 3: Tam eşleşme
        Test(100, "USD", "100 USD"); // 1x100

        // Test 4: Küçük tutar
        Test(30, "USD", "30 USD"); // 20 + 10

        // Test 5: Nadir durum (Greedy başarısız olabilir)
        Test(120, "USD", "120 USD"); // 100 + 20 veya 2x50 + 20
    }

    static void Test(decimal amount, string currency, string description)
    {
        bool result = Manager.IsAmountDispensible(amount, currency);
        Console.WriteLine($"{description}: {(result ? "✓ ÖDENEBİLİR" : "✗ ÖDENEMEYEN")}");
    }
}

// ============ MODEL CLASSES ============

public enum AtmDeviceType
{
    CIM, CDM, IDC, PIN, CHK, PTRR, PTRJ, REC
}

public class BaseDevice
{
    public AtmDeviceType DeviceClass { get; set; }
    public int Status { get; set; }
}

public class CdmDevice : BaseDevice
{
    public Dictionary<int, CdmCassette> Cassettes { get; set; }
}

public class BaseCassette
{
    public string DeviceId { get; set; }
    public short CassetteId { get; set; }
    public string CassetteType { get; set; }
    public decimal BanknoteType { get; set; }
    public string CurrencyCode { get; set; }
    public int CurrentCount { get; set; }
    public int Status { get; set; }
}

public class CdmCassette : BaseCassette { }

public class Diagnostic
{
    public static Dictionary<AtmDeviceType, BaseDevice> Diagnostics;
}

public class GenerateData
{
    public static CdmCassette GetCdmCassette(
        string deviceId,
        short cassetteId,
        string cassetteType,
        decimal banknoteType,
        string currencyCode,
        int currentCount,
        int status)
    {
        return new CdmCassette
        {
            DeviceId = deviceId,
            CassetteId = cassetteId,
            CassetteType = cassetteType,
            BanknoteType = banknoteType,
            CurrencyCode = currencyCode,
            CurrentCount = currentCount,
            Status = status
        };
    }
}
```
------------------DISPENSE PRO-----------------------------
```c#
using System;
using System.Collections.Generic;
using System.Linq;

public static class AtmButtonPlanner
{
    private const string DefaultCalcPrefix = "calc_";

    // ---------------------------
    // Yardımcı / Ortak
    // ---------------------------

    /// <summary>
    /// Verilen item'ların IsDispensible flag'lerini günceller
    /// </summary>
    public static void FlagDispensibility<T>(IEnumerable<T> items) where T : BaseAmount
    {
        if (items == null) return;

        foreach (var item in items.Where(i => i != null))
        {
            item.IsDispensible = Manager.IsAmountDispensible(item.Amount, item.CurrencyCode);
        }
    }

    /// <summary>
    /// Predefined amount'ların baseline snapshot'ını oluşturur
    /// </summary>
    public static IReadOnlyDictionary<string, decimal> SnapshotBaselineByIndex(IList<PredefinedAmount> pre)
    {
        if (pre == null) throw new ArgumentNullException(nameof(pre));
        return pre.ToDictionary(p => p.Index, p => p.Amount, StringComparer.Ordinal);
    }

    /// <summary>
    /// Baseline sözlüğünün tüm slot index'lerini kapsadığını doğrular
    /// </summary>
    private static void ValidateBaselineCoverage(
        IList<PredefinedAmount> slots,
        IReadOnlyDictionary<string, decimal> baselineByIndex)
    {
        if (slots == null) throw new ArgumentNullException(nameof(slots));
        if (baselineByIndex == null) throw new ArgumentNullException(nameof(baselineByIndex));
        
        var missingIndices = slots
            .Select(s => s.Index)
            .Where(idx => !baselineByIndex.ContainsKey(idx))
            .ToList();

        if (missingIndices.Any())
            throw new ArgumentException(
                $"Baseline sözlüğü şu index değerlerini içermiyor: {string.Join(", ", missingIndices)}");
    }

    /// <summary>
    /// Tüm slotların aynı para biriminde olduğunu doğrular
    /// </summary>
    private static void ValidateSingleCurrency(IList<PredefinedAmount> slots)
    {
        if (slots == null || slots.Count == 0) return;

        var currencies = slots
            .Select(s => s.CurrencyCode)
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (currencies.Count > 1)
            throw new InvalidOperationException(
                $"Slots birden fazla para birimi içeriyor: {string.Join(", ", currencies)}");
    }

    // ---------------------------
    // Replacement (tek para birimi, benzersiz değer garantisi)
    // ---------------------------

    private sealed class SlotReplacer
    {
        private readonly IList<PredefinedAmount> _slots;
        private readonly IReadOnlyDictionary<string, decimal> _baselineByIndex;
        private readonly List<ApiAmount> _remainingApiAmounts;
        private readonly HashSet<decimal> _currentAmounts;

        public SlotReplacer(
            IList<PredefinedAmount> slots,
            IEnumerable<ApiAmount> apiAmounts,
            IReadOnlyDictionary<string, decimal> baselineByIndex)
        {
            _slots = slots ?? throw new ArgumentNullException(nameof(slots));
            _baselineByIndex = baselineByIndex ?? throw new ArgumentNullException(nameof(baselineByIndex));
            _remainingApiAmounts = apiAmounts?.Where(a => a != null && a.IsDispensible).ToList() 
                ?? new List<ApiAmount>();
            _currentAmounts = new HashSet<decimal>(_slots.Select(s => s.Amount));
        }

        public void ReplaceSlots()
        {
            if (_slots.Count == 0 || _remainingApiAmounts.Count == 0) return;
            ValidateBaselineCoverage(_slots, _baselineByIndex);

            // 1) ENABLE → 2) DISABLE
            ReplaceInPhase(isDispensible: true);
            ReplaceInPhase(isDispensible: false);
        }

        private void ReplaceInPhase(bool isDispensible)
        {
            // Geriye doğru iterate ederek RemoveAt performansını artırıyoruz
            for (int i = _remainingApiAmounts.Count - 1; i >= 0; i--)
            {
                var api = _remainingApiAmounts[i];

                // Aynı tutarı ikinci kez yerleştirme
                if (_currentAmounts.Contains(api.Amount))
                    continue;

                var candidateIdx = Enumerable.Range(0, _slots.Count)
                    .Where(idx =>
                        _slots[idx].IsDispensible == isDispensible &&
                        string.IsNullOrEmpty(_slots[idx].ReplacedIndex))
                    .ToList();

                if (candidateIdx.Count == 0) continue;

                var best = FindNearestSlotIndex(candidateIdx, api.Amount);
                if (!best.HasValue) continue;

                ReplaceSlot(best.Value, api);
                _remainingApiAmounts.RemoveAt(i); // O(1) sondan silme
            }
        }

        private int? FindNearestSlotIndex(List<int> candidateIndices, decimal targetAmount)
        {
            if (candidateIndices.Count == 0) return null;

            return candidateIndices
                .OrderBy(i => Math.Abs(_baselineByIndex[_slots[i].Index] - targetAmount))
                .ThenBy(i => _baselineByIndex[_slots[i].Index])
                .ThenBy(i => i)
                .First();
        }

        private void ReplaceSlot(int slotIndex, ApiAmount apiAmount)
        {
            var slot = _slots[slotIndex];
            slot.Amount        = apiAmount.Amount;
            slot.AmountSource  = AmountSource.Api;
            slot.ReplacedIndex = apiAmount.Index;
            slot.IsDispensible = true;

            _currentAmounts.Add(slot.Amount);
        }
    }

    // ---------------------------
    // Fill (tek para birimi, cap ve benzersizlik korunur)
    // ---------------------------

    private sealed class DisabledSlotFiller
    {
        private readonly IList<PredefinedAmount> _slots;
        private readonly IReadOnlyDictionary<string, decimal> _baselineByIndex;
        private readonly decimal _maxAtmPayout;
        private readonly string _calcPrefix;

        public DisabledSlotFiller(
            IList<PredefinedAmount> slots,
            IReadOnlyDictionary<string, decimal> baselineByIndex,
            decimal maxAtmPayout,
            string calcPrefix = DefaultCalcPrefix)
        {
            _slots = slots ?? throw new ArgumentNullException(nameof(slots));
            _baselineByIndex = baselineByIndex ?? throw new ArgumentNullException(nameof(baselineByIndex));
            _maxAtmPayout = maxAtmPayout;
            _calcPrefix = calcPrefix ?? DefaultCalcPrefix;
        }

        public void FillSlots()
        {
            if (_slots.Count == 0) return;
            ValidateBaselineCoverage(_slots, _baselineByIndex);

            var currency = _slots[0].CurrencyCode;

            // BASE = listede en küçük ödenebilir
            var baseUnit = _slots
                .Where(s => s.IsDispensible)
                .Select(s => s.Amount)
                .DefaultIfEmpty(0m)
                .Min();

            if (baseUnit <= 0 || _maxAtmPayout < baseUnit) return;

            var used = new HashSet<decimal>(_slots.Select(s => s.Amount));
            int kMax = (int)Math.Floor(_maxAtmPayout / baseUnit);

            // Cap altında, çakışmayan ve dispensible olan adaylar
            var candidates = Enumerable.Range(1, kMax)
                .Select(k => baseUnit * k)
                .Where(v => !used.Contains(v) && Manager.IsAmountDispensible(v, currency))
                .ToList();

            int calcNo = 1;
            foreach (var slot in _slots
                .Where(s => !s.IsDispensible && string.IsNullOrEmpty(s.ReplacedIndex))
                .OrderBy(s => _baselineByIndex[s.Index]))
            {
                if (candidates.Count == 0) break;

                var cand = candidates[0];
                candidates.RemoveAt(0);

                slot.Amount        = cand;
                slot.AmountSource  = AmountSource.Calculated;
                slot.ReplacedIndex = $"{_calcPrefix}{calcNo++}";
                slot.IsDispensible = true;

                used.Add(cand);
            }
        }
    }

    // ---------------------------
    // Public API
    // ---------------------------

    public static void Replace_EnabledThenDisabled(
        IList<PredefinedAmount> slots,
        IEnumerable<ApiAmount> apiInOrder,
        IReadOnlyDictionary<string, decimal> baselineByIndex)
    {
        if (slots == null) throw new ArgumentNullException(nameof(slots));
        ValidateBaselineCoverage(slots, baselineByIndex);

        new SlotReplacer(slots, apiInOrder, baselineByIndex).ReplaceSlots();
    }

    public static void FillDisabledWithMultiplesFromCurrentMinDispensible(
        IList<PredefinedAmount> slots,
        IReadOnlyDictionary<string, decimal> baselineByIndex,
        decimal maxAtmPayout,
        string calcPrefix = DefaultCalcPrefix)
    {
        if (slots == null) throw new ArgumentNullException(nameof(slots));
        ValidateBaselineCoverage(slots, baselineByIndex);

        new DisabledSlotFiller(slots, baselineByIndex, maxAtmPayout, calcPrefix).FillSlots();
    }

    public static IList<PredefinedAmount> Plan_ReplaceThenFill(
        IList<PredefinedAmount> predefinedN,
        IList<ApiAmount> apiInOrder,
        decimal maxAtmPayout,
        bool runFlagStep = false)
    {
        if (predefinedN == null || predefinedN.Count == 0) 
            return predefinedN ?? new List<PredefinedAmount>();

        var baseline = SnapshotBaselineByIndex(predefinedN);

        if (runFlagStep)
        {
            FlagDispensibility(predefinedN);
            FlagDispensibility(apiInOrder);
        }

        Replace_EnabledThenDisabled(predefinedN, apiInOrder, baseline);
        FillDisabledWithMultiplesFromCurrentMinDispensible(predefinedN, baseline, maxAtmPayout);

        return predefinedN;
    }
}
```
------------------DISPENSE GPT-----------------------------
```c#
using System;
using System.Collections.Generic;
using System.Linq;

public static class Manager
{
    // ---------- Public API ----------

    /// <summary>
    /// amount tutarı, currencyCode para biriminde ÖDENEBİLİR mi?
    /// - CDM ve REC ayrı ayrı denenir (karıştırılmaz).
    /// - maxDispensibleAmount aşılırsa false.
    /// </summary>
    public static bool IsAmountDispensible(decimal amount, string currencyCode, decimal maxDispensibleAmount)
    {
        if (!ValidateInputs(amount, currencyCode, maxDispensibleAmount))
            return false;

        return IsAmountDispensibleOnDevice(AtmDeviceType.CDM, amount, currencyCode)
            || IsAmountDispensibleOnDevice(AtmDeviceType.REC, amount, currencyCode);
    }

    // Geriye dönük uyumluluk (cap olmadan)
    public static bool IsAmountDispensible(decimal amount, string currencyCode)
        => IsAmountDispensible(amount, currencyCode, decimal.MaxValue);

    // ---------- Private: orchestration per device ----------

    private static bool IsAmountDispensibleOnDevice(AtmDeviceType deviceType, decimal amount, string currencyCode)
    {
        // 1) Kullanılabilir kasetleri topla
        var usable = GetUsableCassettes(deviceType, currencyCode);
        if (!usable.Any())
            return false;

        // 2) Kupüre göre grupla (adetleri topla)
        var groups = AggregateByDenomination(usable);
        if (groups.Count == 0)
            return false;

        // 3) Hızlı kapasite kontrolü
        if (!HasSufficientCapacity(groups, amount))
            return false;

        // 4) Kısıtlı knapsack ile tam tutar var mı?
        return CanMakeExactAmount(groups, amount);
    }

    // ---------- Private: validation ----------

    private static bool ValidateInputs(decimal amount, string currencyCode, decimal maxDispensibleAmount)
    {
        if (amount <= 0m) return false;
        if (string.IsNullOrWhiteSpace(currencyCode)) return false;
        if (maxDispensibleAmount > 0m && amount > maxDispensibleAmount) return false;
        return true;
    }

    // ---------- Private: cassette retrieval & filtering ----------

    private static IEnumerable<CdmCassette> GetUsableCassettes(AtmDeviceType deviceType, string currencyCode)
    {
        var allCassettes = Diagnostic.Diagnostics?.Values?
            .OfType<CdmDevice>()
            .SelectMany(d => d.Cassettes.Values)
            ?? Enumerable.Empty<CdmCassette>();

        string typeName = deviceType.ToString();

        return allCassettes.Where(c =>
            c != null &&
            string.Equals(c.CassetteType, typeName, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(c.CurrencyCode, currencyCode, StringComparison.OrdinalIgnoreCase) &&
            c.Status < 4 &&
            c.CurrentCount > 0 &&
            c.BanknoteType > 0m);
    }

    // ---------- Private: grouping & capacity ----------

    private sealed class DenomGroup
    {
        public decimal Denom { get; init; }
        public int     Count { get; init; }
    }

    private static List<DenomGroup> AggregateByDenomination(IEnumerable<CdmCassette> cassettes)
    {
        return cassettes
            .GroupBy(c => c.BanknoteType)
            .Select(g => new DenomGroup { Denom = g.Key, Count = g.Sum(x => x.CurrentCount) })
            .OrderByDescending(g => g.Denom) // sıralama şart değil, okunurluk için
            .ToList();
    }

    private static bool HasSufficientCapacity(List<DenomGroup> groups, decimal amount)
    {
        decimal total = groups.Sum(g => g.Denom * g.Count);
        return total >= amount;
    }

    // ---------- Private: bounded subset-sum (binary splitting) ----------

    /// <summary>
    /// Kupür+adet kısıtlarıyla tam 'amount' yapılabiliyor mu?
    /// Binary splitting ile adetleri 1,2,4,... paketlerine bölüp 0/1 knapsack gibi ilerler.
    /// </summary>
    private static bool CanMakeExactAmount(List<DenomGroup> groups, decimal amount)
    {
        var reachable = new HashSet<decimal> { 0m };

        foreach (var g in groups)
        {
            int remaining = g.Count;
            int pack = 1;

            while (remaining > 0)
            {
                int take = Math.Min(pack, remaining);
                decimal chunk = g.Denom * take;

                // snapshot ile genişlet (iterasyon sırasında set'i büyütme)
                foreach (var s in reachable.ToArray())
                {
                    var ns = s + chunk;
                    if (ns > amount) continue;
                    if (ns == amount) return true; // erken çıkış
                    reachable.Add(ns);
                }

                remaining -= take;
                pack <<= 1;
            }
        }

        return false;
    }
}

```
------------------DISPENSE PRO-----------------------------
```c#
using System;
using System.Collections.Generic;
using System.Linq;

public enum DispenseAlgorithm
{
    Greedy,      // Hızlı, basit, %95+ başarı oranı
    SubsetSum    // Yavaş ama %100 matematiksel kesinlik
}

public static class Manager
{
    // Varsayılan algoritma (istediğin zaman değiştirebilirsin)
    private static DispenseAlgorithm _currentAlgorithm = DispenseAlgorithm.SubsetSum;

    /// <summary>
    /// Kullanılacak algoritmayı ayarla
    /// </summary>
    public static void SetAlgorithm(DispenseAlgorithm algorithm)
    {
        _currentAlgorithm = algorithm;
    }

    // ---------- Public API ----------

    /// <summary>
    /// amount tutarı, currencyCode para biriminde ÖDENEBİLİR mi?
    /// - CDM ve REC ayrı ayrı denenir (karıştırılmaz).
    /// - maxDispensibleAmount aşılırsa false.
    /// </summary>
    public static bool IsAmountDispensible(decimal amount, string currencyCode, decimal maxDispensibleAmount)
    {
        if (!ValidateInputs(amount, currencyCode, maxDispensibleAmount))
            return false;

        return IsAmountDispensibleOnDevice(AtmDeviceType.CDM, amount, currencyCode)
            || IsAmountDispensibleOnDevice(AtmDeviceType.REC, amount, currencyCode);
    }

    // Geriye dönük uyumluluk (cap olmadan)
    public static bool IsAmountDispensible(decimal amount, string currencyCode)
        => IsAmountDispensible(amount, currencyCode, decimal.MaxValue);

    // ---------- Private: orchestration per device ----------

    private static bool IsAmountDispensibleOnDevice(AtmDeviceType deviceType, decimal amount, string currencyCode)
    {
        // 1) Kullanılabilir kasetleri topla
        var usable = GetUsableCassettes(deviceType, currencyCode);
        if (!usable.Any())
            return false;

        // 2) Kupüre göre grupla (adetleri topla)
        var groups = AggregateByDenomination(usable);
        if (groups.Count == 0)
            return false;

        // 3) Hızlı kapasite kontrolü
        if (!HasSufficientCapacity(groups, amount))
            return false;

        // 4) Seçilen algoritmaya göre kontrol et
        return _currentAlgorithm == DispenseAlgorithm.Greedy
            ? CanMakeExactAmount_Greedy(groups, amount)
            : CanMakeExactAmount_SubsetSum(groups, amount);
    }

    // ---------- Private: validation ----------

    private static bool ValidateInputs(decimal amount, string currencyCode, decimal maxDispensibleAmount)
    {
        if (amount <= 0m) return false;
        if (string.IsNullOrWhiteSpace(currencyCode)) return false;
        if (maxDispensibleAmount > 0m && amount > maxDispensibleAmount) return false;
        return true;
    }

    // ---------- Private: cassette retrieval & filtering ----------

    private static IEnumerable<CdmCassette> GetUsableCassettes(AtmDeviceType deviceType, string currencyCode)
    {
        var allCassettes = Diagnostic.Diagnostics?.Values?
            .OfType<CdmDevice>()
            .SelectMany(d => d.Cassettes.Values)
            ?? Enumerable.Empty<CdmCassette>();

        string typeName = deviceType.ToString();

        return allCassettes.Where(c =>
            c != null &&
            string.Equals(c.CassetteType, typeName, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(c.CurrencyCode, currencyCode, StringComparison.OrdinalIgnoreCase) &&
            c.Status < 4 &&
            c.CurrentCount > 0 &&
            c.BanknoteType > 0m);
    }

    // ---------- Private: grouping & capacity ----------

    private sealed class DenomGroup
    {
        public decimal Denom { get; init; }
        public int     Count { get; init; }
    }

    private static List<DenomGroup> AggregateByDenomination(IEnumerable<CdmCassette> cassettes)
    {
        return cassettes
            .GroupBy(c => c.BanknoteType)
            .Select(g => new DenomGroup { Denom = g.Key, Count = g.Sum(x => x.CurrentCount) })
            .OrderByDescending(g => g.Denom) // sıralama şart değil, okunurluk için
            .ToList();
    }

    private static bool HasSufficientCapacity(List<DenomGroup> groups, decimal amount)
    {
        decimal total = groups.Sum(g => g.Denom * g.Count);
        return total >= amount;
    }

    // ---------- GREEDY ALGORITHM ----------

    /// <summary>
    /// Greedy algoritma: En büyük kupürden başlayarak küçüğe doğru ilerler.
    /// Avantaj: Çok hızlı O(n), az bellek
    /// Dezavantaj: Bazı nadir durumlarda yanlış negatif verebilir
    /// </summary>
    private static bool CanMakeExactAmount_Greedy(List<DenomGroup> groups, decimal amount)
    {
        decimal remaining = amount;

        foreach (var group in groups)
        {
            // Bu kupürden kaç tane kullanılabilir
            int notesToUse = (int)Math.Min(remaining / group.Denom, group.Count);
            remaining -= notesToUse * group.Denom;

            if (remaining == 0)
                return true;
        }

        return remaining == 0;
    }

    // ---------- SUBSET-SUM ALGORITHM ----------

    /// <summary>
    /// Subset-Sum algoritması (Binary Splitting ile optimize edilmiş)
    /// Kupür+adet kısıtlarıyla tam 'amount' yapılabiliyor mu?
    /// Binary splitting ile adetleri 1,2,4,... paketlerine bölüp 0/1 knapsack gibi ilerler.
    /// Avantaj: %100 matematiksel kesinlik, tüm kombinasyonları kontrol eder
    /// Dezavantaj: Daha fazla bellek ve işlem gerektirir
    /// </summary>
    private static bool CanMakeExactAmount_SubsetSum(List<DenomGroup> groups, decimal amount)
    {
        var reachable = new HashSet<decimal> { 0m };

        foreach (var g in groups)
        {
            int remaining = g.Count;
            int pack = 1;

            // Binary splitting: count'u 1,2,4,8... paketlerine böl
            while (remaining > 0)
            {
                int take = Math.Min(pack, remaining);
                decimal chunk = g.Denom * take;

                // snapshot ile genişlet (iterasyon sırasında set'i büyütme)
                foreach (var s in reachable.ToArray())
                {
                    var ns = s + chunk;
                    if (ns > amount) continue;
                    if (ns == amount) return true; // erken çıkış
                    reachable.Add(ns);
                }

                remaining -= take;
                pack <<= 1; // 1,2,4,8,16... şeklinde ilerle
            }
        }

        return false;
    }
}

// ============ KULLANIM ÖRNEĞİ ============

internal class Program
{
    static void Main(string[] args)
    {
        // Diagnostic dictionary'yi başlat
        Diagnostic.Diagnostics = new Dictionary<AtmDeviceType, BaseDevice>();

        var cdmDevice = new CdmDevice
        {
            DeviceClass = AtmDeviceType.CDM,
            Status = 0,
            Cassettes = new Dictionary<int, CdmCassette>
            {
                {1, GenerateData.GetCdmCassette("CDM001",1,"REJ",0,"USD",0,0) },
                {2, GenerateData.GetCdmCassette("CDM001",2,"REJ",0,"USD",0,0) },
                {3, GenerateData.GetCdmCassette("CDM001",3,"CDM",50,"USD",5,0) },
                {4, GenerateData.GetCdmCassette("CDM001",4,"CDM",20,"USD",6,0) },
                {5, GenerateData.GetCdmCassette("CDM001",5,"CDM",100,"USD",4,0) },
                {6, GenerateData.GetCdmCassette("CDM001",6,"CDM",10,"USD",7,0) },
                {7, GenerateData.GetCdmCassette("CDM001",7,"REC",50,"USD",3,0) },
                {8, GenerateData.GetCdmCassette("CDM001",8,"REC",200,"USD",7,0) },
            }
        };

        // ÖNEMLİ: Device'ı dictionary'ye ekle!
        Diagnostic.Diagnostics.Add(AtmDeviceType.CDM, cdmDevice);

        // Test senaryoları
        Console.WriteLine("=== SUBSET-SUM ALGORITHM (Varsayılan) ===");
        Manager.SetAlgorithm(DispenseAlgorithm.SubsetSum);
        TestDispense();

        Console.WriteLine("\n=== GREEDY ALGORITHM ===");
        Manager.SetAlgorithm(DispenseAlgorithm.Greedy);
        TestDispense();

        // Nadir durum testi: Greedy vs SubsetSum farkı
        Console.WriteLine("\n=== NADIR DURUM: Greedy Başarısız, SubsetSum Başarılı ===");
        TestEdgeCase();
    }

    static void TestDispense()
    {
        // Test 1: Basit tutar
        Test(150, "USD", "150 USD"); // 100 + 2x20 + 10

        // Test 2: Büyük tutar
        Test(800, "USD", "800 USD"); // Toplam: 400+120+70 = 590, yetmez

        // Test 3: Tam eşleşme
        Test(100, "USD", "100 USD"); // 1x100

        // Test 4: Küçük tutar
        Test(30, "USD", "30 USD"); // 20 + 10

        // Test 5: Karmaşık kombinasyon
        Test(120, "USD", "120 USD"); // 100 + 20
    }

    static void TestEdgeCase()
    {
        // Greedy'nin başarısız olabileceği bir durum için özel test
        // Örnek: 60 ve 50 kupürleri varsa, 100 için Greedy 1x60 kullanır ve takılır
        // Ama SubsetSum 2x50 kombinasyonunu bulur

        Diagnostic.Diagnostics.Clear();
        var edgeDevice = new CdmDevice
        {
            DeviceClass = AtmDeviceType.CDM,
            Status = 0,
            Cassettes = new Dictionary<int, CdmCassette>
            {
                {1, GenerateData.GetCdmCassette("CDM002",1,"CDM",60,"USD",3,0) },
                {2, GenerateData.GetCdmCassette("CDM002",2,"CDM",50,"USD",2,0) },
            }
        };
        Diagnostic.Diagnostics.Add(AtmDeviceType.CDM, edgeDevice);

        Console.WriteLine("Kasetler: 3x[60$], 2x[50$]");
        
        Manager.SetAlgorithm(DispenseAlgorithm.Greedy);
        bool greedyResult = Manager.IsAmountDispensible(100, "USD");
        Console.WriteLine($"100$ Greedy:     {(greedyResult ? "✓ ÖDENEBİLİR" : "✗ ÖDENEMEYEN")}");

        Manager.SetAlgorithm(DispenseAlgorithm.SubsetSum);
        bool subsetResult = Manager.IsAmountDispensible(100, "USD");
        Console.WriteLine($"100$ SubsetSum:  {(subsetResult ? "✓ ÖDENEBİLİR" : "✗ ÖDENEMEYEN")}");

        if (greedyResult != subsetResult)
            Console.WriteLine("⚠️ FARK: Greedy yanlış negatif verdi, SubsetSum doğru buldu!");
    }

    static void Test(decimal amount, string currency, string description)
    {
        bool result = Manager.IsAmountDispensible(amount, currency);
        Console.WriteLine($"{description}: {(result ? "✓ ÖDENEBİLİR" : "✗ ÖDENEMEYEN")}");
    }
}

// ============ MODEL CLASSES ============

public enum AtmDeviceType
{
    CIM, CDM, IDC, PIN, CHK, PTRR, PTRJ, REC
}

public class BaseDevice
{
    public AtmDeviceType DeviceClass { get; set; }
    public int Status { get; set; }
}

public class CdmDevice : BaseDevice
{
    public Dictionary<int, CdmCassette> Cassettes { get; set; }
}

public class BaseCassette
{
    public string DeviceId { get; set; }
    public short CassetteId { get; set; }
    public string CassetteType { get; set; }
    public decimal BanknoteType { get; set; }
    public string CurrencyCode { get; set; }
    public int CurrentCount { get; set; }
    public int Status { get; set; }
}

public class CdmCassette : BaseCassette { }

public class Diagnostic
{
    public static Dictionary<AtmDeviceType, BaseDevice> Diagnostics;
}

public class GenerateData
{
    public static CdmCassette GetCdmCassette(
        string deviceId,
        short cassetteId,
        string cassetteType,
        decimal banknoteType,
        string currencyCode,
        int currentCount,
        int status)
    {
        return new CdmCassette
        {
            DeviceId = deviceId,
            CassetteId = cassetteId,
            CassetteType = cassetteType,
            BanknoteType = banknoteType,
            CurrencyCode = currencyCode,
            CurrentCount = currentCount,
            Status = status
        };
    }
}
```

------------------DISPENSE CLAUDE MAX ITEMS RESTRICTION-----------------------------
```c#
using System;
using System.Collections.Generic;
using System.Linq;

public enum DispenseAlgorithm
{
    Greedy,      // Hızlı, basit, %95+ başarı oranı
    SubsetSum    // Yavaş ama %100 matematiksel kesinlik
}

public static class Manager
{
    // Varsayılan algoritma (istediğin zaman değiştirebilirsin)
    private static DispenseAlgorithm _currentAlgorithm = DispenseAlgorithm.SubsetSum;
    
    // Maksimum dispensible banknot adedi (bir seferde verilebilecek maksimum banknot sayısı)
    private static int _maxDispensibleItems = int.MaxValue;

    /// <summary>
    /// Kullanılacak algoritmayı ayarla
    /// </summary>
    public static void SetAlgorithm(DispenseAlgorithm algorithm)
    {
        _currentAlgorithm = algorithm;
    }

    /// <summary>
    /// Maksimum dispensible banknot adedini ayarla
    /// </summary>
    public static void SetMaxDispensibleItems(int maxItems)
    {
        _maxDispensibleItems = maxItems > 0 ? maxItems : int.MaxValue;
    }

    // ---------- Public API ----------

    /// <summary>
    /// amount tutarı, currencyCode para biriminde ÖDENEBİLİR mi?
    /// - CDM ve REC ayrı ayrı denenir (karıştırılmaz).
    /// - maxDispensibleItems (maksimum banknot adedi) aşılırsa false.
    /// </summary>
    public static bool IsAmountDispensible(decimal amount, string currencyCode, int maxDispensibleItems)
    {
        if (!ValidateInputs(amount, currencyCode))
            return false;

        var prevMaxItems = _maxDispensibleItems;
        _maxDispensibleItems = maxDispensibleItems;
        
        bool result = IsAmountDispensibleOnDevice(AtmDeviceType.CDM, amount, currencyCode)
                   || IsAmountDispensibleOnDevice(AtmDeviceType.REC, amount, currencyCode);
        
        _maxDispensibleItems = prevMaxItems;
        return result;
    }

    // Geriye dönük uyumluluk (limit olmadan)
    public static bool IsAmountDispensible(decimal amount, string currencyCode)
        => IsAmountDispensible(amount, currencyCode, _maxDispensibleItems);

    // ---------- Private: orchestration per device ----------

    private static bool IsAmountDispensibleOnDevice(AtmDeviceType deviceType, decimal amount, string currencyCode)
    {
        // 1) Kullanılabilir kasetleri topla
        var usable = GetUsableCassettes(deviceType, currencyCode);
        if (!usable.Any())
            return false;

        // 2) Kupüre göre grupla (adetleri topla)
        var groups = AggregateByDenomination(usable);
        if (groups.Count == 0)
            return false;

        // 3) Hızlı kapasite kontrolü
        if (!HasSufficientCapacity(groups, amount))
            return false;

        // 4) Seçilen algoritmaya göre kontrol et
        return _currentAlgorithm == DispenseAlgorithm.Greedy
            ? CanMakeExactAmount_Greedy(groups, amount)
            : CanMakeExactAmount_SubsetSum(groups, amount);
    }

    // ---------- Private: validation ----------

    private static bool ValidateInputs(decimal amount, string currencyCode)
    {
        if (amount <= 0m) return false;
        if (string.IsNullOrWhiteSpace(currencyCode)) return false;
        return true;
    }

    // ---------- Private: cassette retrieval & filtering ----------

    private static IEnumerable<CdmCassette> GetUsableCassettes(AtmDeviceType deviceType, string currencyCode)
    {
        var allCassettes = Diagnostic.Diagnostics?.Values?
            .OfType<CdmDevice>()
            .SelectMany(d => d.Cassettes.Values)
            ?? Enumerable.Empty<CdmCassette>();

        string typeName = deviceType.ToString();

        return allCassettes.Where(c =>
            c != null &&
            string.Equals(c.CassetteType, typeName, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(c.CurrencyCode, currencyCode, StringComparison.OrdinalIgnoreCase) &&
            c.Status < 4 &&
            c.CurrentCount > 0 &&
            c.BanknoteType > 0m);
    }

    // ---------- Private: grouping & capacity ----------

    private sealed class DenomGroup
    {
        public decimal Denom { get; init; }
        public int     Count { get; init; }
    }

    private static List<DenomGroup> AggregateByDenomination(IEnumerable<CdmCassette> cassettes)
    {
        return cassettes
            .GroupBy(c => c.BanknoteType)
            .Select(g => new DenomGroup { Denom = g.Key, Count = g.Sum(x => x.CurrentCount) })
            .OrderByDescending(g => g.Denom) // sıralama şart değil, okunurluk için
            .ToList();
    }

    private static bool HasSufficientCapacity(List<DenomGroup> groups, decimal amount)
    {
        decimal total = groups.Sum(g => g.Denom * g.Count);
        return total >= amount;
    }

    // ---------- GREEDY ALGORITHM ----------

    /// <summary>
    /// Greedy algoritma: En büyük kupürden başlayarak küçüğe doğru ilerler.
    /// Avantaj: Çok hızlı O(n), az bellek
    /// Dezavantaj: Bazı nadir durumlarda yanlış negatif verebilir
    /// </summary>
    private static bool CanMakeExactAmount_Greedy(List<DenomGroup> groups, decimal amount)
    {
        decimal remaining = amount;
        int totalNotesUsed = 0;

        foreach (var group in groups)
        {
            // Bu kupürden kaç tane kullanılabilir (hem tutar hem banknot adedi limiti)
            int maxByAmount = (int)(remaining / group.Denom);
            int maxByCount = group.Count;
            int maxByLimit = _maxDispensibleItems - totalNotesUsed;
            
            int notesToUse = Math.Min(Math.Min(maxByAmount, maxByCount), maxByLimit);
            
            remaining -= notesToUse * group.Denom;
            totalNotesUsed += notesToUse;

            if (remaining == 0)
                return true;
            
            if (totalNotesUsed >= _maxDispensibleItems)
                return false; // Limit aşıldı ama tutar tamamlanmadı
        }

        return remaining == 0;
    }

    // ---------- SUBSET-SUM ALGORITHM ----------

    /// <summary>
    /// Subset-Sum algoritması (Binary Splitting ile optimize edilmiş)
    /// Kupür+adet kısıtlarıyla tam 'amount' yapılabiliyor mu?
    /// Binary splitting ile adetleri 1,2,4,... paketlerine bölüp 0/1 knapsack gibi ilerler.
    /// Avantaj: %100 matematiksel kesinlik, tüm kombinasyonları kontrol eder
    /// Dezavantaj: Daha fazla bellek ve işlem gerektirir
    /// </summary>
    private static bool CanMakeExactAmount_SubsetSum(List<DenomGroup> groups, decimal amount)
    {
        // State: (tutar, kullanılan_banknot_adedi)
        var reachable = new HashSet<(decimal amount, int noteCount)> { (0m, 0) };

        foreach (var g in groups)
        {
            int remaining = g.Count;
            int pack = 1;

            // Binary splitting: count'u 1,2,4,8... paketlerine böl
            while (remaining > 0)
            {
                int take = Math.Min(pack, remaining);
                decimal chunk = g.Denom * take;

                // snapshot ile genişlet (iterasyon sırasında set'i büyütme)
                var snapshot = reachable.ToArray();
                foreach (var (amt, noteCount) in snapshot)
                {
                    var newAmount = amt + chunk;
                    var newNoteCount = noteCount + take;
                    
                    // Maksimum banknot adedi kontrolü
                    if (newNoteCount > _maxDispensibleItems) continue;
                    
                    // Tutar kontrolü
                    if (newAmount > amount) continue;
                    if (newAmount == amount) return true; // erken çıkış
                    
                    reachable.Add((newAmount, newNoteCount));
                }

                remaining -= take;
                pack <<= 1; // 1,2,4,8,16... şeklinde ilerle
            }
        }

        return false;
    }
}

// ============ KULLANIM ÖRNEĞİ ============

internal class Program
{
    static void Main(string[] args)
    {
        // Diagnostic dictionary'yi başlat
        Diagnostic.Diagnostics = new Dictionary<AtmDeviceType, BaseDevice>();

        var cdmDevice = new CdmDevice
        {
            DeviceClass = AtmDeviceType.CDM,
            Status = 0,
            Cassettes = new Dictionary<int, CdmCassette>
            {
                {1, GenerateData.GetCdmCassette("CDM001",1,"REJ",0,"USD",0,0) },
                {2, GenerateData.GetCdmCassette("CDM001",2,"REJ",0,"USD",0,0) },
                {3, GenerateData.GetCdmCassette("CDM001",3,"CDM",50,"USD",5,0) },
                {4, GenerateData.GetCdmCassette("CDM001",4,"CDM",20,"USD",6,0) },
                {5, GenerateData.GetCdmCassette("CDM001",5,"CDM",100,"USD",4,0) },
                {6, GenerateData.GetCdmCassette("CDM001",6,"CDM",10,"USD",7,0) },
                {7, GenerateData.GetCdmCassette("CDM001",7,"REC",50,"USD",3,0) },
                {8, GenerateData.GetCdmCassette("CDM001",8,"REC",200,"USD",7,0) },
            }
        };

        // ÖNEMLİ: Device'ı dictionary'ye ekle!
        Diagnostic.Diagnostics.Add(AtmDeviceType.CDM, cdmDevice);

        // Maksimum banknot adedi limiti ayarla (örnek: 40 banknot)
        Manager.SetMaxDispensibleItems(40);

        // Test senaryoları
        Console.WriteLine("=== SUBSET-SUM ALGORITHM (Varsayılan) ===");
        Manager.SetAlgorithm(DispenseAlgorithm.SubsetSum);
        TestDispense();

        Console.WriteLine("\n=== GREEDY ALGORITHM ===");
        Manager.SetAlgorithm(DispenseAlgorithm.Greedy);
        TestDispense();

        // Maksimum banknot adedi testi
        Console.WriteLine("\n=== MAKSİMUM BANKNOT ADEDİ TESTİ ===");
        TestMaxNoteLimit();
    }

    static void TestDispense()
    {
        // Test 1: Basit tutar
        Test(150, "USD", "150 USD"); // 100 + 2x20 + 10 = 4 banknot

        // Test 2: Büyük tutar
        Test(800, "USD", "800 USD"); // Toplam: 400+120+70 = 590, yetmez

        // Test 3: Tam eşleşme
        Test(100, "USD", "100 USD"); // 1x100 = 1 banknot

        // Test 4: Küçük tutar
        Test(30, "USD", "30 USD"); // 20 + 10 = 2 banknot

        // Test 5: Karmaşık kombinasyon
        Test(120, "USD", "120 USD"); // 100 + 20 = 2 banknot
    }

    static void TestMaxNoteLimit()
    {
        // Örnek: 500$ çekmek için 50x10$ = 50 banknot gerekir
        // Ama limit 40 banknot ise ödenemez olmalı
        
        Manager.SetMaxDispensibleItems(40);
        Console.WriteLine("Maksimum banknot limiti: 40");
        
        Manager.SetAlgorithm(DispenseAlgorithm.SubsetSum);
        
        // 500$ = En az 5 banknot gerekir (5x100)
        Test(500, "USD", "500$ (5 banknot gerekir)");
        
        // 100$ = 10x10 = 10 banknot (limit altında)
        Test(100, "USD", "100$ (10 banknot ile yapılabilir)");
        
        // Küçük limit testi
        Manager.SetMaxDispensibleItems(3);
        Console.WriteLine("\nMaksimum banknot limiti: 3");
        Test(150, "USD", "150$ (min 2 banknot: 100+50)");
        Test(30, "USD", "30$ (min 3 banknot: 10+10+10)");
    }

    static void Test(decimal amount, string currency, string description)
    {
        bool result = Manager.IsAmountDispensible(amount, currency);
        Console.WriteLine($"{description}: {(result ? "✓ ÖDENEBİLİR" : "✗ ÖDENEMEYEN")}");
    }
}

// ============ MODEL CLASSES ============

public enum AtmDeviceType
{
    CIM, CDM, IDC, PIN, CHK, PTRR, PTRJ, REC
}

public class BaseDevice
{
    public AtmDeviceType DeviceClass { get; set; }
    public int Status { get; set; }
}

public class CdmDevice : BaseDevice
{
    public Dictionary<int, CdmCassette> Cassettes { get; set; }
}

public class BaseCassette
{
    public string DeviceId { get; set; }
    public short CassetteId { get; set; }
    public string CassetteType { get; set; }
    public decimal BanknoteType { get; set; }
    public string CurrencyCode { get; set; }
    public int CurrentCount { get; set; }
    public int Status { get; set; }
}

public class CdmCassette : BaseCassette { }

public class Diagnostic
{
    public static Dictionary<AtmDeviceType, BaseDevice> Diagnostics;
}

public class GenerateData
{
    public static CdmCassette GetCdmCassette(
        string deviceId,
        short cassetteId,
        string cassetteType,
        decimal banknoteType,
        string currencyCode,
        int currentCount,
        int status)
    {
        return new CdmCassette
        {
            DeviceId = deviceId,
            CassetteId = cassetteId,
            CassetteType = cassetteType,
            BanknoteType = banknoteType,
            CurrencyCode = currencyCode,
            CurrentCount = currentCount,
            Status = status
        };
    }
}
```

------------------DISPENSE GPT MAX ITEM RESTRICTION-----------------------------
```c#
using System;
using System.Collections.Generic;
using System.Linq;

public enum DispenseAlgorithm
{
    Greedy,      // Hızlı, basit, %95+ başarı oranı
    SubsetSum    // Yavaş ama %100 matematiksel kesinlik
}

public static class Manager
{
    // Varsayılan algoritma (istediğin zaman değiştirebilirsin)
    private static DispenseAlgorithm _currentAlgorithm = DispenseAlgorithm.SubsetSum;
    
    // Maksimum dispensible banknot adedi (bir seferde verilebilecek maksimum banknot sayısı)
    private static int _maxDispensibleItems = int.MaxValue;

    /// <summary>
    /// Kullanılacak algoritmayı ayarla
    /// </summary>
    public static void SetAlgorithm(DispenseAlgorithm algorithm)
    {
        _currentAlgorithm = algorithm;
    }

    /// <summary>
    /// Maksimum dispensible banknot adedini ayarla
    /// </summary>
    public static void SetMaxDispensibleItems(int maxItems)
    {
        _maxDispensibleItems = maxItems > 0 ? maxItems : int.MaxValue;
    }

    // ---------- Public API ----------

    /// <summary>
    /// amount tutarı, currencyCode para biriminde ÖDENEBİLİR mi?
    /// - CDM ve REC ayrı ayrı denenir (karıştırılmaz).
    /// - maxDispensibleItems (maksimum banknot adedi) aşılırsa false.
    /// </summary>
    public static bool IsAmountDispensible(decimal amount, string currencyCode, int maxDispensibleItems)
    {
        if (!ValidateInputs(amount, currencyCode))
            return false;

        var prevMaxItems = _maxDispensibleItems;
        _maxDispensibleItems = maxDispensibleItems;
        
        bool result = IsAmountDispensibleOnDevice(AtmDeviceType.CDM, amount, currencyCode)
                   || IsAmountDispensibleOnDevice(AtmDeviceType.REC, amount, currencyCode);
        
        _maxDispensibleItems = prevMaxItems;
        return result;
    }

    // Geriye dönük uyumluluk (limit olmadan)
    public static bool IsAmountDispensible(decimal amount, string currencyCode)
        => IsAmountDispensible(amount, currencyCode, _maxDispensibleItems);

    // ---------- Private: orchestration per device ----------

    private static bool IsAmountDispensibleOnDevice(AtmDeviceType deviceType, decimal amount, string currencyCode)
    {
        // 1) Kullanılabilir kasetleri topla
        var usable = GetUsableCassettes(deviceType, currencyCode);
        if (!usable.Any())
            return false;

        // 2) Kupüre göre grupla (adetleri topla)
        var groups = AggregateByDenomination(usable);
        if (groups.Count == 0)
            return false;

        // 3) Hızlı kapasite kontrolü
        if (!HasSufficientCapacity(groups, amount))
            return false;

        // 4) Seçilen algoritmaya göre kontrol et
        return _currentAlgorithm == DispenseAlgorithm.Greedy
            ? CanMakeExactAmount_Greedy(groups, amount)
            : CanMakeExactAmount_SubsetSum(groups, amount);
    }

    // ---------- Private: validation ----------

    private static bool ValidateInputs(decimal amount, string currencyCode)
    {
        if (amount <= 0m) return false;
        if (string.IsNullOrWhiteSpace(currencyCode)) return false;
        return true;
    }

    // ---------- Private: cassette retrieval & filtering ----------

    private static IEnumerable<CdmCassette> GetUsableCassettes(AtmDeviceType deviceType, string currencyCode)
    {
        var allCassettes = Diagnostic.Diagnostics?.Values?
            .OfType<CdmDevice>()
            .SelectMany(d => d.Cassettes.Values)
            ?? Enumerable.Empty<CdmCassette>();

        string typeName = deviceType.ToString();

        return allCassettes.Where(c =>
            c != null &&
            string.Equals(c.CassetteType, typeName, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(c.CurrencyCode, currencyCode, StringComparison.OrdinalIgnoreCase) &&
            c.Status < 4 &&
            c.CurrentCount > 0 &&
            c.BanknoteType > 0m);
    }

    // ---------- Private: grouping & capacity ----------

    private sealed class DenomGroup
    {
        public decimal Denom { get; init; }
        public int     Count { get; init; }
    }

    private static List<DenomGroup> AggregateByDenomination(IEnumerable<CdmCassette> cassettes)
    {
        return cassettes
            .GroupBy(c => c.BanknoteType)
            .Select(g => new DenomGroup { Denom = g.Key, Count = g.Sum(x => x.CurrentCount) })
            .OrderByDescending(g => g.Denom) // sıralama şart değil, okunurluk için
            .ToList();
    }

    private static bool HasSufficientCapacity(List<DenomGroup> groups, decimal amount)
    {
        decimal total = groups.Sum(g => g.Denom * g.Count);
        return total >= amount;
    }

    // ---------- GREEDY ALGORITHM ----------

    /// <summary>
    /// Greedy algoritma: En büyük kupürden başlayarak küçüğe doğru ilerler.
    /// Avantaj: Çok hızlı O(n), az bellek
    /// Dezavantaj: Bazı nadir durumlarda yanlış negatif verebilir
    /// </summary>
    private static bool CanMakeExactAmount_Greedy(List<DenomGroup> groups, decimal amount)
    {
        decimal remaining = amount;
        int totalNotesUsed = 0;

        foreach (var group in groups)
        {
            // Bu kupürden kaç tane kullanılabilir (hem tutar hem banknot adedi limiti)
            int maxByAmount = (int)(remaining / group.Denom);
            int maxByCount = group.Count;
            int maxByLimit = _maxDispensibleItems - totalNotesUsed;
            
            int notesToUse = Math.Min(Math.Min(maxByAmount, maxByCount), maxByLimit);
            
            remaining -= notesToUse * group.Denom;
            totalNotesUsed += notesToUse;

            if (remaining == 0)
                return true;
            
            if (totalNotesUsed >= _maxDispensibleItems)
                return false; // Limit aşıldı ama tutar tamamlanmadı
        }

        return remaining == 0;
    }

    // ---------- SUBSET-SUM ALGORITHM ----------

    /// <summary>
    /// Subset-Sum algoritması (Binary Splitting ile optimize edilmiş)
    /// Kupür+adet kısıtlarıyla tam 'amount' yapılabiliyor mu?
    /// reachable[sum] = o suma ulaşmak için gereken minimum banknot adedi (<= _maxDispensibleItems)
    /// Binary splitting ile adetleri 1,2,4,... paketlerine bölüp 0/1 knapsack gibi ilerler.
    /// Avantaj: %100 matematiksel kesinlik, bellek optimize
    /// </summary>
    private static bool CanMakeExactAmount_SubsetSum(List<DenomGroup> groups, decimal amount)
    {
        // Input validasyonları
        if (amount <= 0m) return false;
        if (_maxDispensibleItems <= 0) return false;
        if (groups == null || groups.Count == 0) return false;

        // Geçerli grupları filtrele ve sırala (local değişken kullan, parametreyi değiştirme)
        var validGroups = groups
            .Where(g => g.Denom > 0m && g.Count > 0)
            .OrderByDescending(g => g.Denom) // şart değil; okunurluk için
            .ToList();

        if (validGroups.Count == 0) return false;

        // Hızlı kapasite kontrolü (erken çıkış optimizasyonu)
        decimal capacity = validGroups.Sum(g => g.Denom * g.Count);
        if (capacity < amount) return false;

        // Key: ulaşılan tutar, Value: o tutara ulaşmak için gereken EN AZ banknot sayısı
        var reachable = new Dictionary<decimal, int> { [0m] = 0 };

        foreach (var g in validGroups)
        {
            int remaining = g.Count;
            int pack = 1;

            // Binary splitting: count'u 1,2,4,8... paketlerine böl
            while (remaining > 0)
            {
                int take = Math.Min(pack, remaining);
                decimal chunkValue = g.Denom * take;
                int chunkNotes = take;

                // snapshot üzerinden genişlet (iterasyon sırasında dictionary'yi değiştirme)
                foreach (var kv in reachable.ToArray())
                {
                    decimal newAmount = kv.Key + chunkValue;
                    if (newAmount > amount) continue;

                    int newNotes = kv.Value + chunkNotes;
                    if (newNotes > _maxDispensibleItems) continue;

                    // Hedef tutara ulaştık!
                    if (newAmount == amount) return true;

                    // Bu tutara daha az banknot ile ulaşabiliyorsak güncelle
                    if (!reachable.TryGetValue(newAmount, out var bestNotes) || newNotes < bestNotes)
                        reachable[newAmount] = newNotes;
                }

                remaining -= take;
                pack <<= 1; // 1,2,4,8,16... şeklinde ilerle
            }
        }

        return false;
    }
}

// ============ KULLANIM ÖRNEĞİ ============

internal class Program
{
    static void Main(string[] args)
    {
        // Diagnostic dictionary'yi başlat
        Diagnostic.Diagnostics = new Dictionary<AtmDeviceType, BaseDevice>();

        var cdmDevice = new CdmDevice
        {
            DeviceClass = AtmDeviceType.CDM,
            Status = 0,
            Cassettes = new Dictionary<int, CdmCassette>
            {
                {1, GenerateData.GetCdmCassette("CDM001",1,"REJ",0,"USD",0,0) },
                {2, GenerateData.GetCdmCassette("CDM001",2,"REJ",0,"USD",0,0) },
                {3, GenerateData.GetCdmCassette("CDM001",3,"CDM",50,"USD",5,0) },
                {4, GenerateData.GetCdmCassette("CDM001",4,"CDM",20,"USD",6,0) },
                {5, GenerateData.GetCdmCassette("CDM001",5,"CDM",100,"USD",4,0) },
                {6, GenerateData.GetCdmCassette("CDM001",6,"CDM",10,"USD",7,0) },
                {7, GenerateData.GetCdmCassette("CDM001",7,"REC",50,"USD",3,0) },
                {8, GenerateData.GetCdmCassette("CDM001",8,"REC",200,"USD",7,0) },
            }
        };

        // ÖNEMLİ: Device'ı dictionary'ye ekle!
        Diagnostic.Diagnostics.Add(AtmDeviceType.CDM, cdmDevice);

        // Maksimum banknot adedi limiti ayarla (örnek: 40 banknot)
        Manager.SetMaxDispensibleItems(40);

        // Test senaryoları
        Console.WriteLine("=== SUBSET-SUM ALGORITHM (Varsayılan) ===");
        Manager.SetAlgorithm(DispenseAlgorithm.SubsetSum);
        TestDispense();

        Console.WriteLine("\n=== GREEDY ALGORITHM ===");
        Manager.SetAlgorithm(DispenseAlgorithm.Greedy);
        TestDispense();

        // Maksimum banknot adedi testi
        Console.WriteLine("\n=== MAKSİMUM BANKNOT ADEDİ TESTİ ===");
        TestMaxNoteLimit();
    }

    static void TestDispense()
    {
        // Test 1: Basit tutar
        Test(150, "USD", "150 USD"); // 100 + 2x20 + 10 = 4 banknot

        // Test 2: Büyük tutar
        Test(800, "USD", "800 USD"); // Toplam: 400+120+70 = 590, yetmez

        // Test 3: Tam eşleşme
        Test(100, "USD", "100 USD"); // 1x100 = 1 banknot

        // Test 4: Küçük tutar
        Test(30, "USD", "30 USD"); // 20 + 10 = 2 banknot

        // Test 5: Karmaşık kombinasyon
        Test(120, "USD", "120 USD"); // 100 + 20 = 2 banknot
    }

    static void TestMaxNoteLimit()
    {
        // Örnek: 500$ çekmek için 50x10$ = 50 banknot gerekir
        // Ama limit 40 banknot ise ödenemez olmalı
        
        Manager.SetMaxDispensibleItems(40);
        Console.WriteLine("Maksimum banknot limiti: 40");
        
        Manager.SetAlgorithm(DispenseAlgorithm.SubsetSum);
        
        // 500$ = En az 5 banknot gerekir (5x100)
        Test(500, "USD", "500$ (5 banknot gerekir)");
        
        // 100$ = 10x10 = 10 banknot (limit altında)
        Test(100, "USD", "100$ (10 banknot ile yapılabilir)");
        
        // Küçük limit testi
        Manager.SetMaxDispensibleItems(3);
        Console.WriteLine("\nMaksimum banknot limiti: 3");
        Test(150, "USD", "150$ (min 2 banknot: 100+50)");
        Test(30, "USD", "30$ (min 3 banknot: 10+10+10)");
    }

    static void Test(decimal amount, string currency, string description)
    {
        bool result = Manager.IsAmountDispensible(amount, currency);
        Console.WriteLine($"{description}: {(result ? "✓ ÖDENEBİLİR" : "✗ ÖDENEMEYEN")}");
    }
}

// ============ MODEL CLASSES ============

public enum AtmDeviceType
{
    CIM, CDM, IDC, PIN, CHK, PTRR, PTRJ, REC
}

public class BaseDevice
{
    public AtmDeviceType DeviceClass { get; set; }
    public int Status { get; set; }
}

public class CdmDevice : BaseDevice
{
    public Dictionary<int, CdmCassette> Cassettes { get; set; }
}

public class BaseCassette
{
    public string DeviceId { get; set; }
    public short CassetteId { get; set; }
    public string CassetteType { get; set; }
    public decimal BanknoteType { get; set; }
    public string CurrencyCode { get; set; }
    public int CurrentCount { get; set; }
    public int Status { get; set; }
}

public class CdmCassette : BaseCassette { }

public class Diagnostic
{
    public static Dictionary<AtmDeviceType, BaseDevice> Diagnostics;
}

public class GenerateData
{
    public static CdmCassette GetCdmCassette(
        string deviceId,
        short cassetteId,
        string cassetteType,
        decimal banknoteType,
        string currencyCode,
        int currentCount,
        int status)
    {
        return new CdmCassette
        {
            DeviceId = deviceId,
            CassetteId = cassetteId,
            CassetteType = cassetteType,
            BanknoteType = banknoteType,
            CurrencyCode = currencyCode,
            CurrentCount = currentCount,
            Status = status
        };
    }
}
```
------------------LAST GPT-----------------------------
```c#
<!doctype html>
<html>
<head>
  <meta http-equiv="X-UA-Compatible" content="IE=edge" />
  <meta charset="utf-8" />
  <title>Buttons Panel</title>
  <link rel="stylesheet" href="css/fastbuttons.css" />
  <script type="text/javascript" src="js/lottie.min.js"></script> <!-- varsa -->
  <script type="text/javascript" src="js/fastbuttons.js"></script>
</head>
<body onload="ButtonsUI.initialize()">
  <section id="bp-panel">
    <div id="bp-lottie" aria-hidden="true"></div>
    <div id="bp-buttons"></div>
  </section>
</body>
</html>


```

-----------------daasdsadE-----------------------------
```c#
html,body{margin:0;padding:0;background:#f5f7fa;font-family:"Segoe UI",Arial,sans-serif}

#bp-panel{
  position:relative;width:1024px;margin:24px auto;padding:28px;
  background:#fff;border-radius:18px;box-shadow:0 6px 22px rgba(0,0,0,.06);overflow:hidden;
}

#bp-lottie{position:absolute;top:0;right:0;bottom:0;left:0;pointer-events:none;z-index:0}

#bp-buttons{position:relative;z-index:1;text-align:center;font-size:0}
.bp-cell{display:inline-block;vertical-align:top;width:300px;margin:12px;font-size:16px}

.bp-btn{
  display:block;width:100%;height:90px;line-height:90px;text-align:center;
  border-radius:14px;border:1px solid #e3e8ee;background:#f9fafb;color:#1f2a37;font-size:28px;
  box-shadow:inset 0 1px 0 rgba(255,255,255,.65),0 8px 24px rgba(16,24,40,.06);
  cursor:pointer;outline:none;
}
.bp-btn:disabled{opacity:.35;cursor:not-allowed}
.bp-btn.is-pressed{
  background:#eaf2ff;border-color:#6fa6ff;color:#0f2a4a;
  box-shadow:inset 0 0 0 2px rgba(111,166,255,.45),0 10px 28px rgba(16,24,40,.10);
}

.bp-curr{font-weight:600;margin-right:8px}


```
------------------ttt 2-----------------------------
```c#

// ----- semboller & format (ES5) -----
var BP_SYMBOLS = { 'AED':'Đ', 'TRY':'₺', 'USD':'$', 'EUR':'€' };
function bpFormatAmount(n){ var s = Math.floor(Number(n)||0).toString(); return s.replace(/\B(?=(\d{3})+(?!\d))/g, ','); }

// ----- tek entry point -----
var ButtonsUI = {
  initialize: function () {
    // Lottie varsa kullan; yoksa dosyayı include etme
    lottie.loadAnimation({
      container: document.getElementById('bp-lottie'),
      renderer: 'svg',
      loop: true,
      autoplay: true,
      path: 'lotties/ring.json'
    });

    var host = document.getElementById('bp-buttons');
    host.innerHTML = '';

    // C# HtmlBridge: GetAmounts() -> { items:[ {Index,CurrencyCode,Amount,IsDispensible,AmountSource} ] }
    var data = window.external.GetAmounts();
    var items = data.items;

    for (var i = 0; i < items.length; i++) {
      var it = items[i];

      var cell = document.createElement('div');
      cell.className = 'bp-cell';

      var btn = document.createElement('button');
      btn.className = 'bp-btn';
      if (!it.IsDispensible) btn.disabled = true;

      var sym = BP_SYMBOLS[it.CurrencyCode] || it.CurrencyCode;
      btn.innerHTML = '<span class="bp-curr">'+ sym +'</span>' + bpFormatAmount(it.Amount);

      btn.onclick = (function(item, b){
        return function(){
          b.className += ' is-pressed';
          setTimeout(function(){ b.className = b.className.replace(' is-pressed',''); }, 140);

          // C# HtmlBridge: OnAmountClicked(index, currency, amount, isDispensible, amountSource)
          window.external.OnAmountClicked(item.Index, item.CurrencyCode, item.Amount, item.IsDispensible, item.AmountSource);
        };
      })(it, btn);

      cell.appendChild(btn);
      host.appendChild(cell);
    }
  }
};


```
------------------ttt 1-----------------------------
```css
[ComVisible(true)]
[ClassInterface(ClassInterfaceType.AutoDual)]
public class HtmlBridge
{
    public object GetAmounts() => new { items = /* List<...> map */ };
    public void OnAmountClicked(string index, string currency, object amount, bool isDispensible, string amountSource)
    {
        var amt = Convert.ToDecimal(amount); // IE'de decimal/double gelebilir
        // .. senin akışın ..
    }
}


```
-----------------------------------------------
```Html
<!DOCTYPE html>
<html>
<head>
    <meta http-equiv="X-UA-Compatible" content="IE=edge" />
    <meta charset="utf-8" />
    <title>Predefined Amounts with Lottie</title>
    <style>
        * {
            margin: 0;
            padding: 0;
            box-sizing: border-box;
        }

        body {
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            background: #f5f5f5;
            display: flex;
            align-items: center;
            justify-content: center;
            min-height: 100vh;
            padding: 20px;
        }

        .container {
            position: relative;
            width: 100%;
            max-width: 1200px;
        }

        .lottie-container {
            position: absolute;
            top: 0;
            left: 0;
            width: 100%;
            height: 100%;
            pointer-events: none;
            z-index: 1;
        }

        .buttons-wrapper {
            position: relative;
            z-index: 2;
        }

        .buttons-grid {
            display: grid;
            grid-template-columns: repeat(4, 1fr);
            gap: 20px;
        }

        .amount-button {
            background: white;
            border: 2px solid #e0e0e0;
            border-radius: 12px;
            padding: 30px 20px;
            cursor: pointer;
            transition: background 0.2s ease;
            box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
        }

        .amount-button:active {
            background: #f0f0f0;
        }

        .amount-button .content {
            display: flex;
            align-items: center;
            justify-content: center;
            gap: 8px;
        }

        .amount-button .text {
            font-size: 24px;
            font-weight: 600;
            color: #2d3748;
        }
    </style>
</head>
<body>
    <div class="container">
        <!-- Lottie animasyonu burada olacak -->
        <div class="lottie-container" id="lottieContainer"></div>
        
        <!-- Butonlar -->
        <div class="buttons-wrapper">
            <div id="buttonsContainer" class="buttons-grid"></div>
        </div>
    </div>

    <!-- Lottie kütüphanesi -->
    <script src="https://cdnjs.cloudflare.com/ajax/libs/lottie-web/5.12.2/lottie.min.js"></script>

    <script>
        (function() {
            'use strict';

            // Lottie animasyonunu başlat
            function initializeLottie() {
                try {
                    var animation = lottie.loadAnimation({
                        container: document.getElementById('lottieContainer'),
                        renderer: 'svg',
                        loop: true,
                        autoplay: true,
                        path: 'animation.json' // Lottie JSON dosyanızın yolu
                    });
                } catch (e) {
                    // Lottie yüklenemezse sessiz kal
                }
            }

            function initialize() {
                try {
                    var jsonString = window.external.GetAmounts();
                    var amounts = parseJson(jsonString);
                    renderButtons(amounts);
                    initializeLottie();
                } catch (e) {
                    // Hata durumunda sessiz kal
                }
            }

            function parseJson(jsonString) {
                return JSON.parse(jsonString);
            }

            function renderButtons(amounts) {
                var container = document.getElementById('buttonsContainer');
                container.innerHTML = '';
                
                var displayAmounts = amounts.slice(0, 4);
                
                for (var i = 0; i < displayAmounts.length; i++) {
                    var button = createButton(displayAmounts[i]);
                    container.appendChild(button);
                }
            }

            function createButton(amount) {
                var button = document.createElement('button');
                button.className = 'amount-button';
                
                var content = document.createElement('div');
                content.className = 'content';
                
                var text = document.createElement('div');
                text.className = 'text';
                text.textContent = amount.CurrencyCode + ' ' + formatAmount(amount.Amount);
                
                content.appendChild(text);
                button.appendChild(content);
                
                button.onclick = function() {
                    handleClick(amount);
                };
                
                return button;
            }

            function formatAmount(amount) {
                return Math.floor(amount).toLocaleString('tr-TR');
            }

            function handleClick(amount) {
                try {
                    window.external.OnAmountSelected(
                        amount.Index,
                        amount.Amount,
                        amount.CurrencyCode,
                        amount.IsDispensible,
                        amount.AmountSource
                    );
                } catch (e) {
                    // Hata durumunda sessiz kal
                }
            }

            if (document.readyState === 'loading') {
                document.addEventListener('DOMContentLoaded', initialize);
            } else {
                initialize();
            }

            window.refreshAmounts = function() {
                initialize();
            };

        })();
    </script>
</body>
</html>




```
