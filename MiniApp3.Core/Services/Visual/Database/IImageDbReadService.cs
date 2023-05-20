using MiniApp3.Core.Dtos;
using MiniApp3.Core.Entities;
using SharedLibrary.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniApp3.Core.Services.Visual.Database
{
    public interface IImageDbReadService
    {
        public Task<Response<ImageDbServiceResponse>> GetThumnailPhotoAsync(string id);
    }
}
