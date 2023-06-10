using Microsoft.EntityFrameworkCore;
using MiniApp3.Core.Dtos;
using MiniApp3.Core.Entities;
using MiniApp3.Core.Repositories;
using MiniApp3.Core.Services.Visual.Database;
using SharedLibrary.Dtos;
using System.Diagnostics;

namespace MiniApp3.Service.Services.ImageSaveServices.Database.Services.ReadServices
{
    public class ImageDbReadService : IImageDbReadService
    {
        private readonly IEntityRepository<ImageData> _repository;
        public ImageDbReadService(IEntityRepository<ImageData> repository)
        {
            _repository = repository;
        }

        public async Task<Response<ImageDbServiceResponse>> GetThumnailPhotoAsync(string id)
        {
            Stream? image = null;
            try
            {
                bool recordIsExist = _repository.Where(x => x.Id.ToString() == id).Any();
                if (!recordIsExist)
                {

                    return Response<ImageDbServiceResponse>.Fail("Image not found!", 404, true);
                }
                var result = await _repository.Where(x => x.Id.ToString() == id).FirstOrDefaultAsync();
                image = await _repository.ReadPhotoDirectlyFromDatabase(id, "ThumbnailContent");
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                return Response<ImageDbServiceResponse>.Fail(e.Message, 404, true);
            }

            var newDto = new ImageDbServiceResponse { Image = image };
            return Response<ImageDbServiceResponse>.Success(newDto, 200);
        }
    }
}
