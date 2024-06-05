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

    public class MstPVGClaimDraftRepository : RepositoryBase<MstPVGClaimDraft>, IMstPVGClaimDraftRepository
    {

        public RepositoryContext _context { get; }
        public MstPVGClaimDraftRepository(RepositoryContext repositoryContext, IApplicationReadDbConnection readDbConnection, IApplicationWriteDbConnection writeDbConnection)
   : base(repositoryContext, writeDbConnection, readDbConnection)
        {
            _context = repositoryContext;
        }

        public void CreatePVGClaimDraft(MstPVGClaimDraft mstPVGClaimDraft)
        {
            Create(mstPVGClaimDraft);
        }

        public void DeletePVGClaimDraft(MstPVGClaimDraft mstPVGClaimDraft)
        {
            Delete(mstPVGClaimDraft);
        }



        /*       public IEnumerable<MstPVGClaim> PVGClaimByClaimType(int? claimTypeId)
               {
                   throw new NotImplementedException();
               }
        */
        public async Task<IEnumerable<MstPVGClaimDraft>> GetAllPVGClaimDraftAsync()
        {
            return await FindAll()
            .OrderBy(mc => mc.PVGCNo)
             .ToListAsync();
        }

        public async Task<IEnumerable<CustomClaim>> GetAllPVGClaimDraftWithDetailsAsync(int userID, int facilityID, int statusID, string fromDate, string toDate)
        {
            var procedureName = "GetAllPVGClaimDraftWithDetails";
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

        //public async Task<IEnumerable<MstPVGClaimDraft>> GetAllPVGClaimDraftForExportToBankAsync(string id, int facilityID, string fromDate, string toDate)
        //{
        //    if (id == "")
        //    {
        //        return await FindByCondition(mc => mc.ApprovalStatus == 9 && mc.FacilityID == facilityID && mc.CreatedDate >= DateTime.ParseExact(fromDate, "dd/MM/yyyy", System.Globalization.CultureInfo.CurrentUICulture.DateTimeFormat) && mc.CreatedDate <= DateTime.ParseExact(toDate, "dd/MM/yyyy", System.Globalization.CultureInfo.CurrentUICulture.DateTimeFormat))
        //        .Include(mu => mu.MstUser)
        //        .Include(md => md.MstDepartment)
        //        .Include(mf => mf.MstFacility)
        //        .OrderByDescending(m => m.PVGCNo)
        //        .ToListAsync();
        //    }
        //    else
        //    {
        //        return await FindByCondition(mc => mc.ApprovalStatus == 9 && id.Contains(mc.PVGCID.ToString()))
        //         .Include(mu => mu.MstUser)
        //         .Include(md => md.MstDepartment)
        //         .Include(mf => mf.MstFacility)
        //         .OrderByDescending(m => m.PVGCNo)
        //         .ToListAsync();
        //    }
        //}

        //public async Task<IEnumerable<MstPVGClaimDraft>> GetAllPVGClaimDraftForAPExportAsync(string id, int facilityID, string fromDate, string toDate)
        //{
        //    if (id == "")
        //    {
        //        return await FindByCondition(mc => mc.ApprovalStatus == 3 && mc.FacilityID == facilityID && mc.CreatedDate >= DateTime.ParseExact(fromDate, "dd/MM/yyyy", System.Globalization.CultureInfo.CurrentUICulture.DateTimeFormat) && mc.CreatedDate <= DateTime.ParseExact(toDate, "dd/MM/yyyy", System.Globalization.CultureInfo.CurrentUICulture.DateTimeFormat).AddDays(1))
        //        .Include(mu => mu.MstUser)
        //        .Include(md => md.MstDepartment)
        //        .Include(mf => mf.MstFacility)
        //        .OrderByDescending(m => m.PVGCNo)
        //        .ToListAsync();
        //    }
        //    else
        //    {
        //        return await FindByCondition(mc => mc.ApprovalStatus == 3 && id.Contains(mc.PVGCID.ToString()))
        //         .Include(mu => mu.MstUser)
        //         .Include(md => md.MstDepartment)
        //         .Include(mf => mf.MstFacility)
        //         .OrderByDescending(m => m.PVGCNo)
        //         .ToListAsync();
        //    }
        //}


        public async Task<IEnumerable<MstPVGClaimDraft>> GetAllPVGClaimDraftWithDetailsByFacilityIDAsync(int userID, int facilityID)
        {
            return await FindByCondition(ec => ec.FacilityID.Equals(facilityID) && ec.UserID.Equals(userID)).OrderByDescending(ec => ec.PVGCID)
            .Include(mu => mu.MstUser)
            .Include(md => md.MstDepartment)
            .Include(mf => mf.MstFacility)
                        .ToListAsync();
        }

        public void UpdatePVGClaimDraft(MstPVGClaimDraft mstPVGClaimDraft)
        {
            Update(mstPVGClaimDraft);
        }

        public async Task<MstPVGClaimDraft> GetPVGClaimDraftByIdAsync(long? pVGCID)
        {
            return await FindByCondition(mstEC => mstEC.PVGCID.Equals(pVGCID))
                .Include(mu => mu.MstUser)
             .Include(md => md.MstDepartment)
             .Include(mf => mf.MstFacility)
                    .FirstOrDefaultAsync();
        }

        public async Task<int> SaveItemsDraft(MstPVGClaimDraft mstPVGClaimDraft, List<DtPVGClaimDraft> dtPVGClaimsDraft, List<DtPVGClaimSummaryDraft> dtPVGClaimSummariesDraft)
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

                    parameters.Add("PVGCID", mstPVGClaimDraft.PVGCID, DbType.Int64);
                    parameters.Add("PVGCNo", mstPVGClaimDraft.PVGCNo, DbType.String);
                    parameters.Add("UserID", mstPVGClaimDraft.UserID, DbType.Int64);
                    parameters.Add("PaymentMode", mstPVGClaimDraft.PaymentMode, DbType.String);
                    parameters.Add("GrandTotal", mstPVGClaimDraft.GrandTotal, DbType.Currency);
                    parameters.Add("TotalAmount", mstPVGClaimDraft.TotalAmount, DbType.Currency);
                    parameters.Add("Company", mstPVGClaimDraft.Company, DbType.String);
                    parameters.Add("DepartmentID", mstPVGClaimDraft.DepartmentID, DbType.Int32);
                    parameters.Add("FacilityID", mstPVGClaimDraft.FacilityID, DbType.Int32);
                    parameters.Add("CreatedDate", mstPVGClaimDraft.CreatedDate, DbType.DateTime2);
                    parameters.Add("ModifiedDate", mstPVGClaimDraft.ModifiedDate, DbType.DateTime2);
                    parameters.Add("CreatedBy", mstPVGClaimDraft.CreatedBy, DbType.Int32);
                    parameters.Add("ModifiedBy", mstPVGClaimDraft.ModifiedBy, DbType.Int32);
                    parameters.Add("ApprovalDate", mstPVGClaimDraft.ApprovalDate, DbType.DateTime2);
                    parameters.Add("ApprovalBy", mstPVGClaimDraft.ApprovalBy, DbType.Int32);
                    parameters.Add("TnC", mstPVGClaimDraft.TnC, DbType.Boolean);
                    parameters.Add("ApprovalStatus", mstPVGClaimDraft.ApprovalStatus, DbType.Int32);
                    //Add Department
                    //_context.Connection.QuerySingleAsync
                    //var addMstClaimQuery = $"INSERT INTO MstMileageClaim( MCNo, UserID, TravelMode, Verifier, Approver, FinalApprover, ApprovalStatus, GrandTotal, Company, DepartmentID, FacilityID, CreatedDate, ModifiedDate, CreatedBy, ModifiedBy, ApprovalDate, ApprovalBy, TnC) VALUES('{mstMileageClaim.MCNo}',{mstMileageClaim.UserID},'{mstMileageClaim.TravelMode}', '1,2','2,8',8,1,'{mstMileageClaim.GrandTotal}','{mstMileageClaim.Company}',1,1,'{DateTime.Now}','{DateTime.Now}',2,2,'{DateTime.Now}',2,1);SELECT CAST(SCOPE_IDENTITY() as int)";
                    var addMstClaimQuery = "AddOrEditMstPVGClaimDraft";
                    //var mstClaimId = await _context.Connection.QuerySingleAsync<int>(addMstClaimQuery, transaction: transaction);
                    mstClaimId = await RepositoryContext.Connection.QueryFirstOrDefaultAsync<int>(addMstClaimQuery, parameters, transaction: transaction, commandType: CommandType.StoredProcedure);

                    //var mstClaimId = await  _writeDbConnection.QuerySingleAsync<int>(addMstClaimQuery, transaction: transaction);
                    //Check if Department Id is not Zero.
                    if (mstClaimId == 0)
                    {
                        throw new Exception("Expense Id");
                    }

                        if (mstPVGClaimDraft.PVGCID != 0)
                        {
                            if (dtPVGClaimsDraft.Count > 0)
                            {
                            var dtPVGClaimsDrafts = RepositoryContext.dtPVGClaimDraft.Where(x => x.PVGCID == mstClaimId);
                            foreach (var dtPVGClaimsDraftitem in dtPVGClaimsDrafts)
                            {
                                var exists = dtPVGClaimsDraft.Where(x => x.PVGCItemID == dtPVGClaimsDraftitem.PVGCItemID).FirstOrDefault();
                                if (exists == null)
                                {
                                    RepositoryContext.dtPVGClaimDraft.Remove(dtPVGClaimsDraftitem);
                                    await RepositoryContext.SaveChangesAsync(default);
                                }
                            }
                            RepositoryContext.ChangeTracker.Clear();

                            foreach (var dtPVGClaims1 in dtPVGClaimsDraft)
                                {
                                    dtPVGClaims1.PVGCID = mstClaimId;
                                    dtPVGClaims1.MstExpenseCategory = null;
                                    RepositoryContext.dtPVGClaimDraft.Update(dtPVGClaims1);
                                    await RepositoryContext.SaveChangesAsync(default);
                                }
                            }
                            if (dtPVGClaimSummariesDraft != null)
                            {
                                if (dtPVGClaimSummariesDraft.Count > 0)
                                {
                                    foreach (var dtPVGClaimSummaryDraft in dtPVGClaimSummariesDraft)
                                    {
                                        dtPVGClaimSummaryDraft.PVGCID = mstClaimId;
                                        await RepositoryContext.dtPVGClaimSummaryDraft.AddAsync(dtPVGClaimSummaryDraft);
                                        await RepositoryContext.SaveChangesAsync(default);
                                    }
                                }
                            }
                        }
                        else
                        {
                            foreach (var dtPVGClaims1 in dtPVGClaimsDraft)
                            {
                                dtPVGClaims1.MstExpenseCategory = null;
                                dtPVGClaims1.PVGCID = mstClaimId;
                                dtPVGClaims1.PVGCItemID = 0;
                            await RepositoryContext.dtPVGClaimDraft.AddAsync(dtPVGClaims1);
                                await RepositoryContext.SaveChangesAsync(default);
                                //return Json(new { res = true });
                            }
                            foreach (var dtPVGClaimSummaryDraft in dtPVGClaimSummariesDraft)
                            {
                                dtPVGClaimSummaryDraft.PVGCID = mstClaimId;
                                await RepositoryContext.dtPVGClaimSummaryDraft.AddAsync(dtPVGClaimSummaryDraft);
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

        public async Task<int> SaveSummaryDraft(int pVGCID, List<DtPVGClaimSummaryDraft> dtPVGClaimSummariesDraft, MstPVGClaimAudit mstPVGClaimAudit)
        {
            var mstClaimId = 0;

            RepositoryContext.Connection.Open();
            using (var transaction = RepositoryContext.Connection.BeginTransaction())
            {
                try
                {
                    RepositoryContext.Database.UseTransaction(transaction as DbTransaction);

                    if (dtPVGClaimSummariesDraft.Count > 0)
                    {
                        foreach (var dtPVGClaimSummaryDraft in dtPVGClaimSummariesDraft)
                        {
                            dtPVGClaimSummaryDraft.CItemID = 0;
                            await RepositoryContext.dtPVGClaimSummaryDraft.AddAsync(dtPVGClaimSummaryDraft);
                            await RepositoryContext.SaveChangesAsync(default);
                        }
                    }

                    await RepositoryContext.MstPVGClaimAudit.AddAsync(mstPVGClaimAudit);
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

        public async Task<string> GetVerifierAsync(long? pVGCID)
        {
            return FindByCondition(mstEC => mstEC.PVGCID.Equals(pVGCID))
                    .FirstOrDefaultAsync().GetAwaiter().GetResult().Verifier.ToString();
        }

        public async Task<string> GetApproverAsync(long? pVGCID)
        {
            return FindByCondition(mstEC => mstEC.PVGCID.Equals(pVGCID))
                    .FirstOrDefaultAsync().GetAwaiter().GetResult().Approver.ToString();
        }

        public async Task<string> GetUserApproverAsync(long? pVGCID)
        {
            return FindByCondition(mstMC => mstMC.PVGCID.Equals(pVGCID))
                    .FirstOrDefaultAsync().GetAwaiter().GetResult().UserApprovers.ToString();
        }

        public async Task<string> GetHODApproverAsync(long? pVGCID)
        {
            return FindByCondition(mstMC => mstMC.PVGCID.Equals(pVGCID))
                    .FirstOrDefaultAsync().GetAwaiter().GetResult().HODApprover.ToString();
        }

        public bool ExistsApproval(string ID, int ApprovedStatus, string UserID, string Screen)
        {
            int Id = Convert.ToInt32(ID);
            string value = "";
            if (Screen == "PVG")
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
            if (Screen == "PVG")
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

        public async Task<int> UpdateMstPVGClaimDraftStatus(long? PVGCID, int? approvalStatus, int? approvedBy, DateTime? approvedDate, string reason, string verifierIDs, string approverIDs, string userApproverIDs, string hodApprover, bool isAlternateApprover, int? financeStartDay)
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

                    parameters.Add("PVGCID", PVGCID, DbType.Int64);
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
                    var addMstClaimQuery = "UpdateMstPVGClaimDraftStatus";
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

        //public DataTable InsertExcel(int userID)
        //{
        //    string conString = string.Empty;
        //    DataTable dtGetData = new DataTable();
        //    DataTable dt = new DataTable();
        //    DataSet ds = new DataSet();

        //    SqlCommand cmd = new SqlCommand();

        //    using (SqlConnection con = new SqlConnection(_context.Connection.ConnectionString))
        //    {

        //        cmd.Connection = con;
        //        cmd.CommandText = "SP_PVGClaimInsertion";
        //        con.Open();
        //        cmd.CommandType = CommandType.StoredProcedure;
        //        cmd.Parameters.Add(new SqlParameter("@ClaimUserID", userID));
        //        cmd.ExecuteNonQuery();



        //        SqlCommand CmdInvaild = new SqlCommand();
        //        CmdInvaild.Connection = con;
        //        CmdInvaild.CommandText = "SP_PVGClaimRecordInvalid";
        //        CmdInvaild.CommandType = CommandType.StoredProcedure;
        //        CmdInvaild.Parameters.Add(new SqlParameter("@ClaimUserID", userID));

        //        SqlDataAdapter sda = new SqlDataAdapter(CmdInvaild);
        //        sda.Fill(ds);
        //        CmdInvaild.ExecuteNonQuery();
        //        con.Close();
        //        dtGetData = ds.Tables[0];
        //    }

        //    return dtGetData;

        //}
    }
}

