using EClaimsEntities;
using EClaimsEntities.Models;
using EClaimsRepository.Contracts;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EClaimsRepository.Repository
{

    public class DtPVCClaimDraftRepository : RepositoryBase<DtPVCClaimDraft>, IDtPVCClaimDraftRepository
    {
        public DtPVCClaimDraftRepository(RepositoryContext repositoryContext, IApplicationReadDbConnection readDbConnection, IApplicationWriteDbConnection writeDbConnection)
       : base(repositoryContext, writeDbConnection, readDbConnection)
        {
        }

        public void CreateDtPVCClaimDraft(DtPVCClaimDraft dtPVCClaimDraft)
        {
            Create(dtPVCClaimDraft);
        }

        public void DeleteDtPVCClaimDraft(DtPVCClaimDraft dtPVCClaimDraft)
        {
            Delete(dtPVCClaimDraft);
        }

        public async Task<IEnumerable<DtPVCClaimDraft>> GetAllDtPVCClaimDraftAsync()
        {
            {
                return await FindAll()
                .OrderBy(mc => mc.PVCCItemID)
                 .ToListAsync();
            }
        }

        public async Task<IEnumerable<DtPVCClaimDraft>> GetAllDtPVCClaimWithDetailsDraftAsync()
        {
            return await FindAll()
            .Include(mf => mf.MstExpenseCategory)
            .ToListAsync();
        }

        public Task<DtPVCClaimDraft> GetDtPVCClaimDraftByIdAsync(int? pVCCItemID)
        {
            throw new NotImplementedException();
        }

        public async Task<List<DtPVCClaimDraft>> GetDtPVCClaimDraftByIdAsync(long? pVCCID)
        {
            return await FindByCondition(mstEC => mstEC.PVCCID.Equals(pVCCID))
                .Include(mex => mex.MstExpenseCategory)
                .OrderBy(mstEC => mstEC.OrderBy)
                .ToListAsync();
        }

        public void UpdateDtPVCClaimDraft(DtPVCClaimDraft dtPVCClaimDraft)
        {
            Update(dtPVCClaimDraft);
        }
    }

}
