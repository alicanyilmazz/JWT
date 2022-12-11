using SharedLibrary.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace AuthServer.Core.Services
{
    public interface IService<TEntity,TDto> where TEntity : class where TDto: class
    {
        Task<Response<TDto>> GetByIdAsync(int id);
        Task<Response<IQueryable<TDto>>> GetAllAsync();
        Task<Response<IEnumerable<TEntity>>> GetAllWithoutQueryAsync();
        IQueryable<TEntity> WhereWithQuery(Expression<Func<TEntity, bool>> predicate);
        IEnumerable<TEntity> Where(Expression<Func<TEntity, bool>> predicate);
        Task AddAsync(TEntity entity);
        void Remove(TEntity entity);
        TEntity Update(TEntity entity);
    }
}
