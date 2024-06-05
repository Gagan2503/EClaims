using EClaimsEntities.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EClaimsRepository.Contracts
{
    public interface IMstRoleRepository : IRepositoryBase<MstRole>
    {
        Task<IEnumerable<MstRole>> GetAllRolesAsync();
        Task<MstRole> GetRoleByIdAsync(int? RoleId);
        Task<MstRole> GetRoleByNameAsync(string roleName);
        //Task<MstRole> GetRoleWithDetailsAsync(int? RoleId);
        //void CreateRole(MstRole mstRole);
        //void UpdateRole(MstRole mstRole);
        //void DeleteRole(MstRole mstRole);
    }
}
