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
    public class DtPVGClaimDraftRepository : RepositoryBase<DtPVGClaimDraft>, IDtPVGClaimDraftRepository
    {
        public DtPVGClaimDraftRepository(RepositoryContext repositoryContext, IApplicationReadDbConnection readDbConnection, IApplicationWriteDbConnection writeDbConnection)
   : base(repositoryContext, writeDbConnection, readDbConnection)
        {
        }

        public void CreateDtPVGClaimDraft(DtPVGClaimDraft dtPVGClaimDraft)
        {
            Create(dtPVGClaimDraft);
        }

        public void DeleteDtPVGClaimDraft(DtPVGClaimDraft dtPVGClaimDraft)
        {
            Delete(dtPVGClaimDraft);
        }

        public async Task<IEnumerable<DtPVGClaimDraft>> GetAllDtPVGClaimDraftAsync()
        {
            {
                return await FindAll()
                .OrderBy(mc => mc.PVGCItemID)
                 .ToListAsync();
            }
        }

        public async Task<IEnumerable<DtPVGClaimDraft>> GetAllDtPVGClaimDraftWithDetailsAsync()
        {
            return await FindAll()
            .Include(mf => mf.MstExpenseCategory)
            .ToListAsync();
        }

        public async Task<DtPVGClaimDraft> GetDtPVGClaimDraftByIdAsync(int? pVGCItemID)
        {
            return await FindByCondition(mstPVGC => mstPVGC.PVGCID.Equals(pVGCItemID))
        .FirstOrDefaultAsync();
        }

        public async Task<List<DtPVGClaimDraft>> GetDtPVGClaimDraftByIdAsync(long? pVGCID)
        {
            return await FindByCondition(mstEC => mstEC.PVGCID.Equals(pVGCID))
                .Include(mex => mex.MstExpenseCategory)
                .OrderBy(mstEC => mstEC.OrderBy)
                .ToListAsync();
        }

        public void UpdateDtPVGClaimDraft(DtPVGClaimDraft dtPVGClaimDraft)
        {
            Update(dtPVGClaimDraft);
        }
    }
}
