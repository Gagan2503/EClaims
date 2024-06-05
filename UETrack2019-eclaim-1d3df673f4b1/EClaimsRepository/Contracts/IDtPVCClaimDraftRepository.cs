using EClaimsEntities.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EClaimsRepository.Contracts
{
   public interface IDtPVCClaimDraftRepository:IRepositoryBase<DtPVCClaimDraft>
    {
        Task<IEnumerable<DtPVCClaimDraft>> GetAllDtPVCClaimDraftAsync();
        Task<IEnumerable<DtPVCClaimDraft>> GetAllDtPVCClaimWithDetailsDraftAsync();
        Task<DtPVCClaimDraft> GetDtPVCClaimDraftByIdAsync(int? PVCCItemID);

        // IEnumerable<MstPVCClaim> PVCClaimByClaimType(int? claimTypeId);
        Task<List<DtPVCClaimDraft>> GetDtPVCClaimDraftByIdAsync(long? pVCCID);
        void CreateDtPVCClaimDraft(DtPVCClaimDraft dtPVCClaimDraft);
        void UpdateDtPVCClaimDraft(DtPVCClaimDraft dtPVCClaimDraft);
        void DeleteDtPVCClaimDraft(DtPVCClaimDraft dtPVCClaimDarft);
    }
}
