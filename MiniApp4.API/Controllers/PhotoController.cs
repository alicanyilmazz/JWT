﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniApp4.Core.Dtos;
using MiniApp4.Core.Entities;
using MiniApp4.Core.Services;
using SharedLibrary.Dtos;
using System.Security.Claims;

namespace MiniApp4.API.Controllers
{
    [Authorize]
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class PhotoController : CustomBaseController
    {
        private readonly IService<Photo, PhotoDto> _photoService;

        public PhotoController(IService<Photo, PhotoDto> photoService)
        {
            _photoService = photoService;
        }

        [HttpGet]
        public async Task<IActionResult> GetPhotos(CancellationToken cancellationToken)
        {
            return ActionResultInstance(await _photoService.GetAllAsync());
        }

        [HttpPost]
        public async Task<IActionResult> SavePhoto(IFormFile photo, CancellationToken cancellationToken)
        {
            if (photo.Length > 3 * 1024 * 1024)
            {
                return ActionResultInstance(Response<NoDataDto>.Fail($"Image size can not be greater than 3 MB.", 400, true));
            }
            if (photo != null && photo.Length > 0)
            {
                var userName = HttpContext.User?.Identity?.Name;
                var userIdClaim = User.Claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier);
                string randomFileName = string.Empty;
                randomFileName = Guid.NewGuid().ToString() + Path.GetExtension(photo.FileName);
                var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/photos", randomFileName);
                using var stream = new FileStream(path, FileMode.Create);
                await photo.CopyToAsync(stream, cancellationToken);
                var photoInfo = new PhotoDto { Url = "photos/" + randomFileName, PhotoId = userName + '|' + userIdClaim?.Value };
                return ActionResultInstance(await _photoService.AddAsync(photoInfo));
            }
            return ActionResultInstance(Response<NoDataDto>.Fail("Photo can not be empty.", 400, true));
        }

      
        [HttpDelete]
        public async Task<IActionResult> DeletePhoto(PhotoDeleteDto photoDeleteDto, CancellationToken cancellationToken)
        {
            var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", photoDeleteDto.Url);

            if (System.IO.File.Exists(path))
            {
                System.IO.File.Delete(path);
                return ActionResultInstance(await _photoService.Remove(photoDeleteDto.Id));
            }

            return ActionResultInstance(Response<NoDataDto>.Fail("Photo does not exist.", 400, true));
        }
    }
}
