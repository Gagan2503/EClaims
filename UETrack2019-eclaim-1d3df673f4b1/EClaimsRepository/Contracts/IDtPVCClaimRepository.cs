using EClaimsEntities.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EClaimsRepository.Contracts
{
   public interface IDtPVCClaimRepository : IRepositoryBase<DtPVCClaim>
    {
        Task<IEnumerable<DtPVCClaim>> GetAllDtPVCClaimAsync();
        Task<IEnumerable<DtPVCClaim>> GetAllDtPVCClaimWithDetailsAsync();
        Task<DtPVCClaim> GetDtPVCClaimByIdAsync(int? PVCCItemID);

        // IEnumerable<MstPVCClaim> PVCClaimByClaimType(int? claimTypeId);
        Task<List<DtPVCClaim>> GetDtPVCClaimByIdAsync(long? pVCCID);
        void CreateDtPVCClaim(DtPVCClaim dtPVCClaim);
        void UpdateDtPVCClaim(DtPVCClaim dtPVCClaim);
        void DeleteDtPVCClaim(DtPVCClaim dtPVCClaim);
    }
}
