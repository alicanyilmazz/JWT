﻿using MiniApp3.Core.Dtos;
using MiniApp3.Core.Dtos.StoredProcedureDto;
using MiniApp3.Core.Repositories;
using MiniApp3.Core.Repositories.StoredProcedureRepositories;
using MiniApp3.Core.Services.Visual.Server;
using MiniApp3.Service.DtoMappers;
using SharedLibrary.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniApp3.Service.Services.ImageSaveServices.Server.Services.ReadServices
{
    public class ImageServerReadServiceDefault : IImageServerReadService
    {
        private readonly IStoredProcedureQueryRepository _storedProcedureQueryRepository;
        public ImageServerReadServiceDefault(IStoredProcedureQueryRepository storedProcedureQueryRepository)
        {
            _storedProcedureQueryRepository = storedProcedureQueryRepository;
        }
        public async Task<Response<IEnumerable<ImageServerServiceResponse>>> GetPhotosAsync()
        {
            IEnumerable<ImageServerServiceResponse> entities;
            try
            {
                entities = ObjectMapper.Mapper.Map<IEnumerable<ImageServerServiceResponse>>(await _storedProcedureQueryRepository.GetImages());
            }
            catch (Exception e)
            {
                return Response<IEnumerable<ImageServerServiceResponse>>.Fail(e.Message, 404, true);
            }
            return Response<IEnumerable<ImageServerServiceResponse>>.Success(entities, 200);
        }

        public async Task<Response<IEnumerable<ImageServerServiceResponse>>> GetPhotoAsync(string imageId)
        {
            IEnumerable<ImageServerServiceResponse> entities;
            try
            {
                entities = ObjectMapper.Mapper.Map<IEnumerable<ImageServerServiceResponse>>(await _storedProcedureQueryRepository.GetImage(imageId));
            }
            catch (Exception e)
            {
                return Response<IEnumerable<ImageServerServiceResponse>>.Fail(e.Message, 404, true);
            }
            return Response<IEnumerable<ImageServerServiceResponse>>.Success(entities, 200);
        }
    }
}
