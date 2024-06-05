using EClaimsEntities.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EClaimsRepository.Contracts
{
    public interface IMstClaimTypeRepository : IRepositoryBase<MstClaimType>
    {
        Task<IEnumerable<MstClaimType>> GetAllClaimTypeAsync();
       // Task<IEnumerable<MstFacility>> GetAllFacilitiesWithDepartmentsAsync();
       // Task<MstFacility> GetFacilityByIdAsync(int? facilityId);
        //Task<MstFacility> GetMstFacilityWithDetailsAsync(int facilityId);

        //IEnumerable<MstFacility> FacilitiesByDepartment(int? departmentId);

        //void CreateFacility(MstFacility mstFacility);
        //void UpdateFacility(MstFacility mstFacility);
        //void DeleteFacility(MstFacility mstFacility);
    }
}
