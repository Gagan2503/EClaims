using Dapper;
using EClaimsEntities;
using EClaimsEntities.Models;
using EClaimsRepository.Contracts;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EClaimsRepository.Repository
{
    public class MstHRPVCClaimDraftRepository : RepositoryBase<MstHRPVCClaimDraft>, IMstHRPVCClaimDraftRepository
    {
        public RepositoryContext _context { get; }
        public MstHRPVCClaimDraftRepository(RepositoryContext repositoryContext, IApplicationReadDbConnection readDbConnection, IApplicationWriteDbConnection writeDbConnection)
   : base(repositoryContext, writeDbConnection, readDbConnection)
        {
            _context = repositoryContext;
        }

        public void DeleteHRPVCClaim(MstHRPVCClaimDraft mstHRPVCClaim)
        {
            Delete(mstHRPVCClaim);
        }

        public async Task<MstHRPVCClaimDraft> GetHRPVCClaimByIdAsync(long? hRPVCCID)
        {
            return await FindByCondition(mstEC => mstEC.HRPVCCID.Equals(hRPVCCID))
                .Include(mu => mu.MstUser)
             .Include(mf => mf.MstFacility)
              .Include(md => md.MstDepartment)
                    .FirstOrDefaultAsync();
        }

     
    }
}
// public async Task<MstHRPVCClaim> GetHRPVCClaimByIdAsync(int? HRPVCCID)
// {
//    return await FindByCondition(mstEC => mstEC.HRPVCCID.Equals(HRPVCCID))
//.FirstOrDefaultAsync();
// }








