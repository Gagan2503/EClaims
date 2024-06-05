using Dapper;
using EClaimsEntities;
using EClaimsEntities.Models;
using EClaimsRepository.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;

namespace EClaimsRepository.Repository
{
    public class MstTaxClassRepository : RepositoryBase<MstTaxClass>, IMstTaxClassRepository
    {
        public MstTaxClassRepository(RepositoryContext repositoryContext, IApplicationReadDbConnection readDbConnection, IApplicationWriteDbConnection writeDbConnection)
   : base(repositoryContext, writeDbConnection, readDbConnection)
        {
        }

        public async Task<MstTaxClass> CreateAndReturnTaxClass(MstTaxClass mstTaxClass)
        {
            RepositoryContext.Connection.Open();
            using (var transaction = RepositoryContext.Connection.BeginTransaction())
            {
                try
                {
                    RepositoryContext.Database.UseTransaction(transaction as DbTransaction);
                    if (mstTaxClass.IsDefault)
                    {
                        // Assuming UpdateIsDefaultIsOptionalTaxClassColumns returns a Task<int>
                        var res = await UpdateIsDefaultIsOptionalTaxClassColumns(transaction as DbTransaction);
                    }

                    mstTaxClass.ApprovalDate = DateTime.Now;
                    mstTaxClass.ApprovalStatus = 1;
                    mstTaxClass.ApprovalBy = 1;

                    await RepositoryContext.mstTaxClass.AddAsync(mstTaxClass);
                    await RepositoryContext.SaveChangesAsync();
                    // Create and return the tax class entity
                    //var createdEntity = CreateAndReturnEntity(mstTaxClass);

                    // Commit the EF Core transaction if everything is successful
                    //await transaction.CommitAsync();
                    transaction.Commit();
                    return null;

                    //return createdEntity;
                }
                catch (Exception ex)
                {
                    // Log the exception, and rollback the EF Core transaction
                    transaction.Rollback();
                    throw new Exception("Failed to create and return tax class.", ex);
                }
            }
        }

        public async Task<MstTaxClass> UpdateAndReturnTaxClass(MstTaxClass updatedTaxClass)
        {
            using (var transaction = await RepositoryContext.Database.BeginTransactionAsync())
            {
                try
                {
                    if (updatedTaxClass.IsDefault)
                    {
                        // Assuming UpdateIsDefaultIsOptionalTaxClassColumns returns a Task<int>
                        var res = await UpdateIsDefaultIsOptionalTaxClassColumns(transaction.GetDbTransaction());
                    }

                    var existingTaxClass = await GetTaxClassByIdAsync(updatedTaxClass.TaxClassID);// await RepositoryContext.MstTaxClass.FindAsync(updatedTaxClass.TaxClassID);

                    if (existingTaxClass == null)
                    {
                        throw new InvalidOperationException($"Tax class with ID {updatedTaxClass.TaxClassID} not found.");
                    }

                    // Update the properties of the existing tax class
                    existingTaxClass.IsDefault = updatedTaxClass.IsDefault;
                    existingTaxClass.IsOptional = updatedTaxClass.IsOptional;
                    existingTaxClass.OptionalTaxClassID = updatedTaxClass.OptionalTaxClassID;
                    existingTaxClass.Code = updatedTaxClass.Code;
                    existingTaxClass.TaxClass = updatedTaxClass.TaxClass;
                    existingTaxClass.Description = updatedTaxClass.Description;
                    existingTaxClass.ModifiedBy = updatedTaxClass.ModifiedBy;
                    existingTaxClass.ModifiedDate = updatedTaxClass.ModifiedDate;
                    // ... Update other properties as needed ...

                    // Update the entity in the database
                    RepositoryContext.mstTaxClass.Update(existingTaxClass);
                    await RepositoryContext.SaveChangesAsync();

                    // Commit the EF Core transaction if everything is successful
                    await transaction.CommitAsync();

                    return existingTaxClass;
                }
                catch (Exception ex)
                {
                    // Log the exception, and rollback the EF Core transaction
                    await transaction.RollbackAsync();
                    throw new Exception("Failed to update and return tax class.", ex);
                }
            }
        }


        public void CreateTaxClass(MstTaxClass mstTaxClass)
        {
            CreateAndReturnTaxClass(mstTaxClass);
        }



        public void DeleteTaxClass(MstTaxClass mstTaxClass)
        {
            Delete(mstTaxClass);
        }



