using EClaimsEntities.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EClaimsRepository.Contracts
{
   public interface IMstHRPVGClaimRepository : IRepositoryBase<MstHRPVGClaim>
    {
        Task<IEnumerable<MstHRPVGClaim>> GetAllHRPVGClaimAsync();
        Task<IEnumerable<MstHRPVGClaim>> GetAllHRPVGClaimWithDetailsAsync();
        Task<List<CustomHRPVGClaim>> GetAllHRPVGClaimWithDetailsByFacilityIDForExportToBankAsync(string claimID, int facilityID, string fromDate, string toDate);
        Task<List<CustomHRPVGClaim>> GetAllHRPVGClaimWithDetailsByFacilityIDForAPExportAsync(string claimID, int facilityID, string fromDate, string toDate);
        Task<List<CustomHRPVGClaim>> GetAllHRPVGClaimWithDetailsByFacilityIDAsync(int userID, int facilityID, int statusID, string fromDate, string toDate);
        //Task<MstHRPVGClaim> GetHRPVGClaimByIdAsync(int? ECID);

        // IEnumerable<MstHRPVGClaim> HRPVGClaimByClaimType(int? claimTypeId);

        void CreateHRPVGClaim(MstHRPVGClaim mstHRPVGClaim);
        void UpdateHRPVGClaim(MstHRPVGClaim mstHRPVGClaim);
        void DeleteHRPVGClaim(MstHRPVGClaim mstHRPVGClaim);
        Task<MstHRPVGClaim> GetHRPVGClaimByIdAsync(long? hRPVGCID);

        Task<string> GetVerifierAsync(long? hRPVGCID);
        Task<string> GetApproverAsync(long? hRPVGCID);
        Task<string> GetUserApproverAsync(long? hRPVGCID);
        Task<string> GetHODApproverAsync(long? hRPVGCID);
        bool ExistsApproval(string ID, int ApprovedStatus, string UserID, string Screen);
        string GetApproval(string ID, int ApprovedStatus, string UserID, string Screen);
        Task<int> UpdateMstHRPVGClaimStatus(long? HRPVGCID, int? approvalStatus, int? approvedBy, DateTime? approvedDate, string reason, string verifierIDs, string approverIDs, string userApproverIDs, string hodApprover, bool isAlternateApprover,int? financeStartDay);
        Task<int> SaveSummary(int hRPVGCID, List<DtHRPVGClaimSummary> dtHRPVGClaimSummaries, MstHRPVGClaimAudit mstHRPVGClaimAudit);
        Task<int> SaveItems(MstHRPVGClaim mstHRPVGClaim, List<DtHRPVGClaim> dtHRPVGClaims, List<DtHRPVGClaimSummary> dtHRPVGClaimSummaries);
        DataTable InsertExcel(int userID, int createdBy);
    }
}
