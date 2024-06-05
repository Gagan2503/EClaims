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
    public class DtPVGClaimSummaryDraftRepository : RepositoryBase<DtPVGClaimSummaryDraft>, IDtPVGClaimSummaryDraftRepository
    {
        public DtPVGClaimSummaryDraftRepository(RepositoryContext repositoryContext, IApplicationReadDbConnection readDbConnection, IApplicationWriteDbConnection writeDbConnection)
 : base(repositoryContext, writeDbConnection, readDbConnection)
        {
        }

        public void CreateDtPVGClaimSummaryDraft(DtPVGClaimSummaryDraft dtPVGClaimSummaryDraft)
        {
            Create(dtPVGClaimSummaryDraft);
        }

        public void UpdateDtPVGClaimSummaryDraft(DtPVGClaimSummaryDraft dtPVGClaimSummaryDraft)
        {
            Update(dtPVGClaimSummaryDraft);
        }

        public void DeleteDtPVGClaimSummaryDraft(DtPVGClaimSummaryDraft dtPVGClaimSummaryDraft)
        {
            Delete(dtPVGClaimSummaryDraft);
        }

        public async Task<List<DtPVGClaimSummaryDraft>> GetDtPVGClaimSummaryDraftByIdAsync(long? hRPVGCID)
        {
            return await FindByCondition(mstEC => mstEC.PVGCID.Equals(hRPVGCID))
               .ToListAsync();
        }
    }
}
