using EClaimsEntities.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EClaimsRepository.Contracts
{
   public interface IMstPVCClaimRepository : IRepositoryBase<MstPVCClaim>
    {
        Task<IEnumerable<MstPVCClaim>> GetAllPVCClaimAsync();
        Task<List<CustomClaim>> GetAllPVCClaimWithDetailsAsync(int userID, int facilityID, int statusID, string fromDate, string toDate);
        Task<IEnumerable<MstPVCClaim>> GetAllPVCClaimForExportToBankAsync(string id, int facilityID, string fromDate, string toDate);
        Task<IEnumerable<MstPVCClaim>> GetAllPVCClaimForAPExportAsync(string id, int facilityID, string fromDate, string toDate);
        Task<IEnumerable<MstPVCClaim>> GetAllPVCClaimWithDetailsByFacilityIDAsync(int userID, int facilityID);
        //Task<MstPVCClaim> GetPVCClaimByIdAsync(int? ECID);

        // IEnumerable<MstPVCClaim> PVCClaimByClaimType(int? claimTypeId);

        void CreatePVCClaim(MstPVCClaim mstPVCClaim);
        void UpdatePVCClaim(MstPVCClaim mstPVCClaim);
        void DeletePVCClaim(MstPVCClaim mstPVCClaim);
        Task<MstPVCClaim> GetPVCClaimByIdAsync(long? pVCCID);

        Task<string> GetVerifierAsync(long? pVCCID);
        Task<string> GetApproverAsync(long? pVCCID);
        Task<string> GetUserApproverAsync(long? pVCCID);
        Task<string> GetHODApproverAsync(long? pVCCID);
        bool ExistsApproval(string ID, int ApprovedStatus, string UserID, string Screen);
        string GetApproval(string ID, int ApprovedStatus, string UserID, string Screen);
        Task<int> UpdateMstPVCClaimStatus(long? PVCCID, int? approvalStatus, int? approvedBy, DateTime? approvedDate, string reason, string verifierIDs, string approverIDs, string userApproverIDs, string hodApprover, bool isAlternateApprover, int? financeStartDay);
        Task<int> SaveSummary(int pVCCID, List<DtPVCClaimSummary> dtPVCClaimSummaries, MstPVCClaimAudit mstPVCClaimAudit);
        Task<int> SaveItems(MstPVCClaim mstPVCClaim, List<DtPVCClaim> dtPVCClaims, List<DtPVCClaimSummary> dtPVCClaimSummaries);
        DataTable InsertExcel(int userID, int createdBy);
    }
}
