using EClaimsEntities.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EClaimsRepository.Contracts
{
    public interface IDtHRPVCClaimSummaryRepository : IRepositoryBase<DtHRPVCClaimSummary>
    {
        void CreateDtHRPVCClaimSummary(DtHRPVCClaimSummary dtHRPVCClaim);
        void UpdateDtHRPVCClaimSummary(DtHRPVCClaimSummary dtHRPVCClaim);
        void DeleteDtHRPVCClaimSummary(DtHRPVCClaimSummary dtHRPVCClaim);
        Task<List<DtHRPVCClaimSummary>> GetDtHRPVCClaimSummaryByIdAsync(long? hRPVCCID);
    }
}
