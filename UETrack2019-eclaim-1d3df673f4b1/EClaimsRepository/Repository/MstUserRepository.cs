using EClaimsRepository.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EClaimsEntities.Models;
using EClaimsEntities;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using EClaimsRepository.Extensions;
using System.Data;
using System.Data.SqlClient;
using Dapper;

namespace EClaimsRepository.Repository
{
    public class MstUserRepository : RepositoryBase<MstUser>, IMstUserRepository
    {

        public RepositoryContext _context { get; }
        public IRepositoryContext _repositoryContext1 { get; }
        public IApplicationReadDbConnection _readDbConnection { get; }
        public IApplicationWriteDbConnection _writeDbConnection { get; }
        public MstUserRepository(RepositoryContext repositoryContext, IApplicationReadDbConnection readDbConnection, IApplicationWriteDbConnection writeDbConnection)
   : base(repositoryContext, writeDbConnection, readDbConnection)
        {

            _context = repositoryContext;
            _readDbConnection = readDbConnection;
            _writeDbConnection = writeDbConnection;
        }

        public MstUser CreateAndReturnUser(MstUser mstUser)
        {
            return CreateAndReturnEntity(mstUser);
        }

        public MstUser Authenticate(MstUser mstUser)
        {
            return FindByCondition(mu => mu.UserName.Equals(mstUser.UserName) && mu.Password.Equals(mstUser.Password) && mu.IsActive)
                   .FirstOrDefault();
        }

        public void CreateUser(MstUser mstUser)
        {
            Create(mstUser);
        }

        public void UpdateUser(MstUser mstUser)
        {
            Update(mstUser);
        }

        public void DeleteUser(MstUser mstUser)
        {
            Delete(mstUser);
        }
       

        public MstUser GetUserByID(long userId)
        {
            return FindByCondition(a => a.UserID == userId)
                                .FirstOrDefault();
        }

        public MstUser GetByUserName(string strUserName)
        {
            return FindByCondition(mu => mu.UserName.Equals(strUserName))
                    .FirstOrDefault();
        }

        public async Task<MstUser> GetUserByExternalProviderAsync(string nameIdentifier)
        {
            //return await FindByCondition(mu => mu.AuthenticationSource.Equals(provider) && mu.NameIdentifier.Equals(nameIdentifier))
                return await FindByCondition(mu => mu.NameIdentifier.Equals(nameIdentifier))
                .Include(ur => ur.DtUserRoles)
                .FirstOrDefaultAsync();
        }

        public async Task<MstUser> GetUserByIdAsync(int? userId)
        {
            return await FindByCondition(mu => mu.UserID.Equals(userId))
                    .Include(dtu => dtu.DtUserRoles)
                    .Include(dtf => dtf.DtUserFacilities)
                    .FirstOrDefaultAsync();
        }

        public async Task<MstUser> GetUserWithDetailsByIdAsync(int? userId)
        {
            return await FindByCondition(mu => mu.UserID.Equals(userId))
                .Include(md => md.MstFacility)
                //.Include(md => md.DtUserFacilities)
                .ThenInclude(mdd => mdd.MstDepartment)
                .FirstOrDefaultAsync();
        }


        public bool TryValidateUser(string username, string password, out List<Claim> claims)
        {
            claims = new List<Claim>();
            var user = Authenticate(new MstUser { UserName = username, Password = password });

            if (user is null)
            {
                return false;
            }
            else
            {
                claims.Add(new Claim(ClaimTypes.NameIdentifier, username));
                claims.Add(new Claim("username", username));
                claims.Add(new Claim(ClaimTypes.GivenName, user.Name));
                claims.Add(new Claim("userid", user.UserID.ToString()));
                //claims.Add(new Claim("facilityid", user.FacilityID.ToString()));
                //claims.Add(new Claim("ishod", user.IsHOD.ToString()));
                //   claims.Add(new Claim(ClaimTypes.Surname, user.Surname));
                //claims.Add(new Claim(ClaimTypes.Email, user.EmailAddress));
                // claims.Add(new Claim(ClaimTypes.MobilePhone, user.Mobile));

                return true;
            }
        }

