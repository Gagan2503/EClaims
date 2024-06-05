using EClaimsEntities.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EClaimsRepository.Contracts
{
   public interface IMstHRPVCClaimRepository : IRepositoryBase<MstHRPVCClaim>
    {
        Task<IEnumerable<MstHRPVCClaim>> GetAllHRPVCClaimAsync();
        Task<IEnumerable<MstHRPVCClaim>> GetAllHRPVCClaimWithDetailsAsync();
        Task<List<CustomHRPVCClaim>> GetAllHRPVCClaimWithDetailsByFacilityIDForAPExportAsync(string claimID, int facilityID, string fromDate, string toDate);
        Task<List<CustomHRPVCClaim>> GetAllHRPVCClaimWithDetailsByFacilityIDAsync(int userID, int facilityID, int statusID, string fromDate, string toDate);
        Task<List<CustomHRPVCClaim>> GetAllHRPVCClaimWithDraftDetailsByFacilityIDAsync(int userID, int facilityID, int statusID, string fromDate, string toDate);
        //Task<MstHRPVCClaim> GetHRPVCClaimByIdAsync(int? ECID);

        // IEnumerable<MstHRPVCClaim> HRPVCClaimByClaimType(int? claimTypeId);

        void CreateHRPVCClaim(MstHRPVCClaim mstHRPVCClaim);
        void UpdateHRPVCClaim(MstHRPVCClaim mstHRPVCClaim);
        void DeleteHRPVCClaim(MstHRPVCClaim mstHRPVCClaim);
        Task<MstHRPVCClaim> GetHRPVCClaimByIdAsync(long? hRPVCCID);

        Task<string> GetVerifierAsync(long? hRPVCCID);
        Task<string> GetApproverAsync(long? hRPVCCID);
        Task<string> GetUserApproverAsync(long? hRPVCCID);
        Task<string> GetHODApproverAsync(long? hRPVCCID);
        bool ExistsApproval(string ID, int ApprovedStatus, string UserID, string Screen);
        string GetApproval(string ID, int ApprovedStatus, string UserID, string Screen);
        Task<int> UpdateMstHRPVCClaimStatus(long? HRPVCCID, int? approvalStatus, int? approvedBy, DateTime? approvedDate, string reason, string verifierIDs, string approverIDs, string userApproverIDs, string hodApprover, bool isAlternateApprover, int? financeStartDay);
        Task<int> SaveSummary(int hRPVCCID, List<DtHRPVCClaimSummary> dtHRPVCClaimSummaries, MstHRPVCClaimAudit mstHRPVCClaimAudit);
        Task<int> SaveItems(MstHRPVCClaim mstHRPVCClaim, List<DtHRPVCClaim> dtHRPVCClaims, List<DtHRPVCClaimSummary> dtHRPVCClaimSummaries);
        Task<int> SaveItemsDraft(MstHRPVCClaimDraft mstHRPVCClaim, List<DtHRPVCClaimDraft> dtHRPVCClaims, List<DtHRPVCClaimSummaryDraft> dtHRPVCClaimSummaries);
        DataTable InsertExcel(int userID, int createdBy);
    }
}
