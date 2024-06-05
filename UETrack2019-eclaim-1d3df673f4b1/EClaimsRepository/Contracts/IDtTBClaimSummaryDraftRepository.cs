using EClaimsEntities.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EClaimsRepository.Contracts
{
    public interface IDtTBClaimSummaryDraftRepository : IRepositoryBase<DtTBClaimSummaryDraft>
    {
        void CreateDtTBClaimSummaryDraft(DtTBClaimSummaryDraft dtTBClaim);
        void UpdateDtTBClaimSummaryDraft(DtTBClaimSummaryDraft dtTBClaim);
        void DeleteDtTBClaimSummaryDraft(DtTBClaimSummaryDraft dtTBClaim);
        Task<List<DtTBClaimSummaryDraft>> GetDtTBClaimSummaryDraftByIdAsync(long? tBCID);
    }
}