        public MstUser CreateExternalUser(string provider, List<Claim> claims)
        {
            var mstUser = new MstUser();
            mstUser.AuthenticationSource = provider;
            mstUser.NameIdentifier = claims.GetClaim(ClaimTypes.NameIdentifier);
            mstUser.UserName = claims.GetClaim("username");
            mstUser.Name = claims.GetClaim(ClaimTypes.GivenName);
            mstUser.Surname = claims.GetClaim(ClaimTypes.Surname);
            var name = claims.GetClaim("name");
            // very rudimentary handling of splitting a users fullname into first and last name. Not very robust.
            if (string.IsNullOrEmpty(mstUser.Name))
            {
                mstUser.Name = name?.Split(' ').First();
            }
            if (string.IsNullOrEmpty(mstUser.Surname))
            {
                if (!string.IsNullOrEmpty(name))
                {
                    var nameSplit = name?.Split(' ');
                    if (nameSplit.Length > 1)
                    {
                        mstUser.Surname = name?.Split(' ').Last();
                    }
                }
            }
            //mstUser.EmailAddress = claims.GetClaim(ClaimTypes.Email);
            mstUser.EmailAddress = "";
            mstUser.EmployeeNo = "microsoft";
            mstUser.FacilityID = 1;
            mstUser.Phone = "";
            mstUser.AccessFailedCount = 5;
            mstUser.CreationTime = DateTime.Now;
            mstUser.CreatorUserId = 1;
            mstUser.DeleterUserId = 1;
            mstUser.DeletionTime = DateTime.Now;
            mstUser.IsActive = true;
            mstUser.IsDeleted = false;
            mstUser.IsEmailConfirmed = true;
            mstUser.IsLockoutEnabled = true;
            mstUser.IsPhoneNumberConfirmed = true;
            mstUser.IsTwoFactorEnabled = false;
            mstUser.LastModificationTime = DateTime.Now;
            mstUser.LastModifierUserId = 1;
            mstUser.LockoutEndDateUtc = DateTime.Now;
            // mstUser.Mobile = claims.GetClaim(ClaimTypes.MobilePhone);

            //mstUser.Roles = "NewUser";
            var entity = CreateAndReturnUser(mstUser);

            //var mstRole = G
            //_context.Mstu.Add(mstUser);
            //_context.SaveChanges();
            //mstUser.DtUserRoles.Add(new DtUserRoles { UserID = entity.UserID, RoleID = 1 });
            return entity;
        }

        public async Task<IEnumerable<MstUser>> GetAllUsersAsync(string type="all")
        {
            if (type == "all")
            {
                return await FindAll()
               .OrderBy(mu => mu.Name)
               .ToListAsync();
            }
            else
            {
                return await FindByCondition(mu => mu.IsActive)
              .OrderBy(mu => mu.Name)
              .ToListAsync();
            }
        }

        public async Task<List<MstUser>> GetAllMCUsersForQueryAsync(int UserId,List<string> UserIds)
        {
            if(UserIds.Count > 0)
            {
                return await FindByCondition(u => u.UserID != UserId && UserIds.Contains(u.UserID.ToString()))
                .ToListAsync();
            }
           else
            {
                return await FindByCondition(u => u.UserID != UserId )
                .ToListAsync();
            }
        }


        public Task<IEnumerable<MstUser>> GetAllUsersWithRolesAsync()
        {
            throw new NotImplementedException();
        }

        public async Task<IEnumerable<MstUser>> GetAllHODUsersAsync()
        {
            return await FindByCondition(ur => ur.IsHOD && ur.IsActive)
                 .OrderBy(mc => mc.Name)
                 .ToListAsync();
        }

