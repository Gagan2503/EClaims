using EClaimsEntities.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EClaimsRepository.Contracts
{
    public interface IDtHRPVCClaimFileUploadRepositoryDraft : IRepositoryBase<DtHRPVCClaimFileUploadDraft>
    {
        void CreateDtHRPVCClaimFileUpload(DtHRPVCClaimFileUploadDraft dtHRPVCClaimFileUpload);
        Task<List<DtHRPVCClaimFileUploadDraft>> GetDtHRPVCClaimAuditByIdAsync(long? hRPVCCID);
    }
}
