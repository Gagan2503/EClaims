using EClaimsEntities.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EClaimsRepository.Contracts
{
   public interface IDtTBClaimRepository : IRepositoryBase<DtTBClaim>
    {
        Task<IEnumerable<DtTBClaim>> GetAllDtTBClaimAsync();
        Task<IEnumerable<DtTBClaim>> GetAllDtTBClaimWithDetailsAsync();
        Task<DtTBClaim> GetDtTBClaimByIdAsync(int? TBCItemID);

        Task<List<DtTBClaim>> GetDtTBClaimByIdAsync(long? tBCID);

        // IEnumerable<MstTBClaim> TBClaimByClaimType(int? claimTypeId);

        void CreateDtTBClaim(DtTBClaim dtTBClaim);
        void UpdateDtTBClaim(DtTBClaim dtTBClaim);
        void DeleteDtTBClaim(DtTBClaim dtTBClaim);
    }
}
