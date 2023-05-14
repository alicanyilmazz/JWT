using MiniApp3.Core.Entities;
using MiniApp3.Core.Repositories;
using MiniApp3.Core.Services;
using MiniApp3.Core.UnitOfWork;
using SharedLibrary.Dtos;
using SixLabors.ImageSharp.Formats.Jpeg;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;
using Image = SixLabors.ImageSharp.Image;

namespace MiniApp3.Service.Services
{
    public class ServerSideTransactionImageProcessingService : IImageProcessingServices
    {
        private const int ThumbnailWidth = 300;
        private const int FullScreenWidth = 1000;
        public async Task<Response<NoDataDto>> ProcessAsync(IEnumerable<ImageInputModel> images)
        {
            var tasks = images.Select(image => Task.Run(async () =>
            {
                try
                {
                    using var imageResult = await Image.LoadAsync(image.Content);
                    await SaveImageAsync(imageResult, $"Original_{image.Name}", imageResult.Width);
                    await SaveImageAsync(imageResult, $"FullScreen_{image.Name}", FullScreenWidth);
                    await SaveImageAsync(imageResult, $"Thumbnail_{image.Name}", ThumbnailWidth);
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
            }
            catch (Exception e)
            {
                return Response<NoDataDto>.Fail(e.Message, 404, true);
            }
            return Response<NoDataDto>.Success(200);
        }

        private async Task SaveImageAsync(Image image, string name, int resizeWidth)
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
            await image.SaveAsJpegAsync(name, new JpegEncoder
            {
                Quality = 75
            });
        }
    }
}
