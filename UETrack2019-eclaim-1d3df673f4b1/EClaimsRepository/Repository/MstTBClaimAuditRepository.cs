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
    class MstTBClaimAuditRepository : RepositoryBase<MstTBClaimAudit>, IMstTBClaimAuditRepository
    {
        public MstTBClaimAuditRepository(RepositoryContext repositoryContext, IApplicationReadDbConnection readDbConnection, IApplicationWriteDbConnection writeDbConnection)
         : base(repositoryContext, writeDbConnection, readDbConnection)
        {

        }

        public async Task CreateTBClaimAudit(MstTBClaimAudit mstTBClaimAudit)
        {
            Create(mstTBClaimAudit);
        }

        public void DeleteTBClaimAudit(MstTBClaimAudit mstTBClaimAudit)
        {
            Delete(mstTBClaimAudit);
        }

        public async Task<List<MstTBClaimAudit>> GetMstTBClaimAuditByIdAsync(long? tCID)
        {
            return FindByCondition(dtTB => dtTB.TBCID.Equals(tCID)).ToList();
        }

        public void UpdateTBClaimAudit(MstTBClaimAudit mstTBClaimAudit)
        {
            Update(mstTBClaimAudit);
        }
    }
}
