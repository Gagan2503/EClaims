using EClaimsEntities.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EClaimsRepository.Contracts
{
    public interface IDtApprovalMatrixRepository : IRepositoryBase<DtApprovalMatrix>
    {
        //Task<IEnumerable<DtApprovalMatrix>> GetAllExpenseCategoriesAsync();
        //Task<IEnumerable<DtApprovalMatrix>> GetAllExpenseCategoriesWithTypesAsync();
        //Task<DtApprovalMatrix> GetDtApprovalMatrixByAMIDAsync(int? aMID);

        IEnumerable<DtApprovalMatrix> GetDtApprovalMatrixByAMID(int? aMID);

        void CreateDtApprovalMatrixAsync(DtApprovalMatrix dtApprovalMatrix);
        //void DeleteDtApprovalMatrixAsync(int aMID);

        Task DeleteDtApprovalMatrixAsync(int aMID);
        void CreateDtApprovalMatrix(DtApprovalMatrix dtApprovalMatrix);
        void UpdateDtApprovalMatrix(DtApprovalMatrix dtApprovalMatrix);
        void DeleteDtApprovalMatrix(DtApprovalMatrix dtApprovalMatrix);
    }
}
