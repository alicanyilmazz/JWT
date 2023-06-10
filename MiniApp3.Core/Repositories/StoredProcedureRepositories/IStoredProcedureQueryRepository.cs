using MiniApp3.Core.Dtos.StoredProcedureDto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniApp3.Core.Repositories.StoredProcedureRepositories
{
    public interface IStoredProcedureQueryRepository
    {
        public Task<List<ServerImagesInformation>> ReadPhotoInformation();
        public Task<List<ImageQualityResponse>> GetImageQualityConfigs();
    }
}
