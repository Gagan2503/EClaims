using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EClaimsRepository.Contracts
{
    public interface IRepositoryWrapper
    {
        IMstDepartmentRepository MstDepartment { get; }
        IMstTaxClassRepository MstTaxClass { get; }
        IMstFacilityRepository MstFacility { get; }
        IMstUserRepository MstUser { get; }
        IMstExpenseCategoryRepository MstExpenseCategory { get; }
        IMstBankSwiftBICRepository MstBankSwiftBIC { get; }
        IMstCostTypeRepository MstCostType { get; }
        IMstCostStructureRepository MstCostStructure { get; }
        IMstClaimTypeRepository MstClaimType { get; }
        IMstRoleRepository MstRole { get; }
        IDtUserRolesRepository DtUserRoles { get; }
        IDtUserFacilitiesRepository DtUserFacilities { get; }
        IMstScreensRepository MstScreens { get; }
        IMstApprovalMatrixRepository MstApprovalMatrix { get; }
        IDtApprovalMatrixRepository DtApprovalMatrix { get; }
        IMstMileageClaimRepository MstMileageClaim { get; }
        IMstMileageClaimDratRepository MstMileageClaimDraft { get; }
        IDtMileageClaimRepository DtMileageClaim { get; }
        IDtMileageClaimDraftRepository DtMileageClaimDraft { get; }
        IMstExpenseClaimRepository MstExpenseClaim { get; }
        IMstExpenseClaimDraftRepository MstExpenseClaimDraft { get; }
        IDtExpenseClaimRepository DtExpenseClaim { get; }
        IDtExpenseClaimDraftRepository DtExpenseClaimDraft { get; }
        IMstTBClaimRepository MstTBClaim { get; }
        IMstTBClaimDraftRepository MstTBClaimDraft { get; }
        IDtTBClaimRepository DtTBClaim { get; }
        IDtTBClaimDraftRepository DtTBClaimDraft { get; }
        IMstPVCClaimRepository MstPVCClaim { get; }
        IMstPVCClaimAuditRepository MstPVCClaimAudit { get; }
        IMstPVGClaimAuditRepository MstPVGClaimAudit { get; }
        IDtPVCClaimRepository DtPVCClaim { get; }
        IMstPVGClaimRepository MstPVGClaim { get; }
        IDtPVGClaimRepository DtPVGClaim { get; }
        IMstBankDetailsRepository MstBankDetails { get; }
        IMstAlternateApproversRepository MstAlternateApprover { get; }
        IDtMileageClaimFileUploadRepository DtMileageClaimFileUpload { get; }
        IDtMileageClaimFileUploadDraftRepository DtMileageClaimFileUploadDraft { get; }
        IDtExpenseClaimFileUploadRepository DtExpenseClaimFileUpload { get; }
        IDtExpenseClaimFileUploadDraftRepository DtExpenseClaimFileUploadDraft { get; }
        IDtPVCClaimFileUploadRepository DtPVCClaimFileUpload { get; }
        IDtTBClaimFileUploadRepository DtTBClaimFileUpload { get; }
        IDtTBClaimFileUploadDraftRepository DtTBClaimFileUploadDraft { get; }
        IMstExpenseClaimAuditRepository MstExpenseClaimAudit { get; }
        IMstMileageClaimAuditRepository MstMileageClaimAudit { get; }
        IMstTBClaimAuditRepository MstTBClaimAudit { get; }
        IMstUserApproversRepository MstUserApprovers { get; }
        IMstDelegateUsersRepository MstDelegateUsers { get; }
        IMstQueryRepository MstQuery { get; }
        IDtPVGClaimFileUploadRepository DtPVGClaimFileUpload { get; }
        IMstHRPVCClaimRepository MstHRPVCClaim { get; }
        IMstHRPVCClaimDraftRepository MstHRPVCClaimDraft { get; }
        IDtHRPVCClaimRepository DtHRPVCClaim { get; }
        IDtHRPVCClaimDraftRepository DtHRPVCClaimDraft { get; }
        IDtHRPVCClaimSummaryRepository DtHRPVCClaimSummary { get; }
        IDtHRPVGClaimSummaryRepository DtHRPVGClaimSummary { get; }
        IDtPVCClaimSummaryRepository DtPVCClaimSummary { get; }
        IDtPVGClaimSummaryRepository DtPVGClaimSummary { get; }
        IDtMileageClaimSummaryRepository DtMileageClaimSummary { get; }
        IDtMileageClaimSummaryDraftRepository DtMileageClaimSummaryDraft { get; }
        IDtExpenseClaimSummaryRepository DtExpenseClaimSummary { get; }
        IDtExpenseClaimSummaryDraftRepository DtExpenseClaimSummaryDraft { get; }
        IDtTBClaimSummaryRepository DtTBClaimSummary { get; }
        IDtTBClaimSummaryDraftRepository DtTBClaimSummaryDraft { get; }
        IMstHRPVGClaimRepository MstHRPVGClaim { get; }
        IMstHRPVGClaimDraftRepository MstHRPVGClaimDraft { get; }
        IDtHRPVGClaimRepository DtHRPVGClaim { get; }
        IDtHRPVGDraftClaimRepository DtHRPVGClaimDraft { get; }
        IDtHRPVGClaimFileUploadRepository DtHRPVGClaimFileUpload { get; }
        IDtHRPVCClaimFileUploadRepository DtHRPVCClaimFileUpload { get; }
        IDtHRPVGClaimFileUploadRepositoryDraft DtHRPVCGlaimFileUploadDraft { get; }
        IDtHRPVCClaimFileUploadRepositoryDraft DtHRPVCClaimFileUploadDraft { get; }
        IMstHRPVCClaimAuditRepository MstHRPVCClaimAudit { get; }
        IMstHRPVGClaimAuditRepository MstHRPVGClaimAudit { get; }
        IMstPVCClaimDraftRepository MstPVCClaimDraft { get; }
        IDtPVCClaimDraftRepository DtPVCClaimDraft { get; }
        IMstPVGClaimDraftRepository MstPVGClaimDraft { get; }
        IDtPVGClaimDraftRepository DtPVGClaimDraft { get; }
        IDtPVCClaimFileUploadDraftRepository DtPVCClaimFileUploadDraft { get; }
        IDtPVGClaimFileUploadDraftRepository DtPVGClaimFileUploadDraft { get; }
        IDtPVGClaimSummaryDraftRepository DtPVGClaimSummaryDraft { get; }

        Task SaveAsync();
    }
}
