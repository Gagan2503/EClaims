using EClaimsEntities.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EClaimsRepository.Contracts
{
    public interface IDtExpenseClaimSummaryDraftRepository : IRepositoryBase<DtExpenseClaimSummaryDraft>
    {
        void CreateDtExpenseClaimSummaryDraft(DtExpenseClaimSummaryDraft dtExpenseClaim);
        void UpdateDtExpenseClaimSummaryDraft(DtExpenseClaimSummaryDraft dtExpenseClaim);
        void DeleteDtExpenseClaimSummaryDraft(DtExpenseClaimSummaryDraft dtExpenseClaim);
        Task<List<DtExpenseClaimSummaryDraft>> GetDtExpenseClaimSummaryDraftByIdAsync(long? eCID);
    }
}
