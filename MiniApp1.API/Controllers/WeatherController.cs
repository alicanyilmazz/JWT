using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniApp1.Core.Dtos;
using MiniApp1.Core.Entities;
using MiniApp1.Core.Services;

namespace MiniApp1.API.Controllers
{
    //[Authorize]
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class WeatherController : CustomBaseController
    {
        private readonly IService<Weather, WeatherDto> _service;

        public WeatherController(IService<Weather, WeatherDto> service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            return ActionResultInstance(await _service.GetAllAsync());
        }
    }
}