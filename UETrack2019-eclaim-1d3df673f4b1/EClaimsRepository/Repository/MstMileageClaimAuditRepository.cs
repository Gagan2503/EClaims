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
    public class MstMileageClaimAuditRepository : RepositoryBase<MstMileageClaimAudit>, IMstMileageClaimAuditRepository
    {
        public MstMileageClaimAuditRepository(RepositoryContext repositoryContext, IApplicationReadDbConnection readDbConnection, IApplicationWriteDbConnection writeDbConnection)
  : base(repositoryContext, writeDbConnection, readDbConnection)
        {
        }

        public async Task CreateMileageClaimAudit(MstMileageClaimAudit mstMileageClaimAudit)
        {
            Create(mstMileageClaimAudit);
        }

        public void DeleteMileageClaimAudit(MstMileageClaimAudit mstMileageClaimAudit)
        {
            Delete(mstMileageClaimAudit);
        }

        public async Task<List<MstMileageClaimAudit>> GetMstMileageClaimAuditByIdAsync(long? mCID)
        {
            return FindByCondition(dtMC => dtMC.MCID.Equals(mCID)).ToList();
        }


        public void UpdateMileageClaimAudit(MstMileageClaimAudit mstMileageClaimAudit)
        {
            Update(mstMileageClaimAudit);
        }
    }
}
