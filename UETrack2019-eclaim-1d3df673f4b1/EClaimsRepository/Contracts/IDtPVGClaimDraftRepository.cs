using EClaimsEntities.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EClaimsRepository.Contracts
{
    public interface IDtPVGClaimDraftRepository : IRepositoryBase<DtPVGClaimDraft>
    {
        Task<IEnumerable<DtPVGClaimDraft>> GetAllDtPVGClaimDraftAsync();
        Task<IEnumerable<DtPVGClaimDraft>> GetAllDtPVGClaimDraftWithDetailsAsync();
        Task<DtPVGClaimDraft> GetDtPVGClaimDraftByIdAsync(int? PVGCItemID);

        // IEnumerable<MstPVGClaim> PVGClaimByClaimType(int? claimTypeId);
        Task<List<DtPVGClaimDraft>> GetDtPVGClaimDraftByIdAsync(long? pVGCID);
        void CreateDtPVGClaimDraft(DtPVGClaimDraft dtPVGClaimDraft);
        void UpdateDtPVGClaimDraft(DtPVGClaimDraft dtPVGClaimDraft);
        void DeleteDtPVGClaimDraft(DtPVGClaimDraft dtPVGClaimDraft);
    }
}
