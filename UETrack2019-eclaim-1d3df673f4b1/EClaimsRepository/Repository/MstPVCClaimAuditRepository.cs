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
    public class MstPVCClaimAuditRepository : RepositoryBase<MstPVCClaimAudit>, IMstPVCClaimAuditRepository
    {
        public MstPVCClaimAuditRepository(RepositoryContext repositoryContext, IApplicationReadDbConnection readDbConnection, IApplicationWriteDbConnection writeDbConnection)
   : base(repositoryContext, writeDbConnection, readDbConnection)
        {

        }

        public async Task CreatePVCClaimAudit(MstPVCClaimAudit mstPVCClaimAudit)
        {
            Create(mstPVCClaimAudit);
        }

        public void DeletePVCClaimAudit(MstPVCClaimAudit mstPVCClaimAudit)
        {
            Delete(mstPVCClaimAudit);
        }

        public async Task<List<MstPVCClaimAudit>> GetMstPVCClaimAuditByIdAsync(long? pVCCID)
        {
            return FindByCondition(dtMC => dtMC.PVCCID.Equals(pVCCID)).ToList();
        }

        public void UpdatePVCClaimAudit(MstPVCClaimAudit mstPVCClaimAudit)
        {
            Update(mstPVCClaimAudit);
        }
    }
}
