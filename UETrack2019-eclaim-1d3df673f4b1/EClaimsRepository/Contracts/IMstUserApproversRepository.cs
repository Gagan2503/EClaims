using EClaimsEntities.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EClaimsRepository.Contracts
{
    public interface IMstUserApproversRepository: IRepositoryBase<MstUserApprovers>
    {
        Task<IEnumerable<MstUserApprovers>> GetUserApproversByUserIdAsync(int userId);
        Task<IEnumerable<MstUserApprovers>> GetUserApproversByUserIdFacilityIdAsync(int userId, int? facilityId);
        Task<MstUserApprovers> CheckWhetherUserIsSuperiorAsync(int userId);
        void CreateUserApprovers(MstUserApprovers mstUserApprovers);
    }
}
