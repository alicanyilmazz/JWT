using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniApp3.API.Common.Constants;
using MiniApp3.Core.Entities;
using MiniApp3.Core.Services.Visual.Database;
using SharedLibrary.Dtos;

namespace MiniApp3.API.Controllers
{
    [Authorize]
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class DatabaseImagesController : CustomBaseController
    {
        private readonly IImageDbSaveManager _imageManager;
        private readonly IImageDbSaveServices _imageService;
        private readonly IImageDbReadService _imageReadService;

        public DatabaseImagesController(IImageDbSaveManager imageManager, IImageDbSaveServices imageService, IImageDbReadService imageReadService)
        {
            _imageManager = imageManager;
            _imageService = imageService;
            _imageReadService = imageReadService;
        }

        [HttpPost()]
        [RequestSizeLimit(Magnitude.ThreeMegabytes)]
        public async Task<IActionResult> Save(IFormFile[] photos, CancellationToken cancellationToken)
        {
            if (photos.Length > Magnitude.ThreeMegabytes)
            {
                return ActionResultInstance(Response<NoDataDto>.Fail($"Image size can not be greater than 3 MB.", 400, true));
            }
            var result = await _imageManager.Save(_imageService, photos.Select(x => new ImageDbServiceRequest
            {
                Name = x.FileName,
                Type = x.ContentType,
                Content = x.OpenReadStream()
            }));
            return ActionResultInstance(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(string id)
        {
            var result = await _imageReadService.GetThumnailPhotoAsync(id);
            if (result.IsSuccessful)
                return File(result.Data.Image, "image/jpeg");

            return ActionResultInstance(result);
        }

        //[HttpGet("{id}")]
        //public async Task<IActionResult> GetFromCache(string id)
        //{
        //    var result = await _imageReadService.GetThumnailPhotoAsync(id);
        //    if (result.IsSuccessful)
        //        return ReturnImage(result.Data.Image);

        //    return ActionResultInstance(result);
        //}
    }
}
