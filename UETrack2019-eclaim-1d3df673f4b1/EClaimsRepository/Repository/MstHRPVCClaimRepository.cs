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
    public class MstHRPVCClaimRepository : RepositoryBase<MstHRPVCClaim>, IMstHRPVCClaimRepository
    {
        public RepositoryContext _context { get; }
        public MstHRPVCClaimRepository(RepositoryContext repositoryContext, IApplicationReadDbConnection readDbConnection, IApplicationWriteDbConnection writeDbConnection)
   : base(repositoryContext, writeDbConnection, readDbConnection)
        {
            _context = repositoryContext;
        }

        public void CreateHRPVCClaim(MstHRPVCClaim mstHRPVCClaim)
        {
            Create(mstHRPVCClaim);
        }

        public void DeleteHRPVCClaim(MstHRPVCClaim mstHRPVCClaim)
        {
            Delete(mstHRPVCClaim);
        }



        /*       public IEnumerable<MstHRPVCClaim> HRPVCClaimByClaimType(int? claimTypeId)
               {
                   throw new NotImplementedException();
               }
        */
        public async Task<IEnumerable<MstHRPVCClaim>> GetAllHRPVCClaimAsync()
        {
            return await FindAll()
            .OrderBy(mc => mc.HRPVCCNo)
             .ToListAsync();
        }

        public async Task<IEnumerable<MstHRPVCClaim>> GetAllHRPVCClaimWithDetailsAsync()
        {
            return await FindAll()
            .Include(mu => mu.MstUser)
            .Include(mf => mf.MstFacility)
             .Include(md => md.MstDepartment)
            .OrderByDescending(m => m.HRPVCCNo)
            .ToListAsync();
        }

        public async Task<IEnumerable<MstHRPVCClaim>> GetAllHRPVCClaimForAPExportAsync(string id)
        {
                return await FindByCondition(mc => mc.ApprovalStatus == 3 && id.Contains(mc.HRPVCCID.ToString()))
                 .Include(mu => mu.MstUser)
                 .Include(md => md.MstDepartment)
                 .Include(mf => mf.MstFacility)
                 .OrderByDescending(m => m.HRPVCCNo)
                 .ToListAsync();
        }

        public async Task<List<CustomHRPVCClaim>> GetAllHRPVCClaimWithDetailsByFacilityIDForAPExportAsync(string claimID,int facilityID,string fromDate,string toDate)
        {
            var procedureName = "GetAllHRPVCClaimWithDetailsForAPExport";
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
                var queries = await RepositoryContext.Connection.QueryAsync<CustomHRPVCClaim>(procedureName, parameters, commandType: CommandType.StoredProcedure);
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

        public async Task<List<CustomHRPVCClaim>> GetAllHRPVCClaimWithDetailsByFacilityIDAsync(int userID, int facilityID, int statusID, string fromDate, string toDate)
        {
            var procedureName = "GetAllHRPVCClaimWithDetails";
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
                var queries = await RepositoryContext.Connection.QueryAsync<CustomHRPVCClaim>(procedureName, parameters, commandType: CommandType.StoredProcedure);
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
        public async Task<List<CustomHRPVCClaim>> GetAllHRPVCClaimWithDraftDetailsByFacilityIDAsync(int userID, int facilityID, int statusID, string fromDate, string toDate)
        {
            var procedureName = "GetAllHRPVCClaimWithDraftDetails";
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
                var queries = await RepositoryContext.Connection.QueryAsync<CustomHRPVCClaim>(procedureName, parameters, commandType: CommandType.StoredProcedure);
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
        public void UpdateHRPVCClaim(MstHRPVCClaim mstHRPVCClaim)
        {
            Update(mstHRPVCClaim);
        }

        public async Task<MstHRPVCClaim> GetHRPVCClaimByIdAsync(long? hRPVCCID)
        {
            return await FindByCondition(mstEC => mstEC.HRPVCCID.Equals(hRPVCCID))
                .Include(mu => mu.MstUser)
             .Include(mf => mf.MstFacility)
              .Include(md => md.MstDepartment)
                    .FirstOrDefaultAsync();
        }

        public async Task<int> SaveSummary(int hRPVCCID, List<DtHRPVCClaimSummary> dtHRPVCClaimSummaries, MstHRPVCClaimAudit mstHRPVCClaimAudit)
        {
            var mstClaimId = 0;

            RepositoryContext.Connection.Open();
            using (var transaction = RepositoryContext.Connection.BeginTransaction())
            {
                try
                {
                    RepositoryContext.Database.UseTransaction(transaction as DbTransaction);

                    if (dtHRPVCClaimSummaries.Count > 0)
                    {
                        foreach (var dtHRPVCClaimSummary in dtHRPVCClaimSummaries)
                        {
                                dtHRPVCClaimSummary.CItemID = 0;
                                await RepositoryContext.dtHRPVCClaimSummary.AddAsync(dtHRPVCClaimSummary);
                                await RepositoryContext.SaveChangesAsync(default);
                        }
                    }

                    await RepositoryContext.mstHRPVCClaimAudit.AddAsync(mstHRPVCClaimAudit);
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

        public async Task<int> SaveItems(MstHRPVCClaim mstHRPVCClaim, List<DtHRPVCClaim> dtHRPVCClaims,List<DtHRPVCClaimSummary> dtHRPVCClaimSummaries)
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

                    parameters.Add("HRPVCCID", mstHRPVCClaim.HRPVCCID, DbType.Int64);
                    parameters.Add("HRPVCCNo", mstHRPVCClaim.HRPVCCNo, DbType.String);
                    parameters.Add("UserID", mstHRPVCClaim.UserID, DbType.Int64);
                    parameters.Add("GrandTotal", mstHRPVCClaim.GrandTotal, DbType.Currency);
                    parameters.Add("TotalAmount", mstHRPVCClaim.TotalAmount, DbType.Currency);
                    parameters.Add("FacilityID", mstHRPVCClaim.FacilityID, DbType.Int32);
                    parameters.Add("DepartmentID", mstHRPVCClaim.DepartmentID, DbType.Int32);
                    parameters.Add("Particulars", mstHRPVCClaim.ParticularsOfPayment, DbType.String);
                    parameters.Add("ChequeNo", mstHRPVCClaim.ChequeNo, DbType.String);
                    parameters.Add("VoucherNo", mstHRPVCClaim.VoucherNo, DbType.String);
                    parameters.Add("Amount", mstHRPVCClaim.Amount, DbType.Currency);
                    parameters.Add("CreatedDate", mstHRPVCClaim.CreatedDate, DbType.DateTime2);
                    parameters.Add("ModifiedDate", mstHRPVCClaim.ModifiedDate, DbType.DateTime2);
                    parameters.Add("CreatedBy", mstHRPVCClaim.CreatedBy, DbType.Int32);
                    parameters.Add("ModifiedBy", mstHRPVCClaim.ModifiedBy, DbType.Int32);
                    parameters.Add("ApprovalDate", mstHRPVCClaim.ApprovalDate, DbType.DateTime2);
                    parameters.Add("ApprovalBy", mstHRPVCClaim.ApprovalBy, DbType.Int32);
                    parameters.Add("DelegatedBy", mstHRPVCClaim.DelegatedBy, DbType.Int32);
                    parameters.Add("TnC", mstHRPVCClaim.TnC, DbType.Boolean);
                    parameters.Add("ApprovalStatus", mstHRPVCClaim.ApprovalStatus, DbType.Int32);
                    //Add Department
                    //_context.Connection.QuerySingleAsync
                    //var addMstClaimQuery = $"INSERT INTO MstMileageClaim( MCNo, UserID, TravelMode, Verifier, Approver, FinalApprover, ApprovalStatus, GrandTotal, Company, DepartmentID, FacilityID, CreatedDate, ModifiedDate, CreatedBy, ModifiedBy, ApprovalDate, ApprovalBy, TnC) VALUES('{mstMileageClaim.MCNo}',{mstMileageClaim.UserID},'{mstMileageClaim.TravelMode}', '1,2','2,8',8,1,'{mstMileageClaim.GrandTotal}','{mstMileageClaim.Company}',1,1,'{DateTime.Now}','{DateTime.Now}',2,2,'{DateTime.Now}',2,1);SELECT CAST(SCOPE_IDENTITY() as int)";
                    var addMstClaimQuery = "AddOrEditMstHRPVCClaim";
                    //var mstClaimId = await _context.Connection.QuerySingleAsync<int>(addMstClaimQuery, transaction: transaction);
                    mstClaimId = await RepositoryContext.Connection.QueryFirstOrDefaultAsync<int>(addMstClaimQuery, parameters, transaction: transaction, commandType: CommandType.StoredProcedure);

                    //var mstClaimId = await  _writeDbConnection.QuerySingleAsync<int>(addMstClaimQuery, transaction: transaction);
                    //Check if Department Id is not Zero.
                    if (mstClaimId == 0)
                    {
                        throw new Exception("HRPVCClaim Id");
                    }

                    if (mstHRPVCClaim.HRPVCCID != 0)
                    {
                        if (dtHRPVCClaims.Count > 0)
                        {
                            foreach (var dtHRPVCClaims1 in dtHRPVCClaims)
                            {
                                dtHRPVCClaims1.HRPVCCID = mstClaimId;
                                // Setting this value to zero since its identity column
                                dtHRPVCClaims1.HRPVCCItemID = 0;
                                await RepositoryContext.dtHRPVCClaim.AddAsync(dtHRPVCClaims1);
                                await RepositoryContext.SaveChangesAsync(default);
                            }
                        }
                        if(dtHRPVCClaimSummaries != null)
                        {
                            if (dtHRPVCClaimSummaries.Count > 0)
                            {
                                foreach (var dtHRPVCClaimSummary in dtHRPVCClaimSummaries)
                                {
                                    dtHRPVCClaimSummary.HRPVCCID = mstClaimId;
                                    await RepositoryContext.dtHRPVCClaimSummary.AddAsync(dtHRPVCClaimSummary);
                                    await RepositoryContext.SaveChangesAsync(default);
                                }
                            }
                        }
                    }
                    else
                    {
                        foreach (var dtHRPVCClaims1 in dtHRPVCClaims)
                        {
                            dtHRPVCClaims1.HRPVCCID = mstClaimId;
                            dtHRPVCClaims1.HRPVCCItemID = 0;
                            await RepositoryContext.dtHRPVCClaim.AddAsync(dtHRPVCClaims1);
                            await RepositoryContext.SaveChangesAsync(default);
                            //return Json(new { res = true });
                        }
                        foreach (var dtHRPVCClaimSummary in dtHRPVCClaimSummaries)
                        {
                            dtHRPVCClaimSummary.HRPVCCID = mstClaimId;
                            await RepositoryContext.dtHRPVCClaimSummary.AddAsync(dtHRPVCClaimSummary);
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
        public async Task<int> SaveItemsDraft(MstHRPVCClaimDraft mstHRPVCClaim, List<DtHRPVCClaimDraft> dtHRPVCClaims, List<DtHRPVCClaimSummaryDraft> dtHRPVCClaimSummaries)
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

                    parameters.Add("HRPVCCID", mstHRPVCClaim.HRPVCCID, DbType.Int64);
                    parameters.Add("HRPVCCNo", mstHRPVCClaim.HRPVCCNo, DbType.String);
                    parameters.Add("UserID", mstHRPVCClaim.UserID, DbType.Int64);
                    parameters.Add("GrandTotal", mstHRPVCClaim.GrandTotal, DbType.Currency);
                    parameters.Add("TotalAmount", mstHRPVCClaim.TotalAmount, DbType.Currency);
                    parameters.Add("FacilityID", mstHRPVCClaim.FacilityID, DbType.Int32);
                    parameters.Add("DepartmentID", mstHRPVCClaim.DepartmentID, DbType.Int32);
                    parameters.Add("Particulars", mstHRPVCClaim.ParticularsOfPayment, DbType.String);
                    parameters.Add("ChequeNo", mstHRPVCClaim.ChequeNo, DbType.String);
                    parameters.Add("VoucherNo", mstHRPVCClaim.VoucherNo, DbType.String);
                    parameters.Add("Amount", mstHRPVCClaim.Amount, DbType.Currency);
                    parameters.Add("CreatedDate", mstHRPVCClaim.CreatedDate, DbType.DateTime2);
                    parameters.Add("ModifiedDate", mstHRPVCClaim.ModifiedDate, DbType.DateTime2);
                    parameters.Add("CreatedBy", mstHRPVCClaim.CreatedBy, DbType.Int32);
                    parameters.Add("ModifiedBy", mstHRPVCClaim.ModifiedBy, DbType.Int32);
                    parameters.Add("ApprovalDate", mstHRPVCClaim.ApprovalDate, DbType.DateTime2);
                    parameters.Add("ApprovalBy", mstHRPVCClaim.ApprovalBy, DbType.Int32);
                    parameters.Add("TnC", mstHRPVCClaim.TnC, DbType.Boolean);
                    parameters.Add("ApprovalStatus", mstHRPVCClaim.ApprovalStatus, DbType.Int32);
                    //Add Department
                    //_context.Connection.QuerySingleAsync
                    //var addMstClaimQuery = $"INSERT INTO MstMileageClaim( MCNo, UserID, TravelMode, Verifier, Approver, FinalApprover, ApprovalStatus, GrandTotal, Company, DepartmentID, FacilityID, CreatedDate, ModifiedDate, CreatedBy, ModifiedBy, ApprovalDate, ApprovalBy, TnC) VALUES('{mstMileageClaim.MCNo}',{mstMileageClaim.UserID},'{mstMileageClaim.TravelMode}', '1,2','2,8',8,1,'{mstMileageClaim.GrandTotal}','{mstMileageClaim.Company}',1,1,'{DateTime.Now}','{DateTime.Now}',2,2,'{DateTime.Now}',2,1);SELECT CAST(SCOPE_IDENTITY() as int)";
                    var addMstClaimQuery = "AddOrEditMstHRPVCClaimDraft";
                    //var mstClaimId = await _context.Connection.QuerySingleAsync<int>(addMstClaimQuery, transaction: transaction);
                    mstClaimId = await RepositoryContext.Connection.QueryFirstOrDefaultAsync<int>(addMstClaimQuery, parameters, transaction: transaction, commandType: CommandType.StoredProcedure);

                    //var mstClaimId = await  _writeDbConnection.QuerySingleAsync<int>(addMstClaimQuery, transaction: transaction);
                    //Check if Department Id is not Zero.
                    if (mstClaimId == 0)
                    {
                        throw new Exception("HRPVCClaim Id");
                    }

                    if (mstHRPVCClaim.HRPVCCID != 0)
                    {
                        if (dtHRPVCClaims.Count > 0)
                        {
                            foreach (var dtHRPVCClaims1 in dtHRPVCClaims)
                            {
                                dtHRPVCClaims1.HRPVCCID = mstClaimId;
                                // Setting this value to zero since its identity column
                                dtHRPVCClaims1.HRPVCCItemID = 0;
                                await RepositoryContext.dtHRPVCClaimdraft.AddAsync(dtHRPVCClaims1);
                                await RepositoryContext.SaveChangesAsync(default);
                            }
                        }
                        if (dtHRPVCClaimSummaries != null)
                        {
                            if (dtHRPVCClaimSummaries.Count > 0)
                            {
                                foreach (var dtHRPVCClaimSummary in dtHRPVCClaimSummaries)
                                {
                                    dtHRPVCClaimSummary.HRPVCCID = mstClaimId;
                                    await RepositoryContext.dtHRPVCClaimSummaryDraft.AddAsync(dtHRPVCClaimSummary);
                                    await RepositoryContext.SaveChangesAsync(default);
                                }
                            }
                        }
                    }
                    else
                    {
                        foreach (var dtHRPVCClaims1 in dtHRPVCClaims)
                        {
                            dtHRPVCClaims1.HRPVCCID = mstClaimId;
                            dtHRPVCClaims1.HRPVCCItemID = 0;
                            await RepositoryContext.dtHRPVCClaimdraft.AddAsync(dtHRPVCClaims1);
                            await RepositoryContext.SaveChangesAsync(default);
                            //return Json(new { res = true });
                        }
                        foreach (var dtHRPVCClaimSummary in dtHRPVCClaimSummaries)
                        {
                            dtHRPVCClaimSummary.HRPVCCID = mstClaimId;
                            await RepositoryContext.dtHRPVCClaimSummaryDraft.AddAsync(dtHRPVCClaimSummary);
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
        public async Task<string> GetVerifierAsync(long? hRPVCCID)
        {
            return FindByCondition(mstEC => mstEC.HRPVCCID.Equals(hRPVCCID))
                    .FirstOrDefaultAsync().GetAwaiter().GetResult().Verifier.ToString();
        }

        public async Task<string> GetApproverAsync(long? hRPVCCID)
        {
            return FindByCondition(mstEC => mstEC.HRPVCCID.Equals(hRPVCCID))
                    .FirstOrDefaultAsync().GetAwaiter().GetResult().Approver.ToString();
        }

        public async Task<string> GetUserApproverAsync(long? hRPVCCID)
        {
            return FindByCondition(mstMC => mstMC.HRPVCCID.Equals(hRPVCCID))
                    .FirstOrDefaultAsync().GetAwaiter().GetResult().UserApprovers.ToString();
        }

        public async Task<string> GetHODApproverAsync(long? hRPVCCID)
        {
            return FindByCondition(mstMC => mstMC.HRPVCCID.Equals(hRPVCCID))
                    .FirstOrDefaultAsync().GetAwaiter().GetResult().HODApprover.ToString();
        }

        public bool ExistsApproval(string ID, int ApprovedStatus, string UserID, string Screen)
        {
            int Id = Convert.ToInt32(ID);
            string value = "";
            if (Screen == "HRPVC")
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
            if (Screen == "HRPVC")
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

        public async Task<int> UpdateMstHRPVCClaimStatus(long? HRPVCCID, int? approvalStatus, int? approvedBy, DateTime? approvedDate, string reason, string verifierIDs, string approverIDs, string userApproverIDs, string hodApprover, bool isAlternateApprover, int? financeStartDay)
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

                    parameters.Add("HRPVCCID", HRPVCCID, DbType.Int64);
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
                    var addMstClaimQuery = "UpdateMstHRPVCClaimStatus";
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
                cmd.CommandText = "SP_HRPVCClaimInsertion";
                con.Open();
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add(new SqlParameter("@ClaimUserID", userID));
                cmd.Parameters.Add(new SqlParameter("@CreatedBy", createdBy));
                cmd.ExecuteNonQuery();



                SqlCommand CmdInvaild = new SqlCommand();
                CmdInvaild.Connection = con;
                CmdInvaild.CommandText = "SP_HRPVCClaimRecordInvalid";
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
// public async Task<MstHRPVCClaim> GetHRPVCClaimByIdAsync(int? HRPVCCID)
// {
//    return await FindByCondition(mstEC => mstEC.HRPVCCID.Equals(HRPVCCID))
//.FirstOrDefaultAsync();
// }








