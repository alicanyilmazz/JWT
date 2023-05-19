using Microsoft.EntityFrameworkCore;
using MiniApp3.Core.Dtos;
using MiniApp3.Core.Entities;
using MiniApp3.Core.Repositories;
using MiniApp3.Core.Services;
using SharedLibrary.Dtos;
using System.Diagnostics;

namespace MiniApp3.Service.Services
{
    public class ImageReadService : IImageReadService
    {
        private readonly IRepository<ImageData> _repository;
        public ImageReadService(IRepository<ImageData> repository)
        {
            _repository = repository;
        }

        public async Task<Response<ImageInformationDto>> GetThumnailPhotoAsync(string id)
        {
            Stream? image = null;
            try
            {
                bool recordIsExist = _repository.Where(x => x.Id.ToString() == id).Any();
                if (!recordIsExist)
                {

                    return Response<ImageInformationDto>.Fail("Image not found!", 404, true);
                }
                var result = await _repository.Where(x => x.Id.ToString() == id).FirstOrDefaultAsync();
                image = await _repository.ReadPhotoDirectlyFromDatabase(id, "ThumbnailContent");
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                return Response<ImageInformationDto>.Fail(e.Message, 404, true);
            }

            var newDto = new ImageInformationDto { Image = image };
            return Response<ImageInformationDto>.Success(newDto, 200);
        }
    }
}
