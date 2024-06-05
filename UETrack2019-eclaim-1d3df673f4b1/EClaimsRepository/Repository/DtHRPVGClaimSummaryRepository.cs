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
    public class DtHRPVGClaimSummaryRepository : RepositoryBase<DtHRPVGClaimSummary>, IDtHRPVGClaimSummaryRepository
    {
        public DtHRPVGClaimSummaryRepository(RepositoryContext repositoryContext, IApplicationReadDbConnection readDbConnection, IApplicationWriteDbConnection writeDbConnection)
   : base(repositoryContext, writeDbConnection, readDbConnection)
        {
        }

        public void CreateDtHRPVGClaimSummary(DtHRPVGClaimSummary dtHRPVGClaimSummary)
        {
            Create(dtHRPVGClaimSummary);
        }

        public void UpdateDtHRPVGClaimSummary(DtHRPVGClaimSummary dtHRPVGClaimSummary)
        {
            Update(dtHRPVGClaimSummary);
        }

        public void DeleteDtHRPVGClaimSummary(DtHRPVGClaimSummary dtHRPVGClaimSummary)
        {
            Delete(dtHRPVGClaimSummary);
        }

        public async Task<List<DtHRPVGClaimSummary>> GetDtHRPVGClaimSummaryByIdAsync(long? hRPVGCID)
        {
            return await FindByCondition(mstEC => mstEC.HRPVGCID.Equals(hRPVGCID))
               .ToListAsync();
        }
    }
}
