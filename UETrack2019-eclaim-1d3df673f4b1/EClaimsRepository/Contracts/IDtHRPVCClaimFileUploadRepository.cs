using EClaimsEntities.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EClaimsRepository.Contracts
{
    public interface IDtHRPVCClaimFileUploadRepository : IRepositoryBase<DtHRPVCClaimFileUpload>
    {
        void CreateDtHRPVCClaimFileUpload(DtHRPVCClaimFileUpload dtHRPVCClaimFileUpload);
        void UpdateDtHRPVCClaimFileUpload(DtHRPVCClaimFileUpload dtHRPVCClaimFileUpload);
        void DeleteDtHRPVCClaimFileUpload(DtHRPVCClaimFileUpload dtHRPVCClaimFileUpload);
        Task<List<DtHRPVCClaimFileUpload>> GetDtHRPVCClaimAuditByIdAsync(long? hRPVCCID);
        Task<DtHRPVCClaimFileUpload> GetDtHRPVCClaimFileUploadByIdAsync(long? hRPVCCID);
    }
}
