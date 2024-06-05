using EClaimsEntities.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EClaimsRepository.Contracts
{
    public interface IMstBankDetailsRepository : IRepositoryBase<MstBankDetails>
    {
        Task<MstBankDetails> GetBankDetailsByUserIdAsync(int userId);
        void CreateBankDetails(MstBankDetails mstBankDetails);
    }
}
