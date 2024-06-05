using EClaimsEntities;
using EClaimsEntities.Models;
using EClaimsRepository.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EClaimsRepository.Repository
{
    public class MstExpenseClaimAuditRepository : RepositoryBase<MstExpenseClaimAudit>, IMstExpenseClaimAuditRepository
    {
        public MstExpenseClaimAuditRepository(RepositoryContext repositoryContext, IApplicationReadDbConnection readDbConnection, IApplicationWriteDbConnection writeDbConnection)
   : base(repositoryContext, writeDbConnection, readDbConnection)
        {

        }

        public async Task CreateExpenseClaimAudit(MstExpenseClaimAudit mstExpenseClaimAudit)
        {
            Create(mstExpenseClaimAudit);
        }

        public void DeleteExpenseClaimAudit(MstExpenseClaimAudit mstExpenseClaimAudit)
        {
            Delete(mstExpenseClaimAudit);
        }

        public void UpdateExpenseClaimAudit(MstExpenseClaimAudit mstExpenseClaimAudit)
        {
            Update(mstExpenseClaimAudit);
        }

        public async Task<List<MstExpenseClaimAudit>> GetMstExpenseClaimAuditByIdAsync(long? eCID)
        {
            return FindByCondition(dtMC => dtMC.ECID.Equals(eCID)).ToList();
        }

    }
}
