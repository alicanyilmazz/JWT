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

