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
    public class DtHRPVCClaimDraftRepository : RepositoryBase<DtHRPVCClaimDraft>, IDtHRPVCClaimDraftRepository
    {
        public DtHRPVCClaimDraftRepository(RepositoryContext repositoryContext, IApplicationReadDbConnection readDbConnection, IApplicationWriteDbConnection writeDbConnection)
   : base(repositoryContext, writeDbConnection, readDbConnection)
        {
        }


        public async Task<List<DtHRPVCClaimDraft>> GetDtHRPVCClaimByIdAsync(long? hRPVCCID)
        {
            return await FindByCondition(mstEC => mstEC.HRPVCCID.Equals(hRPVCCID))
                .ToListAsync();
        }

        
    }
}
