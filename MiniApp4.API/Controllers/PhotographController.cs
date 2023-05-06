using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MiniApp4.API.Common.Constants;
using MiniApp4.API.Utilities.Visual.Concrete;
using MiniApp4.API.Utilities.Visual;
using SharedLibrary.Dtos;
using MiniApp4.API.Utilities.Visual.Abstract;

namespace MiniApp4.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PhotographController : CustomBaseController
    {
        private readonly IImageManager _imageManager;

        public PhotographController(IImageManager imageManager)
        {
            _imageManager = imageManager;
        }

        [HttpPost]
        [RequestSizeLimit(Magnitude.ThreeMegabytes)]
        public async Task<IActionResult> SavePhotos(IFormFile[] photos, CancellationToken cancellationToken)
        {
            if (photos.Length > Magnitude.ThreeMegabytes)
            {
                return ActionResultInstance(Response<NoDataDto>.Fail($"Image size can not be greater than 3 MB.", 400, true));
            }
            await _imageManager.Process(new ImageServerService(), photos.Select(x => new ImageInputModel
            {
                Name = x.FileName,
                Type = x.ContentType,
                Content = x.OpenReadStream()
            }));
            return ActionResultInstance(Response<NoDataDto>.Fail("Photo can not be empty.", 400, true));
        }
    }
}
