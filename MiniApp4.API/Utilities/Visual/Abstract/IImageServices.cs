namespace MiniApp4.API.Utilities.Visual.Abstract
{
    public interface IImageServices
    {
        public Task ProcessAsync(IEnumerable<ImageInputModel> images);
    }
}
