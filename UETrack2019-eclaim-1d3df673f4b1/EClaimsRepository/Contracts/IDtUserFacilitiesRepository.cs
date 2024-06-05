using EClaimsEntities.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EClaimsRepository.Contracts
{
    public interface IDtUserFacilitiesRepository : IRepositoryBase<DtUserFacilities>
    {
        Task<IEnumerable<MstFacility>> GetAllFacilitiesByUserIdAsync(int? userId);
    }
}
