using EClaimsEntities.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EClaimsRepository.Contracts
{
   public interface IDtHRPVCClaimRepository : IRepositoryBase<DtHRPVCClaim>
    {
        Task<IEnumerable<DtHRPVCClaim>> GetAllDtHRPVCClaimAsync();
        Task<IEnumerable<DtHRPVCClaim>> GetAllDtHRPVCClaimWithDetailsAsync();
        Task<DtHRPVCClaim> GetDtHRPVCClaimByIdAsync(int? HRPVCCItemID);

        // IEnumerable<MstHRPVCClaim> HRPVCClaimByClaimType(int? claimTypeId);

        void CreateDtHRPVCClaim(DtHRPVCClaim dtHRPVCClaim);
        void UpdateDtHRPVCClaim(DtHRPVCClaim dtHRPVCClaim);
        void DeleteDtHRPVCClaim(DtHRPVCClaim dtHRPVCClaim);
        Task<List<DtHRPVCClaim>> GetDtHRPVCClaimByIdAsync(long? hRPVCCID);
    }
}
