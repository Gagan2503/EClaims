using EClaimsEntities.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EClaimsRepository.Contracts
{
    public interface IDtTBClaimFileUploadDraftRepository : IRepositoryBase<DtTBClaimFileUploadDraft>
    {
        void CreateDtTBClaimFileUploadDraft(DtTBClaimFileUploadDraft dtTBClaimFileUpload);
        void UpdateDtTBClaimFileUploadDraft(DtTBClaimFileUploadDraft dtTBClaimFileUpload);
        void DeleteDtTBClaimFileUploadDraft(DtTBClaimFileUploadDraft dtTBClaimFileUpload);
        Task<List<DtTBClaimFileUploadDraft>> GetDtTBClaimDraftAuditByIdAsync(long? tBCID);

        Task<DtTBClaimFileUploadDraft> GetDtTBClaimFileUploadDraftByIdAsync(long? eFID);
    }
}
