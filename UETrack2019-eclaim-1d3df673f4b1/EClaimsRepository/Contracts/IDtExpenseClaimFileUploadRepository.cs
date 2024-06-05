using EClaimsEntities.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EClaimsRepository.Contracts
{
    public interface IDtExpenseClaimFileUploadRepository : IRepositoryBase<DtExpenseClaimFileUpload>
    {
        void CreateDtExpenseClaimFileUpload(DtExpenseClaimFileUpload dtExpenseClaimFileUpload);
        void UpdateDtExpenseClaimFileUpload(DtExpenseClaimFileUpload dtExpenseClaimFileUpload);
        void DeleteDtExpenseClaimFileUpload(DtExpenseClaimFileUpload dtExpenseClaimFileUpload);
        Task<List<DtExpenseClaimFileUpload>> GetDtExpenseClaimAuditByIdAsync(long? eCID);
        Task<DtExpenseClaimFileUpload> GetDtExpenseClaimFileUploadByIdAsync(long? eFID);
    }
}
