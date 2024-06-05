using EClaimsEntities.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Data.SqlClient;
using System.Data;

namespace EClaimsEntities
{
    public class RepositoryContext : DbContext, IRepositoryContext
    {
        public RepositoryContext(DbContextOptions<RepositoryContext> options)
            : base(options)
        {
        }

        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            OnBeforeSaving();
            return base.SaveChanges(acceptAllChangesOnSuccess);
        }

        public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default(CancellationToken))
        {
            OnBeforeSaving();
            return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }

        private void OnBeforeSaving()
        {
            foreach (var entry in ChangeTracker.Entries<DtApprovalMatrix>())
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        entry.CurrentValues["IsDeleted"] = false;
                        break;

                    case EntityState.Deleted:
                        entry.State = EntityState.Modified;
                        entry.CurrentValues["IsDeleted"] = true;
                        break;
                }
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // specify global query filter
            modelBuilder.Entity<DtApprovalMatrix>().HasQueryFilter(s => !s.IsDeleted);
            //modelBuilder.Entity<DtUserRoles>().HasKey(dur => dur.UserRoleID);

            modelBuilder.Entity<DtUserRoles>().HasKey(x => x.UserRoleID);
            modelBuilder.Entity<DtUserRoles>()
            .HasOne<MstUser>(x => x.User)
            .WithMany(s => s.DtUserRoles)
            .HasForeignKey(x => x.UserID);

            modelBuilder.Entity<DtUserRoles>()
                        .HasOne<MstRole>(x => x.Role)
                        .WithMany(s => s.DtUserRoles)
                        .HasForeignKey(x => x.RoleID);
            /*
            modelBuilder.Entity<PersonRelative>().HasKey(x => x.Id);
            modelBuilder.Entity<PersonRelative>()
            .HasOne<Person>(x => x.Person)
            .WithMany(s => s.PersonRelative)
            .HasForeignKey(x => x.PersonId);

            modelBuilder.Entity<PersonRelative>()
                        .HasOne<Realtive>(x => x.Realtive)
                        .WithMany(s => s.PersonRelative)
                        .HasForeignKey(x => x.RelativeId);


            //    modelBuilder.Entity<DtUserRoles>()
            //.HasKey(dur => new { dur.UserID, dur.RoleID });
            modelBuilder.Entity<DtUserRoles>()
                .HasOne(mu => mu.User)
                .WithMany(mu => mu.DtUserRoles)
                .HasForeignKey(mu => mu.UserID);
            modelBuilder.Entity<DtUserRoles>()
                .HasOne(ur => ur.Role)
                .WithMany(ur => ur.DtUserRoles)
                .HasForeignKey(ur => ur.RoleID);
            */
        }

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
        public DbSet<MstPVCClaimDraft> mstPVCClaimDraft { get; set; }
        public DbSet<DtPVCClaimDraft> dtPVCClaimDraft { get; set; }
        public DbSet<DtPVCClaimSummaryDraft> dtPVCClaimSummaryDraft { get; set; }
        public DbSet<DtPVGClaimSummaryDraft> dtPVGClaimSummaryDraft { get; set; }
        public DbSet<DtPVCClaimDraftFileUpload> dtPVCClaimDraftFileUpload { get; set; }
        public DbSet<MstPVGClaimDraft> mstPVGClaimDraft { get; set; }
        public DbSet<DtPVGClaimDraft> dtPVGClaimDraft { get; set; }
        public DbSet<DtPVGClaimFileUploadDraft> dtPVGClaimFileUploadDraft { get; set; }
        public DbSet<DtUserRoles> dtUserRoles { get; set; }
        public DbSet<DtUserFacilities> dtUserFacilities { get; set; }
        public DbSet<MstScreens> mstScreens { get; set; }
        public DbSet<MstApprovalMatrix> mstApprovalMatrix { get; set; }
        public DbSet<DtApprovalMatrix> dtApprovalMatrix { get; set; }
        public DbSet<MstMileageClaim> mstMileageClaim { get; set; }
        public DbSet<MstEmailAuditLog> mstEmailAuditLog { get; set; }
        public DbSet<MstMileageClaimDraft> mstMileageClaimDraft { get; set; }
        public DbSet<MstMileageClaimAudit> MstMileageClaimAudit { get; set; }
        public DbSet<DtMileageClaim> dtMileageClaim { get; set; }
        public DbSet<DtMileageClaimDraft> dtMileageClaimDraft { get; set; }
        public DbSet<DtMileageClaimFileUpload> dtMileageClaimFileUpload { get; set; }
        public DbSet<DtMileageClaimFileUploadDraft> dtMileageClaimFileUploadDraft { get; set; }
        public DbSet<MstExpenseClaim> mstExpenseClaim { get; set; }
        public DbSet<MstExpenseClaimDraft> mstExpenseClaimDraft { get; set; }
        public DbSet<MstExpenseClaimAudit> MstExpenseClaimAudit { get; set; }
        public DbSet<DtExpenseClaim> dtExpenseClaim { get; set; }
        public DbSet<DtExpenseClaimDraft> dtExpenseClaimDraft { get; set; }
        public DbSet<DtExpenseClaimFileUpload> dtExpenseClaimFileUpload { get; set; }
        public DbSet<DtExpenseClaimFileUploadDraft> dtExpenseClaimFileUploadDraft { get; set; }
        public DbSet<MstTBClaim> mstTBClaim { get; set; }
        public DbSet<MstTBClaimDraft> mstTBClaimDraft { get; set; }
        public DbSet<MstTBClaimAudit> MstTBClaimAudit { get; set; }
        public DbSet<DtTBClaim> dtTBClaim { get; set; }
        public DbSet<DtTBClaimDraft> dtTBClaimDraft { get; set; }
        public DbSet<DtTBClaimFileUpload> dtTBClaimFileUpload { get; set; }
        public DbSet<DtTBClaimFileUploadDraft> dtTBClaimFileUploadDraft { get; set; }
        public DbSet<MstPVCClaim> mstPVCClaim { get; set; }
        public DbSet<MstHRPVCClaim> mstHRPVCClaim { get; set; }
        public DbSet<MstHRPVCClaimDraft> mstHRPVCClaimDraft { get; set; }
        public DbSet<MstPVCClaimAudit> MstPVCClaimAudit { get; set; }
        public DbSet<DtPVCClaim> dtPVCClaim { get; set; }
        public DbSet<DtHRPVCClaim> dtHRPVCClaim { get; set; }
        public DbSet<DtHRPVCClaimDraft> dtHRPVCClaimdraft { get; set; }
        public DbSet<DtHRPVCClaimSummary> dtHRPVCClaimSummary { get; set; }
        public DbSet<DtHRPVCClaimSummaryDraft> dtHRPVCClaimSummaryDraft { get; set; }
        public DbSet<DtHRPVGClaimSummary> dtHRPVGClaimSummary { get; set; }
        public DbSet<DtHRPVGClaimDraftSummary> dtHRPVGClaimDraftSummary { get; set; }
        public DbSet<DtPVCClaimSummary> dtPVCClaimSummary { get; set; }
        public DbSet<DtPVGClaimSummary> dtPVGClaimSummary { get; set; }
        public DbSet<DtMileageClaimSummary> dtMileageClaimSummary { get; set; }
        public DbSet<DtMileageClaimSummaryDraft> dtMileageClaimSummaryDraft { get; set; }
        public DbSet<DtExpenseClaimSummary> dtExpenseClaimSummary { get; set; }
        public DbSet<DtExpenseClaimSummaryDraft> dtExpenseClaimSummaryDraft { get; set; }
        public DbSet<DtTBClaimSummary> dtTBClaimSummary { get; set; }
        public DbSet<DtTBClaimSummaryDraft> dtTBClaimSummaryDraft { get; set; }
        public DbSet<DtPVCClaimFileUpload> dtPVCClaimFileUpload { get; set; }
        public DbSet<MstPVGClaim> mstPVGClaim { get; set; }
        public DbSet<MstHRPVGClaim> mstHRPVGClaim { get; set; }
        public DbSet<MstHRPVGClaimDraft> mstHRPVGClaimDraft { get; set; }
        public DbSet<MstHRPVGClaimAudit> mstHRPVGClaimAudit { get; set; }
        public DbSet<MstHRPVCClaimAudit> mstHRPVCClaimAudit { get; set; }
        public DbSet<MstPVGClaimAudit> MstPVGClaimAudit { get; set; }
        public DbSet<DtPVGClaim> dtPVGClaim { get; set; }
        public DbSet<DtHRPVGClaim> dtHRPVGClaim { get; set; }
        public DbSet<DtHRPVGClaimDraft> dtHRPVGClaimDraft { get; set; }
        public DbSet<DtPVGClaimFileUpload> dtPVGClaimFileUpload { get; set; }
        public DbSet<DtHRPVGClaimFileUpload> dtHRPVGClaimFileUpload { get; set; }
        public DbSet<DtHRPVCClaimFileUpload> dtHRPVCClaimFileUpload { get; set; }
        public DbSet<DtHRPVCClaimFileUploadDraft> dtHRPVCClaimFileUploadDraft { get; set; }
        public IDbConnection Connection => Database.GetDbConnection();

        public DbSet<MstBankDetails> MstBankDetails { get; set; }
        public DbSet<MstAlternateApprovers> MstAlternateApprovers { get; set; }
        public DbSet<MstDelegateUsers> mstDelegateUsers { get; set; }
        public DbSet<MstUserApprovers> MstUserApprovers { get; set; }
        public DbSet<MstQuery> mstQuery { get; set; }
    }
}
