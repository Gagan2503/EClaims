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
    public class DtMileageClaimFileUploadRepository : RepositoryBase<DtMileageClaimFileUpload>, IDtMileageClaimFileUploadRepository
    {
        public DtMileageClaimFileUploadRepository(RepositoryContext repositoryContext, IApplicationReadDbConnection readDbConnection, IApplicationWriteDbConnection writeDbConnection)
   : base(repositoryContext, writeDbConnection, readDbConnection)
        {
        }

        public void CreateDtMileageClaim(DtMileageClaimFileUpload dtMileageClaimFileUpload)
        {
            Create(dtMileageClaimFileUpload);
        }

        public void DeleteDtMileageClaim(DtMileageClaimFileUpload dtMileageClaimFileUpload)
        {
            Delete(dtMileageClaimFileUpload);
        }

        public void UpdateDtMileageClaim(DtMileageClaimFileUpload dtMileageClaimFileUpload)
        {
            Update(dtMileageClaimFileUpload);
        }

        public async Task<List<DtMileageClaimFileUpload>> GetDtMileageClaimAuditByIdAsync(long? mCID)
        {
            return FindByCondition(dtMC => dtMC.MCID.Equals(mCID)).ToList();
        }

        public async Task<DtMileageClaimFileUpload> GetDtMileageClaimFileUploadByIdAsync(long? eFID)
        {
            return FindByCondition(dtMC => dtMC.FileID.Equals(eFID)).FirstOrDefault();
        }
    }
}
