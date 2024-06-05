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
    public class MstHRPVGClaimRepository : RepositoryBase<MstHRPVGClaim>, IMstHRPVGClaimRepository
    {
        public RepositoryContext _context { get; }
        public MstHRPVGClaimRepository(RepositoryContext repositoryContext, IApplicationReadDbConnection readDbConnection, IApplicationWriteDbConnection writeDbConnection)
   : base(repositoryContext, writeDbConnection, readDbConnection)
        {
            _context = repositoryContext;
        }

        public void CreateHRPVGClaim(MstHRPVGClaim mstHRPVGClaim)
        {
            Create(mstHRPVGClaim);
        }

        public void DeleteHRPVGClaim(MstHRPVGClaim mstHRPVGClaim)
        {
            Delete(mstHRPVGClaim);
        }



        /*       public IEnumerable<MstHRPVGClaim> HRPVGClaimByClaimType(int? claimTypeId)
               {
                   throw new NotImplementedException();
               }
        */
        public async Task<IEnumerable<MstHRPVGClaim>> GetAllHRPVGClaimAsync()
        {
            return await FindAll()
            .OrderBy(mc => mc.HRPVGCNo)
             .ToListAsync();
        }

        public async Task<IEnumerable<MstHRPVGClaim>> GetAllHRPVGClaimWithDetailsAsync()
        {
            return await FindAll()
            .Include(mu => mu.MstUser)
            .Include(md => md.MstDepartment)
            .Include(mf => mf.MstFacility)
            .OrderByDescending(m => m.HRPVGCNo)
            .ToListAsync();
        }

        public async Task<List<CustomHRPVGClaim>> GetAllHRPVGClaimWithDetailsByFacilityIDForExportToBankAsync(string claimID, int facilityID, string fromDate, string toDate)
        {
            var procedureName = "GetAllHRPVGClaimWithDetailsForExportToBank";
            var parameters = new DynamicParameters();
            parameters.Add("ClaimID", claimID, DbType.String, ParameterDirection.Input);
            parameters.Add("FacilityID", facilityID, DbType.Int32, ParameterDirection.Input);
            parameters.Add("FromDate", fromDate, DbType.String, ParameterDirection.Input);
            parameters.Add("ToDate", toDate, DbType.String, ParameterDirection.Input);
            //using ()
            //{
            RepositoryContext.Connection.Open();
            try
            {
                var queries = await RepositoryContext.Connection.QueryAsync<CustomHRPVGClaim>(procedureName, parameters, commandType: CommandType.StoredProcedure);
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

        public async Task<List<CustomHRPVGClaim>> GetAllHRPVGClaimWithDetailsByFacilityIDForAPExportAsync(string claimID, int facilityID, string fromDate, string toDate)
        {
            var procedureName = "GetAllHRPVGClaimWithDetailsForAPExport";
            var parameters = new DynamicParameters();
            parameters.Add("ClaimID", claimID, DbType.String, ParameterDirection.Input);
            parameters.Add("FacilityID", facilityID, DbType.Int32, ParameterDirection.Input);
            parameters.Add("FromDate", fromDate, DbType.String, ParameterDirection.Input);
            parameters.Add("ToDate", toDate, DbType.String, ParameterDirection.Input);
            //using ()
            //{
            RepositoryContext.Connection.Open();
            try
            {
                var queries = await RepositoryContext.Connection.QueryAsync<CustomHRPVGClaim>(procedureName, parameters, commandType: CommandType.StoredProcedure);
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

        public async Task<List<CustomHRPVGClaim>> GetAllHRPVGClaimWithDetailsByFacilityIDAsync(int userID, int facilityID, int statusID, string fromDate, string toDate)
        {
            var procedureName = "GetAllHRPVGClaimWithDetails";
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
                var queries = await RepositoryContext.Connection.QueryAsync<CustomHRPVGClaim>(procedureName, parameters, commandType: CommandType.StoredProcedure);
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
       
        public void UpdateHRPVGClaim(MstHRPVGClaim mstHRPVGClaim)
        {
            Update(mstHRPVGClaim);
        }

        public async Task<MstHRPVGClaim> GetHRPVGClaimByIdAsync(long? hRPVGCID)
        {
            return await FindByCondition(mstEC => mstEC.HRPVGCID.Equals(hRPVGCID))
                .Include(mu => mu.MstUser)
             .Include(mf => mf.MstFacility)
             .Include(md => md.MstDepartment)
                    .FirstOrDefaultAsync();
        }

        public async Task<int> SaveSummary(int hRPVGCID, List<DtHRPVGClaimSummary> dtHRPVGClaimSummaries, MstHRPVGClaimAudit mstHRPVGClaimAudit)
        {
            var mstClaimId = 0;

            RepositoryContext.Connection.Open();
            using (var transaction = RepositoryContext.Connection.BeginTransaction())
            {
                try
                {
                    RepositoryContext.Database.UseTransaction(transaction as DbTransaction);

                    if (dtHRPVGClaimSummaries.Count > 0)
                    {
                        foreach (var dtHRPVGClaimSummary in dtHRPVGClaimSummaries)
                        {
                            dtHRPVGClaimSummary.CItemID = 0;
                            await RepositoryContext.dtHRPVGClaimSummary.AddAsync(dtHRPVGClaimSummary);
                            await RepositoryContext.SaveChangesAsync(default);
                        }
                    }

                    await RepositoryContext.mstHRPVGClaimAudit.AddAsync(mstHRPVGClaimAudit);
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

        public async Task<int> SaveItems(MstHRPVGClaim mstHRPVGClaim, List<DtHRPVGClaim> dtHRPVGClaims, List<DtHRPVGClaimSummary> dtHRPVGClaimSummaries)
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

                    parameters.Add("HRPVGCID", mstHRPVGClaim.HRPVGCID, DbType.Int64);
                    parameters.Add("HRPVGCNo", mstHRPVGClaim.HRPVGCNo, DbType.String);
                    parameters.Add("UserID", mstHRPVGClaim.UserID, DbType.Int64);
                    parameters.Add("PaymentMode", mstHRPVGClaim.PaymentMode, DbType.String);
                    parameters.Add("GrandTotal", mstHRPVGClaim.GrandTotal, DbType.Currency);
                    parameters.Add("TotalAmount", mstHRPVGClaim.TotalAmount, DbType.Currency);
                    parameters.Add("FacilityID", mstHRPVGClaim.FacilityID, DbType.Int32);
                    parameters.Add("DepartmentID", mstHRPVGClaim.DepartmentID, DbType.Int32);
                    parameters.Add("Particulars", mstHRPVGClaim.ParticularsOfPayment, DbType.String);
                    parameters.Add("ChequeNo", mstHRPVGClaim.ChequeNo, DbType.String);
                    parameters.Add("VoucherNo", mstHRPVGClaim.VoucherNo, DbType.String);
                    parameters.Add("Amount", mstHRPVGClaim.Amount, DbType.Currency);
                    parameters.Add("CreatedDate", mstHRPVGClaim.CreatedDate, DbType.DateTime2);
                    parameters.Add("ModifiedDate", mstHRPVGClaim.ModifiedDate, DbType.DateTime2);
                    parameters.Add("CreatedBy", mstHRPVGClaim.CreatedBy, DbType.Int32);
                    parameters.Add("ModifiedBy", mstHRPVGClaim.ModifiedBy, DbType.Int32);
                    parameters.Add("ApprovalDate", mstHRPVGClaim.ApprovalDate, DbType.DateTime2);
                    parameters.Add("ApprovalBy", mstHRPVGClaim.ApprovalBy, DbType.Int32);
                    parameters.Add("DelegatedBy", mstHRPVGClaim.DelegatedBy, DbType.Int32);
                    parameters.Add("TnC", mstHRPVGClaim.TnC, DbType.Boolean);
                    parameters.Add("ApprovalStatus", mstHRPVGClaim.ApprovalStatus, DbType.Int32);
                    //Add Department
                    //_context.Connection.QuerySingleAsync
                    //var addMstClaimQuery = $"INSERT INTO MstMileageClaim( MCNo, UserID, TravelMode, Verifier, Approver, FinalApprover, ApprovalStatus, GrandTotal, Company, DepartmentID, FacilityID, CreatedDate, ModifiedDate, CreatedBy, ModifiedBy, ApprovalDate, ApprovalBy, TnC) VALUES('{mstMileageClaim.MCNo}',{mstMileageClaim.UserID},'{mstMileageClaim.TravelMode}', '1,2','2,8',8,1,'{mstMileageClaim.GrandTotal}','{mstMileageClaim.Company}',1,1,'{DateTime.Now}','{DateTime.Now}',2,2,'{DateTime.Now}',2,1);SELECT CAST(SCOPE_IDENTITY() as int)";
                    var addMstClaimQuery = "AddOrEditMstHRPVGClaim";
                    //var mstClaimId = await _context.Connection.QuerySingleAsync<int>(addMstClaimQuery, transaction: transaction);
                    mstClaimId = await RepositoryContext.Connection.QueryFirstOrDefaultAsync<int>(addMstClaimQuery, parameters, transaction: transaction, commandType: CommandType.StoredProcedure);

                    //var mstClaimId = await  _writeDbConnection.QuerySingleAsync<int>(addMstClaimQuery, transaction: transaction);
                    //Check if Department Id is not Zero.
                    if (mstClaimId == 0)
                    {
                        throw new Exception("HRPVGClaim Id");
                    }

                    if (mstHRPVGClaim.HRPVGCID != 0)
                    {
                        if (dtHRPVGClaims.Count > 0)
                        {
                            foreach (var dtHRPVGClaims1 in dtHRPVGClaims)
                            {
                                dtHRPVGClaims1.HRPVGCID = mstClaimId;
                                // Setting this value to zero since its identity column
                                dtHRPVGClaims1.HRPVGCItemID = 0;
                                await RepositoryContext.dtHRPVGClaim.AddAsync(dtHRPVGClaims1);
                                await RepositoryContext.SaveChangesAsync(default);
                            }
                        }
                        if (dtHRPVGClaimSummaries != null)
                        {
                            if (dtHRPVGClaimSummaries.Count > 0)
                            {
                                foreach (var dtHRPVGClaimSummary in dtHRPVGClaimSummaries)
                                {
                                    dtHRPVGClaimSummary.HRPVGCID = mstClaimId;
                                    await RepositoryContext.dtHRPVGClaimSummary.AddAsync(dtHRPVGClaimSummary);
                                    await RepositoryContext.SaveChangesAsync(default);
                                }
                            }
                        }
                    }
                    else
                    {
                        foreach (var dtHRPVGClaims1 in dtHRPVGClaims)
                        {
                            dtHRPVGClaims1.HRPVGCID = mstClaimId;
                            dtHRPVGClaims1.HRPVGCItemID = 0;
                            await RepositoryContext.dtHRPVGClaim.AddAsync(dtHRPVGClaims1);
                            await RepositoryContext.SaveChangesAsync(default);
                            //return Json(new { res = true });
                        }
                        foreach (var dtHRPVGClaimSummary in dtHRPVGClaimSummaries)
                        {
                            dtHRPVGClaimSummary.HRPVGCID = mstClaimId;
                            await RepositoryContext.dtHRPVGClaimSummary.AddAsync(dtHRPVGClaimSummary);
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

        public async Task<string> GetVerifierAsync(long? hRPVGCID)
        {
            return FindByCondition(mstEC => mstEC.HRPVGCID.Equals(hRPVGCID))
                    .FirstOrDefaultAsync().GetAwaiter().GetResult().Verifier.ToString();
        }

        public async Task<string> GetApproverAsync(long? hRPVGCID)
        {
            return FindByCondition(mstEC => mstEC.HRPVGCID.Equals(hRPVGCID))
                    .FirstOrDefaultAsync().GetAwaiter().GetResult().Approver.ToString();
        }

        public async Task<string> GetUserApproverAsync(long? hRPVGCID)
        {
            return FindByCondition(mstMC => mstMC.HRPVGCID.Equals(hRPVGCID))
                    .FirstOrDefaultAsync().GetAwaiter().GetResult().UserApprovers.ToString();
        }

        public async Task<string> GetHODApproverAsync(long? hRPVGCID)
        {
            return FindByCondition(mstMC => mstMC.HRPVGCID.Equals(hRPVGCID))
                    .FirstOrDefaultAsync().GetAwaiter().GetResult().HODApprover.ToString();
        }

        public bool ExistsApproval(string ID, int ApprovedStatus, string UserID, string Screen)
        {
            int Id = Convert.ToInt32(ID);
            string value = "";
            if (Screen == "HRPVG")
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
            if (Screen == "HRPVG")
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

        public async Task<int> UpdateMstHRPVGClaimStatus(long? HRPVGCID, int? approvalStatus, int? approvedBy, DateTime? approvedDate, string reason, string verifierIDs, string approverIDs, string userApproverIDs, string hodApprover, bool isAlternateApprover,int? financeStartDay)
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

                    parameters.Add("HRPVGCID", HRPVGCID, DbType.Int64);
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
                    var addMstClaimQuery = "UpdateMstHRPVGClaimStatus";
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
                cmd.CommandText = "SP_HRPVGClaimInsertion";
                con.Open();
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add(new SqlParameter("@ClaimUserID", userID));
                cmd.Parameters.Add(new SqlParameter("@CreatedBy", createdBy));
                cmd.ExecuteNonQuery();



                SqlCommand CmdInvaild = new SqlCommand();
                CmdInvaild.Connection = con;
                CmdInvaild.CommandText = "SP_HRPVGClaimRecordInvalid";
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
// public async Task<MstHRPVGClaim> GetHRPVGClaimByIdAsync(int? HRPVGCID)
// {
//    return await FindByCondition(mstEC => mstEC.HRPVGCID.Equals(HRPVGCID))
//.FirstOrDefaultAsync();
// }








