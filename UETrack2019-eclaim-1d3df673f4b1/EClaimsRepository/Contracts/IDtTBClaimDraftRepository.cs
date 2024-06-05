using EClaimsEntities.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EClaimsRepository.Contracts
{
    public interface IDtTBClaimDraftRepository : IRepositoryBase<DtTBClaimDraft>
    {
        Task<IEnumerable<DtTBClaimDraft>> GetAllDtTBClaimDraftAsync();
        Task<IEnumerable<DtTBClaimDraft>> GetAllDtTBClaimDraftWithDetailsAsync();
        Task<DtTBClaimDraft> GetDtTBClaimDraftByIdAsync(int? TBCItemID);

        Task<List<DtTBClaimDraft>> GetDtTBClaimDraftByIdAsync(long? tBCID);

        // IEnumerable<MstTBClaim> TBClaimByClaimType(int? claimTypeId);

        void CreateDtTBClaimDraft(DtTBClaimDraft dtTBClaim);
        void UpdateDtTBClaimDraft(DtTBClaimDraft dtTBClaim);
        void DeleteDtTBClaimDraft(DtTBClaimDraft dtTBClaim);
    }
}
