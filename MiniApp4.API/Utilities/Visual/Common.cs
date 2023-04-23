using SixLabors.ImageSharp.Memory;

namespace MiniApp4.API.Utilities.Visual
{
    public class Common
    {
        public static string ResizeImage(Image image , int maxWidth , int maxHeight)
        {
            if (image.Width > maxWidth || image.Height > maxHeight)
            {
                double widthRatio = (double)image.Width / (double)maxWidth;
                double heightRatio = (double)image.Height / (double)maxHeight;
                double ratio = Math.Max(widthRatio, heightRatio);
                int newWidth = (int)(image.Width / ratio);
                int newHeight = (int)(image.Height / ratio);
                return newHeight.ToString() + "," + newWidth.ToString();
            }
            return image.Height.ToString() + "," + image.Width.ToString();
        }
    }
}
