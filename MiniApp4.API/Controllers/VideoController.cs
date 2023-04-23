using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MiniApp4.API.Common.Constants;
using MiniApp4.Core.Dtos;
using MiniApp4.Core.Entities;
using MiniApp4.Core.Services;
using SharedLibrary.Dtos;

namespace MiniApp4.API.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class VideoController : CustomBaseController
    {
        private readonly IService<Video, VideoDto> _videoService;

        public VideoController(IService<Video, VideoDto> videoService)
        {
            _videoService = videoService;
        }

        [HttpPost]
        public async Task<IActionResult> SaveVideo(IFormFile video, CancellationToken cancellationToken)
        {
            if (video.Length > Magnitude.FourMegabytes)
            {
                return ActionResultInstance(Response<NoDataDto>.Fail($"Image size can not be greater than 3 MB.", 400, true));
            }
            if (video != null && video.Length > 0)
            {
                string randomFileName = string.Empty;
                randomFileName = Guid.NewGuid().ToString() + Path.GetExtension(video.FileName);
                var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/videos", randomFileName);
                using var stream = new FileStream(path, FileMode.Create);
                await video.CopyToAsync(stream, cancellationToken);
                var videoInfo = new VideoDto { Url = "videos/" + randomFileName, VideoId = Guid.NewGuid().ToString() };
                return ActionResultInstance(await _videoService.AddAsync(videoInfo));
            }
            return ActionResultInstance(Response<NoDataDto>.Fail("Video can not be empty.", 400, true));
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteVideo(VideoDeleteDto videoDeleteDto, CancellationToken cancellationToken)
        {
            var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", videoDeleteDto.Url);

            if (System.IO.File.Exists(path))
            {
                System.IO.File.Delete(path);
                return ActionResultInstance(await _videoService.Remove(videoDeleteDto.Id));
            }

            return ActionResultInstance(Response<NoDataDto>.Fail("Photo does not exist.", 400, true));
        }
    }
}
