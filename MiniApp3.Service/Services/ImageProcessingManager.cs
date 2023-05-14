using MiniApp3.Core.Entities;
using MiniApp3.Core.Services;
using SharedLibrary.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniApp3.Service.Services
{
    public class ImageProcessingManager : IImageProcessingManager
    {
        public async Task<Response<NoDataDto>> Process(IImageProcessingServices services, IEnumerable<ImageInputModel> images)
        {
            return await services.ProcessAsync(images);
        }
    }
}
