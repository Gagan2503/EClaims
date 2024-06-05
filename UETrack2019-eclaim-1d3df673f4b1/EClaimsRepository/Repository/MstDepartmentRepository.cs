using Dapper;
using EClaimsEntities;
using EClaimsEntities.Models;
using EClaimsRepository.Contracts;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;

namespace EClaimsRepository.Repository
{
    public class MstDepartmentRepository : RepositoryBase<MstDepartment>, IMstDepartmentRepository
    {
        public MstDepartmentRepository(RepositoryContext repositoryContext, IApplicationReadDbConnection readDbConnection, IApplicationWriteDbConnection writeDbConnection)
   : base(repositoryContext, writeDbConnection, readDbConnection)
        {
        }

        public MstDepartment CreateAndReturnDepartment(MstDepartment mstDepartment)
        {
            return CreateAndReturnEntity(mstDepartment);
        }

        public void CreateDepartment(MstDepartment mstDepartment)
        {
            CreateAndReturnDepartment(mstDepartment);
        }



        public void DeleteDepartment(MstDepartment mstDepartment)
        {
            Delete(mstDepartment);
        }



        public async Task<IEnumerable<MstDepartment>> GetAllDepartmentAsync(string type="all")
        {
            if(type == "all")
            {
                return await FindAll()
               .OrderBy(md => md.Department)
               .ToListAsync();
            }
            else
            {
                return await FindByCondition(md => md.IsActive)
              .OrderBy(md => md.Department)
              .ToListAsync();
            }
        }


        public IEnumerable<MstDepartment> GetAllDepartment()
        {
            return FindAll()
                .OrderBy(md => md.Department)
                .ToList();
        }

        public async Task<MstDepartment> GetDepartmentByIdAsync(int? departmentId)
        {
            return await FindByCondition(mstDepartment => mstDepartment.DepartmentID.Equals(departmentId))
                    .FirstOrDefaultAsync();
        }

        public async Task<MstDepartment> GetDepartmentWithDetailsAsync(int? departmentId)
        {
            return await FindByCondition(mstDepartment => mstDepartment.DepartmentID.Equals(departmentId))
               .Include(mf => mf.Facilities)
               .FirstOrDefaultAsync();
        }


        public void UpdateDepartment(MstDepartment mstDepartment)
        {
            Update(mstDepartment);
        }

        public bool ValidateDepartment(MstDepartment mstDepartment,string mode)
        {

            var department = Authenticate(mstDepartment,mode);

            if (department is null)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        public MstDepartment Authenticate(MstDepartment mstDepartment,string mode)
        {
            if(mode == "create")
            {
                return FindByCondition(md => md.Department.Equals(mstDepartment.Department))
                   .FirstOrDefault();
            }
            else
            {
                return FindByCondition(md => (md.Department.Equals(mstDepartment.Department)) && md.DepartmentID!=mstDepartment.DepartmentID)
                   .FirstOrDefault();
            }
            
        }

        public async Task<int> InsertApprovalMatrixForDepartment(int? departmentID, int? userID)
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

                    parameters.Add("DeptID", departmentID, DbType.Int32);
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
    }
}
