using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EClaimsEntities.Models;

namespace EClaimsRepository.Contracts
{
   public interface IDtPVCClaimFileUploadDraftRepository : IRepositoryBase<DtPVCClaimDraftFileUpload>
    {
        void CreateDtPVCClaimFileUploadDraft(DtPVCClaimDraftFileUpload dtPVCClaimFileUpload);
        void UpdateDtPVCClaimFileUploadDraft(DtPVCClaimDraftFileUpload dtPVCClaimFileUpload);
        void DeleteDtPVCClaimFileUploadDraft(DtPVCClaimDraftFileUpload dtPVCClaimFileUpload);
        Task<List<DtPVCClaimDraftFileUpload>> GetDtPVCClaimDraftAuditByIdAsync(long? eCID);
        Task<DtPVCClaimDraftFileUpload> GetDtPVCClaimDraftFileUploadByIdAsync(long? eFID);
    }
}
