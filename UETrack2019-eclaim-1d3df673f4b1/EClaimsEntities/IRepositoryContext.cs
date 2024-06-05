using EClaimsEntities.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EClaimsEntities
{
    public interface IRepositoryContext
    {
        public IDbConnection Connection { get; }
        DatabaseFacade Database { get; }
        public DbSet<MstDepartment> mstDepartments { get; set; }
        public DbSet<MstTaxClass> mstTaxClass { get; set; }
        public DbSet<MstFacility> mstFacilities { get; set; }
        public DbSet<MstUser> users { get; set; }
        public DbSet<MstCostType> mstCostTypes { get; set; }
        public DbSet<MstClaimType> mstClaimTypes { get; set; }
        public DbSet<MstCostStructure> mstCostStructures { get; set; }

        public DbSet<MstExpenseCategory> mstExpenseCategories { get; set; }
        public DbSet<MstBankSwiftBIC> mstBankSwiftBICs { get; set; }

        public DbSet<MstRole> mstRoles { get; set; }

        public DbSet<DtUserRoles> dtUserRoles { get; set; }
        public DbSet<MstScreens> mstScreens { get; set; }
        public DbSet<MstApprovalMatrix> mstApprovalMatrix { get; set; }
        public DbSet<DtApprovalMatrix> dtApprovalMatrix { get; set; }
        public DbSet<MstMileageClaim> mstMileageClaim { get; set; }
        public DbSet<MstMileageClaimDraft> mstMileageClaimDraft { get; set; }
        public DbSet<MstMileageClaimAudit> MstMileageClaimAudit { get; set; }
        public DbSet<DtMileageClaim> dtMileageClaim { get; set; }
        public DbSet<DtMileageClaimFileUpload> dtMileageClaimFileUpload { get; set; }
        public DbSet<DtMileageClaimFileUploadDraft> dtMileageClaimFileUploadDraft { get; set; }
        public DbSet<MstExpenseClaim> mstExpenseClaim { get; set; }
        public DbSet<MstExpenseClaimAudit> MstExpenseClaimAudit { get; set; }
        public DbSet<MstExpenseClaimDraft> mstExpenseClaimDraft { get; set; }
        public DbSet<DtExpenseClaim> dtExpenseClaim { get; set; }
        public DbSet<DtExpenseClaimDraft> dtExpenseClaimDraft { get; set; }
        public DbSet<DtExpenseClaimFileUpload> dtExpenseClaimFileUpload { get; set; }
        public DbSet<DtExpenseClaimFileUploadDraft> dtExpenseClaimFileUploadDraft { get; set; }
        public DbSet<MstPVCClaimDraft> mstPVCClaimDraft { get; set; }
        public DbSet<DtPVCClaimDraft> dtPVCClaimDraft { get; set; }
        public DbSet<DtPVCClaimDraftFileUpload> dtPVCClaimDraftFileUpload { get; set; }
        public DbSet<MstPVGClaimDraft> mstPVGClaimDraft { get; set; }
        public DbSet<DtPVGClaimDraft> dtPVGClaimDraft { get; set; }
        public DbSet<DtPVGClaimFileUploadDraft> dtPVGClaimFileUploadDraft { get; set; }
        public DbSet<MstTBClaim> mstTBClaim { get; set; }
        public DbSet<MstTBClaimDraft> mstTBClaimDraft { get; set; }
        public DbSet<MstTBClaimAudit> MstTBClaimAudit { get; set; }
        public DbSet<DtTBClaim> dtTBClaim { get; set; }
        public DbSet<DtTBClaimDraft> dtTBClaimDraft { get; set; }
        public DbSet<DtTBClaimFileUpload> dtTBClaimFileUpload { get; set; }
        public DbSet<DtTBClaimFileUploadDraft> dtTBClaimFileUploadDraft { get; set; }
        public DbSet<MstPVCClaim> mstPVCClaim { get; set; }
        public DbSet<MstPVCClaimAudit> MstPVCClaimAudit { get; set; }
        public DbSet<DtPVCClaim> dtPVCClaim { get; set; }
        public DbSet<DtPVCClaimFileUpload> dtPVCClaimFileUpload { get; set; }
        public DbSet<MstPVGClaim> mstPVGClaim { get; set; }
        public DbSet<MstPVGClaimAudit> MstPVGClaimAudit { get; set; }
        public DbSet<DtPVGClaim> dtPVGClaim { get; set; }
        public DbSet<DtPVGClaimFileUpload> dtPVGClaimFileUpload { get; set; }
        public DbSet<DtHRPVGClaimFileUpload> dtHRPVGClaimFileUpload { get; set; }
        public DbSet<DtHRPVCClaimFileUpload> dtHRPVCClaimFileUpload { get; set; }
        public DbSet<MstHRPVGClaimAudit> mstHRPVGClaimAudit { get; set; }
        public DbSet<MstHRPVCClaimAudit> mstHRPVCClaimAudit { get; set; }
        public DbSet<MstEmailAuditLog> mstEmailAuditLog { get; set; }
        public DbSet<MstDelegateUsers> mstDelegateUsers { get; set; }
        Task<int> SaveChangesAsync(CancellationToken cancellationToken);
    }
}
