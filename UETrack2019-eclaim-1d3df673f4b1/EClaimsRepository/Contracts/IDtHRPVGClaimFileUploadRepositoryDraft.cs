using EClaimsEntities.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EClaimsRepository.Contracts
{
    public interface IDtHRPVGClaimFileUploadRepositoryDraft : IRepositoryBase<DtHRPVGClaimFileUploadDraft>
    {
        void CreateDtHRPVGClaimFileUpload(DtHRPVGClaimFileUploadDraft dtHRPVGClaimFileUpload);
        void UpdateDtHRPVGClaimFileUpload(DtHRPVGClaimFileUploadDraft dtHRPVGClaimFileUpload);
        void DeleteDtHRPVGClaimFileUpload(DtHRPVGClaimFileUploadDraft dtHRPVGClaimFileUpload);
        Task<List<DtHRPVGClaimFileUploadDraft>> GetDtHRPVGClaimAuditByIdAsync(long? hRPVGCID);
        Task<DtHRPVGClaimFileUploadDraft> GetDtHRPVGClaimFileUploadByIdAsync(long? hRPVGFID);
    }
}
