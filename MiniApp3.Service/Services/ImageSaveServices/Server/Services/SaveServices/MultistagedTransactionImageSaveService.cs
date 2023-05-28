using MiniApp3.Core.Common.Constants;
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
        private readonly IImageQualityRepository _qualityRepository;
        public MultistagedTransactionImageSaveService(IUnitOfWork unitOfWork, IRepository<ImageFile> repository, IImageQualityRepository qualityRepository)
        {
            _unitOfWork = unitOfWork;
            _repository = repository;
            _qualityRepository = qualityRepository;
        }
        public async Task<Response<NoDataDto>> SaveAsync(IEnumerable<ImageDbServiceRequest> images, string directory)
        {
            var imageStorage = new ConcurrentDictionary<string, ImageFile>();
            var imageDetailStorage = new ConcurrentDictionary<string, ImageFileDetail>();
            var totalImages = await _repository.CountAsync();
            var imageQualityConfigs = await _qualityRepository.GetImageQualityConfigs();
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

                    List<Task> tasks = new List<Task>();
                    if (imageQualityConfigs.Count > 0)
                    {
                        foreach (var config in imageQualityConfigs)
                        {
                            if (config.IsOriginal)
                            {
                                tasks.Add(SaveImageAsync(new ImageProcessInput(image: imageResult, name: $"{config.Name}_{name}", path: storagePath, resizeWidth: imageResult.Width, quality: config.Rate)));
                            }
                            else
                            {
                                tasks.Add(SaveImageAsync(new ImageProcessInput(image: imageResult, name: $"{config.Name}_{name}", path: storagePath, resizeWidth: config.ResizeWidth, quality: config.Rate)));
                            }
                        }
                    }

                    await Task.WhenAll(tasks);
                    foreach (var config in imageQualityConfigs)
                    {
                        imageDetailStorage.TryAdd(config.Name, new ImageFileDetail()
                        {
                            ImageId = id,
                            Type = config.Name,
                            QualityRate = $"{config.Rate}%"
                        });
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
                    await _repository.SaveImageImageFile(image);
                }
                foreach (var image in imageDetailStorage.Values)
                {
                    await _repository.SaveImageImageFileDetail(image);
                }
            }
            catch (Exception e)
            {
                return Response<NoDataDto>.Fail(e.Message, 404, true);
            }
            return Response<NoDataDto>.Success(200);
        }

        private async Task SaveImageAsync(ImageProcessInput input)
        {
            try
            {
                var width = input.Image.Width;
                var height = input.Image.Height;
                if (width > input.ResizeWidth)
                {
                    height = (int)(double)(input.ResizeWidth / width * height);
                    width = input.ResizeWidth;
                }
                input.Image.Mutate(x => x.Resize(new Size(width, height)));
                input.Image.Metadata.ExifProfile = null;
                await input.Image.SaveAsJpegAsync($"{input.Path}/{input.Name}", new JpegEncoder
                {
                    Quality = input.Quality,
                });
            }
            catch (Exception)
            {
                throw;
            }
        }

        class ImageProcessInput
        {
            public Image Image { get; set; }
            public string Name { get; set; }
            public string Path { get; set; }
            public int ResizeWidth { get; set; }
            public string Type { get; set; }
            public int Quality { get; set; }
            public ImageProcessInput(Image image, string name, string path, int resizeWidth, int quality)
            {
                Image = image;
                Name = name;
                Path = path;
                ResizeWidth = resizeWidth;
                Quality = quality;
            }
        }
    }
}
