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
    public class DtExpenseClaimRepository : RepositoryBase<DtExpenseClaim>, IDtExpenseClaimRepository
    {
        public DtExpenseClaimRepository(RepositoryContext repositoryContext, IApplicationReadDbConnection readDbConnection, IApplicationWriteDbConnection writeDbConnection)
   : base(repositoryContext, writeDbConnection, readDbConnection)
        {
        }

        public void CreateDtExpenseClaim(DtExpenseClaim dtExpenseClaim)
        {
            Create(dtExpenseClaim);
        }

        public void DeleteDtExpenseClaim(DtExpenseClaim dtExpenseClaim)
        {
            Delete(dtExpenseClaim);
        }

        public async Task<IEnumerable<DtExpenseClaim>> GetAllDtExpenseClaimAsync()
        {
            {
                return await FindAll()
                .OrderBy(mc => mc.ECItemID)
                 .ToListAsync();
            }
        }

        public async Task<IEnumerable<DtExpenseClaim>> GetAllDtExpenseClaimWithDetailsAsync()
        {
            return await FindAll()
            .Include(mf => mf.MstExpenseCategory)
            .ToListAsync();
        }

        public async Task<List<DtExpenseClaim>> GetDtExpenseClaimByIdAsync(long? eCID)
        {
            return await FindByCondition(mstEC => mstEC.ECID.Equals(eCID))
                .Include(mex => mex.MstExpenseCategory)
                .ToListAsync();
        }

        public async Task<DtExpenseClaim> GetTopDtExpenseClaimByIdAsync(long? eCID)
        {
            return await FindByCondition(mstEC => mstEC.ECID.Equals(eCID))
                .OrderBy(mc => mc.ECItemID)
                .FirstOrDefaultAsync();
        }

        public Task<DtExpenseClaim> GetDtExpenseClaimByIdAsync(int? ECItemID)
        {
            throw new NotImplementedException();
        }

        public void UpdateDtExpenseClaim(DtExpenseClaim dtExpenseClaim)
        {
            Update(dtExpenseClaim);
        }
    }
}
