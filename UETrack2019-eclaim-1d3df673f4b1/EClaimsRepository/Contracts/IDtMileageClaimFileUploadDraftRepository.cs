using EClaimsEntities.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EClaimsRepository.Contracts
{
    public interface IDtMileageClaimFileUploadDraftRepository : IRepositoryBase<DtMileageClaimFileUploadDraft>
    {
        void CreateDtMileageClaimDraft(DtMileageClaimFileUploadDraft dtMileageClaimFileUpload);
        void UpdateDtMileageClaimDraft(DtMileageClaimFileUploadDraft dtMileageClaimFileUpload);
        void DeleteDtMileageClaimDraft(DtMileageClaimFileUploadDraft dtMileageClaimFileUpload);
        Task<List<DtMileageClaimFileUploadDraft>> GetDtMileageClaimDraftAuditByIdAsync(long? mCID);
        Task<DtMileageClaimFileUploadDraft> GetDtMileageClaimFileUploadDraftByIdAsync(long? eFID);
    }
}
