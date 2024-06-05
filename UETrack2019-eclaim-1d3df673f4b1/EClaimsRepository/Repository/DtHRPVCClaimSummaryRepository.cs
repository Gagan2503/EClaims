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
    public class DtHRPVCClaimSummaryRepository : RepositoryBase<DtHRPVCClaimSummary>, IDtHRPVCClaimSummaryRepository
    {
        public DtHRPVCClaimSummaryRepository(RepositoryContext repositoryContext, IApplicationReadDbConnection readDbConnection, IApplicationWriteDbConnection writeDbConnection)
   : base(repositoryContext, writeDbConnection, readDbConnection)
        {
        }

        public void CreateDtHRPVCClaimSummary(DtHRPVCClaimSummary dtHRPVCClaimSummary)
        {
            Create(dtHRPVCClaimSummary);
        }

        public void UpdateDtHRPVCClaimSummary(DtHRPVCClaimSummary dtHRPVCClaimSummary)
        {
            Update(dtHRPVCClaimSummary);
        }

        public void DeleteDtHRPVCClaimSummary(DtHRPVCClaimSummary dtHRPVCClaimSummary)
        {
            Delete(dtHRPVCClaimSummary);
        }

        public async Task<List<DtHRPVCClaimSummary>> GetDtHRPVCClaimSummaryByIdAsync(long? hRPVCCID)
        {
            return await FindByCondition(mstEC => mstEC.HRPVCCID.Equals(hRPVCCID))
               .ToListAsync();
        }
    }
}
