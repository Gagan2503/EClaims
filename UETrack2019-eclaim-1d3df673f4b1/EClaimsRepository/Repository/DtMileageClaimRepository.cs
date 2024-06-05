using EClaimsEntities;
using EClaimsEntities.Models;
using EClaimsRepository.Contracts;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace EClaimsRepository.Repository
{
   public class DtMileageClaimRepository : RepositoryBase<DtMileageClaim>, IDtMileageClaimRepository
    {
        public DtMileageClaimRepository(RepositoryContext repositoryContext, IApplicationReadDbConnection readDbConnection, IApplicationWriteDbConnection writeDbConnection)
   : base(repositoryContext, writeDbConnection, readDbConnection)
        {
        }

        public void CreateDtMileageClaim(EClaimsEntities.Models.DtMileageClaim dtMileageClaim)
        {
            Create(dtMileageClaim);
        }

        

        public void DeleteDtMileageClaim(EClaimsEntities.Models.DtMileageClaim dtMileageClaim)
        {
            Delete(dtMileageClaim);
        }

        public async Task<IEnumerable<DtMileageClaim>> GetAllDtMileageClaimAsync()
        {
            return await FindAll()
             .OrderBy(mc => mc.MCItemID)
             .ToListAsync();
        }

        public async Task<IEnumerable<DtMileageClaim>> GetAllDtMileageClaimWithDetailsAsync()
        {
            return await FindAll()
                .Include(mf => mf.MstFacility)
                .ToListAsync();
        }

        public async Task<List<DtMileageClaim>> GetDtMileageClaimByIdAsync(long? mCID)
        {
            return await FindByCondition(dtMC => dtMC.MCID.Equals(mCID))
                 .Include(mf => mf.MstFacility)
                 .ToListAsync();
        }


        public void UpdateDtMileageClaim(EClaimsEntities.Models.DtMileageClaim dtMileageClaim)
        {
            throw new NotImplementedException();
        }

    }
}
