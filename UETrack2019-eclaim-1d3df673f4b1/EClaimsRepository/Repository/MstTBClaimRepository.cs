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
    public class MstTBClaimRepository : RepositoryBase<MstTBClaim>, IMstTBClaimRepository
    {
        public RepositoryContext _context { get; }

        public MstTBClaimRepository(RepositoryContext repositoryContext, IApplicationReadDbConnection readDbConnection, IApplicationWriteDbConnection writeDbConnection)
   : base(repositoryContext, writeDbConnection, readDbConnection)
        {
            _context = repositoryContext;
        }

        public void CreateTBClaim(MstTBClaim mstTBClaim)
        {
            Create(mstTBClaim);
        }

        public void DeleteTBClaim(MstTBClaim mstTBClaim)
        {
            Delete(mstTBClaim);
        }

 /*       public IEnumerable<MstTBClaim> TBClaimByClaimType(int? claimTypeId)
        {
            throw new NotImplementedException();
        }
 */
        public async Task<IEnumerable<MstTBClaim>> GetAllTBClaimAsync()
        {
            return await FindAll()
            .OrderBy(mc => mc.TBCNo)
             .ToListAsync();
        }

        public async Task<IEnumerable<CustomClaim>> GetAllTBClaimWithDetailsAsync(int userID, int facilityID, int statusID, string fromDate, string toDate)
        {
            var procedureName = "GetAllTBClaimWithDetails";
            var parameters = new DynamicParameters();
            parameters.Add("UserID", userID, DbType.Int32, ParameterDirection.Input);
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

        public async Task<IEnumerable<MstTBClaim>> GetAllTBClaimForExportToBankAsync(string id, int facilityID, string fromDate, string toDate)
        {
            if (id == "")
            {
                return await FindByCondition(mc => mc.ApprovalStatus == 9 && mc.FacilityID == facilityID && mc.CreatedDate >= DateTime.ParseExact(fromDate, "dd/MM/yyyy", System.Globalization.CultureInfo.CurrentUICulture.DateTimeFormat) && mc.CreatedDate <= DateTime.ParseExact(toDate, "dd/MM/yyyy", System.Globalization.CultureInfo.CurrentUICulture.DateTimeFormat))
                .Include(mu => mu.MstUser)
                .Include(md => md.MstDepartment)
                .Include(mf => mf.MstFacility)
                .OrderByDescending(m => m.TBCNo)
                .ToListAsync();
            }
            else
            {
                return await FindByCondition(mc => mc.ApprovalStatus == 9 && id.Contains(mc.TBCID.ToString()))
                 .Include(mu => mu.MstUser)
                 .Include(md => md.MstDepartment)
                 .Include(mf => mf.MstFacility)
                 .OrderByDescending(m => m.TBCNo)
                 .ToListAsync();
            }
        }

        public async Task<IEnumerable<MstTBClaim>> GetAllTBClaimForAPExportAsync(string id, int facilityID, string fromDate, string toDate)
        {
            if (id == "")
            {
                return await FindByCondition(mc => mc.ApprovalStatus == 3 && mc.FacilityID == facilityID && mc.CreatedDate >= DateTime.ParseExact(fromDate, "dd/MM/yyyy", System.Globalization.CultureInfo.CurrentUICulture.DateTimeFormat) && mc.CreatedDate <= DateTime.ParseExact(toDate, "dd/MM/yyyy", System.Globalization.CultureInfo.CurrentUICulture.DateTimeFormat).AddDays(1))
                .Include(mu => mu.MstUser)
                .Include(md => md.MstDepartment)
                .Include(mf => mf.MstFacility)
                .OrderByDescending(m => m.TBCNo)
                .ToListAsync();
            }
            else
            {
                return await FindByCondition(mc => mc.ApprovalStatus == 3 && id.Contains(mc.TBCID.ToString()))
                 .Include(mu => mu.MstUser)
                 .Include(md => md.MstDepartment)
                 .Include(mf => mf.MstFacility)
                 .OrderByDescending(m => m.TBCNo)
                 .ToListAsync();
            }
        }

        public async Task<IEnumerable<MstTBClaim>> GetAllTBClaimWithDetailsByFacilityIDAsync(int userID,int facilityID)
        {
            //return await FindByCondition(tb => tb.FacilityID.Equals(facilityID) && tb.UserID.Equals(userID))
            return await FindByCondition(tb => tb.UserID.Equals(userID))
            .Include(mu => mu.MstUser)
            .Include(md => md.MstDepartment)
            .Include(mf => mf.MstFacility)
                .ToListAsync();
        }

        public void UpdateTBClaim(MstTBClaim mstTBClaim)
        {
            Update(mstTBClaim);
        }

        public async Task<MstTBClaim> GetTBClaimByIdAsync(long? tCID)
        {
            return await FindByCondition(mstEC => mstEC.TBCID.Equals(tCID))
                .Include(mu => mu.MstUser)
             .Include(md => md.MstDepartment)
             .Include(mf => mf.MstFacility)
                    .FirstOrDefaultAsync();
        }

        public async Task<int> SaveItems(MstTBClaim mstTBClaim, List<DtTBClaim> dtTBClaims, List<DtTBClaimSummary> dtTBClaimSummaries)
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

                    parameters.Add("TBCID", mstTBClaim.TBCID, DbType.Int64);
                    parameters.Add("TBCNo", mstTBClaim.TBCNo, DbType.String);
                    parameters.Add("UserID", mstTBClaim.UserID, DbType.Int64);
                    parameters.Add("Month", mstTBClaim.Month, DbType.Int32);
                    parameters.Add("Year", mstTBClaim.Year, DbType.Int32);
                    parameters.Add("GrandTotal", mstTBClaim.GrandTotal, DbType.Currency);
                    parameters.Add("Company", mstTBClaim.Company, DbType.String);
                    parameters.Add("DepartmentID", mstTBClaim.DepartmentID, DbType.Int32);
                    parameters.Add("FacilityID", mstTBClaim.FacilityID, DbType.Int32);
                    parameters.Add("CreatedDate", mstTBClaim.CreatedDate, DbType.DateTime2);
                    parameters.Add("ModifiedDate", mstTBClaim.ModifiedDate, DbType.DateTime2);
                    parameters.Add("CreatedBy", mstTBClaim.CreatedBy, DbType.Int32);
                    parameters.Add("ModifiedBy", mstTBClaim.ModifiedBy, DbType.Int32);
                    parameters.Add("ApprovalDate", mstTBClaim.ApprovalDate, DbType.DateTime2);
                    parameters.Add("ApprovalBy", mstTBClaim.ApprovalBy, DbType.Int32);
                    parameters.Add("DelegatedBy", mstTBClaim.DelegatedBy, DbType.Int32);
                    parameters.Add("TnC", mstTBClaim.TnC, DbType.Boolean);
                    parameters.Add("ApprovalStatus", mstTBClaim.ApprovalStatus, DbType.Int32);
                    //Add Department
                    //_context.Connection.QuerySingleAsync
                    //var addMstClaimQuery = $"INSERT INTO MstMileageClaim( MCNo, UserID, TravelMode, Verifier, Approver, FinalApprover, ApprovalStatus, GrandTotal, Company, DepartmentID, FacilityID, CreatedDate, ModifiedDate, CreatedBy, ModifiedBy, ApprovalDate, ApprovalBy, TnC) VALUES('{mstMileageClaim.MCNo}',{mstMileageClaim.UserID},'{mstMileageClaim.TravelMode}', '1,2','2,8',8,1,'{mstMileageClaim.GrandTotal}','{mstMileageClaim.Company}',1,1,'{DateTime.Now}','{DateTime.Now}',2,2,'{DateTime.Now}',2,1);SELECT CAST(SCOPE_IDENTITY() as int)";
                    var addMstClaimQuery = "AddOrEditMstTBClaim";
                    //var mstClaimId = await _context.Connection.QuerySingleAsync<int>(addMstClaimQuery, transaction: transaction);
                    mstClaimId = await RepositoryContext.Connection.QueryFirstOrDefaultAsync<int>(addMstClaimQuery, parameters, transaction: transaction, commandType: CommandType.StoredProcedure);

                    //var mstClaimId = await  _writeDbConnection.QuerySingleAsync<int>(addMstClaimQuery, transaction: transaction);
                    //Check if Department Id is not Zero.
                    if (mstClaimId == 0)
                    {
                        throw new Exception("TB Id");
                    }

                    if (mstTBClaim.TBCID != 0)
                    {
                        if (dtTBClaims.Count > 0)
                        {
                            foreach (var dtTBClaims1 in dtTBClaims)
                            {
                                dtTBClaims1.TBCID = mstClaimId;
                                // Setting this value to zero since its identity column
                                dtTBClaims1.TBCItemID = 0;
                                dtTBClaims1.MstExpenseCategory = null;
                                await RepositoryContext.dtTBClaim.AddAsync(dtTBClaims1);
                                await RepositoryContext.SaveChangesAsync(default);
                            }
                        }
                        if (dtTBClaimSummaries != null)
                        {
                            if (dtTBClaimSummaries.Count > 0)
                            {
                                foreach (var dtTBClaimSummary in dtTBClaimSummaries)
                                {
                                    dtTBClaimSummary.TBCID = mstClaimId;
                                    await RepositoryContext.dtTBClaimSummary.AddAsync(dtTBClaimSummary);
                                    await RepositoryContext.SaveChangesAsync(default);
                                }
                            }
                        }
                    }
                    else
                    {
                        foreach (var dtTBClaims1 in dtTBClaims)
                        {
                            dtTBClaims1.TBCID = mstClaimId;
                            dtTBClaims1.TBCItemID = 0;
                            dtTBClaims1.MstExpenseCategory = null;
                            await RepositoryContext.dtTBClaim.AddAsync(dtTBClaims1);
                            await RepositoryContext.SaveChangesAsync(default);
                        }
                        foreach (var dtTBClaimSummary in dtTBClaimSummaries)
                        {
                            dtTBClaimSummary.TBCID = mstClaimId;
                            await RepositoryContext.dtTBClaimSummary.AddAsync(dtTBClaimSummary);
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

        public async Task<int> SaveSummary(int tBCID, List<DtTBClaimSummary> dtTBClaimSummaries, MstTBClaimAudit mstTBClaimAudit)
        {
            var mstClaimId = 0;

            RepositoryContext.Connection.Open();
            using (var transaction = RepositoryContext.Connection.BeginTransaction())
            {
                try
                {
                    RepositoryContext.Database.UseTransaction(transaction as DbTransaction);

                    if (dtTBClaimSummaries.Count > 0)
                    {
                        foreach (var dtTBClaimSummary in dtTBClaimSummaries)
                        {
                            dtTBClaimSummary.CItemID = 0;
                            await RepositoryContext.dtTBClaimSummary.AddAsync(dtTBClaimSummary);
                            await RepositoryContext.SaveChangesAsync(default);
                        }
                    }

                    await RepositoryContext.MstTBClaimAudit.AddAsync(mstTBClaimAudit);
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
        public async Task<string> GetVerifierAsync(long? tBCID)
        {
            return FindByCondition(mstBC => mstBC.TBCID.Equals(tBCID))
                   .FirstOrDefaultAsync().GetAwaiter().GetResult().Verifier.ToString();
        }

        public async Task<string> GetApproverAsync(long? tBCID)
        {
            return FindByCondition(mstBC => mstBC.TBCID.Equals(tBCID))
                   .FirstOrDefaultAsync().GetAwaiter().GetResult().Approver.ToString();
        }

        public async Task<string> GetUserApproverAsync(long? mCID)
        {
            return FindByCondition(mstMC => mstMC.TBCID.Equals(mCID))
                    .FirstOrDefaultAsync().GetAwaiter().GetResult().UserApprovers.ToString();
        }

        public async Task<string> GetHODApproverAsync(long? mCID)
        {
            return FindByCondition(mstMC => mstMC.TBCID.Equals(mCID))
                    .FirstOrDefaultAsync().GetAwaiter().GetResult().HODApprover.ToString();
        }

        public bool ExistsApproval(string ID, int ApprovedStatus, string UserID, string Screen)
        {
            int Id = Convert.ToInt32(ID);
            string value = "";
            if (Screen == "TelephoneBill")
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

        public string GetApproverVerifier(string ID, int ApprovedStatus, string UserID, string Screen)
        {
            int Id = Convert.ToInt32(ID);
            string value = "";
            if (Screen == "TelephoneBill")
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
                cmd.CommandText = "SP_TelephoneBillClaimInsertion";
                con.Open();
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add(new SqlParameter("@ClaimUserID", userID));
                cmd.Parameters.Add(new SqlParameter("@CreatedBy", createdBy));
                cmd.ExecuteNonQuery();



                SqlCommand CmdInvaild = new SqlCommand();
                CmdInvaild.Connection = con;
                CmdInvaild.CommandText = "SP_TelephoneBillClaimRecordInavild";
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

        public async Task<int> UpdateMstTBClaimStatus(long? TBCID, int? approvalStatus, int? approvedBy, DateTime? approvedDate, string reason, string verifierIDs, string approverIDs, string userApproverIDs, string hodApprover, bool isAlternateApprover, int? financeStartDay)
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

                    parameters.Add("TBCID", TBCID, DbType.Int64);
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
                    var addMstClaimQuery = "UpdateMstTBClaimStatus";
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
    }
}
