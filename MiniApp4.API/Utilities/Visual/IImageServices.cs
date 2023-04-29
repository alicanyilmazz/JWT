namespace MiniApp4.API.Utilities.Visual
{
    public interface IImageServices
    {
        public Task Process(IEnumerable<ImageInputModel> images);
    }
}
