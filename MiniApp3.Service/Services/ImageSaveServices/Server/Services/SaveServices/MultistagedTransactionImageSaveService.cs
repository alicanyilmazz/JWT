using MiniApp3.Core.Dtos.StoredProcedureDto;
using MiniApp3.Core.Entities;
using MiniApp3.Core.Repositories;
using MiniApp3.Core.Services;
using MiniApp3.Core.Services.Visual.Server;
using MiniApp3.Core.UnitOfWork;
using MiniApp3.Data.Repositories.StoredProcedureRepositories;
using SharedLibrary.Dtos;
using SixLabors.ImageSharp.Formats.Jpeg;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;
using Image = SixLabors.ImageSharp.Image;

namespace MiniApp3.Service.Services.ImageSaveServices.Server.Services.SaveServices
{
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

                    var original = new ImageResult(image: imageResult, name: $"Original_{name}", path: storagePath, resizeWidth: imageResult.Width);
                    var fullscreen = new ImageResult(image: imageResult, name: $"FullScreen_{name}", path: storagePath, resizeWidth: FullScreenWidth);
                    var thumbnail = new ImageResult(image: imageResult, name: $"Thumbnail_{name}", path: storagePath, resizeWidth: ThumbnailWidth);

                    List<Task<string>> task = new List<Task<string>>
                    {
                        SaveImageAsync(original),
                        SaveImageAsync(fullscreen),
                        SaveImageAsync(thumbnail)
                    };

                    await Task.WhenAll(task);
                    foreach (var item in task)
                    {
                        if (item.IsCompletedSuccessfully)
                        {
                            imageDetailStorage.TryAdd(item.Result, new ImageFileDetail()
                            {
                                ImageId = id,
                                Type = item.Result,
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
                foreach (var image in imageStorage.Values)
                {
                    //await _repository.SaveImagesServerData(image);
                }
                foreach (var image in imageDetailStorage.Values)
                {
                    //await _repository.SaveImagesServerData(image);
                }
            }
            catch (Exception e)
            {
                return Response<NoDataDto>.Fail(e.Message, 404, true);
            }
            return Response<NoDataDto>.Success(200);
        }

        private async Task<string> SaveImageAsync(ImageResult imageResult)
        {
            try
            {
                var width = imageResult.Image.Width;
                var height = imageResult.Image.Height;
                if (width > imageResult.ResizeWidth)
                {
                    height = (int)(double)(imageResult.ResizeWidth / width * height);
                    width = imageResult.ResizeWidth;
                }
                imageResult.Image.Mutate(x => x.Resize(new Size(width, height)));
                imageResult.Image.Metadata.ExifProfile = null;
                await imageResult.Image.SaveAsJpegAsync($"{imageResult.Path}/{imageResult.Name}", new JpegEncoder
                {
                    Quality = 75
                });
            }
            catch (Exception)
            {
                // Log
                throw;
            }
            return imageResult.Name;
        }

        class ImageResult
        {
            public Image Image { get; set; }
            public string Name { get; set; }
            public string Path { get; set; }
            public int ResizeWidth { get; set; }
            public ImageResult(Image image, string name, string path, int resizeWidth)
            {
                Image = image;
                Name = name;
                Path = path;
                ResizeWidth = resizeWidth;
            }
        }
    }
}
