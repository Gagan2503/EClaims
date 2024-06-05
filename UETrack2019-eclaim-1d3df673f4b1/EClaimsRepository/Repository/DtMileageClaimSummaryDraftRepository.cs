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
    class DtMileageClaimSummaryDraftRepository : RepositoryBase<DtMileageClaimSummaryDraft>, IDtMileageClaimSummaryDraftRepository
    {
        public DtMileageClaimSummaryDraftRepository(RepositoryContext repositoryContext, IApplicationReadDbConnection readDbConnection, IApplicationWriteDbConnection writeDbConnection)
   : base(repositoryContext, writeDbConnection, readDbConnection)
        {
        }
        public void CreateDtMileageClaimSummaryDraft(DtMileageClaimSummaryDraft dtMileageClaimSummary)
        {
            Create(dtMileageClaimSummary);
        }

        public void UpdateDtMileageClaimSummaryDraft(DtMileageClaimSummaryDraft dtMileageClaimSummary)
        {
            Update(dtMileageClaimSummary);
        }

        public void DeleteDtMileageClaimSummaryDraft(DtMileageClaimSummaryDraft dtMileageClaimSummary)
        {
            Delete(dtMileageClaimSummary);
        }

        public async Task<List<DtMileageClaimSummaryDraft>> GetDtMileageClaimSummaryDraftByIdAsync(long? mCID)
        {
            return await FindByCondition(mstEC => mstEC.MCID.Equals(mCID))
               .ToListAsync();
        }
    }
}
