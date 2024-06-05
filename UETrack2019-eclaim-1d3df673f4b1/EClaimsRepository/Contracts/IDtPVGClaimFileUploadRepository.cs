using EClaimsEntities.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EClaimsRepository.Contracts 
{
    public interface IDtPVGClaimFileUploadRepository : IRepositoryBase<DtPVGClaimFileUpload>
    {
        void CreateDtPVGClaimFileUpload(DtPVGClaimFileUpload dtPVGClaimFileUpload);
        void UpdateDtPVGClaimFileUpload(DtPVGClaimFileUpload dtPVGClaimFileUpload);
        void DeleteDtPVGClaimFileUpload(DtPVGClaimFileUpload dtPVGClaimFileUpload);
        Task<List<DtPVGClaimFileUpload>> GetDtPVGClaimAuditByIdAsync(long? pVGCID);
        Task<DtPVGClaimFileUpload> GetDtPVGClaimFileUploadByIdAsync(long? pVGFID);
    }
}
