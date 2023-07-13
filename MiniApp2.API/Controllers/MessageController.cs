using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniApp2.Core.Dtos;
using MiniApp2.Core.Entities;
using MiniApp2.Core.Services;

namespace MiniApp2.API.Controllers
{
    [Authorize]
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class MessageController : CustomBaseController
    {
        private readonly IService<Message, MessageDto> _service;

        public MessageController(IService<Message, MessageDto> service)
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