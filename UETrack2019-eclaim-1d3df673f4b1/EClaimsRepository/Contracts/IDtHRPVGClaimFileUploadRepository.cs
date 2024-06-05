using EClaimsEntities.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EClaimsRepository.Contracts
{
    public interface IDtHRPVGClaimFileUploadRepository : IRepositoryBase<DtHRPVGClaimFileUpload>
    {
        void CreateDtHRPVGClaimFileUpload(DtHRPVGClaimFileUpload dtHRPVGClaimFileUpload);
        void UpdateDtHRPVGClaimFileUpload(DtHRPVGClaimFileUpload dtHRPVGClaimFileUpload);
        void DeleteDtHRPVGClaimFileUpload(DtHRPVGClaimFileUpload dtHRPVGClaimFileUpload);
        Task<List<DtHRPVGClaimFileUpload>> GetDtHRPVGClaimAuditByIdAsync(long? hRPVGCID);
        Task<DtHRPVGClaimFileUpload> GetDtHRPVGClaimFileUploadByIdAsync(long? hRPVGFID);
    }
}
