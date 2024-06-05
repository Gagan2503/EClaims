using EClaimsEntities.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EClaimsRepository.Contracts
{
    public interface IDtMileageClaimSummaryRepository : IRepositoryBase<DtMileageClaimSummary>
    {
        void CreateDtMileageClaimSummary(DtMileageClaimSummary dtMileageClaim);
        void UpdateDtMileageClaimSummary(DtMileageClaimSummary dtMileageClaim);
        void DeleteDtMileageClaimSummary(DtMileageClaimSummary dtMileageClaim);
        Task<List<DtMileageClaimSummary>> GetDtMileageClaimSummaryByIdAsync(long? mCID);
    }
}
