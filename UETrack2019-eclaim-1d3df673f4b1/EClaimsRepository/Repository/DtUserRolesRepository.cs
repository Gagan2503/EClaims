using EClaimsEntities;
using EClaimsEntities.Models;
using EClaimsRepository.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EClaimsRepository.Repository
{
    public class DtUserRolesRepository : RepositoryBase<DtUserRoles>, IDtUserRolesRepository
    {
        public DtUserRolesRepository(RepositoryContext repositoryContext, IApplicationReadDbConnection readDbConnection, IApplicationWriteDbConnection writeDbConnection)
   : base(repositoryContext, writeDbConnection, readDbConnection)
        {
        }
        public void CreateUserRoles(DtUserRoles mstDtUserRoles)
        {
            Create(mstDtUserRoles);
        }

        public void DeleteUserRoles(DtUserRoles mstDtUserRoles)
        {
            Delete(mstDtUserRoles);
        }

        public  IEnumerable<MstRole> GetAllRolesByUserIdAsync(int? userId)
        {
           return  FindByCondition(ur => ur.UserID == userId)
                .Select(u => u.Role)
                .ToList();
        }

        public IEnumerable<MstUser> GetAllUsersByRoleIdAsync(int? roleId)
        {
            return FindByCondition(ur => ur.RoleID == roleId && ur.User.IsActive)
                 .Select(u => u.User)
                 .ToList();
        }

        public IEnumerable<MstUser> GetAllHRHODUsersByRoleIdAsync(int? roleId)
        {
            return FindByCondition(ur => ur.RoleID == roleId && ur.User.IsHOD && ur.User.IsActive)
                 .Select(u => u.User)
                 .ToList();
        }

        public IEnumerable<MstUser> GetAllHODUsersAsync()
        {
            return FindByCondition(ur => ur.User.IsHOD && ur.User.IsActive)
                 .Select(u => u.User)
                 .ToList();
        }
    }
}
