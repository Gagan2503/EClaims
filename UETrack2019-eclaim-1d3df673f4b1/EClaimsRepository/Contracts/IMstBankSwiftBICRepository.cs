using EClaimsEntities.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EClaimsRepository.Contracts
{
    public interface IMstBankSwiftBICRepository : IRepositoryBase<MstBankSwiftBIC>
    {
        Task<MstBankSwiftBIC> GetBankSwiftBICByBankCodeAsync(long bankCode);
        Task<IEnumerable<MstBankSwiftBIC>> GetAllBankSwiftBICAsync();
    }
}
