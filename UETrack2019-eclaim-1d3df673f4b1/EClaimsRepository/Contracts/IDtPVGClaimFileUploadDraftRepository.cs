using EClaimsEntities.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EClaimsRepository.Contracts
{
   public interface IDtPVGClaimFileUploadDraftRepository : IRepositoryBase<DtPVGClaimFileUploadDraft>
    {
        void CreateDtPVGClaimFileUploadDraft(DtPVGClaimFileUploadDraft dtPVGClaimFileUploadDraft);
        void UpdateDtPVGClaimFileUploadDraft(DtPVGClaimFileUploadDraft dtPVGClaimFileUploadDraft);
        void DeleteDtPVGClaimFileUploadDraft(DtPVGClaimFileUploadDraft dtPVGClaimFileUploadDraft);
        Task<List<DtPVGClaimFileUploadDraft>> GetDtPVGClaimDraftAuditByIdAsync(long? pVGCID);
        Task<DtPVGClaimFileUploadDraft> GetDtPVGClaimFileUploadDraftByIdAsync(long? pVGFID);
    }
}
