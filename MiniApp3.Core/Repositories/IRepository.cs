using MiniApp3.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace MiniApp3.Core.Repositories
{
    public interface IRepository<TEntity> where TEntity : class
    {
        Task<TEntity> GetByIdAsync(int id);
        IQueryable<TEntity> GetAll();
        Task<IEnumerable<TEntity>> GetAllAsync();
        IQueryable<TEntity> Where(Expression<Func<TEntity, bool>> predicate);
        Task AddAsync(TEntity entity);
        void Remove(TEntity entity);
        Task<TEntity> Update(TEntity entity);
        public void Commit();
        public Task CommitAsync();
        public Task<int> CountAsync();
        public Task<List<object>> ReadPhotoInfoDirectlyFromDatabase();
        public Task<Stream> ReadPhotoDirectlyFromDatabase(string id, string content);
    }
}
