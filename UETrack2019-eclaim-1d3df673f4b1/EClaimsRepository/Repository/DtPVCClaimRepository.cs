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
    public class DtPVCClaimRepository : RepositoryBase<DtPVCClaim>, IDtPVCClaimRepository
    {
        public DtPVCClaimRepository(RepositoryContext repositoryContext, IApplicationReadDbConnection readDbConnection, IApplicationWriteDbConnection writeDbConnection)
   : base(repositoryContext, writeDbConnection, readDbConnection)
        {
        }

        public void CreateDtPVCClaim(DtPVCClaim dtPVCClaim)
        {
            Create(dtPVCClaim);
        }

        public void DeleteDtPVCClaim(DtPVCClaim dtPVCClaim)
        {
            Delete(dtPVCClaim);
        }

        public async Task<IEnumerable<DtPVCClaim>> GetAllDtPVCClaimAsync()
        {
            {
                return await FindAll()
                .OrderBy(mc => mc.PVCCItemID)
                 .ToListAsync();
            }
        }

        public async Task<IEnumerable<DtPVCClaim>> GetAllDtPVCClaimWithDetailsAsync()
        {
            return await FindAll()
            .Include(mf => mf.MstExpenseCategory)
            .ToListAsync();
        }

        public Task<DtPVCClaim> GetDtPVCClaimByIdAsync(int? pVCCItemID)
        {
            throw new NotImplementedException();
        }

        public async Task<List<DtPVCClaim>> GetDtPVCClaimByIdAsync(long? pVCCID)
        {
            return await FindByCondition(mstEC => mstEC.PVCCID.Equals(pVCCID))
                .Include(mex => mex.MstExpenseCategory)
                .ToListAsync();
        }

        public void UpdateDtPVCClaim(DtPVCClaim dtPVCClaim)
        {
            Update(dtPVCClaim);
        }
    }
}
