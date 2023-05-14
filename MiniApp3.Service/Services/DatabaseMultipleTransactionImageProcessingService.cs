using MiniApp3.Core.Entities;
using MiniApp3.Core.Repositories;
using MiniApp3.Core.Services;
using MiniApp3.Core.UnitOfWork;
using SharedLibrary.Dtos;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;
using Image = SixLabors.ImageSharp.Image;

namespace MiniApp3.Service.Services
{
    public class DatabaseMultipleTransactionImageProcessingService : IImageProcessingServices
    {
        private const int ThumbnailWidth = 300;
        private const int FullScreenWidth = 1000;

        private readonly IUnitOfWork _unitOfWork;
        private readonly IRepository<ImageData> _repository;

        public DatabaseMultipleTransactionImageProcessingService(IUnitOfWork unitOfWork, IRepository<ImageData> repository)
        {
            _unitOfWork = unitOfWork;
            _repository = repository;
        }

        public async Task<Response<NoDataDto>> ProcessAsync(IEnumerable<ImageInputModel> images)
        {
            var tasks = images.Select(image => Task.Run(async () =>
            {
                try
                {
                    using var imageResult = await Image.LoadAsync(image.Content);
                    var original = await SaveImageAsync(imageResult, imageResult.Width);
                    var fullscreen = await SaveImageAsync(imageResult, FullScreenWidth);
                    var thumnail = await SaveImageAsync(imageResult, ThumbnailWidth);

                    await _repository.AddAsync(new ImageData
                    {
                        OriginalFileName = image.Name,
                        OriginalType = image.Type,
                        OriginalContent = original,
                        ThumbnailContent = thumnail,
                        FullScreenContent = fullscreen
                    });

                    //await _repository.CommitAsync();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("XXX"+ex);
                    throw;
                }
            })).ToList();

            try
            {
                await Task.WhenAll(tasks);
            }
            catch (Exception e)
            {
                return Response<NoDataDto>.Fail(e.Message, 404, true);
            }
            return Response<NoDataDto>.Success(200);
        }
        private async Task<byte[]> SaveImageAsync(Image image, int resizeWidth)
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
            var memoryStream = new MemoryStream();
            await image.SaveAsJpegAsync(memoryStream, new JpegEncoder
            {
                Quality = 75
            });
            return memoryStream.ToArray();
        }
    }
}
