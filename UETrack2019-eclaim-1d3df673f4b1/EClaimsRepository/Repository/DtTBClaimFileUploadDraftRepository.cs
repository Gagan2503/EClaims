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
    public class DtTBClaimFileUploadDraftRepository : RepositoryBase<DtTBClaimFileUploadDraft>, IDtTBClaimFileUploadDraftRepository
    {
        public DtTBClaimFileUploadDraftRepository(RepositoryContext repositoryContext, IApplicationReadDbConnection readDbConnection, IApplicationWriteDbConnection writeDbConnection)
   : base(repositoryContext, writeDbConnection, readDbConnection)
        {
        }

        public void CreateDtTBClaimFileUploadDraft(DtTBClaimFileUploadDraft dtTBClaimFileUpload)
        {
            Create(dtTBClaimFileUpload);
        }

        public void DeleteDtTBClaimFileUploadDraft(DtTBClaimFileUploadDraft dtTBClaimFileUpload)
        {
            Delete(dtTBClaimFileUpload);
        }

        public void UpdateDtTBClaimFileUploadDraft(DtTBClaimFileUploadDraft dtTBClaimFileUpload)
        {
            Update(dtTBClaimFileUpload);
        }

        public async Task<List<DtTBClaimFileUploadDraft>> GetDtTBClaimDraftAuditByIdAsync(long? tBCID)
        {
            return FindByCondition(dtMC => dtMC.TBCID.Equals(tBCID)).ToList();
        }

        public async Task<DtTBClaimFileUploadDraft> GetDtTBClaimFileUploadDraftByIdAsync(long? eFID)
        {
            return FindByCondition(dtMC => dtMC.FileID.Equals(eFID)).FirstOrDefault();
        }
    }
}
