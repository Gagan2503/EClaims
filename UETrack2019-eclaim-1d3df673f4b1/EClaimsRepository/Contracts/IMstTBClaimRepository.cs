using EClaimsEntities.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EClaimsRepository.Contracts
{
   public interface IMstTBClaimRepository : IRepositoryBase<MstTBClaim>
    {
        Task<IEnumerable<MstTBClaim>> GetAllTBClaimAsync();
        Task<IEnumerable<CustomClaim>> GetAllTBClaimWithDetailsAsync(int userID, int facilityID, int statusID, string fromDate, string toDate);
        Task<IEnumerable<MstTBClaim>> GetAllTBClaimForExportToBankAsync(string id, int facilityID, string fromDate, string toDate);
        Task<IEnumerable<MstTBClaim>> GetAllTBClaimForAPExportAsync(string id, int facilityID, string fromDate, string toDate);
        Task<IEnumerable<MstTBClaim>> GetAllTBClaimWithDetailsByFacilityIDAsync(int userID, int facilityID);

        Task<MstTBClaim> GetTBClaimByIdAsync(long? tCID);

        // IEnumerable<MstTBClaim> TBClaimByClaimType(int? claimTypeId);
        Task<string> GetVerifierAsync(long? tBCID);
        Task<string> GetApproverAsync(long? tBCID);
        Task<string> GetUserApproverAsync(long? tBCID);
        Task<string> GetHODApproverAsync(long? tBCID);
        bool ExistsApproval(string ID, int ApprovedStatus, string UserID, string Screen);
        string GetApproverVerifier(string ID, int ApprovedStatus, string UserID, string Screen);
        Task<int> UpdateMstTBClaimStatus(long? TBCID, int? approvalStatus, int? approvedBy, DateTime? approvedDate, string reason, string verifierIDs, string approverIDs, string userApproverIDs, string hodApprover, bool isAlternateApprover, int? financeStartDay);

        Task<int> SaveSummary(int tBCID, List<DtTBClaimSummary> dtTBClaimSummaries, MstTBClaimAudit mstTBClaimAudit);
        Task<int> SaveItems(MstTBClaim mstTBClaim, List<DtTBClaim> dtTBClaims, List<DtTBClaimSummary> dtTBClaimSummaries);

        void CreateTBClaim(MstTBClaim mstTBClaim);
        void UpdateTBClaim(MstTBClaim mstTBClaim);
        void DeleteTBClaim(MstTBClaim mstTBClaim);
        DataTable InsertExcel(int userID, int createdBy);
    }
}
