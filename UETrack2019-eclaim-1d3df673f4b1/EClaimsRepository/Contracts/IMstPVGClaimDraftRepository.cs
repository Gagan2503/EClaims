using EClaimsEntities.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EClaimsRepository.Contracts
{
   public interface IMstPVGClaimDraftRepository : IRepositoryBase<MstPVGClaimDraft>
    {
        Task<IEnumerable<MstPVGClaimDraft>> GetAllPVGClaimDraftAsync();
        Task<IEnumerable<CustomClaim>> GetAllPVGClaimDraftWithDetailsAsync(int userID, int facilityID, int statusID, string fromDate, string toDate);
      //  Task<IEnumerable<MstPVGClaimDraft>> GetAllPVGClaimForExportToBankAsync(string id, int facilityID, string fromDate, string toDate);
      //  Task<IEnumerable<MstPVGClaimDraft>> GetAllPVGClaimForAPExportAsync(string id, int facilityID, string fromDate, string toDate);
        Task<IEnumerable<MstPVGClaimDraft>> GetAllPVGClaimDraftWithDetailsByFacilityIDAsync(int userID, int facilityID);
        //Task<MstPVGClaim> GetPVGClaimByIdAsync(int? ECID);

        // IEnumerable<MstPVGClaim> PVGClaimByClaimType(int? claimTypeId);

        void CreatePVGClaimDraft(MstPVGClaimDraft mstPVGClaimDraft);
        void UpdatePVGClaimDraft(MstPVGClaimDraft mstPVGClaimDraft);
        void DeletePVGClaimDraft(MstPVGClaimDraft mstPVGClaimDraft);
        Task<MstPVGClaimDraft> GetPVGClaimDraftByIdAsync(long? pVGCID);
        Task<string> GetVerifierAsync(long? pVGCID);
        Task<string> GetApproverAsync(long? pVGCID);
        Task<string> GetUserApproverAsync(long? pVGCID);
        Task<string> GetHODApproverAsync(long? pVGCID);
        bool ExistsApproval(string ID, int ApprovedStatus, string UserID, string Screen);
        string GetApproval(string ID, int ApprovedStatus, string UserID, string Screen);
        Task<int> UpdateMstPVGClaimDraftStatus(long? PVGCID, int? approvalStatus, int? approvedBy, DateTime? approvedDate, string reason, string verifierIDs, string approverIDs, string userApproverIDs, string hodApprover, bool isAlternateApprover, int? financeStartDay);
        Task<int> SaveSummaryDraft(int pVGCID, List<DtPVGClaimSummaryDraft> dtPVGClaimSummariesDraft, MstPVGClaimAudit mstPVGClaimAudit);
        Task<int> SaveItemsDraft(MstPVGClaimDraft mstPVGClaimDraft, List<DtPVGClaimDraft> dtPVGClaimsDraft, List<DtPVGClaimSummaryDraft> dtPVGClaimSummariesDraft);
        //DataTable InsertExcel(int userID);
    }
}
