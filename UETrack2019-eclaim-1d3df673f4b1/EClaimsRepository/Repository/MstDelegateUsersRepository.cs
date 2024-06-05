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
    public class MstDelegateUsersRepository : RepositoryBase<MstDelegateUsers>, IMstDelegateUsersRepository
    {
        public MstDelegateUsersRepository(RepositoryContext repositoryContext, IApplicationReadDbConnection readDbConnection, IApplicationWriteDbConnection writeDbConnection)
   : base(repositoryContext, writeDbConnection, readDbConnection)
        {

        }

        public void CreateDelegateUser(MstDelegateUsers mstAlternateApprovers)
        {
            Create(mstAlternateApprovers);
        }

        public async Task<MstDelegateUsers> GetDelegateUserByUserIdAsync(int userId)
        {
            return FindByCondition(a => a.UserId == userId).FirstOrDefault();
        }
    }
}
