using EClaimsEntities.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EClaimsRepository.Contracts
{
   public interface IMstHRPVGClaimDraftRepository : IRepositoryBase<MstHRPVGClaimDraft>
    {
        Task<IEnumerable<MstHRPVGClaimDraft>> GetAllHRPVGClaimAsync();
        Task<IEnumerable<MstHRPVGClaimDraft>> GetAllHRPVGClaimWithDetailsAsync();
        Task<List<CustomHRPVGClaim>> GetAllHRPVGClaimWithDetailsByFacilityIDForExportToBankAsync(string claimID, int facilityID, string fromDate, string toDate);
        Task<List<CustomHRPVGClaim>> GetAllHRPVGClaimWithDetailsByFacilityIDForAPExportAsync(string claimID, int facilityID, string fromDate, string toDate);
        Task<List<CustomHRPVGClaim>> GetAllHRPVGClaimWithDraftDetailsByFacilityIDAsync(int userID, int facilityID, int statusID, string fromDate, string toDate);
        //Task<MstHRPVGClaim> GetHRPVGClaimByIdAsync(int? ECID);

        // IEnumerable<MstHRPVGClaim> HRPVGClaimByClaimType(int? claimTypeId);

        void CreateHRPVGClaim(MstHRPVGClaimDraft mstHRPVGClaim);
        void UpdateHRPVGClaim(MstHRPVGClaimDraft mstHRPVGClaim);
        void DeleteHRPVGClaim(MstHRPVGClaimDraft mstHRPVGClaim);
        Task<MstHRPVGClaimDraft> GetHRPVGClaimByIdAsync(long? hRPVGCID);

        Task<string> GetVerifierAsync(long? hRPVGCID);
        Task<string> GetApproverAsync(long? hRPVGCID);
        Task<string> GetUserApproverAsync(long? hRPVGCID);
        Task<string> GetHODApproverAsync(long? hRPVGCID);
        bool ExistsApproval(string ID, int ApprovedStatus, string UserID, string Screen);
        string GetApproval(string ID, int ApprovedStatus, string UserID, string Screen);
        Task<int> UpdateMstHRPVGClaimStatus(long? HRPVGCID, int? approvalStatus, int? approvedBy, DateTime? approvedDate, string reason, string verifierIDs, string approverIDs, string userApproverIDs, string hodApprover, bool isAlternateApprover,int? financeStartDay);
        Task<int> SaveSummary(int hRPVGCID, List<DtHRPVGClaimSummary> dtHRPVGClaimSummaries, MstHRPVGClaimAudit mstHRPVGClaimAudit);
        Task<int> SaveItemsDraft(MstHRPVGClaimDraft mstHRPVGClaim, List<DtHRPVGClaimDraft> dtHRPVGClaims, List<DtHRPVGClaimDraftSummary> dtHRPVGClaimSummaries);
        DataTable InsertExcel(int userID);
    }
}
