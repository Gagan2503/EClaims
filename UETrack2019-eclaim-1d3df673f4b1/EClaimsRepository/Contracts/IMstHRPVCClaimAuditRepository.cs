using EClaimsEntities.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EClaimsRepository.Contracts
{
    public interface IMstHRPVCClaimAuditRepository : IRepositoryBase<MstHRPVCClaimAudit>
    {
        Task CreateHRPVCClaimAudit(MstHRPVCClaimAudit mstHRPVCClaimAudit);
        void UpdateHRPVCClaimAudit(MstHRPVCClaimAudit mstHRPVCClaimAudit);
        void DeleteHRPVCClaimAudit(MstHRPVCClaimAudit mstHRPVCClaimAudit);
        Task<List<MstHRPVCClaimAudit>> GetMstHRPVCClaimAuditByIdAsync(long? hRPVCCID);
    }
}
