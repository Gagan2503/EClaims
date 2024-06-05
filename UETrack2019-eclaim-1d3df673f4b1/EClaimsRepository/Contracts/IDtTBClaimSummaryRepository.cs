using EClaimsEntities.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EClaimsRepository.Contracts
{
    public interface IDtTBClaimSummaryRepository : IRepositoryBase<DtTBClaimSummary>
    {
        void CreateDtTBClaimSummary(DtTBClaimSummary dtTBClaim);
        void UpdateDtTBClaimSummary(DtTBClaimSummary dtTBClaim);
        void DeleteDtTBClaimSummary(DtTBClaimSummary dtTBClaim);
        Task<List<DtTBClaimSummary>> GetDtTBClaimSummaryByIdAsync(long? tBCID);
    }
}
