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
    class DtExpenseClaimSummaryDraftRepository : RepositoryBase<DtExpenseClaimSummaryDraft>, IDtExpenseClaimSummaryDraftRepository
    {
        public DtExpenseClaimSummaryDraftRepository(RepositoryContext repositoryContext, IApplicationReadDbConnection readDbConnection, IApplicationWriteDbConnection writeDbConnection)
   : base(repositoryContext, writeDbConnection, readDbConnection)
        {
        }
        public void CreateDtExpenseClaimSummaryDraft(DtExpenseClaimSummaryDraft dtExpenseClaimSummary)
        {
            Create(dtExpenseClaimSummary);
        }

        public void UpdateDtExpenseClaimSummaryDraft(DtExpenseClaimSummaryDraft dtExpenseClaimSummary)
        {
            Update(dtExpenseClaimSummary);
        }

        public void DeleteDtExpenseClaimSummaryDraft(DtExpenseClaimSummaryDraft dtExpenseClaimSummary)
        {
            Delete(dtExpenseClaimSummary);
        }

        public async Task<List<DtExpenseClaimSummaryDraft>> GetDtExpenseClaimSummaryDraftByIdAsync(long? eCID)
        {
            return await FindByCondition(mstEC => mstEC.ECID.Equals(eCID))
               .ToListAsync();
        }
    }
}
