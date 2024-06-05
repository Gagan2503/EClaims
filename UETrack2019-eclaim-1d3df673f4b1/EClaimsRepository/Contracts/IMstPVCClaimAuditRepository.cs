using EClaimsEntities.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EClaimsRepository.Contracts
{
    public interface IMstPVCClaimAuditRepository : IRepositoryBase<MstPVCClaimAudit>
    {
        Task CreatePVCClaimAudit(MstPVCClaimAudit mstPVCClaimAudit);
        void UpdatePVCClaimAudit(MstPVCClaimAudit mstPVCClaimAudit);
        void DeletePVCClaimAudit(MstPVCClaimAudit mstPVCClaimAudit);
        Task<List<MstPVCClaimAudit>> GetMstPVCClaimAuditByIdAsync(long? pVCCID);
    }
}
