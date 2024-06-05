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
    public class MstPVCClaimDraftRepository : RepositoryBase<MstPVCClaimDraft>, IMstPVCClaimDraftRepository
    {
        public RepositoryContext _context { get; }
        public MstPVCClaimDraftRepository(RepositoryContext repositoryContext, IApplicationReadDbConnection readDbConnection, IApplicationWriteDbConnection writeDbConnection)
   : base(repositoryContext, writeDbConnection, readDbConnection)
        {
            _context = repositoryContext;
        }


        public async Task<List<CustomClaim>> GetAllPVCClaimDraftWithDetailsAsync(int userID, int facilityID, int statusID, string fromDate, string toDate)
        {
            var procedureName = "GetAllPVCClaimDraftWithDetails";
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

        public async Task<IEnumerable<MstPVCClaimDraft>> GetAllPVCClaimDraftWithDetailsByFacilityIDAsync(int userID, int facilityID)
        {
            return await FindByCondition(ec => ec.FacilityID.Equals(facilityID) && ec.UserID.Equals(userID)).OrderByDescending(ec => ec.PVCCID)
            .Include(mu => mu.MstUser)
            .Include(md => md.MstDepartment)
            .Include(mf => mf.MstFacility)
                        .ToListAsync();
        }

        public async Task<int> SaveItemsDraft(MstPVCClaimDraft mstPVCClaimDraft, List<DtPVCClaimDraft> dtPVCClaimsDraft, List<DtPVCClaimSummaryDraft> dtPVCClaimSummariesDraft)
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

                    parameters.Add("PVCCID", mstPVCClaimDraft.PVCCID, DbType.Int64);
                    parameters.Add("PVCCNo", mstPVCClaimDraft.PVCCNo, DbType.String);
                    parameters.Add("UserID", mstPVCClaimDraft.UserID, DbType.Int64);
                    parameters.Add("GrandTotal", mstPVCClaimDraft.GrandTotal, DbType.Currency);
                    parameters.Add("TotalAmount", mstPVCClaimDraft.TotalAmount, DbType.Currency);
                    parameters.Add("Company", mstPVCClaimDraft.Company, DbType.String);
                    parameters.Add("DepartmentID", mstPVCClaimDraft.DepartmentID, DbType.Int32);
                    parameters.Add("FacilityID", mstPVCClaimDraft.FacilityID, DbType.Int32);
                    parameters.Add("CreatedDate", mstPVCClaimDraft.CreatedDate, DbType.DateTime2);
                    parameters.Add("ModifiedDate", mstPVCClaimDraft.ModifiedDate, DbType.DateTime2);
                    parameters.Add("CreatedBy", mstPVCClaimDraft.CreatedBy, DbType.Int32);
                    parameters.Add("ModifiedBy", mstPVCClaimDraft.ModifiedBy, DbType.Int32);
                    parameters.Add("ApprovalDate", mstPVCClaimDraft.ApprovalDate, DbType.DateTime2);
                    parameters.Add("ApprovalBy", mstPVCClaimDraft.ApprovalBy, DbType.Int32);
                    parameters.Add("TnC", mstPVCClaimDraft.TnC, DbType.Boolean);
                    parameters.Add("ApprovalStatus", mstPVCClaimDraft.ApprovalStatus, DbType.Int32);
                    //Add Department
                    //_context.Connection.QuerySingleAsync
                    //var addMstClaimQuery = $"INSERT INTO MstMileageClaim( MCNo, UserID, TravelMode, Verifier, Approver, FinalApprover, ApprovalStatus, GrandTotal, Company, DepartmentID, FacilityID, CreatedDate, ModifiedDate, CreatedBy, ModifiedBy, ApprovalDate, ApprovalBy, TnC) VALUES('{mstMileageClaim.MCNo}',{mstMileageClaim.UserID},'{mstMileageClaim.TravelMode}', '1,2','2,8',8,1,'{mstMileageClaim.GrandTotal}','{mstMileageClaim.Company}',1,1,'{DateTime.Now}','{DateTime.Now}',2,2,'{DateTime.Now}',2,1);SELECT CAST(SCOPE_IDENTITY() as int)";
                    var addMstClaimQuery = "AddOrEditMstPVCClaimDraft";
                    //var mstClaimId = await _context.Connection.QuerySingleAsync<int>(addMstClaimQuery, transaction: transaction);
                    mstClaimId = await RepositoryContext.Connection.QueryFirstOrDefaultAsync<int>(addMstClaimQuery, parameters, transaction: transaction, commandType: CommandType.StoredProcedure);

                    //var mstClaimId = await  _writeDbConnection.QuerySingleAsync<int>(addMstClaimQuery, transaction: transaction);
                    //Check if Department Id is not Zero.
                    if (mstClaimId == 0)
                    {
                        throw new Exception("Expense Id");
                    }

                    if (mstPVCClaimDraft.PVCCID != 0)
                    {
                        if (dtPVCClaimsDraft.Count > 0)
                        {
                            var dtPVCClaimsDrafts = RepositoryContext.dtPVCClaimDraft.Where(x => x.PVCCID == mstClaimId);
                            foreach (var dtPVCClaimsDraftitem in dtPVCClaimsDrafts)
                            {
                                var exists = dtPVCClaimsDraft.Where(x => x.PVCCItemID == dtPVCClaimsDraftitem.PVCCItemID).FirstOrDefault();
                                if (exists == null)
                                {
                                    RepositoryContext.dtPVCClaimDraft.Remove(dtPVCClaimsDraftitem);
                                    await RepositoryContext.SaveChangesAsync(default);
                                }
                            }
                            RepositoryContext.ChangeTracker.Clear();

                            foreach (var dtPVCClaims1 in dtPVCClaimsDraft)
                            {
                                dtPVCClaims1.PVCCID = mstClaimId;
                                dtPVCClaims1.MstExpenseCategory = null;
                                RepositoryContext.dtPVCClaimDraft.Update(dtPVCClaims1);
                                await RepositoryContext.SaveChangesAsync(default);
                            }
                        }
                        if (dtPVCClaimSummariesDraft != null)
                        {
                            if (dtPVCClaimSummariesDraft.Count > 0)
                            {
                                foreach (var dtPVCClaimSummaryDraft in dtPVCClaimSummariesDraft)
                                {
                                    dtPVCClaimSummaryDraft.PVCCID = mstClaimId;
                                    await RepositoryContext.dtPVCClaimSummaryDraft.AddAsync(dtPVCClaimSummaryDraft);
                                    await RepositoryContext.SaveChangesAsync(default);
                                }
                            }
                        }
                    }
                    else
                    {
                        foreach (var dtPVCClaims1 in dtPVCClaimsDraft)
                        {
                            dtPVCClaims1.PVCCID = mstClaimId;
                            dtPVCClaims1.MstExpenseCategory = null;
                            dtPVCClaims1.PVCCItemID = 0;
                            await RepositoryContext.dtPVCClaimDraft.AddAsync(dtPVCClaims1);
                            await RepositoryContext.SaveChangesAsync(default);
                            //return Json(new { res = true });
                        }
                        foreach (var dtPVCClaimSummaryDraft in dtPVCClaimSummariesDraft)
                        {
                            dtPVCClaimSummaryDraft.PVCCID = mstClaimId;
                            await RepositoryContext.dtPVCClaimSummaryDraft.AddAsync(dtPVCClaimSummaryDraft);
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

        public async Task<MstPVCClaimDraft> GetPVCClaimDraftByIdAsync(long? pVCCID)
        {
            return await FindByCondition(mstEC => mstEC.PVCCID.Equals(pVCCID))
                .Include(mu => mu.MstUser)
             .Include(md => md.MstDepartment)
             .Include(mf => mf.MstFacility)
                    .FirstOrDefaultAsync();
        }

        public void DeletePVCClaimDraft(MstPVCClaimDraft mstPVCClaim)
        {
            Delete(mstPVCClaim);
        }
    }
}
