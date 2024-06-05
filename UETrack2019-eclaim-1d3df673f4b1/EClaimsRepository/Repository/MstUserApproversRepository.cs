using EClaimsEntities;
using EClaimsEntities.Models;
using EClaimsRepository.Contracts;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EClaimsRepository.Repository
{
    public class MstUserApproversRepository : RepositoryBase<MstUserApprovers>, IMstUserApproversRepository
    {
        public MstUserApproversRepository(RepositoryContext repositoryContext, IApplicationReadDbConnection readDbConnection, IApplicationWriteDbConnection writeDbConnection)
            : base(repositoryContext, writeDbConnection, readDbConnection)
        {
        }
        public void CreateUserApprovers(MstUserApprovers mstUserApprovers)
        {
            Create(mstUserApprovers);
        }

        public async Task<IEnumerable<MstUserApprovers>> GetUserApproversByUserIdAsync(int userId)
        {
            return await FindByCondition(approver => approver.UserId.Equals(userId)).ToListAsync();
        }

        public async Task<IEnumerable<MstUserApprovers>> GetUserApproversByUserIdFacilityIdAsync(int userId,int? facilityId)
        {
            return await FindByCondition(approver => approver.UserId.Equals(userId) && approver.FacilityId.Equals(facilityId)).ToListAsync();
        }

        public async Task<MstUserApprovers> CheckWhetherUserIsSuperiorAsync(int userId)
        {
            return await FindByCondition(approver => approver.ApproverId.Equals(userId) && approver.IsApproverActive).FirstOrDefaultAsync();
        }
    }
}
