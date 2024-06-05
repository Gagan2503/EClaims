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
    public class MstHRPVCClaimAuditRepository : RepositoryBase<MstHRPVCClaimAudit>, IMstHRPVCClaimAuditRepository
    {
        public MstHRPVCClaimAuditRepository(RepositoryContext repositoryContext, IApplicationReadDbConnection readDbConnection, IApplicationWriteDbConnection writeDbConnection)
   : base(repositoryContext, writeDbConnection, readDbConnection)
        {

        }

        public async Task CreateHRPVCClaimAudit(MstHRPVCClaimAudit mstHRPVCClaimAudit)
        {
            Create(mstHRPVCClaimAudit);
        }

        public void DeleteHRPVCClaimAudit(MstHRPVCClaimAudit mstHRPVCClaimAudit)
        {
            Delete(mstHRPVCClaimAudit);
        }

        public async Task<List<MstHRPVCClaimAudit>> GetMstHRPVCClaimAuditByIdAsync(long? hRPVCCID)
        {
            return FindByCondition(dtMC => dtMC.HRPVCCID.Equals(hRPVCCID)).ToList();
        }

        public void UpdateHRPVCClaimAudit(MstHRPVCClaimAudit mstHRPVCClaimAudit)
        {
            Update(mstHRPVCClaimAudit);
        }
    }
}