        public async Task<IEnumerable<MstTaxClass>> GetAllTaxClassAsync(string type = "all")
        {
            if (type == "all")
            {
                return await FindAll()
               .OrderBy(md => md.TaxClass)
               .ToListAsync();
            }
            else
            {
                return await FindByCondition(md => md.IsActive)
              .OrderBy(md => md.TaxClass)
              .ToListAsync();
            }
        }

        public async Task<IEnumerable<MstTaxClass>> GetAllTaxClassDataAsync()
        {
            var procedureName = "GetAllTaxClass";
            RepositoryContext.Connection.Open();
            try
            {
                var queries = await RepositoryContext.Connection.QueryAsync<MstTaxClass>(procedureName, commandType: CommandType.StoredProcedure);
                return queries.ToList();
            }
            catch (Exception ex)
            {
                throw;
            }
            finally
            {
                RepositoryContext.Connection.Close();
            }
        }


        public IEnumerable<MstTaxClass> GetAllTaxClass()
        {
            return FindAll()
                .OrderBy(md => md.TaxClass)
                .ToList();
        }

        public async Task<MstTaxClass> GetTaxClassByIdAsync(int? taxClassId)
        {
            return await FindByCondition(MstTaxClass => MstTaxClass.TaxClassID.Equals(taxClassId))
                    .FirstOrDefaultAsync();
        }

        public async Task<MstTaxClass> GetTaxClassWithDetailsAsync(int? taxClassId)
        {
            return await FindByCondition(MstTaxClass => MstTaxClass.TaxClassID.Equals(taxClassId))
               .FirstOrDefaultAsync();
        }




        public bool ValidateTaxClass(MstTaxClass mstTaxClass, string mode)
        {

            var taxclass = Authenticate(mstTaxClass, mode);

            if (taxclass is null)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        public MstTaxClass Authenticate(MstTaxClass mstTaxClass, string mode)
        {
            if (mode == "create")
            {
                return FindByCondition(md => md.TaxClass.Equals(mstTaxClass.TaxClass))
                   .FirstOrDefault();
            }
            else
            {
                return FindByCondition(md => (md.TaxClass.Equals(mstTaxClass.TaxClass)) && md.TaxClassID != mstTaxClass.TaxClassID)
                   .FirstOrDefault();
            }

        }

        public async Task<int> InsertApprovalMatrixForTaxClass(int? taxClassID, int? userID)
        {
            var mstMatrixId = 0;

            RepositoryContext.Connection.Open();
            using (var transaction = RepositoryContext.Connection.BeginTransaction())
            {
                try
                {
                    RepositoryContext.Database.UseTransaction(transaction as DbTransaction);
                    //Check if Department Exists (By Name)
                    /*
                    bool DepartmentExists = await _dbContext.Departments.AnyAsync(a => a.Name == employeeDto.Department.Name);
                    if (DepartmentExists)
                    {
                        throw new Exception("Department Already Exists");
                    }
                    */

                    var parameters = new DynamicParameters();

                    parameters.Add("TaxClassID", taxClassID, DbType.Int32);
                    parameters.Add("UserID", userID, DbType.Int32);

                    var addMstDeptQuery = "AddOrEditMstApprovalMatrix";
                    //var mstClaimId = await _context.Connection.QuerySingleAsync<int>(addMstClaimQuery, transaction: transaction);
                    mstMatrixId = await RepositoryContext.Connection.QueryFirstOrDefaultAsync<int>(addMstDeptQuery, parameters, transaction: transaction, commandType: CommandType.StoredProcedure);
                    transaction.Commit();
                    return mstMatrixId;
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    throw;
                }
                finally
                {
                    RepositoryContext.Connection.Close();
                }
            }
        }

        public async Task<int> UpdateIsDefaultIsOptionalTaxClassColumns(DbTransaction transaction)
        {
            try
            {
                var parameters = new DynamicParameters();
                var mstTaxClassQuery = "UpdateIsDefaultIsOptionalTaxClassColumns";
                var resId = await RepositoryContext.Connection.QueryFirstOrDefaultAsync<int>(mstTaxClassQuery, parameters, transaction: transaction, commandType: CommandType.StoredProcedure);
                return resId;
            }
            catch (Exception ex)
            {
                // Log the exception if needed
                throw new Exception("Failed to update columns.", ex);
            }
        }

    }
}
