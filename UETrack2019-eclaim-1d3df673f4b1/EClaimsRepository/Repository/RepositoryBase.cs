using EClaimsEntities;
using EClaimsRepository.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace EClaimsRepository.Repository
{
    public abstract class RepositoryBase<T> : IRepositoryBase<T> where T : class
    {
        protected RepositoryContext RepositoryContext { get; set; }
        protected IApplicationWriteDbConnection WriteDbConnection { get; set; }
        protected IApplicationReadDbConnection ReadDbConnection { get; set; }

        public RepositoryBase(RepositoryContext repositoryContext,IApplicationWriteDbConnection writeDbConnection,IApplicationReadDbConnection readDbConnection)
        {
            this.RepositoryContext = repositoryContext;
            this.WriteDbConnection = writeDbConnection;
            this.ReadDbConnection = readDbConnection;
        }

        public IQueryable<T> FindAll()
        {
            return this.RepositoryContext.Set<T>().AsNoTracking();
        }

        public IQueryable<T> FindByCondition(Expression<Func<T, bool>> expression)
        {
            return this.RepositoryContext.Set<T>().Where(expression).AsNoTracking();
        }

        public void Create(T entity)
        {
            this.RepositoryContext.Set<T>().Add(entity);
        }

        public T CreateAndReturnEntity(T entity)
        {
            var objEntity =  this.RepositoryContext.Set<T>().Add(entity);
            //this.RepositoryContext.SaveChangesAsync();
            return objEntity.Entity;
        }

        public void Update(T entity)
        {
            this.RepositoryContext.Set<T>().Update(entity);
        }

        public void CreateRange(List<T> entities)
        {
            this.RepositoryContext.Set<List<T>>().AddRange(entities.ToList());
        }

        public void UpdateRange(List<T> entities)
        {
            this.RepositoryContext.Set<List<T>>().UpdateRange(entities);
        }

        public void Delete(T entity)
        {
            this.RepositoryContext.Set<T>().Remove(entity);
        }

        public void DeleteRange(IEnumerable<T> entities)
        {
            this.RepositoryContext.Set<IEnumerable<T>>().RemoveRange(entities);
        }
    }
}
