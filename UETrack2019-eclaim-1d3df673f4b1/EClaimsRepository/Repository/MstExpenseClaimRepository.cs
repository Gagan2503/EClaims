using Dapper;
using EClaimsEntities;
using EClaimsEntities.Models;
using EClaimsRepository.Contracts;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EClaimsRepository.Repository
{
    public class MstExpenseClaimRepository : RepositoryBase<MstExpenseClaim>, IMstExpenseClaimRepository
    {
        public RepositoryContext _context { get; }
        public MstExpenseClaimRepository(RepositoryContext repositoryContext, IApplicationReadDbConnection readDbConnection, IApplicationWriteDbConnection writeDbConnection)
   : base(repositoryContext, writeDbConnection, readDbConnection)
        {
            _context = repositoryContext;
        }

        public void CreateExpenseClaim(MstExpenseClaim mstExpenseClaim)
        {
            Create(mstExpenseClaim);
        }

        public void DeleteExpenseClaim(MstExpenseClaim mstExpenseClaim)
        {
            Delete(mstExpenseClaim);
        }

        /*       public IEnumerable<MstExpenseClaim> ExpenseClaimByClaimType(int? claimTypeId)
               {
                   throw new NotImplementedException();
               }
        */
        public async Task<IEnumerable<MstExpenseClaim>> GetAllExpenseClaimAsync()
        {
            return await FindAll()
            .OrderBy(mc => mc.ECNo)
             .ToListAsync();
        }

        public async Task<IEnumerable<CustomClaim>> GetAllExpenseClaimWithDetailsAsync(string expenseID,int userID, int facilityID, int statusID, string fromDate, string toDate)
        {
            var procedureName = "GetAllExpenseClaimWithDetails";
            var parameters = new DynamicParameters();
            parameters.Add("UserID", userID, DbType.Int32, ParameterDirection.Input);
            parameters.Add("ExpenseID", expenseID, DbType.String, ParameterDirection.Input);
            parameters.Add("FacilityID", facilityID, DbType.Int32, ParameterDirection.Input);
            parameters.Add("StatusID", statusID, DbType.Int32, ParameterDirection.Input);
            parameters.Add("FromDate", fromDate, DbType.String, ParameterDirection.Input);
            parameters.Add("ToDate", toDate, DbType.String, ParameterDirection.Input);
            //using ()
            //{
            RepositoryContext.Connection.Open();
            try
            {
                var queries = await RepositoryContext.Connection.QueryAsync<CustomClaim>(procedureName, parameters, commandType: CommandType.StoredProcedure);
                //var company = await connection.QueryFirstOrDefaultAsync<Company>
                //    (procedureName, parameters, commandType: CommandType.StoredProcedure);
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

        
        public async Task<IEnumerable<MstExpenseClaim>> GetAllExpenseClaimForExportToBankAsync(string id, int facilityID, string fromDate, string toDate)
        {
            if (id == "")
            {
                return await FindByCondition(mc => mc.ApprovalStatus == 9 && mc.FacilityID == facilityID && mc.CreatedDate >= DateTime.ParseExact(fromDate, "dd/MM/yyyy", System.Globalization.CultureInfo.CurrentUICulture.DateTimeFormat) && mc.CreatedDate <= DateTime.ParseExact(toDate, "dd/MM/yyyy", System.Globalization.CultureInfo.CurrentUICulture.DateTimeFormat))
                .Include(mu => mu.MstUser)
                .Include(md => md.MstDepartment)
                .Include(mf => mf.MstFacility)
                .OrderByDescending(m => m.ECNo)
                .ToListAsync();
            }
            else
            {
                return await FindByCondition(mc => mc.ApprovalStatus == 9 && id.Contains(mc.ECID.ToString()))
                 .Include(mu => mu.MstUser)
                 .Include(md => md.MstDepartment)
                 .Include(mf => mf.MstFacility)
                 .OrderByDescending(m => m.ECNo)
                 .ToListAsync();
            }
        }

        public async Task<IEnumerable<MstExpenseClaim>> GetAllExpenseClaimForAPExportAsync(string id, int facilityID, string fromDate, string toDate)
        {
            if (id == "")
            {
                return await FindByCondition(mc => mc.ApprovalStatus == 3 && mc.FacilityID == facilityID && mc.CreatedDate >= DateTime.ParseExact(fromDate, "dd/MM/yyyy", System.Globalization.CultureInfo.CurrentUICulture.DateTimeFormat) && mc.CreatedDate <= DateTime.ParseExact(toDate, "dd/MM/yyyy", System.Globalization.CultureInfo.CurrentUICulture.DateTimeFormat).AddDays(1))
                .Include(mu => mu.MstUser)
                .Include(md => md.MstDepartment)
                .Include(mf => mf.MstFacility)
                .OrderByDescending(m => m.ECNo)
                .ToListAsync();
            }
            else
            {
                return await FindByCondition(mc => mc.ApprovalStatus == 3 && id.Contains(mc.ECID.ToString()))
                 .Include(mu => mu.MstUser)
                 .Include(md => md.MstDepartment)
                 .Include(mf => mf.MstFacility)
                 .OrderByDescending(m => m.ECNo)
                 .ToListAsync();
            }
        }

        public async Task<IEnumerable<MstExpenseClaim>> GetAllExpenseClaimWithDetailsByFacilityIDAsync(int userID, int facilityID)
        {
            //return await FindByCondition(ec => ec.FacilityID.Equals(facilityID) && ec.UserID.Equals(userID)).OrderByDescending(ec=> ec.ECID)
            return await FindByCondition(ec => ec.UserID.Equals(userID)).OrderByDescending(ec => ec.ECID)
            .Include(mu => mu.MstUser)
            .Include(md => md.MstDepartment)
            .Include(mf => mf.MstFacility)
                        .ToListAsync();
        }

        //public async Task<MstExpenseClaim> GetExpenseClaimByIdAsync(int? eCID)
        //{
        //    return await FindByCondition(mstEC => mstEC.ECID.Equals(eCID))
        //.FirstOrDefaultAsync();
        //}

        public void UpdateExpenseClaim(MstExpenseClaim mstExpenseClaim)
        {
            Update(mstExpenseClaim);
        }

        public async Task<MstExpenseClaim> GetExpenseClaimByIdAsync(long? eCID)
        {
            return await FindByCondition(mstEC => mstEC.ECID.Equals(eCID))
                .Include(mu => mu.MstUser)
             .Include(md => md.MstDepartment)
             .Include(mf => mf.MstFacility)
                    .FirstOrDefaultAsync();
        }

        public async Task<int> SaveItems(MstExpenseClaim mstExpenseClaim, List<DtExpenseClaim> dtExpenseClaims, List<DtExpenseClaimSummary> dtExpenseClaimSummaries)
        {
            var mstClaimId = 0;

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

                    parameters.Add("ECID", mstExpenseClaim.ECID, DbType.Int64);
                    parameters.Add("ECNo", mstExpenseClaim.ECNo, DbType.String);
                    parameters.Add("UserID", mstExpenseClaim.UserID, DbType.Int64);
                    parameters.Add("ClaimType", mstExpenseClaim.ClaimType, DbType.String);
                    parameters.Add("GrandTotal", mstExpenseClaim.GrandTotal, DbType.Currency);
                    parameters.Add("TotalAmount", mstExpenseClaim.TotalAmount, DbType.Currency);
                    parameters.Add("Company", mstExpenseClaim.Company, DbType.String);
                    parameters.Add("DepartmentID", mstExpenseClaim.DepartmentID, DbType.Int32);
                    parameters.Add("FacilityID", mstExpenseClaim.FacilityID, DbType.Int32);
                    parameters.Add("CreatedDate", mstExpenseClaim.CreatedDate, DbType.DateTime2);
                    parameters.Add("ModifiedDate", mstExpenseClaim.ModifiedDate, DbType.DateTime2);
                    parameters.Add("CreatedBy", mstExpenseClaim.CreatedBy, DbType.Int32);
                    parameters.Add("ModifiedBy", mstExpenseClaim.ModifiedBy, DbType.Int32);
                    parameters.Add("ApprovalDate", mstExpenseClaim.ApprovalDate, DbType.DateTime2);
                    parameters.Add("ApprovalBy", mstExpenseClaim.ApprovalBy, DbType.Int32);
                    parameters.Add("DelegatedBy", mstExpenseClaim.DelegatedBy, DbType.Int32);
                    parameters.Add("TnC", mstExpenseClaim.TnC, DbType.Boolean);
                    parameters.Add("ApprovalStatus", mstExpenseClaim.ApprovalStatus, DbType.Int32);
                    //Add Department
                    //_context.Connection.QuerySingleAsync
                    //var addMstClaimQuery = $"INSERT INTO MstMileageClaim( MCNo, UserID, TravelMode, Verifier, Approver, FinalApprover, ApprovalStatus, GrandTotal, Company, DepartmentID, FacilityID, CreatedDate, ModifiedDate, CreatedBy, ModifiedBy, ApprovalDate, ApprovalBy, TnC) VALUES('{mstMileageClaim.MCNo}',{mstMileageClaim.UserID},'{mstMileageClaim.TravelMode}', '1,2','2,8',8,1,'{mstMileageClaim.GrandTotal}','{mstMileageClaim.Company}',1,1,'{DateTime.Now}','{DateTime.Now}',2,2,'{DateTime.Now}',2,1);SELECT CAST(SCOPE_IDENTITY() as int)";
                    var addMstClaimQuery = "AddOrEditMstExpenseClaim";
                    //var mstClaimId = await _context.Connection.QuerySingleAsync<int>(addMstClaimQuery, transaction: transaction);
                    mstClaimId = await RepositoryContext.Connection.QueryFirstOrDefaultAsync<int>(addMstClaimQuery, parameters, transaction: transaction, commandType: CommandType.StoredProcedure);

                    //var mstClaimId = await  _writeDbConnection.QuerySingleAsync<int>(addMstClaimQuery, transaction: transaction);
                    //Check if Department Id is not Zero.
                    if (mstClaimId == 0)
                    {
                        throw new Exception("Expense Id");
                    }

                    if (mstExpenseClaim.ECID != 0)
                    {
                        if (dtExpenseClaims.Count > 0)
                        {
                            foreach (var dtExpenseClaims1 in dtExpenseClaims)
                            {
                                dtExpenseClaims1.ECID = mstClaimId;
                                // Setting this value to zero since its identity column
                                dtExpenseClaims1.ECItemID = 0;
                                dtExpenseClaims1.MstExpenseCategory = null;
                                //RepositoryContext.dtExpenseClaim.Update(dtExpenseClaims1);
                                await RepositoryContext.dtExpenseClaim.AddAsync(dtExpenseClaims1);
                                await RepositoryContext.SaveChangesAsync(default);
                            }
                        }
                        if (dtExpenseClaimSummaries != null)
                        {
                            if (dtExpenseClaimSummaries.Count > 0)
                            {
                                foreach (var dtExpenseClaimSummary in dtExpenseClaimSummaries)
                                {
                                    dtExpenseClaimSummary.ECID = mstClaimId;
                                    await RepositoryContext.dtExpenseClaimSummary.AddAsync(dtExpenseClaimSummary);
                                    await RepositoryContext.SaveChangesAsync(default);
                                }
                            }
                        }
                    }
                    else
                    {
                        foreach (var dtExpenseClaims1 in dtExpenseClaims)
                        {
                            dtExpenseClaims1.ECID = mstClaimId;
                            // Setting this value to zero since its identity column
                            dtExpenseClaims1.ECItemID = 0;
                            dtExpenseClaims1.MstExpenseCategory = null;
                            await RepositoryContext.dtExpenseClaim.AddAsync(dtExpenseClaims1);
                            await RepositoryContext.SaveChangesAsync(default);
                            //return Json(new { res = true });
                        }
                        foreach (var dtExpenseClaimSummary in dtExpenseClaimSummaries)
                        {
                            dtExpenseClaimSummary.ECID = mstClaimId;
                            await RepositoryContext.dtExpenseClaimSummary.AddAsync(dtExpenseClaimSummary);
                            await RepositoryContext.SaveChangesAsync(default);
                            //return Json(new { res = true });
                        }
                    }
                    transaction.Commit();
                    return mstClaimId;
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

        public async Task<int> SaveSummary(int eCID, List<DtExpenseClaimSummary> dtExpenseClaimSummaries, MstExpenseClaimAudit mstExpenseClaimAudit)
        {
            var mstClaimId = 0;

            RepositoryContext.Connection.Open();
            using (var transaction = RepositoryContext.Connection.BeginTransaction())
            {
                try
                {
                    RepositoryContext.Database.UseTransaction(transaction as DbTransaction);

                    if (dtExpenseClaimSummaries.Count > 0)
                    {
                        foreach (var dtExpenseClaimSummary in dtExpenseClaimSummaries)
                        {
                            dtExpenseClaimSummary.CItemID = 0;
                            await RepositoryContext.dtExpenseClaimSummary.AddAsync(dtExpenseClaimSummary);
                            await RepositoryContext.SaveChangesAsync(default);
                        }
                    }

                    await RepositoryContext.MstExpenseClaimAudit.AddAsync(mstExpenseClaimAudit);
                    await RepositoryContext.SaveChangesAsync(default);

                    transaction.Commit();
                    return mstClaimId;
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
        public async Task<string> GetVerifierAsync(long? eCID)
        {
            return FindByCondition(mstEC => mstEC.ECID.Equals(eCID))
                   .FirstOrDefaultAsync().GetAwaiter().GetResult().Verifier.ToString();
        }

        public async Task<string> GetApproverAsync(long? eCID)
        {
            return FindByCondition(mstEC => mstEC.ECID.Equals(eCID))
                    .FirstOrDefaultAsync().GetAwaiter().GetResult().Approver.ToString();
        }

        public async Task<string> GetUserApproverAsync(long? mCID)
        {
            return FindByCondition(mstMC => mstMC.ECID.Equals(mCID))
                    .FirstOrDefaultAsync().GetAwaiter().GetResult().UserApprovers.ToString();
        }

        public async Task<string> GetHODApproverAsync(long? mCID)
        {
            return FindByCondition(mstMC => mstMC.ECID.Equals(mCID))
                    .FirstOrDefaultAsync().GetAwaiter().GetResult().HODApprover.ToString();
        }

        public bool ExistsApproval(string ID, int ApprovedStatus, string UserID, string Screen)
        {
            int Id = Convert.ToInt32(ID);
            string value = "";
            if (Screen == "Expense")
            {
                if (ApprovedStatus == 1)
                {
                    value = GetVerifierAsync(Id).GetAwaiter().GetResult().ToString();
                }
                else if (ApprovedStatus == 6)
                {
                    value = GetUserApproverAsync(Id).GetAwaiter().GetResult().ToString();
                }
                else if (ApprovedStatus == 7)
                {
                    value = GetHODApproverAsync(Id).GetAwaiter().GetResult().ToString();
                }
                else
                {
                    value = GetApproverAsync(Id).GetAwaiter().GetResult().ToString();
                }
            }
            bool contains = value.Split(',').Contains(UserID);
            return contains;
        }

        public string GetApproval(string ID, int ApprovedStatus, string UserID, string Screen)
        {
            int Id = Convert.ToInt32(ID);
            string value = "";
            if (Screen == "Expense")
            {
                if (ApprovedStatus == 1)
                {
                    value = GetVerifierAsync(Id).GetAwaiter().GetResult().ToString();
                }
                else if (ApprovedStatus == 6)
                {
                    value = GetUserApproverAsync(Id).GetAwaiter().GetResult().ToString();
                }
                else if (ApprovedStatus == 7)
                {
                    value = GetHODApproverAsync(Id).GetAwaiter().GetResult().ToString();
                }
                else
                {
                    value = GetApproverAsync(Id).GetAwaiter().GetResult().ToString();
                }
            }
            return value;
        }

        public async Task<int> UpdateMstExpenseClaimStatus(long? ECID, int? approvalStatus, int? approvedBy, DateTime? approvedDate, string reason, string verifierIDs, string approverIDs, string userApproverIDs, string hodApprover, bool isAlternateApprover, int? financeStartDay)
        {
            var mstClaimId = 0;

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

                    parameters.Add("ECID", ECID, DbType.Int64);
                    parameters.Add("ApprovalStatus", approvalStatus, DbType.Int32);
                    parameters.Add("ApprovedBy", approvedBy, DbType.Int32);
                    parameters.Add("ApprovedDate", approvedDate, DbType.DateTime2);
                    parameters.Add("Reason", reason, DbType.String);
                    parameters.Add("VerifierIDs", verifierIDs, DbType.String);
                    parameters.Add("ApproverIDs", approverIDs, DbType.String);
                    parameters.Add("UserApproverIDs", userApproverIDs, DbType.String);
                    parameters.Add("HODApproverID", hodApprover, DbType.String);
                    parameters.Add("IsAlternateUser", isAlternateApprover, DbType.Boolean);
                    parameters.Add("FinanceStartDay", financeStartDay, DbType.Int32);
                    //Add Department
                    //_context.Connection.QuerySingleAsync
                    //var addMstClaimQuery = $"INSERT INTO MstMileageClaim( MCNo, UserID, TravelMode, Verifier, Approver, FinalApprover, ApprovalStatus, GrandTotal, Company, DepartmentID, FacilityID, CreatedDate, ModifiedDate, CreatedBy, ModifiedBy, ApprovalDate, ApprovalBy, TnC) VALUES('{mstMileageClaim.MCNo}',{mstMileageClaim.UserID},'{mstMileageClaim.TravelMode}', '1,2','2,8',8,1,'{mstMileageClaim.GrandTotal}','{mstMileageClaim.Company}',1,1,'{DateTime.Now}','{DateTime.Now}',2,2,'{DateTime.Now}',2,1);SELECT CAST(SCOPE_IDENTITY() as int)";
                    var addMstClaimQuery = "UpdateMstExpenseClaimStatus";
                    //var mstClaimId = await _context.Connection.QuerySingleAsync<int>(addMstClaimQuery, transaction: transaction);
                    mstClaimId = await RepositoryContext.Connection.QueryFirstOrDefaultAsync<int>(addMstClaimQuery, parameters, transaction: transaction, commandType: CommandType.StoredProcedure);

                    //var mstClaimId = await  _writeDbConnection.QuerySingleAsync<int>(addMstClaimQuery, transaction: transaction);
                    //Check if Department Id is not Zero.
                    //if (mstClaimId == 0)
                    //{
                    //    throw new Exception("Mileage Id");
                    //}


                    //foreach (var dtMileageClaim1 in dtMileageClaims)
                    //{
                    //    dtMileageClaim1.MCID = mstClaimId;
                    //    await RepositoryContext.dtMileageClaim.AddAsync(dtMileageClaim1);
                    //    await RepositoryContext.SaveChangesAsync(default);
                    //    //return Json(new { res = true });

                    //}
                    transaction.Commit();
                    return mstClaimId;
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

        public DataTable InsertExcel(int userID, int createdBy)
        {
            string conString = string.Empty;
            DataTable dtGetData = new DataTable();
            DataTable dt = new DataTable();
            DataSet ds = new DataSet();

            SqlCommand cmd = new SqlCommand();

            using (SqlConnection con = new SqlConnection(_context.Connection.ConnectionString))
            {

                cmd.Connection = con;
                cmd.CommandText = "SP_ExpenseClaimInsertion";
                con.Open();
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add(new SqlParameter("@ClaimUserID", userID));
                cmd.Parameters.Add(new SqlParameter("@CreatedBy", createdBy));
                cmd.ExecuteNonQuery();



                SqlCommand CmdInvaild = new SqlCommand();
                CmdInvaild.Connection = con;
                CmdInvaild.CommandText = "SP_ExpesneClaimRecordInavild";
                CmdInvaild.CommandType = CommandType.StoredProcedure;
                CmdInvaild.Parameters.Add(new SqlParameter("@ClaimUserID", userID));

                SqlDataAdapter sda = new SqlDataAdapter(CmdInvaild);
                sda.Fill(ds);
                CmdInvaild.ExecuteNonQuery();
                con.Close();
                dtGetData = ds.Tables[0];
            }

            return dtGetData;

        }
    }
}
