using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MiniApp3.Core.Entities;
using MiniApp3.Core.Repositories.StoredProcedureRepositories;
using MiniApp3.Data.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniApp3.Data.Repositories.StoredProcedureRepositories.Command
{
    public class StoredProcedureCommandRepository : IStoredProcedureCommandRepository
    {
        private readonly DbContext _context;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public StoredProcedureCommandRepository(IServiceScopeFactory serviceScopeFactory)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _context = _serviceScopeFactory.CreateScope().ServiceProvider.GetRequiredService<AppDbContext>();
        }

        public async Task SaveImageImageFile(ImageFile image)
        {
            var ImageId = new SqlParameter("ImageId", image.ImageId);
            var Folder = new SqlParameter("Folder", image.Folder);
            var Extension = new SqlParameter("Extension", image.Extension);
            await _context.Database.ExecuteSqlInterpolatedAsync($"EXEC [dbo].[IMAGE_FILE_INSERT] @ImageId={ImageId}, @Folder={Folder}, @Extension={Extension}");
        }
        public async Task SaveImageImageFileDetail(ImageFileDetail imageFileDetails)
        {
            var ImageId = new SqlParameter("ImageId", imageFileDetails.ImageId);
            var Type = new SqlParameter("Type", imageFileDetails.Type);
            var QualityRate = new SqlParameter("QualityRate", imageFileDetails.QualityRate);
            await _context.Database.ExecuteSqlInterpolatedAsync($"EXEC [dbo].[IMAGE_FILE_DETAIL_INSERT] @ImageId={ImageId}, @Type={Type}, @QualityRate={QualityRate}");
        }
    }
}
