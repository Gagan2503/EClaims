using EClaimsEntities.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EClaimsRepository.Contracts
{
   public interface IDtHRPVGDraftClaimRepository : IRepositoryBase<DtHRPVGClaimDraft>
    {
        //Task<IEnumerable<DtHRPVGClaim>> GetAllDtHRPVGClaimAsync();
        //Task<IEnumerable<DtHRPVGClaim>> GetAllDtHRPVGClaimWithDetailsAsync();

        //Task<DtHRPVGClaim> GetDtHRPVGClaimByHRPVGCItemIDAsync(long? hRPVGCItemID);
        Task<DtHRPVGClaimDraft> GetDtHRPVGClaimByIdAsync(int? HRPVGCItemID);
        Task<List<DtHRPVGClaimDraft>> GetDtHRPVGClaimByIdAsync(long? hRPVGCID);
        //// IEnumerable<MstHRPVGClaim> HRPVGClaimByClaimType(int? claimTypeId);

        //void CreateDtHRPVGClaim(DtHRPVGClaim dtHRPVGClaim);
        //void UpdateDtHRPVGClaim(DtHRPVGClaim dtHRPVGClaim);
        //void DeleteDtHRPVGClaim(DtHRPVGClaim dtHRPVGClaim);
    }
}
