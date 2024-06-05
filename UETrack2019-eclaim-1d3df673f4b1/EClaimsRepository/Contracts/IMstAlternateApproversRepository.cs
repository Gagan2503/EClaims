using EClaimsEntities.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EClaimsRepository.Contracts
{
    public interface IMstAlternateApproversRepository : IRepositoryBase<MstAlternateApprovers>
    {
        Task<MstAlternateApprovers> GetAlternateApproverByUserIdAsync(int userId);
        void CreateAlternateApprover(MstAlternateApprovers mstAlternateApprover);
    }
}
