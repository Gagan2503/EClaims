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
    public class MstBankSwiftBICRepository : RepositoryBase<MstBankSwiftBIC>, IMstBankSwiftBICRepository
    {
        public MstBankSwiftBICRepository(RepositoryContext repositoryContext, IApplicationReadDbConnection readDbConnection, IApplicationWriteDbConnection writeDbConnection)
  : base(repositoryContext, writeDbConnection, readDbConnection)
        {
        }

        public async Task<MstBankSwiftBIC> GetBankSwiftBICByBankCodeAsync(long bankCode)
        {
            return FindByCondition(a => a.BankCode == bankCode).FirstOrDefault();
        }

        public async Task<IEnumerable<MstBankSwiftBIC>> GetAllBankSwiftBICAsync()
        {
            return await FindAll()
              .OrderBy(mu => mu.BankName)
              .ToListAsync();
        }
    }
}
