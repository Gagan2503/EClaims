using EClaimsEntities.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EClaimsRepository.Contracts
{
    public interface IDtMileageClaimRepository : IRepositoryBase<DtMileageClaim>
    {
        Task<IEnumerable<DtMileageClaim>> GetAllDtMileageClaimAsync();
        Task<IEnumerable<DtMileageClaim>> GetAllDtMileageClaimWithDetailsAsync();
        //Task<DtMileageClaim> GetDtMileageClaimByIdAsync(int? MCItemID);
        Task<List<DtMileageClaim>> GetDtMileageClaimByIdAsync(long? mCID);
        // IEnumerable<MstMileageClaim> MileageClaimByClaimType(int? claimTypeId);

        void CreateDtMileageClaim(DtMileageClaim dtMileageClaim);
        void UpdateDtMileageClaim(DtMileageClaim dtMileageClaim);
        void DeleteDtMileageClaim(DtMileageClaim dtMileageClaim);
    }
}
