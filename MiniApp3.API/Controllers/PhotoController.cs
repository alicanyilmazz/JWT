using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MiniApp3.API.Common.Constants;
using MiniApp3.Core.Entities;
using MiniApp3.Core.Services;
using MiniApp3.Service.Services;
using SharedLibrary.Dtos;

namespace MiniApp3.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PhotoController : CustomBaseController
    {
        private readonly IImageProcessingManager _imageManager;
        private readonly IImageProcessingServices _imageService;
        private readonly IImageReadService _imageReadService;

        public PhotoController(IImageProcessingManager imageManager, IImageProcessingServices imageService, IImageReadService imageReadService)
        {
            _imageManager = imageManager;
            _imageService = imageService;
            _imageReadService = imageReadService;
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

        [HttpGet("{id}")]
        public async Task<IActionResult> GetThumnailPhoto(string id)
        {
            var result = await _imageReadService.GetThumnailPhotoAsync(id);
            return File(result,"image/jpeg");
        }

        //[HttpGet]
        //public HttpResponseMessage Generate()
        //{
        //    var stream = new MemoryStream();
        //    // processing the stream.

        //    var result = new HttpResponseMessage(HttpStatusCode.OK)
        //    {
        //        Content = new ByteArrayContent(stream.ToArray())
        //    };
        //    result.Content.Headers.ContentDisposition =
        //        new System.Net.Http.Headers.ContentDispositionHeaderValue("attachment")
        //        {
        //            FileName = "CertificationCard.pdf"
        //        };
        //    result.Content.Headers.ContentType =
        //        new MediaTypeHeaderValue("application/octet-stream");

        //    return result;
        //}

        //public IActionResult ReturnPhoto(Stream photo)
        //{
        //    var headers = this.Response.GetTypedHeaders();

        //    headers.CacheControl = new Microsoft.Net.Http.Headers.CacheControlHeaderValue
        //    {
        //        Public = true,
        //        MaxAge = TimeSpan.FromDays(1)
        //    };

        //    headers.Expires = new DateTimeOffset(DateTime.UtcNow.AddHours(1));

        //    return this.File(photo, "image/jpeg");
        //}
    }
}
