using EClaimsEntities.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EClaimsRepository.Contracts
{
   public interface IDtHRPVGClaimRepository : IRepositoryBase<DtHRPVGClaim>
    {
        Task<IEnumerable<DtHRPVGClaim>> GetAllDtHRPVGClaimAsync();
        Task<IEnumerable<DtHRPVGClaim>> GetAllDtHRPVGClaimWithDetailsAsync();

        Task<DtHRPVGClaim> GetDtHRPVGClaimByHRPVGCItemIDAsync(long? hRPVGCItemID);
        Task<DtHRPVGClaim> GetDtHRPVGClaimByIdAsync(int? HRPVGCItemID);
        Task<List<DtHRPVGClaim>> GetDtHRPVGClaimByIdAsync(long? hRPVGCID);
        // IEnumerable<MstHRPVGClaim> HRPVGClaimByClaimType(int? claimTypeId);

        void CreateDtHRPVGClaim(DtHRPVGClaim dtHRPVGClaim);
        void UpdateDtHRPVGClaim(DtHRPVGClaim dtHRPVGClaim);
        void DeleteDtHRPVGClaim(DtHRPVGClaim dtHRPVGClaim);
    }
}