        public IEnumerable<MstUser> GetUerApprovers()
        {
            return FindAll().ToList();
        }
       public bool ValidateUser(MstUser mstUser, string mode)
        {
            var user = AuthenticateUser(mstUser, mode);

            if (user is null)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        public MstUser AuthenticateUser(MstUser mstUser, string mode)
        {
            if (mode == "create")
            {
                return FindByCondition(mu => mu.EmailAddress.Equals(mstUser.EmailAddress) || mu.EmployeeNo.Equals(mstUser.EmployeeNo))
                 .FirstOrDefault();
            }
            else
            {
                return FindByCondition(mu => (mu.EmailAddress.Equals(mstUser.EmailAddress) || mu.EmployeeNo.Equals(mstUser.EmployeeNo)) && mu.UserID != mstUser.UserID)
                .FirstOrDefault();
            }
        }
        public IEnumerable<MstUser> GetUserApprovers()
        {
            return FindByCondition(mu => mu.IsHOD && mu.IsActive)
                .ToList();

        }

        public DataTable InsertExcel()
        {
            string conString = string.Empty;
            DataTable dtGetData = new DataTable();
            DataTable dt = new DataTable();
            DataSet ds = new DataSet();
           
            SqlCommand cmd = new SqlCommand();

            using (SqlConnection con = new SqlConnection(_context.Connection.ConnectionString))
            {
              
                    cmd.Connection = con;
                    cmd.CommandText = "SP_UserInsertion";
                    con.Open();
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.ExecuteNonQuery();



                    SqlCommand CmdInvaild = new SqlCommand();
                    CmdInvaild.Connection = con;
                    CmdInvaild.CommandText = "SP_UserRecordInavild";
                    CmdInvaild.CommandType = CommandType.StoredProcedure;

                    SqlDataAdapter sda = new SqlDataAdapter(CmdInvaild);
                    sda.Fill(ds);
                    CmdInvaild.ExecuteNonQuery();
                    con.Close();
                    dtGetData = ds.Tables[0];
            }

            return dtGetData;

        }

        public async Task<List<CustomClaim>> GetAllHODSummaryClaimsAsync(int? UserId, string moduleName, int statusID, string fromDate, string toDate)
        {
            //return await FindByCondition(j => j.ID == Mcid && (j.SenderID == UserId || j.ReceiverID == UserId) && j.ModuleType.ToString().Trim() == Module).OrderBy(j=>j.SentTime)
            //.ToListAsync();
            var procedureName = "GetAllHODSummaryClaims";
            var parameters = new DynamicParameters();
            parameters.Add("UserID", UserId, DbType.Int32, ParameterDirection.Input);
            parameters.Add("ModuleName", moduleName, DbType.String, ParameterDirection.Input);
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

        public async Task<List<CustomClaim>> GetAllPendingApprovalClaimsAsync(int? UserId, string moduleName)
        {
            //return await FindByCondition(j => j.ID == Mcid && (j.SenderID == UserId || j.ReceiverID == UserId) && j.ModuleType.ToString().Trim() == Module).OrderBy(j=>j.SentTime)
            //.ToListAsync();
            var procedureName = "GetAllPendingApprovalClaims";
            var parameters = new DynamicParameters();
            parameters.Add("UserID", UserId, DbType.Int32, ParameterDirection.Input);
            parameters.Add("ModuleName", moduleName, DbType.String, ParameterDirection.Input);
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

        public async Task<List<CustomClaim>> GetAllPendingUserApprovalClaimsAsync(int? UserId)
        {
            //return await FindByCondition(j => j.ID == Mcid && (j.SenderID == UserId || j.ReceiverID == UserId) && j.ModuleType.ToString().Trim() == Module).OrderBy(j=>j.SentTime)
            //.ToListAsync();
            var procedureName = "GetAllPendingUserApprovalClaims";
            var parameters = new DynamicParameters();
            parameters.Add("UserID", UserId, DbType.Int32, ParameterDirection.Input);
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

        public async Task<List<CustomClaim>> GetAllPendingApprovalEmailsAsync()
        {
            //return await FindByCondition(j => j.ID == Mcid && (j.SenderID == UserId || j.ReceiverID == UserId) && j.ModuleType.ToString().Trim() == Module).OrderBy(j=>j.SentTime)
            //.ToListAsync();
            var procedureName = "GetAllPendingApprovalEmails";
            RepositoryContext.Connection.Open();
            try
            {
                var queries = await RepositoryContext.Connection.QueryAsync<CustomClaim>(procedureName, commandType: CommandType.StoredProcedure);
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

        public async Task<List<CustomClaim>> GetAllUserSubmittedClaimsAsync(int? UserId)
        {
            //return await FindByCondition(j => j.ID == Mcid && (j.SenderID == UserId || j.ReceiverID == UserId) && j.ModuleType.ToString().Trim() == Module).OrderBy(j=>j.SentTime)
            //.ToListAsync();
            var procedureName = "GetAllUserSubmittedClaims";
            var parameters = new DynamicParameters();
            parameters.Add("UserID", UserId, DbType.Int32, ParameterDirection.Input);
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

        public async Task<List<CustomIndividualClaimCount>> GetAllUserSubmittedClaimsCountAsync(int? UserId)
        {
            //return await FindByCondition(j => j.ID == Mcid && (j.SenderID == UserId || j.ReceiverID == UserId) && j.ModuleType.ToString().Trim() == Module).OrderBy(j=>j.SentTime)
            //.ToListAsync();
            var procedureName = "GetAllUserSubmittedClaimsCount";
            var parameters = new DynamicParameters();
            parameters.Add("UserID", UserId, DbType.Int32, ParameterDirection.Input);
            RepositoryContext.Connection.Open();
            try
            {
                var queries = await RepositoryContext.Connection.QueryAsync<CustomIndividualClaimCount>(procedureName, parameters, commandType: CommandType.StoredProcedure);
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

        public async Task<CustomClaimCount> GetAllUserApprovalClaimsCountAsync(int? UserId)
        {
            //return await FindByCondition(j => j.ID == Mcid && (j.SenderID == UserId || j.ReceiverID == UserId) && j.ModuleType.ToString().Trim() == Module).OrderBy(j=>j.SentTime)
            //.ToListAsync();
            var procedureName = "GetAllUserApprovalClaimsCount";
            var parameters = new DynamicParameters();
            parameters.Add("UserID", UserId, DbType.Int32, ParameterDirection.Input);
            RepositoryContext.Connection.Open();
            try
            {
                var queries = await RepositoryContext.Connection.QueryAsync<CustomClaimCount>(procedureName, parameters, commandType: CommandType.StoredProcedure);
                //var company = await connection.QueryFirstOrDefaultAsync<Company>
                //    (procedureName, parameters, commandType: CommandType.StoredProcedure);
                return queries.FirstOrDefault();
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

        public async Task<List<CustomIndividualClaimCount>> GetAllUserIndividualApprovalClaimsCountAsync(int? UserId,string Type)
        {
            //return await FindByCondition(j => j.ID == Mcid && (j.SenderID == UserId || j.ReceiverID == UserId) && j.ModuleType.ToString().Trim() == Module).OrderBy(j=>j.SentTime)
            //.ToListAsync();
            var procedureName = "GetAllUserIndividualApprovalClaimsCount";
            var parameters = new DynamicParameters();
            parameters.Add("UserID", UserId, DbType.Int32, ParameterDirection.Input);
            parameters.Add("Type", Type, DbType.String, ParameterDirection.Input);
            RepositoryContext.Connection.Open();
            try
            {
                var queries = await RepositoryContext.Connection.QueryAsync<CustomIndividualClaimCount>(procedureName, parameters, commandType: CommandType.StoredProcedure);
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

        public async Task<List<CustomIndividualClaimCount>> GetAllUserApprovedClaimsCountAsync(int? UserId, string Type)
        {
            //return await FindByCondition(j => j.ID == Mcid && (j.SenderID == UserId || j.ReceiverID == UserId) && j.ModuleType.ToString().Trim() == Module).OrderBy(j=>j.SentTime)
            //.ToListAsync();
            var procedureName = "GetAllUserApprovedClaimsCount";
            var parameters = new DynamicParameters();
            parameters.Add("UserID", UserId, DbType.Int32, ParameterDirection.Input);
            parameters.Add("Type", Type, DbType.String, ParameterDirection.Input);
            RepositoryContext.Connection.Open();
            try
            {
                var queries = await RepositoryContext.Connection.QueryAsync<CustomIndividualClaimCount>(procedureName, parameters, commandType: CommandType.StoredProcedure);
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

        public async Task<CustomClaimCount> GetAllUserVerificationClaimsCountAsync(int? UserId)
        {
            //return await FindByCondition(j => j.ID == Mcid && (j.SenderID == UserId || j.ReceiverID == UserId) && j.ModuleType.ToString().Trim() == Module).OrderBy(j=>j.SentTime)
            //.ToListAsync();
            var procedureName = "GetAllUserVerificationClaimsCount";
            var parameters = new DynamicParameters();
            parameters.Add("UserID", UserId, DbType.Int32, ParameterDirection.Input);
            RepositoryContext.Connection.Open();
            try
            {
                var queries = await RepositoryContext.Connection.QueryAsync<CustomClaimCount>(procedureName, parameters, commandType: CommandType.StoredProcedure);
                //var company = await connection.QueryFirstOrDefaultAsync<Company>
                //    (procedureName, parameters, commandType: CommandType.StoredProcedure);
                return queries.FirstOrDefault();
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

        public async Task<CustomClaimCount> GetAllUserTOTALCLAIMSCOUNTTHISYEARAsync(int? UserId)
        {
            //return await FindByCondition(j => j.ID == Mcid && (j.SenderID == UserId || j.ReceiverID == UserId) && j.ModuleType.ToString().Trim() == Module).OrderBy(j=>j.SentTime)
            //.ToListAsync();
            var procedureName = "GetAllUserTOTALCLAIMSCOUNTTHISYEAR";
            var parameters = new DynamicParameters();
            parameters.Add("UserID", UserId, DbType.Int32, ParameterDirection.Input);
            RepositoryContext.Connection.Open();
            try
            {
                var queries = await RepositoryContext.Connection.QueryAsync<CustomClaimCount>(procedureName, parameters, commandType: CommandType.StoredProcedure);
                //var company = await connection.QueryFirstOrDefaultAsync<Company>
                //    (procedureName, parameters, commandType: CommandType.StoredProcedure);
                return queries.FirstOrDefault();
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

        public async Task<CustomClaimCount> GetAllUserTOTALCLAIMSCOUNTTILLNOWAsync(int? UserId)
        {
            //return await FindByCondition(j => j.ID == Mcid && (j.SenderID == UserId || j.ReceiverID == UserId) && j.ModuleType.ToString().Trim() == Module).OrderBy(j=>j.SentTime)
            //.ToListAsync();
            var procedureName = "GetAllUserTOTALCLAIMSCOUNTTILLNOW";
            var parameters = new DynamicParameters();
            parameters.Add("UserID", UserId, DbType.Int32, ParameterDirection.Input);
            RepositoryContext.Connection.Open();
            try
            {
                var queries = await RepositoryContext.Connection.QueryAsync<CustomClaimCount>(procedureName, parameters, commandType: CommandType.StoredProcedure);
                //var company = await connection.QueryFirstOrDefaultAsync<Company>
                //    (procedureName, parameters, commandType: CommandType.StoredProcedure);
                return queries.FirstOrDefault();
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


        public async Task<List<CustomClaim>> GetAllHRSummaryClaimsAsync(int? UserId, string moduleName, int statusID, string fromDate, string toDate)
        {
            //return await FindByCondition(j => j.ID == Mcid && (j.SenderID == UserId || j.ReceiverID == UserId) && j.ModuleType.ToString().Trim() == Module).OrderBy(j=>j.SentTime)
            //.ToListAsync();
            var procedureName = "GetAllHRSummaryClaims";
            var parameters = new DynamicParameters();
            parameters.Add("UserID", UserId, DbType.Int32, ParameterDirection.Input);
            parameters.Add("ModuleName", moduleName, DbType.String, ParameterDirection.Input);
            parameters.Add("StatusID", statusID, DbType.Int32, ParameterDirection.Input);
            parameters.Add("FromDate", fromDate, DbType.String, ParameterDirection.Input);
            parameters.Add("ToDate", toDate, DbType.String, ParameterDirection.Input);
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
    }
}
