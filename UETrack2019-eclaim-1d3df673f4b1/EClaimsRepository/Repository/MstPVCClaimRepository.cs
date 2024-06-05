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
    public class MstPVCClaimRepository : RepositoryBase<MstPVCClaim>, IMstPVCClaimRepository
    {
        public RepositoryContext _context { get; }
        public MstPVCClaimRepository(RepositoryContext repositoryContext, IApplicationReadDbConnection readDbConnection, IApplicationWriteDbConnection writeDbConnection)
   : base(repositoryContext, writeDbConnection, readDbConnection)
        {
            _context = repositoryContext;
        }

        public void CreatePVCClaim(MstPVCClaim mstPVCClaim)
        {
            Create(mstPVCClaim);
        }

        public void DeletePVCClaim(MstPVCClaim mstPVCClaim)
        {
            Delete(mstPVCClaim);
        }

        

        /*       public IEnumerable<MstPVCClaim> PVCClaimByClaimType(int? claimTypeId)
               {
                   throw new NotImplementedException();
               }
        */
        public async Task<IEnumerable<MstPVCClaim>> GetAllPVCClaimAsync()
        {
            return await FindAll()
            .OrderBy(mc => mc.PVCCNo)
             .ToListAsync();
        }


        public async Task<List<CustomClaim>> GetAllPVCClaimWithDetailsAsync(int userID, int facilityID, int statusID, string fromDate, string toDate)
        {
            var procedureName = "GetAllPVCClaimWithDetails";
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

        public async Task<IEnumerable<MstPVCClaim>> GetAllPVCClaimForExportToBankAsync(string id, int facilityID, string fromDate, string toDate)
        {
            if (id == "")
            {
                return await FindByCondition(mc => mc.ApprovalStatus == 9 && mc.FacilityID == facilityID && mc.CreatedDate >= DateTime.ParseExact(fromDate, "dd/MM/yyyy", System.Globalization.CultureInfo.CurrentUICulture.DateTimeFormat) && mc.CreatedDate <= DateTime.ParseExact(toDate, "dd/MM/yyyy", System.Globalization.CultureInfo.CurrentUICulture.DateTimeFormat))
                .Include(mu => mu.MstUser)
                .Include(md => md.MstDepartment)
                .Include(mf => mf.MstFacility)
                .OrderByDescending(m => m.PVCCNo)
                .ToListAsync();
            }
            else
            {
                return await FindByCondition(mc => mc.ApprovalStatus == 9 && id.Contains(mc.PVCCID.ToString()))
                 .Include(mu => mu.MstUser)
                 .Include(md => md.MstDepartment)
                 .Include(mf => mf.MstFacility)
                 .OrderByDescending(m => m.PVCCNo)
                 .ToListAsync();
            }
        }

        public async Task<IEnumerable<MstPVCClaim>> GetAllPVCClaimForAPExportAsync(string id, int facilityID, string fromDate, string toDate)
        {
            if (id == "")
            {
                return await FindByCondition(mc => mc.ApprovalStatus == 3 && mc.FacilityID == facilityID && mc.CreatedDate >= DateTime.ParseExact(fromDate, "dd/MM/yyyy", System.Globalization.CultureInfo.CurrentUICulture.DateTimeFormat) && mc.CreatedDate <= DateTime.ParseExact(toDate, "dd/MM/yyyy", System.Globalization.CultureInfo.CurrentUICulture.DateTimeFormat).AddDays(1))
                .Include(mu => mu.MstUser)
                .Include(md => md.MstDepartment)
                .Include(mf => mf.MstFacility)
                .OrderByDescending(m => m.PVCCNo)
                .ToListAsync();
            }
            else
            {
                return await FindByCondition(mc => mc.ApprovalStatus == 3 && id.Contains(mc.PVCCID.ToString()))
                 .Include(mu => mu.MstUser)
                 .Include(md => md.MstDepartment)
                 .Include(mf => mf.MstFacility)
                 .OrderByDescending(m => m.PVCCNo)
                 .ToListAsync();
            }
        }


        public async Task<IEnumerable<MstPVCClaim>> GetAllPVCClaimWithDetailsByFacilityIDAsync(int userID, int facilityID)
        {
            //return await FindByCondition(ec => ec.FacilityID.Equals(facilityID) && ec.UserID.Equals(userID)).OrderByDescending(ec => ec.PVCCID)
            return await FindByCondition(ec => ec.UserID.Equals(userID)).OrderByDescending(ec => ec.PVCCID)
            .Include(mu => mu.MstUser)
            .Include(md => md.MstDepartment)
            .Include(mf => mf.MstFacility)
                        .ToListAsync();
        }

        public void UpdatePVCClaim(MstPVCClaim mstPVCClaim)
        {
            Update(mstPVCClaim);
        }

        public async Task<MstPVCClaim> GetPVCClaimByIdAsync(long? pVCCID)
        {
            return await FindByCondition(mstEC => mstEC.PVCCID.Equals(pVCCID))
                .Include(mu => mu.MstUser)
             .Include(md => md.MstDepartment)
             .Include(mf => mf.MstFacility)
                    .FirstOrDefaultAsync();
        }

        public async Task<int> SaveItems(MstPVCClaim mstPVCClaim, List<DtPVCClaim> dtPVCClaims, List<DtPVCClaimSummary> dtPVCClaimSummaries)
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

                    parameters.Add("PVCCID", mstPVCClaim.PVCCID, DbType.Int64);
                    parameters.Add("PVCCNo", mstPVCClaim.PVCCNo, DbType.String);
                    parameters.Add("UserID", mstPVCClaim.UserID, DbType.Int64);
                    parameters.Add("GrandTotal", mstPVCClaim.GrandTotal, DbType.Currency);
                    parameters.Add("TotalAmount", mstPVCClaim.TotalAmount, DbType.Currency);
                    parameters.Add("Company", mstPVCClaim.Company, DbType.String);
                    parameters.Add("DepartmentID", mstPVCClaim.DepartmentID, DbType.Int32);
                    parameters.Add("FacilityID", mstPVCClaim.FacilityID, DbType.Int32);
                    parameters.Add("CreatedDate", mstPVCClaim.CreatedDate, DbType.DateTime2);
                    parameters.Add("ModifiedDate", mstPVCClaim.ModifiedDate, DbType.DateTime2);
                    parameters.Add("CreatedBy", mstPVCClaim.CreatedBy, DbType.Int32);
                    parameters.Add("ModifiedBy", mstPVCClaim.ModifiedBy, DbType.Int32);
                    parameters.Add("ApprovalDate", mstPVCClaim.ApprovalDate, DbType.DateTime2);
                    parameters.Add("ApprovalBy", mstPVCClaim.ApprovalBy, DbType.Int32);
                    parameters.Add("DelegatedBy", mstPVCClaim.DelegatedBy, DbType.Int32);
                    parameters.Add("TnC", mstPVCClaim.TnC, DbType.Boolean);
                    parameters.Add("ApprovalStatus", mstPVCClaim.ApprovalStatus, DbType.Int32);
                    //Add Department
                    //_context.Connection.QuerySingleAsync
                    //var addMstClaimQuery = $"INSERT INTO MstMileageClaim( MCNo, UserID, TravelMode, Verifier, Approver, FinalApprover, ApprovalStatus, GrandTotal, Company, DepartmentID, FacilityID, CreatedDate, ModifiedDate, CreatedBy, ModifiedBy, ApprovalDate, ApprovalBy, TnC) VALUES('{mstMileageClaim.MCNo}',{mstMileageClaim.UserID},'{mstMileageClaim.TravelMode}', '1,2','2,8',8,1,'{mstMileageClaim.GrandTotal}','{mstMileageClaim.Company}',1,1,'{DateTime.Now}','{DateTime.Now}',2,2,'{DateTime.Now}',2,1);SELECT CAST(SCOPE_IDENTITY() as int)";
                    var addMstClaimQuery = "AddOrEditMstPVCClaim";
                    //var mstClaimId = await _context.Connection.QuerySingleAsync<int>(addMstClaimQuery, transaction: transaction);
                    mstClaimId = await RepositoryContext.Connection.QueryFirstOrDefaultAsync<int>(addMstClaimQuery, parameters, transaction: transaction, commandType: CommandType.StoredProcedure);

                    //var mstClaimId = await  _writeDbConnection.QuerySingleAsync<int>(addMstClaimQuery, transaction: transaction);
                    //Check if Department Id is not Zero.
                    if (mstClaimId == 0)
                    {
                        throw new Exception("Expense Id");
                    }

                    if (mstPVCClaim.PVCCID != 0)
                    {
                        if (dtPVCClaims.Count > 0)
                        {
                            foreach (var dtPVCClaims1 in dtPVCClaims)
                            {
                                dtPVCClaims1.PVCCID = mstClaimId;
                                dtPVCClaims1.MstExpenseCategory = null;
                                // Setting this value to zero since its identity column
                                dtPVCClaims1.PVCCItemID = 0;
                                await RepositoryContext.dtPVCClaim.AddAsync(dtPVCClaims1);
                                await RepositoryContext.SaveChangesAsync(default);
                            }
                        }
                        if (dtPVCClaimSummaries != null)
                        {
                            if (dtPVCClaimSummaries.Count > 0)
                            {
                                foreach (var dtPVCClaimSummary in dtPVCClaimSummaries)
                                {
                                    dtPVCClaimSummary.PVCCID = mstClaimId;
                                    await RepositoryContext.dtPVCClaimSummary.AddAsync(dtPVCClaimSummary);
                                    await RepositoryContext.SaveChangesAsync(default);
                                }
                            }
                        }
                    }
                    else
                    {
                        foreach (var dtPVCClaims1 in dtPVCClaims)
                        {
                            dtPVCClaims1.PVCCID = mstClaimId;
                            dtPVCClaims1.PVCCItemID = 0;
                            dtPVCClaims1.MstExpenseCategory = null;
                            await RepositoryContext.dtPVCClaim.AddAsync(dtPVCClaims1);
                            await RepositoryContext.SaveChangesAsync(default);
                            //return Json(new { res = true });
                        }
                        foreach (var dtPVCClaimSummary in dtPVCClaimSummaries)
                        {
                            dtPVCClaimSummary.PVCCID = mstClaimId;
                            await RepositoryContext.dtPVCClaimSummary.AddAsync(dtPVCClaimSummary);
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

        public async Task<int> SaveSummary(int pVCCID, List<DtPVCClaimSummary> dtPVCClaimSummaries, MstPVCClaimAudit mstPVCClaimAudit)
        {
            var mstClaimId = 0;

            RepositoryContext.Connection.Open();
            using (var transaction = RepositoryContext.Connection.BeginTransaction())
            {
                try
                {
                    RepositoryContext.Database.UseTransaction(transaction as DbTransaction);

                    if (dtPVCClaimSummaries.Count > 0)
                    {
                        foreach (var dtPVCClaimSummary in dtPVCClaimSummaries)
                        {
                            dtPVCClaimSummary.CItemID = 0;
                            await RepositoryContext.dtPVCClaimSummary.AddAsync(dtPVCClaimSummary);
                            await RepositoryContext.SaveChangesAsync(default);
                        }
                    }

                    await RepositoryContext.MstPVCClaimAudit.AddAsync(mstPVCClaimAudit);
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
        public async Task<string> GetVerifierAsync(long? pVCCID)
        {
            return FindByCondition(mstEC => mstEC.PVCCID.Equals(pVCCID))
                    .FirstOrDefaultAsync().GetAwaiter().GetResult().Verifier.ToString();
        }

        public async Task<string> GetApproverAsync(long? pVCCID)
        {
            return FindByCondition(mstEC => mstEC.PVCCID.Equals(pVCCID))
                    .FirstOrDefaultAsync().GetAwaiter().GetResult().Approver.ToString();
        }

        public async Task<string> GetUserApproverAsync(long? pVCCID)
        {
            return FindByCondition(mstMC => mstMC.PVCCID.Equals(pVCCID))
                    .FirstOrDefaultAsync().GetAwaiter().GetResult().UserApprovers.ToString();
        }

        public async Task<string> GetHODApproverAsync(long? pVCCID)
        {
            return FindByCondition(mstMC => mstMC.PVCCID.Equals(pVCCID))
                    .FirstOrDefaultAsync().GetAwaiter().GetResult().HODApprover.ToString();
        }

        public bool ExistsApproval(string ID, int ApprovedStatus, string UserID, string Screen)
        {
            int Id = Convert.ToInt32(ID);
            string value = "";
            if (Screen == "PVC")
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
            if (Screen == "PVC")
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

        public async Task<int> UpdateMstPVCClaimStatus(long? PVCCID, int? approvalStatus, int? approvedBy, DateTime? approvedDate, string reason, string verifierIDs, string approverIDs, string userApproverIDs, string hodApprover, bool isAlternateApprover, int? financeStartDay)
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

                    parameters.Add("PVCCID", PVCCID, DbType.Int64);
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
                    var addMstClaimQuery = "UpdateMstPVCClaimStatus";
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
                cmd.CommandText = "SP_PVCClaimInsertion";
                con.Open();
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add(new SqlParameter("@ClaimUserID", userID));
                cmd.Parameters.Add(new SqlParameter("@CreatedBy", createdBy));
                cmd.ExecuteNonQuery();



                SqlCommand CmdInvaild = new SqlCommand();
                CmdInvaild.Connection = con;
                CmdInvaild.CommandText = "SP_PVCClaimRecordInvalid";
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
       // public async Task<MstPVCClaim> GetPVCClaimByIdAsync(int? pVCCID)
       // {
        //    return await FindByCondition(mstEC => mstEC.PVCCID.Equals(pVCCID))
        //.FirstOrDefaultAsync();
       // }


        

       

        

