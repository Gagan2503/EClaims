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
    class DtMileageClaimSummaryRepository : RepositoryBase<DtMileageClaimSummary>, IDtMileageClaimSummaryRepository
    {
        public DtMileageClaimSummaryRepository(RepositoryContext repositoryContext, IApplicationReadDbConnection readDbConnection, IApplicationWriteDbConnection writeDbConnection)
   : base(repositoryContext, writeDbConnection, readDbConnection)
        {
        }
        public void CreateDtMileageClaimSummary(DtMileageClaimSummary dtMileageClaimSummary)
        {
            Create(dtMileageClaimSummary);
        }

        public void UpdateDtMileageClaimSummary(DtMileageClaimSummary dtMileageClaimSummary)
        {
            Update(dtMileageClaimSummary);
        }

        public void DeleteDtMileageClaimSummary(DtMileageClaimSummary dtMileageClaimSummary)
        {
            Delete(dtMileageClaimSummary);
        }

        public async Task<List<DtMileageClaimSummary>> GetDtMileageClaimSummaryByIdAsync(long? mCID)
        {
            return await FindByCondition(mstEC => mstEC.MCID.Equals(mCID))
               .ToListAsync();
        }
    }
}
