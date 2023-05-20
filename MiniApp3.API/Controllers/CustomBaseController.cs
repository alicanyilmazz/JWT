using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SharedLibrary.Dtos;

namespace MiniApp3.API.Controllers
{
    public class CustomBaseController : ControllerBase
    {
        public IActionResult ActionResultInstance<T>(Response<T> response) where T : class
        {
            return new ObjectResult(response)
            {
                StatusCode = response.StatusCode
            };
        }
        //public IActionResult ReturnImage(Stream image)
        //{
        //    var headers = this.Response.GetTypedHeaders();
        //    headers.CacheControl = new Microsoft.Net.Http.Headers.CacheControlHeaderValue
        //    {
        //        Public = true,
        //        MaxAge = TimeSpan.FromDays(1)
        //    };
        //    headers.Expires = new DateTimeOffset(DateTime.UtcNow.AddDays(1));

        //    return this.File(image, "image/jpeg");
        //}
    }
}
