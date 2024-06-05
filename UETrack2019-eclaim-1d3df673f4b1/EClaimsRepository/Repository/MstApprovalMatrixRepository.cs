using EClaimsEntities;
using EClaimsEntities.Models;
using EClaimsRepository.Contracts;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EClaimsRepository.Repository
{
    public class MstApprovalMatrixRepository : RepositoryBase<MstApprovalMatrix>, IMstApprovalMatrixRepository
    {
        public MstApprovalMatrixRepository(RepositoryContext repositoryContext, IApplicationReadDbConnection readDbConnection, IApplicationWriteDbConnection writeDbConnection)
   : base(repositoryContext, writeDbConnection, readDbConnection)
        {
        }

        public void CreateApprovalMatrix(MstApprovalMatrix mstApprovalMatrix)
        {
            Create(mstApprovalMatrix);
        }

        public void UpdateApprovalMatrix(MstApprovalMatrix mstApprovalMatrix)
        {
            Update(mstApprovalMatrix);
        }

        public void DeleteApprovalMatrix(MstApprovalMatrix mstApprovalMatrix)
        {
            Delete(mstApprovalMatrix);
        }

        public async Task<IEnumerable<MstApprovalMatrix>> GetAllApprovalMatrixAsync()
        {
            return await FindAll()
               .OrderBy(am => am.AMID)
               .ToListAsync();
        }

        public async Task<IEnumerable<MstApprovalMatrix>> GetAllApprovalMatrixWithScreensAsync()
        {
            return await FindAll()
                .Include(ms => ms.MstScreens)
                .ToListAsync();
        }

        public async Task<MstApprovalMatrix> GetApprovalMatrixByIdAsync(int? AMID)
        {
            return await FindByCondition(mstApprovalMatrix => mstApprovalMatrix.AMID.Equals(AMID))
                    .FirstOrDefaultAsync();
        }

        public IEnumerable<MstApprovalMatrix> GetApprovalMatrixByScreens(int? screenId)
        {
            return FindByCondition(sc => sc.ScreenID.Equals(screenId))
                .Include(x => x.MstScreens)
                .ToList();
        }

        public void UpdateMstApprovalMatrixAsync(MstApprovalMatrix mstApprovalMatrix)
        {
            //            dtApprovalMatrix.AMID;dtApprovalMatrix.AmountFrom;dtApprovalMatrix.AmountTo;dtApprovalMatrix.Approver;dtApprovalMatrix.Verifier;

            var aMIDParam = new SqlParameter("@AMID", mstApprovalMatrix.AMID);
            var approvalRequiredParam = new SqlParameter("@ApprovalRequired", mstApprovalMatrix.ApprovalRequired);
            var verificationLevelsParam = new SqlParameter("@VerificationLevels", mstApprovalMatrix.VerificationLevels);
            var approvalLevelsParam = new SqlParameter("@ApprovalLevels", mstApprovalMatrix.ApprovalLevels);
            var modifiedByParam = new SqlParameter("@ModifiedBy", mstApprovalMatrix.ModifiedBy);

            this.RepositoryContext.Database.ExecuteSqlRawAsync("exec UpdateApprovalMatrix @AMID,",
                        aMIDParam);


            //var approvalMatrices = this.RepositoryContext
            // .dtApprovalMatrix
            // .FromSqlRaw("exec AddDtApprovalMatrix @AMID, @Verifier,@Approver,@AmountFrom,@AmountTo",
            //             aMIDParam,verifierParam,approverParam,amountFromParam,amountToParam)
            //         .ToList();

        }
    }
}
