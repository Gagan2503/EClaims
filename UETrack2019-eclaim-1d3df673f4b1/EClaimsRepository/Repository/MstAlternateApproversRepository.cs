using EClaimsEntities;
using EClaimsEntities.Models;
using EClaimsRepository.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EClaimsRepository.Repository
{
    public class MstAlternateApproversRepository : RepositoryBase<MstAlternateApprovers>, IMstAlternateApproversRepository
    {
        public MstAlternateApproversRepository(RepositoryContext repositoryContext, IApplicationReadDbConnection readDbConnection, IApplicationWriteDbConnection writeDbConnection)
   : base(repositoryContext, writeDbConnection, readDbConnection)
        {

        }

        public void CreateAlternateApprover(MstAlternateApprovers mstAlternateApprovers)
        {
            Create(mstAlternateApprovers);
        }

        public async Task<MstAlternateApprovers> GetAlternateApproverByUserIdAsync(int userId)
        {
            return FindByCondition(a => a.UserId == userId).FirstOrDefault();
        }
    }
}
