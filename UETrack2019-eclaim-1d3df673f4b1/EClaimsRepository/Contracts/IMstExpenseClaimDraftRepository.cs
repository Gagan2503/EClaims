using EClaimsEntities.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EClaimsRepository.Contracts
{
    public interface IMstExpenseClaimDraftRepository : IRepositoryBase<MstExpenseClaimDraft>
    {
        Task<IEnumerable<MstExpenseClaimDraft>> GetAllExpenseClaimDraftsAsync();
        Task<IEnumerable<CustomClaim>> GetAllExpenseClaimDraftsWithDetailsAsync(string expenseID, int userID, int facilityID, int statusID, string fromDate, string toDate);
        Task<IEnumerable<MstExpenseClaimDraft>> GetAllExpenseClaimDraftsWithDetailsByFacilityIDAsync(int userID, int facilityID);

        void CreateExpenseClaimDraft(MstExpenseClaimDraft mstExpenseClaim);
        void UpdateExpenseClaimDraft(MstExpenseClaimDraft mstExpenseClaim);
        void DeleteExpenseClaimDraft(MstExpenseClaimDraft mstExpenseClaim);
        Task<MstExpenseClaimDraft> GetExpenseClaimDraftByIdAsync(long? eCID);
        
        Task<int> SaveDraftSummary(int eCID, List<DtExpenseClaimSummaryDraft> dtExpenseClaimSummaries, MstExpenseClaimAudit mstExpenseClaimAudit);
        Task<int> SaveDraftItems(MstExpenseClaimDraft mstExpenseClaim, List<DtExpenseClaimDraft> dtExpenseClaims, List<DtExpenseClaimSummaryDraft> dtExpenseClaimSummaries);
        DataTable InsertExcel(int userID);
    }
}
