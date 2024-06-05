using EClaimsEntities.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EClaimsRepository.Contracts
{
    public interface IMstApprovalMatrixRepository : IRepositoryBase<MstApprovalMatrix>
    {
        Task<IEnumerable<MstApprovalMatrix>> GetAllApprovalMatrixAsync();
        Task<IEnumerable<MstApprovalMatrix>> GetAllApprovalMatrixWithScreensAsync();
        Task<MstApprovalMatrix> GetApprovalMatrixByIdAsync(int? AMID);
        IEnumerable<MstApprovalMatrix> GetApprovalMatrixByScreens(int? screenId);

        void UpdateMstApprovalMatrixAsync(MstApprovalMatrix mstApprovalMatrix);
        void CreateApprovalMatrix(MstApprovalMatrix mstApprovalMatrix);
        void UpdateApprovalMatrix(MstApprovalMatrix mstApprovalMatrix);
        void DeleteApprovalMatrix(MstApprovalMatrix mstApprovalMatrix);
    }
}
