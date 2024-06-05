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
    public class DtHRPVGClaimRepository : RepositoryBase<DtHRPVGClaim>, IDtHRPVGClaimRepository
    {
        public DtHRPVGClaimRepository(RepositoryContext repositoryContext, IApplicationReadDbConnection readDbConnection, IApplicationWriteDbConnection writeDbConnection)
  : base(repositoryContext, writeDbConnection, readDbConnection)
        {
        }

        public void CreateDtHRPVGClaim(DtHRPVGClaim dtHRPVGClaim)
        {
            throw new NotImplementedException();
        }

        public void CreateDtPVGClaim(DtHRPVGClaim dtHRPVGClaim)
        {
            Create(dtHRPVGClaim);
        }

        public void DeleteDtHRPVGClaim(DtHRPVGClaim dtHRPVGClaim)
        {
            Delete(dtHRPVGClaim);
        }

        public async Task<IEnumerable<DtHRPVGClaim>> GetAllDtHRPVGClaimAsync()
        {
            {
                return await FindAll()
                .OrderBy(mc => mc.HRPVGCItemID)
                 .ToListAsync();
            }
        }

        public async Task<IEnumerable<DtHRPVGClaim>> GetAllDtHRPVGClaimWithDetailsAsync()
        {
            return await FindAll()
            .ToListAsync();
        }

        public Task<DtHRPVGClaim> GetDtHRPVGClaimByIdAsync(int? hRPVGCItemID)
        {
            throw new NotImplementedException();
        }

        public async Task<DtHRPVGClaim> GetDtHRPVGClaimByHRPVGCItemIDAsync(long? hRPVGCItemID)
        {
            return await FindByCondition(mstHRPVGC => mstHRPVGC.HRPVGCItemID.Equals(hRPVGCItemID))
        .FirstOrDefaultAsync();
        }


        public async Task<List<DtHRPVGClaim>> GetDtHRPVGClaimByIdAsync(long? hRPVGCID)
        {
            return await FindByCondition(mstEC => mstEC.HRPVGCID.Equals(hRPVGCID))
                .ToListAsync();
        }
       

        public void UpdateDtHRPVGClaim(DtHRPVGClaim dtHRPVGClaim)
        {
            Update(dtHRPVGClaim);
        }
    }
}
