using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MiniApp3.Core.Dtos.StoredProcedureDto;
using MiniApp3.Core.Entities;
using MiniApp3.Core.Repositories;
using MiniApp3.Data.Context;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;
using ImageFile = MiniApp3.Core.Entities.ImageFile;
using ImageFileDetail = MiniApp3.Core.Entities.ImageFileDetail;

namespace MiniApp3.Data.Repositories.GenericRepositories
{
    public class Repository<TEntity> : IRepository<TEntity> where TEntity : class
    {
        private readonly DbContext _context;
        private readonly DbSet<TEntity> _dbSet;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public Repository(IServiceScopeFactory serviceScopeFactory)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _context = _serviceScopeFactory.CreateScope().ServiceProvider.GetRequiredService<AppDbContext>();
            _dbSet = _context.Set<TEntity>();
        }

        public async Task AddAsync(TEntity entity)
        {
            await _dbSet.AddAsync(entity);
            await _context.SaveChangesAsync();
        }

        public IQueryable<TEntity> GetAll()
        {
            return _dbSet.AsNoTracking().AsQueryable();
        }

        public async Task<IEnumerable<TEntity>> GetAllAsync()
        {
            return await _dbSet.AsNoTracking().ToListAsync();
        }

        public async Task<TEntity> GetByIdAsync(int id)
        {
            var entity = await _dbSet.FindAsync(id);
            if (entity != null)
            {
                _context.Entry(entity).State = EntityState.Detached;
            }
            return entity;
        }

        public void Remove(TEntity entity)
        {
            _dbSet.Remove(entity);
        }

        public async Task<TEntity> Update(TEntity entity)
        {
            _context.Entry(entity).State = EntityState.Modified;
            return entity;
        }

        public IQueryable<TEntity> Where(Expression<Func<TEntity, bool>> predicate)
        {
            return _dbSet.Where(predicate);
        }

        public void Commit()
        {
            _context.SaveChanges();
        }

        public async Task CommitAsync()
        {
            await _context.SaveChangesAsync();
        }
        public async Task<int> CountAsync()
        {
            return await _dbSet.CountAsync();
        }

        public async Task<List<TEntity>> ReadPhotoInformation()
        {
            return await _dbSet.FromSqlRaw("EXEC GET_ALL_IMAGES").ToListAsync();
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
        public async Task<List<string>> ReadPhotoInfoDirectlyFromDatabase()
        {
            try
            {
                var database = _context.Database;
                var dbConnection = (SqlConnection)database.GetDbConnection();

                var command = new SqlCommand($"SELECT [Folder] + '/' + CAST([Id] AS nvarchar(36)) FROM [ADVANCEPHOTODB].[dbo].[ImageFile]", dbConnection);
                //command.Parameters.Add(new SqlParameter("@id", id));
                dbConnection.Open();
                var reader = await command.ExecuteReaderAsync();
                var result = new List<string>();
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        result.Add(reader.GetString(0));
                    }
                }
                reader.Close();
                return result;
            }
            catch (Exception)
            {
                // Log
            }
            return null;
        }
        public async Task<Stream> ReadPhotoDirectlyFromDatabase(string id, string content)
        {
            try
            {
                var database = _context.Database;
                var dbConnection = (SqlConnection)database.GetDbConnection();

                var command = new SqlCommand($"SELECT {content} FROM [ADVANCEPHOTODB].[dbo].[ImageData] WHERE Id = @id", dbConnection);
                command.Parameters.Add(new SqlParameter("@id", id));
                dbConnection.Open();
                var reader = await command.ExecuteReaderAsync();
                Stream result = null;
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        result = reader.GetStream(0);
                    }
                }
                reader.Close();
                return result;
            }
            catch (Exception)
            {
                // Log
            }
            return null;
        }
    }
}
