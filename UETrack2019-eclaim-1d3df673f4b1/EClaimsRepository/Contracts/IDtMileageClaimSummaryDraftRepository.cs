using EClaimsEntities.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EClaimsRepository.Contracts
{
    public interface IDtMileageClaimSummaryDraftRepository : IRepositoryBase<DtMileageClaimSummaryDraft>
    {
        void CreateDtMileageClaimSummaryDraft(DtMileageClaimSummaryDraft dtMileageClaim);
        void UpdateDtMileageClaimSummaryDraft(DtMileageClaimSummaryDraft dtMileageClaim);
        void DeleteDtMileageClaimSummaryDraft(DtMileageClaimSummaryDraft dtMileageClaim);
        Task<List<DtMileageClaimSummaryDraft>> GetDtMileageClaimSummaryDraftByIdAsync(long? mCID);
    }

}
