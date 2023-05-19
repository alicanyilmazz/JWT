using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MiniApp3.API.Common.Constants;
using MiniApp3.Core.Entities;
using MiniApp3.Core.Services;
using MiniApp3.Service.Services;
using SharedLibrary.Dtos;

namespace MiniApp3.API.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class SavePhotoController : CustomBaseController
    {
        private readonly IImageProcessingManager _imageManager;
        private readonly IImageProcessingServices _imageService;

        public SavePhotoController(IImageProcessingManager imageManager, IImageProcessingServices imageService)
        {
            _imageManager = imageManager;
            _imageService = imageService;
        }

        [HttpPost]
        [RequestSizeLimit(Magnitude.ThreeMegabytes)]
        public async Task<IActionResult> SavePhotos(IFormFile[] photos, CancellationToken cancellationToken)
        {
            if (photos.Length > Magnitude.ThreeMegabytes)
            {
                return ActionResultInstance(Response<NoDataDto>.Fail($"Image size can not be greater than 3 MB.", 400, true));
            }
            var result = await _imageManager.Process(_imageService, photos.Select(x => new ImageInputModel
            {
                Name = x.FileName,
                Type = x.ContentType,
                Content = x.OpenReadStream()
            }));
            return ActionResultInstance(result);
        }
    }
}
