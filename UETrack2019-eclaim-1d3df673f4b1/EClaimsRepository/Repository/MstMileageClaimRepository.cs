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
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EClaimsRepository.Repository
{
    public class MstMileageClaimRepository : RepositoryBase<MstMileageClaim>, IMstMileageClaimRepository
    {
        public RepositoryContext _context { get; }
        //public IRepositoryContext _repositoryContext1 { get; }
        //public IApplicationReadDbConnection _readDbConnection { get; }
        //public IApplicationWriteDbConnection _writeDbConnection { get; }

        //     public MstMileageClaimRepository(RepositoryContext repositoryContext,IRepositoryContext repositoryContext1, IApplicationReadDbConnection readDbConnection, IApplicationWriteDbConnection writeDbConnection)
        //: base(repositoryContext,writeDbConnection,readDbConnection)
        //     {
        //         _context = repositoryContext;
        //         _repositoryContext1 = repositoryContext1;
        //         _readDbConnection = readDbConnection;
        //         _writeDbConnection = writeDbConnection;
        //     }

        public MstMileageClaimRepository(RepositoryContext repositoryContext, IApplicationReadDbConnection readDbConnection, IApplicationWriteDbConnection writeDbConnection)
  : base(repositoryContext, writeDbConnection, readDbConnection)
        {
            _context = repositoryContext;
        }

        public void CreateMileageClaim(MstMileageClaim mstMileageClaim)
        {
            Create(mstMileageClaim);
        }

        public void DeleteMileageClaim(MstMileageClaim mstMileageClaim)
        {
            Delete(mstMileageClaim);
        }

        public async Task<IEnumerable<MstMileageClaim>> GetAllMileageClaimAsync()
        {
            return await FindAll()
             .OrderBy(mc => mc.MCNo)
             .ToListAsync();
        }

        public async Task<IEnumerable<CustomClaim>> GetAllMileageClaimWithDetailsAsync(int userID, int facilityID, int statusID, string fromDate, string toDate)
        {
            var procedureName = "GetAllMileageClaimWithDetails";
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

        public async Task<IEnumerable<CustomClaim>> GetAllMileageClaimForAPExportAsync(string id,string claimType,int facilityID, int statusID, string fromDate,string toDate)
        {
            var procedureName = "GetAllClaimsForAPExport";
            var parameters = new DynamicParameters();
            parameters.Add("CIDs", id, DbType.String, ParameterDirection.Input);
            parameters.Add("ClaimType", claimType, DbType.String, ParameterDirection.Input);
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
            //if (id == "")
            //{
            //     return await FindByCondition(mc => mc.ApprovalStatus == 3 && mc.FacilityID == facilityID && mc.CreatedDate >= DateTime.ParseExact(fromDate, "dd/MM/yyyy", System.Globalization.CultureInfo.CreateSpecificCulture("en-US"))  && mc.CreatedDate <= DateTime.ParseExact(toDate, "dd/MM/yyyy", System.Globalization.CultureInfo.CreateSpecificCulture("en-US")).AddDays(1))
            //     .Include(mu => mu.MstUser)
            //     .Include(md => md.MstDepartment)
            //     .Include(mf => mf.MstFacility)
            //     .OrderByDescending(m => m.MCNo)
            //     .ToListAsync();
            //}
            //else
            //{
            //    return await FindByCondition(mc => mc.ApprovalStatus == 3 && id.Contains(mc.MCID.ToString()))
            //     .Include(mu => mu.MstUser)
            //     .Include(md => md.MstDepartment)
            //     .Include(mf => mf.MstFacility)
            //     .OrderByDescending(m => m.MCNo)
            //     .ToListAsync();
            //}
        }

        public async Task<IEnumerable<CustomClaim>> GetAllMileageClaimForExportToBankAsync(string id, string claimType, int facilityID, int statusID, string fromDate, string toDate)
        {

            var procedureName = "GetAllClaimsForExportToBank";
            var parameters = new DynamicParameters();
            parameters.Add("CIDs", id, DbType.String, ParameterDirection.Input);
            parameters.Add("ClaimType", claimType, DbType.String, ParameterDirection.Input);
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
            //if (id == "")
            //{
            //    return await FindByCondition(mc => mc.ApprovalStatus == 9 && mc.FacilityID == facilityID && mc.CreatedDate >= DateTime.ParseExact(fromDate, "dd/MM/yyyy", System.Globalization.CultureInfo.CurrentUICulture.DateTimeFormat) && mc.CreatedDate <= DateTime.ParseExact(toDate, "dd/MM/yyyy", System.Globalization.CultureInfo.CurrentUICulture.DateTimeFormat))
            //    .Include(mu => mu.MstUser)
            //    .Include(md => md.MstDepartment)
            //    .Include(mf => mf.MstFacility)
            //    .OrderByDescending(m => m.MCNo)
            //    .ToListAsync();
            //}
            //else
            //{
            //    return await FindByCondition(mc => mc.ApprovalStatus == 9 && id.Contains(mc.MCID.ToString()))
            //     .Include(mu => mu.MstUser)
            //     .Include(md => md.MstDepartment)
            //     .Include(mf => mf.MstFacility)
            //     .OrderByDescending(m => m.MCNo)
            //     .ToListAsync();
            //}
        }

        public async Task<IEnumerable<CustomClaimReports>> GetAllUserClaimsReportAsync(int userID,string id, string claimType, int facilityID, int statusID, string fromDate, string toDate)
        {

            var procedureName = "GetAllUserClaimsReport";
            var parameters = new DynamicParameters();
            parameters.Add("UserID", userID, DbType.Int32, ParameterDirection.Input);
            parameters.Add("CIDs", id, DbType.String, ParameterDirection.Input);
            parameters.Add("ClaimType", claimType, DbType.String, ParameterDirection.Input);
            parameters.Add("FacilityID", facilityID, DbType.Int32, ParameterDirection.Input);
            parameters.Add("StatusID", statusID, DbType.Int32, ParameterDirection.Input);
            parameters.Add("FromDate", fromDate, DbType.String, ParameterDirection.Input);
            parameters.Add("ToDate", toDate, DbType.String, ParameterDirection.Input);
            //using ()
            //{
            RepositoryContext.Connection.Open();
            try
            {
                var queries = await RepositoryContext.Connection.QueryAsync<CustomClaimReports>(procedureName, parameters, commandType: CommandType.StoredProcedure);
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

        public async Task<IEnumerable<CustomClaimReports>> GetAllUserPVGClaimsReportAsync(string role,int userID, string id, string claimType, int facilityID, int statusID, string fromDate, string toDate)
        {

            var procedureName = "GetAllUserPVGClaimsReport";
            var parameters = new DynamicParameters();
            parameters.Add("Role", role, DbType.String, ParameterDirection.Input);
            parameters.Add("UserID", userID, DbType.Int32, ParameterDirection.Input);
            parameters.Add("CIDs", id, DbType.String, ParameterDirection.Input);
            parameters.Add("ClaimType", claimType, DbType.String, ParameterDirection.Input);
            parameters.Add("FacilityID", facilityID, DbType.Int32, ParameterDirection.Input);
            parameters.Add("StatusID", statusID, DbType.Int32, ParameterDirection.Input);
            parameters.Add("FromDate", fromDate, DbType.String, ParameterDirection.Input);
            parameters.Add("ToDate", toDate, DbType.String, ParameterDirection.Input);
            //using ()
            //{
            RepositoryContext.Connection.Open();
            try
            {
                var queries = await RepositoryContext.Connection.QueryAsync<CustomClaimReports>(procedureName, parameters, commandType: CommandType.StoredProcedure);
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


        public async Task<IEnumerable<CustomClaimReports>> GetAllUserPVCClaimsReportAsync(int userID, string id, string claimType, int facilityID, int statusID, string fromDate, string toDate)
        {

            var procedureName = "GetAllUserPVCClaimsReport";
            var parameters = new DynamicParameters();
            parameters.Add("UserID", userID, DbType.Int32, ParameterDirection.Input);
            parameters.Add("CIDs", id, DbType.String, ParameterDirection.Input);
            parameters.Add("ClaimType", claimType, DbType.String, ParameterDirection.Input);
            parameters.Add("FacilityID", facilityID, DbType.Int32, ParameterDirection.Input);
            parameters.Add("StatusID", statusID, DbType.Int32, ParameterDirection.Input);
            parameters.Add("FromDate", fromDate, DbType.String, ParameterDirection.Input);
            parameters.Add("ToDate", toDate, DbType.String, ParameterDirection.Input);
            //using ()
            //{
            RepositoryContext.Connection.Open();
            try
            {
                var queries = await RepositoryContext.Connection.QueryAsync<CustomClaimReports>(procedureName, parameters, commandType: CommandType.StoredProcedure);
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

        public async Task<IEnumerable<CustomClaimReports>> GetAllUserHRPVCClaimsReportAsync(int userID, string id, string claimType, int facilityID, int statusID, string fromDate, string toDate)
        {

            var procedureName = "GetAllUserHRPVCClaimsReport";
            var parameters = new DynamicParameters();
            parameters.Add("UserID", userID, DbType.Int32, ParameterDirection.Input);
            parameters.Add("CIDs", id, DbType.String, ParameterDirection.Input);
            parameters.Add("ClaimType", claimType, DbType.String, ParameterDirection.Input);
            parameters.Add("FacilityID", facilityID, DbType.Int32, ParameterDirection.Input);
            parameters.Add("StatusID", statusID, DbType.Int32, ParameterDirection.Input);
            parameters.Add("FromDate", fromDate, DbType.String, ParameterDirection.Input);
            parameters.Add("ToDate", toDate, DbType.String, ParameterDirection.Input);
            //using ()
            //{
            RepositoryContext.Connection.Open();
            try
            {
                var queries = await RepositoryContext.Connection.QueryAsync<CustomClaimReports>(procedureName, parameters, commandType: CommandType.StoredProcedure);
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

        public async Task<IEnumerable<CustomClaimReports>> GetAllUserHRPVGClaimsReportAsync(int userID, string id, string claimType, int facilityID, int statusID, string fromDate, string toDate)
        {

            var procedureName = "GetAllUserHRPVGClaimsReport";
            var parameters = new DynamicParameters();
            parameters.Add("UserID", userID, DbType.Int32, ParameterDirection.Input);
            parameters.Add("CIDs", id, DbType.String, ParameterDirection.Input);
            parameters.Add("ClaimType", claimType, DbType.String, ParameterDirection.Input);
            parameters.Add("FacilityID", facilityID, DbType.Int32, ParameterDirection.Input);
            parameters.Add("StatusID", statusID, DbType.Int32, ParameterDirection.Input);
            parameters.Add("FromDate", fromDate, DbType.String, ParameterDirection.Input);
            parameters.Add("ToDate", toDate, DbType.String, ParameterDirection.Input);
            //using ()
            //{
            RepositoryContext.Connection.Open();
            try
            {
                var queries = await RepositoryContext.Connection.QueryAsync<CustomClaimReports>(procedureName, parameters, commandType: CommandType.StoredProcedure);
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


        public async Task<IEnumerable<MstMileageClaim>> GetAllMileageClaimWithDetailsByFacilityIDAsync(int userID, int facilityID)
        {
            //return await FindByCondition(mc => mc.FacilityID.Equals(facilityID) && mc.UserID.Equals(userID))
            return await FindByCondition(mc => mc.UserID.Equals(userID))
             .Include(mu => mu.MstUser)
             .Include(md => md.MstDepartment)
             .Include(mf => mf.MstFacility)
             .ToListAsync();
        }

        public async Task<MstMileageClaim> GetMileageClaimByIdAsync(long? mCID)
        {
            return await FindByCondition(mstMC => mstMC.MCID.Equals(mCID))
                .Include(mu => mu.MstUser)
             .Include(md => md.MstDepartment)
             .Include(mf => mf.MstFacility)
                    .FirstOrDefaultAsync();
        }

        public void UpdateMileageClaim(MstMileageClaim mstMileageClaim)
        {
            Update(mstMileageClaim);
        }

        public async Task<int> SaveItems(MstMileageClaim mstMileageClaim, List<DtMileageClaim> dtMileageClaims, List<DtMileageClaimSummary> dtMileageClaimSummaries)
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

                    parameters.Add("MCID", mstMileageClaim.MCID, DbType.Int64);
                    parameters.Add("MCNo", mstMileageClaim.MCNo, DbType.String);
                    parameters.Add("UserID", mstMileageClaim.UserID, DbType.Int64);
                    parameters.Add("TravelMode", mstMileageClaim.TravelMode, DbType.String);
                    parameters.Add("GrandTotal", mstMileageClaim.GrandTotal, DbType.Currency);
                    parameters.Add("TotalKm", mstMileageClaim.TotalKm, DbType.Currency);
                    parameters.Add("Company", mstMileageClaim.Company, DbType.String);
                    parameters.Add("DepartmentID", mstMileageClaim.DepartmentID, DbType.Int32);
                    parameters.Add("FacilityID", mstMileageClaim.FacilityID, DbType.Int32);
                    parameters.Add("CreatedDate", mstMileageClaim.CreatedDate, DbType.DateTime2);
                    parameters.Add("ModifiedDate", mstMileageClaim.ModifiedDate, DbType.DateTime2);
                    parameters.Add("CreatedBy", mstMileageClaim.CreatedBy, DbType.Int32);
                    parameters.Add("ModifiedBy", mstMileageClaim.ModifiedBy, DbType.Int32);
                    parameters.Add("ApprovalDate", mstMileageClaim.ApprovalDate, DbType.DateTime2);
                    parameters.Add("ApprovalBy", mstMileageClaim.ApprovalBy, DbType.Int32);
                    parameters.Add("DelegatedBy", mstMileageClaim.DelegatedBy, DbType.Int32);
                    parameters.Add("TnC", mstMileageClaim.TnC, DbType.Boolean);
                    parameters.Add("ApprovalStatus", mstMileageClaim.ApprovalStatus, DbType.Int32);
                    //Add Department
                    //_context.Connection.QuerySingleAsync
                    //var addMstClaimQuery = $"INSERT INTO MstMileageClaim( MCNo, UserID, TravelMode, Verifier, Approver, FinalApprover, ApprovalStatus, GrandTotal, Company, DepartmentID, FacilityID, CreatedDate, ModifiedDate, CreatedBy, ModifiedBy, ApprovalDate, ApprovalBy, TnC) VALUES('{mstMileageClaim.MCNo}',{mstMileageClaim.UserID},'{mstMileageClaim.TravelMode}', '1,2','2,8',8,1,'{mstMileageClaim.GrandTotal}','{mstMileageClaim.Company}',1,1,'{DateTime.Now}','{DateTime.Now}',2,2,'{DateTime.Now}',2,1);SELECT CAST(SCOPE_IDENTITY() as int)";
                    var addMstClaimQuery = "AddOrEditMstMileageClaim";
                    //var mstClaimId = await _context.Connection.QuerySingleAsync<int>(addMstClaimQuery, transaction: transaction);
                    mstClaimId = await RepositoryContext.Connection.QueryFirstOrDefaultAsync<int>(addMstClaimQuery, parameters, transaction: transaction, commandType: CommandType.StoredProcedure);

                    //var mstClaimId = await  _writeDbConnection.QuerySingleAsync<int>(addMstClaimQuery, transaction: transaction);
                    //Check if Department Id is not Zero.
                    if (mstClaimId == 0)
                    {
                        throw new Exception("Mileage Id");
                    }

                    if (mstMileageClaim.MCID != 0)
                    {
                        if (dtMileageClaims.Count > 0)
                        {
                            foreach (var dtMileageClaim1 in dtMileageClaims)
                            {
                                dtMileageClaim1.MCID = mstClaimId;
                                dtMileageClaim1.MCItemID = 0;
                                await RepositoryContext.dtMileageClaim.AddAsync(dtMileageClaim1);
                                await RepositoryContext.SaveChangesAsync(default);
                            }
                        }
                        if (dtMileageClaimSummaries != null)
                        {
                            if (dtMileageClaimSummaries.Count > 0)
                            {
                                foreach (var dtMileageClaimSummary in dtMileageClaimSummaries)
                                {
                                    dtMileageClaimSummary.MCID = mstClaimId;
                                    await RepositoryContext.dtMileageClaimSummary.AddAsync(dtMileageClaimSummary);
                                    await RepositoryContext.SaveChangesAsync(default);
                                }
                            }
                        }
                    }
                    else
                    {
                        foreach (var dtMileageClaim1 in dtMileageClaims)
                        {
                            dtMileageClaim1.MCID = mstClaimId;
                            dtMileageClaim1.MCItemID = 0;
                            await RepositoryContext.dtMileageClaim.AddAsync(dtMileageClaim1);
                            await RepositoryContext.SaveChangesAsync(default);
                            //return Json(new { res = true });

                        }
                        foreach (var dtMileageClaimSummary in dtMileageClaimSummaries)
                        {
                            dtMileageClaimSummary.MCID = mstClaimId;
                            await RepositoryContext.dtMileageClaimSummary.AddAsync(dtMileageClaimSummary);
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
            //return Json(new { res = true });

        }

        public async Task<int> SaveSummary(int eCID, List<DtMileageClaimSummary> dtMileageClaimSummaries, MstMileageClaimAudit mstMileageClaimAudit)
        {
            var mstClaimId = 0;

            RepositoryContext.Connection.Open();
            using (var transaction = RepositoryContext.Connection.BeginTransaction())
            {
                try
                {
                    RepositoryContext.Database.UseTransaction(transaction as DbTransaction);

                    if (dtMileageClaimSummaries.Count > 0)
                    {
                        foreach (var dtMileageClaimSummary in dtMileageClaimSummaries)
                        {
                            dtMileageClaimSummary.CItemID = 0;
                            await RepositoryContext.dtMileageClaimSummary.AddAsync(dtMileageClaimSummary);
                            await RepositoryContext.SaveChangesAsync(default);
                        }
                    }

                    await RepositoryContext.MstMileageClaimAudit.AddAsync(mstMileageClaimAudit);
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

        public async Task<int> UpdateMstMileageClaimStatus(long? MCID, int? approvalStatus, int? approvedBy, DateTime? approvedDate, string reason, string verifierIDs, string approverIDs,string userApproverIDs,string hodApprover, bool isAlternateApprover, int? financeStartDay)
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

                    parameters.Add("MCID", MCID, DbType.Int64);
                    parameters.Add("ApprovalStatus", approvalStatus, DbType.Int32);
                    parameters.Add("ApprovedBy", approvedBy, DbType.Int32);
                    parameters.Add("ApprovedDate", approvedDate, DbType.DateTime2);
                    parameters.Add("Reason", reason , DbType.String);
                    parameters.Add("VerifierIDs", verifierIDs, DbType.String);
                    parameters.Add("ApproverIDs", approverIDs, DbType.String);
                    parameters.Add("UserApproverIDs", userApproverIDs, DbType.String);
                    parameters.Add("HODApproverID", hodApprover, DbType.String);
                    parameters.Add("IsAlternateUser", isAlternateApprover, DbType.Boolean);
                    parameters.Add("FinanceStartDay", financeStartDay, DbType.Int32);
                    //Add Department
                    //_context.Connection.QuerySingleAsync
                    //var addMstClaimQuery = $"INSERT INTO MstMileageClaim( MCNo, UserID, TravelMode, Verifier, Approver, FinalApprover, ApprovalStatus, GrandTotal, Company, DepartmentID, FacilityID, CreatedDate, ModifiedDate, CreatedBy, ModifiedBy, ApprovalDate, ApprovalBy, TnC) VALUES('{mstMileageClaim.MCNo}',{mstMileageClaim.UserID},'{mstMileageClaim.TravelMode}', '1,2','2,8',8,1,'{mstMileageClaim.GrandTotal}','{mstMileageClaim.Company}',1,1,'{DateTime.Now}','{DateTime.Now}',2,2,'{DateTime.Now}',2,1);SELECT CAST(SCOPE_IDENTITY() as int)";
                    var addMstClaimQuery = "UpdateMstMileageClaimStatus";
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

        public async Task<string> GetVerifierAsync(long? mCID)
        {
            return FindByCondition(mstMC => mstMC.MCID.Equals(mCID))
                    .FirstOrDefaultAsync().GetAwaiter().GetResult().Verifier.ToString();
        }

        public async Task<string> GetApproverAsync(long? mCID)
        {
            return FindByCondition(mstMC => mstMC.MCID.Equals(mCID))
                    .FirstOrDefaultAsync().GetAwaiter().GetResult().Approver.ToString();
        }

        public async Task<string> GetUserApproverAsync(long? mCID)
        {
            return FindByCondition(mstMC => mstMC.MCID.Equals(mCID))
                    .FirstOrDefaultAsync().GetAwaiter().GetResult().UserApprovers.ToString();
        }

        public async Task<string> GetHODApproverAsync(long? mCID)
        {
            return FindByCondition(mstMC => mstMC.MCID.Equals(mCID))
                    .FirstOrDefaultAsync().GetAwaiter().GetResult().HODApprover.ToString();
        }

        public bool ExistsApproval(string ID, int ApprovedStatus, string UserID, string Screen)
        {
            //ERPEntities objERPEntities = new ERPEntities();
            int Id = Convert.ToInt32(ID);
            string value = "";
            if (Screen == "Mileage")
            {
                if (ApprovedStatus == 1)
                {
                    value = GetVerifierAsync(Id).GetAwaiter().GetResult().ToString();
                }
                else if(ApprovedStatus == 6)
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
            //ERPEntities objERPEntities = new ERPEntities();
            int Id = Convert.ToInt32(ID);
            string value = "";
            if (Screen == "Mileage")
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
                cmd.CommandText = "SP_MileageClaimInsertion";
                con.Open();
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add(new SqlParameter("@ClaimUserID", userID));
                cmd.Parameters.Add(new SqlParameter("@CreatedBy", createdBy));
                cmd.ExecuteNonQuery();



                SqlCommand CmdInvaild = new SqlCommand();
                CmdInvaild.Connection = con;
                CmdInvaild.CommandText = "SP_MileageClaimRecordInavild";
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
