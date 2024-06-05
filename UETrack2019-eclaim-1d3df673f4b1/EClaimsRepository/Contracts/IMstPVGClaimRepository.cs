using EClaimsEntities.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EClaimsRepository.Contracts
{
   public interface IMstPVGClaimRepository : IRepositoryBase<MstPVGClaim>
    {
        Task<IEnumerable<MstPVGClaim>> GetAllPVGClaimAsync();
        Task<IEnumerable<CustomClaim>> GetAllPVGClaimWithDetailsAsync(int userID, int facilityID, int statusID, string fromDate, string toDate);
        Task<IEnumerable<MstPVGClaim>> GetAllPVGClaimForExportToBankAsync(string id, int facilityID, string fromDate, string toDate);
        Task<IEnumerable<MstPVGClaim>> GetAllPVGClaimForAPExportAsync(string id, int facilityID, string fromDate, string toDate);
        Task<IEnumerable<MstPVGClaim>> GetAllPVGClaimWithDetailsByFacilityIDAsync(int userID, int facilityID);
        //Task<MstPVGClaim> GetPVGClaimByIdAsync(int? ECID);

        // IEnumerable<MstPVGClaim> PVGClaimByClaimType(int? claimTypeId);

        void CreatePVGClaim(MstPVGClaim mstPVGClaim);
        void UpdatePVGClaim(MstPVGClaim mstPVGClaim);
        void DeletePVGClaim(MstPVGClaim mstPVGClaim);
        Task<MstPVGClaim> GetPVGClaimByIdAsync(long? pVGCID);

        Task<string> GetVerifierAsync(long? pVGCID);
        Task<string> GetApproverAsync(long? pVGCID);
        Task<string> GetUserApproverAsync(long? pVGCID);
        Task<string> GetHODApproverAsync(long? pVGCID);
        bool ExistsApproval(string ID, int ApprovedStatus, string UserID, string Screen);
        string GetApproval(string ID, int ApprovedStatus, string UserID, string Screen);
        Task<int> UpdateMstPVGClaimStatus(long? PVGCID, int? approvalStatus, int? approvedBy, DateTime? approvedDate, string reason, string verifierIDs, string approverIDs, string userApproverIDs, string hodApprover, bool isAlternateApprover, int? financeStartDay);
        Task<int> SaveSummary(int pVGCID, List<DtPVGClaimSummary> dtPVGClaimSummaries, MstPVGClaimAudit mstPVGClaimAudit);
        Task<int> SaveItems(MstPVGClaim mstPVGClaim, List<DtPVGClaim> dtPVGClaims, List<DtPVGClaimSummary> dtPVGClaimSummaries);
        DataTable InsertExcel(int userID,int createdBy);
    }
}
