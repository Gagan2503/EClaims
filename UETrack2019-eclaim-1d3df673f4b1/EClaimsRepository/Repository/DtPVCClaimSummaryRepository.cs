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
    public class DtPVCClaimSummaryRepository : RepositoryBase<DtPVCClaimSummary>, IDtPVCClaimSummaryRepository
    {
        public DtPVCClaimSummaryRepository(RepositoryContext repositoryContext, IApplicationReadDbConnection readDbConnection, IApplicationWriteDbConnection writeDbConnection)
   : base(repositoryContext, writeDbConnection, readDbConnection)
        {
        }

        public void CreateDtPVCClaimSummary(DtPVCClaimSummary dtPVCClaimSummary)
        {
            Create(dtPVCClaimSummary);
        }

        public void UpdateDtPVCClaimSummary(DtPVCClaimSummary dtPVCClaimSummary)
        {
            Update(dtPVCClaimSummary);
        }

        public void DeleteDtPVCClaimSummary(DtPVCClaimSummary dtPVCClaimSummary)
        {
            Delete(dtPVCClaimSummary);
        }

        public async Task<List<DtPVCClaimSummary>> GetDtPVCClaimSummaryByIdAsync(long? hRPVCCID)
        {
            return await FindByCondition(mstEC => mstEC.PVCCID.Equals(hRPVCCID))
               .ToListAsync();
        }
    }
}
