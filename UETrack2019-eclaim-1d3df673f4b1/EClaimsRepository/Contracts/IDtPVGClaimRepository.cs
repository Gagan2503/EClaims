using EClaimsEntities.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EClaimsRepository.Contracts
{
   public interface IDtPVGClaimRepository : IRepositoryBase<DtPVGClaim>
    {
        Task<IEnumerable<DtPVGClaim>> GetAllDtPVGClaimAsync();
        Task<IEnumerable<DtPVGClaim>> GetAllDtPVGClaimWithDetailsAsync();
        Task<DtPVGClaim> GetDtPVGClaimByPVGCItemIDAsync(long? PVGCItemID);

        // IEnumerable<MstPVGClaim> PVGClaimByClaimType(int? claimTypeId);
        Task<List<DtPVGClaim>> GetDtPVGClaimByIdAsync(long? pVGCID);
        void CreateDtPVGClaim(DtPVGClaim dtPVGClaim);
        void UpdateDtPVGClaim(DtPVGClaim dtPVGClaim);
        void DeleteDtPVGClaim(DtPVGClaim dtPVGClaim);
    }
}
