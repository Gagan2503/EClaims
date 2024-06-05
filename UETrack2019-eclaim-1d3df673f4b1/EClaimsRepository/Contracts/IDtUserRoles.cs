using EClaimsEntities.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EClaimsRepository.Contracts
{
    public interface IDtUserRolesRepository : IRepositoryBase<DtUserRoles>
    {
        // Task<IEnumerable<MstDepartment>> GetAllDepartmentAsync();
        // IEnumerable<MstDepartment> GetAllDepartment();
        //Task<MstRole> GetAllRolesByUserIdAsync(int? userId);
        IEnumerable<MstRole> GetAllRolesByUserIdAsync(int? userId);
       // Task<MstDepartment> GetDepartmentWithDetailsAsync(int? departmentId);
        void CreateUserRoles(DtUserRoles mstDtUserRoles);
        //void UpdateDepartment(MstDepartment mstDepartment);
        void DeleteUserRoles(DtUserRoles mstDtUserRoles);
    }
}
