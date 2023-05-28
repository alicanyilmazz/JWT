using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MiniApp3.Core.Dtos.StoredProcedureDto;
using MiniApp3.Core.Entities;
using MiniApp3.Core.Repositories;
using MiniApp3.Data.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniApp3.Data.Repositories.Repositories
{
    public class ImageQualityRepository : IImageQualityRepository
    {
        private readonly DbContext _context;
        private readonly DbSet<ImageQualityResult> _dbSet;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public ImageQualityRepository(IServiceScopeFactory serviceScopeFactory)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _context = _serviceScopeFactory.CreateScope().ServiceProvider.GetRequiredService<AppDbContext>();
            _dbSet = _context.Set<ImageQualityResult>();
        }

        public async Task<List<ImageQualityResult>> GetImageQualityConfigs()
        {
           return await _dbSet.FromSqlInterpolated($"EXEC GET_IMAGE_QUALITY").ToListAsync();
        }
    }
}
