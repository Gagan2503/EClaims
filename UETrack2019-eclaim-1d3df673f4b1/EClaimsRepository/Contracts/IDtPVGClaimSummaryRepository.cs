using EClaimsEntities.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EClaimsRepository.Contracts
{
    public interface IDtPVGClaimSummaryRepository : IRepositoryBase<DtPVGClaimSummary>
    {
        void CreateDtPVGClaimSummary(DtPVGClaimSummary dtPVGClaim);
        void UpdateDtPVGClaimSummary(DtPVGClaimSummary dtPVGClaim);
        void DeleteDtPVGClaimSummary(DtPVGClaimSummary dtPVGClaim);
        Task<List<DtPVGClaimSummary>> GetDtPVGClaimSummaryByIdAsync(long? pVGCID);
    }
}
