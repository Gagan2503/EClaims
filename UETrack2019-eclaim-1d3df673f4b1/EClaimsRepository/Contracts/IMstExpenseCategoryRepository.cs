using EClaimsEntities.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EClaimsRepository.Contracts
{
    public interface IMstExpenseCategoryRepository : IRepositoryBase<MstExpenseCategory>
    {
        Task<IEnumerable<MstExpenseCategory>> GetAllExpenseCategoriesAsync(string type = "all");
        Task<IEnumerable<MstExpenseCategory>> GetAllExpenseCategoriesWithTypesAsync();
        Task<IEnumerable<MstExpenseCategory>> GetAllExpenseCategoriesByClaimTypesAsync( string claimType, string type = "all");
        Task<MstExpenseCategory> GetExpenseCategoryByIdAsync(int? expenseCategoryId);
        Task<MstExpenseCategory> GetExpenseCategoryWithTypesByIdAsync(int? expenseCategoryId);
        //IEnumerable<MstExpenseCategory> ExpenseCategoriesByClaimType(int? claimTypeId);
        Task<IEnumerable<MstExpenseCategory>> ExpenseCategoriesByClaimTypeID(int? claimTypeId);
        //Task<IEnumerable<MstExpenseCategory>> ExpenseCategoriesByClaimType(string claimType);
        Task<MstExpenseCategory> ExpenseCategoriesByClaimType(string claimType);
        bool ValidateExpenseCategory(MstExpenseCategory mstExpenseCategory, string mode);
        MstExpenseCategory Authenticate(MstExpenseCategory mstExpenseCategory, string mode);
        void CreateExpenseCategory(MstExpenseCategory mstExpenseCategory);
        void UpdateExpenseCategory(MstExpenseCategory mstExpenseCategory);
        void DeleteExpenseCategory(MstExpenseCategory mstExpenseCategory);
    }
}
