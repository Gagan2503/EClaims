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
    public class MstHRPVGClaimAuditRepository : RepositoryBase<MstHRPVGClaimAudit>, IMstHRPVGClaimAuditRepository
    {
        public MstHRPVGClaimAuditRepository(RepositoryContext repositoryContext, IApplicationReadDbConnection readDbConnection, IApplicationWriteDbConnection writeDbConnection)
   : base(repositoryContext, writeDbConnection, readDbConnection)
        {

        }

        public async Task CreateHRPVGClaimAudit(MstHRPVGClaimAudit mstHRPVGClaimAudit)
        {
            Create(mstHRPVGClaimAudit);
        }

        public void DeleteHRPVGClaimAudit(MstHRPVGClaimAudit mstHRPVGClaimAudit)
        {
            Delete(mstHRPVGClaimAudit);
        }

        public async Task<List<MstHRPVGClaimAudit>> GetMstHRPVGClaimAuditByIdAsync(long? hRPVGCID)
        {
            return FindByCondition(dtMC => dtMC.HRPVGCID.Equals(hRPVGCID)).ToList();
        }

        public void UpdateHRPVGClaimAudit(MstHRPVGClaimAudit mstHRPVGClaimAudit)
        {
            Update(mstHRPVGClaimAudit);
        }
    }
}
