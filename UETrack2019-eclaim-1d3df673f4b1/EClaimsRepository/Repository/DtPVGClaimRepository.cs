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
    public class DtPVGClaimRepository : RepositoryBase<DtPVGClaim>, IDtPVGClaimRepository
    {
        public DtPVGClaimRepository(RepositoryContext repositoryContext, IApplicationReadDbConnection readDbConnection, IApplicationWriteDbConnection writeDbConnection)
   : base(repositoryContext, writeDbConnection, readDbConnection)
        {
        }

        public void CreateDtPVGClaim(DtPVGClaim dtPVGClaim)
        {
            Create(dtPVGClaim);
        }

        public void DeleteDtPVGClaim(DtPVGClaim dtPVGClaim)
        {
            Delete(dtPVGClaim);
        }

        public async Task<IEnumerable<DtPVGClaim>> GetAllDtPVGClaimAsync()
        {
            {
                return await FindAll()
                .OrderBy(mc => mc.PVGCItemID)
                 .ToListAsync();
            }
        }

        public async Task<IEnumerable<DtPVGClaim>> GetAllDtPVGClaimWithDetailsAsync()
        {
            return await FindAll()
            .Include(mf => mf.MstExpenseCategory)
            .ToListAsync();
        }

        public async Task<DtPVGClaim> GetDtPVGClaimByPVGCItemIDAsync(long? pVGCItemID)
        {
            return await FindByCondition(mstPVGC => mstPVGC.PVGCItemID.Equals(pVGCItemID))
        .FirstOrDefaultAsync();
        }

        public async Task<List<DtPVGClaim>> GetDtPVGClaimByIdAsync(long? pVGCID)
        {
            return await FindByCondition(mstEC => mstEC.PVGCID.Equals(pVGCID))
                .Include(mex => mex.MstExpenseCategory)
                .ToListAsync();
        }

        public void UpdateDtPVGClaim(DtPVGClaim dtPVGClaim)
        {
            Update(dtPVGClaim);
        }
    }
}
