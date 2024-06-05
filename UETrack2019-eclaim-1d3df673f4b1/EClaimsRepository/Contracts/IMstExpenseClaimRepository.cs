using EClaimsEntities.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EClaimsRepository.Contracts
{
   public interface IMstExpenseClaimRepository : IRepositoryBase<MstExpenseClaim>
    {
        Task<IEnumerable<MstExpenseClaim>> GetAllExpenseClaimAsync();
        Task<IEnumerable<CustomClaim>> GetAllExpenseClaimWithDetailsAsync(string expenseID, int userID, int facilityID, int statusID, string fromDate, string toDate);
        Task<IEnumerable<MstExpenseClaim>> GetAllExpenseClaimWithDetailsByFacilityIDAsync(int userID, int facilityID);
        Task<IEnumerable<MstExpenseClaim>> GetAllExpenseClaimForExportToBankAsync(string id, int facilityID, string fromDate, string toDate);
        Task<IEnumerable<MstExpenseClaim>> GetAllExpenseClaimForAPExportAsync(string id, int facilityID, string fromDate, string toDate);
        //Task<MstExpenseClaim> GetExpenseClaimByIdAsync(int? ECID);

        // IEnumerable<MstExpenseClaim> ExpenseClaimByClaimType(int? claimTypeId);

        void CreateExpenseClaim(MstExpenseClaim mstExpenseClaim);
        void UpdateExpenseClaim(MstExpenseClaim mstExpenseClaim);
        void DeleteExpenseClaim(MstExpenseClaim mstExpenseClaim);
        Task<MstExpenseClaim> GetExpenseClaimByIdAsync(long? eCID);
        
        Task<string> GetVerifierAsync(long? eCID);
        Task<string> GetApproverAsync(long? eCID);
        Task<string> GetUserApproverAsync(long? eCID);
        Task<string> GetHODApproverAsync(long? eCID);
        bool ExistsApproval(string ID, int ApprovedStatus, string UserID, string Screen);
        string GetApproval(string ID, int ApprovedStatus, string UserID, string Screen);
        Task<int> UpdateMstExpenseClaimStatus(long? ECID, int? approvalStatus, int? approvedBy, DateTime? approvedDate, string reason, string verifierIDs, string approverIDs, string userApproverIDs, string hodApprover, bool isAlternateApprover, int? financeStartDay);
        Task<int> SaveSummary(int eCID, List<DtExpenseClaimSummary> dtExpenseClaimSummaries, MstExpenseClaimAudit mstExpenseClaimAudit);
        Task<int> SaveItems(MstExpenseClaim mstExpenseClaim, List<DtExpenseClaim> dtExpenseClaims, List<DtExpenseClaimSummary> dtExpenseClaimSummaries);
        DataTable InsertExcel(int userID, int createdBy);
    }
}
