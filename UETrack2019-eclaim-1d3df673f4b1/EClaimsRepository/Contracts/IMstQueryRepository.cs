using EClaimsEntities.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EClaimsRepository.Contracts
{
    public interface IMstQueryRepository : IRepositoryBase<MstQuery>
    {
        Task CreateQuery(MstQuery mstQuery);
        void UpdateQuery(MstQuery mstQuery);
        void DeleteQuery(MstQuery mstQuery);
        Task<List<MstQuery>> GetAllClaimsQueriesAsync(int? UserId, long? Mcid, string Module);
    }
}
