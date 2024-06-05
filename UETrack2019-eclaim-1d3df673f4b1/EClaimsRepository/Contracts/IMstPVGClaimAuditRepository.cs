using EClaimsEntities.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EClaimsRepository.Contracts
{
    public interface IMstPVGClaimAuditRepository : IRepositoryBase<MstPVGClaimAudit>
    {
        Task CreatePVGClaimAudit(MstPVGClaimAudit mstPVGClaimAudit);
        void UpdatePVGClaimAudit(MstPVGClaimAudit mstPVGClaimAudit);
        void DeletePVGClaimAudit(MstPVGClaimAudit mstPVGClaimAudit);
        Task<List<MstPVGClaimAudit>> GetMstPVGClaimAuditByIdAsync(long? pVGCID);
    }
}
