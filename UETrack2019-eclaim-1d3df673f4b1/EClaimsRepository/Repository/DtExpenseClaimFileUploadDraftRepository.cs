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
    public class DtExpenseClaimFileUploadDraftRepository : RepositoryBase<DtExpenseClaimFileUploadDraft>, IDtExpenseClaimFileUploadDraftRepository
    {
        public DtExpenseClaimFileUploadDraftRepository(RepositoryContext repositoryContext, IApplicationReadDbConnection readDbConnection, IApplicationWriteDbConnection writeDbConnection)
   : base(repositoryContext, writeDbConnection, readDbConnection)
        {
        }
        public void CreateDtExpenseClaimFileUploadDraft(DtExpenseClaimFileUploadDraft dtExpenseClaimFileUpload)
        {
            Create(dtExpenseClaimFileUpload);
        }

        public void DeleteDtExpenseClaimFileUploadDraft(DtExpenseClaimFileUploadDraft dtExpenseClaimFileUpload)
        {
            Delete(dtExpenseClaimFileUpload);
        }

        public async Task<List<DtExpenseClaimFileUploadDraft>> GetDtExpenseClaimDraftAuditByIdAsync(long? eCID)
        {
            return FindByCondition(dtMC => dtMC.ECID.Equals(eCID)).ToList();
        }

        public async Task<DtExpenseClaimFileUploadDraft> GetDtExpenseClaimDraftFileUploadByIdAsync(long? eFID)
        {
            return FindByCondition(dtMC => dtMC.FileID.Equals(eFID)).FirstOrDefault();
        }

        public void UpdateDtExpenseClaimFileUploadDraft(DtExpenseClaimFileUploadDraft dtExpenseClaimFileUpload)
        {
            Update(dtExpenseClaimFileUpload);
        }

    }
}
