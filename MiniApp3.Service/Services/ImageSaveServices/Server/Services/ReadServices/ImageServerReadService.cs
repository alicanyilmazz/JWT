using MiniApp3.Core.Dtos;
using MiniApp3.Core.Entities;
using MiniApp3.Core.Repositories;
using MiniApp3.Core.Services.Visual.Server;
using SharedLibrary.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniApp3.Service.Services.ImageSaveServices.Server.Services.ReadServices
{
    public class ImageServerReadService : IImageServerReadService
    {
        private readonly IEntityRepository<ImageFile> _repository;
        public ImageServerReadService(IEntityRepository<ImageFile> repository)
        {
            _repository = repository;
        }

        public async Task<Response<IEnumerable<ImageServerServiceResponse>>> GetAllPhotoAsync()
        {
            var result = await _repository.ReadPhotoInfoDirectlyFromDatabase();
            var res = new List<ImageServerServiceResponse>();
            foreach (var item in result)
            {
                res.Add(new ImageServerServiceResponse { Path = item });
            }
            return Response<IEnumerable<ImageServerServiceResponse>>.Success(res, 200);
        }

        public Task<Response<ImageServerServiceResponse>> GetThumnailPhotoAsync(string id)
        {
            throw new NotImplementedException();
        }
    }
}
