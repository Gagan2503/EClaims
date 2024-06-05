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
    public class DtMileageClaimFileUploadDraftRepository : RepositoryBase<DtMileageClaimFileUploadDraft>, IDtMileageClaimFileUploadDraftRepository
    {
        public DtMileageClaimFileUploadDraftRepository(RepositoryContext repositoryContext, IApplicationReadDbConnection readDbConnection, IApplicationWriteDbConnection writeDbConnection)
   : base(repositoryContext, writeDbConnection, readDbConnection)
        {
        }

        public void CreateDtMileageClaimDraft(DtMileageClaimFileUploadDraft dtMileageClaimFileUpload)
        {
            Create(dtMileageClaimFileUpload);
        }

        public void DeleteDtMileageClaimDraft(DtMileageClaimFileUploadDraft dtMileageClaimFileUpload)
        {
            Delete(dtMileageClaimFileUpload);
        }

        public void UpdateDtMileageClaimDraft(DtMileageClaimFileUploadDraft dtMileageClaimFileUpload)
        {
            Update(dtMileageClaimFileUpload);
        }

        public async Task<List<DtMileageClaimFileUploadDraft>> GetDtMileageClaimDraftAuditByIdAsync(long? mCID)
        {
            return FindByCondition(dtMC => dtMC.MCID.Equals(mCID)).ToList();
        }

        public async Task<DtMileageClaimFileUploadDraft> GetDtMileageClaimFileUploadDraftByIdAsync(long? eFID)
        {
            return FindByCondition(dtMC => dtMC.FileID.Equals(eFID)).FirstOrDefault();
        }
    }
}
