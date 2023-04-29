using Microsoft.AspNetCore.Mvc.RazorPages;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using Image = SixLabors.ImageSharp.Image;

namespace MiniApp4.API.Utilities.Visual
{
    public class ImageService : IImageServices
    {
        private const int ThumbnailWidth = 300;
        private const int FullScreenWidth = 1000;
        public async Task Process(IEnumerable<ImageInputModel> images)
        {
            var tasks = new List<Task>();
            foreach (var image in images)
            {
                tasks.Add(Task.Run(() =>
                {
                    using var imageResult = Image.Load(image.Content);
                    SaveImageAsync(imageResult, $"Original_{image.Name}", imageResult.Width);
                    SaveImageAsync(imageResult, $"FullScreen_{image.Name}", FullScreenWidth);
                    SaveImageAsync(imageResult, $"Thumbnail_{image.Name}", ThumbnailWidth);
                }));
            }
            await Task.WhenAll(tasks);
        }

        private async Task SaveImageAsync(Image image, string name, int resizeWidth)
        {
            var width = image.Width;
            var height = image.Height;
            if (width > resizeWidth)
            {
                height = (int)((double)(resizeWidth / width * height));
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
