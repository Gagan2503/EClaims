using EClaimsEntities.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EClaimsRepository.Contracts
{
   public interface IDtPVGClaimSummaryDraftRepository : IRepositoryBase<DtPVGClaimSummaryDraft>
    {
        void CreateDtPVGClaimSummaryDraft(DtPVGClaimSummaryDraft dtPVGClaimDraft);
        void UpdateDtPVGClaimSummaryDraft(DtPVGClaimSummaryDraft dtPVGClaimDraft);
        void DeleteDtPVGClaimSummaryDraft(DtPVGClaimSummaryDraft dtPVGClaimDraft);
        Task<List<DtPVGClaimSummaryDraft>> GetDtPVGClaimSummaryDraftByIdAsync(long? pVGCID);
    }
}
