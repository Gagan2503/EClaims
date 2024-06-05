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
    public class DtTBClaimDraftRepository : RepositoryBase<DtTBClaimDraft>, IDtTBClaimDraftRepository
    {
        public DtTBClaimDraftRepository(RepositoryContext repositoryContext, IApplicationReadDbConnection readDbConnection, IApplicationWriteDbConnection writeDbConnection)
   : base(repositoryContext, writeDbConnection, readDbConnection)
        {
        }

        public void CreateDtTBClaimDraft(DtTBClaimDraft dtTBClaim)
        {
            Create(dtTBClaim);
        }

        public void DeleteDtTBClaimDraft(DtTBClaimDraft dtTBClaim)
        {
            Delete(dtTBClaim);
        }

        public async Task<IEnumerable<DtTBClaimDraft>> GetAllDtTBClaimDraftAsync()
        {
            {
                return await FindAll()
                .OrderBy(mc => mc.TBCItemID)
                 .ToListAsync();
            }
        }

        public async Task<IEnumerable<DtTBClaimDraft>> GetAllDtTBClaimDraftWithDetailsAsync()
        {
            return await FindAll()
            .Include(mf => mf.MstExpenseCategory)
            .ToListAsync();
        }

        public async Task<DtTBClaimDraft> GetDtTBClaimDraftByIdAsync(int? tBCItemID)
        {
            return await FindByCondition(mstTBC => mstTBC.TBCID.Equals(tBCItemID))
        .FirstOrDefaultAsync();
        }

        public async Task<List<DtTBClaimDraft>> GetDtTBClaimDraftByIdAsync(long? tBCID)
        {
            return await FindByCondition(mstEC => mstEC.TBCID.Equals(tBCID))
                .Include(mex => mex.MstExpenseCategory)
                .OrderBy(dtMC => dtMC.OrderBy)
                .ToListAsync();
        }

        public void UpdateDtTBClaimDraft(DtTBClaimDraft dtTBClaim)
        {
            Update(dtTBClaim);
        }
    }
}
