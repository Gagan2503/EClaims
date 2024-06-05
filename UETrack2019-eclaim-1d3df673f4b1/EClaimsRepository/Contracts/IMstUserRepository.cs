using EClaimsEntities.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace EClaimsRepository.Contracts
{
    public interface IMstUserRepository : IRepositoryBase<MstUser>
    {
        MstUser Authenticate(MstUser mstUser);

        bool TryValidateUser(string username, string password, out List<Claim> claims);

        MstUser GetByUserName(String strUserName);
        Task<MstUser> GetUserByIdAsync(int? userId);
        Task<MstUser> GetUserWithDetailsByIdAsync(int? userId);
        Task<MstUser> GetUserByExternalProviderAsync(string nameIdentifier);
        Task<List<MstUser>> GetAllMCUsersForQueryAsync(int UserId, List<string> UserIds);
        Task<IEnumerable<MstUser>> GetAllHODUsersAsync();
        Task<IEnumerable<MstUser>> GetAllUsersAsync(string type = "all");
        Task<IEnumerable<MstUser>> GetAllUsersWithRolesAsync();

        bool ValidateUser(MstUser mstUser, string mode);
        MstUser AuthenticateUser(MstUser mstUser, string mode);

        void CreateUser(MstUser mstUser);
        MstUser CreateAndReturnUser(MstUser mstUser);

        MstUser CreateExternalUser(string provider, List<Claim> claims);

        void UpdateUser(MstUser mstUser);
        void DeleteUser(MstUser mstUser);
        MstUser GetUserByID(long userId);
        IEnumerable<MstUser> GetUserApprovers();
        DataTable InsertExcel();
        Task<List<CustomClaim>> GetAllHODSummaryClaimsAsync(int? UserId, string moduleName, int statusID, string fromDate, string toDate);
        Task<List<CustomClaim>> GetAllPendingApprovalClaimsAsync(int? UserId, string moduleName);

        Task<List<CustomClaim>> GetAllPendingUserApprovalClaimsAsync(int? UserId);
        Task<List<CustomClaim>> GetAllPendingApprovalEmailsAsync();
        Task<List<CustomClaim>> GetAllUserSubmittedClaimsAsync(int? UserId);
        Task<List<CustomIndividualClaimCount>> GetAllUserSubmittedClaimsCountAsync(int? UserId);
        Task<CustomClaimCount> GetAllUserApprovalClaimsCountAsync(int? UserId);
        Task<List<CustomIndividualClaimCount>> GetAllUserApprovedClaimsCountAsync(int? UserId, string Type);
        Task<List<CustomIndividualClaimCount>> GetAllUserIndividualApprovalClaimsCountAsync(int? UserId,string Type);
        Task<CustomClaimCount> GetAllUserVerificationClaimsCountAsync(int? UserId);
        Task<CustomClaimCount> GetAllUserTOTALCLAIMSCOUNTTHISYEARAsync(int? UserId);
        Task<CustomClaimCount> GetAllUserTOTALCLAIMSCOUNTTILLNOWAsync(int? UserId);

        Task<List<CustomClaim>> GetAllHRSummaryClaimsAsync(int? UserId, string moduleName, int statusID, string fromDate, string toDate);
    }
}
