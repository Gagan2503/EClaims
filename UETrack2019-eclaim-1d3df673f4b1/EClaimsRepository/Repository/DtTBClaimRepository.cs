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
    public class DtTBClaimRepository : RepositoryBase<DtTBClaim>, IDtTBClaimRepository
    {
        public DtTBClaimRepository(RepositoryContext repositoryContext, IApplicationReadDbConnection readDbConnection, IApplicationWriteDbConnection writeDbConnection)
   : base(repositoryContext, writeDbConnection, readDbConnection)
        {
        }

        public void CreateDtTBClaim(DtTBClaim dtTBClaim)
        {
            Create(dtTBClaim);
        }

        public void DeleteDtTBClaim(DtTBClaim dtTBClaim)
        {
            Delete(dtTBClaim);
        }

        public async Task<IEnumerable<DtTBClaim>> GetAllDtTBClaimAsync()
        {
            {
                return await FindAll()
                .OrderBy(mc => mc.TBCItemID)
                 .ToListAsync();
            }
        }

        public async Task<IEnumerable<DtTBClaim>> GetAllDtTBClaimWithDetailsAsync()
        {
            return await FindAll()
            .Include(mf => mf.MstExpenseCategory)
            .ToListAsync();
        }

        public async Task<DtTBClaim> GetDtTBClaimByIdAsync(int? tBCItemID)
        {
            return await FindByCondition(mstTBC => mstTBC.TBCID.Equals(tBCItemID))
        .FirstOrDefaultAsync();
        }

        public async Task<List<DtTBClaim>> GetDtTBClaimByIdAsync(long? tBCID)
        {
            return await FindByCondition(mstEC => mstEC.TBCID.Equals(tBCID))
                .Include(mex => mex.MstExpenseCategory)
                .ToListAsync();
        }

        public void UpdateDtTBClaim(DtTBClaim dtTBClaim)
        {
            Update(dtTBClaim);
        }
    }
}
