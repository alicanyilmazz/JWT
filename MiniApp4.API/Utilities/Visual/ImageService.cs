﻿using Microsoft.AspNetCore.Mvc.RazorPages;
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
            var tasks = images.Select(image => Task.Run(async () =>
            {
                using var imageResult = await Image.LoadAsync(image.Content);
                await SaveImageAsync(imageResult, $"Original_{image.Name}", imageResult.Width);
                await SaveImageAsync(imageResult, $"FullScreen_{image.Name}", FullScreenWidth);
                await SaveImageAsync(imageResult, $"Thumbnail_{image.Name}", ThumbnailWidth);
            })).ToList();
           
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
