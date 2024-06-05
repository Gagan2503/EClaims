using EClaimsEntities.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EClaimsRepository.Contracts
{
    public interface IMstHRPVGClaimAuditRepository : IRepositoryBase<MstHRPVGClaimAudit>
    {
        Task CreateHRPVGClaimAudit(MstHRPVGClaimAudit mstHRPVGClaimAudit);
        void UpdateHRPVGClaimAudit(MstHRPVGClaimAudit mstHRPVGClaimAudit);
        void DeleteHRPVGClaimAudit(MstHRPVGClaimAudit mstHRPVGClaimAudit);
        Task<List<MstHRPVGClaimAudit>> GetMstHRPVGClaimAuditByIdAsync(long? hRPVGCID);
    }
}
