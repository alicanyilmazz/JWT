using MiniApp3.Core.Entities;
using MiniApp3.Core.Services.Visual.Database;
using SharedLibrary.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniApp3.Service.Services.ImageSaveServices.Database.Managers
{
    public class ImageDbSaveManager : IImageDbSaveManager
    {
        public async Task<Response<NoDataDto>> Save(IImageDbSaveServices services, IEnumerable<ImageDbServiceRequest> images)
        {
            return await services.SaveAsync(images);
        }
    }
}
