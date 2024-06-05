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
    public class MstCostStructureRepository : RepositoryBase<MstCostStructure>, IMstCostStructureRepository
    {
        public MstCostStructureRepository(RepositoryContext repositoryContext, IApplicationReadDbConnection readDbConnection, IApplicationWriteDbConnection writeDbConnection)
   : base(repositoryContext, writeDbConnection, readDbConnection)
        {
        }

        public async Task<IEnumerable<MstCostStructure>> GetAllCostStructureAsync()
        {
            return await FindAll()
           .OrderBy(cs => cs.CostStructure)
           .ToListAsync();
        }
    }
}
