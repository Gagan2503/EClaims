using EClaimsEntities.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EClaimsRepository.Contracts
{
    public interface IDtHRPVGClaimSummaryRepository : IRepositoryBase<DtHRPVGClaimSummary>
    {
        void CreateDtHRPVGClaimSummary(DtHRPVGClaimSummary dtHRPVGClaim);
        void UpdateDtHRPVGClaimSummary(DtHRPVGClaimSummary dtHRPVGClaim);
        void DeleteDtHRPVGClaimSummary(DtHRPVGClaimSummary dtHRPVGClaim);
        Task<List<DtHRPVGClaimSummary>> GetDtHRPVGClaimSummaryByIdAsync(long? hRPVGCID);
    }
}
