using EClaimsEntities.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EClaimsRepository.Contracts
{
    public interface IMstDepartmentRepository : IRepositoryBase<MstDepartment>
    {
        Task<IEnumerable<MstDepartment>> GetAllDepartmentAsync(string type = "all");
        IEnumerable<MstDepartment> GetAllDepartment();
        Task<MstDepartment> GetDepartmentByIdAsync(int? departmentId);
        Task<MstDepartment> GetDepartmentWithDetailsAsync(int? departmentId);
        Task<int> InsertApprovalMatrixForDepartment(int? departmentID, int? userID);
        MstDepartment CreateAndReturnDepartment(MstDepartment mstDepartment);
        bool ValidateDepartment(MstDepartment mstDepartment, string mode);
        MstDepartment Authenticate(MstDepartment mstDepartment, string mode);
        void CreateDepartment(MstDepartment mstDepartment);
        void UpdateDepartment(MstDepartment mstDepartment);
        void DeleteDepartment(MstDepartment mstDepartment);
    }
}
