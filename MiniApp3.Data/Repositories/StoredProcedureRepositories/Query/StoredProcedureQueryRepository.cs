using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MiniApp3.Core.Dtos.StoredProcedureDto;
using MiniApp3.Core.Entities;
using MiniApp3.Core.Repositories.StoredProcedureRepositories;
using MiniApp3.Data.Context;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniApp3.Data.Repositories.StoredProcedureRepositories.Query
{
    public class StoredProcedureQueryRepository : IStoredProcedureQueryRepository
    {
        private readonly DbContext _context;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public StoredProcedureQueryRepository(IServiceScopeFactory serviceScopeFactory)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _context = _serviceScopeFactory.CreateScope().ServiceProvider.GetRequiredService<AppDbContext>();
        }
        public async Task<List<ServerImagesInformation>> ReadPhotoInformation()
        {
            return await _context.Set<ServerImagesInformation>().FromSqlRaw("EXEC GET_ALL_IMAGES").ToListAsync();
        }

        public async Task<List<ImageQualityResponse>> GetImageQualityConfigs()
        {
            return await _context.Set<ImageQualityResponse>().FromSqlInterpolated($"EXEC GET_IMAGE_QUALITY").ToListAsync();
        }
        public async Task<int> GetNumberOfRecord()
        {
            var recordCountParameter = new SqlParameter
            {
                ParameterName = "@RecordCount",
                SqlDbType = SqlDbType.Int,
                Direction = ParameterDirection.Output
            };
            await _context.Database.ExecuteSqlRawAsync($"EXEC GET_NUMBER_OF_IMAGE_FILE @RecordCount OUTPUT", recordCountParameter);

            return (int)recordCountParameter.Value;
        }
    }
}
