using EClaimsEntities.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EClaimsRepository.Contracts
{
    public interface IDtMileageClaimFileUploadRepository : IRepositoryBase<DtMileageClaimFileUpload>
    {
        void CreateDtMileageClaim(DtMileageClaimFileUpload dtMileageClaimFileUpload);
        void UpdateDtMileageClaim(DtMileageClaimFileUpload dtMileageClaimFileUpload);
        void DeleteDtMileageClaim(DtMileageClaimFileUpload dtMileageClaimFileUpload);
        Task<List<DtMileageClaimFileUpload>> GetDtMileageClaimAuditByIdAsync(long? mCID);
        Task<DtMileageClaimFileUpload> GetDtMileageClaimFileUploadByIdAsync(long? eFID);
    }
}
