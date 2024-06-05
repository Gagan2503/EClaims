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
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EClaimsRepository.Repository
{
    public class MstMileageClaimDraftRepository : RepositoryBase<MstMileageClaimDraft>, IMstMileageClaimDratRepository
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

        public MstMileageClaimDraftRepository(RepositoryContext repositoryContext, IApplicationReadDbConnection readDbConnection, IApplicationWriteDbConnection writeDbConnection)
  : base(repositoryContext, writeDbConnection, readDbConnection)
        {
            _context = repositoryContext;
        }

        public void CreateMileageClaimDraft(MstMileageClaimDraft mstMileageClaim)
        {
            Create(mstMileageClaim);
        }

        public void DeleteMileageClaimDraft(MstMileageClaimDraft mstMileageClaim)
        {
            Delete(mstMileageClaim);
        }

        public async Task<IEnumerable<MstMileageClaimDraft>> GetAllMileageClaimDraftAsync()
        {
            return await FindAll()
             .OrderBy(mc => mc.MCNo)
             .ToListAsync();
        }

        public async Task<IEnumerable<CustomClaim>> GetAllMileageClaimDraftWithDetailsAsync(int userID, int facilityID, int statusID, string fromDate, string toDate)
        {
            var procedureName = "GetAllMileageClaimDraftsWithDetails";
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

        public async Task<IEnumerable<MstMileageClaimDraft>> GetAllMileageClaimDraftWithDetailsByFacilityIDAsync(int userID, int facilityID)
        {
            return await FindByCondition(mc => mc.FacilityID.Equals(facilityID) && mc.UserID.Equals(userID))
             .Include(mu => mu.MstUser)
             .Include(md => md.MstDepartment)
             .Include(mf => mf.MstFacility)
             .ToListAsync();
        }

        public async Task<MstMileageClaimDraft> GetMileageClaimDraftByIdAsync(long? mCID)
        {
            return await FindByCondition(mstMC => mstMC.MCID.Equals(mCID))
                .Include(mu => mu.MstUser)
             .Include(md => md.MstDepartment)
             .Include(mf => mf.MstFacility)
                    .FirstOrDefaultAsync();
        }

        public void UpdateMileageClaimDraft(MstMileageClaimDraft mstMileageClaim)
        {
            Update(mstMileageClaim);
        }

        public async Task<int> SaveDraftItems(MstMileageClaimDraft mstMileageClaim, List<DtMileageClaimDraft> dtMileageClaims, List<DtMileageClaimSummaryDraft> dtMileageClaimSummaries)
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
                    parameters.Add("TnC", mstMileageClaim.TnC, DbType.Boolean);
                    parameters.Add("ApprovalStatus", mstMileageClaim.ApprovalStatus, DbType.Int32);
                    //Add Department
                    //_context.Connection.QuerySingleAsync
                    //var addMstClaimQuery = $"INSERT INTO MstMileageClaim( MCNo, UserID, TravelMode, Verifier, Approver, FinalApprover, ApprovalStatus, GrandTotal, Company, DepartmentID, FacilityID, CreatedDate, ModifiedDate, CreatedBy, ModifiedBy, ApprovalDate, ApprovalBy, TnC) VALUES('{mstMileageClaim.MCNo}',{mstMileageClaim.UserID},'{mstMileageClaim.TravelMode}', '1,2','2,8',8,1,'{mstMileageClaim.GrandTotal}','{mstMileageClaim.Company}',1,1,'{DateTime.Now}','{DateTime.Now}',2,2,'{DateTime.Now}',2,1);SELECT CAST(SCOPE_IDENTITY() as int)";
                    var addMstClaimQuery = "AddOrEditMstMileageClaimDraft";
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
                            var dtMileageClaimDrafts = RepositoryContext.dtMileageClaimDraft.Where(x => x.MCID == mstClaimId);
                            foreach (var dtMileageClaimDraft in dtMileageClaimDrafts)
                            {
                                var exists = dtMileageClaims.Where(x => x.MCItemID == dtMileageClaimDraft.MCItemID).FirstOrDefault();
                                if (exists == null)
                                {
                                    RepositoryContext.dtMileageClaimDraft.Remove(dtMileageClaimDraft);
                                    await RepositoryContext.SaveChangesAsync(default);
                                }
                            }
                            RepositoryContext.ChangeTracker.Clear();

                            foreach (var dtMileageClaim1 in dtMileageClaims)
                            {
                                dtMileageClaim1.MCID = mstClaimId;
                                RepositoryContext.dtMileageClaimDraft.Update(dtMileageClaim1);
                                await RepositoryContext.SaveChangesAsync(default);
                                //return Json(new { res = true });

                            }
                        }
                        if (dtMileageClaimSummaries != null)
                        {
                            if (dtMileageClaimSummaries.Count > 0)
                            {
                                foreach (var dtMileageClaimSummary in dtMileageClaimSummaries)
                                {
                                    dtMileageClaimSummary.MCID = mstClaimId;
                                    await RepositoryContext.dtMileageClaimSummaryDraft.AddAsync(dtMileageClaimSummary);
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
                            await RepositoryContext.dtMileageClaimDraft.AddAsync(dtMileageClaim1);
                            await RepositoryContext.SaveChangesAsync(default);
                            //return Json(new { res = true });

                        }
                        foreach (var dtMileageClaimSummary in dtMileageClaimSummaries)
                        {
                            dtMileageClaimSummary.MCID = mstClaimId;
                            await RepositoryContext.dtMileageClaimSummaryDraft.AddAsync(dtMileageClaimSummary);
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

        public async Task<int> SaveSummaryDraft(int eCID, List<DtMileageClaimSummaryDraft> dtMileageClaimSummaries, MstMileageClaimAudit mstMileageClaimAudit)
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
                            await RepositoryContext.dtMileageClaimSummaryDraft.AddAsync(dtMileageClaimSummary);
                            await RepositoryContext.SaveChangesAsync(default);
                        }
                    }

                    //await RepositoryContext.MstMileageClaimAudit.AddAsync(mstMileageClaimAudit);
                    //await RepositoryContext.SaveChangesAsync(default);

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

        public async Task<int> UpdateMstMileageClaimDraftStatus(long? MCID, int? approvalStatus, int? approvedBy, DateTime? approvedDate, string reason, string verifierIDs, string approverIDs, string userApproverIDs, string hodApprover)
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
                    parameters.Add("Reason", reason, DbType.String);
                    parameters.Add("VerifierIDs", verifierIDs, DbType.String);
                    parameters.Add("ApproverIDs", approverIDs, DbType.String);
                    parameters.Add("UserApproverIDs", userApproverIDs, DbType.String);
                    parameters.Add("HODApproverID", hodApprover, DbType.String);
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
    }
}
