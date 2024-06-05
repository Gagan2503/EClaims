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
    public class DtPVCClaimFileUploadDraftRepository : RepositoryBase<DtPVCClaimDraftFileUpload>, IDtPVCClaimFileUploadDraftRepository
    {
        public DtPVCClaimFileUploadDraftRepository(RepositoryContext repositoryContext, IApplicationReadDbConnection readDbConnection, IApplicationWriteDbConnection writeDbConnection)
   : base(repositoryContext, writeDbConnection, readDbConnection)
        {
        }
        public void CreateDtPVCClaimFileUploadDraft(DtPVCClaimDraftFileUpload dtPVCClaimFileUpload)
        {
            Create(dtPVCClaimFileUpload);
        }

        public void DeleteDtPVCClaimFileUploadDraft(DtPVCClaimDraftFileUpload dtPVCClaimFileUpload)
        {
            Delete(dtPVCClaimFileUpload);
        }

        public async Task<List<DtPVCClaimDraftFileUpload>> GetDtPVCClaimDraftAuditByIdAsync(long? eCID)
        {
            return FindByCondition(dtMC => dtMC.PVCCID.Equals(eCID)).ToList();
        }

        public async Task<DtPVCClaimDraftFileUpload> GetDtPVCClaimDraftFileUploadByIdAsync(long? eFID)
        {
            return FindByCondition(dtMC => dtMC.FileID.Equals(eFID)).FirstOrDefault();
        }

        public void UpdateDtPVCClaimFileUploadDraft(DtPVCClaimDraftFileUpload dtPVCClaimFileUploadDraft)
        {
            Update(dtPVCClaimFileUploadDraft);
        }

    }

}
