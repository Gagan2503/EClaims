using EClaimsEntities.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EClaimsRepository.Contracts
{
    public interface IMstTaxClassRepository : IRepositoryBase<MstTaxClass>
    {
        Task<IEnumerable<MstTaxClass>> GetAllTaxClassAsync(string type = "all");
        Task<IEnumerable<MstTaxClass>> GetAllTaxClassDataAsync();
        IEnumerable<MstTaxClass> GetAllTaxClass();
        Task<MstTaxClass> GetTaxClassByIdAsync(int? taxClassId);
        Task<MstTaxClass> GetTaxClassWithDetailsAsync(int? taxClassId);
        Task<int> InsertApprovalMatrixForTaxClass(int? taxClassId, int? userID);
        Task<MstTaxClass> CreateAndReturnTaxClass(MstTaxClass mstTaxClass);
        bool ValidateTaxClass(MstTaxClass mstTaxClass, string mode);
        MstTaxClass Authenticate(MstTaxClass mstTaxClass, string mode);
        void CreateTaxClass(MstTaxClass mstTaxClass);
        Task<MstTaxClass> UpdateAndReturnTaxClass(MstTaxClass updatedTaxClass);
        void DeleteTaxClass(MstTaxClass mstTaxClass);
    }
}
