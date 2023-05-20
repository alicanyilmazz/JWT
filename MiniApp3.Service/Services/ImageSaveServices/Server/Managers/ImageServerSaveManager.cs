using MiniApp3.Core.Entities;
using MiniApp3.Core.Services.Visual.Server;
using SharedLibrary.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniApp3.Service.Services.ImageSaveServices.Server.Managers
{
    public class ImageServerSaveManager : IImageServerSaveManager
    {
        public async Task<Response<NoDataDto>> Save(IImageServerSaveService services, IEnumerable<ImageDbServiceRequest> images)
        {
            return await services.SaveAsync(images);
        }
    }
}
