using EClaimsEntities.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EClaimsRepository.Contracts
{
    public interface IDtPVCClaimFileUploadRepository : IRepositoryBase<DtPVCClaimFileUpload>
    {
        void CreateDtPVCClaimFileUpload(DtPVCClaimFileUpload dtPVCClaimFileUpload);
        void UpdateDtPVCClaimFileUpload(DtPVCClaimFileUpload dtPVCClaimFileUpload);
        void DeleteDtPVCClaimFileUpload(DtPVCClaimFileUpload dtPVCClaimFileUpload);
        Task<List<DtPVCClaimFileUpload>> GetDtPVCClaimAuditByIdAsync(long? pVCCID);
        Task<DtPVCClaimFileUpload> GetDtPVCClaimFileUploadByIdAsync(long? pVCCID);
    }
}
