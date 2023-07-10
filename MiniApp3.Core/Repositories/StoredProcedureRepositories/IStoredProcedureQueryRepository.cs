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
        public Task<List<ServerImagesInformation>> GetImage(string imageId);
        public Task<List<ServerImagesInformation>> GetImages();
        public Task<List<ImageQualityResponse>> GetImageQualityConfigs();
        public Task<Stream> ReadPhotoDirectlyFromDatabase(string id, string content);
        /// ExecuteSqlInterpolatedAsync yöntemi, çıktı parametrelerini doğrudan desteklemez. Bu nedenle, çıktı parametresi kullanırken ExecuteSqlRawAsync yöntemini kullanmanız gerekmektedir.
        /// <summary>
        /// This methods returns number of ImageFile record.
        /// </summary>
        /// <returns>NumberOfImageFile</returns>
        public Task<int> GetNumberOfRecord();
    }
}
