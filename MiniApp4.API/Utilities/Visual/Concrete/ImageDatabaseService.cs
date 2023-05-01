using MiniApp4.API.Utilities.Visual.Abstract;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using Image = SixLabors.ImageSharp.Image;

namespace MiniApp4.API.Utilities.Visual.Concrete
{
    public class ImageDatabaseService : IImageServices
    {
        private const int ThumbnailWidth = 300;
        private const int FullScreenWidth = 1000;
        public async Task ProcessAsync(IEnumerable<ImageInputModel> images)
        {
            var tasks = images.Select(image => Task.Run(async () =>
            {
                try
                {
                    using var imageResult = await Image.LoadAsync(image.Content);
                    var original = await SaveImageAsync(imageResult, imageResult.Width);
                    var fullscreen = await SaveImageAsync(imageResult, FullScreenWidth);
                    var thumnail = await SaveImageAsync(imageResult, ThumbnailWidth);
                }
                catch (Exception)
                {
                    // Log
                    throw;
                }
            })).ToList();

            await Task.WhenAll(tasks);
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
