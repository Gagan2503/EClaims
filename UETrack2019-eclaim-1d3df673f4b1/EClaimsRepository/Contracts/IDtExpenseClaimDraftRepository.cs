using EClaimsEntities.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EClaimsRepository.Contracts
{
    public interface IDtExpenseClaimDraftRepository : IRepositoryBase<DtExpenseClaimDraft>
    {
        Task<IEnumerable<DtExpenseClaimDraft>> GetAllDtExpenseClaimDraftAsync();
        Task<IEnumerable<DtExpenseClaimDraft>> GetAllDtExpenseClaimDraftWithDetailsAsync();
        Task<DtExpenseClaimDraft> GetDtExpenseClaimDraftByIdAsync(int? ECItemID);

        Task<List<DtExpenseClaimDraft>> GetDtExpenseClaimDraftByIdAsync(long? eCID);
        Task<DtExpenseClaimDraft> GetTopDtExpenseClaimDraftByIdAsync(long? eCID);
        void CreateDtExpenseClaimDraft(DtExpenseClaimDraft dtExpenseClaim);
        void UpdateDtExpenseClaimDraft(DtExpenseClaimDraft dtExpenseClaim);
        void DeleteDtExpenseClaimDraft(DtExpenseClaimDraft dtExpenseClaim);
    }
}
