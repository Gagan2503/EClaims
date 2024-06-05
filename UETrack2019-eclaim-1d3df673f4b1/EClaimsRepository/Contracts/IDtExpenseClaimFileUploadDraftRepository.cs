using EClaimsEntities.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace EClaimsRepository.Contracts
{
    public interface IDtExpenseClaimFileUploadDraftRepository : IRepositoryBase<DtExpenseClaimFileUploadDraft>
    {
        void CreateDtExpenseClaimFileUploadDraft(DtExpenseClaimFileUploadDraft dtExpenseClaimFileUpload);
        void UpdateDtExpenseClaimFileUploadDraft(DtExpenseClaimFileUploadDraft dtExpenseClaimFileUpload);
        void DeleteDtExpenseClaimFileUploadDraft(DtExpenseClaimFileUploadDraft dtExpenseClaimFileUpload);
        Task<List<DtExpenseClaimFileUploadDraft>> GetDtExpenseClaimDraftAuditByIdAsync(long? eCID);
        Task<DtExpenseClaimFileUploadDraft> GetDtExpenseClaimDraftFileUploadByIdAsync(long? eFID);
    }
}
