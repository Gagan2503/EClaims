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
    public class DtHRPVCClaimRepository : RepositoryBase<DtHRPVCClaim>, IDtHRPVCClaimRepository
    {
        public DtHRPVCClaimRepository(RepositoryContext repositoryContext, IApplicationReadDbConnection readDbConnection, IApplicationWriteDbConnection writeDbConnection)
   : base(repositoryContext, writeDbConnection, readDbConnection)
        {
        }

        public void CreateDtHRPVCClaim(DtHRPVCClaim dtHRPVCClaim)
        {
            throw new NotImplementedException();
        }

        public void CreateDtPVCClaim(DtHRPVCClaim dtHRPVCClaim)
        {
            Create(dtHRPVCClaim);
        }

        public void DeleteDtHRPVCClaim(DtHRPVCClaim dtHRPVCClaim)
        {
            Delete(dtHRPVCClaim);
        }

        public async Task<IEnumerable<DtHRPVCClaim>> GetAllDtHRPVCClaimAsync()
        {
            {
                return await FindAll()
                .OrderBy(mc => mc.HRPVCCItemID)
                 .ToListAsync();
            }
        }

        public async Task<IEnumerable<DtHRPVCClaim>> GetAllDtHRPVCClaimWithDetailsAsync()
        {
            return await FindAll()
            .ToListAsync();
        }

        public Task<DtHRPVCClaim> GetDtHRPVCClaimByIdAsync(int? hRPVCCItemID)
        {
            throw new NotImplementedException();
        }

        public async Task<List<DtHRPVCClaim>> GetDtHRPVCClaimByIdAsync(long? hRPVCCID)
        {
            return await FindByCondition(mstEC => mstEC.HRPVCCID.Equals(hRPVCCID))
                .ToListAsync();
        }

        public void UpdateDtHRPVCClaim(DtHRPVCClaim dtHRPVCClaim)
        {
            Update(dtHRPVCClaim);
        }
    }
}
