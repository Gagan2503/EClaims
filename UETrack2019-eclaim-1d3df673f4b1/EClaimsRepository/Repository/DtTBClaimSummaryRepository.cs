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
    public class DtTBClaimSummaryRepository : RepositoryBase<DtTBClaimSummary>, IDtTBClaimSummaryRepository
    {
        public DtTBClaimSummaryRepository(RepositoryContext repositoryContext, IApplicationReadDbConnection readDbConnection, IApplicationWriteDbConnection writeDbConnection)
   : base(repositoryContext, writeDbConnection, readDbConnection)
        {
        }
        public void CreateDtTBClaimSummary(DtTBClaimSummary dtTBClaimSummary)
        {
            Create(dtTBClaimSummary);
        }

        public void UpdateDtTBClaimSummary(DtTBClaimSummary dtTBClaimSummary)
        {
            Update(dtTBClaimSummary);
        }

        public void DeleteDtTBClaimSummary(DtTBClaimSummary dtTBClaimSummary)
        {
            Delete(dtTBClaimSummary);
        }

        public async Task<List<DtTBClaimSummary>> GetDtTBClaimSummaryByIdAsync(long? tBCID)
        {
            return await FindByCondition(mstEC => mstEC.TBCID.Equals(tBCID))
               .ToListAsync();
        }
    }
}
