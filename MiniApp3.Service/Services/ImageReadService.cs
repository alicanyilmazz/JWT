using MiniApp3.Core.Entities;
using MiniApp3.Core.Repositories;
using MiniApp3.Core.Services;
using MiniApp3.Core.UnitOfWork;
using SharedLibrary.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniApp3.Service.Services
{
    public class ImageReadService : IImageReadService
    {
        private readonly IRepository<ImageData> _repository;
        public ImageReadService(IRepository<ImageData> repository)
        {
            _repository = repository;
        }
        public async Task<Response<Stream?>> GetThumnailPhotoAsync(string id)
        { 
            try
            {
                await _repository.ReadPhotoDirectlyFromDatabase(id, "ThumbnailContent");
            }
            catch (Exception e)
            {
                return Response<NoDataDto>.Fail(e.Message, 404, true);
            }
            return Response<NoDataDto>.Success(200);
        }
    }
}
