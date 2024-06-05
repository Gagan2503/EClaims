using EClaimsEntities.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EClaimsRepository.Contracts
{
    public interface IMstMileageClaimAuditRepository : IRepositoryBase<MstMileageClaimAudit>
    {
        Task CreateMileageClaimAudit(MstMileageClaimAudit mstMileageClaimAudit);
        void UpdateMileageClaimAudit(MstMileageClaimAudit mstMileageClaimAudit);
        void DeleteMileageClaimAudit(MstMileageClaimAudit mstMileageClaimAudit);

        Task<List<MstMileageClaimAudit>> GetMstMileageClaimAuditByIdAsync(long? mCID);
    }
}
