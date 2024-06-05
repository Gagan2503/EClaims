using Dapper;
using EClaimsEntities;
using EClaimsEntities.Models;
using EClaimsRepository.Contracts;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EClaimsRepository.Repository
{
    public class MstExpenseClaimDraftRepository : RepositoryBase<MstExpenseClaimDraft>, IMstExpenseClaimDraftRepository
    {
        public RepositoryContext _context { get; }
        public MstExpenseClaimDraftRepository(RepositoryContext repositoryContext, IApplicationReadDbConnection readDbConnection, IApplicationWriteDbConnection writeDbConnection)
   : base(repositoryContext, writeDbConnection, readDbConnection)
        {
            _context = repositoryContext;
        }

        public void CreateExpenseClaimDraft(MstExpenseClaimDraft mstExpenseClaim)
        {
            Create(mstExpenseClaim);
        }

        public void DeleteExpenseClaimDraft(MstExpenseClaimDraft mstExpenseClaim)
        {
            Delete(mstExpenseClaim);
        }

        public async Task<IEnumerable<MstExpenseClaimDraft>> GetAllExpenseClaimDraftsAsync()
        {
            return await FindAll()
            .OrderBy(mc => mc.ECNo)
             .ToListAsync();
        }

        public async Task<IEnumerable<CustomClaim>> GetAllExpenseClaimDraftsWithDetailsAsync(string expenseID, int userID, int facilityID, int statusID, string fromDate, string toDate)
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

        public async Task<IEnumerable<MstExpenseClaimDraft>> GetAllExpenseClaimDraftsWithDetailsByFacilityIDAsync(int userID, int facilityID)
        {
            return await FindByCondition(ec => ec.FacilityID.Equals(facilityID) && ec.UserID.Equals(userID)).OrderByDescending(ec => ec.ECID)
            .Include(mu => mu.MstUser)
            .Include(md => md.MstDepartment)
            .Include(mf => mf.MstFacility)
                        .ToListAsync();
        }

        public void UpdateExpenseClaimDraft(MstExpenseClaimDraft mstExpenseClaim)
        {
            Update(mstExpenseClaim);
        }

        public async Task<MstExpenseClaimDraft> GetExpenseClaimDraftByIdAsync(long? eCID)
        {
            return await FindByCondition(mstEC => mstEC.ECID.Equals(eCID))
                .Include(mu => mu.MstUser)
             .Include(md => md.MstDepartment)
             .Include(mf => mf.MstFacility)
                    .FirstOrDefaultAsync();
        }

        public async Task<int> SaveDraftItems(MstExpenseClaimDraft mstExpenseClaim, List<DtExpenseClaimDraft> dtExpenseClaims, List<DtExpenseClaimSummaryDraft> dtExpenseClaimSummaries)
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
                    parameters.Add("TnC", mstExpenseClaim.TnC, DbType.Boolean);
                    parameters.Add("ApprovalStatus", mstExpenseClaim.ApprovalStatus, DbType.Int32);
                    //Add Department
                    //_context.Connection.QuerySingleAsync
                    //var addMstClaimQuery = $"INSERT INTO MstMileageClaim( MCNo, UserID, TravelMode, Verifier, Approver, FinalApprover, ApprovalStatus, GrandTotal, Company, DepartmentID, FacilityID, CreatedDate, ModifiedDate, CreatedBy, ModifiedBy, ApprovalDate, ApprovalBy, TnC) VALUES('{mstMileageClaim.MCNo}',{mstMileageClaim.UserID},'{mstMileageClaim.TravelMode}', '1,2','2,8',8,1,'{mstMileageClaim.GrandTotal}','{mstMileageClaim.Company}',1,1,'{DateTime.Now}','{DateTime.Now}',2,2,'{DateTime.Now}',2,1);SELECT CAST(SCOPE_IDENTITY() as int)";
                    var addMstClaimQuery = "AddOrEditMstExpenseClaimDraft";
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
                            var dtExpenseClaimDrafts = RepositoryContext.dtExpenseClaimDraft.Where(x => x.ECID == mstClaimId);
                            foreach (var dtExpenseClaimDraft in dtExpenseClaimDrafts)
                            {
                                var exists = dtExpenseClaims.Where(x => x.ECItemID == dtExpenseClaimDraft.ECItemID).FirstOrDefault();
                                if (exists == null)
                                {
                                    RepositoryContext.dtExpenseClaimDraft.Remove(dtExpenseClaimDraft);
                                    await RepositoryContext.SaveChangesAsync(default);
                                }
                            }
                            RepositoryContext.ChangeTracker.Clear();

                            foreach (var dtExpenseClaims1 in dtExpenseClaims)
                            {
                                dtExpenseClaims1.ECID = mstClaimId;
                                dtExpenseClaims1.MstExpenseCategory = null;
                                RepositoryContext.dtExpenseClaimDraft.Update(dtExpenseClaims1);
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
                                    await RepositoryContext.dtExpenseClaimSummaryDraft.AddAsync(dtExpenseClaimSummary);
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
                            dtExpenseClaims1.MstExpenseCategory = null;
                            dtExpenseClaims1.ECItemID = 0;
                            await RepositoryContext.dtExpenseClaimDraft.AddAsync(dtExpenseClaims1);
                            await RepositoryContext.SaveChangesAsync(default);
                            //return Json(new { res = true });
                        }
                        foreach (var dtExpenseClaimSummary in dtExpenseClaimSummaries)
                        {
                            dtExpenseClaimSummary.ECID = mstClaimId;
                            await RepositoryContext.dtExpenseClaimSummaryDraft.AddAsync(dtExpenseClaimSummary);
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

        public async Task<int> SaveDraftSummary(int eCID, List<DtExpenseClaimSummaryDraft> dtExpenseClaimSummaries, MstExpenseClaimAudit mstExpenseClaimAudit)
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
                            await RepositoryContext.dtExpenseClaimSummaryDraft.AddAsync(dtExpenseClaimSummary);
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

        public DataTable InsertExcel(int userID)
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
