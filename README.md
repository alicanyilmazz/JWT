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
