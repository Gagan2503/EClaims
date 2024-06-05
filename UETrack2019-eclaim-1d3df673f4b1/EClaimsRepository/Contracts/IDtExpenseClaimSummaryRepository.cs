using EClaimsEntities.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EClaimsRepository.Contracts
{
    public interface IDtExpenseClaimSummaryRepository : IRepositoryBase<DtExpenseClaimSummary>
    {
        void CreateDtExpenseClaimSummary(DtExpenseClaimSummary dtExpenseClaim);
        void UpdateDtExpenseClaimSummary(DtExpenseClaimSummary dtExpenseClaim);
        void DeleteDtExpenseClaimSummary(DtExpenseClaimSummary dtExpenseClaim);
        Task<List<DtExpenseClaimSummary>> GetDtExpenseClaimSummaryByIdAsync(long? eCID);
    }
}
