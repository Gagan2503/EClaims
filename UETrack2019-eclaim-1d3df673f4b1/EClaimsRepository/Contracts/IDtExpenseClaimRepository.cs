using EClaimsEntities.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EClaimsRepository.Contracts
{
   public interface IDtExpenseClaimRepository : IRepositoryBase<DtExpenseClaim>
    {
        Task<IEnumerable<DtExpenseClaim>> GetAllDtExpenseClaimAsync();
        Task<IEnumerable<DtExpenseClaim>> GetAllDtExpenseClaimWithDetailsAsync();
        Task<DtExpenseClaim> GetDtExpenseClaimByIdAsync(int? ECItemID);

        // IEnumerable<MstExpenseClaim> ExpenseClaimByClaimType(int? claimTypeId);
        Task<List<DtExpenseClaim>> GetDtExpenseClaimByIdAsync(long? eCID);
        Task<DtExpenseClaim> GetTopDtExpenseClaimByIdAsync(long? eCID);
        void CreateDtExpenseClaim(DtExpenseClaim dtExpenseClaim);
        void UpdateDtExpenseClaim(DtExpenseClaim dtExpenseClaim);
        void DeleteDtExpenseClaim(DtExpenseClaim dtExpenseClaim);
    }
}
