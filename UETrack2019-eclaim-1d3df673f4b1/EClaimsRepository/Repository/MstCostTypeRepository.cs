using EClaimsEntities;
using EClaimsEntities.Models;
using EClaimsRepository.Contracts;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EClaimsRepository.Repository
{
    public class MstCostTypeRepository : RepositoryBase<MstCostType>, IMstCostTypeRepository
    {
        public MstCostTypeRepository(RepositoryContext repositoryContext, IApplicationReadDbConnection readDbConnection, IApplicationWriteDbConnection writeDbConnection)
   : base(repositoryContext, writeDbConnection, readDbConnection)
        {
        }

        public async Task<IEnumerable<MstCostType>> GetAllCostTypeAsync()
        {
            return await FindAll()
          .OrderBy(ct => ct.CostType)
          .ToListAsync();
        }
    }
}
