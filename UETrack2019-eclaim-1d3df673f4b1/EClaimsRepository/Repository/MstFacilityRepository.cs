using EClaimsEntities;
using EClaimsRepository.Contracts;
using EClaimsEntities.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EClaimsRepository.Repository
{
    public class MstFacilityRepository : RepositoryBase<MstFacility>, IMstFacilityRepository
    {
        public MstFacilityRepository(RepositoryContext repositoryContext, IApplicationReadDbConnection readDbConnection, IApplicationWriteDbConnection writeDbConnection)
   : base(repositoryContext, writeDbConnection, readDbConnection)
        {
        }


        public void CreateFacility(MstFacility mstFacility)
        {
            Create(mstFacility);
        }

        public void UpdateFacility(MstFacility mstFacility)
        {
            Update(mstFacility);
        }

        public void DeleteFacility(MstFacility mstFacility)
        {
            Delete(mstFacility);
        }

        public IEnumerable<MstFacility> FacilitiesByDepartment(int? departmentId)
        {
            return FindByCondition(a => a.DepartmentID.Equals(departmentId)).ToList();
        }


        public async Task<IEnumerable<MstFacility>> GetAllFacilityAsync(string type = "all")
        {
            if (type == "all")
            {
                return await FindAll()
               .OrderBy(mf => mf.FacilityName)
               .ToListAsync();
            }
            else
            {
                return await FindByCondition(md => md.IsActive)
               .OrderBy(mf => mf.FacilityName)
               .ToListAsync();
            }
        }


        public async Task<MstFacility> GetFacilityByIdAsync(int? facilityId)
        {
            return await FindByCondition(mstFacility => mstFacility.FacilityID.Equals(facilityId))
                    .FirstOrDefaultAsync();
        }

        public async Task<MstFacility> GetFacilityWithDepartmentByIdAsync(int? facilityId)
        {
            return await FindByCondition(mstFacility => mstFacility.FacilityID.Equals(facilityId))
                    .Include(m => m.MstDepartment)
                    .FirstOrDefaultAsync();
        }


        public async Task<IEnumerable<MstFacility>> GetAllFacilitiesWithDepartmentsAsync()
        {
            return await FindAll()
                .Include(m => m.MstDepartment)
                .ToListAsync();
            //_context.mstFacilities.Include(m => m.MstDepartment);
        }

        public bool ValidateFacility(MstFacility mstFacility, string mode)
        {
            var facility = Authenticate(mstFacility,mode);

            if (facility is null)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        public MstFacility Authenticate(MstFacility mstFacility, string mode)
        {
            if (mode == "create")
            {
                return FindByCondition(mf => mf.FacilityName.Equals(mstFacility.FacilityName) )
                   .FirstOrDefault();
            }
            else
            {
                return FindByCondition(mf => (mf.FacilityName.Equals(mstFacility.FacilityName) ) && mf.FacilityID != (mstFacility.FacilityID))
                .FirstOrDefault();
            }
        }
    }
}
