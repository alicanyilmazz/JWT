using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniApp3.Core.Services
{
    public interface IImageReadService
    {
        public Task<Stream?> GetThumnailPhotoAsync(string id);
    }
}
