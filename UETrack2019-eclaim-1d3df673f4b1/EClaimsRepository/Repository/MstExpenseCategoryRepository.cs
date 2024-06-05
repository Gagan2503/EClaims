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
    public class MstExpenseCategoryRepository : RepositoryBase<MstExpenseCategory>, IMstExpenseCategoryRepository
    {
        public MstExpenseCategoryRepository(RepositoryContext repositoryContext, IApplicationReadDbConnection readDbConnection, IApplicationWriteDbConnection writeDbConnection)
   : base(repositoryContext, writeDbConnection, readDbConnection)
        {
        }

        public void CreateExpenseCategory(MstExpenseCategory mstExpenseCategory)
        {
            Create(mstExpenseCategory);
        }

        public void UpdateExpenseCategory(MstExpenseCategory mstExpenseCategory)
        {
            Update(mstExpenseCategory);
        }

        public void DeleteExpenseCategory(MstExpenseCategory mstExpenseCategory)
        {
            Delete(mstExpenseCategory);
        }

        public async Task<IEnumerable<MstExpenseCategory>> ExpenseCategoriesByClaimTypeID(int? claimTypeId)
        {
            return await FindByCondition(a => a.ClaimTypeID.Equals(claimTypeId))
                .Include(ct => ct.MstClaimType)
                .ToListAsync();
        }

        //public async Task<IEnumerable<MstExpenseCategory>> ExpenseCategoriesByClaimType(string claimType)
        //{
        //    return await FindByCondition(a => a.MstClaimType.ClaimType.Equals(claimType))
        //        .Include(ct => ct.MstClaimType)
        //        .OrderBy(a => a.ModifiedDate)
        //        .ToListAsync();
        //}

        public async Task<MstExpenseCategory> ExpenseCategoriesByClaimType(string claimType)
        {
            return await FindByCondition(a => a.MstClaimType.ClaimType.Equals(claimType) && a.IsActive)
                .Include(ct => ct.MstClaimType)
                .OrderBy(a => a.ModifiedDate)
                .FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<MstExpenseCategory>> GetAllExpenseCategoriesByClaimTypesAsync(string claimType, string type = "all")
        {
            if (type == "all")
            {
                return await FindByCondition(a => a.MstClaimType.ClaimType.Equals(claimType))
               .Include(ct => ct.MstClaimType)
               .OrderBy(ec => ec.Description)
               .ToListAsync();
            }
            else
            {
                return await FindByCondition(a => a.MstClaimType.ClaimType.Equals(claimType) && a.IsActive)
               .Include(ct => ct.MstClaimType)
               .OrderBy(ec => ec.Description)
               .ToListAsync();
            }
        }

        public async Task<IEnumerable<MstExpenseCategory>> GetAllExpenseCategoriesAsync(string type="all")
        {
            if (type == "all")
            {
                return await FindAll()
               .OrderBy(ec => ec.Description)
               .ToListAsync();
            }
            else
            {
                return await FindByCondition(mec => mec.IsActive)
                 .OrderBy(ec => ec.Description)
                 .ToListAsync();
            }
        }

        public async Task<IEnumerable<MstExpenseCategory>> GetAllExpenseCategoriesWithTypesAsync()
        {
            return await FindAll()
                .Include(ct => ct.MstClaimType)
                .Include(cs => cs.MstCostStructure)
                .Include(cst => cst.MstCostType)
                .ToListAsync();
        }

        public async Task<MstExpenseCategory> GetExpenseCategoryByIdAsync(int? expenseCategoryId)
        {
            return await FindByCondition(exCategory => exCategory.ExpenseCategoryID.Equals(expenseCategoryId))
                    .FirstOrDefaultAsync();
        }

        public async Task<MstExpenseCategory> GetExpenseCategoryWithTypesByIdAsync(int? expenseCategoryId)
        {
            return await FindByCondition(exCategory => exCategory.ExpenseCategoryID.Equals(expenseCategoryId))
                    .Include(ct => ct.MstClaimType)
                    .Include(cs => cs.MstCostStructure)
                    .Include(cst => cst.MstCostType)
                    .FirstOrDefaultAsync();
        }

        public bool ValidateExpenseCategory(MstExpenseCategory mstExpenseCategory, string mode)
        {
            var expenseCategory = Authenticate(mstExpenseCategory, mode);

            if (expenseCategory is null)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        public MstExpenseCategory Authenticate(MstExpenseCategory mstExpenseCategory, string mode)
        {
            if (mode == "create")
            {
                return FindByCondition(mec => mec.CategoryCode.Equals(mstExpenseCategory.CategoryCode) || mec.Description.Equals(mstExpenseCategory.Description))
                  .FirstOrDefault();
            }
            else
            {
                return FindByCondition(mec => (mec.CategoryCode.Equals(mstExpenseCategory.CategoryCode) || mec.Description.Equals(mstExpenseCategory.Description)) && mec.ExpenseCategoryID != mstExpenseCategory.ExpenseCategoryID)
                    .FirstOrDefault();
            }
        }
    }
}
