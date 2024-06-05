using EClaimsEntities;
using EClaimsEntities.Models;
using EClaimsRepository.Contracts;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace EClaimsRepository.Repository
{
    public class MstScreensRepository : RepositoryBase<MstScreens>, IMstScreensRepository
    {
        public MstScreensRepository(RepositoryContext repositoryContext, IApplicationReadDbConnection readDbConnection, IApplicationWriteDbConnection writeDbConnection)
   : base(repositoryContext, writeDbConnection, readDbConnection)
        {
        }
        public async Task<IEnumerable<MstScreens>> GetAllScreensAsync()
        {
            return await FindAll()
            .OrderBy(mst => mst.ScreenName)
            .ToListAsync();
        }

        public IEnumerable<MstScreens> GetAllScreensByModuleAsync(string moduleName)
        {
            return FindByCondition(a => a.ModuleName.Equals(moduleName)).ToList();
        }
    }
}
