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
    public class DtApprovalMatrixRepository : RepositoryBase<DtApprovalMatrix>, IDtApprovalMatrixRepository
    {
        public DtApprovalMatrixRepository(RepositoryContext repositoryContext, IApplicationReadDbConnection readDbConnection, IApplicationWriteDbConnection writeDbConnection)
   : base(repositoryContext, writeDbConnection, readDbConnection)
        {
        }

        public void CreateDtApprovalMatrix(DtApprovalMatrix dtApprovalMatrix)
        {
            Create(dtApprovalMatrix);
        }

        public void CreateRangeDtApprovalMatrix(List<DtApprovalMatrix> dtApprovalMatrix)
        {
            CreateRange(dtApprovalMatrix);
        }

        public void DeleteRangeDtApprovalMatrix(IEnumerable<DtApprovalMatrix> dtApprovalMatrix)
        {
            DeleteRange(dtApprovalMatrix);
        }

        public void UpdateDtApprovalMatrix(DtApprovalMatrix dtApprovalMatrix)
        {
            Update(dtApprovalMatrix);
        }

        public void DeleteDtApprovalMatrix(DtApprovalMatrix dtApprovalMatrix)
        {
            Delete(dtApprovalMatrix);
        }

        public IEnumerable<DtApprovalMatrix> GetDtApprovalMatrixByAMID(int? aMID)
        {
            return FindByCondition(a => a.AMID.Equals(aMID)).ToList();
        }

        public void CreateDtApprovalMatrixAsync(DtApprovalMatrix dtApprovalMatrix)
        {
//            dtApprovalMatrix.AMID;dtApprovalMatrix.AmountFrom;dtApprovalMatrix.AmountTo;dtApprovalMatrix.Approver;dtApprovalMatrix.Verifier;

            var aMIDParam = new SqlParameter("@AMID", dtApprovalMatrix.AMID);
            var verifierParam = new SqlParameter("@Verifier", dtApprovalMatrix.Verifier);
            var approverParam = new SqlParameter("@Approver", dtApprovalMatrix.Approver);
            var amountFromParam = new SqlParameter("@AmountFrom", dtApprovalMatrix.AmountFrom);
            var amountToParam = new SqlParameter("@AmountTo", dtApprovalMatrix.AmountTo);

             this.RepositoryContext.Database.ExecuteSqlRawAsync("exec AddDtApprovalMatrix @AMID, @Verifier,@Approver,@AmountFrom,@AmountTo",
                         aMIDParam, verifierParam, approverParam, amountFromParam, amountToParam);


            //var approvalMatrices = this.RepositoryContext
            // .dtApprovalMatrix
            // .FromSqlRaw("exec AddDtApprovalMatrix @AMID, @Verifier,@Approver,@AmountFrom,@AmountTo",
            //             aMIDParam,verifierParam,approverParam,amountFromParam,amountToParam)
            //         .ToList();

        }

        public async Task DeleteDtApprovalMatrixAsync(int aMID)
        {
            RepositoryContext.Remove(RepositoryContext.dtApprovalMatrix.Select(dta => dta.AMID == aMID));
            /*
            //            dtApprovalMatrix.AMID;dtApprovalMatrix.AmountFrom;dtApprovalMatrix.AmountTo;dtApprovalMatrix.Approver;dtApprovalMatrix.Verifier;

            var aMIDParam = new SqlParameter("@AMID", aMID);
            //var verifierParam = new SqlParameter("@Verifier", dtApprovalMatrix.Verifier);
            //var approverParam = new SqlParameter("@Approver", dtApprovalMatrix.Approver);
            //var amountFromParam = new SqlParameter("@AmountFrom", dtApprovalMatrix.AmountFrom);
            //var amountToParam = new SqlParameter("@AmountTo", dtApprovalMatrix.AmountTo);

            this.RepositoryContext.Database.ExecuteSqlRawAsync("exec DeleteDtApprovalMatrix @AMID",
                        aMIDParam);


            //var approvalMatrices = this.RepositoryContext
            // .dtApprovalMatrix
            // .FromSqlRaw("exec AddDtApprovalMatrix @AMID, @Verifier,@Approver,@AmountFrom,@AmountTo",
            //             aMIDParam,verifierParam,approverParam,amountFromParam,amountToParam)
            //         .ToList();
            */
        }
    }
}
