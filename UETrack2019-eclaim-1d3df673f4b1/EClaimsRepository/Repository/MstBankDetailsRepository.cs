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
    public class MstBankDetailsRepository : RepositoryBase<MstBankDetails>, IMstBankDetailsRepository
    {
        public MstBankDetailsRepository(RepositoryContext repositoryContext, IApplicationReadDbConnection readDbConnection, IApplicationWriteDbConnection writeDbConnection)
   : base(repositoryContext, writeDbConnection, readDbConnection)
        {

        }

        public void CreateBankDetails(MstBankDetails mstBankDetails)
        {
            Create(mstBankDetails);
        }

        public async Task<MstBankDetails> GetBankDetailsByUserIdAsync(int userId)
        {
            return FindByCondition(a => a.UserId == userId).FirstOrDefault();
        }
    }
}
