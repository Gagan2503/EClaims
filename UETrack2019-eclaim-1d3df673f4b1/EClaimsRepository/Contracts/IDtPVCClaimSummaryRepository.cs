using EClaimsEntities.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EClaimsRepository.Contracts
{
    public interface IDtPVCClaimSummaryRepository : IRepositoryBase<DtPVCClaimSummary>
    {
        void CreateDtPVCClaimSummary(DtPVCClaimSummary dtPVCClaim);
        void UpdateDtPVCClaimSummary(DtPVCClaimSummary dtPVCClaim);
        void DeleteDtPVCClaimSummary(DtPVCClaimSummary dtPVCClaim);
        Task<List<DtPVCClaimSummary>> GetDtPVCClaimSummaryByIdAsync(long? pVCCID);
    }
}
