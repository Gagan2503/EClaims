using EClaimsEntities.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EClaimsRepository.Contracts
{
    public interface IMstFacilityRepository : IRepositoryBase<MstFacility>
    {
        Task<IEnumerable<MstFacility>> GetAllFacilityAsync(string type = "all");
        Task<IEnumerable<MstFacility>> GetAllFacilitiesWithDepartmentsAsync();
        Task<MstFacility> GetFacilityByIdAsync(int? facilityId);
        //Task<MstFacility> GetMstFacilityWithDetailsAsync(int facilityId);
        Task<MstFacility> GetFacilityWithDepartmentByIdAsync(int? facilityId);
        IEnumerable<MstFacility> FacilitiesByDepartment(int? departmentId);
        bool ValidateFacility(MstFacility mstFacility, string mode);
        MstFacility Authenticate(MstFacility mstFacility, string mode);
        void CreateFacility(MstFacility mstFacility);
        void UpdateFacility(MstFacility mstFacility);
        void DeleteFacility(MstFacility mstFacility);
    }
}
