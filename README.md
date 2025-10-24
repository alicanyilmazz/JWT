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

Kaset: [50×0, 100×1, 200×0, 500×0, 1000×0]

Default: {100}

API: []

(0,2) — Mümkün

Kaset: [50×0, 100×0, 200×1, 500×1, 1000×0]

Default: {200,500}

API: []

(0,3) — Mümkün

Kaset: [50×0, 100×1, 200×1, 500×1, 1000×0]

Default: {100,200,500}

API: []

(0,4) — Mümkün

Kaset: [50×0, 100×2, 200×2, 500×1, 1000×0]

Default: {100,200,500,1000} (1000 için 200+200+500=900; +100=1000 veya toplam ≥1000 sağlanır)

API: []

1 API

(1,0) — Mümkün

Kaset: [50×1, 100×0, 200×0, 500×0, 1000×0]

Default: { }

API: [50] → {50}

(1,1) — Mümkün

Kaset: [50×1, 100×0, 200×0, 500×0, 1000×1]

Default: {1000}

API: [50] → {50}

(1,2) — Mümkün

Kaset: [50×1, 100×0, 200×1, 500×1, 1000×0]

Default: {200,500}

API: [50] → {50}

(1,3) — Mümkün

Kaset: [50×1, 100×1, 200×1, 500×1, 1000×0]

Default: {100,200,500}

API: [50] → {50}

(1,4) — Mümkün

Kaset: [50×1, 100×2, 200×2, 500×1, 1000×0]

Default: {100,200,500,1000}

API: [50] → {50}

2 API

(2,0) — İmkânsız

Gerekçe: Default=0 demek küçük toplamlarla 100 bile karşılanamıyor; aynı stokla 2 farklı API tutarını sağlamak mantıksal olarak mümkün değil (örnekler IsAmountDispensible kuralı altında).

(2,1) — Mümkün

Kaset: [50×1, 100×0, 200×0, 500×0, 1000×2]

Default: {1000}

API: [50, 1000] → {50,1000}

(2,2) — Mümkün

Kaset: [50×1, 100×0, 200×1, 500×1, 1000×0]

Default: {200,500}

API: [50, 500] → {50,500}

(2,3) — Mümkün

Kaset: [50×3, 100×1, 200×1, 500×1, 1000×0]

Default: {100,200,500}

API: [50, 150] → {50,150} (150 için 100+50)

(2,4) — Mümkün

Kaset: [50×1, 100×2, 200×2, 500×1, 1000×0]

Default: {100,200,500,1000}

API: [600, 1000] → {600,1000} (600 için 200+200+100+100; 1000 için toplam ≥1000)

3 API

(3,0) — İmkânsız

Gerekçe: Default=0 kısıtı varken (küçük toplam) 3 farklı API tutarını sağlamak mümkün değil.

(3,1) — Mümkün

Kaset: [50×0, 100×0, 200×0, 500×0, 1000×3]

Default: {1000}

API: [1000, 2000, 3000] → {1000,2000,3000}

(3,2) — Mümkün

Kaset: [50×1, 100×0, 200×1, 500×1, 1000×0]

Default: {200,500}

API: [50, 200, 500] → {50,200,500}

(3,3) — Mümkün

Kaset: [50×3, 100×1, 200×1, 500×1, 1000×0]

Default: {100,200,500}

API: [50, 200, 500] → {50,200,500}

(3,4) — Mümkün

Kaset: [50×1, 100×2, 200×2, 500×1, 1000×1]

Default: {100,200,500,1000}

API: [50, 1000, 2000] → {50,1000,2000}

≥4 API

(≥4,0) — İmkânsız

Gerekçe: Default=0 (çok düşük toplam) iken 4+ farklı API tutarı sağlamak mümkün değil.

(≥4,1) — Mümkün

Kaset: [50×0, 100×0, 200×0, 500×0, 1000×5]

Default: {1000}

API: [1000,2000,3000,4000,5000] → 5 adet (≥4)

(≥4,2) — Mümkün

Kaset: [50×0, 100×1, 200×0, 500×0, 1000×4]

Default: {100,1000}

API: [1000,2000,3000,4000,100] → 5 adet (≥4)

(≥4,3) — Mümkün

Kaset: [50×1, 100×1, 200×1, 500×1, 1000×0]

Default: {100,200,500}

API: [50,100,150,200,250] → 5 adet (≥4)
(150 = 100+50, 250 = 200+50; 1000 mümkün değil)

(≥4,4) — Mümkün

Kaset: [50×2, 100×3, 200×2, 500×2, 1000×2]

Default: {100,200,500,1000}

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

