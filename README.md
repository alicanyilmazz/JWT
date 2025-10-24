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

```c#

using System;
using System.Collections.Generic;
using System.Linq;

#region Models (senin mevcut modellerinle birebir uyumlu)

public enum AmountSource { Predefined, Api, Calculated }

public abstract class BaseAmount
{
    public string  Index         { get; set; }   // "pre_1".."pre_4" | "api_1".."api_6" | "calc_n"
    public string  CurrencyCode  { get; set; }
    public decimal Amount        { get; set; }
    public bool    IsDispensible { get; set; }

    protected BaseAmount(string index, decimal amount, string currency)
    { Index = index; Amount = amount; CurrencyCode = currency; }
}

public sealed class ApiAmount : BaseAmount
{
    public ApiAmount(string index, decimal amount, string currency)
        : base(index, amount, currency) { }
}

public sealed class PredefinedAmount : BaseAmount
{
    public AmountSource AmountSource { get; set; } = AmountSource.Predefined;
    public string ReplacedIndex { get; set; } = string.Empty; // "" | "api_i" | "calc_n"
    public PredefinedAmount(string index, decimal amount, string currency)
        : base(index, amount, currency) { }
}

#endregion

#region Services / Strategies

public interface IDispenseService
{
    bool IsDispensible(string currencyCode, decimal amount);
}

public sealed class ManagerDispenseService : IDispenseService
{
    public bool IsDispensible(string currencyCode, decimal amount)
        => Manager.IsAmountDispensible(amount, currencyCode);
}

public interface IBaselineProvider
{
    decimal GetBaseline(string preIndex); // "pre_1" → 100, "pre_2" → 200 ...
}

// Plan başında snapshot alır: baseline = o anki Predefined.Amount (100/200/500/1000)
public sealed class SnapshotBaselineProvider : IBaselineProvider
{
    private readonly IReadOnlyDictionary<string, decimal> _baseline;
    public SnapshotBaselineProvider(IEnumerable<PredefinedAmount> predefined)
    {
        _baseline = predefined.ToDictionary(p => p.Index, p => p.Amount, StringComparer.Ordinal);
    }
    public decimal GetBaseline(string preIndex) => _baseline[preIndex];
}

public static class FlagExtensions
{
    public static void FlagDispensibility<T>(this IEnumerable<T> items, IDispenseService svc) where T : BaseAmount
    {
        foreach (var x in items ?? Enumerable.Empty<T>())
            x.IsDispensible = svc.IsDispensible(x.CurrencyCode, x.Amount);
    }
}

public interface IApiReplacementStrategy
{
    void Apply(List<PredefinedAmount> slots, IEnumerable<ApiAmount> api, IBaselineProvider baseline);
}

public sealed class NearestEnableReplacementStrategy : IApiReplacementStrategy
{
    public void Apply(List<PredefinedAmount> slots, IEnumerable<ApiAmount> api, IBaselineProvider baseline)
    {
        if (slots == null || slots.Count != 4) return;

        // enable & değişmemiş slot indeksleri
        var candidateIdx = Enumerable.Range(0, slots.Count)
            .Where(i => slots[i].IsDispensible && string.IsNullOrEmpty(slots[i].ReplacedIndex))
            .ToList();

        foreach (var a in (api ?? Enumerable.Empty<ApiAmount>()).Where(x => x.IsDispensible))
        {
            if (candidateIdx.Count == 0) break;

            var bestIdx = candidateIdx
                .Select(i => new { i, b = baseline.GetBaseline(slots[i].Index) })
                .OrderBy(x => Math.Abs(x.b - a.Amount))
                .ThenBy(x => x.b)
                .ThenBy(x => x.i)
                .Select(x => (int?)x.i)
                .FirstOrDefault();

            if (bestIdx is null) continue;

            var s = slots[bestIdx.Value];
            s.Amount        = a.Amount;
            s.AmountSource  = AmountSource.Api;
            s.ReplacedIndex = a.Index;
            s.IsDispensible = true; // a zaten flag'li

            candidateIdx.Remove(bestIdx.Value);
        }
    }
}

public interface IDisabledFillStrategy
{
    void Fill(List<PredefinedAmount> slots, IBaselineProvider baseline, IDispenseService svc, string calcPrefix = "calc_");
}

public sealed class SmallestUnitMultiplesFillStrategy : IDisabledFillStrategy
{
    private static readonly decimal[] Units = { 100m, 200m, 500m, 1000m };

    public void Fill(List<PredefinedAmount> slots, IBaselineProvider baseline, IDispenseService svc, string calcPrefix = "calc_")
    {
        if (slots == null || slots.Count != 4) return;

        var currency = slots.First().CurrencyCode;
        var baseUnit = Units.FirstOrDefault(u => svc.IsDispensible(currency, u));
        if (baseUnit == 0m) return;

        var used = new HashSet<decimal>(slots.Select(s => s.Amount));

        int calcNo = 1;
        foreach (var slot in slots
            .Where(s => !s.IsDispensible && string.IsNullOrEmpty(s.ReplacedIndex))
            .OrderBy(s => baseline.GetBaseline(s.Index)))
        {
            var cand = Enumerable.Range(1, 300)        // 1U, 2U, 3U, ...
                .Select(k => baseUnit * k)
                .FirstOrDefault(v => !used.Contains(v) && svc.IsDispensible(currency, v));

            if (cand == 0m) continue;

            slot.Amount        = cand;
            slot.AmountSource  = AmountSource.Calculated;
            slot.ReplacedIndex = $"{calcPrefix}{calcNo++}";
            slot.IsDispensible = true;

            used.Add(cand);
        }
    }
}

#endregion

#region Orchestrator

public sealed class ButtonPlanner
{
    private readonly IDispenseService _dispense;
    private readonly IApiReplacementStrategy _replaceStrategy;
    private readonly IDisabledFillStrategy _fillStrategy;

    public ButtonPlanner(
        IDispenseService dispense,
        IApiReplacementStrategy replaceStrategy,
        IDisabledFillStrategy fillStrategy)
    {
        _dispense = dispense;
        _replaceStrategy = replaceStrategy;
        _fillStrategy = fillStrategy;
    }

    /// <summary>
    /// 1) Flag → 2) API replace → 3) Disabled fill (multiples) → 4) Slots geri döner
    /// </summary>
    public List<PredefinedAmount> Plan(List<PredefinedAmount> predefined4, List<ApiAmount> apiInOrder, bool runFlagStep = true)
    {
        if (predefined4 == null || predefined4.Count != 4) return predefined4 ?? new List<PredefinedAmount>();

        // baseline snapshot
        var baseline = new SnapshotBaselineProvider(predefined4);

        if (runFlagStep)
        {
            predefined4.FlagDispensibility(_dispense);
            (apiInOrder ?? Enumerable.Empty<ApiAmount>()).FlagDispensibility(_dispense);
        }

        _replaceStrategy.Apply(predefined4, apiInOrder, baseline);
        _fillStrategy.Fill(predefined4, baseline, _dispense, "calc_");

        return predefined4;
    }

    // UI için küçükten büyüğe sıralı görünüm istersen:
    public sealed class ButtonView
    {
        public decimal Amount { get; init; }
        public string CurrencyCode { get; init; }
        public bool IsDispensible { get; init; }
        public AmountSource Source { get; init; }
        public string PreIndex { get; init; }
        public string ReplacedIndex { get; init; }
        public decimal Baseline { get; init; }
    }

    public List<ButtonView> BuildUiButtonsSorted(List<PredefinedAmount> slots, IBaselineProvider baseline)
        => slots.Select(p => new ButtonView {
                Amount        = p.Amount,
                CurrencyCode  = p.CurrencyCode,
                IsDispensible = p.IsDispensible,
                Source        = p.AmountSource,
                PreIndex      = p.Index,
                ReplacedIndex = p.ReplacedIndex,
                Baseline      = baseline.GetBaseline(p.Index)
            })
            .OrderBy(x => x.Amount)
            .ThenBy(x => x.Baseline)
            .ThenBy(x => x.PreIndex, StringComparer.Ordinal)
            .ToList();
}

#endregion

#region Usage (örnek)

// kurulum
// var predefined = new List<PredefinedAmount> {
//     new PredefinedAmount("pre_1", 100,  "AED"),
//     new PredefinedAmount("pre_2", 200,  "AED"),
//     new PredefinedAmount("pre_3", 500,  "AED"),
//     new PredefinedAmount("pre_4", 1000, "AED")
// };
// var api = new List<ApiAmount> {
//     new ApiAmount("api_1", 700, "AED"),
//     new ApiAmount("api_2", 300, "AED")
// };
// var planner = new ButtonPlanner(
//     dispense:        new ManagerDispenseService(),
//     replaceStrategy: new NearestEnableReplacementStrategy(),
//     fillStrategy:    new SmallestUnitMultiplesFillStrategy()
// );
// var planned = planner.Plan(predefined, api, runFlagStep: true);
// var ui = planner.BuildUiButtonsSorted(planned, new SnapshotBaselineProvider(predefined));

#endregion


```
------------------------------------------------------------
```c#
public static class AtmButtonPlanner
{
    // 1) Flag (kısa)
    public static void FlagDispensibility<T>(IEnumerable<T> items) where T : BaseAmount
    {
        foreach (var x in items ?? Enumerable.Empty<T>())
            x.IsDispensible = Manager.IsAmountDispensible(x.Amount, x.CurrencyCode);
    }

    // 2) API → en yakın ENABLE predefined’a yerleştir (LINQ ile sade)
    public static void ApplyApiReplacementsInGivenOrder(
        List<PredefinedAmount> slots,
        IEnumerable<ApiAmount> apiInOrder,
        IReadOnlyDictionary<string, decimal> baselineByIndex)
    {
        if (slots == null || slots.Count != 4) return;

        // enable & değişmemiş slot indeksleri
        var candidateIdx = Enumerable.Range(0, slots.Count)
            .Where(i => slots[i].IsDispensible && string.IsNullOrEmpty(slots[i].ReplacedIndex))
            .ToList();

        foreach (var api in (apiInOrder ?? Enumerable.Empty<ApiAmount>()).Where(a => a.IsDispensible))
        {
            if (candidateIdx.Count == 0) break;

            var bestIdx = candidateIdx
                .Select(i => new { i, baseline = baselineByIndex[slots[i].Index] })
                .OrderBy(x => Math.Abs(x.baseline - api.Amount))
                .ThenBy(x => x.baseline)
                .ThenBy(x => x.i)
                .Select(x => (int?)x.i)
                .FirstOrDefault();

            if (bestIdx is null) continue;

            var s = slots[bestIdx.Value];
            s.Amount        = api.Amount;
            s.AmountSource  = AmountSource.Api;
            s.ReplacedIndex = api.Index;
            s.IsDispensible = true; // api zaten dispensible

            candidateIdx.Remove(bestIdx.Value); // bu slot bir daha seçilmesin
        }
    }

    // 3) Kalan disabled slotları “en küçük dispensible unit” katlarıyla doldur (LINQ ile sade)
    public static void FillDisabledSlotsWithMultiples(
        List<PredefinedAmount> slots,
        IReadOnlyDictionary<string, decimal> baselineByIndex,
        string calcPrefix = "calc_")
    {
        if (slots == null || slots.Count != 4) return;

        var units = new[] { 100m, 200m, 500m, 1000m };
        var cur   = slots[0].CurrencyCode;

        var baseUnit = units.FirstOrDefault(u => Manager.IsAmountDispensible(u, cur));
        if (baseUnit == 0m) return;

        var used = new HashSet<decimal>(slots.Select(s => s.Amount));

        int calcNo = 1;
        foreach (var slot in slots
            .Where(s => !s.IsDispensible && string.IsNullOrEmpty(s.ReplacedIndex))
            .OrderBy(s => baselineByIndex[s.Index]))
        {
            var cand = Enumerable.Range(1, 300)               // 1U, 2U, 3U, ...
                .Select(k => baseUnit * k)
                .FirstOrDefault(v => !used.Contains(v) && Manager.IsAmountDispensible(v, cur));

            if (cand == 0m) continue;                         // uygun aday yoksa geç

            slot.Amount        = cand;
            slot.AmountSource  = AmountSource.Calculated;
            slot.ReplacedIndex = $"{calcPrefix}{calcNo++}";
            slot.IsDispensible = true;

            used.Add(cand);
        }
    }

    // 4) Orkestrasyon (değişmedi)
    public static List<PredefinedAmount> Plan(
        List<PredefinedAmount> predefined4,
        List<ApiAmount> apiInOrder,
        bool runFlagStep = true)
    {
        var baseline = predefined4.ToDictionary(p => p.Index, p => p.Amount);

        if (runFlagStep)
        {
            FlagDispensibility(predefined4);
            FlagDispensibility(apiInOrder);
        }

        ApplyApiReplacementsInGivenOrder(predefined4, apiInOrder, baseline);
        FillDisabledSlotsWithMultiples(predefined4, baseline, "calc_");

        return predefined4;
    }
}


```

