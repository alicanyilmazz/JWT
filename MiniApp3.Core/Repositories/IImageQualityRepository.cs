using MiniApp3.Core.Dtos.StoredProcedureDto;
using MiniApp3.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniApp3.Core.Repositories
{
    public interface IImageQualityRepository
    {
        public Task<List<ImageQualityResult>> GetImageQualityConfigs();
    }
}
