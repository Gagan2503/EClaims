using EClaimsEntities.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EClaimsRepository.Contracts
{
    public interface IDtMileageClaimDraftRepository : IRepositoryBase<DtMileageClaimDraft>
    {
        Task<IEnumerable<DtMileageClaimDraft>> GetAllDtMileageClaimDraftAsync();
        Task<IEnumerable<DtMileageClaimDraft>> GetAllDtMileageClaimDraftWithDetailsAsync();
        //Task<DtMileageClaim> GetDtMileageClaimByIdAsync(int? MCItemID);
        Task<List<DtMileageClaimDraft>> GetDtMileageClaimDraftByIdAsync(long? mCID);
        // IEnumerable<MstMileageClaim> MileageClaimByClaimType(int? claimTypeId);

        void CreateDtMileageClaimDraft(DtMileageClaimDraft dtMileageClaim);
        void UpdateDtMileageClaimDraft(DtMileageClaimDraft dtMileageClaim);
        void DeleteDtMileageClaimDraft(DtMileageClaimDraft dtMileageClaim);
    }
}
