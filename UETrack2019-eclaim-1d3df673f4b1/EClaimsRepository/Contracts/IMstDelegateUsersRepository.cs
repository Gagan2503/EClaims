using EClaimsEntities.Models;
using System.Threading.Tasks;

namespace EClaimsRepository.Contracts
{
    public interface IMstDelegateUsersRepository : IRepositoryBase<MstDelegateUsers>
    {
        Task<MstDelegateUsers> GetDelegateUserByUserIdAsync(int userId);
        void CreateDelegateUser(MstDelegateUsers mstDelegateUsers);
    }
}
