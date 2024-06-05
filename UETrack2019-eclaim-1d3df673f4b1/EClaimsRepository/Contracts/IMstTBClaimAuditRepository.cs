using EClaimsEntities.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EClaimsRepository.Contracts
{
    public interface IMstTBClaimAuditRepository : IRepositoryBase<MstTBClaimAudit>
    {
        Task CreateTBClaimAudit(MstTBClaimAudit mstTBClaimAudit);
        void UpdateTBClaimAudit(MstTBClaimAudit mstTBClaimAudit);
        void DeleteTBClaimAudit(MstTBClaimAudit mstTBClaimAudit);
        Task<List<MstTBClaimAudit>> GetMstTBClaimAuditByIdAsync(long? tCID);
    }
}
