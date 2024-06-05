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
    public class MstPVGClaimAuditRepository : RepositoryBase<MstPVGClaimAudit>, IMstPVGClaimAuditRepository
    {
        public MstPVGClaimAuditRepository(RepositoryContext repositoryContext, IApplicationReadDbConnection readDbConnection, IApplicationWriteDbConnection writeDbConnection)
   : base(repositoryContext, writeDbConnection, readDbConnection)
        {

        }

        public async Task CreatePVGClaimAudit(MstPVGClaimAudit mstPVGClaimAudit)
        {
            Create(mstPVGClaimAudit);
        }

        public void DeletePVGClaimAudit(MstPVGClaimAudit mstPVGClaimAudit)
        {
            Delete(mstPVGClaimAudit);
        }

        public async Task<List<MstPVGClaimAudit>> GetMstPVGClaimAuditByIdAsync(long? pVGCID)
        {
            return FindByCondition(dtMC => dtMC.PVGCID.Equals(pVGCID)).ToList();
        }

        public void UpdatePVGClaimAudit(MstPVGClaimAudit mstPVGClaimAudit)
        {
            Update(mstPVGClaimAudit);
        }
    }
}
