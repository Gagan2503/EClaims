using EClaimsEntities.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace EClaimsRepository.Contracts
{
    public interface IMstMileageClaimDratRepository : IRepositoryBase<MstMileageClaimDraft>
    {
        Task<IEnumerable<MstMileageClaimDraft>> GetAllMileageClaimDraftAsync();
        Task<IEnumerable<CustomClaim>> GetAllMileageClaimDraftWithDetailsAsync(int userID, int facilityID, int statusID, string fromDate, string toDate);
        Task<IEnumerable<MstMileageClaimDraft>> GetAllMileageClaimDraftWithDetailsByFacilityIDAsync(int userID, int facilityID);
        Task<MstMileageClaimDraft> GetMileageClaimDraftByIdAsync(long? MCID);

        // IEnumerable<MstMileageClaim> MileageClaimByClaimType(int? claimTypeId);

        void CreateMileageClaimDraft(MstMileageClaimDraft mstMileageClaim);
        void UpdateMileageClaimDraft(MstMileageClaimDraft mstMileageClaim);
        void DeleteMileageClaimDraft(MstMileageClaimDraft mstMileageClaim);
        Task<int> UpdateMstMileageClaimDraftStatus(long? MCID, int? approvalStatus, int? approvedBy, DateTime? approvedDate, string reason, string verifierIDs, string approverIDs, string userApproverIDs, string hodApprover);
        Task<int> SaveSummaryDraft(int mCID, List<DtMileageClaimSummaryDraft> dtMileageClaimSummaries, MstMileageClaimAudit mstMileageClaimAudit);
        Task<int> SaveDraftItems(MstMileageClaimDraft mstMileageClaim, List<DtMileageClaimDraft> dtMileageClaims, List<DtMileageClaimSummaryDraft> dtMileageClaimSummaries);
    }
}
