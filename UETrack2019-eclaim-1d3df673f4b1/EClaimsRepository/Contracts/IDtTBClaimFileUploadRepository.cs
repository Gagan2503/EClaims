using EClaimsEntities.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EClaimsRepository.Contracts
{
    public interface IDtTBClaimFileUploadRepository : IRepositoryBase<DtTBClaimFileUpload>
    {
        void CreateDtTBClaimFileUpload(DtTBClaimFileUpload dtTBClaimFileUpload);
        void UpdateDtTBClaimFileUpload(DtTBClaimFileUpload dtTBClaimFileUpload);
        void DeleteDtTBClaimFileUpload(DtTBClaimFileUpload dtTBClaimFileUpload);
        Task<List<DtTBClaimFileUpload>> GetDtTBClaimAuditByIdAsync(long? tBCID);
        Task<DtTBClaimFileUpload> GetDtTBClaimFileUploadByIdAsync(long? eFID);
    }
}
