using EClaimsEntities;
using EClaimsEntities.Models;
using EClaimsRepository.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EClaimsRepository.Repository
{
    public class RepositoryWrapper : IRepositoryWrapper
    {
        private RepositoryContext _repoContext;
        private IRepositoryContext _repoContext1;
        private IApplicationReadDbConnection _readDbConnection;
        private IApplicationWriteDbConnection _writeDbConnection;
        private IMstDepartmentRepository _mstDepartment;
        private IMstTaxClassRepository _mstTaxClass;
        private IMstFacilityRepository _mstFacility;
        private IMstUserRepository _mstUser;
        private IMstExpenseCategoryRepository _mstExpenseCategory;
        private IMstBankSwiftBICRepository _mstBankSwiftBIC;
        private IMstCostTypeRepository _mstCostType;
        private IMstCostStructureRepository _mstCostStructure;
        private IMstClaimTypeRepository _mstClaimType;
        private IMstRoleRepository _mstRole;
        private IDtUserRolesRepository _dtUserRoles;
        private IDtUserFacilitiesRepository _dtUserFacilities;
        private IMstScreensRepository _mstScreens;
        private IMstApprovalMatrixRepository _mstApprovalMatrix;
        private IDtApprovalMatrixRepository _dtApprovalMatrix;
        private IMstMileageClaimRepository _mstMileageClaim;
        private IMstMileageClaimDratRepository _mstMileageClaimDraft;
        private IDtMileageClaimRepository _dtMileageClaim;
        private IDtMileageClaimDraftRepository _dtMileageClaimDraft;
        private IMstExpenseClaimRepository _mstExpenseClaim;
        private IMstExpenseClaimDraftRepository _mstExpenseClaimDraft;
        private IDtExpenseClaimRepository _dtExpenseClaim;
        private IDtExpenseClaimDraftRepository _dtExpenseClaimDraft;
        private IMstTBClaimRepository _mstTBClaim;
        private IMstTBClaimDraftRepository _mstTBClaimDraft;
        private IDtTBClaimRepository _dtTBClaim;
        private IDtTBClaimDraftRepository _dtTBClaimDraft;
        private IMstPVCClaimRepository _mstPVCClaim;
        private IMstPVCClaimDraftRepository _mstPVCClaimDraft;
        private IDtPVCClaimRepository _dtPVCClaim;
        private IDtPVCClaimDraftRepository _dtPVCClaimDraft;
        private IMstPVGClaimRepository _mstPVGClaim;
        private IMstPVGClaimDraftRepository _mstPVGClaimDraft;
        private IDtPVGClaimRepository _dtPVGClaim;
        private IDtPVGClaimDraftRepository _dtPVGClaimDraft;
        private IDtPVGClaimSummaryDraftRepository _dtPVGClaimSummaryDraft;
        private IDtPVCClaimFileUploadDraftRepository _dtPVCClaimDraftFileUpload;
        private IDtPVGClaimFileUploadDraftRepository _dtPVGClaimFileUploadDraft;
        private IDtMileageClaimFileUploadRepository _dtMileageClaimFileUpload;
        private IDtMileageClaimFileUploadDraftRepository _dtMileageClaimFileUploadDraft;
        private IMstBankDetailsRepository _mstBankDetails;
        private IMstAlternateApproversRepository _mstAlternateApprovers;
        private IMstUserApproversRepository _mstUserAppoversDetails;
        private IMstDelegateUsersRepository _mstDelegateUsers;
        private IDtHRPVGClaimRepository _dtHRPVGClaim;
        private IDtHRPVGDraftClaimRepository _dtHRPVGClaimDraft;
        private IMstHRPVCClaimRepository _mstHRPVCClaim;
        private IMstHRPVCClaimDraftRepository _mstHRPVCClaimDraft;
        private IDtHRPVCClaimRepository _dtHRPVCClaim;
        private IDtHRPVCClaimDraftRepository _dtHRPVCClaimDraft;
        private IDtHRPVCClaimSummaryRepository _dtHRPVCClaimSummary;
        private IDtHRPVGClaimSummaryRepository _dtHRPVGClaimSummary;
        private IDtPVCClaimSummaryRepository _dtPVCClaimSummary;
        private IDtPVGClaimSummaryRepository _dtPVGClaimSummary;
        private IDtMileageClaimSummaryRepository _dtMileageClaimSummary;
        private IDtMileageClaimSummaryDraftRepository _dtMileageClaimSummaryDraft;
        private IDtExpenseClaimSummaryRepository _dtExpenseClaimSummary;
        private IDtExpenseClaimSummaryDraftRepository _dtExpenseClaimSummaryDraft;
        private IDtTBClaimSummaryRepository _dtTBClaimSummary;
        private IDtTBClaimSummaryDraftRepository _dtTBClaimSummaryDraft;
        private IMstHRPVGClaimRepository _mstHRPVGClaim;
        private IMstHRPVGClaimDraftRepository _mstHRPVGClaimDraft;
        private IDtExpenseClaimFileUploadRepository _dtExpenseClaimFileUpload;
        private IDtExpenseClaimFileUploadDraftRepository _dtExpenseClaimFileUploadDraft;
        private IDtPVCClaimFileUploadRepository _dtPVCClaimFileUpload;
        private IDtPVGClaimFileUploadRepository _dtPVGClaimFileUpload;
        private IDtTBClaimFileUploadRepository _dtTBClaimFileUpload;
        private IDtTBClaimFileUploadDraftRepository _dtTBClaimFileUploadDraft;
        private IMstMileageClaimAuditRepository _mstMileageClaimAudit;
        private IMstExpenseClaimAuditRepository _mstExpenseClaimAudit;
        private IMstPVCClaimAuditRepository _mstPVCClaimAudit;
        private IMstPVGClaimAuditRepository _mstPVGClaimAudit;
        private IMstTBClaimAuditRepository _mstTBClaimAudit;
        private IDtHRPVCClaimFileUploadRepository _dtHRPVCClaimFileUpload;
        private IDtHRPVCClaimFileUploadRepositoryDraft _dtHRPVCClaimFileUploadDraft;
        private IDtHRPVGClaimFileUploadRepository _dtHRPVGClaimFileUpload;
        private IDtHRPVGClaimFileUploadRepositoryDraft _dtHRPVGClaimFileUploadDraft;
        private IMstHRPVCClaimAuditRepository _mstHRPVCClaimAudit;
        private IMstHRPVGClaimAuditRepository _mstHRPVGClaimAudit;
        private IMstQueryRepository _mstQuery;
        public IMstDepartmentRepository MstDepartment
        {
            get
            {
                if (_mstDepartment == null)
                {
                    _mstDepartment = new MstDepartmentRepository(_repoContext, _readDbConnection, _writeDbConnection);
                }

                return _mstDepartment;
            }
        }
        public IMstTaxClassRepository MstTaxClass
        {
            get
            {
                if (_mstTaxClass == null)
                {
                    _mstTaxClass = new MstTaxClassRepository(_repoContext, _readDbConnection, _writeDbConnection);
                }

                return _mstTaxClass;
            }
        }

        public IMstFacilityRepository MstFacility
        {
            get
            {
                if (_mstFacility == null)
                {
                    _mstFacility = new MstFacilityRepository(_repoContext, _readDbConnection, _writeDbConnection);
                }

                return _mstFacility;
            }
        }


        public IMstMileageClaimAuditRepository MstMileageClaimAudit
        {
            get
            {
                if (_mstMileageClaimAudit == null)
                {
                    _mstMileageClaimAudit = new MstMileageClaimAuditRepository(_repoContext, _readDbConnection, _writeDbConnection);
                }

                return _mstMileageClaimAudit;
            }
        }

        public IMstExpenseClaimAuditRepository MstExpenseClaimAudit
        {
            get
            {
                if (_mstExpenseClaimAudit == null)
                {
                    _mstExpenseClaimAudit = new MstExpenseClaimAuditRepository(_repoContext, _readDbConnection, _writeDbConnection);
                }

                return _mstExpenseClaimAudit;
            }
        }

        public IMstPVCClaimAuditRepository MstPVCClaimAudit
        {
            get
            {
                if (_mstPVCClaimAudit == null)
                {
                    _mstPVCClaimAudit = new MstPVCClaimAuditRepository(_repoContext, _readDbConnection, _writeDbConnection);
                }

                return _mstPVCClaimAudit;
            }
        }

        public IMstPVGClaimAuditRepository MstPVGClaimAudit
        {
            get
            {
                if (_mstPVGClaimAudit == null)
                {
                    _mstPVGClaimAudit = new MstPVGClaimAuditRepository(_repoContext, _readDbConnection, _writeDbConnection);
                }

                return _mstPVGClaimAudit;
            }
        }

        public IMstHRPVCClaimAuditRepository MstHRPVCClaimAudit
        {
            get
            {
                if (_mstHRPVCClaimAudit == null)
                {
                    _mstHRPVCClaimAudit = new MstHRPVCClaimAuditRepository(_repoContext, _readDbConnection, _writeDbConnection);
                }

                return _mstHRPVCClaimAudit;
            }
        }

        public IMstHRPVGClaimAuditRepository MstHRPVGClaimAudit
        {
            get
            {
                if (_mstHRPVGClaimAudit == null)
                {
                    _mstHRPVGClaimAudit = new MstHRPVGClaimAuditRepository(_repoContext, _readDbConnection, _writeDbConnection);
                }

                return _mstHRPVGClaimAudit;
            }
        }
        public IMstTBClaimAuditRepository MstTBClaimAudit
        {
            get
            {
                if (_mstTBClaimAudit == null)
                {
                    _mstTBClaimAudit = new MstTBClaimAuditRepository(_repoContext, _readDbConnection, _writeDbConnection);
                }

                return _mstTBClaimAudit;
            }
        }


        public IMstBankDetailsRepository MstBankDetails
        {
            get
            {
                if (_mstBankDetails == null)
                {
                    _mstBankDetails = new MstBankDetailsRepository(_repoContext, _readDbConnection, _writeDbConnection);
                }

                return _mstBankDetails;
            }
        }
        public IMstAlternateApproversRepository MstAlternateApprover
        {
            get
            {
                if (_mstAlternateApprovers == null)
                {
                    _mstAlternateApprovers = new MstAlternateApproversRepository(_repoContext, _readDbConnection, _writeDbConnection);
                }

                return _mstAlternateApprovers;
            }
        }

        public IMstUserRepository MstUser
        {
            get
            {
                if (_mstUser == null)
                {
                    _mstUser = new MstUserRepository(_repoContext, _readDbConnection, _writeDbConnection);
                }

                return _mstUser;
            }
        }

        public IMstExpenseCategoryRepository MstExpenseCategory
        {
            get
            {
                if (_mstExpenseCategory == null)
                {
                    _mstExpenseCategory = new MstExpenseCategoryRepository(_repoContext, _readDbConnection, _writeDbConnection);
                }

                return _mstExpenseCategory;
            }
        }

        public IMstBankSwiftBICRepository MstBankSwiftBIC
        {
            get
            {
                if (_mstBankSwiftBIC == null)
                {
                    _mstBankSwiftBIC = new MstBankSwiftBICRepository(_repoContext, _readDbConnection, _writeDbConnection);
                }

                return _mstBankSwiftBIC;
            }
        }

        public IMstCostTypeRepository MstCostType
        {
            get
            {
                if (_mstCostType == null)
                {
                    _mstCostType = new MstCostTypeRepository(_repoContext, _readDbConnection, _writeDbConnection);
                }

                return _mstCostType;
            }
        }

        public IMstCostStructureRepository MstCostStructure
        {
            get
            {
                if (_mstCostStructure == null)
                {
                    _mstCostStructure = new MstCostStructureRepository(_repoContext, _readDbConnection, _writeDbConnection);
                }

                return _mstCostStructure;
            }
        }

        public IMstClaimTypeRepository MstClaimType
        {
            get
            {
                if (_mstClaimType == null)
                {
                    _mstClaimType = new MstClaimTypeRepository(_repoContext, _readDbConnection, _writeDbConnection);
                }

                return _mstClaimType;
            }
        }

        public IMstRoleRepository MstRole
        {
            get
            {
                if (_mstRole == null)
                {
                    _mstRole = new MstRoleRepository(_repoContext, _readDbConnection, _writeDbConnection);
                }

                return _mstRole;
            }
        }


        public IMstUserApproversRepository MstUserApprovers
        {
            get
            {
                if (_mstUserAppoversDetails == null)
                {
                    _mstUserAppoversDetails = new MstUserApproversRepository(_repoContext, _readDbConnection, _writeDbConnection);
                }
                return _mstUserAppoversDetails;
            }
        }

        public IMstDelegateUsersRepository MstDelegateUsers
        {
            get
            {
                if (_mstDelegateUsers == null)
                {
                    _mstDelegateUsers = new MstDelegateUsersRepository(_repoContext, _readDbConnection, _writeDbConnection);
                }
                return _mstDelegateUsers;
            }
        }

        public IDtUserRolesRepository DtUserRoles
        {
            get
            {
                if (_dtUserRoles == null)
                {
                    _dtUserRoles = new DtUserRolesRepository(_repoContext, _readDbConnection, _writeDbConnection);
                }

                return _dtUserRoles;
            }
        }

        public IDtUserFacilitiesRepository DtUserFacilities
        {
            get
            {
                if (_dtUserFacilities == null)
                {
                    _dtUserFacilities = new DtUserFacilitiesRepository(_repoContext, _readDbConnection, _writeDbConnection);
                }

                return _dtUserFacilities;
            }
        }

        public IMstScreensRepository MstScreens
        {
            get
            {
                if (_mstScreens == null)
                {
                    _mstScreens = new MstScreensRepository(_repoContext, _readDbConnection, _writeDbConnection);
                }

                return _mstScreens;
            }
        }

        public IMstApprovalMatrixRepository MstApprovalMatrix
        {
            get
            {
                if (_mstApprovalMatrix == null)
                {
                    _mstApprovalMatrix = new MstApprovalMatrixRepository(_repoContext, _readDbConnection, _writeDbConnection);
                }

                return _mstApprovalMatrix;
            }
        }

        public IDtApprovalMatrixRepository DtApprovalMatrix
        {
            get
            {
                if (_dtApprovalMatrix == null)
                {
                    _dtApprovalMatrix = new DtApprovalMatrixRepository(_repoContext, _readDbConnection, _writeDbConnection);
                }

                return _dtApprovalMatrix;
            }
        }

        public IMstMileageClaimRepository MstMileageClaim
        {
            get
            {
                if (_mstMileageClaim == null)
                {
                    _mstMileageClaim = new MstMileageClaimRepository(_repoContext, _readDbConnection, _writeDbConnection);
                }

                return _mstMileageClaim;
            }
        }

        public IMstMileageClaimDratRepository MstMileageClaimDraft
        {
            get
            {
                if (_mstMileageClaimDraft == null)
                {
                    _mstMileageClaimDraft = new MstMileageClaimDraftRepository(_repoContext, _readDbConnection, _writeDbConnection);
                }

                return _mstMileageClaimDraft;
            }
        }

        public IDtMileageClaimRepository DtMileageClaim
        {
            get
            {
                if (_dtMileageClaim == null)
                {
                    _dtMileageClaim = new DtMileageClaimRepository(_repoContext, _readDbConnection, _writeDbConnection);
                }

                return _dtMileageClaim;
            }
        }

        public IDtMileageClaimDraftRepository DtMileageClaimDraft
        {
            get
            {
                if (_dtMileageClaimDraft == null)
                {
                    _dtMileageClaimDraft = new DtMileageClaimDraftRepository(_repoContext, _readDbConnection, _writeDbConnection);
                }

                return _dtMileageClaimDraft;
            }
        }

        public IMstExpenseClaimRepository MstExpenseClaim
        {
            get
            {
                if (_mstExpenseClaim == null)
                {
                    _mstExpenseClaim = new MstExpenseClaimRepository(_repoContext, _readDbConnection, _writeDbConnection);
                }

                return _mstExpenseClaim;
            }
        }

        public IMstExpenseClaimDraftRepository MstExpenseClaimDraft
        {
            get
            {
                if (_mstExpenseClaimDraft == null)
                {
                    _mstExpenseClaimDraft = new MstExpenseClaimDraftRepository(_repoContext, _readDbConnection, _writeDbConnection);
                }

                return _mstExpenseClaimDraft;
            }
        }

        public IDtExpenseClaimRepository DtExpenseClaim
        {
            get
            {
                if (_dtExpenseClaim == null)
                {
                    _dtExpenseClaim = new DtExpenseClaimRepository(_repoContext, _readDbConnection, _writeDbConnection);
                }

                return _dtExpenseClaim;
            }
        }

        public IDtExpenseClaimDraftRepository DtExpenseClaimDraft
        {
            get
            {
                if (_dtExpenseClaimDraft == null)
                {
                    _dtExpenseClaimDraft = new DtExpenseClaimDraftRepository(_repoContext, _readDbConnection, _writeDbConnection);
                }

                return _dtExpenseClaimDraft;
            }
        }

        public IMstTBClaimRepository MstTBClaim
        {
            get
            {
                if (_mstTBClaim == null)
                {
                    _mstTBClaim = new MstTBClaimRepository(_repoContext, _readDbConnection, _writeDbConnection);
                }

                return _mstTBClaim;
            }
        }

        public IMstTBClaimDraftRepository MstTBClaimDraft
        {
            get
            {
                if (_mstTBClaimDraft == null)
                {
                    _mstTBClaimDraft = new MstTBClaimDraftRepository(_repoContext, _readDbConnection, _writeDbConnection);
                }

                return _mstTBClaimDraft;
            }
        }

        public IDtTBClaimRepository DtTBClaim
        {
            get
            {
                if (_dtTBClaim == null)
                {
                    _dtTBClaim = new DtTBClaimRepository(_repoContext, _readDbConnection, _writeDbConnection);
                }

                return _dtTBClaim;
            }
        }

        public IDtTBClaimDraftRepository DtTBClaimDraft
        {
            get
            {
                if (_dtTBClaimDraft == null)
                {
                    _dtTBClaimDraft = new DtTBClaimDraftRepository(_repoContext, _readDbConnection, _writeDbConnection);
                }

                return _dtTBClaimDraft;
            }
        }

        public IMstPVCClaimRepository MstPVCClaim
        {
            get
            {
                if (_mstPVCClaim == null)
                {
                    _mstPVCClaim = new MstPVCClaimRepository(_repoContext, _readDbConnection, _writeDbConnection);
                }

                return _mstPVCClaim;
            }
        }

        public IDtPVCClaimRepository DtPVCClaim
        {
            get
            {
                if (_dtPVCClaim == null)
                {
                    _dtPVCClaim = new DtPVCClaimRepository(_repoContext, _readDbConnection, _writeDbConnection);
                }

                return _dtPVCClaim;
            }
        }

        public IMstPVGClaimRepository MstPVGClaim
        {
            get
            {
                if (_mstPVGClaim == null)
                {
                    _mstPVGClaim = new MstPVGClaimRepository(_repoContext, _readDbConnection, _writeDbConnection);
                }

                return _mstPVGClaim;
            }
        }

        public IDtPVGClaimRepository DtPVGClaim
        {
            get
            {
                if (_dtPVGClaim == null)
                {
                    _dtPVGClaim = new DtPVGClaimRepository(_repoContext, _readDbConnection, _writeDbConnection);
                }

                return _dtPVGClaim;
            }
        }

        public IDtMileageClaimFileUploadRepository DtMileageClaimFileUpload
        {
            get
            {
                if (_dtMileageClaimFileUpload == null)
                {
                    _dtMileageClaimFileUpload = new DtMileageClaimFileUploadRepository(_repoContext, _readDbConnection, _writeDbConnection);
                }

                return _dtMileageClaimFileUpload;
            }
        }

        public IDtMileageClaimFileUploadDraftRepository DtMileageClaimFileUploadDraft
        {
            get
            {
                if (_dtMileageClaimFileUploadDraft == null)
                {
                    _dtMileageClaimFileUploadDraft = new DtMileageClaimFileUploadDraftRepository(_repoContext, _readDbConnection, _writeDbConnection);
                }

                return _dtMileageClaimFileUploadDraft;
            }
        }

        public IDtTBClaimFileUploadRepository DtTBClaimFileUpload
        {
            get
            {
                if (_dtTBClaimFileUpload == null)
                {
                    _dtTBClaimFileUpload = new DtTBClaimFileUploadRepository(_repoContext, _readDbConnection, _writeDbConnection);
                }

                return _dtTBClaimFileUpload;
            }
        }

        public IDtTBClaimFileUploadDraftRepository DtTBClaimFileUploadDraft
        {
            get
            {
                if (_dtTBClaimFileUploadDraft == null)
                {
                    _dtTBClaimFileUploadDraft = new DtTBClaimFileUploadDraftRepository(_repoContext, _readDbConnection, _writeDbConnection);
                }

                return _dtTBClaimFileUploadDraft;
            }
        }

        public IDtExpenseClaimFileUploadRepository DtExpenseClaimFileUpload
        {
            get
            {
                if (_dtExpenseClaimFileUpload == null)
                {
                    _dtExpenseClaimFileUpload = new DtExpenseClaimFileUploadRepository(_repoContext, _readDbConnection, _writeDbConnection);
                }

                return _dtExpenseClaimFileUpload;
            }
        }

        public IDtExpenseClaimFileUploadDraftRepository DtExpenseClaimFileUploadDraft
        {
            get
            {
                if (_dtExpenseClaimFileUploadDraft == null)
                {
                    _dtExpenseClaimFileUploadDraft = new DtExpenseClaimFileUploadDraftRepository(_repoContext, _readDbConnection, _writeDbConnection);
                }

                return _dtExpenseClaimFileUploadDraft;
            }
        }

        public IDtPVCClaimFileUploadRepository DtPVCClaimFileUpload
        {
            get
            {
                if (_dtPVCClaimFileUpload == null)
                {
                    _dtPVCClaimFileUpload = new DtPVCClaimFileUploadRepository(_repoContext, _readDbConnection, _writeDbConnection);
                }

                return _dtPVCClaimFileUpload;
            }
        }
        public IMstPVCClaimDraftRepository MstPVCClaimDraft
        {
            get
            {
                if (_mstPVCClaimDraft == null)
                {
                    _mstPVCClaimDraft = new MstPVCClaimDraftRepository(_repoContext, _readDbConnection, _writeDbConnection);
                }

                return _mstPVCClaimDraft;
            }
        }

        public IDtPVCClaimDraftRepository DtPVCClaimDraft
        {
            get
            {
                if (_dtPVCClaimDraft == null)
                {
                    _dtPVCClaimDraft = new DtPVCClaimDraftRepository(_repoContext, _readDbConnection, _writeDbConnection);
                }

                return _dtPVCClaimDraft;
            }
        }
        public IMstPVGClaimDraftRepository MstPVGClaimDraft
        {
            get
            {
                if (_mstPVGClaimDraft == null)
                {
                    _mstPVGClaimDraft = new MstPVGClaimDraftRepository(_repoContext, _readDbConnection, _writeDbConnection);
                }

                return _mstPVGClaimDraft;
            }
        }
        public IDtPVGClaimDraftRepository DtPVGClaimDraft
        {
            get
            {
                if (_dtPVGClaimDraft == null)
                {
                    _dtPVGClaimDraft = new DtPVGClaimDraftRepository(_repoContext, _readDbConnection, _writeDbConnection);
                }

                return _dtPVGClaimDraft;
            }
        }
        public IDtPVCClaimFileUploadDraftRepository DtPVCClaimFileUploadDraft
        {
            get
            {
                if (_dtPVCClaimDraftFileUpload == null)
                {
                    _dtPVCClaimDraftFileUpload = new DtPVCClaimFileUploadDraftRepository(_repoContext, _readDbConnection, _writeDbConnection);
                }

                return _dtPVCClaimDraftFileUpload;
            }
        }
        public IDtPVGClaimFileUploadDraftRepository DtPVGClaimFileUploadDraft
        {
            get
            {
                if (_dtPVGClaimFileUploadDraft == null)
                {
                    _dtPVGClaimFileUploadDraft = new DtPVGClaimFileUploadDraftRepository(_repoContext, _readDbConnection, _writeDbConnection);
                }

                return _dtPVGClaimFileUploadDraft;
            }
        }
        public IDtPVGClaimSummaryDraftRepository DtPVGClaimSummaryDraft
        {
            get
            {
                if (_dtPVGClaimSummaryDraft == null)
                {
                    _dtPVGClaimSummaryDraft = new DtPVGClaimSummaryDraftRepository(_repoContext, _readDbConnection, _writeDbConnection);
                }

                return _dtPVGClaimSummaryDraft;
            }
        }
        public IDtPVGClaimFileUploadRepository DtPVGClaimFileUpload
        {
            get
            {
                if (_dtPVGClaimFileUpload == null)
                {
                    _dtPVGClaimFileUpload = new DtPVGClaimFileUploadRepository(_repoContext, _readDbConnection, _writeDbConnection);
                }

                return _dtPVGClaimFileUpload;
            }
        }

        public IMstHRPVGClaimRepository MstHRPVGClaim
        {
            get
            {
                if (_mstHRPVGClaim == null)
                {
                    _mstHRPVGClaim = new MstHRPVGClaimRepository(_repoContext, _readDbConnection, _writeDbConnection);
                }

                return _mstHRPVGClaim;
            }
        }
        public IMstHRPVGClaimDraftRepository MstHRPVGClaimDraft
        {
            get
            {
                if (_mstHRPVGClaimDraft == null)
                {
                    _mstHRPVGClaimDraft = new MstHRPVGClaimDraftRepository(_repoContext, _readDbConnection, _writeDbConnection);
                }

                return _mstHRPVGClaimDraft;
            }
        }

        public IDtHRPVGClaimRepository DtHRPVGClaim
        {
            get
            {
                if (_dtHRPVGClaim == null)
                {
                    _dtHRPVGClaim = new DtHRPVGClaimRepository(_repoContext, _readDbConnection, _writeDbConnection);
                }

                return _dtHRPVGClaim;
            }
        }
        public IDtHRPVGDraftClaimRepository DtHRPVGClaimDraft
        {
            get
            {
                if (_dtHRPVGClaimDraft == null)
                {
                    _dtHRPVGClaimDraft = new DtHRPVGDraftClaimRepository(_repoContext, _readDbConnection, _writeDbConnection);
                }

                return _dtHRPVGClaimDraft;
            }
        }
        public IMstHRPVCClaimRepository MstHRPVCClaim
        {
            get
            {
                if (_mstHRPVCClaim == null)
                {
                    _mstHRPVCClaim = new MstHRPVCClaimRepository(_repoContext, _readDbConnection, _writeDbConnection);
                }

                return _mstHRPVCClaim;
            }
        }
        public IMstHRPVCClaimDraftRepository MstHRPVCClaimDraft
        {
            get
            {
                if (_mstHRPVCClaimDraft == null)
                {
                    _mstHRPVCClaimDraft = new MstHRPVCClaimDraftRepository(_repoContext, _readDbConnection, _writeDbConnection);
                }

                return _mstHRPVCClaimDraft;
            }
        }
        public IDtHRPVCClaimRepository DtHRPVCClaim
        {
            get
            {
                if (_dtHRPVCClaim == null)
                {
                    _dtHRPVCClaim = new DtHRPVCClaimRepository(_repoContext, _readDbConnection, _writeDbConnection);
                }

                return _dtHRPVCClaim;
            }
        }
        public IDtHRPVCClaimDraftRepository DtHRPVCClaimDraft
        {
            get
            {
                if (_dtHRPVCClaimDraft == null)
                {
                    _dtHRPVCClaimDraft = new DtHRPVCClaimDraftRepository(_repoContext, _readDbConnection, _writeDbConnection);
                }

                return _dtHRPVCClaimDraft;
            }
        }
        public IDtHRPVCClaimSummaryRepository DtHRPVCClaimSummary
        {
            get
            {
                if (_dtHRPVCClaimSummary == null)
                {
                    _dtHRPVCClaimSummary = new DtHRPVCClaimSummaryRepository(_repoContext, _readDbConnection, _writeDbConnection);
                }

                return _dtHRPVCClaimSummary;
            }
        }

        public IDtHRPVGClaimSummaryRepository DtHRPVGClaimSummary
        {
            get
            {
                if (_dtHRPVGClaimSummary == null)
                {
                    _dtHRPVGClaimSummary = new DtHRPVGClaimSummaryRepository(_repoContext, _readDbConnection, _writeDbConnection);
                }

                return _dtHRPVGClaimSummary;
            }
        }

        public IDtPVCClaimSummaryRepository DtPVCClaimSummary
        {
            get
            {
                if (_dtPVCClaimSummary == null)
                {
                    _dtPVCClaimSummary = new DtPVCClaimSummaryRepository(_repoContext, _readDbConnection, _writeDbConnection);
                }

                return _dtPVCClaimSummary;
            }
        }

        public IDtPVGClaimSummaryRepository DtPVGClaimSummary
        {
            get
            {
                if (_dtPVGClaimSummary == null)
                {
                    _dtPVGClaimSummary = new DtPVGClaimSummaryRepository(_repoContext, _readDbConnection, _writeDbConnection);
                }

                return _dtPVGClaimSummary;
            }
        }

        public IDtMileageClaimSummaryRepository DtMileageClaimSummary
        {
            get
            {
                if (_dtMileageClaimSummary == null)
                {
                    _dtMileageClaimSummary = new DtMileageClaimSummaryRepository(_repoContext, _readDbConnection, _writeDbConnection);
                }

                return _dtMileageClaimSummary;
            }
        }
        public IDtMileageClaimSummaryDraftRepository DtMileageClaimSummaryDraft
        {
            get
            {
                if (_dtMileageClaimSummaryDraft == null)
                {
                    _dtMileageClaimSummaryDraft = new DtMileageClaimSummaryDraftRepository(_repoContext, _readDbConnection, _writeDbConnection);
                }

                return _dtMileageClaimSummaryDraft;
            }
        }

        public IDtExpenseClaimSummaryRepository DtExpenseClaimSummary
        {
            get
            {
                if (_dtExpenseClaimSummary == null)
                {
                    _dtExpenseClaimSummary = new DtExpenseClaimSummaryRepository(_repoContext, _readDbConnection, _writeDbConnection);
                }

                return _dtExpenseClaimSummary;
            }
        }
        public IDtExpenseClaimSummaryDraftRepository DtExpenseClaimSummaryDraft
        {
            get
            {
                if (_dtExpenseClaimSummaryDraft == null)
                {
                    _dtExpenseClaimSummaryDraft = new DtExpenseClaimSummaryDraftRepository(_repoContext, _readDbConnection, _writeDbConnection);
                }

                return _dtExpenseClaimSummaryDraft;
            }
        }
        public IDtTBClaimSummaryRepository DtTBClaimSummary
        {
            get
            {
                if (_dtTBClaimSummary == null)
                {
                    _dtTBClaimSummary = new DtTBClaimSummaryRepository(_repoContext, _readDbConnection, _writeDbConnection);
                }

                return _dtTBClaimSummary;
            }
        }
        public IDtTBClaimSummaryDraftRepository DtTBClaimSummaryDraft
        {
            get
            {
                if (_dtTBClaimSummaryDraft == null)
                {
                    _dtTBClaimSummaryDraft = new DtTBClaimSummaryDraftRepository(_repoContext, _readDbConnection, _writeDbConnection);
                }

                return _dtTBClaimSummaryDraft;
            }
        }

        public IDtHRPVCClaimFileUploadRepository DtHRPVCClaimFileUpload
        {
            get
            {
                if (_dtHRPVCClaimFileUpload == null)
                {
                    _dtHRPVCClaimFileUpload = new DtHRPVCClaimFileUploadRepository(_repoContext, _readDbConnection, _writeDbConnection);
                }

                return _dtHRPVCClaimFileUpload;
            }
        }

        public IDtHRPVCClaimFileUploadRepositoryDraft DtHRPVCClaimFileUploadDraft
        {
            get
            {
                if (_dtHRPVCClaimFileUploadDraft == null)
                {
                    _dtHRPVCClaimFileUploadDraft = new DtHRPVCClaimFileUploadRepositoryDraft(_repoContext, _readDbConnection, _writeDbConnection);
                }

                return _dtHRPVCClaimFileUploadDraft;
            }
        }
        public IDtHRPVGClaimFileUploadRepository DtHRPVGClaimFileUpload
        {
            get
            {
                if (_dtHRPVGClaimFileUpload == null)
                {
                    _dtHRPVGClaimFileUpload = new DtHRPVGClaimFileUploadRepository(_repoContext, _readDbConnection, _writeDbConnection);
                }

                return _dtHRPVGClaimFileUpload;
            }
        }
        public IMstQueryRepository MstQuery
        {
            get
            {
                if (_mstQuery == null)
                {
                    _mstQuery = new MstQueryRepository(_repoContext, _readDbConnection, _writeDbConnection);
                }

                return _mstQuery;
            }
        }

       
        public IDtHRPVGClaimFileUploadRepositoryDraft DtHRPVCGlaimFileUploadDraft
        {
            get
            {
                if (_dtHRPVGClaimFileUploadDraft == null)
                {
                    _dtHRPVGClaimFileUploadDraft = new DtHRPVGClaimFileUploadRepositoryDraft(_repoContext, _readDbConnection, _writeDbConnection);
                }

                return _dtHRPVGClaimFileUploadDraft;
            }
        }


        public RepositoryWrapper(RepositoryContext repositoryContext, IApplicationReadDbConnection readDbConnection, IApplicationWriteDbConnection writeDbConnection)
        {
            _repoContext = repositoryContext;
            _readDbConnection = readDbConnection;
            _writeDbConnection = writeDbConnection;
        }

        public async Task SaveAsync()
        {
            await _repoContext.SaveChangesAsync();
        }
    }
}
