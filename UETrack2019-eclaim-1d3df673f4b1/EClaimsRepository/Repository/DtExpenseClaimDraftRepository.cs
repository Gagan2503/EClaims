using EClaimsEntities;
using EClaimsEntities.Models;
using EClaimsRepository.Contracts;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EClaimsRepository.Repository
{
    public class DtExpenseClaimDraftRepository : RepositoryBase<DtExpenseClaimDraft>, IDtExpenseClaimDraftRepository
    {
        public DtExpenseClaimDraftRepository(RepositoryContext repositoryContext, IApplicationReadDbConnection readDbConnection, IApplicationWriteDbConnection writeDbConnection)
   : base(repositoryContext, writeDbConnection, readDbConnection)
        {
        }

        public void CreateDtExpenseClaimDraft(DtExpenseClaimDraft dtExpenseClaim)
        {
            Create(dtExpenseClaim);
        }

        public void DeleteDtExpenseClaimDraft(DtExpenseClaimDraft dtExpenseClaim)
        {
            Delete(dtExpenseClaim);
        }

        public async Task<IEnumerable<DtExpenseClaimDraft>> GetAllDtExpenseClaimDraftAsync()
        {
            {
                return await FindAll()
                .OrderBy(mc => mc.ECItemID)
                 .ToListAsync();
            }
        }

        public async Task<IEnumerable<DtExpenseClaimDraft>> GetAllDtExpenseClaimDraftWithDetailsAsync()
        {
            return await FindAll()
            .Include(mf => mf.MstExpenseCategory)
            .ToListAsync();
        }

        public async Task<List<DtExpenseClaimDraft>> GetDtExpenseClaimDraftByIdAsync(long? eCID)
        {
            return await FindByCondition(mstEC => mstEC.ECID.Equals(eCID))
                .Include(mex => mex.MstExpenseCategory)
                .OrderBy(dtMC => dtMC.OrderBy)
                .ToListAsync();
        }

        public async Task<DtExpenseClaimDraft> GetTopDtExpenseClaimDraftByIdAsync(long? eCID)
        {
            return await FindByCondition(mstEC => mstEC.ECID.Equals(eCID))
                .OrderBy(mc => mc.ECItemID)
                .FirstOrDefaultAsync();
        }

        public Task<DtExpenseClaimDraft> GetDtExpenseClaimDraftByIdAsync(int? ECItemID)
        {
            throw new NotImplementedException();
        }

        public void UpdateDtExpenseClaimDraft(DtExpenseClaimDraft dtExpenseClaim)
        {
            Update(dtExpenseClaim);
        }
    }
}
