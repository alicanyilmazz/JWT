using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MiniApp3.Core.Dtos;
using MiniApp3.Core.Entities;
using MiniApp3.Core.Services;
using SharedLibrary.Dtos;
using System.Net;
using System.Net.Http.Headers;

namespace MiniApp3.API.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class GetPhotoController : CustomBaseController
    {
        private readonly IImageReadService _imageReadService;

        public GetPhotoController(IImageReadService imageReadService)
        {
            _imageReadService = imageReadService;
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetPhotoFromDb(string id)
        {
            var result = await _imageReadService.GetThumnailPhotoAsync(id);
            if (result.IsSuccessful)
                return File(result.Data.Image, "image/jpeg");

            return ActionResultInstance(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetPhotoFromDbCache(string id)
        {
            var result = await _imageReadService.GetThumnailPhotoAsync(id);
            if (result.IsSuccessful)
                return ReturnImage(result.Data.Image);

            return ActionResultInstance(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetPhotoFromServer(string id)
        {
            var result = await _imageReadService.GetThumnailPhotoAsync(id);
            return File(result.Data.Image, "image/jpeg");
        }
    }
}
