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
    public class DtTBClaimSummaryDraftRepository : RepositoryBase<DtTBClaimSummaryDraft>, IDtTBClaimSummaryDraftRepository
    {
        public DtTBClaimSummaryDraftRepository(RepositoryContext repositoryContext, IApplicationReadDbConnection readDbConnection, IApplicationWriteDbConnection writeDbConnection)
   : base(repositoryContext, writeDbConnection, readDbConnection)
        {
        }
        public void CreateDtTBClaimSummaryDraft(DtTBClaimSummaryDraft dtTBClaimSummary)
        {
            Create(dtTBClaimSummary);
        }

        public void UpdateDtTBClaimSummaryDraft(DtTBClaimSummaryDraft dtTBClaimSummary)
        {
            Update(dtTBClaimSummary);
        }

        public void DeleteDtTBClaimSummaryDraft(DtTBClaimSummaryDraft dtTBClaimSummary)
        {
            Delete(dtTBClaimSummary);
        }

        public async Task<List<DtTBClaimSummaryDraft>> GetDtTBClaimSummaryDraftByIdAsync(long? tBCID)
        {
            return await FindByCondition(mstEC => mstEC.TBCID.Equals(tBCID))
               .ToListAsync();
        }
    }
}
