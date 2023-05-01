using MiniApp4.API.Utilities.Visual.Abstract;

namespace MiniApp4.API.Utilities.Visual.Concrete
{
    public class ImageManager : IImageManager
    {
        public async Task Process(IImageServices services, IEnumerable<ImageInputModel> images)
        {
            await services.ProcessAsync(images);
        }
    }
}
