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
    public class DtUserFacilitiesRepository : RepositoryBase<DtUserFacilities>, IDtUserFacilitiesRepository
    {
        public DtUserFacilitiesRepository(RepositoryContext repositoryContext, IApplicationReadDbConnection readDbConnection, IApplicationWriteDbConnection writeDbConnection)
   : base(repositoryContext, writeDbConnection, readDbConnection)
        {
        }

        public async Task<IEnumerable<MstFacility>> GetAllFacilitiesByUserIdAsync(int? userId)
        {
            return await FindByCondition(ur => ur.UserID == userId)
               .Select(u => u.Facility)
               .ToListAsync();
        }
    }
}
