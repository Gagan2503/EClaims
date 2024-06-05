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
    public class MstRoleRepository : RepositoryBase<MstRole>, IMstRoleRepository
    {
        public MstRoleRepository(RepositoryContext repositoryContext, IApplicationReadDbConnection readDbConnection, IApplicationWriteDbConnection writeDbConnection)
   : base(repositoryContext, writeDbConnection, readDbConnection)
        {
        }
        public async Task<IEnumerable<MstRole>> GetAllRolesAsync()
        {
            return await FindAll()
               .OrderBy(mr => mr.RoleName)
               .ToListAsync();
        }

        public async Task<MstRole> GetRoleByIdAsync(int? roleId)
        {
            return await FindByCondition(mstRole => mstRole.RoleID.Equals(roleId))
                    .FirstOrDefaultAsync();
        }

        public async Task<MstRole> GetRoleByNameAsync(string roleName)
        {
            return await FindByCondition(mstRole => mstRole.RoleName.ToLower().Equals(roleName.ToLower()))
                    .FirstOrDefaultAsync();
        }
    }
}
