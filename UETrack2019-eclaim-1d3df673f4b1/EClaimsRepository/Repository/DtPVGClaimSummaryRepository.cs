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
    public class DtPVGClaimSummaryRepository : RepositoryBase<DtPVGClaimSummary>, IDtPVGClaimSummaryRepository
    {
        public DtPVGClaimSummaryRepository(RepositoryContext repositoryContext, IApplicationReadDbConnection readDbConnection, IApplicationWriteDbConnection writeDbConnection)
   : base(repositoryContext, writeDbConnection, readDbConnection)
        {
        }

        public void CreateDtPVGClaimSummary(DtPVGClaimSummary dtPVGClaimSummary)
        {
            Create(dtPVGClaimSummary);
        }

        public void UpdateDtPVGClaimSummary(DtPVGClaimSummary dtPVGClaimSummary)
        {
            Update(dtPVGClaimSummary);
        }

        public void DeleteDtPVGClaimSummary(DtPVGClaimSummary dtPVGClaimSummary)
        {
            Delete(dtPVGClaimSummary);
        }

        public async Task<List<DtPVGClaimSummary>> GetDtPVGClaimSummaryByIdAsync(long? hRPVGCID)
        {
            return await FindByCondition(mstEC => mstEC.PVGCID.Equals(hRPVGCID))
               .ToListAsync();
        }
    }
}
