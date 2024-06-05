using EClaimsEntities.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EClaimsRepository.Contracts
{
    public interface IMstMileageClaimRepository : IRepositoryBase<MstMileageClaim>
    {
        Task<IEnumerable<MstMileageClaim>> GetAllMileageClaimAsync();
        Task<IEnumerable<CustomClaim>> GetAllMileageClaimWithDetailsAsync(int userID, int facilityID, int statusID, string fromDate, string toDate);
        Task<IEnumerable<CustomClaim>> GetAllMileageClaimForAPExportAsync(string id, string claimType, int facilityID, int statusID, string fromDate, string toDate);
        Task<IEnumerable<CustomClaim>> GetAllMileageClaimForExportToBankAsync(string id, string claimType, int facilityID, int statusID, string fromDate, string toDate);
        Task<IEnumerable<CustomClaimReports>> GetAllUserClaimsReportAsync(int userID, string id, string claimType, int facilityID, int statusID, string fromDate, string toDate);
        Task<IEnumerable<CustomClaimReports>> GetAllUserPVCClaimsReportAsync(int userID, string id, string claimType, int facilityID, int statusID, string fromDate, string toDate);
        Task<IEnumerable<CustomClaimReports>> GetAllUserPVGClaimsReportAsync(string role, int userID, string id, string claimType, int facilityID, int statusID, string fromDate, string toDate);
        Task<IEnumerable<CustomClaimReports>> GetAllUserHRPVCClaimsReportAsync(int userID, string id, string claimType, int facilityID, int statusID, string fromDate, string toDate);
        Task<IEnumerable<CustomClaimReports>> GetAllUserHRPVGClaimsReportAsync(int userID, string id, string claimType, int facilityID, int statusID, string fromDate, string toDate);
        Task<IEnumerable<MstMileageClaim>> GetAllMileageClaimWithDetailsByFacilityIDAsync(int userID, int facilityID);
        Task<MstMileageClaim> GetMileageClaimByIdAsync(long? MCID);

        // IEnumerable<MstMileageClaim> MileageClaimByClaimType(int? claimTypeId);

        void CreateMileageClaim(MstMileageClaim mstMileageClaim);
        void UpdateMileageClaim(MstMileageClaim mstMileageClaim);
        void DeleteMileageClaim(MstMileageClaim mstMileageClaim);

        Task<string> GetVerifierAsync(long? mCID);
        Task<string> GetApproverAsync(long? mCID);
        Task<string> GetUserApproverAsync(long? mCID);
        Task<string> GetHODApproverAsync(long? mCID);
        bool ExistsApproval(string ID, int ApprovedStatus, string UserID, string Screen);
        string GetApproval(string ID, int ApprovedStatus, string UserID, string Screen);
        Task<int> UpdateMstMileageClaimStatus(long? MCID, int? approvalStatus, int? approvedBy, DateTime? approvedDate, string reason, string verifierIDs, string approverIDs, string userApproverIDs, string hodApprover, bool isAlternateApprover, int? financeStartDay);
        Task<int> SaveSummary(int mCID, List<DtMileageClaimSummary> dtMileageClaimSummaries, MstMileageClaimAudit mstMileageClaimAudit);
        Task<int> SaveItems(MstMileageClaim mstMileageClaim, List<DtMileageClaim> dtMileageClaims, List<DtMileageClaimSummary> dtMileageClaimSummaries);
        DataTable InsertExcel(int userID, int createdBy);
    }
}
