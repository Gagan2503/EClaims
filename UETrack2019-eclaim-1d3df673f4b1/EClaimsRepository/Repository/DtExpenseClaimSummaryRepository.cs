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
    class DtExpenseClaimSummaryRepository : RepositoryBase<DtExpenseClaimSummary>, IDtExpenseClaimSummaryRepository
    {
        public DtExpenseClaimSummaryRepository(RepositoryContext repositoryContext, IApplicationReadDbConnection readDbConnection, IApplicationWriteDbConnection writeDbConnection)
   : base(repositoryContext, writeDbConnection, readDbConnection)
        {
        }
        public void CreateDtExpenseClaimSummary(DtExpenseClaimSummary dtExpenseClaimSummary)
        {
            Create(dtExpenseClaimSummary);
        }

        public void UpdateDtExpenseClaimSummary(DtExpenseClaimSummary dtExpenseClaimSummary)
        {
            Update(dtExpenseClaimSummary);
        }

        public void DeleteDtExpenseClaimSummary(DtExpenseClaimSummary dtExpenseClaimSummary)
        {
            Delete(dtExpenseClaimSummary);
        }

        public async Task<List<DtExpenseClaimSummary>> GetDtExpenseClaimSummaryByIdAsync(long? eCID)
        {
            return await FindByCondition(mstEC => mstEC.ECID.Equals(eCID))
               .ToListAsync();
        }
    }
}
