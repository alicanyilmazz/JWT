using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MiniApp3.Core.Dtos;
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
using static System.Net.Mime.MediaTypeNames;

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
        public async Task<List<ServerImagesInformation>> GetImages(string imageId)
        {
            var ImageId = new SqlParameter("ImageId", imageId);
            return await _context.Set<ServerImagesInformation>().FromSqlInterpolated($"EXEC GET_IMAGES {ImageId}").ToListAsync();
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
        //public async Task<List<ImageServerServiceResponse>> GetAllImages()
        //{
        //    var ImageId = new SqlParameter("ImageId", imageFileDetails.ImageId);
        //    return await _context.Set<ImageServerServiceResponse>().FromSqlInterpolated($"EXEC GET_IMAGE_QUALITY").ToListAsync();
        //}      
    }
}
