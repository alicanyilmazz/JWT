namespace MiniApp4.API.Utilities.Visual.Abstract
{
    public interface IImageManager
    {
        public Task Process(IImageServices services, IEnumerable<ImageInputModel> images);
    }
}
