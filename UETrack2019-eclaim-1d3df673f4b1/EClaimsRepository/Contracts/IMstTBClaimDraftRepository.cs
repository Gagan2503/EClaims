using EClaimsEntities.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace EClaimsRepository.Contracts
{
    public interface IMstTBClaimDraftRepository : IRepositoryBase<MstTBClaimDraft>
    {
        Task<IEnumerable<MstTBClaimDraft>> GetAllTBClaimDraftAsync();
        Task<IEnumerable<CustomClaim>> GetAllTBClaimDraftWithDetailsAsync(int userID, int facilityID, int statusID, string fromDate, string toDate);
        Task<IEnumerable<MstTBClaimDraft>> GetAllTBClaimDraftWithDetailsByFacilityIDAsync(int userID, int facilityID);

        Task<MstTBClaimDraft> GetTBClaimDraftByIdAsync(long? tCID);

        Task<int> UpdateMstTBClaimDraftStatus(long? TBCID, int? approvalStatus, int? approvedBy, DateTime? approvedDate, string reason, string verifierIDs, string approverIDs, string userApproverIDs, string hodApprover);

        Task<int> SaveDraftSummary(int tBCID, List<DtTBClaimSummaryDraft> dtTBClaimSummaries, MstTBClaimAudit mstTBClaimAudit);
        Task<int> SaveDraftItems(MstTBClaimDraft mstTBClaim, List<DtTBClaimDraft> dtTBClaims, List<DtTBClaimSummaryDraft> dtTBClaimSummaries);

        void CreateTBClaimDraft(MstTBClaimDraft mstTBClaim);
        void UpdateTBClaimDraft(MstTBClaimDraft mstTBClaim);
        void DeleteTBClaimDraft(MstTBClaimDraft mstTBClaim);
    }
}
