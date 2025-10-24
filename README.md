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

 public class MultistagedTransactionImageSaveService : IImageServerSaveService
 {
        private const int ThumbnailWidth = 300;
        private const int FullScreenWidth = 1000;

        private readonly IUnitOfWork _unitOfWork;
        private readonly IRepository<ImageFileInformation> _repository;
        public MultistagedTransactionImageSaveService(IUnitOfWork unitOfWork, IRepository<ImageFileInformation> repository)
        {
            _unitOfWork = unitOfWork;
            _repository = repository;
        }
        public async Task<Response<NoDataDto>> SaveAsync(IEnumerable<ImageDbServiceRequest> images, string directory)
        {
            var imageStorage = new ConcurrentDictionary<string, ImageFile>();
            var imageDetailStorage = new ConcurrentDictionary<string, ImageFileDetail>();
            var totalImages = await _repository.CountAsync();
            var tasks = images.Select(image => Task.Run(async () =>
            {
                try
                {
                    using var imageResult = await Image.LoadAsync(image.Content);

                    var id = Guid.NewGuid();
                    var path = $"/images/{totalImages % 1000}/";
                    var name = $"{id}.jpg";

                    var storagePath = Path.Combine(directory, $"wwwroot{path}".Replace("/", "\\"));

                    if (!Directory.Exists(storagePath))
                    {
                        Directory.CreateDirectory(storagePath);
                    }
                    List<Task<SaveImageAsycnResult>> task = new List<Task<SaveImageAsycnResult>>
                    {
                        SaveImageAsync(imageResult, $"Original_{name}", storagePath, imageResult.Width),
                        SaveImageAsync(imageResult, $"FullScreen_{name}", storagePath, FullScreenWidth),
                        SaveImageAsync(imageResult, $"Thumbnail_{name}", storagePath, ThumbnailWidth)
                    };

                    await Task.WhenAll(task);
                    foreach (var item in task)
                    {
                        if (item.Result.isSuccess)
                        {
                            imageDetailStorage.TryAdd(item.Result.Type, new ImageFileDetail()
                            {
                                ImageId = id,
                                Type = item.Result.Type,
                            });
                        }
                    }

                    imageStorage.TryAdd(image.Name, new ImageFile()
                    {
                        ImageId = id,
                        Folder = path,
                        Extension = "jpg"
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
                var file = new ImageFileInformation();
                foreach (var image in imageStorage)
                {
                    file.ImageId = image.Value.ImageId;
                    file.Folder= image.Value.Folder;
                    file.Extension = image.Value.Extension;
                    //await _repository.SaveImagesToImageFile(image.Value);
                }
                foreach (var image in imageDetailStorage)
                {   
                    file.Type.Add(image.Value.Type);
                    //await _repository.SaveImagesToImageFileDetail(image.Value);
                }
                await _repository.SaveImagesServerData(file);
                //await _repository.SaveImagesToImageFile(image.Value);
                //await _repository.CommitAsync();
            }
            catch (Exception e)
            {
                return Response<NoDataDto>.Fail(e.Message, 404, true);
            }
            return Response<NoDataDto>.Success(200);
        }

        private async Task<SaveImageAsycnResult> SaveImageAsync(Image image, string name, string path, int resizeWidth)
        {
            try
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
            catch (Exception)
            {
                //Log
                return new SaveImageAsycnResult() { isSuccess = false, Type = name };
            }
            return new SaveImageAsycnResult() { isSuccess = true, Type = name };
        }

        class SaveImageAsycnResult
        {
            public bool isSuccess { get; set; }
            public string Type { get; set; }
        }
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

0 API

(0,0) — Mümkün

Kaset: [50×0, 100×0, 200×0, 500×0, 1000×0]

Default: { } (0)

API: [] veya [75,125] → { } (0)

(0,1) — Mümkün

Kaset: [50×0, 100×1, 200×0, 500×0, 1000×0] (100)

Default: {100} (1)

API: []

(0,2) — Mümkün

Kaset: [50×0, 100×0, 200×1, 500×1, 1000×0] (200, 500; 1000 için 200+500=700<1000)

Default: {200,500} (2)

API: []

(0,3) — Mümkün

Kaset: [50×0, 100×1, 200×1, 500×1, 1000×0] (100,200,500; 1000 için toplam 800<1000)

Default: {100,200,500} (3)

API: []

(0,4) — Mümkün

Kaset: [50×0, 100×2, 200×2, 500×1, 1000×0] (1000 için 200+400+500=1100≥1000)

Default: {100,200,500,1000} (4)

API: []

1 API

(1,0) — Mümkün

Kaset: [50×1, 100×0, 200×0, 500×0, 1000×0] (toplam 50; hiçbir default tutarı karşılanmaz)

Default: { } (0)

API: [50(r1)] → {50} (1)

(1,1) — Mümkün

Kaset: [50×1, 100×0, 200×0, 500×0, 1000×1] (default: sadece 1000)

Default: {1000} (1)

API: [50(r1)] → {50} (1)

(1,2) — Mümkün

Kaset: [50×1, 100×0, 200×1, 500×1, 1000×0] (default: 200,500)

Default: {200,500} (2)

API: [50(r1)] → {50} (1)

(1,3) — Mümkün

Kaset: [50×1, 100×1, 200×1, 500×1, 1000×0] (default: 100,200,500; 1000 için 50+100+200+500=850<1000)

Default: {100,200,500} (3)

API: [50(r1)] → {50} (1)

(1,4) — Mümkün

Kaset: [50×1, 100×2, 200×2, 500×1, 1000×0] (tüm default’lar)

Default: {100,200,500,1000} (4)

API: [50(r1)] → {50} (1)

2 API

(2,0) — İmkânsız

Gerekçe: Default=0 demek özellikle 100 için 50/100 toplamının <100 olması gerekir. Bu koşul altında aynı kasetlerle iki farklı API tutarını karşılamak mümkün değil.

(2,1) — Mümkün

Kaset: [50×1, 100×0, 200×0, 500×0, 1000×2] (default: sadece 1000)

Default: {1000} (1)

API: [50(r1), 1000(r2)] → {50,1000} (2)

(2,2) — Mümkün

Kaset: [50×1, 100×0, 200×1, 500×1, 1000×0]

Default: {200,500} (2)

API: [50(r1), 500(r2)] → {50,500} (2)

(2,3) — Mümkün

Kaset: [50×3, 100×1, 200×1, 500×1, 1000×0] (50 toplam=150; 1000 için 150+100+200+500=950<1000)

Default: {100,200,500} (3)

API: [50(r1), 150(r2)] → {50,150} (2)

(2,4) — Mümkün

Kaset: [50×1, 100×2, 200×2, 500×1, 1000×0]

Default: {100,200,500,1000} (4)

API: [600(r1), 1000(r2)] → {600,1000} (2)

600 için 50+100+200 toplamı = 50 + 200 + 400 = 650 ≥ 600

3 API

(3,0) — İmkânsız

Gerekçe: Default=0 kısıtı varken (özellikle 100 için 50/100 toplamı <100), üç farklı API tutarı sağlamak mümkün değil.

(3,1) — Mümkün

Kaset: [50×0, 100×0, 200×0, 500×0, 1000×3] (default: sadece 1000)

Default: {1000} (1)

API: [1000(r1), 2000(r2), 3000(r3)] → {1000,2000,3000} (3)

(3,2) — Mümkün

Kaset: [50×1, 100×0, 200×1, 500×1, 1000×0] (default: 200,500)

Default: {200,500} (2)

API: [50(r1), 200(r2), 500(r3)] → {50,200,500} (3)

(3,3) — Mümkün

Kaset: [50×3, 100×1, 200×1, 500×1, 1000×0] (1000 için toplam 950<1000)

Default: {100,200,500} (3)

API: [50(r1), 200(r2), 500(r3)] → {50,200,500} (3)

(3,4) — Mümkün

Kaset: [50×1, 100×2, 200×2, 500×1, 1000×1]

Default: {100,200,500,1000} (4)

API: [50(r1), 1000(r2), 2000(r3)] → {50,1000,2000} (3)

≥4 API

(≥4,0) — İmkânsız

Gerekçe: Default=0 için düşük toplam değer kısıtı altında 4+ farklı API tutarını sağlamak mümkün değil (en küçük default olan 100’ü dahi karşılamamak gerekir).

(≥4,1) — Mümkün

Kaset: [50×0, 100×0, 200×0, 500×0, 1000×5] (default: sadece 1000)

Default: {1000} (1)

API: [1000,2000,3000,4000,5000] → 5 adet (≥4)

(≥4,2) — Mümkün

Kaset: [50×0, 100×1, 200×0, 500×0, 1000×4] (default: 100 ve 1000)

Default: {100,1000} (2)

API: [1000,2000,3000,4000,100] → 5 adet (≥4)

(≥4,3) — Mümkün

Kaset: [50×1, 100×1, 200×1, 500×1, 1000×0] (1000 için toplam 850<1000)

Default: {100,200,500} (3)

API: [50,100,150,200,250] → 5 adet (≥4)

150 ve 250 için 50+100 toplamı yeterli olmak zorunda; örnekte 50×1 (50), 100×1 (100) ⇒ 150’ye yetiyor; 250 için 50+100=150 yetmez; istersen 50×2 yap: 50×2, 100×1, 200×1, 500×1 ⇒ 1000 toplamı yine <1000 (2×50 + 100 + 200 + 500 = 900)

(≥4,4) — Mümkün

Kaset: [50×2, 100×3, 200×2, 500×2, 1000×2] (rahat kapasite)

Default: {100,200,500,1000} (4)

API: [50,100,150,200,600,700,1000,2000] → 8 adet (≥4)


----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
0 API

(0,0) – Mümkün (sadece ATM boşken)
Kaset: [0,0,0,0]
Default: { }
API: []

(0,1) – Mümkün
Kaset: [100×1, 0, 0, 0]
Default: {100}
API: []

(0,2) – Mümkün
Kaset: [100×1, 200×1, 0, 0]
Default: {100,200}
API: []

(0,3) – Mümkün
Kaset: [100×1, 200×1, 500×1, 0] (toplam 800 < 1000)
Default: {100,200,500}
API: []

(0,4) – Mümkün
Kaset: [100×1, 200×1, 500×1, 1000×1]
Default: {100,200,500,1000}
API: []

1 API

(1,0) – İmkânsız (Default=0 ancak ATM boş; API de 0 olur)

(1,1) – Mümkün
Kaset: [0,0,0,1000×1]
Default: {1000}
API (ör.): [1000(r1)] → {1000} (1)

(1,2) – Mümkün
Kaset: [100×1, 200×1, 0, 0]
Default: {100,200}
API (ör.): [200(r1)] → {200} (1)

(1,3) – Mümkün
Kaset: [100×1, 200×1, 500×1, 0]
Default: {100,200,500}
API (ör.): [500(r1)] → {500} (1)

(1,4) – Mümkün
Kaset: [100×1, 200×1, 500×1, 1000×1]
Default: {100,200,500,1000}
API (ör.): [1000(r1)] → {1000} (1)

2 API

(2,0) – İmkânsız (gerekçe: Default=0 ⇒ ATM boş)

(2,1) – Mümkün
Kaset: [0,0,0,1000×3]
Default: {1000}
API (ör.): [1000(r1), 2000(r2)] → {1000,2000} (2)

(2,2) – Mümkün
Kaset: [0,0,500×2,0]
Default: {500,1000}
API (ör.): [500(r1), 1000(r2)] → {500,1000} (2)

(2,3) – Mümkün
Kaset: [100×1, 200×1, 500×1, 0]
Default: {100,200,500}
API (ör.): [100(r1), 500(r2)] → {100,500} (2)

(2,4) – Mümkün
Kaset: [100×2, 200×2, 500×1, 1000×1]
Default: {100,200,500,1000}
API (ör.): [600(r1), 1000(r2)] → {600,1000} (2)
(600 için 100-değer 200 + 200-değer 400 = 600)

3 API

(3,0) – İmkânsız

(3,1) – Mümkün
Kaset: [0,0,0,1000×3]
Default: {1000}
API (ör.): [1000(r1), 2000(r2), 3000(r3)] → 3

(3,2) – Mümkün
Kaset: [0,0,500×3,0]
Default: {500,1000}
API (ör.): [500(r1), 1000(r2), 1500(r3)] → 3

(3,3) – Mümkün
Kaset: [100×2, 200×1, 500×1, 0] (toplam 900 < 1000)
Default: {100,200,500}
API (ör.): [100(r1), 200(r2), 500(r3)] → 3

(3,4) – Mümkün
Kaset: [100×3, 200×2, 500×1, 1000×1]
Default: {100,200,500,1000}
API (ör.): [100(r1), 500(r2), 1000(r3)] → 3

≥4 API

(≥4,0) – İmkânsız

(≥4,1) – Mümkün
Kaset (seçenek A): [0,0,0,1000×4] → Default {1000}
API (ör.): [1000,2000,3000,4000] → 4
Kaset (seçenek B): [0,200×4,0,0] → Default {200}
API (ör.): [200,400,600,800] → 4 (1000 hâlâ false; 200×4=800)

(≥4,2) – Mümkün
Kaset: [0,0,500×4,0]
Default: {500,1000}
API (ör.): [500,1000,1500,2000] → 4

(≥4,3) – Mümkün
Kaset: [100×2, 200×1, 500×1, 0] (toplam 900 < 1000)
Default: {100,200,500}
API (ör.): [100,200,400,500] → 4
(açıklama: 400 için 100-değer 200 + 200-değer 200 = 400)

(≥4,4) – Mümkün
Kaset: [100×5, 200×3, 500×2, 1000×2]
Default: {100,200,500,1000}
API (ör.): [100,300,600,1000,2000] → ≥4



------------------------------------------------------------
```c#
using System;
using System.Collections.Generic;
using System.Linq;

#region Models

public enum AmountSource { Predefined, Api, Calculated }

public abstract class BaseAmount
{
    public string  Index         { get; set; }   // "pre_1".."pre_4" | "api_1".."api_6" | "calc_n" (sadece iz)
    public string  CurrencyCode  { get; set; }
    public decimal Amount        { get; set; }
    public bool    IsDispensible { get; set; }   // Manager.IsAmountDispensible ile set edilir

    protected BaseAmount(string index, decimal amount, string currency)
    {
        Index = index;
        Amount = amount;
        CurrencyCode = currency;
    }
}

public sealed class ApiAmount : BaseAmount
{
    // Öncelik: verilen sıraya göre işlenecek (rank yok).
    public ApiAmount(string index, decimal amount, string currency)
        : base(index, amount, currency) { }
}

public sealed class PredefinedAmount : BaseAmount
{
    public AmountSource AmountSource { get; set; } = AmountSource.Predefined;

    /// <summary>
    /// Slot değiştiyse:
    ///  - API ile: ilgili API Index ("api_1" vb.)
    ///  - Calculated ile: "calc_n"
    /// Boş ise replace edilmemiştir.
    /// </summary>
    public string ReplacedIndex { get; set; } = string.Empty;

    public PredefinedAmount(string index, decimal amount, string currency)
        : base(index, amount, currency) { }
}

#endregion

public static class AtmButtonPlanner
{
    // 1) Flag (SRP): IsDispensible = Manager.IsAmountDispensible(...)
    public static void FlagDispensibility<T>(IEnumerable<T> items) where T : BaseAmount
    {
        if (items == null) return;
        foreach (var x in items)
            x.IsDispensible = Manager.IsAmountDispensible(x.Amount, x.CurrencyCode);
    }

    // 2) API → en yakın ENABLE predefined’a yerleştir (API listesi verilen sırayla işlenir)
    public static void ApplyApiReplacementsInGivenOrder(
        List<PredefinedAmount> slots,
        IEnumerable<ApiAmount> apiInOrder,
        IReadOnlyDictionary<string, decimal> baselineByIndex // "pre_1"->100, "pre_2"->200, ...
    )
    {
        if (slots == null || slots.Count != 4) return;

        // Replace edilebilir (enable + daha önce değişmemiş) predefined slot indeksleri
        var candidateIdx = Enumerable.Range(0, slots.Count)
                                     .Where(i => slots[i].IsDispensible && string.IsNullOrEmpty(slots[i].ReplacedIndex))
                                     .ToList();
        if (candidateIdx.Count == 0) return;

        var used = new HashSet<int>();

        foreach (var api in apiInOrder ?? Enumerable.Empty<ApiAmount>())
        {
            if (!api.IsDispensible) continue;

            int? best = null;
            decimal bestDiff = decimal.MaxValue;
            decimal bestBaseline = decimal.MaxValue;

            foreach (var i in candidateIdx)
            {
                if (used.Contains(i)) continue;

                var baseline = baselineByIndex[slots[i].Index]; // pre_i -> 100/200/500/1000
                var diff = Math.Abs(baseline - api.Amount);

                if (diff < bestDiff
                    || (diff == bestDiff && baseline < bestBaseline)
                    || (diff == bestDiff && baseline == bestBaseline && (best == null || i < best)))
                {
                    best = i; bestDiff = diff; bestBaseline = baseline;
                }
            }

            if (best == null) continue;

            var slot = slots[best.Value];
            slot.Amount        = api.Amount;
            slot.AmountSource  = AmountSource.Api;
            slot.ReplacedIndex = api.Index;     // "api_1" vb.
            slot.IsDispensible = true;          // API zaten dispensible idi

            used.Add(best.Value);
        }
    }

    // 3) Kalan disabled slotları “en küçük dispensible unit”in katlarıyla doldur (calc_1, calc_2, …)
    public static void FillDisabledSlotsWithMultiples(
        List<PredefinedAmount> slots,
        IReadOnlyDictionary<string, decimal> baselineByIndex, // sadece deterministik sıra için
        string calcPrefix = "calc_")
    {
        if (slots == null || slots.Count != 4) return;

        var units = new[] { 100m, 200m, 500m, 1000m };
        var cur   = slots[0].CurrencyCode;

        // En küçük dispensible unit'i bul
        var baseUnit = units.FirstOrDefault(u => Manager.IsAmountDispensible(u, cur));
        if (baseUnit == 0m) return;

        // Çakışmayı engelle: mevcut Amount’lar
        var usedAmounts = new HashSet<decimal>(slots.Select(s => s.Amount));

        // Doldurulacak: disabled & değişmemiş; sabit sıra için baseline’a göre sırala
        var toFill = slots.Where(s => !s.IsDispensible && string.IsNullOrEmpty(s.ReplacedIndex))
                          .OrderBy(s => baselineByIndex[s.Index]) // "pre_1"→100, "pre_2"→200, ...
                          .ToList();

        int calcCounter = 1;

        foreach (var slot in toFill)
        {
            decimal cand = baseUnit;

            for (int k = 0; k < 300; k++, cand += baseUnit)
            {
                if (usedAmounts.Contains(cand)) continue;
                if (!Manager.IsAmountDispensible(cand, cur)) continue;

                slot.Amount        = cand;
                slot.AmountSource  = AmountSource.Calculated;
                slot.ReplacedIndex = $"{calcPrefix}{calcCounter++}"; // "calc_1", "calc_2", ...
                slot.IsDispensible = true;

                usedAmounts.Add(cand);
                break;
            }
        }
    }

    // 4) Orkestrasyon (flag → API replace → calculated fill)
    public static List<PredefinedAmount> Plan(
        List<PredefinedAmount> predefined4,    // Index: "pre_1".."pre_4", Amount: 100/200/500/1000
        List<ApiAmount> apiInOrder,            // Index: "api_1".."api_6" (öncelik sırası)
        bool runFlagStep = true)
    {
        // Baseline'ları başta fotoğraflıyoruz; sonra Amount değişecek çünkü.
        var baseline = predefined4.ToDictionary(p => p.Index, p => p.Amount);

        if (runFlagStep)
        {
            FlagDispensibility(predefined4);
            FlagDispensibility(apiInOrder);
        }

        ApplyApiReplacementsInGivenOrder(predefined4, apiInOrder, baseline);
        FillDisabledSlotsWithMultiples(predefined4, baseline, "calc_");

        return predefined4; // nihai 4 buton (gelen sıra korunur: pre_1..pre_4)
    }
}


```

