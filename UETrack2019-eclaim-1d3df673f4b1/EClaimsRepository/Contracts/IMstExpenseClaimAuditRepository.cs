using EClaimsEntities.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EClaimsRepository.Contracts
{
    public interface IMstExpenseClaimAuditRepository : IRepositoryBase<MstExpenseClaimAudit>
    {
        Task CreateExpenseClaimAudit(MstExpenseClaimAudit mstExpenseClaimAudit);
        void UpdateExpenseClaimAudit(MstExpenseClaimAudit mstExpenseClaimAudit);
        void DeleteExpenseClaimAudit(MstExpenseClaimAudit mstExpenseClaimAudit);
        Task<List<MstExpenseClaimAudit>> GetMstExpenseClaimAuditByIdAsync(long? eCID);
    }
}
