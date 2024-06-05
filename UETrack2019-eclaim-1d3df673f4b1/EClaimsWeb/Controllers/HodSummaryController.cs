using AutoMapper;
using ClosedXML.Excel;
using EClaimsEntities;
using EClaimsEntities.Models;
using EClaimsRepository.Contracts;
using EClaimsWeb.Helpers;
using EClaimsWeb.Models;
using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace EClaimsWeb.Controllers
{
    [Authorize(Roles = "User,Admin,Finance,HR")]
    [Authorize(Policy = "ShouldBeOnlyHODORSuperiorPolicy")]
    public class HodSummaryController : Controller
    {
        private ILoggerManager _logger;
        private IRepositoryWrapper _repository;
        private IMapper _mapper;
        private IConfiguration _configuration;
        private AlternateApproverHelper _alternateApproverHelper;
        private ISendMailServices _sendMailServices;
        private readonly RepositoryContext _context;
        public HodSummaryController(ILoggerManager logger, IRepositoryWrapper repository, IMapper mapper, RepositoryContext context, IConfiguration configuration, ISendMailServices sendMailServices)
        {
            _logger = logger;
            _repository = repository;
            _mapper = mapper;
            _context = context;
            _configuration = configuration;
            _sendMailServices = sendMailServices;
            _alternateApproverHelper = new AlternateApproverHelper(logger, repository, context);
        }
        public async Task<IActionResult> Index(string moduleName, int statusID, string fromDate, string toDate)
        {
            try
            {
                if (string.IsNullOrEmpty(fromDate) || string.IsNullOrEmpty(toDate))
                {
                    fromDate = DateTime.Now.AddDays(-30).ToString("dd/MM/yyyy");
                    toDate = DateTime.Now.ToString("dd/MM/yyyy");
                }

                List<clsModule> oclsModule = new List<clsModule>();
                //oclsModule.Add(new clsModule() { ModuleName = "Admin Settings", ModuleId = "Admin Settings" });
                oclsModule.Add(new clsModule() { ModuleName = "Mileage Claim", ModuleId = "MileageClaim" });
                oclsModule.Add(new clsModule() { ModuleName = "Expense Claim", ModuleId = "ExpenseClaim" });
                oclsModule.Add(new clsModule() { ModuleName = "TelephoneBill Claim", ModuleId = "TelephoneBillClaim" });
                oclsModule.Add(new clsModule() { ModuleName = "PV-Cheque Claim", ModuleId = "PV-ChequeClaim" });
                oclsModule.Add(new clsModule() { ModuleName = "PV-Giro Claim", ModuleId = "PV-GiroClaim" });


                List<SelectListItem> reports = (from t in oclsModule
                                                select new SelectListItem
                                                {
                                                    Text = t.ModuleName.ToString(),
                                                    Value = t.ModuleId.ToString(),
                                                }).OrderBy(p => p.Text).ToList();

                List<clsModule> oclsModuleStatus = new List<clsModule>();
                //oclsModuleStatus.Add(new clsModule() { ModuleName = "Admin Settings", ModuleId = "Admin Settings" });
                oclsModuleStatus.Add(new clsModule() { ModuleName = "Approved", ModuleId = "3" });
                oclsModuleStatus.Add(new clsModule() { ModuleName = "Awaiting Approval", ModuleId = "6" });
                oclsModuleStatus.Add(new clsModule() { ModuleName = "Awaiting HOD Approval", ModuleId = "7" });
                oclsModuleStatus.Add(new clsModule() { ModuleName = "Awaiting Signatory approval", ModuleId = "2" });
                oclsModuleStatus.Add(new clsModule() { ModuleName = "Awaiting Verification", ModuleId = "1" });
                oclsModuleStatus.Add(new clsModule() { ModuleName = "Exported to AccPac", ModuleId = "9" });
                oclsModuleStatus.Add(new clsModule() { ModuleName = "Exported to Bank", ModuleId = "10" });
                oclsModuleStatus.Add(new clsModule() { ModuleName = "Requested for Void", ModuleId = "-5" });
                oclsModuleStatus.Add(new clsModule() { ModuleName = "Request to Amend", ModuleId = "4" });
                oclsModuleStatus.Add(new clsModule() { ModuleName = "Voided", ModuleId = "5" });

                List<SelectListItem> status = (from t in oclsModuleStatus
                                               select new SelectListItem
                                               {
                                                   Text = t.ModuleName.ToString(),
                                                   Value = t.ModuleId.ToString(),
                                               }).OrderBy(p => p.Text).ToList();

                #region Alternate Approver Check code
                int? delegatedUserId = null;
                int loggedInUserId = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value);

                int? approverId = await _alternateApproverHelper.IsAlternateApprovalSetForUser(loggedInUserId);
                bool isAlternateApproverSet = false;
                if (approverId.HasValue)
                {
                    // Alternate approver is configured for the current user. So, do not show actions
                    isAlternateApproverSet = true;
                }
                else
                {
                    // Current user has not delegated his approvals. Check if the current user has any delegation 
                    delegatedUserId = await _alternateApproverHelper.IsUserHasAnyAlternateApprovalSet(loggedInUserId);
                }
                TempData["IsAlternateApproverSet"] = isAlternateApproverSet;
                #endregion

                List<CustomClaim> mstMileageClaimsWithDetails = await _repository.MstUser.GetAllHODSummaryClaimsAsync(loggedInUserId, moduleName, statusID, fromDate, toDate);

                if (mstMileageClaimsWithDetails != null && mstMileageClaimsWithDetails.Any())
                {
                    mstMileageClaimsWithDetails.ToList().ForEach(c => c.IsDelegated = false);
                }

                if (delegatedUserId != null && delegatedUserId.HasValue)
                {
                    List<CustomClaim> delegatedClaims = await _repository.MstUser.GetAllHODSummaryClaimsAsync(delegatedUserId, moduleName, statusID, fromDate, toDate);
                    if (delegatedClaims != null && delegatedClaims.Any())
                    {
                        delegatedClaims.ToList().ForEach(c => c.IsDelegated = true);
                        mstMileageClaimsWithDetails.AddRange(delegatedClaims);
                    }
                }

                List<CustomClaim> mileageClaimVMs = new List<CustomClaim>();
                foreach (var mc in mstMileageClaimsWithDetails)
                {
                    CustomClaim mileageClaimVM = new CustomClaim();
                    mileageClaimVM.CID = mc.CID;
                    mileageClaimVM.CNO = mc.CNO;
                    mileageClaimVM.Name = mc.Name;
                    mileageClaimVM.CreatedDate = mc.CreatedDate; //Convert.ToDateTime(mc.CreatedDate).ToString("dd/MM/yyyy", System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
                    mileageClaimVM.FacilityName = mc.FacilityName;
                    mileageClaimVM.Phone = mc.Phone;
                    mileageClaimVM.GrandTotal = mc.GrandTotal;
                    mileageClaimVM.TotalAmount = mc.TotalAmount;
                    mileageClaimVM.ApprovalStatus = mc.ApprovalStatus;
                    mileageClaimVM.VoucherNo = mc.VoucherNo;
                    mileageClaimVM.PayeeName = mc.PayeeName;
                    //TempData["ApprovedStatus"] = mc.ApprovalStatus;
                    if (mc.CNO.ToLower().Contains("ec") || mc.CNO.ToLower().Contains("tb") || mc.CNO.ToLower().Contains("mc"))
                    {
                        if (mc.UserApprovers != "")
                        {
                            mileageClaimVM.Approver = mc.UserApprovers.Split(',').First();
                            if ((mileageClaimVM.Approver == HttpContext.User.FindFirst("userid").Value || (delegatedUserId.HasValue && mileageClaimVM.Approver == delegatedUserId.Value.ToString())) &&
                                (mileageClaimVM.ApprovalStatus == 6))
                            {
                                mileageClaimVM.IsActionAllowed = true;
                            }
                        }
                        else if (mc.Verifier != "")
                        {
                            mileageClaimVM.Approver = mc.Verifier.Split(',').First();
                            if ((mileageClaimVM.Approver == HttpContext.User.FindFirst("userid").Value || (delegatedUserId.HasValue && mileageClaimVM.Approver == delegatedUserId.Value.ToString())) &&
                                (mileageClaimVM.ApprovalStatus == 1 || mileageClaimVM.ApprovalStatus == 2))
                            {
                                mileageClaimVM.IsActionAllowed = false;
                            }
                            //string VerifierIDs = string.Join(",", MileageverifierIDs.Skip(1));
                        }
                        else if (mc.HODApprover != "")
                        {
                            mileageClaimVM.Approver = mc.HODApprover.Split(',').First();
                            if ((mileageClaimVM.Approver == HttpContext.User.FindFirst("userid").Value || (delegatedUserId.HasValue && mileageClaimVM.Approver == delegatedUserId.Value.ToString())) &&
                                (mileageClaimVM.ApprovalStatus == 7))
                            {
                                mileageClaimVM.IsActionAllowed = true;
                            }
                        }
                        else if (mc.Approver != "")
                        {
                            mileageClaimVM.Approver = mc.Approver.Split(',').First();
                            if ((mileageClaimVM.Approver == HttpContext.User.FindFirst("userid").Value || (delegatedUserId.HasValue && mileageClaimVM.Approver == delegatedUserId.Value.ToString())) &&
                                (mileageClaimVM.ApprovalStatus == 1 || mileageClaimVM.ApprovalStatus == 2))
                            {
                                mileageClaimVM.IsActionAllowed = false;
                            }
                        }
                        else
                        {
                            mileageClaimVM.Approver = "";
                        }
                    }
                    else
                    {
                        if (mc.UserApprovers != "")
                        {
                            mileageClaimVM.Approver = mc.UserApprovers.Split(',').First();
                            if ((mileageClaimVM.Approver == HttpContext.User.FindFirst("userid").Value || (delegatedUserId.HasValue && mileageClaimVM.Approver == delegatedUserId.Value.ToString())) &&
                                (mileageClaimVM.ApprovalStatus == 6))
                            {
                                mileageClaimVM.IsActionAllowed = true;
                            }
                        }
                        else if (mc.HODApprover != "")
                        {
                            mileageClaimVM.Approver = mc.HODApprover.Split(',').First();
                            if ((mileageClaimVM.Approver == HttpContext.User.FindFirst("userid").Value || (delegatedUserId.HasValue && mileageClaimVM.Approver == delegatedUserId.Value.ToString())) &&
                                (mileageClaimVM.ApprovalStatus == 7))
                            {
                                mileageClaimVM.IsActionAllowed = true;
                            }
                        }
                        else if (mc.Verifier != "")
                        {
                            mileageClaimVM.Approver = mc.Verifier.Split(',').First();
                            if ((mileageClaimVM.Approver == HttpContext.User.FindFirst("userid").Value || (delegatedUserId.HasValue && mileageClaimVM.Approver == delegatedUserId.Value.ToString())) &&
                                (mileageClaimVM.ApprovalStatus == 1 || mileageClaimVM.ApprovalStatus == 2))
                            {
                                mileageClaimVM.IsActionAllowed = false;
                            }
                            //string VerifierIDs = string.Join(",", MileageverifierIDs.Skip(1));
                        }
                        else if (mc.Approver != "")
                        {
                            mileageClaimVM.Approver = mc.Approver.Split(',').First();
                            if ((mileageClaimVM.Approver == HttpContext.User.FindFirst("userid").Value || (delegatedUserId.HasValue && mileageClaimVM.Approver == delegatedUserId.Value.ToString())) &&
                                (mileageClaimVM.ApprovalStatus == 1 || mileageClaimVM.ApprovalStatus == 2))
                            {
                                mileageClaimVM.IsActionAllowed = false;
                            }
                        }
                        else
                        {
                            mileageClaimVM.Approver = "";
                        }
                    }

                    if (mileageClaimVM.Approver != "")
                    {
                        // Check if the approver is having any alternate approver configured
                        var alternateUser = await _alternateApproverHelper.IsAlternateApprovalSetForUser(Convert.ToInt32(mileageClaimVM.Approver));
                        if (alternateUser.HasValue)
                        {
                            var mstUserApprover = await _repository.MstUser.GetUserByIdAsync(alternateUser.Value);
                            mileageClaimVM.Approver = mstUserApprover.Name + " (AA)";
                        }
                        else
                        {
                            var mstUserApprover = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(mileageClaimVM.Approver));
                            mileageClaimVM.Approver = mstUserApprover.Name;
                        }
                    }

                    // Show actions based on alternate approver settings
                    // Override all the isActionAllowed code above. When alternate approval is set, then no need to show the action on any scenario
                    if (isAlternateApproverSet)
                    {
                        mileageClaimVM.IsActionAllowed = false;
                    }

                    mileageClaimVMs.Add(mileageClaimVM);
                }
                _logger.LogInfo($"Returned all Mileage Claims with details from database.");

                var mstMileageClaimVM = new HODClaimSearchViewModel
                {
                    //Screens = new SelectList(await screenQuery.Distinct().ToListAsync()),
                    customClaimVMs = mileageClaimVMs,
                    ReportTypes = new SelectList(reports, "Value", "Text"),
                    Statuses = new SelectList(status, "Value", "Text"),
                    FromDate = fromDate,
                    ToDate = toDate
                };
                //var mstExpenseCategoriesWithTypesResult = _mapper.Map<IEnumerable<MstExpenseCategory>>(mstExpenseCategoriesWithTypes);
                return View(mstMileageClaimVM);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Something went wrong inside GetAllMileageClaimWithDetailsAsync action: {ex.Message}");
                return View();
            }
        }

        public async Task<IActionResult> TBCDetails(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            long TBCID = Convert.ToInt64(id);

            if (User != null && User.Identity.IsAuthenticated)
            {
                var mstUserDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(HttpContext.User.FindFirst("userid").Value));

                var mstTBClaim = await _repository.MstTBClaim.GetTBClaimByIdAsync(id);

                if (mstTBClaim == null)
                {
                    return NotFound();
                }
                #region Alternate Approver Check code
                int? delegatedUserId = null;
                int loggedInUserId = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value);

                int? approverId = await _alternateApproverHelper.IsAlternateApprovalSetForUser(loggedInUserId);
                bool isAlternateApproverSet = false;
                if (approverId.HasValue)
                {
                    // Alternate approver is configured for the current user. So, do not show actions
                    isAlternateApproverSet = true;
                }
                else
                {
                    // Current user has not delegated his approvals. Check if the current user has any delegation 
                    delegatedUserId = await _alternateApproverHelper.IsUserHasAnyAlternateApprovalSet(loggedInUserId);
                }
                TempData["IsAlternateApproverSet"] = isAlternateApproverSet;
                #endregion

                var dtTBSummaries = await _repository.DtTBClaimSummary.GetDtTBClaimSummaryByIdAsync(id);
                var dtTBClaims = await _repository.DtTBClaim.GetDtTBClaimByIdAsync(id);
                TBClaimDetailVM tBClaimDetailVM = new TBClaimDetailVM();
                //List<DtMileageClaimVM> dtMileageClaimVMs = new List<DtMileageClaimVM>();
                tBClaimDetailVM.DtTBClaimVMs = new List<DtTBClaimVM>();
                // List<clsDtSupplierPO> oclsDtSupplierPO = new List<clsDtSupplierPO>();
                foreach (var item in dtTBClaims)
                {
                    DtTBClaimVM dtTBClaimVM = new DtTBClaimVM();

                    dtTBClaimVM.TBCItemID = item.TBCItemID;
                    dtTBClaimVM.TBCID = item.TBCID;
                    dtTBClaimVM.DateOfJourney = item.Date;

                    dtTBClaimVM.Description = item.Description;
                    dtTBClaimVM.Amount = item.Amount;
                    dtTBClaimVM.ExpenseCategory = item.MstExpenseCategory.Description;
                    dtTBClaimVM.AccountCode = item.AccountCode;
                    if (item.FacilityID != null)
                    {
                        var mstFacility = await _repository.MstFacility.GetFacilityByIdAsync(item.FacilityID);
                        dtTBClaimVM.Facility = mstFacility.FacilityName;
                    }
                    //if (item.FacilityID != null)
                    //{
                    //    dtMileageClaimVM.FacilityName = _repository.MstFacility.GetFacilityByIdAsync(item.FacilityID).GetAwaiter().GetResult().FacilityName.ToString();
                    //}
                    ////Need to change to not null
                    //if (item.FromFacilityID != 0)
                    //{
                    //    dtMileageClaimVM.FromFacilityName = _repository.MstFacility.GetFacilityByIdAsync(item.FromFacilityID).GetAwaiter().GetResult().FacilityName.ToString();
                    //}
                    ////Need to change to not null
                    //if (item.ToFacilityID != 0)
                    //{
                    //    dtMileageClaimVM.ToFacilityName = _repository.MstFacility.GetFacilityByIdAsync(item.ToFacilityID).GetAwaiter().GetResult().FacilityName.ToString();
                    //}

                    tBClaimDetailVM.DtTBClaimVMs.Add(dtTBClaimVM);
                }
                tBClaimDetailVM.DtTBClaimSummaries = dtTBSummaries;
                var GroupByQS = tBClaimDetailVM.DtTBClaimVMs.GroupBy(s => s.ExpenseCategoryID);
                //var GroupByQS = (from std in expenseClaimDetailVM.DtExpenseClaimVMs
                //                                                           group std by std.ExpenseCategoryID);

                tBClaimDetailVM.DtTBClaimVMSummary = new List<DtTBClaimVM>();

                foreach (var group in GroupByQS)
                {
                    DtTBClaimVM dtTBClaimVM = new DtTBClaimVM();
                    decimal amount = 0;
                    decimal gst = 0;
                    decimal sumamount = 0;
                    string ExpenseDesc = string.Empty;
                    string ExpenseCat = string.Empty;
                    string AccountCode = string.Empty;
                    int i = 0;
                    foreach (var dtExpense in group)
                    {
                        if (i == 0)
                            ExpenseDesc = dtExpense.Description;
                        i++;
                        amount = amount + dtExpense.Amount;
                        //gst = gst + dtExpense.Gst;
                        //sumamount = sumamount + dtExpense.AmountWithGST;
                        ExpenseCat = dtExpense.ExpenseCategory;
                        AccountCode = dtExpense.AccountCode;
                    }
                    gst = gst / group.Count();
                    dtTBClaimVM.Description = ExpenseDesc;
                    dtTBClaimVM.ExpenseCategory = ExpenseCat;
                    dtTBClaimVM.AccountCode = AccountCode;
                    dtTBClaimVM.Amount = amount;
                    dtTBClaimVM.Gst = gst;
                    //dtTBClaimVM.AmountWithGST = sumamount;
                    tBClaimDetailVM.DtTBClaimVMSummary.Add(dtTBClaimVM);
                }

                tBClaimDetailVM.TBClaimAudits = new List<TBClaimAuditVM>();

                var dtTBClaimAudits = await _repository.MstTBClaimAudit.GetMstTBClaimAuditByIdAsync(id);

                foreach (var item in dtTBClaimAudits)
                {
                    TBClaimAuditVM mstTBClaimAuditVM = new TBClaimAuditVM();
                    mstTBClaimAuditVM.Action = item.Action;
                    mstTBClaimAuditVM.Description = item.Description;
                    mstTBClaimAuditVM.AuditDateTickle = Helper.RelativeDate(item.AuditDate);
                    tBClaimDetailVM.TBClaimAudits.Add(mstTBClaimAuditVM);
                }

                tBClaimDetailVM.TBClaimFileUploads = new List<DtTBClaimFileUpload>();

                tBClaimDetailVM.TBClaimFileUploads = _repository.DtTBClaimFileUpload.GetDtTBClaimAuditByIdAsync(id).Result.ToList();

                TBClaimVM tBClaimVM = new TBClaimVM();
                tBClaimVM.VoucherNo = mstTBClaim.VoucherNo;
                tBClaimVM.Month = mstTBClaim.Month;
                tBClaimVM.Year = mstTBClaim.Year;
                tBClaimVM.GrandTotal = mstTBClaim.GrandTotal;
                tBClaimVM.Company = mstTBClaim.Company;
                tBClaimVM.Name = mstTBClaim.MstUser.Name;
                tBClaimVM.DepartmentName = mstTBClaim.MstDepartment.Department;
                tBClaimVM.FacilityName = mstTBClaim.MstFacility.FacilityName;
                tBClaimVM.CreatedDate = mstTBClaim.CreatedDate.ToString("d");
                tBClaimVM.Verifier = mstTBClaim.Verifier;
                tBClaimVM.Approver = mstTBClaim.Approver;
                ViewBag.TBCID = id;
                tBClaimVM.TBCNo = mstTBClaim.TBCNo;
                TempData["CreatedBy"] = mstTBClaim.CreatedBy;
                ViewBag.Approvalstatus = mstTBClaim.ApprovalStatus;

                TempData["ApprovedStatus"] = mstTBClaim.ApprovalStatus;
                TempData["FinalApproverID"] = mstTBClaim.FinalApprover;
                ViewBag.VoidReason = mstTBClaim.VoidReason == null ? "" : mstTBClaim.VoidReason;
                if (TempData["ApprovedStatus"].ToString() == "1" || TempData["ApprovedStatus"].ToString() == "2" || TempData["ApprovedStatus"].ToString() == "3" || TempData["ApprovedStatus"].ToString() == "-5" || TempData["ApprovedStatus"].ToString() == "6" || TempData["ApprovedStatus"].ToString() == "7" || TempData["ApprovedStatus"].ToString() == "9")
                {
                    ViewBag.ShowVoidBtn = 1;

                    if (mstUserDetails.IsActive && (mstUserDetails.IsHOD || (User.FindFirst("issuperior") is null ? false : User.FindFirst("issuperior").Value.ToString().ToLower() == "true")))
                    {
                        if (int.Parse(TempData["ApprovedStatus"].ToString()) < 3 || TempData["ApprovedStatus"].ToString() == "6" || TempData["ApprovedStatus"].ToString() == "7")
                        {
                            ViewBag.ShowVoidText = "Void";
                        }
                        else
                        {
                            ViewBag.ShowVoidText = "Request for Void";
                        }

                        if (TempData["ApprovedStatus"].ToString() == "-5" && TempData["FinalApproverID"].ToString() != HttpContext.User.FindFirst("userid").Value)
                        {
                            ViewBag.ShowVoidBtn = 0;
                        }
                    }
                    else
                    {
                        ViewBag.ShowVoidBtn = 0;
                    }
                }
                else
                {
                    ViewBag.ShowVoidBtn = 0;
                }

                //Verifier Process code
                TempData["VerifierIDs"] = "";
                TempData["ApproverIDs"] = "";
                TempData["QueryMCVerifierIDs"] = "";
                TempData["QueryMCApproverIDs"] = "";

                if (mstTBClaim.UserApprovers != "")
                {
                    string[] userApproverIDs = mstTBClaim.UserApprovers.Split(',');
                    TempData["QueryMCUserApproverIDs"] = string.Join(",", userApproverIDs);
                    foreach (string approverID in userApproverIDs)
                    {
                        if ((approverID != "" && approverID == HttpContext.User.FindFirst("userid").Value || (delegatedUserId.HasValue && approverID == delegatedUserId.Value.ToString())) && mstUserDetails.IsActive && (mstUserDetails.IsHOD || (User.FindFirst("issuperior") is null ? false : User.FindFirst("issuperior").Value.ToString().ToLower() == "true")))
                        {
                            TempData["ApprovedStatus"] = mstTBClaim.ApprovalStatus;
                            //Session["ApprovedStatus"] = oSupplierPO.Approvalstatus;
                            TempData["UserApproverIDs"] = string.Join(",", userApproverIDs.Skip(1));
                            tBClaimVM.IsActionAllowed = true;
                        }
                        else
                        {
                            TempData["ApprovedStatus"] = "";
                            //Session["ApprovedStatus"] = oSupplierPO.Approvalstatus;
                            TempData["UserApproverIDs"] = mstTBClaim.UserApprovers;
                        }
                        break;
                    }
                }
                else
                {
                    string[] userApproverIDs = mstTBClaim.UserApprovers.Split(',');
                    TempData["QueryMCUserApproverIDs"] = string.Join(",", userApproverIDs);
                }

                if (mstTBClaim.Verifier != "" && mstTBClaim.UserApprovers == "")
                {
                    string[] verifierIDs = mstTBClaim.Verifier.Split(',');
                    TempData["QueryMCVerifierIDs"] = string.Join(",", verifierIDs);
                    foreach (string verifierID in verifierIDs)
                    {
                        if (verifierID != "" && verifierID == HttpContext.User.FindFirst("userid").Value && mstUserDetails.IsActive && (mstUserDetails.IsHOD || (User.FindFirst("issuperior") is null ? false : User.FindFirst("issuperior").Value.ToString().ToLower() == "true")))
                        {
                            TempData["ApprovedStatus"] = mstTBClaim.ApprovalStatus;
                            //Session["ApprovedStatus"] = oSupplierPO.Approvalstatus;
                            TempData["VerifierIDs"] = string.Join(",", verifierIDs.Skip(1));
                        }
                        else
                        {
                            TempData["ApprovedStatus"] = "";
                            //Session["ApprovedStatus"] = oSupplierPO.Approvalstatus;
                            TempData["VerifierIDs"] = mstTBClaim.Verifier;
                        }
                        TempData["ApproverIDs"] = mstTBClaim.Approver;
                        break;
                    }
                }
                else
                {
                    //TempData["VerifierIDs"] = mstTBClaim.Verifier;
                    //TempData["ApproverIDs"] = mstTBClaim.Approver;
                    string[] verifierIDs = mstTBClaim.Verifier.Split(',');
                    TempData["QueryMCVerifierIDs"] = string.Join(",", verifierIDs);
                }

                //Approval Process code
                if (mstTBClaim.Approver != "" && mstTBClaim.Verifier == "" && mstTBClaim.UserApprovers == "" && mstTBClaim.HODApprover == "")
                {
                    string[] approverIDs = mstTBClaim.Approver.Split(',');
                    TempData["QueryMCApproverIDs"] = string.Join(",", approverIDs);
                    foreach (string approverID in approverIDs)
                    {
                        if (approverID != "" && approverID == HttpContext.User.FindFirst("userid").Value && mstUserDetails.IsActive && (mstUserDetails.IsHOD || (User.FindFirst("issuperior") is null ? false : User.FindFirst("issuperior").Value.ToString().ToLower() == "true")))
                        {
                            TempData["ApprovedStatus"] = mstTBClaim.ApprovalStatus;
                            //Session["ApprovedStatus"] = oSupplierPO.Approvalstatus;
                            TempData["ApproverIDs"] = string.Join(",", approverIDs.Skip(1));
                        }
                        else
                        {
                            TempData["ApprovedStatus"] = "";
                            //Session["ApprovedStatus"] = oSupplierPO.Approvalstatus;
                            TempData["ApproverIDs"] = mstTBClaim.Approver;
                        }
                        break;
                    }
                }
                else
                {
                    string[] approverIDs = mstTBClaim.Approver.Split(',');
                    TempData["QueryMCApproverIDs"] = string.Join(",", approverIDs);
                }

                if (mstTBClaim.HODApprover != "" && mstTBClaim.Verifier == "" && mstTBClaim.UserApprovers == "")
                {
                    string[] hodApproverIDs = mstTBClaim.HODApprover.Split(',');
                    TempData["QueryMCHODApproverIDs"] = string.Join(",", hodApproverIDs);
                    foreach (string approverID in hodApproverIDs)
                    {
                        if ((approverID != "" && approverID == HttpContext.User.FindFirst("userid").Value || (delegatedUserId.HasValue && approverID == delegatedUserId.Value.ToString())) && mstUserDetails.IsActive && (mstUserDetails.IsHOD || (User.FindFirst("issuperior") is null ? false : User.FindFirst("issuperior").Value.ToString().ToLower() == "true")))
                        {
                            TempData["ApprovedStatus"] = mstTBClaim.ApprovalStatus;
                            //Session["ApprovedStatus"] = oSupplierPO.Approvalstatus;
                            TempData["HODApproverIDs"] = string.Join(",", hodApproverIDs.Skip(1));
                            tBClaimVM.IsActionAllowed = true;
                        }
                        else
                        {
                            TempData["ApprovedStatus"] = "";
                            //Session["ApprovedStatus"] = oSupplierPO.Approvalstatus;
                            TempData["HODApproverIDs"] = mstTBClaim.HODApprover;
                        }
                        break;
                    }
                }
                else
                {
                    string[] hodApproverIDs = mstTBClaim.HODApprover.Split(',');
                    TempData["QueryMCHODApproverIDs"] = string.Join(",", hodApproverIDs);
                }

                // Show actions based on alternate approver settings
                // Override all the isActionAllowed code above. When alternate approval is set, then no need to show the action on any scenario
                if (isAlternateApproverSet)
                {
                    tBClaimVM.IsActionAllowed = false;
                }
                #region  -- GetQueries -- 


                int UserId = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value);
                ViewBag.userID = UserId;
                //var Userlist = objERPEntities.MstUsers.ToList().Where(i => i.UserID != UserId);
                var UserIds = new List<string>();
                //var Userlist1 = _context.users.ToList().Where(i => i.UserID != UserId);
                var Userlist = await _repository.MstUser.GetAllMCUsersForQueryAsync(UserId, UserIds);
                var Creater = TempData["CreatedBy"];
                var Verifiers = TempData["QueryMCVerifierIDs"];
                var HODApprovers = TempData["QueryMCHODApproverIDs"];
                var UserApprovers = TempData["QueryMCUserApproverIDs"];
                var Approvers = TempData["QueryMCApproverIDs"];

                string[] CreaterId = Creater.ToString().Split(',');
                string[] VerifiersId = Verifiers.ToString().Split(',');
                string[] HODApproversId = HODApprovers.ToString().Split(',');
                string[] UserApproversId = UserApprovers.ToString().Split(',');
                string[] ApproversId = Approvers.ToString().Split(',');

                UserIds.AddRange(CreaterId);
                UserIds.AddRange(UserApproversId);
                UserIds.AddRange(VerifiersId);
                UserIds.AddRange(HODApproversId);
                UserIds.AddRange(ApproversId);
                // Audit users
                //var AuditIDs = objERPEntities.MstSupplierPOAudits.ToList().Where(p => p.SPOID == SPOID).Select(p => p.AuditBy.ToString()).Distinct();
                //var AuditIDs1 = _context.MstMileageClaimAudit.ToList().Where(m => m.MCID == MCID).Select(m => m.AuditBy.ToString()).Distinct();
                //var AuditIDs = _repository.MstMileageClaimAudit.GetMstMileageClaimAuditByIdAsync(MCID).GetAwaiter().GetResult().Select(m => m.AuditBy.ToString()).Distinct();
                var mstTBClaimAudits = await _repository.MstTBClaimAudit.GetMstTBClaimAuditByIdAsync(TBCID);
                var AuditIDs = mstTBClaimAudits.Select(m => m.AuditBy.ToString()).Distinct();
                foreach (var item in AuditIDs)
                {
                    string d = item;
                    UserIds.Add(d);
                }
                // Audit users
                //var spoUsers =  objERPEntities.MstUsers.ToList().Where(i => i.UserID != UserId && UserIds.Contains(i.UserID.ToString()));
                // var mcUsers1 = _context.users.ToList().Where(i => i.UserID != UserId && UserIds.Contains(i.UserID.ToString()));

                var mcUsers = await _repository.MstUser.GetAllMCUsersForQueryAsync(UserId, UserIds);
                var users = (from u in Userlist
                             join ut in mcUsers
                             on u.UserID equals ut.UserID
                             select new SelectListItem
                             {
                                 Text = u.Name.ToString(),
                                 Value = u.UserID.ToString()
                             }).OrderBy(p => p.Text).Distinct();
                ViewBag.queryusers = users;
                if (UserIds.Contains(UserId.ToString()))
                {
                    ViewBag.Access = 1;
                }
                else
                {
                    ViewBag.Access = 0;
                }

                #endregion getQueries




                tBClaimDetailVM.TBClaimVM = tBClaimVM;
                //mileageClaimDetailVM.DtMileageClaimVMs = dtMileageClaimVMs;



                return View(tBClaimDetailVM);
            }
            else
            {
                return Redirect("~/Login/Login");
            }
        }

        public async Task<IActionResult> PVGCDetails(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            long PVGCID = Convert.ToInt64(id);

            if (User != null && User.Identity.IsAuthenticated)
            {
                var mstUserDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(HttpContext.User.FindFirst("userid").Value));

                var mstPVGClaim = await _repository.MstPVGClaim.GetPVGClaimByIdAsync(id);

                if (mstPVGClaim == null)
                {
                    return NotFound();
                }

                #region Alternate Approver Check code
                int? delegatedUserId = null;
                int loggedInUserId = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value);

                int? approverId = await _alternateApproverHelper.IsAlternateApprovalSetForUser(loggedInUserId);
                bool isAlternateApproverSet = false;
                if (approverId.HasValue)
                {
                    // Alternate approver is configured for the current user. So, do not show actions
                    isAlternateApproverSet = true;
                }
                else
                {
                    // Current user has not delegated his approvals. Check if the current user has any delegation 
                    delegatedUserId = await _alternateApproverHelper.IsUserHasAnyAlternateApprovalSet(loggedInUserId);
                }
                TempData["IsAlternateApproverSet"] = isAlternateApproverSet;
                #endregion

                var dtPVGSummaries = await _repository.DtPVGClaimSummary.GetDtPVGClaimSummaryByIdAsync(id);
                var dtPVGClaims = await _repository.DtPVGClaim.GetDtPVGClaimByIdAsync(id);
                PVGClaimDetailVM pVGClaimDetailVM = new PVGClaimDetailVM();
                //List<DtMileageClaimVM> dtMileageClaimVMs = new List<DtMileageClaimVM>();
                pVGClaimDetailVM.DtPVGClaimVMs = new List<DtPVGClaimVM>();
                // List<clsDtSupplierPO> oclsDtSupplierPO = new List<clsDtSupplierPO>();
                foreach (var item in dtPVGClaims)
                {
                    DtPVGClaimVM dtPVGClaimVM = new DtPVGClaimVM();

                    dtPVGClaimVM.PVGCItemID = item.PVGCItemID;
                    dtPVGClaimVM.PVGCID = item.PVGCID;
                    dtPVGClaimVM.Date = item.Date;

                    dtPVGClaimVM.ChequeNo = item.ChequeNo;
                    dtPVGClaimVM.Particulars = item.Particulars;
                    dtPVGClaimVM.Payee = item.Payee;
                    dtPVGClaimVM.InvoiceNo = item.InvoiceNo;
                    dtPVGClaimVM.Amount = item.Amount;
                    dtPVGClaimVM.GST = item.GST;
                    dtPVGClaimVM.AmountWithGST = item.Amount + item.GST;
                    dtPVGClaimVM.ExpenseCategory = item.MstExpenseCategory.Description;
                    dtPVGClaimVM.AccountCode = item.AccountCode;
                    dtPVGClaimVM.ExpenseCategoryID = item.ExpenseCategoryID;
                    dtPVGClaimVM.Bank = item.Bank;
                    dtPVGClaimVM.BankCode = item.BankCode;
                    dtPVGClaimVM.BranchCode = item.BranchCode;
                    dtPVGClaimVM.BankAccount = item.BankAccount;
                    dtPVGClaimVM.Mobile = item.Mobile;
                    if (item.FacilityID != null)
                    {
                        var mstFacility = await _repository.MstFacility.GetFacilityByIdAsync(item.FacilityID);
                        dtPVGClaimVM.Facility = mstFacility.FacilityName;
                    }
                    //if (item.FacilityID != null)
                    //{
                    //    dtMileageClaimVM.FacilityName = _repository.MstFacility.GetFacilityByIdAsync(item.FacilityID).GetAwaiter().GetResult().FacilityName.ToString();
                    //}
                    ////Need to change to not null
                    //if (item.FromFacilityID != 0)
                    //{
                    //    dtMileageClaimVM.FromFacilityName = _repository.MstFacility.GetFacilityByIdAsync(item.FromFacilityID).GetAwaiter().GetResult().FacilityName.ToString();
                    //}
                    ////Need to change to not null
                    //if (item.ToFacilityID != 0)
                    //{
                    //    dtMileageClaimVM.ToFacilityName = _repository.MstFacility.GetFacilityByIdAsync(item.ToFacilityID).GetAwaiter().GetResult().FacilityName.ToString();
                    //}

                    pVGClaimDetailVM.DtPVGClaimVMs.Add(dtPVGClaimVM);
                }

                pVGClaimDetailVM.DtPVGClaimSummaries = dtPVGSummaries;
                var GroupByQS = pVGClaimDetailVM.DtPVGClaimVMs.GroupBy(s => s.ExpenseCategoryID);
                //var GroupByQS = (from std in expenseClaimDetailVM.DtExpenseClaimVMs
                //                                                           group std by std.ExpenseCategoryID);

                //pVGClaimDetailVM.DtPVGClaimVMSummary = new List<DtPVGClaimVM>();

                //foreach (var group in GroupByQS)
                //{
                //    DtPVGClaimVM dtPVGClaimVM = new DtPVGClaimVM();
                //    decimal amount = 0;
                //    decimal gst = 0;
                //    decimal sumamount = 0;
                //    string PVGDesc = string.Empty;
                //    string AccountCode = string.Empty;
                //    foreach (var dtPVG in group)
                //    {
                //        amount = amount + dtPVG.Amount;
                //        gst = gst + dtPVG.GST;
                //        sumamount = sumamount + dtPVG.AmountWithGST;
                //        PVGDesc = dtPVG.ExpenseCategory;
                //        AccountCode = dtPVG.AccountCode;
                //    }
                //    gst = gst / group.Count();
                //    dtPVGClaimVM.ExpenseCategory = PVGDesc;
                //    dtPVGClaimVM.AccountCode = AccountCode;
                //    dtPVGClaimVM.Amount = amount;
                //    dtPVGClaimVM.GST = gst;
                //    dtPVGClaimVM.AmountWithGST = sumamount;
                //    pVGClaimDetailVM.DtPVGClaimVMSummary.Add(dtPVGClaimVM);
                //}

                pVGClaimDetailVM.PVGClaimAudits = new List<PVGClaimAuditVM>();

                var dtPVGClaimAudits = await _repository.MstPVGClaimAudit.GetMstPVGClaimAuditByIdAsync(id);

                foreach (var item in dtPVGClaimAudits)
                {
                    PVGClaimAuditVM mstPVGClaimAuditVM = new PVGClaimAuditVM();
                    mstPVGClaimAuditVM.Action = item.Action;
                    mstPVGClaimAuditVM.Description = item.Description;
                    mstPVGClaimAuditVM.AuditDateTickle = Helper.RelativeDate(item.AuditDate);
                    pVGClaimDetailVM.PVGClaimAudits.Add(mstPVGClaimAuditVM);
                }

                pVGClaimDetailVM.PVGClaimFileUploads = new List<DtPVGClaimFileUpload>();

                pVGClaimDetailVM.PVGClaimFileUploads = _repository.DtPVGClaimFileUpload.GetDtPVGClaimAuditByIdAsync(id).Result.ToList();

                PVGClaimVM pVGClaimVM = new PVGClaimVM();
                //pVGClaimVM.ClaimType = mstPVGClaim.ClaimType;
                pVGClaimVM.VoucherNo = mstPVGClaim.VoucherNo;
                pVGClaimVM.GrandTotal = mstPVGClaim.GrandTotal;
                pVGClaimVM.TotalAmount = mstPVGClaim.TotalAmount;
                pVGClaimVM.GrandGST = pVGClaimVM.TotalAmount - pVGClaimVM.GrandTotal;
                pVGClaimVM.Company = mstPVGClaim.Company;
                pVGClaimVM.Name = mstPVGClaim.MstUser.Name;
                pVGClaimVM.DepartmentName = mstPVGClaim.MstDepartment.Department;
                pVGClaimVM.FacilityName = mstPVGClaim.MstFacility.FacilityName;
                pVGClaimVM.CreatedDate = Convert.ToDateTime(mstPVGClaim.CreatedDate).ToString("dd/MM/yyyy", System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
                pVGClaimVM.Verifier = mstPVGClaim.Verifier;
                pVGClaimVM.Approver = mstPVGClaim.Approver;
                pVGClaimVM.PVGCNo = mstPVGClaim.PVGCNo;
                pVGClaimVM.PaymentMode = mstPVGClaim.PaymentMode;
                ViewBag.PVGCID = id;
                TempData["CreatedBy"] = mstPVGClaim.CreatedBy;
                ViewBag.Approvalstatus = mstPVGClaim.ApprovalStatus;


                TempData["ApprovedStatus"] = mstPVGClaim.ApprovalStatus;
                TempData["FinalApproverID"] = mstPVGClaim.FinalApprover;
                ViewBag.VoidReason = mstPVGClaim.VoidReason == null ? "" : mstPVGClaim.VoidReason;

                if (TempData["ApprovedStatus"].ToString() == "1" || TempData["ApprovedStatus"].ToString() == "2" || TempData["ApprovedStatus"].ToString() == "3" || TempData["ApprovedStatus"].ToString() == "-5" || TempData["ApprovedStatus"].ToString() == "6" || TempData["ApprovedStatus"].ToString() == "7" || TempData["ApprovedStatus"].ToString() == "9")
                {
                    ViewBag.ShowVoidBtn = 1;

                    if (mstUserDetails.IsActive && (mstUserDetails.IsHOD || (User.FindFirst("issuperior") is null ? false : User.FindFirst("issuperior").Value.ToString().ToLower() == "true")))
                    {
                        if (int.Parse(TempData["ApprovedStatus"].ToString()) < 3 || TempData["ApprovedStatus"].ToString() == "6" || TempData["ApprovedStatus"].ToString() == "7")
                        {
                            ViewBag.ShowVoidText = "Void";
                        }
                        else
                        {
                            ViewBag.ShowVoidText = "Request for Void";
                        }

                        if (TempData["ApprovedStatus"].ToString() == "-5" && TempData["FinalApproverID"].ToString() != HttpContext.User.FindFirst("userid").Value)
                        {
                            ViewBag.ShowVoidBtn = 0;
                        }
                    }
                    else
                    {
                        ViewBag.ShowVoidBtn = 0;
                    }
                }
                else
                {
                    ViewBag.ShowVoidBtn = 0;
                }

                //Verifier Process code
                TempData["VerifierIDs"] = "";
                TempData["ApproverIDs"] = "";
                TempData["QueryMCVerifierIDs"] = "";
                TempData["QueryMCApproverIDs"] = "";

                if (mstPVGClaim.UserApprovers != "")
                {
                    string[] userApproverIDs = mstPVGClaim.UserApprovers.Split(',');
                    TempData["QueryMCUserApproverIDs"] = string.Join(",", userApproverIDs);
                    foreach (string approverID in userApproverIDs)
                    {
                        if ((approverID != "" && approverID == HttpContext.User.FindFirst("userid").Value || (delegatedUserId.HasValue && approverID == delegatedUserId.Value.ToString())) && mstUserDetails.IsActive && (mstUserDetails.IsHOD || (User.FindFirst("issuperior") is null ? false : User.FindFirst("issuperior").Value.ToString().ToLower() == "true")))
                        {
                            TempData["ApprovedStatus"] = mstPVGClaim.ApprovalStatus;
                            //Session["ApprovedStatus"] = oSupplierPO.Approvalstatus;
                            TempData["UserApproverIDs"] = string.Join(",", userApproverIDs.Skip(1));
                            pVGClaimVM.IsActionAllowed = true;
                        }
                        else
                        {
                            TempData["ApprovedStatus"] = "";
                            //Session["ApprovedStatus"] = oSupplierPO.Approvalstatus;
                            TempData["UserApproverIDs"] = mstPVGClaim.UserApprovers;
                        }
                        break;
                    }
                }
                else
                {
                    string[] userApproverIDs = mstPVGClaim.UserApprovers.Split(',');
                    TempData["QueryMCUserApproverIDs"] = string.Join(",", userApproverIDs);
                }

                if (mstPVGClaim.Verifier != "" && mstPVGClaim.UserApprovers == "")
                {
                    string[] verifierIDs = mstPVGClaim.Verifier.Split(',');
                    TempData["QueryMCVerifierIDs"] = string.Join(",", verifierIDs);
                    foreach (string verifierID in verifierIDs)
                    {
                        if (verifierID != "" && verifierID == HttpContext.User.FindFirst("userid").Value && mstUserDetails.IsActive && (mstUserDetails.IsHOD || (User.FindFirst("issuperior") is null ? false : User.FindFirst("issuperior").Value.ToString().ToLower() == "true")))
                        {
                            TempData["ApprovedStatus"] = mstPVGClaim.ApprovalStatus;
                            //Session["ApprovedStatus"] = oSupplierPO.Approvalstatus;
                            TempData["VerifierIDs"] = string.Join(",", verifierIDs.Skip(1));
                        }
                        else
                        {
                            TempData["ApprovedStatus"] = "";
                            //Session["ApprovedStatus"] = oSupplierPO.Approvalstatus;
                            TempData["VerifierIDs"] = mstPVGClaim.Verifier;
                        }
                        TempData["ApproverIDs"] = mstPVGClaim.Approver;
                        break;
                    }
                }
                else
                {
                    //TempData["VerifierIDs"] = mstPVGClaim.Verifier;
                    //TempData["ApproverIDs"] = mstPVGClaim.Approver;
                    string[] verifierIDs = mstPVGClaim.Verifier.Split(',');
                    TempData["QueryMCVerifierIDs"] = string.Join(",", verifierIDs);
                }

                //Approval Process code
                if (mstPVGClaim.Approver != "" && mstPVGClaim.Verifier == "" && mstPVGClaim.UserApprovers == "" && mstPVGClaim.HODApprover == "")
                {
                    string[] approverIDs = mstPVGClaim.Approver.Split(',');
                    TempData["QueryMCApproverIDs"] = string.Join(",", approverIDs);
                    foreach (string approverID in approverIDs)
                    {
                        if (approverID != "" && approverID == HttpContext.User.FindFirst("userid").Value && mstUserDetails.IsActive && (mstUserDetails.IsHOD || (User.FindFirst("issuperior") is null ? false : User.FindFirst("issuperior").Value.ToString().ToLower() == "true")))
                        {
                            TempData["ApprovedStatus"] = mstPVGClaim.ApprovalStatus;
                            //Session["ApprovedStatus"] = oSupplierPO.Approvalstatus;
                            TempData["ApproverIDs"] = string.Join(",", approverIDs.Skip(1));
                        }
                        else
                        {
                            TempData["ApprovedStatus"] = "";
                            //Session["ApprovedStatus"] = oSupplierPO.Approvalstatus;
                            TempData["ApproverIDs"] = mstPVGClaim.Approver;
                        }
                        break;
                    }
                }
                else
                {
                    string[] approverIDs = mstPVGClaim.Approver.Split(',');
                    TempData["QueryMCApproverIDs"] = string.Join(",", approverIDs);
                }

                if (mstPVGClaim.HODApprover != "" && mstPVGClaim.UserApprovers == "")
                {
                    string[] hodApproverIDs = mstPVGClaim.HODApprover.Split(',');
                    TempData["QueryMCHODApproverIDs"] = string.Join(",", hodApproverIDs);
                    foreach (string approverID in hodApproverIDs)
                    {
                        if ((approverID != "" && approverID == HttpContext.User.FindFirst("userid").Value || (delegatedUserId.HasValue && approverID == delegatedUserId.Value.ToString())) && mstUserDetails.IsActive && (mstUserDetails.IsHOD || (User.FindFirst("issuperior") is null ? false : User.FindFirst("issuperior").Value.ToString().ToLower() == "true")))
                        {
                            TempData["ApprovedStatus"] = mstPVGClaim.ApprovalStatus;
                            //Session["ApprovedStatus"] = oSupplierPO.Approvalstatus;
                            TempData["HODApproverIDs"] = string.Join(",", hodApproverIDs.Skip(1));
                            pVGClaimVM.IsActionAllowed = true;
                        }
                        else
                        {
                            TempData["ApprovedStatus"] = "";
                            //Session["ApprovedStatus"] = oSupplierPO.Approvalstatus;
                            TempData["HODApproverIDs"] = mstPVGClaim.HODApprover;
                        }
                        break;
                    }
                }
                else
                {
                    string[] hodApproverIDs = mstPVGClaim.HODApprover.Split(',');
                    TempData["QueryMCHODApproverIDs"] = string.Join(",", hodApproverIDs);
                }

                // Show actions based on alternate approver settings
                // Override all the isActionAllowed code above. When alternate approval is set, then no need to show the action on any scenario
                if (isAlternateApproverSet)
                {
                    pVGClaimVM.IsActionAllowed = false;
                }

                #region  -- GetQueries -- 


                int UserId = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value);
                ViewBag.userID = UserId;
                //var Userlist = objERPEntities.MstUsers.ToList().Where(i => i.UserID != UserId);
                var UserIds = new List<string>();
                //var Userlist1 = _context.users.ToList().Where(i => i.UserID != UserId);
                var Userlist = await _repository.MstUser.GetAllMCUsersForQueryAsync(UserId, UserIds);
                var Creater = TempData["CreatedBy"];
                var Verifiers = TempData["QueryMCVerifierIDs"];
                var Approvers = TempData["QueryMCApproverIDs"];
                var HODApprovers = TempData["QueryMCHODApproverIDs"];
                var UserApprovers = TempData["QueryMCUserApproverIDs"];

                string[] CreaterId = Creater.ToString().Split(',');
                string[] VerifiersId = Verifiers.ToString().Split(',');
                string[] ApproversId = Approvers.ToString().Split(',');
                string[] HODApproversId = HODApprovers.ToString().Split(',');
                string[] UserApproversId = UserApprovers.ToString().Split(',');

                UserIds.AddRange(CreaterId);
                UserIds.AddRange(UserApproversId);
                UserIds.AddRange(VerifiersId);
                UserIds.AddRange(HODApproversId);
                UserIds.AddRange(ApproversId);
                // Audit users
                //var AuditIDs = objERPEntities.MstSupplierPOAudits.ToList().Where(p => p.SPOID == SPOID).Select(p => p.AuditBy.ToString()).Distinct();
                //var AuditIDs1 = _context.MstMileageClaimAudit.ToList().Where(m => m.MCID == MCID).Select(m => m.AuditBy.ToString()).Distinct();
                //var AuditIDs = _repository.MstMileageClaimAudit.GetMstMileageClaimAuditByIdAsync(MCID).GetAwaiter().GetResult().Select(m => m.AuditBy.ToString()).Distinct();
                var mstPVGClaimAudits = await _repository.MstPVGClaimAudit.GetMstPVGClaimAuditByIdAsync(PVGCID);
                var AuditIDs = mstPVGClaimAudits.Select(m => m.AuditBy.ToString()).Distinct();
                foreach (var item in AuditIDs)
                {
                    string d = item;
                    UserIds.Add(d);
                }
                // Audit users
                //var spoUsers =  objERPEntities.MstUsers.ToList().Where(i => i.UserID != UserId && UserIds.Contains(i.UserID.ToString()));
                // var mcUsers1 = _context.users.ToList().Where(i => i.UserID != UserId && UserIds.Contains(i.UserID.ToString()));

                var mcUsers = await _repository.MstUser.GetAllMCUsersForQueryAsync(UserId, UserIds);
                var users = (from u in Userlist
                             join ut in mcUsers
                             on u.UserID equals ut.UserID
                             select new SelectListItem
                             {
                                 Text = u.Name.ToString(),
                                 Value = u.UserID.ToString()
                             }).OrderBy(p => p.Text).Distinct();
                ViewBag.queryusers = users;
                if (UserIds.Contains(UserId.ToString()))
                {
                    ViewBag.Access = 1;
                }
                else
                {
                    ViewBag.Access = 0;
                }

                #endregion getQueries


                pVGClaimDetailVM.PVGClaimVM = pVGClaimVM;
                //mileageClaimDetailVM.DtMileageClaimVMs = dtMileageClaimVMs;



                return View(pVGClaimDetailVM);
            }
            else
            {
                return Redirect("~/Login/Login");
            }
        }

        public async Task<IActionResult> PVCCDetails(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            long PVCCID = Convert.ToInt64(id);

            if (User != null && User.Identity.IsAuthenticated)
            {
                var mstUserDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(HttpContext.User.FindFirst("userid").Value));

                var mstPVCClaim = await _repository.MstPVCClaim.GetPVCClaimByIdAsync(id);

                if (mstPVCClaim == null)
                {
                    return NotFound();
                }

                #region Alternate Approver Check code
                int? delegatedUserId = null;
                int loggedInUserId = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value);

                int? approverId = await _alternateApproverHelper.IsAlternateApprovalSetForUser(loggedInUserId);
                bool isAlternateApproverSet = false;
                if (approverId.HasValue)
                {
                    // Alternate approver is configured for the current user. So, do not show actions
                    isAlternateApproverSet = true;
                }
                else
                {
                    // Current user has not delegated his approvals. Check if the current user has any delegation 
                    delegatedUserId = await _alternateApproverHelper.IsUserHasAnyAlternateApprovalSet(loggedInUserId);
                }
                TempData["IsAlternateApproverSet"] = isAlternateApproverSet;
                #endregion

                var dtPVCSummaries = await _repository.DtPVCClaimSummary.GetDtPVCClaimSummaryByIdAsync(id);
                var dtPVCClaims = await _repository.DtPVCClaim.GetDtPVCClaimByIdAsync(id);

                PVCClaimDetailVM pVCClaimDetailVM = new PVCClaimDetailVM();
                //List<DtMileageClaimVM> dtMileageClaimVMs = new List<DtMileageClaimVM>();
                pVCClaimDetailVM.DtPVCClaimVMs = new List<DtPVCClaimVM>();
                // List<clsDtSupplierPO> oclsDtSupplierPO = new List<clsDtSupplierPO>();
                foreach (var item in dtPVCClaims)
                {
                    DtPVCClaimVM dtPVCClaimVM = new DtPVCClaimVM();

                    dtPVCClaimVM.PVCCItemID = item.PVCCItemID;
                    dtPVCClaimVM.PVCCID = item.PVCCID;
                    dtPVCClaimVM.Date = item.Date;

                    dtPVCClaimVM.ChequeNo = item.ChequeNo;
                    dtPVCClaimVM.Particulars = item.Particulars;
                    dtPVCClaimVM.Payee = item.Payee;
                    dtPVCClaimVM.InvoiceNo = item.InvoiceNo;
                    dtPVCClaimVM.Amount = item.Amount;
                    dtPVCClaimVM.GST = item.GST;
                    dtPVCClaimVM.AmountWithGST = item.Amount + item.GST;
                    dtPVCClaimVM.ExpenseCategory = item.MstExpenseCategory.Description;
                    dtPVCClaimVM.AccountCode = item.AccountCode;
                    dtPVCClaimVM.ExpenseCategoryID = item.ExpenseCategoryID;
                    if (item.FacilityID != null)
                    {
                        var mstFacility = await _repository.MstFacility.GetFacilityByIdAsync(item.FacilityID);
                        dtPVCClaimVM.Facility = mstFacility.FacilityName;
                    }
                    //if (item.FacilityID != null)
                    //{
                    //    dtMileageClaimVM.FacilityName = _repository.MstFacility.GetFacilityByIdAsync(item.FacilityID).GetAwaiter().GetResult().FacilityName.ToString();
                    //}
                    ////Need to change to not null
                    //if (item.FromFacilityID != 0)
                    //{
                    //    dtMileageClaimVM.FromFacilityName = _repository.MstFacility.GetFacilityByIdAsync(item.FromFacilityID).GetAwaiter().GetResult().FacilityName.ToString();
                    //}
                    ////Need to change to not null
                    //if (item.ToFacilityID != 0)
                    //{
                    //    dtMileageClaimVM.ToFacilityName = _repository.MstFacility.GetFacilityByIdAsync(item.ToFacilityID).GetAwaiter().GetResult().FacilityName.ToString();
                    //}

                    pVCClaimDetailVM.DtPVCClaimVMs.Add(dtPVCClaimVM);
                }
                pVCClaimDetailVM.DtPVCClaimSummaries = dtPVCSummaries;
                var GroupByQS = pVCClaimDetailVM.DtPVCClaimVMs.GroupBy(s => s.ExpenseCategoryID);
                //var GroupByQS = (from std in expenseClaimDetailVM.DtExpenseClaimVMs
                //                                                           group std by std.ExpenseCategoryID);



                pVCClaimDetailVM.PVCClaimAudits = new List<PVCClaimAuditVM>();

                var dtPVCClaimAudits = await _repository.MstPVCClaimAudit.GetMstPVCClaimAuditByIdAsync(id);

                foreach (var item in dtPVCClaimAudits)
                {
                    PVCClaimAuditVM mstPVCClaimAuditVM = new PVCClaimAuditVM();
                    mstPVCClaimAuditVM.Action = item.Action;
                    mstPVCClaimAuditVM.Description = item.Description;
                    mstPVCClaimAuditVM.AuditDateTickle = Helper.RelativeDate(item.AuditDate);
                    pVCClaimDetailVM.PVCClaimAudits.Add(mstPVCClaimAuditVM);
                }

                pVCClaimDetailVM.PVCClaimFileUploads = new List<DtPVCClaimFileUpload>();

                pVCClaimDetailVM.PVCClaimFileUploads = _repository.DtPVCClaimFileUpload.GetDtPVCClaimAuditByIdAsync(id).Result.ToList();

                PVCClaimVM pVCClaimVM = new PVCClaimVM();
                //pVCClaimVM.ClaimType = mstPVCClaim.ClaimType;
                pVCClaimVM.VoucherNo = mstPVCClaim.VoucherNo;
                pVCClaimVM.GrandTotal = mstPVCClaim.GrandTotal;
                pVCClaimVM.TotalAmount = mstPVCClaim.TotalAmount;
                pVCClaimVM.GrandGST = pVCClaimVM.TotalAmount - pVCClaimVM.GrandTotal;
                pVCClaimVM.Company = mstPVCClaim.Company;
                pVCClaimVM.Name = mstPVCClaim.MstUser.Name;
                pVCClaimVM.DepartmentName = mstPVCClaim.MstDepartment.Department;
                pVCClaimVM.FacilityName = mstPVCClaim.MstFacility.FacilityName;
                pVCClaimVM.CreatedDate = Convert.ToDateTime(mstPVCClaim.CreatedDate).ToString("dd/MM/yyyy", System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
                pVCClaimVM.Verifier = mstPVCClaim.Verifier;
                pVCClaimVM.Approver = mstPVCClaim.Approver;
                pVCClaimVM.PVCCNo = mstPVCClaim.PVCCNo;
                ViewBag.PVCCID = id;
                TempData["CreatedBy"] = mstPVCClaim.CreatedBy;
                ViewBag.Approvalstatus = mstPVCClaim.ApprovalStatus;


                TempData["ApprovedStatus"] = mstPVCClaim.ApprovalStatus;
                TempData["FinalApproverID"] = mstPVCClaim.FinalApprover;
                ViewBag.VoidReason = mstPVCClaim.VoidReason == null ? "" : mstPVCClaim.VoidReason;

                if (TempData["ApprovedStatus"].ToString() == "1" || TempData["ApprovedStatus"].ToString() == "2" || TempData["ApprovedStatus"].ToString() == "3" || TempData["ApprovedStatus"].ToString() == "-5" || TempData["ApprovedStatus"].ToString() == "6" || TempData["ApprovedStatus"].ToString() == "7" || TempData["ApprovedStatus"].ToString() == "9")
                {
                    ViewBag.ShowVoidBtn = 1;

                    if (mstUserDetails.IsActive && (mstUserDetails.IsHOD || (User.FindFirst("issuperior") is null ? false : User.FindFirst("issuperior").Value.ToString().ToLower() == "true")))
                    {
                        if (int.Parse(TempData["ApprovedStatus"].ToString()) < 3 || TempData["ApprovedStatus"].ToString() == "6" || TempData["ApprovedStatus"].ToString() == "7")
                        {
                            ViewBag.ShowVoidText = "Void";
                        }
                        else
                        {
                            ViewBag.ShowVoidText = "Request for Void";
                        }

                        if (TempData["ApprovedStatus"].ToString() == "-5" && TempData["FinalApproverID"].ToString() != HttpContext.User.FindFirst("userid").Value)
                        {
                            ViewBag.ShowVoidBtn = 0;
                        }
                    }
                    else
                    {
                        ViewBag.ShowVoidBtn = 0;
                    }
                }
                else
                {
                    ViewBag.ShowVoidBtn = 0;
                }

                //Verifier Process code
                TempData["VerifierIDs"] = "";
                TempData["ApproverIDs"] = "";
                TempData["QueryMCVerifierIDs"] = "";
                TempData["QueryMCApproverIDs"] = "";
                if (mstPVCClaim.UserApprovers != "")
                {
                    string[] userApproverIDs = mstPVCClaim.UserApprovers.Split(',');
                    TempData["QueryMCUserApproverIDs"] = string.Join(",", userApproverIDs);
                    foreach (string approverID in userApproverIDs)
                    {
                        if ((approverID != "" && approverID == HttpContext.User.FindFirst("userid").Value || (delegatedUserId.HasValue && approverID == delegatedUserId.Value.ToString())) && mstUserDetails.IsActive && (mstUserDetails.IsHOD || (User.FindFirst("issuperior") is null ? false : User.FindFirst("issuperior").Value.ToString().ToLower() == "true")))
                        {
                            TempData["ApprovedStatus"] = mstPVCClaim.ApprovalStatus;
                            //Session["ApprovedStatus"] = oSupplierPO.Approvalstatus;
                            TempData["UserApproverIDs"] = string.Join(",", userApproverIDs.Skip(1));
                            pVCClaimVM.IsActionAllowed = true;
                        }
                        else
                        {
                            TempData["ApprovedStatus"] = "";
                            //Session["ApprovedStatus"] = oSupplierPO.Approvalstatus;
                            TempData["UserApproverIDs"] = mstPVCClaim.UserApprovers;
                        }
                        break;
                    }
                }
                else
                {
                    string[] userApproverIDs = mstPVCClaim.UserApprovers.Split(',');
                    TempData["QueryMCUserApproverIDs"] = string.Join(",", userApproverIDs);
                }

                if (mstPVCClaim.HODApprover != "" && mstPVCClaim.UserApprovers == "")
                {
                    string[] hodApproverIDs = mstPVCClaim.HODApprover.Split(',');
                    TempData["QueryMCHODApproverIDs"] = string.Join(",", hodApproverIDs);
                    foreach (string approverID in hodApproverIDs)
                    {
                        if ((approverID != "" && approverID == HttpContext.User.FindFirst("userid").Value || (delegatedUserId.HasValue && approverID == delegatedUserId.Value.ToString())) && mstUserDetails.IsActive && (mstUserDetails.IsHOD || (User.FindFirst("issuperior") is null ? false : User.FindFirst("issuperior").Value.ToString().ToLower() == "true")))
                        {
                            TempData["ApprovedStatus"] = mstPVCClaim.ApprovalStatus;
                            //Session["ApprovedStatus"] = oSupplierPO.Approvalstatus;
                            TempData["HODApproverIDs"] = string.Join(",", hodApproverIDs.Skip(1));
                            pVCClaimVM.IsActionAllowed = true;
                        }
                        else
                        {
                            TempData["ApprovedStatus"] = "";
                            //Session["ApprovedStatus"] = oSupplierPO.Approvalstatus;
                            TempData["HODApproverIDs"] = mstPVCClaim.HODApprover;
                        }
                        break;
                    }
                }
                else
                {
                    string[] hodApproverIDs = mstPVCClaim.HODApprover.Split(',');
                    TempData["QueryMCHODApproverIDs"] = string.Join(",", hodApproverIDs);
                }

                if (mstPVCClaim.Verifier != "" && mstPVCClaim.UserApprovers == "")
                {
                    string[] verifierIDs = mstPVCClaim.Verifier.Split(',');
                    TempData["QueryMCVerifierIDs"] = string.Join(",", verifierIDs);
                    foreach (string verifierID in verifierIDs)
                    {
                        if (verifierID != "" && verifierID == HttpContext.User.FindFirst("userid").Value && mstUserDetails.IsActive && (mstUserDetails.IsHOD || (User.FindFirst("issuperior") is null ? false : User.FindFirst("issuperior").Value.ToString().ToLower() == "true")))
                        {
                            TempData["ApprovedStatus"] = mstPVCClaim.ApprovalStatus;
                            //Session["ApprovedStatus"] = oSupplierPO.Approvalstatus;
                            TempData["VerifierIDs"] = string.Join(",", verifierIDs.Skip(1));
                        }
                        else
                        {
                            TempData["ApprovedStatus"] = "";
                            //Session["ApprovedStatus"] = oSupplierPO.Approvalstatus;
                            TempData["VerifierIDs"] = mstPVCClaim.Verifier;
                        }
                        TempData["ApproverIDs"] = mstPVCClaim.Approver;
                        break;
                    }
                }
                else
                {
                    //TempData["VerifierIDs"] = mstPVCClaim.Verifier;
                    //TempData["ApproverIDs"] = mstPVCClaim.Approver;
                    string[] verifierIDs = mstPVCClaim.Verifier.Split(',');
                    TempData["QueryMCVerifierIDs"] = string.Join(",", verifierIDs);
                }

                //Approval Process code
                if (mstPVCClaim.Approver != "" && mstPVCClaim.Verifier == "" && mstPVCClaim.UserApprovers == "" && mstPVCClaim.HODApprover == "")
                {
                    string[] approverIDs = mstPVCClaim.Approver.Split(',');
                    TempData["QueryMCApproverIDs"] = string.Join(",", approverIDs);
                    foreach (string approverID in approverIDs)
                    {
                        if (approverID != "" && approverID == HttpContext.User.FindFirst("userid").Value && mstUserDetails.IsActive && (mstUserDetails.IsHOD || (User.FindFirst("issuperior") is null ? false : User.FindFirst("issuperior").Value.ToString().ToLower() == "true")))
                        {
                            TempData["ApprovedStatus"] = mstPVCClaim.ApprovalStatus;
                            //Session["ApprovedStatus"] = oSupplierPO.Approvalstatus;
                            TempData["ApproverIDs"] = string.Join(",", approverIDs.Skip(1));
                        }
                        else
                        {
                            TempData["ApprovedStatus"] = "";
                            //Session["ApprovedStatus"] = oSupplierPO.Approvalstatus;
                            TempData["ApproverIDs"] = mstPVCClaim.Approver;
                        }
                        break;
                    }
                }
                else
                {
                    string[] approverIDs = mstPVCClaim.Approver.Split(',');
                    TempData["QueryMCApproverIDs"] = string.Join(",", approverIDs);
                }

                if (mstPVCClaim.HODApprover != "" && mstPVCClaim.UserApprovers == "")
                {
                    string[] hodApproverIDs = mstPVCClaim.HODApprover.Split(',');
                    TempData["QueryMCHODApproverIDs"] = string.Join(",", hodApproverIDs);
                    foreach (string approverID in hodApproverIDs)
                    {
                        if ((approverID != "" && approverID == HttpContext.User.FindFirst("userid").Value || (delegatedUserId.HasValue && approverID == delegatedUserId.Value.ToString())) && mstUserDetails.IsActive && (mstUserDetails.IsHOD || (User.FindFirst("issuperior") is null ? false : User.FindFirst("issuperior").Value.ToString().ToLower() == "true")))
                        {
                            TempData["ApprovedStatus"] = mstPVCClaim.ApprovalStatus;
                            //Session["ApprovedStatus"] = oSupplierPO.Approvalstatus;
                            TempData["HODApproverIDs"] = string.Join(",", hodApproverIDs.Skip(1));
                            pVCClaimVM.IsActionAllowed = true;
                        }
                        else
                        {
                            TempData["ApprovedStatus"] = "";
                            //Session["ApprovedStatus"] = oSupplierPO.Approvalstatus;
                            TempData["HODApproverIDs"] = mstPVCClaim.HODApprover;
                        }
                        break;
                    }
                }
                else
                {
                    string[] hodApproverIDs = mstPVCClaim.HODApprover.Split(',');
                    TempData["QueryMCHODApproverIDs"] = string.Join(",", hodApproverIDs);
                }

                // Show actions based on alternate approver settings
                // Override all the isActionAllowed code above. When alternate approval is set, then no need to show the action on any scenario
                if (isAlternateApproverSet)
                {
                    pVCClaimVM.IsActionAllowed = false;
                }

                #region  -- GetQueries -- 


                int UserId = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value);
                ViewBag.userID = UserId;
                //var Userlist = objERPEntities.MstUsers.ToList().Where(i => i.UserID != UserId);
                var UserIds = new List<string>();
                //var Userlist1 = _context.users.ToList().Where(i => i.UserID != UserId);
                var Userlist = await _repository.MstUser.GetAllMCUsersForQueryAsync(UserId, UserIds);
                var Creater = TempData["CreatedBy"];
                var Verifiers = TempData["QueryMCVerifierIDs"];
                var Approvers = TempData["QueryMCApproverIDs"];
                var HODApprovers = TempData["QueryMCHODApproverIDs"];
                var UserApprovers = TempData["QueryMCUserApproverIDs"];

                string[] CreaterId = Creater.ToString().Split(',');
                string[] VerifiersId = Verifiers.ToString().Split(',');
                string[] HODApproversId = HODApprovers.ToString().Split(',');
                string[] UserApproversId = UserApprovers.ToString().Split(',');
                string[] ApproversId = Approvers.ToString().Split(',');

                UserIds.AddRange(CreaterId);
                UserIds.AddRange(UserApproversId);
                UserIds.AddRange(VerifiersId);
                UserIds.AddRange(HODApproversId);
                UserIds.AddRange(ApproversId);
                // Audit users
                //var AuditIDs = objERPEntities.MstSupplierPOAudits.ToList().Where(p => p.SPOID == SPOID).Select(p => p.AuditBy.ToString()).Distinct();
                //var AuditIDs1 = _context.MstMileageClaimAudit.ToList().Where(m => m.MCID == MCID).Select(m => m.AuditBy.ToString()).Distinct();
                //var AuditIDs = _repository.MstMileageClaimAudit.GetMstMileageClaimAuditByIdAsync(MCID).GetAwaiter().GetResult().Select(m => m.AuditBy.ToString()).Distinct();
                var mstPVCClaimAudits = await _repository.MstPVCClaimAudit.GetMstPVCClaimAuditByIdAsync(PVCCID);
                var AuditIDs = mstPVCClaimAudits.Select(m => m.AuditBy.ToString()).Distinct();
                foreach (var item in AuditIDs)
                {
                    string d = item;
                    UserIds.Add(d);
                }
                // Audit users
                //var spoUsers =  objERPEntities.MstUsers.ToList().Where(i => i.UserID != UserId && UserIds.Contains(i.UserID.ToString()));
                // var mcUsers1 = _context.users.ToList().Where(i => i.UserID != UserId && UserIds.Contains(i.UserID.ToString()));

                var mcUsers = await _repository.MstUser.GetAllMCUsersForQueryAsync(UserId, UserIds);
                var users = (from u in Userlist
                             join ut in mcUsers
                             on u.UserID equals ut.UserID
                             select new SelectListItem
                             {
                                 Text = u.Name.ToString(),
                                 Value = u.UserID.ToString()
                             }).OrderBy(p => p.Text).Distinct();
                ViewBag.queryusers = users;
                if (UserIds.Contains(UserId.ToString()))
                {
                    ViewBag.Access = 1;
                }
                else
                {
                    ViewBag.Access = 0;
                }

                #endregion getQueries


                pVCClaimDetailVM.PVCClaimVM = pVCClaimVM;
                //mileageClaimDetailVM.DtMileageClaimVMs = dtMileageClaimVMs;



                return View(pVCClaimDetailVM);
            }
            else
            {
                return Redirect("~/Login/Login");
            }
        }

        public async Task<IActionResult> ECDetails(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            long ECID = Convert.ToInt64(id);

            if (User != null && User.Identity.IsAuthenticated)
            {
                var mstUserDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(HttpContext.User.FindFirst("userid").Value));

                var mstExpenseClaim = await _repository.MstExpenseClaim.GetExpenseClaimByIdAsync(id);

                if (mstExpenseClaim == null)
                {
                    return NotFound();
                }

                #region Alternate Approver Check code
                int? delegatedUserId = null;
                int loggedInUserId = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value);

                int? approverId = await _alternateApproverHelper.IsAlternateApprovalSetForUser(loggedInUserId);
                bool isAlternateApproverSet = false;
                if (approverId.HasValue)
                {
                    // Alternate approver is configured for the current user. So, do not show actions
                    isAlternateApproverSet = true;
                }
                else
                {
                    // Current user has not delegated his approvals. Check if the current user has any delegation 
                    delegatedUserId = await _alternateApproverHelper.IsUserHasAnyAlternateApprovalSet(loggedInUserId);
                }
                TempData["IsAlternateApproverSet"] = isAlternateApproverSet;
                #endregion

                var dtExpenseSummaries = await _repository.DtExpenseClaimSummary.GetDtExpenseClaimSummaryByIdAsync(id);
                var dtExpenseClaims = await _repository.DtExpenseClaim.GetDtExpenseClaimByIdAsync(id);
                ExpenseClaimDetailVM expenseClaimDetailVM = new ExpenseClaimDetailVM();
                //List<DtMileageClaimVM> dtMileageClaimVMs = new List<DtMileageClaimVM>();
                expenseClaimDetailVM.DtExpenseClaimVMs = new List<DtExpenseClaimVM>();
                // List<clsDtSupplierPO> oclsDtSupplierPO = new List<clsDtSupplierPO>();
                foreach (var item in dtExpenseClaims)
                {
                    DtExpenseClaimVM dtExpenseClaimVM = new DtExpenseClaimVM();

                    dtExpenseClaimVM.ECItemID = item.ECItemID;
                    dtExpenseClaimVM.ECID = item.ECID;
                    dtExpenseClaimVM.DateOfJourney = item.Date;

                    dtExpenseClaimVM.Description = item.Description;
                    dtExpenseClaimVM.Amount = item.Amount;
                    dtExpenseClaimVM.Gst = item.GST;
                    dtExpenseClaimVM.AmountWithGST = item.Amount + item.GST;
                    dtExpenseClaimVM.ExpenseCategory = item.MstExpenseCategory.Description;
                    dtExpenseClaimVM.AccountCode = item.AccountCode;
                    dtExpenseClaimVM.ExpenseCategoryID = item.ExpenseCategoryID;
                    if (item.FacilityID != null)
                    {
                        var mstFacility = await _repository.MstFacility.GetFacilityByIdAsync(item.FacilityID);
                        dtExpenseClaimVM.Facility = mstFacility.FacilityName;
                    }
                    //if (item.FacilityID != null)
                    //{
                    //    dtMileageClaimVM.FacilityName = _repository.MstFacility.GetFacilityByIdAsync(item.FacilityID).GetAwaiter().GetResult().FacilityName.ToString();
                    //}
                    ////Need to change to not null
                    //if (item.FromFacilityID != 0)
                    //{
                    //    dtMileageClaimVM.FromFacilityName = _repository.MstFacility.GetFacilityByIdAsync(item.FromFacilityID).GetAwaiter().GetResult().FacilityName.ToString();
                    //}
                    ////Need to change to not null
                    //if (item.ToFacilityID != 0)
                    //{
                    //    dtMileageClaimVM.ToFacilityName = _repository.MstFacility.GetFacilityByIdAsync(item.ToFacilityID).GetAwaiter().GetResult().FacilityName.ToString();
                    //}

                    expenseClaimDetailVM.DtExpenseClaimVMs.Add(dtExpenseClaimVM);
                }
                expenseClaimDetailVM.DtExpenseClaimSummaries = dtExpenseSummaries;
                var GroupByQS = expenseClaimDetailVM.DtExpenseClaimVMs.GroupBy(s => s.ExpenseCategoryID);
                //var GroupByQS = (from std in expenseClaimDetailVM.DtExpenseClaimVMs
                //                                                           group std by std.ExpenseCategoryID);

                expenseClaimDetailVM.DtExpenseClaimVMSummary = new List<DtExpenseClaimVM>();

                foreach (var group in GroupByQS)
                {
                    DtExpenseClaimVM dtExpenseClaimVM = new DtExpenseClaimVM();
                    decimal amount = 0;
                    decimal gst = 0;
                    decimal sumamount = 0;
                    string ExpenseDesc = string.Empty;
                    string ExpenseCat = string.Empty;
                    string AccountCode = string.Empty;
                    int i = 0;
                    foreach (var dtExpense in group)
                    {
                        if (i == 0)
                            ExpenseDesc = dtExpense.Description;
                        i++;
                        amount = amount + dtExpense.Amount;
                        gst = gst + dtExpense.Gst;
                        sumamount = sumamount + dtExpense.AmountWithGST;
                        ExpenseCat = dtExpense.ExpenseCategory;
                        AccountCode = dtExpense.AccountCode;
                    }
                    gst = gst / group.Count();
                    dtExpenseClaimVM.Description = ExpenseDesc;
                    dtExpenseClaimVM.ExpenseCategory = ExpenseCat;
                    dtExpenseClaimVM.AccountCode = AccountCode;
                    dtExpenseClaimVM.Amount = amount;
                    dtExpenseClaimVM.Gst = gst;
                    dtExpenseClaimVM.AmountWithGST = sumamount;
                    expenseClaimDetailVM.DtExpenseClaimVMSummary.Add(dtExpenseClaimVM);
                }

                expenseClaimDetailVM.ExpenseClaimAudits = new List<ExpenseClaimAuditVM>();

                var dtExpenseClaimAudits = await _repository.MstExpenseClaimAudit.GetMstExpenseClaimAuditByIdAsync(id);

                foreach (var item in dtExpenseClaimAudits)
                {
                    ExpenseClaimAuditVM mstExpenseClaimAuditVM = new ExpenseClaimAuditVM();
                    mstExpenseClaimAuditVM.Action = item.Action;
                    mstExpenseClaimAuditVM.Description = item.Description;
                    mstExpenseClaimAuditVM.AuditDateTickle = Helper.RelativeDate(item.AuditDate);
                    expenseClaimDetailVM.ExpenseClaimAudits.Add(mstExpenseClaimAuditVM);
                }

                expenseClaimDetailVM.ExpenseClaimFileUploads = new List<DtExpenseClaimFileUpload>();

                expenseClaimDetailVM.ExpenseClaimFileUploads = _repository.DtExpenseClaimFileUpload.GetDtExpenseClaimAuditByIdAsync(id).Result.ToList();

                ExpenseClaimVM expenseClaimVM = new ExpenseClaimVM();
                expenseClaimVM.VoucherNo = mstExpenseClaim.VoucherNo;
                expenseClaimVM.ClaimType = mstExpenseClaim.ClaimType;
                expenseClaimVM.GrandTotal = mstExpenseClaim.GrandTotal;
                expenseClaimVM.TotalAmount = mstExpenseClaim.TotalAmount;
                expenseClaimVM.GrandGST = mstExpenseClaim.TotalAmount - mstExpenseClaim.GrandTotal;
                expenseClaimVM.Company = mstExpenseClaim.Company;
                expenseClaimVM.Name = mstExpenseClaim.MstUser.Name;
                expenseClaimVM.DepartmentName = mstExpenseClaim.MstDepartment.Department;
                expenseClaimVM.FacilityName = mstExpenseClaim.MstFacility.FacilityName;
                expenseClaimVM.CreatedDate = mstExpenseClaim.CreatedDate.ToString("d");
                expenseClaimVM.Verifier = mstExpenseClaim.Verifier;
                expenseClaimVM.Approver = mstExpenseClaim.Approver;
                expenseClaimVM.ECNo = mstExpenseClaim.ECNo;
                ViewBag.ECID = id;
                TempData["CreatedBy"] = mstExpenseClaim.CreatedBy;
                ViewBag.Approvalstatus = mstExpenseClaim.ApprovalStatus;


                TempData["ApprovedStatus"] = mstExpenseClaim.ApprovalStatus;
                TempData["FinalApproverID"] = mstExpenseClaim.FinalApprover;
                ViewBag.VoidReason = mstExpenseClaim.VoidReason == null ? "" : mstExpenseClaim.VoidReason;

                if (TempData["ApprovedStatus"].ToString() == "1" || TempData["ApprovedStatus"].ToString() == "2" || TempData["ApprovedStatus"].ToString() == "3" || TempData["ApprovedStatus"].ToString() == "-5" || TempData["ApprovedStatus"].ToString() == "6" || TempData["ApprovedStatus"].ToString() == "7" || TempData["ApprovedStatus"].ToString() == "9")
                {
                    ViewBag.ShowVoidBtn = 1;

                    if (mstUserDetails.IsActive && (mstUserDetails.IsHOD || (User.FindFirst("issuperior") is null ? false : User.FindFirst("issuperior").Value.ToString().ToLower() == "true")))
                    {
                        if (int.Parse(TempData["ApprovedStatus"].ToString()) < 3 || TempData["ApprovedStatus"].ToString() == "6" || TempData["ApprovedStatus"].ToString() == "7")
                        {
                            ViewBag.ShowVoidText = "Void";
                        }
                        else
                        {
                            ViewBag.ShowVoidText = "Request for Void";
                        }

                        if (TempData["ApprovedStatus"].ToString() == "-5" && TempData["FinalApproverID"].ToString() != HttpContext.User.FindFirst("userid").Value)
                        {
                            ViewBag.ShowVoidBtn = 0;
                        }
                    }
                    else
                    {
                        ViewBag.ShowVoidBtn = 0;
                    }
                }
                else
                {
                    ViewBag.ShowVoidBtn = 0;
                }

                //Verifier Process code
                TempData["VerifierIDs"] = "";
                TempData["ApproverIDs"] = "";
                TempData["QueryMCVerifierIDs"] = "";
                TempData["QueryMCApproverIDs"] = "";

                if (mstExpenseClaim.UserApprovers != "")
                {
                    string[] userApproverIDs = mstExpenseClaim.UserApprovers.Split(',');
                    TempData["QueryMCUserApproverIDs"] = string.Join(",", userApproverIDs);
                    foreach (string approverID in userApproverIDs)
                    {
                        if ((approverID != "" && approverID == HttpContext.User.FindFirst("userid").Value || (delegatedUserId.HasValue && approverID == delegatedUserId.Value.ToString())) && mstUserDetails.IsActive && (mstUserDetails.IsHOD || (User.FindFirst("issuperior") is null ? false : User.FindFirst("issuperior").Value.ToString().ToLower() == "true")))
                        {
                            TempData["ApprovedStatus"] = mstExpenseClaim.ApprovalStatus;
                            //Session["ApprovedStatus"] = oSupplierPO.Approvalstatus;
                            TempData["UserApproverIDs"] = string.Join(",", userApproverIDs.Skip(1));
                            expenseClaimVM.IsActionAllowed = true;
                        }
                        else
                        {
                            TempData["ApprovedStatus"] = "";
                            //Session["ApprovedStatus"] = oSupplierPO.Approvalstatus;
                            TempData["UserApproverIDs"] = mstExpenseClaim.UserApprovers;
                        }
                        break;
                    }
                }
                else
                {
                    string[] userApproverIDs = mstExpenseClaim.UserApprovers.Split(',');
                    TempData["QueryMCUserApproverIDs"] = string.Join(",", userApproverIDs);
                }

                if (mstExpenseClaim.Verifier != "" && mstExpenseClaim.UserApprovers == "")
                {
                    string[] verifierIDs = mstExpenseClaim.Verifier.Split(',');
                    TempData["QueryMCVerifierIDs"] = string.Join(",", verifierIDs);
                    foreach (string verifierID in verifierIDs)
                    {
                        if (verifierID != "" && verifierID == HttpContext.User.FindFirst("userid").Value && mstUserDetails.IsActive && (mstUserDetails.IsHOD || (User.FindFirst("issuperior") is null ? false : User.FindFirst("issuperior").Value.ToString().ToLower() == "true")))
                        {
                            TempData["ApprovedStatus"] = mstExpenseClaim.ApprovalStatus;
                            //Session["ApprovedStatus"] = oSupplierPO.Approvalstatus;
                            TempData["VerifierIDs"] = string.Join(",", verifierIDs.Skip(1));
                        }
                        else
                        {
                            TempData["ApprovedStatus"] = "";
                            //Session["ApprovedStatus"] = oSupplierPO.Approvalstatus;
                            TempData["VerifierIDs"] = mstExpenseClaim.Verifier;
                        }
                        TempData["ApproverIDs"] = mstExpenseClaim.Approver;
                        break;
                    }
                }
                else
                {
                    //TempData["VerifierIDs"] = mstExpenseClaim.Verifier;
                    //TempData["ApproverIDs"] = mstExpenseClaim.Approver;
                    string[] verifierIDs = mstExpenseClaim.Verifier.Split(',');
                    TempData["QueryMCVerifierIDs"] = string.Join(",", verifierIDs);
                }

                //Approval Process code
                if (mstExpenseClaim.Approver != "" && mstExpenseClaim.Verifier == "" && mstExpenseClaim.UserApprovers == "" && mstExpenseClaim.HODApprover == "")
                {
                    string[] approverIDs = mstExpenseClaim.Approver.Split(',');
                    TempData["QueryMCApproverIDs"] = string.Join(",", approverIDs);
                    foreach (string approverID in approverIDs)
                    {
                        if (approverID != "" && approverID == HttpContext.User.FindFirst("userid").Value && mstUserDetails.IsActive && (mstUserDetails.IsHOD || (User.FindFirst("issuperior") is null ? false : User.FindFirst("issuperior").Value.ToString().ToLower() == "true")))
                        {
                            TempData["ApprovedStatus"] = mstExpenseClaim.ApprovalStatus;
                            //Session["ApprovedStatus"] = oSupplierPO.Approvalstatus;
                            TempData["ApproverIDs"] = string.Join(",", approverIDs.Skip(1));
                        }
                        else
                        {
                            TempData["ApprovedStatus"] = "";
                            //Session["ApprovedStatus"] = oSupplierPO.Approvalstatus;
                            TempData["ApproverIDs"] = mstExpenseClaim.Approver;
                        }
                        break;
                    }
                }
                else
                {
                    string[] approverIDs = mstExpenseClaim.Approver.Split(',');
                    TempData["QueryMCApproverIDs"] = string.Join(",", approverIDs);
                }

                if (mstExpenseClaim.HODApprover != "" && mstExpenseClaim.Verifier == "" && mstExpenseClaim.UserApprovers == "")
                {
                    string[] hodApproverIDs = mstExpenseClaim.HODApprover.Split(',');
                    TempData["QueryMCHODApproverIDs"] = string.Join(",", hodApproverIDs);
                    foreach (string approverID in hodApproverIDs)
                    {
                        if ((approverID != "" && approverID == HttpContext.User.FindFirst("userid").Value || (delegatedUserId.HasValue && approverID == delegatedUserId.Value.ToString())) && mstUserDetails.IsActive && (mstUserDetails.IsHOD || (User.FindFirst("issuperior") is null ? false : User.FindFirst("issuperior").Value.ToString().ToLower() == "true")))
                        {
                            TempData["ApprovedStatus"] = mstExpenseClaim.ApprovalStatus;
                            //Session["ApprovedStatus"] = oSupplierPO.Approvalstatus;
                            TempData["HODApproverIDs"] = string.Join(",", hodApproverIDs.Skip(1));
                            expenseClaimVM.IsActionAllowed = true;
                        }
                        else
                        {
                            TempData["ApprovedStatus"] = "";
                            //Session["ApprovedStatus"] = oSupplierPO.Approvalstatus;
                            TempData["HODApproverIDs"] = mstExpenseClaim.HODApprover;
                        }
                        break;
                    }
                }
                else
                {
                    string[] hodApproverIDs = mstExpenseClaim.HODApprover.Split(',');
                    TempData["QueryMCHODApproverIDs"] = string.Join(",", hodApproverIDs);
                }

                // Show actions based on alternate approver settings
                // Override all the isActionAllowed code above. When alternate approval is set, then no need to show the action on any scenario
                if (isAlternateApproverSet)
                {
                    expenseClaimVM.IsActionAllowed = false;
                }

                #region  -- GetQueries -- 


                int UserId = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value);
                ViewBag.userID = UserId;
                //var Userlist = objERPEntities.MstUsers.ToList().Where(i => i.UserID != UserId);
                var UserIds = new List<string>();
                //var Userlist1 = _context.users.ToList().Where(i => i.UserID != UserId);
                var Userlist = await _repository.MstUser.GetAllMCUsersForQueryAsync(UserId, UserIds);
                var Creater = TempData["CreatedBy"];
                var Verifiers = TempData["QueryMCVerifierIDs"];
                var HODApprovers = TempData["QueryMCHODApproverIDs"];
                var UserApprovers = TempData["QueryMCUserApproverIDs"];
                var Approvers = TempData["QueryMCApproverIDs"];

                string[] CreaterId = Creater.ToString().Split(',');
                string[] VerifiersId = Verifiers.ToString().Split(',');
                string[] HODApproversId = HODApprovers.ToString().Split(',');
                string[] UserApproversId = UserApprovers.ToString().Split(',');
                string[] ApproversId = Approvers.ToString().Split(',');

                UserIds.AddRange(CreaterId);
                UserIds.AddRange(UserApproversId);
                UserIds.AddRange(VerifiersId);
                UserIds.AddRange(HODApproversId);
                UserIds.AddRange(ApproversId);
                // Audit users
                //var AuditIDs = objERPEntities.MstSupplierPOAudits.ToList().Where(p => p.SPOID == SPOID).Select(p => p.AuditBy.ToString()).Distinct();
                //var AuditIDs1 = _context.MstMileageClaimAudit.ToList().Where(m => m.MCID == MCID).Select(m => m.AuditBy.ToString()).Distinct();
                //var AuditIDs = _repository.MstMileageClaimAudit.GetMstMileageClaimAuditByIdAsync(MCID).GetAwaiter().GetResult().Select(m => m.AuditBy.ToString()).Distinct();
                var mstExpenseClaimAudits = await _repository.MstExpenseClaimAudit.GetMstExpenseClaimAuditByIdAsync(ECID);
                var AuditIDs = mstExpenseClaimAudits.Select(m => m.AuditBy.ToString()).Distinct();
                foreach (var item in AuditIDs)
                {
                    string d = item;
                    UserIds.Add(d);
                }
                // Audit users
                //var spoUsers =  objERPEntities.MstUsers.ToList().Where(i => i.UserID != UserId && UserIds.Contains(i.UserID.ToString()));
                // var mcUsers1 = _context.users.ToList().Where(i => i.UserID != UserId && UserIds.Contains(i.UserID.ToString()));

                var mcUsers = await _repository.MstUser.GetAllMCUsersForQueryAsync(UserId, UserIds);
                var users = (from u in Userlist
                             join ut in mcUsers
                             on u.UserID equals ut.UserID
                             select new SelectListItem
                             {
                                 Text = u.Name.ToString(),
                                 Value = u.UserID.ToString()
                             }).OrderBy(p => p.Text).Distinct();
                ViewBag.queryusers = users;
                if (UserIds.Contains(UserId.ToString()))
                {
                    ViewBag.Access = 1;
                }
                else
                {
                    ViewBag.Access = 0;
                }

                #endregion getQueries


                expenseClaimDetailVM.ExpenseClaimVM = expenseClaimVM;
                //mileageClaimDetailVM.DtMileageClaimVMs = dtMileageClaimVMs;



                return View(expenseClaimDetailVM);
            }
            else
            {
                return Redirect("~/Login/Login");
            }
        }

        public async Task<IActionResult> Details(long? id, string cno)
        {
            if (id == null)
            {
                return NotFound();
            }
            if ((cno == null && id != null) || cno.ToLower().Contains("mc"))
            {
                long MCID = Convert.ToInt64(id);

                if (User != null && User.Identity.IsAuthenticated)
                {
                    var mstUserDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(HttpContext.User.FindFirst("userid").Value));

                    var mstMileageClaim = await _repository.MstMileageClaim.GetMileageClaimByIdAsync(id);

                    if (mstMileageClaim == null)
                    {
                        return NotFound();
                    }

                    #region Alternate Approver Check code
                    int? delegatedUserId = null;
                    int loggedInUserId = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value);

                    int? approverId = await _alternateApproverHelper.IsAlternateApprovalSetForUser(loggedInUserId);
                    bool isAlternateApproverSet = false;
                    if (approverId.HasValue)
                    {
                        // Alternate approver is configured for the current user. So, do not show actions
                        isAlternateApproverSet = true;
                    }
                    else
                    {
                        // Current user has not delegated his approvals. Check if the current user has any delegation 
                        delegatedUserId = await _alternateApproverHelper.IsUserHasAnyAlternateApprovalSet(loggedInUserId);
                    }
                    TempData["IsAlternateApproverSet"] = isAlternateApproverSet;
                    #endregion

                    var dtMileageSummaries = await _repository.DtMileageClaimSummary.GetDtMileageClaimSummaryByIdAsync(id);
                    var dtMileageClaims = await _repository.DtMileageClaim.GetDtMileageClaimByIdAsync(id);
                    MileageClaimDetailVM mileageClaimDetailVM = new MileageClaimDetailVM();
                    //List<DtMileageClaimVM> dtMileageClaimVMs = new List<DtMileageClaimVM>();
                    mileageClaimDetailVM.DtMileageClaimVMs = new List<DtMileageClaimVM>();
                    // List<clsDtSupplierPO> oclsDtSupplierPO = new List<clsDtSupplierPO>();
                    foreach (var item in dtMileageClaims)
                    {
                        DtMileageClaimVM dtMileageClaimVM = new DtMileageClaimVM();

                        dtMileageClaimVM.MCItemID = item.MCItemID;
                        dtMileageClaimVM.MCID = item.MCID;
                        dtMileageClaimVM.DateOfJourney = item.DateOfJourney;
                        dtMileageClaimVM.InTime = item.InTime;
                        dtMileageClaimVM.OutTime = item.OutTime;
                        dtMileageClaimVM.StartReading = item.StartReading;
                        dtMileageClaimVM.EndReading = item.EndReading;
                        dtMileageClaimVM.Kms = item.Kms;
                        dtMileageClaimVM.Remark = item.Remark;
                        dtMileageClaimVM.Amount = item.Amount;
                        dtMileageClaimVM.AccountCode = item.AccountCode;

                        if (item.FacilityID != null)
                        {
                            var mstFacility = await _repository.MstFacility.GetFacilityByIdAsync(item.FacilityID);
                            dtMileageClaimVM.FacilityName = mstFacility.FacilityName;
                        }
                        dtMileageClaimVM.FromFacilityID = item.FromFacilityID;
                        dtMileageClaimVM.ToFacilityID = item.ToFacilityID;
                        //dtMileageClaimVM.FromFacilityID = item.FromFacilityID;
                        //dtMileageClaimVM.ToFacilityID = item.ToFacilityID;
                        //Need to change to not null
                        //if (item.FromFacilityID != 0)
                        //{
                        //    var mstFacility = await _repository.MstFacility.GetFacilityByIdAsync(item.FromFacilityID);
                        //    dtMileageClaimVM.FromFacilityName = mstFacility.FacilityName;
                        //}
                        ////Need to change to not null
                        //if (item.ToFacilityID != 0)
                        //{
                        //    var mstFacility = await _repository.MstFacility.GetFacilityByIdAsync(item.ToFacilityID);
                        //    dtMileageClaimVM.ToFacilityName = mstFacility.FacilityName;
                        //}

                        mileageClaimDetailVM.DtMileageClaimVMs.Add(dtMileageClaimVM);
                    }
                    mileageClaimDetailVM.DtMileageClaimSummaries = dtMileageSummaries;
                    var GroupByQS = mileageClaimDetailVM.DtMileageClaimVMs.GroupBy(s => s.MCItemID);
                    //var GroupByQS = (from std in expenseClaimDetailVM.DtExpenseClaimVMs
                    //                                                           group std by std.ExpenseCategoryID);

                    mileageClaimDetailVM.DtMileageClaimVMSummary = new List<DtMileageClaimVM>();

                    foreach (var group in GroupByQS)
                    {
                        DtMileageClaimVM dtMileageClaimVM = new DtMileageClaimVM();
                        decimal amount = 0;
                        decimal gst = 0;
                        decimal sumamount = 0;
                        string ExpenseDesc = string.Empty;
                        string ExpenseCat = string.Empty;
                        string AccountCode = string.Empty;
                        int i = 0;
                        foreach (var dtExpense in group)
                        {
                            if (i == 0)
                                ExpenseDesc = "Mileage Claim";
                            i++;
                            amount = amount + dtExpense.Amount;
                            //gst = gst + dtExpense.Gst;
                            //sumamount = sumamount + dtExpense.AmountWithGST;
                            ExpenseCat = "Travelling Private Car";
                            AccountCode = dtExpense.AccountCode;
                        }
                        gst = gst / group.Count();
                        dtMileageClaimVM.Description = ExpenseDesc;
                        dtMileageClaimVM.ExpenseCategory = ExpenseCat;
                        dtMileageClaimVM.AccountCode = AccountCode;
                        dtMileageClaimVM.Amount = amount;
                        //dtMileageClaimVM.Gst = gst;
                        //dtTBClaimVM.AmountWithGST = sumamount;
                        mileageClaimDetailVM.DtMileageClaimVMSummary.Add(dtMileageClaimVM);
                    }

                    mileageClaimDetailVM.MileageClaimAudits = new List<MileageClaimAuditVM>();
                    var dtMileageClaimAudits = await _repository.MstMileageClaimAudit.GetMstMileageClaimAuditByIdAsync(id);

                    foreach (var item in dtMileageClaimAudits)
                    {
                        MileageClaimAuditVM mstMileageClaimAuditVM = new MileageClaimAuditVM();
                        mstMileageClaimAuditVM.Action = item.Action;
                        mstMileageClaimAuditVM.Description = item.Description;
                        mstMileageClaimAuditVM.AuditDateTickle = Helper.RelativeDate(item.AuditDate);
                        mileageClaimDetailVM.MileageClaimAudits.Add(mstMileageClaimAuditVM);
                    }

                    mileageClaimDetailVM.MileageClaimFileUploads = new List<DtMileageClaimFileUpload>();

                    mileageClaimDetailVM.MileageClaimFileUploads = _repository.DtMileageClaimFileUpload.GetDtMileageClaimAuditByIdAsync(id).Result.ToList();

                    MileageClaimVM mileageClaimVM = new MileageClaimVM();
                    mileageClaimVM.VoucherNo = mstMileageClaim.VoucherNo;
                    mileageClaimVM.TravelMode = mstMileageClaim.TravelMode;
                    mileageClaimVM.GrandTotal = mstMileageClaim.GrandTotal;
                    mileageClaimVM.TotalKm = mstMileageClaim.TotalKm;
                    mileageClaimVM.Company = mstMileageClaim.Company;
                    mileageClaimVM.Name = mstMileageClaim.MstUser.Name;
                    mileageClaimVM.DepartmentName = mstMileageClaim.MstDepartment.Department;
                    mileageClaimVM.FacilityName = mstMileageClaim.MstFacility.FacilityName;
                    mileageClaimVM.CreatedDate = mstMileageClaim.CreatedDate.ToString("d");
                    mileageClaimVM.Verifier = mstMileageClaim.Verifier;
                    mileageClaimVM.Approver = mstMileageClaim.Approver;
                    ViewBag.MCID = id;
                    mileageClaimVM.MCNo = mstMileageClaim.MCNo;
                    TempData["CreatedBy"] = mstMileageClaim.CreatedBy;
                    ViewBag.Approvalstatus = mstMileageClaim.ApprovalStatus;


                    TempData["ApprovedStatus"] = mstMileageClaim.ApprovalStatus;
                    TempData["FinalApproverID"] = mstMileageClaim.FinalApprover;
                    ViewBag.VoidReason = mstMileageClaim.VoidReason == null ? "" : mstMileageClaim.VoidReason;

                    if (TempData["ApprovedStatus"].ToString() == "1" || TempData["ApprovedStatus"].ToString() == "2" || TempData["ApprovedStatus"].ToString() == "3" || TempData["ApprovedStatus"].ToString() == "-5" || TempData["ApprovedStatus"].ToString() == "6" || TempData["ApprovedStatus"].ToString() == "7" || TempData["ApprovedStatus"].ToString() == "9" || TempData["ApprovedStatus"].ToString() == "10")
                    {
                        ViewBag.ShowVoidBtn = 1;

                        if (mstUserDetails.IsActive && (mstUserDetails.IsHOD || (User.FindFirst("issuperior") is null ? false : User.FindFirst("issuperior").Value.ToString().ToLower() == "true")))
                        {
                            if (int.Parse(TempData["ApprovedStatus"].ToString()) < 3 || TempData["ApprovedStatus"].ToString() == "6" || TempData["ApprovedStatus"].ToString() == "7")
                            {
                                ViewBag.ShowVoidText = "Void";
                            }
                            else
                            {
                                ViewBag.ShowVoidText = "Request for Void";
                            }

                            if (TempData["ApprovedStatus"].ToString() == "-5" && TempData["FinalApproverID"].ToString() != HttpContext.User.FindFirst("userid").Value)
                            {
                                ViewBag.ShowVoidBtn = 0;
                            }
                        }
                        else
                        {
                            ViewBag.ShowVoidBtn = 0;
                        }
                    }
                    else
                    {
                        ViewBag.ShowVoidBtn = 0;
                    }

                    //Verifier Process code
                    TempData["VerifierIDs"] = "";
                    TempData["ApproverIDs"] = "";
                    TempData["QueryMCVerifierIDs"] = "";
                    TempData["QueryMCApproverIDs"] = "";

                    if (mstMileageClaim.UserApprovers != "")
                    {
                        string[] userApproverIDs = mstMileageClaim.UserApprovers.Split(',');
                        TempData["QueryMCUserApproverIDs"] = string.Join(",", userApproverIDs);
                        foreach (string approverID in userApproverIDs)
                        {
                            if ((approverID != "" && approverID == HttpContext.User.FindFirst("userid").Value || (delegatedUserId.HasValue && approverID == delegatedUserId.Value.ToString())) && mstUserDetails.IsActive && (mstUserDetails.IsHOD || (User.FindFirst("issuperior") is null ? false : User.FindFirst("issuperior").Value.ToString().ToLower() == "true")))
                            {
                                TempData["ApprovedStatus"] = mstMileageClaim.ApprovalStatus;
                                //Session["ApprovedStatus"] = oSupplierPO.Approvalstatus;
                                TempData["UserApproverIDs"] = string.Join(",", userApproverIDs.Skip(1));
                                mileageClaimVM.IsActionAllowed = true;
                            }
                            else
                            {
                                TempData["ApprovedStatus"] = "";
                                //Session["ApprovedStatus"] = oSupplierPO.Approvalstatus;
                                TempData["UserApproverIDs"] = mstMileageClaim.UserApprovers;
                            }
                            break;
                        }
                    }
                    else
                    {
                        string[] userApproverIDs = mstMileageClaim.UserApprovers.Split(',');
                        TempData["QueryMCUserApproverIDs"] = string.Join(",", userApproverIDs);
                    }

                    if (mstMileageClaim.Verifier != "" && mstMileageClaim.UserApprovers == "")
                    {
                        string[] verifierIDs = mstMileageClaim.Verifier.Split(',');
                        TempData["QueryMCVerifierIDs"] = string.Join(",", verifierIDs);
                        foreach (string verifierID in verifierIDs)
                        {
                            if (verifierID != "" && verifierID == HttpContext.User.FindFirst("userid").Value && mstUserDetails.IsActive && (mstUserDetails.IsHOD || (User.FindFirst("issuperior") is null ? false : User.FindFirst("issuperior").Value.ToString().ToLower() == "true")))
                            {
                                TempData["ApprovedStatus"] = mstMileageClaim.ApprovalStatus;
                                //Session["ApprovedStatus"] = oSupplierPO.Approvalstatus;
                                TempData["VerifierIDs"] = string.Join(",", verifierIDs.Skip(1));
                            }
                            else
                            {
                                TempData["ApprovedStatus"] = "";
                                //Session["ApprovedStatus"] = oSupplierPO.Approvalstatus;
                                TempData["VerifierIDs"] = mstMileageClaim.Verifier;
                            }
                            TempData["ApproverIDs"] = mstMileageClaim.Approver;
                            break;
                        }
                    }
                    else
                    {
                        //TempData["VerifierIDs"] = mstMileageClaim.Verifier;
                        //TempData["ApproverIDs"] = mstMileageClaim.Approver;
                        string[] verifierIDs = mstMileageClaim.Verifier.Split(',');
                        TempData["QueryMCVerifierIDs"] = string.Join(",", verifierIDs);
                    }



                    //Approval Process code
                    if (mstMileageClaim.Approver != "" && mstMileageClaim.Verifier == "" && mstMileageClaim.UserApprovers == "" && mstMileageClaim.HODApprover == "")
                    {
                        string[] approverIDs = mstMileageClaim.Approver.Split(',');
                        TempData["QueryMCApproverIDs"] = string.Join(",", approverIDs);
                        foreach (string approverID in approverIDs)
                        {
                            if (approverID != "" && approverID == HttpContext.User.FindFirst("userid").Value && mstUserDetails.IsActive && (mstUserDetails.IsHOD || (User.FindFirst("issuperior") is null ? false : User.FindFirst("issuperior").Value.ToString().ToLower() == "true")))
                            {
                                TempData["ApprovedStatus"] = mstMileageClaim.ApprovalStatus;
                                //Session["ApprovedStatus"] = oSupplierPO.Approvalstatus;
                                TempData["ApproverIDs"] = string.Join(",", approverIDs.Skip(1));
                            }
                            else
                            {
                                TempData["ApprovedStatus"] = "";
                                //Session["ApprovedStatus"] = oSupplierPO.Approvalstatus;
                                TempData["ApproverIDs"] = mstMileageClaim.Approver;
                            }
                            break;
                        }
                    }
                    else
                    {
                        string[] approverIDs = mstMileageClaim.Approver.Split(',');
                        TempData["QueryMCApproverIDs"] = string.Join(",", approverIDs);
                    }



                    if (mstMileageClaim.HODApprover != "" && mstMileageClaim.Verifier == "" && mstMileageClaim.UserApprovers == "")
                    {
                        string[] hodApproverIDs = mstMileageClaim.HODApprover.Split(',');
                        TempData["QueryMCHODApproverIDs"] = string.Join(",", hodApproverIDs);
                        foreach (string approverID in hodApproverIDs)
                        {
                            if ((approverID != "" && approverID == HttpContext.User.FindFirst("userid").Value || (delegatedUserId.HasValue && approverID == delegatedUserId.Value.ToString())) && mstUserDetails.IsActive && (mstUserDetails.IsHOD || (User.FindFirst("issuperior") is null ? false : User.FindFirst("issuperior").Value.ToString().ToLower() == "true")))
                            {
                                TempData["ApprovedStatus"] = mstMileageClaim.ApprovalStatus;
                                //Session["ApprovedStatus"] = oSupplierPO.Approvalstatus;
                                TempData["HODApproverIDs"] = string.Join(",", hodApproverIDs.Skip(1));
                                mileageClaimVM.IsActionAllowed = true;
                            }
                            else
                            {
                                TempData["ApprovedStatus"] = "";
                                //Session["ApprovedStatus"] = oSupplierPO.Approvalstatus;
                                TempData["HODApproverIDs"] = mstMileageClaim.HODApprover;
                            }
                            break;
                        }
                    }
                    else
                    {
                        string[] hodApproverIDs = mstMileageClaim.HODApprover.Split(',');
                        TempData["QueryMCHODApproverIDs"] = string.Join(",", hodApproverIDs);
                    }

                    // Show actions based on alternate approver settings
                    // Override all the isActionAllowed code above. When alternate approval is set, then no need to show the action on any scenario
                    if (isAlternateApproverSet)
                    {
                        mileageClaimVM.IsActionAllowed = false;
                    }

                    #region  -- GetQueries -- 


                    int UserId = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value);
                    ViewBag.userID = UserId;
                    //var Userlist = objERPEntities.MstUsers.ToList().Where(i => i.UserID != UserId);
                    var UserIds = new List<string>();
                    //var Userlist1 = _context.users.ToList().Where(i => i.UserID != UserId);
                    var Userlist = await _repository.MstUser.GetAllMCUsersForQueryAsync(UserId, UserIds);
                    var Creater = TempData["CreatedBy"];
                    var Verifiers = TempData["QueryMCVerifierIDs"];
                    var HODApprovers = TempData["QueryMCHODApproverIDs"];
                    var UserApprovers = TempData["QueryMCUserApproverIDs"];
                    var Approvers = TempData["QueryMCApproverIDs"];

                    string[] CreaterId = Creater.ToString().Split(',');
                    string[] VerifiersId = Verifiers.ToString().Split(',');
                    string[] HODApproversId = HODApprovers.ToString().Split(',');
                    string[] UserApproversId = UserApprovers.ToString().Split(',');
                    string[] ApproversId = Approvers.ToString().Split(',');

                    UserIds.AddRange(CreaterId);
                    UserIds.AddRange(UserApproversId);
                    UserIds.AddRange(VerifiersId);
                    UserIds.AddRange(HODApproversId);
                    UserIds.AddRange(ApproversId);
                    // Audit users
                    //var AuditIDs = objERPEntities.MstSupplierPOAudits.ToList().Where(p => p.SPOID == SPOID).Select(p => p.AuditBy.ToString()).Distinct();
                    //var AuditIDs1 = _context.MstMileageClaimAudit.ToList().Where(m => m.MCID == MCID).Select(m => m.AuditBy.ToString()).Distinct();
                    //var AuditIDs = _repository.MstMileageClaimAudit.GetMstMileageClaimAuditByIdAsync(MCID).GetAwaiter().GetResult().Select(m => m.AuditBy.ToString()).Distinct();
                    var mstMileageClaimAudits = await _repository.MstMileageClaimAudit.GetMstMileageClaimAuditByIdAsync(MCID);
                    var AuditIDs = mstMileageClaimAudits.Select(m => m.AuditBy.ToString()).Distinct();
                    foreach (var item in AuditIDs)
                    {
                        string d = item;
                        UserIds.Add(d);
                    }
                    // Audit users
                    //var spoUsers =  objERPEntities.MstUsers.ToList().Where(i => i.UserID != UserId && UserIds.Contains(i.UserID.ToString()));
                    // var mcUsers1 = _context.users.ToList().Where(i => i.UserID != UserId && UserIds.Contains(i.UserID.ToString()));

                    var mcUsers = await _repository.MstUser.GetAllMCUsersForQueryAsync(UserId, UserIds);
                    var users = (from u in Userlist
                                 join ut in mcUsers
                                 on u.UserID equals ut.UserID
                                 select new SelectListItem
                                 {
                                     Text = u.Name.ToString(),
                                     Value = u.UserID.ToString()
                                 }).OrderBy(p => p.Text).Distinct();
                    ViewBag.queryusers = users;
                    if (UserIds.Contains(UserId.ToString()))
                    {
                        ViewBag.Access = 1;
                    }
                    else
                    {
                        ViewBag.Access = 0;
                    }

                    #endregion getQueries




                    mileageClaimDetailVM.MileageClaimVM = mileageClaimVM;
                    //mileageClaimDetailVM.DtMileageClaimVMs = dtMileageClaimVMs;



                    return View(mileageClaimDetailVM);
                }
                else
                {
                    return Redirect("~/Login/Login");
                }
            }
            else if (cno.ToLower().Contains("ec"))
            {
                return RedirectToAction("ECDetails", "HodSummary", new { id = id });
            }
            else if (cno.ToLower().Contains("tb"))
            {
                return RedirectToAction("TBCDetails", "HodSummary", new { id = id });
            }
            else if (cno.ToLower().Contains("pvg"))
            {
                return RedirectToAction("PVGCDetails", "HodSummary", new { id = id });
            }
            else if (cno.ToLower().Contains("pvc"))
            {
                return RedirectToAction("PVCCDetails", "HodSummary", new { id = id });
            }
            else
            {
                return RedirectToAction("TBCDetails", "HodSummary", new { id = id });
            }
        }

        public async Task<JsonResult> UpdateStatusforVoid(string id, string reason, string approvedStatus, string claim)
        {
            int loggedInUserId = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value);
            bool isAlternateApprover = false;
            var delegatedUserId = await _alternateApproverHelper.IsUserHasAnyAlternateApprovalSet(loggedInUserId);
            if (delegatedUserId.HasValue)
            {
                isAlternateApprover = true;
            }

            if (claim != "" && claim.Contains("ec"))
            {
                if (User != null && User.Identity.IsAuthenticated)
                {
                    int ECID = Convert.ToInt32(id);

                    var mstExpenseClaim = await _repository.MstExpenseClaim.GetExpenseClaimByIdAsync(ECID);

                    if (mstExpenseClaim == null)
                    {
                        // return NotFound();
                    }

                    if (Convert.ToInt32(approvedStatus) == 3 || Convert.ToInt32(approvedStatus) == 9 || Convert.ToInt32(approvedStatus) == 10)
                    {
                        await _repository.MstExpenseClaim.UpdateMstExpenseClaimStatus(ECID, -5, int.Parse(HttpContext.User.FindFirst("userid").Value), DateTime.Now, reason, string.Empty, string.Empty, string.Empty, string.Empty, isAlternateApprover, 0);
                    }
                    else
                    {
                        await _repository.MstExpenseClaim.UpdateMstExpenseClaimStatus(ECID, 5, int.Parse(HttpContext.User.FindFirst("userid").Value), DateTime.Now, reason, string.Empty, string.Empty, string.Empty, string.Empty, isAlternateApprover, 0);
                        string domainUrl = HttpContext.Request.Scheme + "://" + HttpContext.Request.Host;
                        string clickUrl = domainUrl + "/" + "ExpenseClaim/Details/" + ECID;

                        var mstSenderDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(HttpContext.User.FindFirst("userid").Value));
                        var senderName = mstSenderDetails.Name;
                        var mstVerifierDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(mstExpenseClaim.UserID));
                        var toEmail = mstVerifierDetails.EmailAddress;
                        var receiverName = mstVerifierDetails.Name;
                        var claimNo = mstExpenseClaim.ECNo;
                        var screen = "Expense Claim";
                        var approvalType = "Voided ";
                        int userID = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value);
                        var subject = "Expense Claim " + claimNo + " has been Voided ";

                        var rejectReason = reason;
                        var lastApprover = string.Empty;
                        var nextApprover = senderName;

                        BackgroundJob.Enqueue(() => _sendMailServices.SendEmail("Rejected.html", screen, subject, senderName, receiverName, claimNo, approvalType, userID, toEmail, clickUrl, lastApprover, nextApprover, rejectReason));
                    }
                    return Json(new { res = "Done" });
                }
                else
                {
                    return Json(new { res = "Done" });
                }
            }
            else if (claim != "" && claim.Contains("tbc"))
            {
                if (User != null && User.Identity.IsAuthenticated)
                {
                    int TBCID = Convert.ToInt32(id);

                    var mstTBClaim = await _repository.MstTBClaim.GetTBClaimByIdAsync(TBCID);

                    if (mstTBClaim == null)
                    {
                        // return NotFound();
                    }

                    if (Convert.ToInt32(approvedStatus) == 3 || Convert.ToInt32(approvedStatus) == 9 || Convert.ToInt32(approvedStatus) == 10)
                    {
                        await _repository.MstTBClaim.UpdateMstTBClaimStatus(TBCID, -5, int.Parse(HttpContext.User.FindFirst("userid").Value), DateTime.Now, reason, string.Empty, string.Empty, string.Empty, string.Empty, isAlternateApprover, 0);
                    }
                    else
                    {
                        await _repository.MstTBClaim.UpdateMstTBClaimStatus(TBCID, 5, int.Parse(HttpContext.User.FindFirst("userid").Value), DateTime.Now, reason, string.Empty, string.Empty, string.Empty, string.Empty, isAlternateApprover, 0);
                        string domainUrl = HttpContext.Request.Scheme + "://" + HttpContext.Request.Host;
                        string clickUrl = domainUrl + "/" + "TelephoneBillClaim/Details/" + TBCID;

                        var mstSenderDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(HttpContext.User.FindFirst("userid").Value));
                        var senderName = mstSenderDetails.Name;
                        var mstVerifierDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(mstTBClaim.UserID));
                        var toEmail = mstVerifierDetails.EmailAddress;
                        var receiverName = mstVerifierDetails.Name;
                        var claimNo = mstTBClaim.TBCNo;
                        var screen = "Telephone Bill Claim";
                        var approvalType = "Voided ";
                        int userID = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value);
                        var subject = "Telephone Bill Claim " + claimNo + " has been Voided ";

                        var rejectReason = reason;
                        var lastApprover = string.Empty;
                        var nextApprover = senderName;

                        BackgroundJob.Enqueue(() => _sendMailServices.SendEmail("Rejected.html", screen, subject, senderName, receiverName, claimNo, approvalType, userID, toEmail, clickUrl, lastApprover, nextApprover, rejectReason));

                    }
                    return Json(new { res = "Done" });
                }
                else
                {
                    return Json(new { res = "Done" });
                }
            }
            else if (claim != "" && claim.Contains("pvc"))
            {
                if (User != null && User.Identity.IsAuthenticated)
                {
                    int PVCCID = Convert.ToInt32(id);

                    var mstPVCClaim = await _repository.MstPVCClaim.GetPVCClaimByIdAsync(PVCCID);

                    if (mstPVCClaim == null)
                    {
                        // return NotFound();
                    }

                    if (Convert.ToInt32(approvedStatus) == 3 || Convert.ToInt32(approvedStatus) == 9 || Convert.ToInt32(approvedStatus) == 10)
                    {
                        await _repository.MstPVCClaim.UpdateMstPVCClaimStatus(PVCCID, -5, int.Parse(HttpContext.User.FindFirst("userid").Value), DateTime.Now, reason, string.Empty, string.Empty, string.Empty, string.Empty, isAlternateApprover,0);
                    }
                    else
                    {
                        await _repository.MstPVCClaim.UpdateMstPVCClaimStatus(PVCCID, 5, int.Parse(HttpContext.User.FindFirst("userid").Value), DateTime.Now, reason, string.Empty, string.Empty, string.Empty, string.Empty, isAlternateApprover,0);
                        string domainUrl = HttpContext.Request.Scheme + "://" + HttpContext.Request.Host;
                        string clickUrl = domainUrl + "/" + "PVChequeClaim/Details/" + PVCCID;

                        var mstSenderDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(HttpContext.User.FindFirst("userid").Value));
                        var senderName = mstSenderDetails.Name;
                        var mstVerifierDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(mstPVCClaim.UserID));
                        var toEmail = mstVerifierDetails.EmailAddress;
                        var receiverName = mstVerifierDetails.Name;
                        var claimNo = mstPVCClaim.PVCCNo;
                        var screen = "PV Cheque Claim";
                        var approvalType = "Voided ";
                        int userID = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value);
                        var subject = "PV Cheque Claim " + claimNo + " has been Voided ";

                        var rejectReason = reason;
                        var lastApprover = string.Empty;
                        var nextApprover = senderName;

                        BackgroundJob.Enqueue(() => _sendMailServices.SendEmail("Rejected.html", screen, subject, senderName, receiverName, claimNo, approvalType, userID, toEmail, clickUrl, lastApprover, nextApprover, rejectReason));

                    }
                    return Json(new { res = "Done" });
                }
                else
                {
                    return Json(new { res = "Done" });
                }
            }
            else if (claim != "" && claim.Contains("pvg"))
            {
                if (User != null && User.Identity.IsAuthenticated)
                {
                    int PVGCID = Convert.ToInt32(id);

                    var mstPVGClaim = await _repository.MstPVGClaim.GetPVGClaimByIdAsync(PVGCID);

                    if (mstPVGClaim == null)
                    {
                        // return NotFound();
                    }

                    if (Convert.ToInt32(approvedStatus) == 3 || Convert.ToInt32(approvedStatus) == 9 || Convert.ToInt32(approvedStatus) == 10)
                    {
                        await _repository.MstPVGClaim.UpdateMstPVGClaimStatus(PVGCID, -5, int.Parse(HttpContext.User.FindFirst("userid").Value), DateTime.Now, reason, string.Empty, string.Empty, string.Empty, string.Empty, isAlternateApprover,0);
                    }
                    else
                    {
                        await _repository.MstPVGClaim.UpdateMstPVGClaimStatus(PVGCID, 5, int.Parse(HttpContext.User.FindFirst("userid").Value), DateTime.Now, reason, string.Empty, string.Empty, string.Empty, string.Empty, isAlternateApprover,0);
                        string domainUrl = HttpContext.Request.Scheme + "://" + HttpContext.Request.Host;
                        string clickUrl = domainUrl + "/" + "PVGiroClaim/Details/" + PVGCID;

                        var mstSenderDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(HttpContext.User.FindFirst("userid").Value));
                        var senderName = mstSenderDetails.Name;
                        var mstVerifierDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(mstPVGClaim.UserID));
                        var toEmail = mstVerifierDetails.EmailAddress;
                        var receiverName = mstVerifierDetails.Name;
                        var claimNo = mstPVGClaim.PVGCNo;
                        var screen = "PV-GIRO Claim";
                        var approvalType = "Voided ";
                        int userID = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value);
                        var subject = "PV-GIRO Claim " + claimNo + " has been Voided ";

                        var rejectReason = reason;
                        var lastApprover = string.Empty;
                        var nextApprover = senderName;

                        BackgroundJob.Enqueue(() => _sendMailServices.SendEmail("Rejected.html", screen, subject, senderName, receiverName, claimNo, approvalType, userID, toEmail, clickUrl, lastApprover, nextApprover, rejectReason));

                    }
                    return Json(new { res = "Done" });
                }
                else
                {
                    return Json(new { res = "Done" });
                }
            }
            else
            {
                if (User != null && User.Identity.IsAuthenticated)
                {
                    int MCID = Convert.ToInt32(id);

                    var mstMileageClaim = await _repository.MstMileageClaim.GetMileageClaimByIdAsync(MCID);

                    if (mstMileageClaim == null)
                    {
                        // return NotFound();
                    }

                    if (Convert.ToInt32(approvedStatus) == 3 || Convert.ToInt32(approvedStatus) == 9 || Convert.ToInt32(approvedStatus) == 10)
                    {
                        await _repository.MstMileageClaim.UpdateMstMileageClaimStatus(MCID, -5, int.Parse(HttpContext.User.FindFirst("userid").Value), DateTime.Now, reason, string.Empty, string.Empty, string.Empty, string.Empty, isAlternateApprover, 0);
                    }
                    else
                    {
                        await _repository.MstMileageClaim.UpdateMstMileageClaimStatus(MCID, 5, int.Parse(HttpContext.User.FindFirst("userid").Value), DateTime.Now, reason, string.Empty, string.Empty, string.Empty, string.Empty, isAlternateApprover, 0);
                        string domainUrl = HttpContext.Request.Scheme + "://" + HttpContext.Request.Host;
                        string clickUrl = domainUrl + "/" + "MileageClaim/Details/" + MCID;

                        var mstSenderDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(HttpContext.User.FindFirst("userid").Value));
                        var senderName = mstSenderDetails.Name;
                        var mstVerifierDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(mstMileageClaim.UserID));
                        var toEmail = mstVerifierDetails.EmailAddress;
                        var receiverName = mstVerifierDetails.Name;
                        var claimNo = mstMileageClaim.MCNo;
                        var screen = "Mileage Claim";
                        var approvalType = "Voided ";
                        int userID = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value);
                        var subject = "Mileage Claim " + claimNo + " has been Voided ";

                        var rejectReason = reason;
                        var lastApprover = string.Empty;
                        var nextApprover = senderName;

                        BackgroundJob.Enqueue(() => _sendMailServices.SendEmail("Rejected.html", screen, subject, senderName, receiverName, claimNo, approvalType, userID, toEmail, clickUrl, lastApprover, nextApprover, rejectReason));

                    }
                    return Json(new { res = "Done" });
                }
                else
                {
                    return Json(new { res = "Done" });
                }
            }
        }

        public async Task<JsonResult> UpdateRejectedStatus(string id, string reason, string claim)
        {
            int loggedInUserId = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value);
            bool isAlternateApprover = false;
            var delegatedUserId = await _alternateApproverHelper.IsUserHasAnyAlternateApprovalSet(loggedInUserId);
            if (delegatedUserId.HasValue)
            {
                isAlternateApprover = true;
            }

            if (claim != "" && claim.Contains("ec"))
            {
                if (User != null && User.Identity.IsAuthenticated)
                {
                    int ECID = Convert.ToInt32(id);

                    var mstExpenseClaim = await _repository.MstExpenseClaim.GetExpenseClaimByIdAsync(ECID);

                    if (mstExpenseClaim == null)
                    {
                        // return NotFound();
                    }

                    await _repository.MstExpenseClaim.UpdateMstExpenseClaimStatus(ECID, 4, int.Parse(HttpContext.User.FindFirst("userid").Value), DateTime.Now, reason, string.Empty, string.Empty, string.Empty, string.Empty, isAlternateApprover, 0);
                    
                    string domainUrl = HttpContext.Request.Scheme + "://" + HttpContext.Request.Host;
                    string clickUrl = domainUrl + "/" + "ExpenseClaim/Details/" + ECID;

                    var mstSenderDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(HttpContext.User.FindFirst("userid").Value));
                    var senderName = mstSenderDetails.Name;
                    var mstVerifierDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(mstExpenseClaim.UserID));
                    var toEmail = mstVerifierDetails.EmailAddress;
                    var receiverName = mstVerifierDetails.Name;
                    var claimNo = mstExpenseClaim.ECNo;
                    var screen = "Expense Claim";
                    var approvalType = "Rejected Request";
                    int userID = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value);
                    var subject = "Expense Claim " + claimNo + " has been Rejected ";

                    var rejectReason = reason;
                    var lastApprover = string.Empty;
                    var nextApprover = senderName;

                    BackgroundJob.Enqueue(() => _sendMailServices.SendEmail("Rejected.html", screen, subject, senderName, receiverName, claimNo, approvalType, userID, toEmail, clickUrl, lastApprover, nextApprover, rejectReason));

                    return Json(new { res = "Done" });
                }
                else
                {
                    return Json(new { res = "Done" });
                }
            }
            else if (claim != "" && claim.Contains("tbc"))
            {
                if (User != null && User.Identity.IsAuthenticated)
                {
                    int TBCID = Convert.ToInt32(id);

                    var mstTBClaim = await _repository.MstTBClaim.GetTBClaimByIdAsync(TBCID);

                    if (mstTBClaim == null)
                    {
                        // return NotFound();
                    }

                    await _repository.MstTBClaim.UpdateMstTBClaimStatus(TBCID, 4, int.Parse(HttpContext.User.FindFirst("userid").Value), DateTime.Now, reason, string.Empty, string.Empty, string.Empty, string.Empty, isAlternateApprover, 0);
                    string domainUrl = HttpContext.Request.Scheme + "://" + HttpContext.Request.Host;
                    string clickUrl = domainUrl + "/" + "TelephoneBillClaim/Details/" + TBCID;

                    var mstSenderDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(HttpContext.User.FindFirst("userid").Value));
                    var senderName = mstSenderDetails.Name;
                    var mstVerifierDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(mstTBClaim.UserID));
                    var toEmail = mstVerifierDetails.EmailAddress;
                    var receiverName = mstVerifierDetails.Name;
                    var claimNo = mstTBClaim.TBCNo;
                    var screen = "Telephone Bill Claim";
                    var approvalType = "Rejected Request";
                    int userID = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value);
                    var subject = "Telephone Bill Claim " + claimNo + " has been Rejected ";

                    var rejectReason = reason;
                    var lastApprover = string.Empty;
                    var nextApprover = senderName;

                    BackgroundJob.Enqueue(() => _sendMailServices.SendEmail("Rejected.html", screen, subject, senderName, receiverName, claimNo, approvalType, userID, toEmail, clickUrl, lastApprover, nextApprover, rejectReason));

                    return Json(new { res = "Done" });
                }
                else
                {
                    return Json(new { res = "Done" });
                }
            }
            else if (claim != "" && claim.Contains("pvc"))
            {
                if (User != null && User.Identity.IsAuthenticated)
                {
                    int PVCCID = Convert.ToInt32(id);

                    var mstPVCClaim = await _repository.MstPVCClaim.GetPVCClaimByIdAsync(PVCCID);

                    if (mstPVCClaim == null)
                    {
                        // return NotFound();
                    }

                    await _repository.MstPVCClaim.UpdateMstPVCClaimStatus(PVCCID, 4, int.Parse(HttpContext.User.FindFirst("userid").Value), DateTime.Now, reason, string.Empty, string.Empty, string.Empty, string.Empty, isAlternateApprover,0);
                    string domainUrl = HttpContext.Request.Scheme + "://" + HttpContext.Request.Host;
                    string clickUrl = domainUrl + "/" + "PVChequeClaim/Details/" + PVCCID;

                    var mstSenderDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(HttpContext.User.FindFirst("userid").Value));
                    var senderName = mstSenderDetails.Name;
                    var mstVerifierDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(mstPVCClaim.UserID));
                    var toEmail = mstVerifierDetails.EmailAddress;
                    var receiverName = mstVerifierDetails.Name;
                    var claimNo = mstPVCClaim.PVCCNo;
                    var screen = "PV Cheque Claim";
                    var approvalType = "Rejected Request";
                    int userID = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value);
                    var subject = "PV Cheque Claim " + claimNo + " has been Rejected ";

                    var rejectReason = reason;
                    var lastApprover = string.Empty;
                    var nextApprover = senderName;

                    BackgroundJob.Enqueue(() => _sendMailServices.SendEmail("Rejected.html", screen, subject, senderName, receiverName, claimNo, approvalType, userID, toEmail, clickUrl, lastApprover, nextApprover, rejectReason));

                    return Json(new { res = "Done" });
                }
                else
                {
                    return Json(new { res = "Done" });
                }
            }
            else if (claim != "" && claim.Contains("pvg"))
            {
                if (User != null && User.Identity.IsAuthenticated)
                {
                    int PVGCID = Convert.ToInt32(id);

                    var mstPVGClaim = await _repository.MstPVGClaim.GetPVGClaimByIdAsync(PVGCID);

                    if (mstPVGClaim == null)
                    {
                        // return NotFound();
                    }

                    await _repository.MstPVGClaim.UpdateMstPVGClaimStatus(PVGCID, 4, int.Parse(HttpContext.User.FindFirst("userid").Value), DateTime.Now, reason, string.Empty, string.Empty, string.Empty, string.Empty, isAlternateApprover,0);
                    string domainUrl = HttpContext.Request.Scheme + "://" + HttpContext.Request.Host;
                    string clickUrl = domainUrl + "/" + "PVGiroClaim/Details/" + PVGCID;

                    var mstSenderDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(HttpContext.User.FindFirst("userid").Value));
                    var senderName = mstSenderDetails.Name;
                    var mstVerifierDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(mstPVGClaim.UserID));
                    var toEmail = mstVerifierDetails.EmailAddress;
                    var receiverName = mstVerifierDetails.Name;
                    var claimNo = mstPVGClaim.PVGCNo;
                    var screen = "PV-GIRO Claim";
                    var approvalType = "Rejected Request";
                    int userID = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value);
                    var subject = "PV-GIRO Claim " + claimNo + " has been Rejected ";

                    var rejectReason = reason;
                    var lastApprover = string.Empty;
                    var nextApprover = senderName;

                    BackgroundJob.Enqueue(() => _sendMailServices.SendEmail("Rejected.html", screen, subject, senderName, receiverName, claimNo, approvalType, userID, toEmail, clickUrl, lastApprover, nextApprover, rejectReason));

                    return Json(new { res = "Done" });
                }
                else
                {
                    return Json(new { res = "Done" });
                }
            }
            else
            {
                if (User != null && User.Identity.IsAuthenticated)
                {
                    int MCID = Convert.ToInt32(id);

                    var mstMileageClaim = await _repository.MstMileageClaim.GetMileageClaimByIdAsync(MCID);

                    if (mstMileageClaim == null)
                    {
                        // return NotFound();
                    }

                    await _repository.MstMileageClaim.UpdateMstMileageClaimStatus(MCID, 4, int.Parse(HttpContext.User.FindFirst("userid").Value), DateTime.Now, reason, string.Empty, string.Empty, string.Empty, string.Empty, isAlternateApprover, 0);
                    string domainUrl = HttpContext.Request.Scheme + "://" + HttpContext.Request.Host;
                    string clickUrl = domainUrl + "/" + "MileageClaim/Details/" + MCID;

                    var mstSenderDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(HttpContext.User.FindFirst("userid").Value));
                    var senderName = mstSenderDetails.Name;
                    var mstVerifierDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(mstMileageClaim.UserID));
                    var toEmail = mstVerifierDetails.EmailAddress;
                    var receiverName = mstVerifierDetails.Name;
                    var claimNo = mstMileageClaim.MCNo;
                    var screen = "Mileage Claim";
                    var approvalType = "Rejected Request";
                    int userID = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value);
                    var subject = "Mileage Claim " + claimNo + " has been Rejected ";

                    var rejectReason = reason;
                    var lastApprover = string.Empty;
                    var nextApprover = senderName;

                    BackgroundJob.Enqueue(() => _sendMailServices.SendEmail("Rejected.html", screen, subject, senderName, receiverName, claimNo, approvalType, userID, toEmail, clickUrl, lastApprover, nextApprover, rejectReason));

                    return Json(new { res = "Done" });
                }
                else
                {
                    return Json(new { res = "Done" });
                }
            }
        }

        public async Task<JsonResult> UpdateStatus(string id, string claim)
        {
            bool isAlternateApprover = false;
            if (User != null && User.Identity.IsAuthenticated)
            {
                if (claim != "" && claim.Contains("ec"))
                {
                    int ECID = Convert.ToInt32(id);

                    var mstExpenseClaim = await _repository.MstExpenseClaim.GetExpenseClaimByIdAsync(ECID);

                    if (mstExpenseClaim == null)
                    {
                        // return NotFound();
                    }

                    int ApprovedStatus = Convert.ToInt32(mstExpenseClaim.ApprovalStatus);
                    bool excute = _repository.MstExpenseClaim.ExistsApproval(ECID.ToString(), ApprovedStatus, HttpContext.User.FindFirst("userid").Value, "Expense");

                    // If execute is false, Check if the current user is alternate user for this claim
                    if (excute == false)
                    {
                        string hodapprover = _repository.MstExpenseClaim.GetApproval(ECID.ToString(), ApprovedStatus, HttpContext.User.FindFirst("userid").Value, "Expense");
                        int loggedInUserId = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value);
                        var delegatedUserId = await _alternateApproverHelper.IsUserHasAnyAlternateApprovalSet(loggedInUserId);
                        if (!string.IsNullOrEmpty(hodapprover))
                        {
                            if ((hodapprover == delegatedUserId.Value.ToString()))
                            {
                                excute = true;
                                isAlternateApprover = true;
                            }
                        }
                    }

                    if (excute == true)
                    {
                        #region Expense UserApprovers
                        if (ApprovedStatus == 6)
                        {
                            string VerifierIDs = "";
                            string ApproverIDs = "";
                            string UserApproverIDs = "";
                            string HODApproverID = "";
                            try
                            {
                                string[] ExpenseuserApproverIDs = mstExpenseClaim.UserApprovers.Split(',');
                                UserApproverIDs = string.Join(",", ExpenseuserApproverIDs.Skip(1));
                                string[] userApproverIDs = UserApproverIDs.ToString().Split(',');
                                ApproverIDs = mstExpenseClaim.Approver;
                                VerifierIDs = mstExpenseClaim.Verifier;
                                HODApproverID = mstExpenseClaim.HODApprover;
                                //Mail Code Implementation for Verifiers

                                foreach (string userApproverID in userApproverIDs)
                                {
                                    if (userApproverID != "")
                                    {
                                        string domainUrl = HttpContext.Request.Scheme + "://" + HttpContext.Request.Host;
                                        string clickUrl = domainUrl + "/" + "HodSummary/ECDetails/" + ECID;

                                        var mstSenderDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(HttpContext.User.FindFirst("userid").Value));
                                        var senderName = mstSenderDetails.Name;
                                        int? approverId = await _alternateApproverHelper.IsAlternateApprovalSetForUser(Convert.ToInt32(userApproverID));
                                        var mstVerifierDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(userApproverID));
                                        bool isAlternateApproverSet = false;
                                        if (approverId.HasValue)
                                        {
                                            mstVerifierDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(approverId.Value));
                                            // Alternate approver is configured for the current user. So, do not show actions
                                            isAlternateApproverSet = true;
                                        }
                                        var toEmail = mstVerifierDetails.EmailAddress;
                                        var receiverName = mstVerifierDetails.Name;
                                        var claimNo = mstExpenseClaim.ECNo;
                                        var screen = "Expense Claim";
                                        var approvalType = "Approval Request";
                                        int userID = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value);
                                        var subject = "Expense Claim for Approval " + claimNo;

                                        BackgroundJob.Enqueue(() => _sendMailServices.SendEmail("EmailTemplate.html",screen, subject, senderName, receiverName, claimNo, approvalType, userID, toEmail, clickUrl));

                                    }
                                    else
                                    {
                                        string domainUrl = HttpContext.Request.Scheme + "://" + HttpContext.Request.Host;
                                        string clickUrl = domainUrl + "/" + "FinanceExpenseClaim/Details/" + ECID;

                                        var mstSenderDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(HttpContext.User.FindFirst("userid").Value));
                                        var senderName = mstSenderDetails.Name;
                                        int? approverId = await _alternateApproverHelper.IsAlternateApprovalSetForUser(Convert.ToInt32(VerifierIDs.ToString().Split(',')[0].ToString()));
                                        var mstVerifierDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(VerifierIDs.ToString().Split(',')[0].ToString()));
                                        bool isAlternateApproverSet = false;
                                        if (approverId.HasValue)
                                        {
                                            mstVerifierDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(approverId.Value));
                                            // Alternate approver is configured for the current user. So, do not show actions
                                            isAlternateApproverSet = true;
                                        }
                                        var toEmail = mstVerifierDetails.EmailAddress;
                                        var receiverName = mstVerifierDetails.Name;
                                        var claimNo = mstExpenseClaim.ECNo;
                                        var screen = "Expense Claim";
                                        var approvalType = "Verification Request";
                                        int userID = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value);
                                        var subject = "Expense Claim for Verification " + claimNo;

                                        BackgroundJob.Enqueue(() => _sendMailServices.SendEmail("EmailTemplate.html",screen, subject, senderName, receiverName, claimNo, approvalType, userID, toEmail, clickUrl));

                                    }

                                    break;
                                }
                            }
                            catch
                            {
                            }
                            await _repository.MstExpenseClaim.UpdateMstExpenseClaimStatus(ECID, 1, int.Parse(HttpContext.User.FindFirst("userid").Value), DateTime.Now, string.Empty, VerifierIDs.ToString(), ApproverIDs.ToString(), UserApproverIDs.ToString(), HODApproverID.ToString(), isAlternateApprover, 0);

                        }
                        #endregion

                        #region Expense Verifier
                        if (ApprovedStatus == 1)
                        {
                            string VerifierIDs = "";
                            string ApproverIDs = "";
                            string UserApproverIDs = "";
                            string HODApproverID = "";
                            try
                            {
                                string[] ExpenseverifierIDs = mstExpenseClaim.Verifier.Split(',');
                                VerifierIDs = string.Join(",", ExpenseverifierIDs.Skip(1));
                                string[] verifierIDs = VerifierIDs.ToString().Split(',');
                                ApproverIDs = mstExpenseClaim.Approver;
                                UserApproverIDs = mstExpenseClaim.UserApprovers;
                                HODApproverID = mstExpenseClaim.HODApprover;

                                //Mail Code Implementation for Verifiers
                                /*
                                foreach (string verifierID in verifierIDs)
                                {
                                    if (verifierID != "")
                                    {

                                    }
                                    else
                                    {

                                    }

                                    break;
                                }
                                */
                            }
                            catch
                            {
                            }
                            await _repository.MstExpenseClaim.UpdateMstExpenseClaimStatus(ECID, 2, int.Parse(HttpContext.User.FindFirst("userid").Value), DateTime.Now, string.Empty, VerifierIDs.ToString(), ApproverIDs.ToString(), UserApproverIDs.ToString(), HODApproverID.ToString(), isAlternateApprover, 0);

                        }
                        #endregion

                        #region Expense HODApprovers
                        if (ApprovedStatus == 7)
                        {
                            string VerifierIDs = "";
                            string ApproverIDs = "";
                            string UserApproverIDs = "";
                            string HODApproverIDs = "";
                            try
                            {
                                string[] ExpensehodApproverIDs = mstExpenseClaim.HODApprover.Split(',');
                                HODApproverIDs = string.Join(",", ExpensehodApproverIDs.Skip(1));
                                string[] hODApproverIDs = HODApproverIDs.ToString().Split(',');
                                ApproverIDs = mstExpenseClaim.Approver;
                                //VerifierIDs = mstExpenseClaim.Verifier;
                                //HODApproverID = mstExpenseClaim.HODApprover;
                                //Mail Code Implementation for Verifiers
                                foreach (string hODApproverID in hODApproverIDs)
                                {
                                    if (hODApproverID != "")
                                    {

                                    }
                                    else
                                    {
                                        string domainUrl = HttpContext.Request.Scheme + "://" + HttpContext.Request.Host;
                                        string clickUrl = domainUrl + "/" + "FinanceExpenseClaim/Details/" + ECID;

                                        var mstSenderDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(HttpContext.User.FindFirst("userid").Value));
                                        var senderName = mstSenderDetails.Name;
                                        var mstVerifierDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(ApproverIDs.ToString().Split(',')[0].ToString()));
                                        var toEmail = mstVerifierDetails.EmailAddress;
                                        var receiverName = mstVerifierDetails.Name;
                                        var claimNo = mstExpenseClaim.ECNo;
                                        var screen = "Expense Claim";
                                        var approvalType = "Approval Request";
                                        int userID = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value);
                                        var subject = "Expense Claim for Approval " + claimNo;

                                        BackgroundJob.Enqueue(() => _sendMailServices.SendEmail("EmailTemplate.html",screen, subject, senderName, receiverName, claimNo, approvalType, userID, toEmail, clickUrl));
                                    }

                                    break;
                                }
                            }
                            catch
                            {
                            }
                            await _repository.MstExpenseClaim.UpdateMstExpenseClaimStatus(ECID, 2, int.Parse(HttpContext.User.FindFirst("userid").Value), DateTime.Now, string.Empty, VerifierIDs.ToString(), ApproverIDs.ToString(), UserApproverIDs.ToString(), HODApproverIDs.ToString(), isAlternateApprover, 0);

                        }
                        #endregion

                        #region Expense Approver
                        else if (ApprovedStatus == 2)
                        {
                            string VerifierIDs = "";
                            string ApproverIDs = "";
                            string UserApproverIDs = "";
                            string HODApproverID = "";
                            try
                            {
                                string[] ExpenseapproverIDs = mstExpenseClaim.Approver.Split(',');
                                ApproverIDs = string.Join(",", ExpenseapproverIDs.Skip(1));
                                string[] approverIDs = ApproverIDs.Split(',');
                                int CreatedBy = Convert.ToInt32(mstExpenseClaim.CreatedBy);

                                //Mail Code Implementation for Approvers
                                /*
                                foreach (string approverID in approverIDs)
                                {
                                    if (approverID != "")
                                    {

                                    }
                                    else
                                    {

                                    }

                                    break;
                                }
                                */
                            }
                            catch
                            {
                            }
                            string financeStartDay = _configuration.GetValue<string>("FinanceStartDay");
                            await _repository.MstExpenseClaim.UpdateMstExpenseClaimStatus(ECID, 3, int.Parse(HttpContext.User.FindFirst("userid").Value), DateTime.Now, string.Empty, VerifierIDs, ApproverIDs, UserApproverIDs.ToString(), HODApproverID.ToString(), isAlternateApprover, int.Parse(financeStartDay));
                        }

                        #endregion

                        return Json(new { res = "Done" });
                    }
                    else
                    {
                        //TempData["Status_Invocie"] = "Approval";
                        return Json(new { res = "0" });
                    }
                }
                else if (claim != "" && claim.Contains("tbc"))
                {
                    int TBCID = Convert.ToInt32(id);

                    var mstTBClaim = await _repository.MstTBClaim.GetTBClaimByIdAsync(TBCID);

                    if (mstTBClaim == null)
                    {
                        // return NotFound();
                    }

                    int ApprovedStatus = Convert.ToInt32(mstTBClaim.ApprovalStatus);
                    bool excute = _repository.MstTBClaim.ExistsApproval(TBCID.ToString(), ApprovedStatus, HttpContext.User.FindFirst("userid").Value, "TelephoneBill");

                    // If execute is false, Check if the current user is alternate user for this claim
                    if (excute == false)
                    {
                        string hodapprover = _repository.MstTBClaim.GetApproverVerifier(TBCID.ToString(), ApprovedStatus, HttpContext.User.FindFirst("userid").Value, "TelephoneBill");
                        int loggedInUserId = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value);
                        var delegatedUserId = await _alternateApproverHelper.IsUserHasAnyAlternateApprovalSet(loggedInUserId);
                        if (delegatedUserId != null && (hodapprover == delegatedUserId.Value.ToString()))
                        {
                            excute = true;
                            isAlternateApprover = true;
                        }
                    }

                    if (excute == true)
                    {
                        #region TB UserApprovers
                        if (ApprovedStatus == 6)
                        {
                            string VerifierIDs = "";
                            string ApproverIDs = "";
                            string UserApproverIDs = "";
                            string HODApproverID = "";
                            try
                            {
                                string[] TBuserApproverIDs = mstTBClaim.UserApprovers.Split(',');
                                UserApproverIDs = string.Join(",", TBuserApproverIDs.Skip(1));
                                string[] userApproverIDs = UserApproverIDs.ToString().Split(',');
                                ApproverIDs = mstTBClaim.Approver;
                                VerifierIDs = mstTBClaim.Verifier;
                                HODApproverID = mstTBClaim.HODApprover;
                                //Mail Code Implementation for Verifiers
                                foreach (string userApproverID in userApproverIDs)
                                {
                                    if (userApproverID != "")
                                    {
                                        string domainUrl = HttpContext.Request.Scheme + "://" + HttpContext.Request.Host;
                                        string clickUrl = domainUrl + "/" + "HodSummary/TBCDetails/" + TBCID;

                                        var mstSenderDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(HttpContext.User.FindFirst("userid").Value));
                                        var senderName = mstSenderDetails.Name;
                                        int? approverId = await _alternateApproverHelper.IsAlternateApprovalSetForUser(Convert.ToInt32(userApproverID));
                                        var mstVerifierDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(userApproverID));
                                        bool isAlternateApproverSet = false;
                                        if (approverId.HasValue)
                                        {
                                            mstVerifierDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(approverId.Value));
                                            // Alternate approver is configured for the current user. So, do not show actions
                                            isAlternateApproverSet = true;
                                        }
                                        var toEmail = mstVerifierDetails.EmailAddress;
                                        var receiverName = mstVerifierDetails.Name;
                                        var claimNo = mstTBClaim.TBCNo;
                                        var screen = "Telephone Bill Claim";
                                        var approvalType = "Approval Request";
                                        int userID = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value);
                                        var subject = "Telephone Bill Claim for Approval " + claimNo;

                                        BackgroundJob.Enqueue(() => _sendMailServices.SendEmail("EmailTemplate.html",screen, subject, senderName, receiverName, claimNo, approvalType, userID, toEmail, clickUrl));

                                    }
                                    else
                                    {
                                        string domainUrl = HttpContext.Request.Scheme + "://" + HttpContext.Request.Host;
                                        string clickUrl = domainUrl + "/" + "FinanceTBClaim/Details/" + TBCID;

                                        var mstSenderDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(HttpContext.User.FindFirst("userid").Value));
                                        var senderName = mstSenderDetails.Name;
                                        int? approverId = await _alternateApproverHelper.IsAlternateApprovalSetForUser(Convert.ToInt32(VerifierIDs.ToString().Split(',')[0].ToString()));
                                        var mstVerifierDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(VerifierIDs.ToString().Split(',')[0].ToString()));
                                        bool isAlternateApproverSet = false;
                                        if (approverId.HasValue)
                                        {
                                            mstVerifierDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(approverId.Value));
                                            // Alternate approver is configured for the current user. So, do not show actions
                                            isAlternateApproverSet = true;
                                        }
                                        var toEmail = mstVerifierDetails.EmailAddress;
                                        var receiverName = mstVerifierDetails.Name;
                                        var claimNo = mstTBClaim.TBCNo;
                                        var screen = "Telephone Bill Claim";
                                        var approvalType = "Verification Request";
                                        int userID = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value);
                                        var subject = "Telephone Bill Claim for Verification " + claimNo;

                                        BackgroundJob.Enqueue(() => _sendMailServices.SendEmail("EmailTemplate.html",screen, subject, senderName, receiverName, claimNo, approvalType, userID, toEmail, clickUrl));

                                    }

                                    break;
                                }
                            }
                            catch
                            {
                            }
                            await _repository.MstTBClaim.UpdateMstTBClaimStatus(TBCID, 1, int.Parse(HttpContext.User.FindFirst("userid").Value), DateTime.Now, string.Empty, VerifierIDs.ToString(), ApproverIDs.ToString(), UserApproverIDs.ToString(), HODApproverID.ToString(), isAlternateApprover, 0);

                        }
                        #endregion

                        #region TB Verifier
                        if (ApprovedStatus == 1)
                        {
                            string VerifierIDs = "";
                            string ApproverIDs = "";
                            string UserApproverIDs = "";
                            string HODApproverID = "";
                            try
                            {
                                string[] TBverifierIDs = mstTBClaim.Verifier.Split(',');
                                VerifierIDs = string.Join(",", TBverifierIDs.Skip(1));
                                string[] verifierIDs = VerifierIDs.ToString().Split(',');
                                ApproverIDs = mstTBClaim.Approver;
                                UserApproverIDs = mstTBClaim.UserApprovers;
                                HODApproverID = mstTBClaim.HODApprover;

                                //Mail Code Implementation for Verifiers
                                /*
                                foreach (string verifierID in verifierIDs)
                                {
                                    if (verifierID != "")
                                    {

                                    }
                                    else
                                    {

                                    }

                                    break;
                                }
                                */
                            }
                            catch
                            {
                            }
                            await _repository.MstTBClaim.UpdateMstTBClaimStatus(TBCID, 2, int.Parse(HttpContext.User.FindFirst("userid").Value), DateTime.Now, string.Empty, VerifierIDs.ToString(), ApproverIDs.ToString(), UserApproverIDs.ToString(), HODApproverID.ToString(), isAlternateApprover, 0);

                        }
                        #endregion

                        #region TB HODApprovers
                        if (ApprovedStatus == 7)
                        {
                            string VerifierIDs = "";
                            string ApproverIDs = "";
                            string UserApproverIDs = "";
                            string HODApproverID = "";
                            try
                            {
                                string[] TBhodApproverIDs = mstTBClaim.HODApprover.Split(',');
                                HODApproverID = string.Join(",", TBhodApproverIDs.Skip(1));
                                string[] hODApproverIDs = HODApproverID.ToString().Split(',');
                                ApproverIDs = mstTBClaim.Approver;
                                //VerifierIDs = mstTBClaim.Verifier;
                                //HODApproverID = mstTBClaim.HODApprover;
                                //Mail Code Implementation for Verifiers
                                foreach (string hODApproverID in hODApproverIDs)
                                {
                                    if (hODApproverID != "")
                                    {

                                    }
                                    else
                                    {
                                        string domainUrl = HttpContext.Request.Scheme + "://" + HttpContext.Request.Host;
                                        string clickUrl = domainUrl + "/" + "FinanceTBClaim/Details/" + TBCID;

                                        var mstSenderDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(HttpContext.User.FindFirst("userid").Value));
                                        var senderName = mstSenderDetails.Name;
                                        var mstVerifierDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(ApproverIDs.ToString().Split(',')[0].ToString()));
                                        var toEmail = mstVerifierDetails.EmailAddress;
                                        var receiverName = mstVerifierDetails.Name;
                                        var claimNo = mstTBClaim.TBCNo;
                                        var screen = "Telephone Bill Claim";
                                        var approvalType = "Approval Request";
                                        int userID = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value);
                                        var subject = "Telephone Bill Claim for Approval " + claimNo;

                                        BackgroundJob.Enqueue(() => _sendMailServices.SendEmail("EmailTemplate.html",screen, subject, senderName, receiverName, claimNo, approvalType, userID, toEmail, clickUrl));
                                    }

                                    break;
                                }
                            }
                            catch
                            {
                            }
                            await _repository.MstTBClaim.UpdateMstTBClaimStatus(TBCID, 2, int.Parse(HttpContext.User.FindFirst("userid").Value), DateTime.Now, string.Empty, VerifierIDs.ToString(), ApproverIDs.ToString(), UserApproverIDs.ToString(), HODApproverID.ToString(), isAlternateApprover, 0);

                        }
                        #endregion

                        #region TB Approver
                        else if (ApprovedStatus == 2)
                        {
                            string VerifierIDs = "";
                            string ApproverIDs = "";
                            string UserApproverIDs = "";
                            string HODApproverID = "";
                            try
                            {
                                string[] TBapproverIDs = mstTBClaim.Approver.Split(',');
                                ApproverIDs = string.Join(",", TBapproverIDs.Skip(1));
                                string[] approverIDs = ApproverIDs.Split(',');
                                int CreatedBy = Convert.ToInt32(mstTBClaim.CreatedBy);

                                //Mail Code Implementation for Approvers
                                /*
                                foreach (string approverID in approverIDs)
                                {
                                    if (approverID != "")
                                    {

                                    }
                                    else
                                    {

                                    }

                                    break;
                                }
                                */
                            }
                            catch
                            {
                            }
                            string financeStartDay = _configuration.GetValue<string>("FinanceStartDay");
                            await _repository.MstTBClaim.UpdateMstTBClaimStatus(TBCID, 3, int.Parse(HttpContext.User.FindFirst("userid").Value), DateTime.Now, string.Empty, VerifierIDs, ApproverIDs, UserApproverIDs.ToString(), HODApproverID.ToString(), isAlternateApprover, int.Parse(financeStartDay));
                        }

                        #endregion

                        return Json(new { res = "Done" });
                    }
                    else
                    {
                        //TempData["Status_Invocie"] = "Approval";
                        return Json(new { res = "0" });
                    }
                }
                else if (claim != "" && claim.Contains("pvc"))
                {
                    int PVCCID = Convert.ToInt32(id);

                    var mstPVCClaim = await _repository.MstPVCClaim.GetPVCClaimByIdAsync(PVCCID);

                    if (mstPVCClaim == null)
                    {
                        // return NotFound();
                    }


                    int ApprovedStatus = Convert.ToInt32(mstPVCClaim.ApprovalStatus);
                    bool excute = _repository.MstPVCClaim.ExistsApproval(PVCCID.ToString(), ApprovedStatus, HttpContext.User.FindFirst("userid").Value, "PVC");

                    // If execute is false, Check if the current user is alternate user for this claim
                    if (excute == false)
                    {
                        string hodapprover = _repository.MstPVCClaim.GetApproval(PVCCID.ToString(), ApprovedStatus, HttpContext.User.FindFirst("userid").Value, "PVC");
                        int loggedInUserId = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value);
                        var delegatedUserId = await _alternateApproverHelper.IsUserHasAnyAlternateApprovalSet(loggedInUserId);
                        if (delegatedUserId != null && (hodapprover == delegatedUserId.Value.ToString()))
                        {
                            excute = true;
                            isAlternateApprover = true;
                        }
                    }

                    if (excute == true)
                    {
                        #region PVC UserApprovers
                        if (ApprovedStatus == 6)
                        {
                            string VerifierIDs = "";
                            string ApproverIDs = "";
                            string UserApproverIDs = "";
                            string HODApproverID = "";
                            try
                            {
                                string[] MileageuserApproverIDs = mstPVCClaim.UserApprovers.Split(',');
                                UserApproverIDs = string.Join(",", MileageuserApproverIDs.Skip(1));
                                string[] userApproverIDs = UserApproverIDs.ToString().Split(',');
                                ApproverIDs = mstPVCClaim.Approver;
                                VerifierIDs = mstPVCClaim.Verifier;
                                HODApproverID = mstPVCClaim.HODApprover;
                                //Mail Code Implementation for Verifiers
                                foreach (string userApproverID in userApproverIDs)
                                {
                                    if (userApproverID != "")
                                    {
                                        string domainUrl = HttpContext.Request.Scheme + "://" + HttpContext.Request.Host;
                                        string clickUrl = domainUrl + "/" + "HodSummary/PVCCDetails/" + PVCCID;

                                        var mstSenderDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(HttpContext.User.FindFirst("userid").Value));
                                        var senderName = mstSenderDetails.Name;
                                        int? approverId = await _alternateApproverHelper.IsAlternateApprovalSetForUser(Convert.ToInt32(userApproverID));
                                        var mstVerifierDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(userApproverID));
                                        bool isAlternateApproverSet = false;
                                        if (approverId.HasValue)
                                        {
                                            mstVerifierDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(approverId.Value));
                                            // Alternate approver is configured for the current user. So, do not show actions
                                            isAlternateApproverSet = true;
                                        }
                                        var toEmail = mstVerifierDetails.EmailAddress;
                                        var receiverName = mstVerifierDetails.Name;
                                        var claimNo = mstPVCClaim.PVCCNo;
                                        var screen = "PV-Cheque Claim";
                                        var approvalType = "Approval Request";
                                        int userID = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value);
                                        var subject = "PV-Cheque Claim for Approval " + claimNo;

                                        BackgroundJob.Enqueue(() => _sendMailServices.SendEmail("EmailTemplate.html",screen, subject, senderName, receiverName, claimNo, approvalType, userID, toEmail, clickUrl));

                                    }
                                    else
                                    {
                                        string domainUrl = HttpContext.Request.Scheme + "://" + HttpContext.Request.Host;
                                        string clickUrl = domainUrl + "/" + "HodSummary/PVCCDetails/" + PVCCID ;

                                        var mstSenderDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(HttpContext.User.FindFirst("userid").Value));
                                        var senderName = mstSenderDetails.Name;
                                        int? approverId = await _alternateApproverHelper.IsAlternateApprovalSetForUser(Convert.ToInt32(HODApproverID.ToString().Split(',')[0].ToString()));
                                        var mstVerifierDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(HODApproverID.ToString().Split(',')[0].ToString()));
                                        bool isAlternateApproverSet = false;
                                        if (approverId.HasValue)
                                        {
                                            mstVerifierDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(approverId.Value));
                                            // Alternate approver is configured for the current user. So, do not show actions
                                            isAlternateApproverSet = true;
                                        }
                                        var toEmail = mstVerifierDetails.EmailAddress;
                                        var receiverName = mstVerifierDetails.Name;
                                        var claimNo = mstPVCClaim.PVCCNo;
                                        var screen = "PV-Cheque Claim";
                                        var approvalType = "Approval Request";
                                        int userID = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value);
                                        var subject = "PV-Cheque Claim for Approval " + claimNo;

                                        BackgroundJob.Enqueue(() => _sendMailServices.SendEmail("EmailTemplate.html",screen, subject, senderName, receiverName, claimNo, approvalType, userID, toEmail, clickUrl));

                                    }

                                    break;
                                }
                            }
                            catch
                            {
                            }
                            await _repository.MstPVCClaim.UpdateMstPVCClaimStatus(PVCCID, 7, int.Parse(HttpContext.User.FindFirst("userid").Value), DateTime.Now, string.Empty, VerifierIDs.ToString(), ApproverIDs.ToString(), UserApproverIDs.ToString(), HODApproverID.ToString(), isAlternateApprover,0);

                        }
                        #endregion


                        #region PVC Verifier
                        if (ApprovedStatus == 1)
                        {
                            string VerifierIDs = "";
                            string ApproverIDs = "";
                            string UserApproverIDs = "";
                            string HODApproverID = "";
                            try
                            {
                                string[] PVCverifierIDs = mstPVCClaim.Verifier.Split(',');
                                VerifierIDs = string.Join(",", PVCverifierIDs.Skip(1));
                                string[] verifierIDs = VerifierIDs.ToString().Split(',');
                                ApproverIDs = mstPVCClaim.Approver;
                                UserApproverIDs = mstPVCClaim.UserApprovers;
                                HODApproverID = mstPVCClaim.HODApprover;
                                //Mail Code Implementation for Verifiers
                                /*
                                foreach (string verifierID in verifierIDs)
                                {
                                    if (verifierID != "")
                                    {

                                    }
                                    else
                                    {

                                    }

                                    break;
                                }
                                */
                            }
                            catch
                            {
                            }
                            await _repository.MstPVCClaim.UpdateMstPVCClaimStatus(PVCCID, 2, int.Parse(HttpContext.User.FindFirst("userid").Value), DateTime.Now, string.Empty, VerifierIDs.ToString(), ApproverIDs.ToString(), UserApproverIDs.ToString(), HODApproverID.ToString(), false,0);

                        }
                        #endregion

                        #region PVC HODApprovers
                        if (ApprovedStatus == 7)
                        {
                            string VerifierIDs = "";
                            string ApproverIDs = "";
                            string UserApproverIDs = "";
                            string HODApproverID = "";
                            try
                            {
                                string[] ExpensehodApproverIDs = mstPVCClaim.HODApprover.Split(',');
                                HODApproverID = string.Join(",", ExpensehodApproverIDs.Skip(1));
                                string[] hODApproverIDs = HODApproverID.ToString().Split(',');
                                ApproverIDs = mstPVCClaim.Approver;
                                VerifierIDs = mstPVCClaim.Verifier;
                                //HODApproverID = mstExpenseClaim.HODApprover;
                                //Mail Code Implementation for Verifiers
                                foreach (string hODApproverID in hODApproverIDs)
                                {
                                    if (hODApproverID != "")
                                    {

                                    }
                                    else
                                    {
                                        string domainUrl = HttpContext.Request.Scheme + "://" + HttpContext.Request.Host;
                                        string clickUrl = domainUrl + "/" + "FinancePVCClaim/Details/" + PVCCID;

                                        var mstSenderDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(HttpContext.User.FindFirst("userid").Value));
                                        var senderName = mstSenderDetails.Name;
                                        var mstVerifierDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(VerifierIDs.ToString().Split(',')[0].ToString()));
                                        var toEmail = mstVerifierDetails.EmailAddress;
                                        var receiverName = mstVerifierDetails.Name;
                                        var claimNo = mstPVCClaim.PVCCNo;
                                        var screen = "PV-Cheque Claim";
                                        var approvalType = "Verification Request";
                                        int userID = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value);
                                        var subject = "PV-Cheque Claim for Verification " + claimNo;

                                        BackgroundJob.Enqueue(() => _sendMailServices.SendEmail("EmailTemplate.html",screen, subject, senderName, receiverName, claimNo, approvalType, userID, toEmail, clickUrl));
                                    }

                                    break;
                                }
                            }
                            catch
                            {
                            }
                            await _repository.MstPVCClaim.UpdateMstPVCClaimStatus(PVCCID, 1, int.Parse(HttpContext.User.FindFirst("userid").Value), DateTime.Now, string.Empty, VerifierIDs.ToString(), ApproverIDs.ToString(), UserApproverIDs.ToString(), HODApproverID.ToString(), isAlternateApprover,0);

                        }
                        #endregion

                        #region PVC Approver
                        else if (ApprovedStatus == 2)
                        {
                            string VerifierIDs = "";
                            string ApproverIDs = "";
                            string UserApproverIDs = "";
                            string HODApproverID = "";
                            try
                            {
                                string[] PVCapproverIDs = mstPVCClaim.Approver.Split(',');
                                ApproverIDs = string.Join(",", PVCapproverIDs.Skip(1));
                                string[] approverIDs = ApproverIDs.Split(',');
                                int CreatedBy = Convert.ToInt32(mstPVCClaim.CreatedBy);

                                //Mail Code Implementation for Approvers
                                /*
                                foreach (string approverID in approverIDs)
                                {
                                    if (approverID != "")
                                    {

                                    }
                                    else
                                    {

                                    }

                                    break;
                                }
                                */
                            }
                            catch
                            {
                            }
                            string financeStartDay = _configuration.GetValue<string>("FinanceStartDay");
                            await _repository.MstPVCClaim.UpdateMstPVCClaimStatus(PVCCID, 3, int.Parse(HttpContext.User.FindFirst("userid").Value), DateTime.Now, string.Empty, VerifierIDs, ApproverIDs, UserApproverIDs.ToString(), HODApproverID.ToString(), false, int.Parse(financeStartDay));
                        }

                        #endregion

                        return Json(new { res = "Done" });
                    }
                    else
                    {
                        //TempData["Status_Invocie"] = "Approval";
                        return Json(new { res = "0" });
                    }
                }
                else if (claim != "" && claim.Contains("pvg"))
                {
                    int PVGCID = Convert.ToInt32(id);

                    var mstPVGClaim = await _repository.MstPVGClaim.GetPVGClaimByIdAsync(PVGCID);

                    if (mstPVGClaim == null)
                    {
                        // return NotFound();
                    }


                    int ApprovedStatus = Convert.ToInt32(mstPVGClaim.ApprovalStatus);
                    bool excute = _repository.MstPVGClaim.ExistsApproval(PVGCID.ToString(), ApprovedStatus, HttpContext.User.FindFirst("userid").Value, "PVG");

                    // If execute is false, Check if the current user is alternate user for this claim
                    if (excute == false)
                    {
                        string hodapprover = _repository.MstPVGClaim.GetApproval(PVGCID.ToString(), ApprovedStatus, HttpContext.User.FindFirst("userid").Value, "PVG");
                        int loggedInUserId = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value);
                        var delegatedUserId = await _alternateApproverHelper.IsUserHasAnyAlternateApprovalSet(loggedInUserId);
                        if (delegatedUserId != null && (hodapprover == delegatedUserId.Value.ToString()))
                        {
                            excute = true;
                            isAlternateApprover = true;
                        }
                    }

                    if (excute == true)
                    {
                        #region PVG UserApprovers
                        if (ApprovedStatus == 6)
                        {
                            string VerifierIDs = "";
                            string ApproverIDs = "";
                            string UserApproverIDs = "";
                            string HODApproverID = "";
                            try
                            {
                                string[] MileageuserApproverIDs = mstPVGClaim.UserApprovers.Split(',');
                                UserApproverIDs = string.Join(",", MileageuserApproverIDs.Skip(1));
                                string[] userApproverIDs = UserApproverIDs.ToString().Split(',');
                                ApproverIDs = mstPVGClaim.Approver;
                                VerifierIDs = mstPVGClaim.Verifier;
                                HODApproverID = mstPVGClaim.HODApprover;
                                //Mail Code Implementation for Verifiers
                                foreach (string userApproverID in userApproverIDs)
                                {
                                    if (userApproverID != "")
                                    {
                                        string domainUrl = HttpContext.Request.Scheme + "://" + HttpContext.Request.Host;
                                        string clickUrl = domainUrl + "/" + "HodSummary/PVGCDetails/" + PVGCID;

                                        var mstSenderDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(HttpContext.User.FindFirst("userid").Value));
                                        var senderName = mstSenderDetails.Name;
                                        int? approverId = await _alternateApproverHelper.IsAlternateApprovalSetForUser(Convert.ToInt32(userApproverID));
                                        var mstVerifierDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(userApproverID));
                                        bool isAlternateApproverSet = false;
                                        if (approverId.HasValue)
                                        {
                                            mstVerifierDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(approverId.Value));
                                            // Alternate approver is configured for the current user. So, do not show actions
                                            isAlternateApproverSet = true;
                                        }
                                        var toEmail = mstVerifierDetails.EmailAddress;
                                        var receiverName = mstVerifierDetails.Name;
                                        var claimNo = mstPVGClaim.PVGCNo;
                                        var screen = "PV-GIRO Claim";
                                        var approvalType = "Approval Request";
                                        int userID = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value);
                                        var subject = "PV-GIRO Claim for Approval " + claimNo;

                                        BackgroundJob.Enqueue(() => _sendMailServices.SendEmail("EmailTemplate.html",screen, subject, senderName, receiverName, claimNo, approvalType, userID, toEmail, clickUrl));

                                    }
                                    else
                                    {
                                        string domainUrl = HttpContext.Request.Scheme + "://" + HttpContext.Request.Host;
                                        string clickUrl = domainUrl + "/" + "HodSummary/PVGCDetails/" + PVGCID;

                                        var mstSenderDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(HttpContext.User.FindFirst("userid").Value));
                                        var senderName = mstSenderDetails.Name;
                                        int? approverId = await _alternateApproverHelper.IsAlternateApprovalSetForUser(Convert.ToInt32(HODApproverID.ToString().Split(',')[0].ToString()));
                                        var mstVerifierDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(HODApproverID.ToString().Split(',')[0].ToString()));
                                        bool isAlternateApproverSet = false;
                                        if (approverId.HasValue)
                                        {
                                            mstVerifierDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(approverId.Value));
                                            // Alternate approver is configured for the current user. So, do not show actions
                                            isAlternateApproverSet = true;
                                        }
                                        var toEmail = mstVerifierDetails.EmailAddress;
                                        var receiverName = mstVerifierDetails.Name;
                                        var claimNo = mstPVGClaim.PVGCNo;
                                        var screen = "PV-GIRO Claim";
                                        var approvalType = "Approval Request";
                                        int userID = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value);
                                        var subject = "PV-GIRO Claim for Approval " + claimNo;

                                        BackgroundJob.Enqueue(() => _sendMailServices.SendEmail("EmailTemplate.html",screen, subject, senderName, receiverName, claimNo, approvalType, userID, toEmail, clickUrl));

                                    }

                                    break;
                                }
                            }
                            catch
                            {
                            }
                            await _repository.MstPVGClaim.UpdateMstPVGClaimStatus(PVGCID, 7, int.Parse(HttpContext.User.FindFirst("userid").Value), DateTime.Now, string.Empty, VerifierIDs.ToString(), ApproverIDs.ToString(), UserApproverIDs.ToString(), HODApproverID.ToString(), isAlternateApprover,0);

                        }
                        #endregion
                        #region PVG Verifier
                        if (ApprovedStatus == 1)
                        {
                            string VerifierIDs = "";
                            string ApproverIDs = "";
                            string UserApproverIDs = "";
                            string HODApproverID = "";
                            try
                            {
                                string[] PVGverifierIDs = mstPVGClaim.Verifier.Split(',');
                                VerifierIDs = string.Join(",", PVGverifierIDs.Skip(1));
                                string[] verifierIDs = VerifierIDs.ToString().Split(',');
                                ApproverIDs = mstPVGClaim.Approver;
                                UserApproverIDs = mstPVGClaim.UserApprovers;
                                HODApproverID = mstPVGClaim.HODApprover;
                                //Mail Code Implementation for Verifiers
                                /*
                                foreach (string verifierID in verifierIDs)
                                {
                                    if (verifierID != "")
                                    {

                                    }
                                    else
                                    {

                                    }

                                    break;
                                }
                                */
                            }
                            catch
                            {
                            }
                            await _repository.MstPVGClaim.UpdateMstPVGClaimStatus(PVGCID, 2, int.Parse(HttpContext.User.FindFirst("userid").Value), DateTime.Now, string.Empty, VerifierIDs.ToString(), ApproverIDs.ToString(), UserApproverIDs.ToString(), HODApproverID.ToString(), isAlternateApprover,0);

                        }
                        #endregion

                        #region PVG HODApprovers
                        if (ApprovedStatus == 7)
                        {
                            string VerifierIDs = "";
                            string ApproverIDs = "";
                            string UserApproverIDs = "";
                            string HODApproverID = "";
                            try
                            {
                                string[] ExpensehodApproverIDs = mstPVGClaim.HODApprover.Split(',');
                                HODApproverID = string.Join(",", ExpensehodApproverIDs.Skip(1));
                                string[] hODApproverIDs = HODApproverID.ToString().Split(',');
                                ApproverIDs = mstPVGClaim.Approver;
                                VerifierIDs = mstPVGClaim.Verifier;
                                //HODApproverID = mstExpenseClaim.HODApprover;
                                //Mail Code Implementation for Verifiers
                                foreach (string hODApproverID in hODApproverIDs)
                                {
                                    if (hODApproverID != "")
                                    {

                                    }
                                    else
                                    {
                                        string domainUrl = HttpContext.Request.Scheme + "://" + HttpContext.Request.Host;
                                        string clickUrl = domainUrl + "/" + "FinancePVGClaim/Details/" + PVGCID;

                                        var mstSenderDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(HttpContext.User.FindFirst("userid").Value));
                                        var senderName = mstSenderDetails.Name;
                                        var mstVerifierDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(VerifierIDs.ToString().Split(',')[0].ToString()));
                                        var toEmail = mstVerifierDetails.EmailAddress;
                                        var receiverName = mstVerifierDetails.Name;
                                        var claimNo = mstPVGClaim.PVGCNo;
                                        var screen = "PV-GIRO Claim";
                                        var approvalType = "Verification Request";
                                        int userID = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value);
                                        var subject = "PV-GIRO Claim for Verification " + claimNo;

                                        BackgroundJob.Enqueue(() => _sendMailServices.SendEmail("EmailTemplate.html",screen, subject, senderName, receiverName, claimNo, approvalType, userID, toEmail, clickUrl));
                                    }

                                    break;
                                }
                            }
                            catch
                            {
                            }
                            await _repository.MstPVGClaim.UpdateMstPVGClaimStatus(PVGCID, 1, int.Parse(HttpContext.User.FindFirst("userid").Value), DateTime.Now, string.Empty, VerifierIDs.ToString(), ApproverIDs.ToString(), UserApproverIDs.ToString(), HODApproverID.ToString(), isAlternateApprover,0);

                        }
                        #endregion

                        #region PVG Approver
                        else if (ApprovedStatus == 2)
                        {
                            string VerifierIDs = "";
                            string ApproverIDs = "";
                            string UserApproverIDs = "";
                            string HODApproverID = "";
                            try
                            {
                                string[] PVGapproverIDs = mstPVGClaim.Approver.Split(',');
                                ApproverIDs = string.Join(",", PVGapproverIDs.Skip(1));
                                string[] approverIDs = ApproverIDs.Split(',');
                                int CreatedBy = Convert.ToInt32(mstPVGClaim.CreatedBy);

                                //Mail Code Implementation for Approvers
                                /*
                                foreach (string approverID in approverIDs)
                                {
                                    if (approverID != "")
                                    {

                                    }
                                    else
                                    {

                                    }

                                    break;
                                }
                                */
                            }
                            catch
                            {
                            }
                            string financeStartDay = _configuration.GetValue<string>("FinanceStartDay");
                            await _repository.MstPVGClaim.UpdateMstPVGClaimStatus(PVGCID, 3, int.Parse(HttpContext.User.FindFirst("userid").Value), DateTime.Now, string.Empty, VerifierIDs, ApproverIDs, UserApproverIDs.ToString(), HODApproverID.ToString(), isAlternateApprover, int.Parse(financeStartDay));
                        }

                        #endregion

                        return Json(new { res = "Done" });
                    }
                    else
                    {
                        //TempData["Status_Invocie"] = "Approval";
                        return Json(new { res = "0" });
                    }
                }
                else
                {

                    int MCID = Convert.ToInt32(id);


                    var mstMileageClaim = await _repository.MstMileageClaim.GetMileageClaimByIdAsync(MCID);

                    if (mstMileageClaim == null)
                    {
                        // return NotFound();
                    }


                    int ApprovedStatus = Convert.ToInt32(mstMileageClaim.ApprovalStatus);
                    bool excute = _repository.MstMileageClaim.ExistsApproval(MCID.ToString(), ApprovedStatus, HttpContext.User.FindFirst("userid").Value, "Mileage");

                    // If execute is false, Check if the current user is alternate user for this claim
                    if (excute == false)
                    {
                        string hodapprover = _repository.MstMileageClaim.GetApproval(MCID.ToString(), ApprovedStatus, HttpContext.User.FindFirst("userid").Value, "Mileage");
                        int loggedInUserId = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value);
                        var delegatedUserId = await _alternateApproverHelper.IsUserHasAnyAlternateApprovalSet(loggedInUserId);
                        if (hodapprover == delegatedUserId.Value.ToString())
                        {
                            excute = true;
                            isAlternateApprover = true;
                        }
                    }

                    if (excute == true)
                    {
                        #region Mileage UserApprovers
                        if (ApprovedStatus == 6)
                        {
                            string VerifierIDs = "";
                            string ApproverIDs = "";
                            string UserApproverIDs = "";
                            string HODApproverID = "";
                            try
                            {
                                string[] MileageuserApproverIDs = mstMileageClaim.UserApprovers.Split(',');
                                UserApproverIDs = string.Join(",", MileageuserApproverIDs.Skip(1));
                                string[] userApproverIDs = UserApproverIDs.ToString().Split(',');
                                ApproverIDs = mstMileageClaim.Approver;
                                VerifierIDs = mstMileageClaim.Verifier;
                                HODApproverID = mstMileageClaim.HODApprover;
                                //Mail Code Implementation for Verifiers

                                foreach (string userApproverID in userApproverIDs)
                                {
                                    if (userApproverID != "")
                                    {
                                        string domainUrl = HttpContext.Request.Scheme + "://" + HttpContext.Request.Host;
                                        string clickUrl = domainUrl + "/" + "HodSummary/Details/" + MCID;

                                        var mstSenderDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(HttpContext.User.FindFirst("userid").Value));
                                        var senderName = mstSenderDetails.Name;
                                        int? approverId = await _alternateApproverHelper.IsAlternateApprovalSetForUser(Convert.ToInt32(userApproverID));
                                        var mstVerifierDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(userApproverID));
                                        bool isAlternateApproverSet = false;
                                        if (approverId.HasValue)
                                        {
                                            mstVerifierDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(approverId.Value));
                                            // Alternate approver is configured for the current user. So, do not show actions
                                            isAlternateApproverSet = true;
                                        }
                                        var toEmail = mstVerifierDetails.EmailAddress;
                                        var receiverName = mstVerifierDetails.Name;
                                        var claimNo = mstMileageClaim.MCNo;
                                        var screen = "Mileage Claim";
                                        var approvalType = "Approval Request";
                                        int userID = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value);
                                        var subject = "Mileage Claim for Approval " + claimNo;

                                        BackgroundJob.Enqueue(() => _sendMailServices.SendEmail("EmailTemplate.html",screen, subject, senderName, receiverName, claimNo, approvalType, userID, toEmail, clickUrl));

                                    }
                                    else
                                    {
                                        string domainUrl = HttpContext.Request.Scheme + "://" + HttpContext.Request.Host;
                                        string clickUrl = domainUrl + "/" + "FinanceMileageClaim/Details/" + MCID;

                                        var mstSenderDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(HttpContext.User.FindFirst("userid").Value));
                                        var senderName = mstSenderDetails.Name;
                                        int? approverId = await _alternateApproverHelper.IsAlternateApprovalSetForUser(Convert.ToInt32(VerifierIDs.ToString().Split(',')[0].ToString()));
                                        var mstVerifierDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(VerifierIDs.ToString().Split(',')[0].ToString()));
                                        bool isAlternateApproverSet = false;
                                        if (approverId.HasValue)
                                        {
                                            mstVerifierDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(approverId.Value));
                                            // Alternate approver is configured for the current user. So, do not show actions
                                            isAlternateApproverSet = true;
                                        }
                                        var toEmail = mstVerifierDetails.EmailAddress;
                                        var receiverName = mstVerifierDetails.Name;
                                        var claimNo = mstMileageClaim.MCNo;
                                        var screen = "Mileage Claim";
                                        var approvalType = "Verification Request";
                                        int userID = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value);
                                        var subject = "Mileage Claim for Verification " + claimNo;

                                        BackgroundJob.Enqueue(() => _sendMailServices.SendEmail("EmailTemplate.html",screen, subject, senderName, receiverName, claimNo, approvalType, userID, toEmail, clickUrl));

                                    }

                                    break;
                                }
                            }
                            catch
                            {
                            }
                            await _repository.MstMileageClaim.UpdateMstMileageClaimStatus(MCID, 1, int.Parse(HttpContext.User.FindFirst("userid").Value), DateTime.Now, string.Empty, VerifierIDs.ToString(), ApproverIDs.ToString(), UserApproverIDs.ToString(), HODApproverID.ToString(), isAlternateApprover, 0);

                        }
                        #endregion

                        #region Mileage Verifier
                        if (ApprovedStatus == 1)
                        {
                            string VerifierIDs = "";
                            string ApproverIDs = "";
                            string UserApproverIDs = "";
                            string HODApproverID = "";
                            try
                            {
                                string[] MileageverifierIDs = mstMileageClaim.Verifier.Split(',');
                                VerifierIDs = string.Join(",", MileageverifierIDs.Skip(1));
                                string[] verifierIDs = VerifierIDs.ToString().Split(',');
                                ApproverIDs = mstMileageClaim.Approver;
                                UserApproverIDs = mstMileageClaim.UserApprovers;
                                HODApproverID = mstMileageClaim.HODApprover;
                                //Mail Code Implementation for Verifiers
                                /*
                                foreach (string verifierID in verifierIDs)
                                {
                                    if (verifierID != "")
                                    {

                                    }
                                    else
                                    {

                                    }

                                    break;
                                }
                                */
                            }
                            catch
                            {
                            }
                            await _repository.MstMileageClaim.UpdateMstMileageClaimStatus(MCID, 2, int.Parse(HttpContext.User.FindFirst("userid").Value), DateTime.Now, string.Empty, VerifierIDs.ToString(), ApproverIDs.ToString(), UserApproverIDs.ToString(), HODApproverID.ToString(), isAlternateApprover, 0);

                        }
                        #endregion

                        #region Mileage HODApprovers
                        if (ApprovedStatus == 7)
                        {
                            string VerifierIDs = "";
                            string ApproverIDs = "";
                            string UserApproverIDs = "";
                            string HODApproverIDs = "";
                            try
                            {
                                string[] MileagehodApproverIDs = mstMileageClaim.HODApprover.Split(',');
                                HODApproverIDs = string.Join(",", MileagehodApproverIDs.Skip(1));
                                string[] hODApproverIDs = HODApproverIDs.ToString().Split(',');
                                ApproverIDs = mstMileageClaim.Approver;
                                //VerifierIDs = mstMileageClaim.Verifier;
                                //HODApproverID = mstMileageClaim.HODApprover;
                                //Mail Code Implementation for Verifiers
                                foreach (string hODApproverID in hODApproverIDs)
                                {
                                    if (hODApproverID != "")
                                    {

                                    }
                                    else
                                    {
                                        string domainUrl = HttpContext.Request.Scheme + "://" + HttpContext.Request.Host;
                                        string clickUrl = domainUrl + "/" + "FinanceMileageClaim/Details/" + MCID;

                                        var mstSenderDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(HttpContext.User.FindFirst("userid").Value));
                                        var senderName = mstSenderDetails.Name;
                                        var mstVerifierDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(ApproverIDs.ToString().Split(',')[0].ToString()));
                                        var toEmail = mstVerifierDetails.EmailAddress;
                                        var receiverName = mstVerifierDetails.Name;
                                        var claimNo = mstMileageClaim.MCNo;
                                        var screen = "Mileage Claim";
                                        var approvalType = "Approval Request";
                                        int userID = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value);
                                        var subject = "Mileage Claim for Approval " + claimNo;

                                        BackgroundJob.Enqueue(() => _sendMailServices.SendEmail("EmailTemplate.html",screen, subject, senderName, receiverName, claimNo, approvalType, userID, toEmail, clickUrl));
                                    }

                                    break;
                                }
                            }
                            catch
                            {
                            }
                            await _repository.MstMileageClaim.UpdateMstMileageClaimStatus(MCID, 2, int.Parse(HttpContext.User.FindFirst("userid").Value), DateTime.Now, string.Empty, VerifierIDs.ToString(), ApproverIDs.ToString(), UserApproverIDs.ToString(), HODApproverIDs.ToString(), isAlternateApprover, 0);

                        }
                        #endregion

                        #region Mileage Approver
                        else if (ApprovedStatus == 2)
                        {
                            string VerifierIDs = "";
                            string ApproverIDs = "";
                            string UserApproverIDs = "";
                            string HODApproverID = "";
                            try
                            {
                                string[] MileageapproverIDs = mstMileageClaim.Approver.Split(',');
                                ApproverIDs = string.Join(",", MileageapproverIDs.Skip(1));
                                string[] approverIDs = ApproverIDs.Split(',');
                                int CreatedBy = Convert.ToInt32(mstMileageClaim.CreatedBy);

                                //Mail Code Implementation for Approvers
                                /*
                                foreach (string approverID in approverIDs)
                                {
                                    if (approverID != "")
                                    {

                                    }
                                    else
                                    {

                                    }

                                    break;
                                }
                                */
                            }
                            catch
                            {
                            }
                            string financeStartDay = _configuration.GetValue<string>("FinanceStartDay");
                            await _repository.MstMileageClaim.UpdateMstMileageClaimStatus(MCID, 3, int.Parse(HttpContext.User.FindFirst("userid").Value), DateTime.Now, string.Empty, VerifierIDs, ApproverIDs, UserApproverIDs.ToString(), HODApproverID.ToString(), isAlternateApprover, int.Parse(financeStartDay));
                        }

                        #endregion

                        return Json(new { res = "Done" });
                    }
                    else
                    {
                        //TempData["Status_Invocie"] = "Approval";
                        return Json(new { res = "0" });
                    }
                }

            }
            else
            {
                return Json(new { res = "Done" });
            }

        }

        public async Task<JsonResult> ExporttoExcel(string data)
        {
            var hodClaimSearch = JsonConvert.DeserializeObject<HodClaimSearch>(data);
            var mstMileageClaimsWithDetails = await _repository.MstUser.GetAllHODSummaryClaimsAsync(Convert.ToInt32(HttpContext.User.FindFirst("userid").Value), hodClaimSearch.ModuleName, hodClaimSearch.StatusID, hodClaimSearch.FromDate, hodClaimSearch.ToDate);
            List<CustomClaim> mileageClaimVMs = new List<CustomClaim>();

            DataTable dt = new DataTable("Grid");
            dt.Columns.AddRange(new DataColumn[9] { new DataColumn("Claim"),
                                        new DataColumn("Requester"),
                                        new DataColumn("Date Created"),
                                        new DataColumn("Facility"),
                                        new DataColumn("Payee"),
                                        new DataColumn("Contact Number"),
                                        new DataColumn("Total Claim"),
                                        new DataColumn("Approver"),
                                        new DataColumn("Status")});

            foreach (var mc in mstMileageClaimsWithDetails)
            {
                CustomClaim mileageClaimVM = new CustomClaim();
                mileageClaimVM.ApprovalStatus = mc.ApprovalStatus;
                if (mc.ApprovalStatus == 1)
                {
                    mileageClaimVM.ClaimStatusName = "Awaiting Verification";

                }
                else if (mc.ApprovalStatus == 2)
                {
                    mileageClaimVM.ClaimStatusName = "Awaiting Signatory approval";

                }
                else if (mc.ApprovalStatus == 3)
                {
                    mileageClaimVM.ClaimStatusName = "Approved";

                }
                else if (mc.ApprovalStatus == 4)
                {
                    mileageClaimVM.ClaimStatusName = "Request to Amend";
                }
                else if (mc.ApprovalStatus == 5)
                {
                    mileageClaimVM.ClaimStatusName = "Voided";

                }
                else if (mc.ApprovalStatus == -5)
                {
                    mileageClaimVM.ClaimStatusName = "Requested to Void";

                }
                else if (mc.ApprovalStatus == 6)
                {
                    mileageClaimVM.ClaimStatusName = "Awaiting approval";

                }
                else if (mc.ApprovalStatus == 7)
                {
                    mileageClaimVM.ClaimStatusName = "Awaiting HOD approval";

                }
                else if (mc.ApprovalStatus == 9)
                {
                    mileageClaimVM.ClaimStatusName = "Exported to AccPac";

                }
                else if (mc.ApprovalStatus == 10)
                {
                    mileageClaimVM.ClaimStatusName = "Exported to Bank";

                }
                else
                {
                    mileageClaimVM.ClaimStatusName = "New";
                }

                if (mc.UserApprovers != "")
                {
                    mileageClaimVM.Approver = mc.UserApprovers.Split(',').First();
                    if (mileageClaimVM.Approver == HttpContext.User.FindFirst("userid").Value && (mileageClaimVM.ApprovalStatus == 6))
                    {
                        mileageClaimVM.IsActionAllowed = true;
                    }
                }
                else if (mc.Verifier != "")
                {
                    mileageClaimVM.Approver = mc.Verifier.Split(',').First();
                    if (mileageClaimVM.Approver == HttpContext.User.FindFirst("userid").Value && (mileageClaimVM.ApprovalStatus == 1 || mileageClaimVM.ApprovalStatus == 2))
                    {
                        mileageClaimVM.IsActionAllowed = true;
                    }

                    //string VerifierIDs = string.Join(",", MileageverifierIDs.Skip(1));
                }
                else if (mc.HODApprover != "")
                {
                    mileageClaimVM.Approver = mc.HODApprover.Split(',').First();
                    if (mileageClaimVM.Approver == HttpContext.User.FindFirst("userid").Value && (mileageClaimVM.ApprovalStatus == 7))
                    {
                        mileageClaimVM.IsActionAllowed = true;
                    }
                }
                else if (mc.Approver != "")
                {
                    mileageClaimVM.Approver = mc.Approver.Split(',').First();
                    if (mileageClaimVM.Approver == HttpContext.User.FindFirst("userid").Value && (mileageClaimVM.ApprovalStatus == 1 || mileageClaimVM.ApprovalStatus == 2))
                    {
                        mileageClaimVM.IsActionAllowed = true;
                    }
                }
                else
                {
                    mileageClaimVM.Approver = "";
                }

                if (mileageClaimVM.Approver != "")
                {
                    var mstUserApprover = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(mileageClaimVM.Approver));
                    if (mileageClaimVM.ApprovalStatus != 3 && mileageClaimVM.ApprovalStatus != 4 && mileageClaimVM.ApprovalStatus != -5 && mileageClaimVM.ApprovalStatus != 5)
                        mileageClaimVM.Approver = mstUserApprover.Name;
                    else
                        mileageClaimVM.Approver = "";
                }

                //mileageClaimVMs.Add(mileageClaimVM);
                dt.Rows.Add(mileageClaimVM.CNO = mc.CNO,
                            mileageClaimVM.Name = mc.Name,
                            mileageClaimVM.CreatedDate = mc.CreatedDate,// Convert.ToDateTime(mc.CreatedDate).ToString("dd/MM/yyyy", System.Globalization.CultureInfo.CreateSpecificCulture("en-US")),
                            mileageClaimVM.FacilityName = mc.FacilityName,
                            mileageClaimVM.Name = mc.Name,
                            mileageClaimVM.Phone = mc.Phone,
                            mileageClaimVM.GrandTotal = mc.GrandTotal,
                            mileageClaimVM.Approver = mileageClaimVM.Approver,
                            mileageClaimVM.ClaimStatusName = mileageClaimVM.ClaimStatusName);
            }

            if (dt != null && dt.Rows.Count > 0)
            {
                DataRow[] drows = dt.Select();
                for (int i = 0; i < drows.Length; i++)
                {
                    dt.Rows[i]["Total Claim"] = "$" + dt.Rows[i]["Total Claim"];
                    dt.Rows[i].EndEdit();
                    dt.AcceptChanges();
                }
            }

            string filename = "HODClaims-Export" + DateTime.Now.ToString("ddMMyyyyss") + ".xlsx";
            var path = "FileUploads/temp/";
            string pathToFilesold = Path.Combine(path, filename);
            if (CloudStorageAccount.TryParse(_configuration.GetSection("ConnectionStrings")["BlobConnectionString"], out CloudStorageAccount storageAccount))
            {
                CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

                CloudBlobContainer container = blobClient.GetContainerReference(_configuration.GetSection("ConnectionStrings")["BlobContainerName"]);

                CloudBlockBlob blockBlob = container.GetBlockBlobReference(pathToFilesold);

                using (XLWorkbook wb = new XLWorkbook())
                {
                    wb.Worksheets.Add(dt);
                    using (var stream = await blockBlob.OpenWriteAsync())
                    {
                        wb.SaveAs(stream);
                    }
                }
            }
            return Json(new { fileName = pathToFilesold });
        }

        public async Task<IActionResult> GetPrintTBClaimDetails(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            long TBCID = Convert.ToInt64(id);
            TBClaimDetailVM tBClaimDetailVM = new TBClaimDetailVM();
            if (User != null && User.Identity.IsAuthenticated)
            {
                var mstUserDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(HttpContext.User.FindFirst("userid").Value));

                var mstTBClaim = await _repository.MstTBClaim.GetTBClaimByIdAsync(id);

                if (mstTBClaim == null)
                {
                    return NotFound();
                }
                var dtTBSummaries = await _repository.DtTBClaimSummary.GetDtTBClaimSummaryByIdAsync(id);
                var dtTBClaims = await _repository.DtTBClaim.GetDtTBClaimByIdAsync(id);

                //List<DtMileageClaimVM> dtMileageClaimVMs = new List<DtMileageClaimVM>();
                tBClaimDetailVM.DtTBClaimVMs = new List<DtTBClaimVM>();
                // List<clsDtSupplierPO> oclsDtSupplierPO = new List<clsDtSupplierPO>();
                foreach (var item in dtTBClaims)
                {
                    DtTBClaimVM dtTBClaimVM = new DtTBClaimVM();

                    dtTBClaimVM.TBCItemID = item.TBCItemID;
                    dtTBClaimVM.TBCID = item.TBCID;
                    dtTBClaimVM.DateOfJourney = item.Date;

                    dtTBClaimVM.Description = item.Description;
                    dtTBClaimVM.Amount = item.Amount;
                    dtTBClaimVM.ExpenseCategory = item.MstExpenseCategory.Description;
                    dtTBClaimVM.AccountCode = item.AccountCode;

                    //if (item.FacilityID != null)
                    //{
                    //    dtMileageClaimVM.FacilityName = _repository.MstFacility.GetFacilityByIdAsync(item.FacilityID).GetAwaiter().GetResult().FacilityName.ToString();
                    //}
                    ////Need to change to not null
                    //if (item.FromFacilityID != 0)
                    //{
                    //    dtMileageClaimVM.FromFacilityName = _repository.MstFacility.GetFacilityByIdAsync(item.FromFacilityID).GetAwaiter().GetResult().FacilityName.ToString();
                    //}
                    ////Need to change to not null
                    //if (item.ToFacilityID != 0)
                    //{
                    //    dtMileageClaimVM.ToFacilityName = _repository.MstFacility.GetFacilityByIdAsync(item.ToFacilityID).GetAwaiter().GetResult().FacilityName.ToString();
                    //}

                    tBClaimDetailVM.DtTBClaimVMs.Add(dtTBClaimVM);
                }
                tBClaimDetailVM.DtTBClaimSummaries = dtTBSummaries;
                tBClaimDetailVM.TBClaimAudits = new List<TBClaimAuditVM>();

                var dtTBClaimAudits = await _repository.MstTBClaimAudit.GetMstTBClaimAuditByIdAsync(id);

                foreach (var item in dtTBClaimAudits)
                {
                    TBClaimAuditVM mstTBClaimAuditVM = new TBClaimAuditVM();
                    mstTBClaimAuditVM.Action = item.Action;
                    mstTBClaimAuditVM.Description = item.Description;
                    mstTBClaimAuditVM.AuditDateTickle = Helper.RelativeDate(item.AuditDate);
                    tBClaimDetailVM.TBClaimAudits.Add(mstTBClaimAuditVM);
                }

                tBClaimDetailVM.TBClaimFileUploads = new List<DtTBClaimFileUpload>();

                tBClaimDetailVM.TBClaimFileUploads = _repository.DtTBClaimFileUpload.GetDtTBClaimAuditByIdAsync(id).Result.ToList();

                TBClaimVM tBClaimVM = new TBClaimVM();
                tBClaimVM.Month = mstTBClaim.Month;
                tBClaimVM.Year = mstTBClaim.Year;
                tBClaimVM.GrandTotal = mstTBClaim.GrandTotal;
                tBClaimVM.Company = mstTBClaim.Company;
                tBClaimVM.Name = mstTBClaim.MstUser.Name;
                tBClaimVM.DepartmentName = mstTBClaim.MstDepartment.Department;
                tBClaimVM.FacilityName = mstTBClaim.MstFacility.FacilityName;
                tBClaimVM.CreatedDate = mstTBClaim.CreatedDate.ToString("d");
                tBClaimVM.Verifier = mstTBClaim.Verifier;
                tBClaimVM.Approver = mstTBClaim.Approver;
                tBClaimVM.TBCNo = mstTBClaim.TBCNo;
                tBClaimVM.VoucherNo = mstTBClaim.VoucherNo;
                tBClaimDetailVM.TBClaimVM = tBClaimVM;
                //mileageClaimDetailVM.DtMileageClaimVMs = dtMileageClaimVMs;
            }
            return PartialView("GetTBDetailsPrint", tBClaimDetailVM);
        }

        public async Task<IActionResult> GetMileagePrintClaimDetails(long? id)
        {
            long MCID = Convert.ToInt64(id);
            MileageClaimDetailVM mileageClaimDetailVM = new MileageClaimDetailVM();
            if (User != null && User.Identity.IsAuthenticated)
            {
                var mstUserDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(HttpContext.User.FindFirst("userid").Value));

                var mstMileageClaim = await _repository.MstMileageClaim.GetMileageClaimByIdAsync(id);

                if (mstMileageClaim == null)
                {
                    return NotFound();
                }
                var dtMileageSummaries = await _repository.DtMileageClaimSummary.GetDtMileageClaimSummaryByIdAsync(id);
                var dtMileageClaims = await _repository.DtMileageClaim.GetDtMileageClaimByIdAsync(id);

                //List<DtMileageClaimVM> dtMileageClaimVMs = new List<DtMileageClaimVM>();
                mileageClaimDetailVM.DtMileageClaimVMs = new List<DtMileageClaimVM>();
                // List<clsDtSupplierPO> oclsDtSupplierPO = new List<clsDtSupplierPO>();
                foreach (var item in dtMileageClaims)
                {
                    DtMileageClaimVM dtMileageClaimVM = new DtMileageClaimVM();

                    dtMileageClaimVM.MCItemID = item.MCItemID;
                    dtMileageClaimVM.MCID = item.MCID;
                    dtMileageClaimVM.DateOfJourney = item.DateOfJourney;
                    dtMileageClaimVM.InTime = item.InTime;
                    dtMileageClaimVM.OutTime = item.OutTime;
                    dtMileageClaimVM.StartReading = item.StartReading;
                    dtMileageClaimVM.EndReading = item.EndReading;
                    dtMileageClaimVM.Kms = item.Kms;
                    dtMileageClaimVM.Remark = item.Remark;
                    dtMileageClaimVM.Amount = item.Amount;
                    dtMileageClaimVM.AccountCode = item.AccountCode;

                    if (item.FacilityID != null)
                    {
                        var mstFacility = await _repository.MstFacility.GetFacilityByIdAsync(item.FacilityID);
                        dtMileageClaimVM.FacilityName = mstFacility.FacilityName;
                    }
                    //if (item.FromFacilityID != null || item.FromFacilityID != string.Empty)
                    //{
                    //    var mstFacility = await _repository.MstFacility.GetFacilityByIdAsync(Int32.Parse(item.FromFacilityID));
                    //    dtMileageClaimVM.FromFacilityName = mstFacility.FacilityName;
                    //}
                    //if (item.ToFacilityID != null || item.ToFacilityID != string.Empty)
                    //{
                    //    var mstFacility = await _repository.MstFacility.GetFacilityByIdAsync(Int32.Parse(item.ToFacilityID));
                    //    dtMileageClaimVM.ToFacilityName = mstFacility.FacilityName;
                    //}
                    dtMileageClaimVM.FromFacilityID = item.FromFacilityID;
                    dtMileageClaimVM.ToFacilityID = item.ToFacilityID;

                    //Need to change to not null
                    //if (item.FromFacilityID != 0)
                    //{
                    //    var mstFacility = await _repository.MstFacility.GetFacilityByIdAsync(item.FromFacilityID);
                    //    dtMileageClaimVM.FromFacilityName = mstFacility.FacilityName;
                    //}
                    ////Need to change to not null
                    //if (item.ToFacilityID != 0)
                    //{
                    //    var mstFacility = await _repository.MstFacility.GetFacilityByIdAsync(item.ToFacilityID);
                    //    dtMileageClaimVM.ToFacilityName = mstFacility.FacilityName;
                    //}

                    mileageClaimDetailVM.DtMileageClaimVMs.Add(dtMileageClaimVM);
                }
                mileageClaimDetailVM.DtMileageClaimSummaries = dtMileageSummaries;
                mileageClaimDetailVM.MileageClaimAudits = new List<MileageClaimAuditVM>();
                var dtMileageClaimAudits = await _repository.MstMileageClaimAudit.GetMstMileageClaimAuditByIdAsync(id);

                foreach (var item in dtMileageClaimAudits)
                {
                    MileageClaimAuditVM mstMileageClaimAuditVM = new MileageClaimAuditVM();
                    mstMileageClaimAuditVM.Action = item.Action;
                    mstMileageClaimAuditVM.Description = item.Description;
                    mstMileageClaimAuditVM.AuditDateTickle = Helper.RelativeDate(item.AuditDate);
                    mileageClaimDetailVM.MileageClaimAudits.Add(mstMileageClaimAuditVM);
                }

                mileageClaimDetailVM.MileageClaimFileUploads = new List<DtMileageClaimFileUpload>();

                mileageClaimDetailVM.MileageClaimFileUploads = _repository.DtMileageClaimFileUpload.GetDtMileageClaimAuditByIdAsync(id).Result.ToList();

                MileageClaimVM mileageClaimVM = new MileageClaimVM();
                mileageClaimVM.TravelMode = mstMileageClaim.TravelMode;
                mileageClaimVM.GrandTotal = mstMileageClaim.GrandTotal;
                mileageClaimVM.TotalKm = mstMileageClaim.TotalKm;
                mileageClaimVM.Company = mstMileageClaim.Company;
                mileageClaimVM.Name = mstMileageClaim.MstUser.Name;
                mileageClaimVM.DepartmentName = mstMileageClaim.MstDepartment.Department;
                mileageClaimVM.FacilityName = mstMileageClaim.MstFacility.FacilityName;
                mileageClaimVM.CreatedDate = mstMileageClaim.CreatedDate.ToString("d");
                mileageClaimVM.Verifier = mstMileageClaim.Verifier;
                mileageClaimVM.Approver = mstMileageClaim.Approver;
                mileageClaimVM.VoucherNo = mstMileageClaim.VoucherNo;
                ViewBag.MCID = id;
                mileageClaimVM.MCNo = mstMileageClaim.MCNo;
                mileageClaimDetailVM.MileageClaimVM = mileageClaimVM;
                //mileageClaimDetailVM.DtMileageClaimVMs = dtMileageClaimVMs;
            }
            return PartialView("GetMileageDetailsPrint", mileageClaimDetailVM);
        }

        public async Task<IActionResult> GetPrintClaimDetails(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            long ECID = Convert.ToInt64(id);
            ExpenseClaimDetailVM expenseClaimDetailVM = new ExpenseClaimDetailVM();
            if (User != null && User.Identity.IsAuthenticated)
            {
                var mstExpenseClaim = await _repository.MstExpenseClaim.GetExpenseClaimByIdAsync(id);

                if (mstExpenseClaim == null)
                {
                    return NotFound();
                }
                var dtExpenseSummaries = await _repository.DtExpenseClaimSummary.GetDtExpenseClaimSummaryByIdAsync(id);
                var dtExpenseClaims = await _repository.DtExpenseClaim.GetDtExpenseClaimByIdAsync(id);

                //List<DtMileageClaimVM> dtMileageClaimVMs = new List<DtMileageClaimVM>();
                expenseClaimDetailVM.DtExpenseClaimVMs = new List<DtExpenseClaimVM>();
                // List<clsDtSupplierPO> oclsDtSupplierPO = new List<clsDtSupplierPO>();
                foreach (var item in dtExpenseClaims)
                {
                    DtExpenseClaimVM dtExpenseClaimVM = new DtExpenseClaimVM();

                    dtExpenseClaimVM.ECItemID = item.ECItemID;
                    dtExpenseClaimVM.ECID = item.ECID;
                    dtExpenseClaimVM.DateOfJourney = item.Date;

                    dtExpenseClaimVM.Description = item.Description;
                    dtExpenseClaimVM.Amount = item.Amount;
                    dtExpenseClaimVM.Gst = item.GST;
                    dtExpenseClaimVM.AmountWithGST = item.Amount + item.GST;
                    dtExpenseClaimVM.ExpenseCategory = item.MstExpenseCategory.Description;
                    dtExpenseClaimVM.AccountCode = item.AccountCode;
                    dtExpenseClaimVM.ExpenseCategoryID = item.ExpenseCategoryID;

                    //if (item.FacilityID != null)
                    //{
                    //    dtMileageClaimVM.FacilityName = _repository.MstFacility.GetFacilityByIdAsync(item.FacilityID).GetAwaiter().GetResult().FacilityName.ToString();
                    //}
                    ////Need to change to not null
                    //if (item.FromFacilityID != 0)
                    //{
                    //    dtMileageClaimVM.FromFacilityName = _repository.MstFacility.GetFacilityByIdAsync(item.FromFacilityID).GetAwaiter().GetResult().FacilityName.ToString();
                    //}
                    ////Need to change to not null
                    //if (item.ToFacilityID != 0)
                    //{
                    //    dtMileageClaimVM.ToFacilityName = _repository.MstFacility.GetFacilityByIdAsync(item.ToFacilityID).GetAwaiter().GetResult().FacilityName.ToString();
                    //}

                    expenseClaimDetailVM.DtExpenseClaimVMs.Add(dtExpenseClaimVM);
                }
                expenseClaimDetailVM.DtExpenseClaimSummaries = dtExpenseSummaries;
                var GroupByQS = expenseClaimDetailVM.DtExpenseClaimVMs.GroupBy(s => s.ExpenseCategoryID);
                //var GroupByQS = (from std in expenseClaimDetailVM.DtExpenseClaimVMs
                //                                                           group std by std.ExpenseCategoryID);

                expenseClaimDetailVM.DtExpenseClaimVMSummary = new List<DtExpenseClaimVM>();

                foreach (var group in GroupByQS)
                {
                    DtExpenseClaimVM dtExpenseClaimVM = new DtExpenseClaimVM();
                    decimal amount = 0;
                    decimal gst = 0;
                    decimal sumamount = 0;
                    string ExpenseDesc = string.Empty;
                    string AccountCode = string.Empty;
                    foreach (var dtExpense in group)
                    {
                        amount = amount + dtExpense.Amount;
                        gst = gst + dtExpense.Gst;
                        sumamount = sumamount + dtExpense.AmountWithGST;
                        ExpenseDesc = dtExpense.ExpenseCategory;
                        AccountCode = dtExpense.AccountCode;
                    }
                    gst = gst / group.Count();
                    dtExpenseClaimVM.ExpenseCategory = ExpenseDesc;
                    dtExpenseClaimVM.AccountCode = AccountCode;
                    dtExpenseClaimVM.Amount = amount;
                    dtExpenseClaimVM.Gst = gst;
                    dtExpenseClaimVM.AmountWithGST = sumamount;
                    expenseClaimDetailVM.DtExpenseClaimVMSummary.Add(dtExpenseClaimVM);
                }

                expenseClaimDetailVM.ExpenseClaimAudits = new List<ExpenseClaimAuditVM>();

                var dtExpenseClaimAudits = await _repository.MstExpenseClaimAudit.GetMstExpenseClaimAuditByIdAsync(id);

                foreach (var item in dtExpenseClaimAudits)
                {
                    ExpenseClaimAuditVM mstExpenseClaimAuditVM = new ExpenseClaimAuditVM();
                    mstExpenseClaimAuditVM.Action = item.Action;
                    mstExpenseClaimAuditVM.Description = item.Description;
                    mstExpenseClaimAuditVM.AuditDateTickle = Helper.RelativeDate(item.AuditDate);
                    expenseClaimDetailVM.ExpenseClaimAudits.Add(mstExpenseClaimAuditVM);
                }

                expenseClaimDetailVM.ExpenseClaimFileUploads = new List<DtExpenseClaimFileUpload>();

                expenseClaimDetailVM.ExpenseClaimFileUploads = _repository.DtExpenseClaimFileUpload.GetDtExpenseClaimAuditByIdAsync(id).Result.ToList();

                ExpenseClaimVM expenseClaimVM = new ExpenseClaimVM();
                expenseClaimVM.ClaimType = mstExpenseClaim.ClaimType;
                expenseClaimVM.GrandTotal = mstExpenseClaim.GrandTotal;
                expenseClaimVM.TotalAmount = mstExpenseClaim.TotalAmount;
                expenseClaimVM.Company = mstExpenseClaim.Company;
                expenseClaimVM.Name = mstExpenseClaim.MstUser.Name;
                expenseClaimVM.DepartmentName = mstExpenseClaim.MstDepartment.Department;
                expenseClaimVM.FacilityName = mstExpenseClaim.MstFacility.FacilityName;
                expenseClaimVM.CreatedDate = Convert.ToDateTime(mstExpenseClaim.CreatedDate).ToString("dd/MM/yyyy", System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
                expenseClaimVM.Verifier = mstExpenseClaim.Verifier;
                expenseClaimVM.Approver = mstExpenseClaim.Approver;
                expenseClaimVM.ECNo = mstExpenseClaim.ECNo;
                expenseClaimVM.VoucherNo = mstExpenseClaim.VoucherNo;
                ViewBag.ECID = id;
                expenseClaimDetailVM.ExpenseClaimVM = expenseClaimVM;
                //mileageClaimDetailVM.DtMileageClaimVMs = dtMileageClaimVMs;
            }
            return PartialView("GetExpenseDetailsPrint", expenseClaimDetailVM);
        }

        public async Task<IActionResult> GetPVGCDetailsPrint(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            long PVGCID = Convert.ToInt64(id);
            PVGClaimDetailVM pVGClaimDetailVM = new PVGClaimDetailVM();
            if (User != null && User.Identity.IsAuthenticated)
            {
                var mstPVGClaim = await _repository.MstPVGClaim.GetPVGClaimByIdAsync(id);

                if (mstPVGClaim == null)
                {
                    return NotFound();
                }

                var dtPVGSummaries = await _repository.DtPVGClaimSummary.GetDtPVGClaimSummaryByIdAsync(id);
                var dtPVGClaims = await _repository.DtPVGClaim.GetDtPVGClaimByIdAsync(id);

                //List<DtMileageClaimVM> dtMileageClaimVMs = new List<DtMileageClaimVM>();
                pVGClaimDetailVM.DtPVGClaimVMs = new List<DtPVGClaimVM>();
                // List<clsDtSupplierPO> oclsDtSupplierPO = new List<clsDtSupplierPO>();
                foreach (var item in dtPVGClaims)
                {
                    DtPVGClaimVM dtPVGClaimVM = new DtPVGClaimVM();

                    dtPVGClaimVM.PVGCItemID = item.PVGCItemID;
                    dtPVGClaimVM.PVGCID = item.PVGCID;
                    dtPVGClaimVM.Date = item.Date;

                    dtPVGClaimVM.ChequeNo = item.ChequeNo;
                    dtPVGClaimVM.Particulars = item.Particulars;
                    dtPVGClaimVM.Payee = item.Payee;
                    dtPVGClaimVM.InvoiceNo = item.InvoiceNo;
                    dtPVGClaimVM.Amount = item.Amount;
                    dtPVGClaimVM.GST = item.GST;
                    dtPVGClaimVM.AmountWithGST = item.Amount + item.GST;
                    dtPVGClaimVM.ExpenseCategory = item.MstExpenseCategory.Description;
                    dtPVGClaimVM.AccountCode = item.AccountCode;
                    dtPVGClaimVM.ExpenseCategoryID = item.ExpenseCategoryID;
                    dtPVGClaimVM.Bank = item.Bank;
                    dtPVGClaimVM.BankCode = item.BankCode;
                    dtPVGClaimVM.BranchCode = item.BranchCode;
                    dtPVGClaimVM.BankAccount = item.BankAccount;
                    dtPVGClaimVM.Mobile = item.Mobile;

                    //if (item.FacilityID != null)
                    //{
                    //    dtMileageClaimVM.FacilityName = _repository.MstFacility.GetFacilityByIdAsync(item.FacilityID).GetAwaiter().GetResult().FacilityName.ToString();
                    //}
                    ////Need to change to not null
                    //if (item.FromFacilityID != 0)
                    //{
                    //    dtMileageClaimVM.FromFacilityName = _repository.MstFacility.GetFacilityByIdAsync(item.FromFacilityID).GetAwaiter().GetResult().FacilityName.ToString();
                    //}
                    ////Need to change to not null
                    //if (item.ToFacilityID != 0)
                    //{
                    //    dtMileageClaimVM.ToFacilityName = _repository.MstFacility.GetFacilityByIdAsync(item.ToFacilityID).GetAwaiter().GetResult().FacilityName.ToString();
                    //}

                    pVGClaimDetailVM.DtPVGClaimVMs.Add(dtPVGClaimVM);
                }

                pVGClaimDetailVM.DtPVGClaimSummaries = dtPVGSummaries;
                var GroupByQS = pVGClaimDetailVM.DtPVGClaimVMs.GroupBy(s => s.ExpenseCategoryID);
                //var GroupByQS = (from std in pVGClaimDetailVM.DtPVGClaimVMs
                //                                                           group std by std.PVGCategoryID);

                //pVGClaimDetailVM.DtPVGClaimVMSummary = new List<DtPVGClaimVM>();

                //foreach (var group in GroupByQS)
                //{
                //    DtPVGClaimVM dtPVGClaimVM = new DtPVGClaimVM();
                //    decimal amount = 0;
                //    decimal gst = 0;
                //    decimal sumamount = 0;
                //    string PVGDesc = string.Empty;
                //    string AccountCode = string.Empty;
                //    foreach (var dtPVG in group)
                //    {
                //        amount = amount + dtPVG.Amount;
                //        gst = gst + dtPVG.GST;
                //        sumamount = sumamount + dtPVG.AmountWithGST;
                //        PVGDesc = dtPVG.ExpenseCategory;
                //        AccountCode = dtPVG.AccountCode;
                //    }
                //    gst = gst / group.Count();
                //    dtPVGClaimVM.ExpenseCategory = PVGDesc;
                //    dtPVGClaimVM.AccountCode = AccountCode;
                //    dtPVGClaimVM.Amount = amount;
                //    dtPVGClaimVM.GST = gst;
                //    dtPVGClaimVM.AmountWithGST = sumamount;
                //    pVGClaimDetailVM.DtPVGClaimVMSummary.Add(dtPVGClaimVM);
                //}

                pVGClaimDetailVM.PVGClaimAudits = new List<PVGClaimAuditVM>();

                var dtPVGClaimAudits = await _repository.MstPVGClaimAudit.GetMstPVGClaimAuditByIdAsync(id);

                foreach (var item in dtPVGClaimAudits)
                {
                    PVGClaimAuditVM mstPVGClaimAuditVM = new PVGClaimAuditVM();
                    mstPVGClaimAuditVM.Action = item.Action;
                    mstPVGClaimAuditVM.Description = item.Description;
                    mstPVGClaimAuditVM.AuditDateTickle = Helper.RelativeDate(item.AuditDate);
                    pVGClaimDetailVM.PVGClaimAudits.Add(mstPVGClaimAuditVM);
                }

                pVGClaimDetailVM.PVGClaimFileUploads = new List<DtPVGClaimFileUpload>();

                pVGClaimDetailVM.PVGClaimFileUploads = _repository.DtPVGClaimFileUpload.GetDtPVGClaimAuditByIdAsync(id).Result.ToList();

                PVGClaimVM pVGClaimVM = new PVGClaimVM();
                //pVGClaimVM.ClaimType = mstPVGClaim.ClaimType;
                pVGClaimVM.GrandTotal = mstPVGClaim.GrandTotal;
                pVGClaimVM.TotalAmount = mstPVGClaim.TotalAmount;
                pVGClaimVM.Company = mstPVGClaim.Company;
                pVGClaimVM.Name = mstPVGClaim.MstUser.Name;
                pVGClaimVM.DepartmentName = mstPVGClaim.MstDepartment.Department;
                pVGClaimVM.FacilityName = mstPVGClaim.MstFacility.FacilityName;
                pVGClaimVM.CreatedDate = Convert.ToDateTime(mstPVGClaim.CreatedDate).ToString("dd/MM/yyyy", System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
                pVGClaimVM.Verifier = mstPVGClaim.Verifier;
                pVGClaimVM.Approver = mstPVGClaim.Approver;
                pVGClaimVM.PVGCNo = mstPVGClaim.PVGCNo;
                pVGClaimVM.VoucherNo = mstPVGClaim.VoucherNo;
                ViewBag.PVGCID = id;
                pVGClaimDetailVM.PVGClaimVM = pVGClaimVM;
                //mileageClaimDetailVM.DtMileageClaimVMs = dtMileageClaimVMs;
            }
            return PartialView("GetPVGCDetailsPrint", pVGClaimDetailVM);
        }

        public async Task<IActionResult> GetPVCCDetailsPrint(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            long PVCCID = Convert.ToInt64(id);
            PVCClaimDetailVM pVCClaimDetailVM = new PVCClaimDetailVM();
            if (User != null && User.Identity.IsAuthenticated)
            {
                var mstPVCClaim = await _repository.MstPVCClaim.GetPVCClaimByIdAsync(id);

                if (mstPVCClaim == null)
                {
                    return NotFound();
                }
                var dtPVCSummaries = await _repository.DtPVCClaimSummary.GetDtPVCClaimSummaryByIdAsync(id);
                var dtPVCClaims = await _repository.DtPVCClaim.GetDtPVCClaimByIdAsync(id);

                //List<DtMileageClaimVM> dtMileageClaimVMs = new List<DtMileageClaimVM>();
                pVCClaimDetailVM.DtPVCClaimVMs = new List<DtPVCClaimVM>();
                // List<clsDtSupplierPO> oclsDtSupplierPO = new List<clsDtSupplierPO>();
                foreach (var item in dtPVCClaims)
                {
                    DtPVCClaimVM dtPVCClaimVM = new DtPVCClaimVM();

                    dtPVCClaimVM.PVCCItemID = item.PVCCItemID;
                    dtPVCClaimVM.PVCCID = item.PVCCID;
                    dtPVCClaimVM.Date = item.Date;

                    dtPVCClaimVM.ChequeNo = item.ChequeNo;
                    dtPVCClaimVM.Particulars = item.Particulars;
                    dtPVCClaimVM.Payee = item.Payee;
                    dtPVCClaimVM.InvoiceNo = item.InvoiceNo;
                    dtPVCClaimVM.Amount = item.Amount;
                    dtPVCClaimVM.GST = item.GST;
                    dtPVCClaimVM.AmountWithGST = item.Amount + item.GST;
                    dtPVCClaimVM.ExpenseCategory = item.MstExpenseCategory.Description;
                    dtPVCClaimVM.AccountCode = item.AccountCode;
                    dtPVCClaimVM.ExpenseCategoryID = item.ExpenseCategoryID;

                    //if (item.FacilityID != null)
                    //{
                    //    dtMileageClaimVM.FacilityName = _repository.MstFacility.GetFacilityByIdAsync(item.FacilityID).GetAwaiter().GetResult().FacilityName.ToString();
                    //}
                    ////Need to change to not null
                    //if (item.FromFacilityID != 0)
                    //{
                    //    dtMileageClaimVM.FromFacilityName = _repository.MstFacility.GetFacilityByIdAsync(item.FromFacilityID).GetAwaiter().GetResult().FacilityName.ToString();
                    //}
                    ////Need to change to not null
                    //if (item.ToFacilityID != 0)
                    //{
                    //    dtMileageClaimVM.ToFacilityName = _repository.MstFacility.GetFacilityByIdAsync(item.ToFacilityID).GetAwaiter().GetResult().FacilityName.ToString();
                    //}

                    pVCClaimDetailVM.DtPVCClaimVMs.Add(dtPVCClaimVM);
                }
                pVCClaimDetailVM.DtPVCClaimSummaries = dtPVCSummaries;
                var GroupByQS = pVCClaimDetailVM.DtPVCClaimVMs.GroupBy(s => s.ExpenseCategoryID);
                //var GroupByQS = (from std in pVCClaimDetailVM.DtPVCClaimVMs
                //                                                           group std by std.PVCCategoryID);


                pVCClaimDetailVM.PVCClaimAudits = new List<PVCClaimAuditVM>();

                var dtPVCClaimAudits = await _repository.MstPVCClaimAudit.GetMstPVCClaimAuditByIdAsync(id);

                foreach (var item in dtPVCClaimAudits)
                {
                    PVCClaimAuditVM mstPVCClaimAuditVM = new PVCClaimAuditVM();
                    mstPVCClaimAuditVM.Action = item.Action;
                    mstPVCClaimAuditVM.Description = item.Description;
                    mstPVCClaimAuditVM.AuditDateTickle = Helper.RelativeDate(item.AuditDate);
                    pVCClaimDetailVM.PVCClaimAudits.Add(mstPVCClaimAuditVM);
                }

                pVCClaimDetailVM.PVCClaimFileUploads = new List<DtPVCClaimFileUpload>();

                pVCClaimDetailVM.PVCClaimFileUploads = _repository.DtPVCClaimFileUpload.GetDtPVCClaimAuditByIdAsync(id).Result.ToList();

                PVCClaimVM pVCClaimVM = new PVCClaimVM();
                //pVCClaimVM.ClaimType = mstPVCClaim.ClaimType;
                pVCClaimVM.GrandTotal = mstPVCClaim.GrandTotal;
                pVCClaimVM.TotalAmount = mstPVCClaim.TotalAmount;
                pVCClaimVM.Company = mstPVCClaim.Company;
                pVCClaimVM.Name = mstPVCClaim.MstUser.Name;
                pVCClaimVM.DepartmentName = mstPVCClaim.MstDepartment.Department;
                pVCClaimVM.FacilityName = mstPVCClaim.MstFacility.FacilityName;
                pVCClaimVM.CreatedDate = Convert.ToDateTime(mstPVCClaim.CreatedDate).ToString("dd/MM/yyyy", System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
                pVCClaimVM.Verifier = mstPVCClaim.Verifier;
                pVCClaimVM.Approver = mstPVCClaim.Approver;
                pVCClaimVM.PVCCNo = mstPVCClaim.PVCCNo;
                pVCClaimVM.VoucherNo = mstPVCClaim.VoucherNo;
                ViewBag.PVCCID = id;
                pVCClaimDetailVM.PVCClaimVM = pVCClaimVM;
                //mileageClaimDetailVM.DtMileageClaimVMs = dtMileageClaimVMs;
            }
            return PartialView("GetPVCCDetailsPrint", pVCClaimDetailVM);
        }

        public async Task<IActionResult> GetPrint(string data)
        {
            var hodClaimSearch = JsonConvert.DeserializeObject<HodClaimSearch>(data);

            var mstMileageClaimsWithDetails = await _repository.MstUser.GetAllHODSummaryClaimsAsync(Convert.ToInt32(HttpContext.User.FindFirst("userid").Value), hodClaimSearch.ModuleName, hodClaimSearch.StatusID, hodClaimSearch.FromDate, hodClaimSearch.ToDate);
            List<CustomClaim> mileageClaimVMs = new List<CustomClaim>();

            foreach (var mc in mstMileageClaimsWithDetails)
            {
                CustomClaim mileageClaimVM = new CustomClaim();

                mileageClaimVM.CNO = mc.CNO;
                mileageClaimVM.Name = mc.Name;
                mileageClaimVM.CreatedDate = mc.CreatedDate; //Convert.ToDateTime(mc.CreatedDate).ToString("dd/MM/yyyy", System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
                mileageClaimVM.FacilityName = mc.FacilityName;
                mileageClaimVM.Name = mc.Name;
                mileageClaimVM.Phone = mc.Phone;
                mileageClaimVM.GrandTotal = mc.GrandTotal;
                mileageClaimVM.ApprovalStatus = mc.ApprovalStatus;

                if (mc.ApprovalStatus == 1)
                {
                    mileageClaimVM.ClaimStatusName = "Awaiting Verification";

                }
                else if (mc.ApprovalStatus == 2)
                {
                    mileageClaimVM.ClaimStatusName = "Awaiting Signatory approval";

                }
                else if (mc.ApprovalStatus == 3)
                {
                    mileageClaimVM.ClaimStatusName = "Approved";

                }
                else if (mc.ApprovalStatus == 4)
                {
                    mileageClaimVM.ClaimStatusName = "Request to Amend";
                }
                else if (mc.ApprovalStatus == 5)
                {
                    mileageClaimVM.ClaimStatusName = "Voided";

                }
                else if (mc.ApprovalStatus == -5)
                {
                    mileageClaimVM.ClaimStatusName = "Requested to Void";

                }
                else if (mc.ApprovalStatus == 6)
                {
                    mileageClaimVM.ClaimStatusName = "Awaiting approval";

                }
                else if (mc.ApprovalStatus == 7)
                {
                    mileageClaimVM.ClaimStatusName = "Awaiting HOD approval";

                }
                else if (mc.ApprovalStatus == 9)
                {
                    mileageClaimVM.ClaimStatusName = "Exported to AccPac";

                }
                else if (mc.ApprovalStatus == 10)
                {
                    mileageClaimVM.ClaimStatusName = "Exported to Bank";

                }
                else
                {
                    mileageClaimVM.ClaimStatusName = "New";
                }

                if (mc.UserApprovers != "")
                {
                    mileageClaimVM.Approver = mc.UserApprovers.Split(',').First();
                    if (mileageClaimVM.Approver == HttpContext.User.FindFirst("userid").Value && (mileageClaimVM.ApprovalStatus == 6))
                    {
                        mileageClaimVM.IsActionAllowed = true;
                    }
                }
                else if (mc.Verifier != "")
                {
                    mileageClaimVM.Approver = mc.Verifier.Split(',').First();
                    if (mileageClaimVM.Approver == HttpContext.User.FindFirst("userid").Value && (mileageClaimVM.ApprovalStatus == 1 || mileageClaimVM.ApprovalStatus == 2))
                    {
                        mileageClaimVM.IsActionAllowed = true;
                    }

                    //string VerifierIDs = string.Join(",", MileageverifierIDs.Skip(1));
                }
                else if (mc.HODApprover != "")
                {
                    mileageClaimVM.Approver = mc.HODApprover.Split(',').First();
                    if (mileageClaimVM.Approver == HttpContext.User.FindFirst("userid").Value && (mileageClaimVM.ApprovalStatus == 7))
                    {
                        mileageClaimVM.IsActionAllowed = true;
                    }
                }
                else if (mc.Approver != "")
                {
                    mileageClaimVM.Approver = mc.Approver.Split(',').First();
                    if (mileageClaimVM.Approver == HttpContext.User.FindFirst("userid").Value && (mileageClaimVM.ApprovalStatus == 1 || mileageClaimVM.ApprovalStatus == 2))
                    {
                        mileageClaimVM.IsActionAllowed = true;
                    }
                }
                else
                {
                    mileageClaimVM.Approver = "";
                }

                if (mileageClaimVM.Approver != "")
                {
                    var mstUserApprover = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(mileageClaimVM.Approver));
                    mileageClaimVM.Approver = mstUserApprover.Name;
                }
                mileageClaimVMs.Add(mileageClaimVM);
            }
            return PartialView("GetSummaryPrint", mileageClaimVMs);
        }
        public async Task<IActionResult> DownloadFile(string fileName)
        {
            MemoryStream ms = new MemoryStream();
            if (CloudStorageAccount.TryParse(_configuration.GetSection("ConnectionStrings")["BlobConnectionString"], out CloudStorageAccount storageAccount))
            {
                CloudBlobClient BlobClient = storageAccount.CreateCloudBlobClient();
                CloudBlobContainer container = BlobClient.GetContainerReference(_configuration.GetSection("ConnectionStrings")["BlobContainerName"]);

                if (await container.ExistsAsync())
                {
                    CloudBlob file = container.GetBlobReference(fileName);

                    if (await file.ExistsAsync())
                    {
                        await file.DownloadToStreamAsync(ms);
                        Stream blobStream = file.OpenReadAsync().Result;
                        return File(blobStream, file.Properties.ContentType, "HODClaims-Export.xlsx");
                    }
                    else
                    {
                        return Content("File does not exist");
                    }
                }
                else
                {
                    return Content("Container does not exist");
                }
            }
            else
            {
                return Content("Error opening storage");
            }
        }
        public async Task<IActionResult> Download(string id, string name, string claim)
        {
            if (claim != "" && claim.Contains("ec"))
            {
                MemoryStream ms = new MemoryStream();
                if (CloudStorageAccount.TryParse(_configuration.GetSection("ConnectionStrings")["BlobConnectionString"], out CloudStorageAccount storageAccount))
                {
                    CloudBlobClient BlobClient = storageAccount.CreateCloudBlobClient();
                    CloudBlobContainer container = BlobClient.GetContainerReference(_configuration.GetSection("ConnectionStrings")["BlobContainerName"]);

                    if (await container.ExistsAsync())
                    {
                        CloudBlob file = container.GetBlobReference("FileUploads/ExpenseClaimFiles/" + id);

                        if (await file.ExistsAsync())
                        {
                            await file.DownloadToStreamAsync(ms);
                            Stream blobStream = file.OpenReadAsync().Result;
                            return File(blobStream, file.Properties.ContentType, name);
                        }
                        else
                        {
                            return Content("File does not exist");
                        }
                    }
                    else
                    {
                        return Content("Container does not exist");
                    }
                }
                else
                {
                    return Content("Error opening storage");
                }
            }
            else if (claim != "" && claim.Contains("tbc"))
            {
                MemoryStream ms = new MemoryStream();
                if (CloudStorageAccount.TryParse(_configuration.GetSection("ConnectionStrings")["BlobConnectionString"], out CloudStorageAccount storageAccount))
                {
                    CloudBlobClient BlobClient = storageAccount.CreateCloudBlobClient();
                    CloudBlobContainer container = BlobClient.GetContainerReference(_configuration.GetSection("ConnectionStrings")["BlobContainerName"]);

                    if (await container.ExistsAsync())
                    {
                        CloudBlob file = container.GetBlobReference("FileUploads/TBClaimFiles/" + id);

                        if (await file.ExistsAsync())
                        {
                            await file.DownloadToStreamAsync(ms);
                            Stream blobStream = file.OpenReadAsync().Result;
                            return File(blobStream, file.Properties.ContentType, name);
                        }
                        else
                        {
                            return Content("File does not exist");
                        }
                    }
                    else
                    {
                        return Content("Container does not exist");
                    }
                }
                else
                {
                    return Content("Error opening storage");
                }
            }
            else if (claim != "" && claim.Contains("pvc"))
            {
                MemoryStream ms = new MemoryStream();
                if (CloudStorageAccount.TryParse(_configuration.GetSection("ConnectionStrings")["BlobConnectionString"], out CloudStorageAccount storageAccount))
                {
                    CloudBlobClient BlobClient = storageAccount.CreateCloudBlobClient();
                    CloudBlobContainer container = BlobClient.GetContainerReference(_configuration.GetSection("ConnectionStrings")["BlobContainerName"]);

                    if (await container.ExistsAsync())
                    {
                        CloudBlob file = container.GetBlobReference("FileUploads/PVCClaimFiles/" + id);

                        if (await file.ExistsAsync())
                        {
                            await file.DownloadToStreamAsync(ms);
                            Stream blobStream = file.OpenReadAsync().Result;
                            return File(blobStream, file.Properties.ContentType, name);
                        }
                        else
                        {
                            return Content("File does not exist");
                        }
                    }
                    else
                    {
                        return Content("Container does not exist");
                    }
                }
                else
                {
                    return Content("Error opening storage");
                }
            }
            else if (claim != "" && claim.Contains("pvg"))
            {
                MemoryStream ms = new MemoryStream();
                if (CloudStorageAccount.TryParse(_configuration.GetSection("ConnectionStrings")["BlobConnectionString"], out CloudStorageAccount storageAccount))
                {
                    CloudBlobClient BlobClient = storageAccount.CreateCloudBlobClient();
                    CloudBlobContainer container = BlobClient.GetContainerReference(_configuration.GetSection("ConnectionStrings")["BlobContainerName"]);

                    if (await container.ExistsAsync())
                    {
                        CloudBlob file = container.GetBlobReference("FileUploads/PVGClaimFiles/" + id);

                        if (await file.ExistsAsync())
                        {
                            await file.DownloadToStreamAsync(ms);
                            Stream blobStream = file.OpenReadAsync().Result;
                            return File(blobStream, file.Properties.ContentType, name);
                        }
                        else
                        {
                            return Content("File does not exist");
                        }
                    }
                    else
                    {
                        return Content("Container does not exist");
                    }
                }
                else
                {
                    return Content("Error opening storage");
                }
            }
            else
            {
                MemoryStream ms = new MemoryStream();
                if (CloudStorageAccount.TryParse(_configuration.GetSection("ConnectionStrings")["BlobConnectionString"], out CloudStorageAccount storageAccount))
                {
                    CloudBlobClient BlobClient = storageAccount.CreateCloudBlobClient();
                    CloudBlobContainer container = BlobClient.GetContainerReference(_configuration.GetSection("ConnectionStrings")["BlobContainerName"]);

                    if (await container.ExistsAsync())
                    {
                        CloudBlob file = container.GetBlobReference("FileUploads/MileageClaimFiles/" + id);

                        if (await file.ExistsAsync())
                        {
                            await file.DownloadToStreamAsync(ms);
                            Stream blobStream = file.OpenReadAsync().Result;
                            return File(blobStream, file.Properties.ContentType, name);
                        }
                        else
                        {
                            return Content("File does not exist");
                        }
                    }
                    else
                    {
                        return Content("Container does not exist");
                    }
                }
                else
                {
                    return Content("Error opening storage");
                }
            }
        }

        #region -- SendMessage --
        public async Task<JsonResult> AddMessage(string data)
        {
            var queryParamViewModel = JsonConvert.DeserializeObject<QueryParam>(data);
            if (queryParamViewModel.Claim != "" && queryParamViewModel.Claim.Contains("ec"))
            {
                var UserIds = queryParamViewModel.RecieverId.Select(s => int.Parse(s)).ToArray();
                if (HttpContext.User.FindFirst("userid").Value != null)
                {
                    var result = "";
                    try
                    {
                        long ECID = Convert.ToInt64(queryParamViewModel.Cid);
                        int UserID = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value);
                        // newly Added Code
                        var expenseClaim = await _repository.MstExpenseClaim.GetExpenseClaimByIdAsync(ECID);
                        for (int i = 0; i < UserIds.Length; i++)
                        {
                            MstQuery clsdtExpenseQuery = new MstQuery();
                            // if (data["MessageDescription"] != null)               
                            clsdtExpenseQuery.ModuleType = "Expense Claim";
                            //  clsdtSupplierQuery.ID = Convert.ToInt64(data["SPOID"]);
                            clsdtExpenseQuery.ID = ECID;
                            clsdtExpenseQuery.SenderID = UserID;
                            //var recieverId = data["queryusers"];       
                            clsdtExpenseQuery.ReceiverID = Convert.ToInt32(UserIds[i]);
                            clsdtExpenseQuery.MessageDescription = queryParamViewModel.Message;
                            clsdtExpenseQuery.SentTime = DateTime.Now;
                            //clsdtExpenseQuery.NotificationStatus = false;
                            await _repository.MstQuery.CreateQuery(clsdtExpenseQuery);
                            //await _repository.SaveAsync();
                            //objERPEntities.AddToMstQueries(clsdtSupplierQuery);
                            //objERPEntities.SaveChanges();
                            result = "Success";

                            var receiver = await _repository.MstUser.GetUserByIdAsync(UserIds[i]);
                            //var reciever = objERPEntities.MstUsers.ToList().Where(p => p.UserID == Convert.ToInt32(UserIds[i]) && p.InstanceID == int.Parse(Session["InstanceID"].ToString())).ToList().FirstOrDefault();
                            MstExpenseClaimAudit auditUpdate = new MstExpenseClaimAudit();
                            auditUpdate.ECID = ECID;
                            auditUpdate.Action = "0";
                            auditUpdate.AuditDate = DateTime.Now;
                            auditUpdate.AuditBy = UserID;
                            //auditUpdate.InstanceID = 1;
                            string time = DateTime.Now.ToString("tt", System.Globalization.CultureInfo.InvariantCulture);
                            DateTime date = DateTime.Now;
                            string formattedDate = date.ToString("dd'/'MM'/'yyyy hh:mm:ss");
                            auditUpdate.Description = "" + User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.GivenName).Value.ToString() + " Sent Query to " + receiver.Name + " on " + formattedDate + " " + time + " ";
                            auditUpdate.SentTo = receiver.Name;
                            await _repository.MstExpenseClaimAudit.CreateExpenseClaimAudit(auditUpdate);
                            await _repository.SaveAsync();

                            string domainUrl = HttpContext.Request.Scheme + "://" + HttpContext.Request.Host;
                            string clickUrl = string.Empty;

                            if (expenseClaim.CreatedBy.ToString().Contains(UserIds[i].ToString()))
                                clickUrl = domainUrl + "/" + "ExpenseClaim/Details/" + ECID;
                            else if (expenseClaim.DApprover.Contains(UserIds[i].ToString()) || expenseClaim.DVerifier.Contains(UserIds[i].ToString()))
                                clickUrl = domainUrl + "/" + "FinanceExpenseClaim/Details/" + ECID;
                            else
                                clickUrl = domainUrl + "/" + "HodSummary/ECDetails/" + ECID;
                            //if (expenseClaim.DUserApprovers.Contains(UserIds[i].ToString()) || expenseClaim.DHODApprover.Contains(UserIds[i].ToString()))

                            //var mstSenderDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(HttpContext.User.FindFirst("userid").Value));
                            var senderName = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.GivenName).Value.ToString();
                            //var mstVerifierDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(approverID));
                            var toEmail = receiver.EmailAddress;
                            var receiverName = receiver.Name;
                            var claimNo = expenseClaim.ECNo;
                            var screen = "Expense Claim";
                            var approvalType = "Query";
                            int userID = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value);
                            var subject = "Expense Claim Query " + claimNo;
                            BackgroundJob.Enqueue(() => _sendMailServices.SendEmail("QueryTemplate.html", screen, subject, senderName, receiverName, claimNo, approvalType, userID, toEmail, clickUrl, string.Empty, string.Empty, queryParamViewModel.Message));
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Something went wrong inside CreateExpenseClaimAudit action: {ex.Message}");
                    }
                    return Json(result);
                }
                else
                {
                    return Json(new { res = "Yes" });
                }
                //return RedirectToAction("IndexSG", new RouteValueDictionary(
                //    new { controller ="ViewSupplierPurchaseOrderDetails", action = "IndexSG", Id = clsdtSupplierQuery.ID }));
            }
            else if (queryParamViewModel.Claim != "" && queryParamViewModel.Claim.Contains("tbc"))
            {
                var UserIds = queryParamViewModel.RecieverId.Select(s => int.Parse(s)).ToArray();
                if (HttpContext.User.FindFirst("userid").Value != null)
                {
                    var result = "";
                    try
                    {
                        long TBCID = Convert.ToInt64(queryParamViewModel.Cid);
                        int UserID = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value);
                        // newly Added Code
                        var tbClaim = await _repository.MstTBClaim.GetTBClaimByIdAsync(TBCID);
                        for (int i = 0; i < UserIds.Length; i++)
                        {
                            MstQuery clsdtTBQuery = new MstQuery();
                            // if (data["MessageDescription"] != null)               
                            clsdtTBQuery.ModuleType = "TelephoneBill Claim";
                            //  clsdtSupplierQuery.ID = Convert.ToInt64(data["SPOID"]);
                            clsdtTBQuery.ID = TBCID;
                            clsdtTBQuery.SenderID = UserID;
                            //var recieverId = data["queryusers"];       
                            clsdtTBQuery.ReceiverID = Convert.ToInt32(UserIds[i]);
                            clsdtTBQuery.MessageDescription = queryParamViewModel.Message;
                            clsdtTBQuery.SentTime = DateTime.Now;
                            //clsdtExpenseQuery.NotificationStatus = false;
                            await _repository.MstQuery.CreateQuery(clsdtTBQuery);
                            //await _repository.SaveAsync();
                            //objERPEntities.AddToMstQueries(clsdtSupplierQuery);
                            //objERPEntities.SaveChanges();
                            result = "Success";

                            var receiver = await _repository.MstUser.GetUserByIdAsync(UserIds[i]);
                            //var reciever = objERPEntities.MstUsers.ToList().Where(p => p.UserID == Convert.ToInt32(UserIds[i]) && p.InstanceID == int.Parse(Session["InstanceID"].ToString())).ToList().FirstOrDefault();
                            MstTBClaimAudit auditUpdate = new MstTBClaimAudit();
                            auditUpdate.TBCID = TBCID;
                            auditUpdate.Action = "0";
                            auditUpdate.AuditDate = DateTime.Now;
                            auditUpdate.AuditBy = UserID;
                            //auditUpdate.InstanceID = 1;
                            string time = DateTime.Now.ToString("tt", System.Globalization.CultureInfo.InvariantCulture);
                            DateTime date = DateTime.Now;
                            string formattedDate = date.ToString("dd'/'MM'/'yyyy hh:mm:ss");
                            auditUpdate.Description = "" + User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.GivenName).Value.ToString() + " Sent Query to " + receiver.Name + " on " + formattedDate + " " + time + " ";
                            auditUpdate.SentTo = receiver.Name;
                            await _repository.MstTBClaimAudit.CreateTBClaimAudit(auditUpdate);
                            await _repository.SaveAsync();

                            string domainUrl = HttpContext.Request.Scheme + "://" + HttpContext.Request.Host;
                            string clickUrl = string.Empty;

                            if (tbClaim.CreatedBy.ToString().Contains(UserIds[i].ToString()))
                                clickUrl = domainUrl + "/" + "TelephoneBillClaim/Details/" + TBCID;
                            else if (tbClaim.DApprover.Contains(UserIds[i].ToString()) || tbClaim.DVerifier.Contains(UserIds[i].ToString()))
                                clickUrl = domainUrl + "/" + "FinanceTBClaim/Details/" + TBCID;
                            else
                                clickUrl = domainUrl + "/" + "HodSummary/TBCDetails/" + TBCID;
                            //if (tbClaim.DUserApprovers.Contains(UserIds[i].ToString()) || tbClaim.DHODApprover.Contains(UserIds[i].ToString()))

                            //var mstSenderDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(HttpContext.User.FindFirst("userid").Value));
                            var senderName = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.GivenName).Value.ToString();
                            //var mstVerifierDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(approverID));
                            var toEmail = receiver.EmailAddress;
                            var receiverName = receiver.Name;
                            var claimNo = tbClaim.TBCNo;
                            var screen = "Telephone Bill Claim";
                            var approvalType = "Query";
                            int userID = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value);
                            var subject = "Telephone Bill Claim Query " + claimNo;
                            BackgroundJob.Enqueue(() => _sendMailServices.SendEmail("QueryTemplate.html", screen, subject, senderName, receiverName, claimNo, approvalType, userID, toEmail, clickUrl, string.Empty, string.Empty, queryParamViewModel.Message));

                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Something went wrong inside CreateExpenseClaimAudit action: {ex.Message}");
                    }
                    return Json(result);
                }
                else
                {
                    return Json(new { res = "Yes" });
                }
                //return RedirectToAction("IndexSG", new RouteValueDictionary(
                //    new { controller ="ViewSupplierPurchaseOrderDetails", action = "IndexSG", Id = clsdtSupplierQuery.ID }));
            }
            else if (queryParamViewModel.Claim != "" && queryParamViewModel.Claim.Contains("pvc"))
            {
                var UserIds = queryParamViewModel.RecieverId.Select(s => int.Parse(s)).ToArray();
                if (HttpContext.User.FindFirst("userid").Value != null)
                {
                    var result = "";
                    try
                    {
                        long PVCCID = Convert.ToInt64(queryParamViewModel.Cid);
                        int UserID = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value);
                        // newly Added Code
                        var pVCClaim = await _repository.MstPVCClaim.GetPVCClaimByIdAsync(PVCCID);
                        for (int i = 0; i < UserIds.Length; i++)
                        {
                            MstQuery clsdtPVCQuery = new MstQuery();
                            // if (data["MessageDescription"] != null)               
                            clsdtPVCQuery.ModuleType = "PVC Claim";
                            //  clsdtSupplierQuery.ID = Convert.ToInt64(data["SPOID"]);
                            clsdtPVCQuery.ID = PVCCID;
                            clsdtPVCQuery.SenderID = UserID;
                            //var recieverId = data["queryusers"];       
                            clsdtPVCQuery.ReceiverID = Convert.ToInt32(UserIds[i]);
                            clsdtPVCQuery.MessageDescription = queryParamViewModel.Message;
                            clsdtPVCQuery.SentTime = DateTime.Now;
                            //clsdtPVCQuery.NotificationStatus = false;
                            await _repository.MstQuery.CreateQuery(clsdtPVCQuery);
                            //await _repository.SaveAsync();
                            //objERPEntities.AddToMstQueries(clsdtSupplierQuery);
                            //objERPEntities.SaveChanges();
                            result = "Success";

                            var receiver = await _repository.MstUser.GetUserByIdAsync(UserIds[i]);
                            //var reciever = objERPEntities.MstUsers.ToList().Where(p => p.UserID == Convert.ToInt32(UserIds[i]) && p.InstanceID == int.Parse(Session["InstanceID"].ToString())).ToList().FirstOrDefault();
                            MstPVCClaimAudit auditUpdate = new MstPVCClaimAudit();
                            auditUpdate.PVCCID = PVCCID;
                            auditUpdate.Action = "0";
                            auditUpdate.AuditDate = DateTime.Now;
                            auditUpdate.AuditBy = UserID;
                            //auditUpdate.InstanceID = 1;
                            string time = DateTime.Now.ToString("tt", System.Globalization.CultureInfo.InvariantCulture);
                            DateTime date = DateTime.Now;
                            string formattedDate = date.ToString("dd'/'MM'/'yyyy hh:mm:ss");
                            auditUpdate.Description = "" + User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.GivenName).Value.ToString() + " Sent Query to " + receiver.Name + " on " + formattedDate + " " + time + " ";
                            auditUpdate.SentTo = receiver.Name;
                            await _repository.MstPVCClaimAudit.CreatePVCClaimAudit(auditUpdate);
                            await _repository.SaveAsync();

                            string domainUrl = HttpContext.Request.Scheme + "://" + HttpContext.Request.Host;
                            string clickUrl = string.Empty;

                            if (pVCClaim.CreatedBy.ToString().Contains(UserIds[i].ToString()))
                                clickUrl = domainUrl + "/" + "PVChequeClaim/Details/" + PVCCID;
                            else if (pVCClaim.DApprover.Contains(UserIds[i].ToString()) || pVCClaim.DVerifier.Contains(UserIds[i].ToString()))
                                clickUrl = domainUrl + "/" + "FinancePVCClaim/Details/" + PVCCID;
                            else
                                clickUrl = domainUrl + "/" + "HodSummary/PVCCDetails/" + PVCCID;
                            //if (pVCClaim.DUserApprovers.Contains(UserIds[i].ToString()) || pVCClaim.DHODApprover.Contains(UserIds[i].ToString()))

                            //var mstSenderDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(HttpContext.User.FindFirst("userid").Value));
                            var senderName = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.GivenName).Value.ToString();
                            //var mstVerifierDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(approverID));
                            var toEmail = receiver.EmailAddress;
                            var receiverName = receiver.Name;
                            var claimNo = pVCClaim.PVCCNo;
                            var screen = "PV-Cheque Claim";
                            var approvalType = "Query";
                            int userID = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value);
                            var subject = "PV-Cheque Claim Query " + claimNo;
                            BackgroundJob.Enqueue(() => _sendMailServices.SendEmail("QueryTemplate.html", screen, subject, senderName, receiverName, claimNo, approvalType, userID, toEmail, clickUrl, string.Empty, string.Empty, queryParamViewModel.Message));

                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Something went wrong inside CreatePVCClaimAudit action: {ex.Message}");
                    }
                    return Json(result);
                }
                else
                {
                    return Json(new { res = "Yes" });
                }
            }
            else if (queryParamViewModel.Claim != "" && queryParamViewModel.Claim.Contains("pvg"))
            {
                var UserIds = queryParamViewModel.RecieverId.Select(s => int.Parse(s)).ToArray();
                if (HttpContext.User.FindFirst("userid").Value != null)
                {
                    var result = "";
                    try
                    {
                        long PVGCID = Convert.ToInt64(queryParamViewModel.Cid);
                        int UserID = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value);
                        // newly Added Code
                        var pVGClaim = await _repository.MstPVGClaim.GetPVGClaimByIdAsync(PVGCID);
                        for (int i = 0; i < UserIds.Length; i++)
                        {
                            MstQuery clsdtPVGQuery = new MstQuery();
                            // if (data["MessageDescription"] != null)               
                            clsdtPVGQuery.ModuleType = "PVG Claim";
                            //  clsdtSupplierQuery.ID = Convert.ToInt64(data["SPOID"]);
                            clsdtPVGQuery.ID = PVGCID;
                            clsdtPVGQuery.SenderID = UserID;
                            //var recieverId = data["queryusers"];       
                            clsdtPVGQuery.ReceiverID = Convert.ToInt32(UserIds[i]);
                            clsdtPVGQuery.MessageDescription = queryParamViewModel.Message;
                            clsdtPVGQuery.SentTime = DateTime.Now;
                            //clsdtPVGQuery.NotificationStatus = false;
                            await _repository.MstQuery.CreateQuery(clsdtPVGQuery);
                            //await _repository.SaveAsync();
                            //objERPEntities.AddToMstQueries(clsdtSupplierQuery);
                            //objERPEntities.SaveChanges();
                            result = "Success";

                            var receiver = await _repository.MstUser.GetUserByIdAsync(UserIds[i]);
                            //var reciever = objERPEntities.MstUsers.ToList().Where(p => p.UserID == Convert.ToInt32(UserIds[i]) && p.InstanceID == int.Parse(Session["InstanceID"].ToString())).ToList().FirstOrDefault();
                            MstPVGClaimAudit auditUpdate = new MstPVGClaimAudit();
                            auditUpdate.PVGCID = PVGCID;
                            auditUpdate.Action = "0";
                            auditUpdate.AuditDate = DateTime.Now;
                            auditUpdate.AuditBy = UserID;
                            //auditUpdate.InstanceID = 1;
                            string time = DateTime.Now.ToString("tt", System.Globalization.CultureInfo.InvariantCulture);
                            DateTime date = DateTime.Now;
                            string formattedDate = date.ToString("dd'/'MM'/'yyyy hh:mm:ss");
                            auditUpdate.Description = "" + User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.GivenName).Value.ToString() + " Sent Query to " + receiver.Name + " on " + formattedDate + " " + time + " ";
                            auditUpdate.SentTo = receiver.Name;
                            await _repository.MstPVGClaimAudit.CreatePVGClaimAudit(auditUpdate);
                            await _repository.SaveAsync();

                            string domainUrl = HttpContext.Request.Scheme + "://" + HttpContext.Request.Host;
                            string clickUrl = string.Empty;

                            if (pVGClaim.CreatedBy.ToString().Contains(UserIds[i].ToString()))
                                clickUrl = domainUrl + "/" + "PVGIROClaim/Details/" + PVGCID;
                            else if (pVGClaim.DApprover.Contains(UserIds[i].ToString()) || pVGClaim.DVerifier.Contains(UserIds[i].ToString()))
                                clickUrl = domainUrl + "/" + "FinancePVGClaim/Details/" + PVGCID;
                            else
                                clickUrl = domainUrl + "/" + "HodSummary/PVGCDetails/" + PVGCID;
                            //if (pVGClaim.DUserApprovers.Contains(UserIds[i].ToString()) || pVGClaim.DHODApprover.Contains(UserIds[i].ToString()))

                            //var mstSenderDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(HttpContext.User.FindFirst("userid").Value));
                            var senderName = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.GivenName).Value.ToString();
                            //var mstVerifierDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(approverID));
                            var toEmail = receiver.EmailAddress;
                            var receiverName = receiver.Name;
                            var claimNo = pVGClaim.PVGCNo;
                            var screen = "PV-GIRO Claim";
                            var approvalType = "Query";
                            int userID = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value);
                            var subject = "PV-GIRO Claim Query " + claimNo;
                            BackgroundJob.Enqueue(() => _sendMailServices.SendEmail("QueryTemplate.html", screen, subject, senderName, receiverName, claimNo, approvalType, userID, toEmail, clickUrl, string.Empty, string.Empty, queryParamViewModel.Message));

                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Something went wrong inside CreatePVGClaimAudit action: {ex.Message}");
                    }
                    return Json(result);
                }
                else
                {
                    return Json(new { res = "Yes" });
                }
            }
            else
            {
                var UserIds = queryParamViewModel.RecieverId.Select(s => int.Parse(s)).ToArray();
                if (HttpContext.User.FindFirst("userid").Value != null)
                {
                    var result = "";
                    try
                    {
                        long MCID = Convert.ToInt64(queryParamViewModel.Cid);
                        int UserID = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value);
                        // newly Added Code
                        var mileageClaim = await _repository.MstMileageClaim.GetMileageClaimByIdAsync(MCID);
                        for (int i = 0; i < UserIds.Length; i++)
                        {
                            MstQuery clsdtMileageQuery = new MstQuery();
                            // if (data["MessageDescription"] != null)               
                            clsdtMileageQuery.ModuleType = "Mileage Claim";
                            //  clsdtSupplierQuery.ID = Convert.ToInt64(data["SPOID"]);
                            clsdtMileageQuery.ID = MCID;
                            clsdtMileageQuery.SenderID = UserID;
                            //var recieverId = data["queryusers"];       
                            clsdtMileageQuery.ReceiverID = Convert.ToInt32(UserIds[i]);
                            clsdtMileageQuery.MessageDescription = queryParamViewModel.Message;
                            clsdtMileageQuery.SentTime = DateTime.Now;
                            //clsdtMileageQuery.NotificationStatus = false;
                            await _repository.MstQuery.CreateQuery(clsdtMileageQuery);
                            //await _repository.SaveAsync();
                            //objERPEntities.AddToMstQueries(clsdtSupplierQuery);
                            //objERPEntities.SaveChanges();
                            result = "Success";

                            var receiver = await _repository.MstUser.GetUserByIdAsync(UserIds[i]);
                            //var reciever = objERPEntities.MstUsers.ToList().Where(p => p.UserID == Convert.ToInt32(UserIds[i]) && p.InstanceID == int.Parse(Session["InstanceID"].ToString())).ToList().FirstOrDefault();
                            MstMileageClaimAudit auditUpdate = new MstMileageClaimAudit();
                            auditUpdate.MCID = MCID;
                            auditUpdate.Action = "0";
                            auditUpdate.AuditDate = DateTime.Now;
                            auditUpdate.AuditBy = UserID;
                            //auditUpdate.InstanceID = 1;
                            string time = DateTime.Now.ToString("tt", System.Globalization.CultureInfo.InvariantCulture);
                            DateTime date = DateTime.Now;
                            string formattedDate = date.ToString("dd'/'MM'/'yyyy hh:mm:ss A");
                            auditUpdate.Description = "" + User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.GivenName).Value.ToString() + " Sent Query to " + receiver.Name + " on " + formattedDate + " " + time + " ";
                            auditUpdate.SentTo = receiver.Name;
                            await _repository.MstMileageClaimAudit.CreateMileageClaimAudit(auditUpdate);
                            await _repository.SaveAsync();

                            string domainUrl = HttpContext.Request.Scheme + "://" + HttpContext.Request.Host;
                            string clickUrl = string.Empty;

                            if (mileageClaim.CreatedBy.ToString().Contains(UserIds[i].ToString()))
                                clickUrl = domainUrl + "/" + "MileageClaim/Details/" + MCID;
                            else if (mileageClaim.DApprover.Contains(UserIds[i].ToString()) || mileageClaim.DVerifier.Contains(UserIds[i].ToString()))
                                clickUrl = domainUrl + "/" + "FinanceMileageClaim/Details/" + MCID;
                            else
                                clickUrl = domainUrl + "/" + "HodSummary/Details/" + MCID;
                            //if (mileageClaim.DUserApprovers.Contains(UserIds[i].ToString()) || mileageClaim.DHODApprover.Contains(UserIds[i].ToString()))

                            //var mstSenderDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(HttpContext.User.FindFirst("userid").Value));
                            var senderName = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.GivenName).Value.ToString();
                            //var mstVerifierDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(approverID));
                            var toEmail = receiver.EmailAddress;
                            var receiverName = receiver.Name;
                            var claimNo = mileageClaim.MCNo;
                            var screen = "Mileage Claim";
                            var approvalType = "Query";
                            int userID = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value);
                            var subject = "Mileage Claim Query " + claimNo;
                            BackgroundJob.Enqueue(() => _sendMailServices.SendEmail("QueryTemplate.html", screen, subject, senderName, receiverName, claimNo, approvalType, userID, toEmail, clickUrl, string.Empty, string.Empty, queryParamViewModel.Message));
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Something went wrong inside CreateDepartment action: {ex.Message}");
                    }
                    return Json(result);
                }
                else
                {
                    return Json(new { res = "Yes" });
                }
            }
        }
        #endregion SendMessage

        #region -- GetMessages --

        public async Task<JsonResult> GetMessages(string id, string claim)
        {
            if (claim != "" && claim.Contains("ec"))
            {
                try
                {
                    var result = new LinkedList<object>();

                    //   var spoid = Convert.ToInt64(Session["id"]);
                    var ecid = Convert.ToInt32(id);
                    int UserId = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value);
                    ViewBag.userID = UserId;
                    //var queries1 = _context.mstQuery.ToList().Where(j => j.ID == smcid && (j.SenderID == UserId || j.ReceiverID == UserId) && j.ModuleType.ToString().Trim() == "Expense Claim").OrderBy(j => j.SentTime);
                    var queries = await _repository.MstQuery.GetAllClaimsQueriesAsync(UserId, ecid, "Expense Claim");
                    //var queries = objERPEntities.MstQueries.ToList().Where(j => j.ID == spoid && (j.SenderID == UserId || j.ReceiverID == UserId) && j.ModuleType.ToString().Trim() == "Purchase Order").OrderBy(j => j.SentTime);
                    var mstUsers = await _repository.MstUser.GetAllUsersAsync("active");
                    var VarSuspect = (from s in queries
                                      join st in mstUsers
                                               on s.SenderID equals st.UserID
                                      select new Querydata
                                      {
                                          MsgID = s.MsgID,
                                          ModuleType = s.ModuleType.Trim(),
                                          ID = (long)s.ID,
                                          SenderID = (long)s.SenderID,
                                          RecieverID = (long)s.ReceiverID,
                                          //SentTime = Convert.ToDateTime(s.SentTime.ToString("dd-MM-yyyy HH:mm")),
                                          // SentTime = Convert.ToDateTime(s.SentTime.ToString("dd-MM-yyyy HH:mm"), CultureInfo.InvariantCulture),
                                          SentTime = DateTime.ParseExact(s.SentTime.ToString("dd-MM-yyyy HH:mm"), "dd-MM-yyyy HH:mm", CultureInfo.InvariantCulture),
                                          MessageDescription = s.MessageDescription,
                                          FullName = st.Name
                                      }).OrderBy(s => s.SentTime).ToList();

                    foreach (var message in VarSuspect)
                    {
                        DateTime strDate = (DateTime)message.SentTime;
                        var datadecimal = "";
                        string strDate1 = strDate.ToString("dd/MM/yyyy h:mm tt");

                        //var FullName = objERPEntities.MstUsers.ToList().Where(p => p.UserID == Convert.ToInt32(message.SenderID)).FirstOrDefault().FullName;
                        //var FullName =  _repository.MstUser.GetUserByIdAsync((int?)message.SenderID).GetAwaiter().GetResult().Name; 
                        var mstUserSender = await _repository.MstUser.GetUserByIdAsync((int?)message.SenderID);
                        var FullName = mstUserSender.Name;

                        //var DesignationID = objERPEntities.MstUsers.ToList().Where(p => p.UserID == Convert.ToInt32(message.SenderID)).FirstOrDefault().DesignationID;

                        //var Designation = objERPEntities.MstDesignations.ToList().Where(p => p.DesignationID == DesignationID).FirstOrDefault().Designation;
                        //var Designation = "";
                        //var FullName1 = objERPEntities.MstUsers.ToList().Where(p => p.UserID == Convert.ToInt32(message.RecieverID)).FirstOrDefault().FullName;
                        var mstUserReceiver = await _repository.MstUser.GetUserByIdAsync((int?)message.RecieverID);
                        var FullName1 = mstUserReceiver.Name;
                        //var Designation1 = "";
                        //var DesignationID1 = objERPEntities.MstUsers.ToList().Where(p => p.UserID == Convert.ToInt32(message.RecieverID)).FirstOrDefault().DesignationID;

                        //var Designation1 = objERPEntities.MstDesignations.ToList().Where(p => p.DesignationID == DesignationID1).FirstOrDefault().Designation;
                        if (message.SenderID == Convert.ToInt32(HttpContext.User.FindFirst("userid").Value))
                        {
                            datadecimal = "R";
                        }
                        else
                        {
                            datadecimal = "L";
                        }
                        result.AddLast(new { Username = FullName, Designation = FullName1, PostDateTime = strDate1, MessageBody = message.MessageDescription, Datadecimal = datadecimal }); //Datadecimal = message.Datadecimal
                                                                                                                                                                                            //result.AddLast(new { Username = FullName + " (" + Designation + ")", Designation = FullName1 + " (" + Designation1 + ")", PostDateTime = strDate1, MessageBody = message.MessageDescription, Datadecimal = datadecimal }); //Datadecimal = message.Datadecimal
                    }
                    return Json(new { SuspectData = result });
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Something went wrong inside CreateDepartment action: {ex.Message}");
                }
                return Json(null);
            }
            else if (claim != "" && claim.Contains("tbc"))
            {
                try
                {
                    var result = new LinkedList<object>();

                    //   var spoid = Convert.ToInt64(Session["id"]);
                    var tbcid = Convert.ToInt32(id);
                    int UserId = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value);
                    ViewBag.userID = UserId;
                    //var queries1 = _context.mstQuery.ToList().Where(j => j.ID == smcid && (j.SenderID == UserId || j.ReceiverID == UserId) && j.ModuleType.ToString().Trim() == "Expense Claim").OrderBy(j => j.SentTime);
                    var queries = await _repository.MstQuery.GetAllClaimsQueriesAsync(UserId, tbcid, "TelephoneBill Claim");
                    //var queries = objERPEntities.MstQueries.ToList().Where(j => j.ID == spoid && (j.SenderID == UserId || j.ReceiverID == UserId) && j.ModuleType.ToString().Trim() == "Purchase Order").OrderBy(j => j.SentTime);
                    var mstUsers = await _repository.MstUser.GetAllUsersAsync("active");
                    var VarSuspect = (from s in queries
                                      join st in mstUsers
                                               on s.SenderID equals st.UserID
                                      select new Querydata
                                      {
                                          MsgID = s.MsgID,
                                          ModuleType = s.ModuleType.Trim(),
                                          ID = (long)s.ID,
                                          SenderID = (long)s.SenderID,
                                          RecieverID = (long)s.ReceiverID,
                                          //SentTime = Convert.ToDateTime(s.SentTime.ToString("dd-MM-yyyy HH:mm")),
                                          // SentTime = Convert.ToDateTime(s.SentTime.ToString("dd-MM-yyyy HH:mm"), CultureInfo.InvariantCulture),
                                          SentTime = DateTime.ParseExact(s.SentTime.ToString("dd-MM-yyyy HH:mm"), "dd-MM-yyyy HH:mm", CultureInfo.InvariantCulture),
                                          MessageDescription = s.MessageDescription,
                                          FullName = st.Name
                                      }).OrderBy(s => s.SentTime).ToList();

                    foreach (var message in VarSuspect)
                    {
                        DateTime strDate = (DateTime)message.SentTime;
                        var datadecimal = "";
                        string strDate1 = strDate.ToString("dd/MM/yyyy h:mm tt");

                        //var FullName = objERPEntities.MstUsers.ToList().Where(p => p.UserID == Convert.ToInt32(message.SenderID)).FirstOrDefault().FullName;
                        //var FullName =  _repository.MstUser.GetUserByIdAsync((int?)message.SenderID).GetAwaiter().GetResult().Name; 
                        var mstUserSender = await _repository.MstUser.GetUserByIdAsync((int?)message.SenderID);
                        var FullName = mstUserSender.Name;

                        //var DesignationID = objERPEntities.MstUsers.ToList().Where(p => p.UserID == Convert.ToInt32(message.SenderID)).FirstOrDefault().DesignationID;

                        //var Designation = objERPEntities.MstDesignations.ToList().Where(p => p.DesignationID == DesignationID).FirstOrDefault().Designation;
                        //var Designation = "";
                        //var FullName1 = objERPEntities.MstUsers.ToList().Where(p => p.UserID == Convert.ToInt32(message.RecieverID)).FirstOrDefault().FullName;
                        var mstUserReceiver = await _repository.MstUser.GetUserByIdAsync((int?)message.RecieverID);
                        var FullName1 = mstUserReceiver.Name;
                        //var Designation1 = "";
                        //var DesignationID1 = objERPEntities.MstUsers.ToList().Where(p => p.UserID == Convert.ToInt32(message.RecieverID)).FirstOrDefault().DesignationID;

                        //var Designation1 = objERPEntities.MstDesignations.ToList().Where(p => p.DesignationID == DesignationID1).FirstOrDefault().Designation;
                        if (message.SenderID == Convert.ToInt32(HttpContext.User.FindFirst("userid").Value))
                        {
                            datadecimal = "R";
                        }
                        else
                        {
                            datadecimal = "L";
                        }
                        result.AddLast(new { Username = FullName, Designation = FullName1, PostDateTime = strDate1, MessageBody = message.MessageDescription, Datadecimal = datadecimal }); //Datadecimal = message.Datadecimal
                                                                                                                                                                                            //result.AddLast(new { Username = FullName + " (" + Designation + ")", Designation = FullName1 + " (" + Designation1 + ")", PostDateTime = strDate1, MessageBody = message.MessageDescription, Datadecimal = datadecimal }); //Datadecimal = message.Datadecimal
                    }
                    return Json(new { SuspectData = result });
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Something went wrong inside CreateDepartment action: {ex.Message}");
                }
                return Json(null);
            }
            else if (claim != "" && claim.Contains("pvc"))
            {
                try
                {
                    var result = new LinkedList<object>();

                    //   var spoid = Convert.ToInt64(Session["id"]);
                    var ecid = Convert.ToInt32(id);
                    int UserId = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value);
                    ViewBag.userID = UserId;
                    //var queries1 = _context.mstQuery.ToList().Where(j => j.ID == smcid && (j.SenderID == UserId || j.ReceiverID == UserId) && j.ModuleType.ToString().Trim() == "PVC Claim").OrderBy(j => j.SentTime);
                    var queries = await _repository.MstQuery.GetAllClaimsQueriesAsync(UserId, ecid, "PVC Claim");
                    //var queries = objERPEntities.MstQueries.ToList().Where(j => j.ID == spoid && (j.SenderID == UserId || j.ReceiverID == UserId) && j.ModuleType.ToString().Trim() == "Purchase Order").OrderBy(j => j.SentTime);
                    var mstUsers = await _repository.MstUser.GetAllUsersAsync("active");
                    var VarSuspect = (from s in queries
                                      join st in mstUsers
                                               on s.SenderID equals st.UserID
                                      select new Querydata
                                      {
                                          MsgID = s.MsgID,
                                          ModuleType = s.ModuleType.Trim(),
                                          ID = (long)s.ID,
                                          SenderID = (long)s.SenderID,
                                          RecieverID = (long)s.ReceiverID,
                                          //SentTime = Convert.ToDateTime(s.SentTime.ToString("dd-MM-yyyy HH:mm")),
                                          // SentTime = Convert.ToDateTime(s.SentTime.ToString("dd-MM-yyyy HH:mm"), CultureInfo.InvariantCulture),
                                          SentTime = DateTime.ParseExact(s.SentTime.ToString("dd-MM-yyyy HH:mm"), "dd-MM-yyyy HH:mm", CultureInfo.InvariantCulture),
                                          MessageDescription = s.MessageDescription,
                                          FullName = st.Name
                                      }).OrderBy(s => s.SentTime).ToList();

                    foreach (var message in VarSuspect)
                    {
                        DateTime strDate = (DateTime)message.SentTime;
                        var datadecimal = "";
                        string strDate1 = strDate.ToString("dd/MM/yyyy h:mm tt");

                        //var FullName = objERPEntities.MstUsers.ToList().Where(p => p.UserID == Convert.ToInt32(message.SenderID)).FirstOrDefault().FullName;
                        //var FullName =  _repository.MstUser.GetUserByIdAsync((int?)message.SenderID).GetAwaiter().GetResult().Name; 
                        var mstUserSender = await _repository.MstUser.GetUserByIdAsync((int?)message.SenderID);
                        var FullName = mstUserSender.Name;

                        //var DesignationID = objERPEntities.MstUsers.ToList().Where(p => p.UserID == Convert.ToInt32(message.SenderID)).FirstOrDefault().DesignationID;

                        //var Designation = objERPEntities.MstDesignations.ToList().Where(p => p.DesignationID == DesignationID).FirstOrDefault().Designation;
                        //var Designation = "";
                        //var FullName1 = objERPEntities.MstUsers.ToList().Where(p => p.UserID == Convert.ToInt32(message.RecieverID)).FirstOrDefault().FullName;
                        var mstUserReceiver = await _repository.MstUser.GetUserByIdAsync((int?)message.RecieverID);
                        var FullName1 = mstUserReceiver.Name;
                        //var Designation1 = "";
                        //var DesignationID1 = objERPEntities.MstUsers.ToList().Where(p => p.UserID == Convert.ToInt32(message.RecieverID)).FirstOrDefault().DesignationID;

                        //var Designation1 = objERPEntities.MstDesignations.ToList().Where(p => p.DesignationID == DesignationID1).FirstOrDefault().Designation;
                        if (message.SenderID == Convert.ToInt32(HttpContext.User.FindFirst("userid").Value))
                        {
                            datadecimal = "R";
                        }
                        else
                        {
                            datadecimal = "L";
                        }
                        result.AddLast(new { Username = FullName, Designation = FullName1, PostDateTime = strDate1, MessageBody = message.MessageDescription, Datadecimal = datadecimal }); //Datadecimal = message.Datadecimal
                                                                                                                                                                                            //result.AddLast(new { Username = FullName + " (" + Designation + ")", Designation = FullName1 + " (" + Designation1 + ")", PostDateTime = strDate1, MessageBody = message.MessageDescription, Datadecimal = datadecimal }); //Datadecimal = message.Datadecimal
                    }
                    return Json(new { SuspectData = result });
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Something went wrong inside CreateDepartment action: {ex.Message}");
                }
                return Json(null);
            }
            else if (claim != "" && claim.Contains("pvg"))
            {
                try
                {
                    var result = new LinkedList<object>();

                    //   var spoid = Convert.ToInt64(Session["id"]);
                    var ecid = Convert.ToInt32(id);
                    int UserId = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value);
                    ViewBag.userID = UserId;
                    //var queries1 = _context.mstQuery.ToList().Where(j => j.ID == smcid && (j.SenderID == UserId || j.ReceiverID == UserId) && j.ModuleType.ToString().Trim() == "PVG Claim").OrderBy(j => j.SentTime);
                    var queries = await _repository.MstQuery.GetAllClaimsQueriesAsync(UserId, ecid, "PVG Claim");
                    //var queries = objERPEntities.MstQueries.ToList().Where(j => j.ID == spoid && (j.SenderID == UserId || j.ReceiverID == UserId) && j.ModuleType.ToString().Trim() == "Purchase Order").OrderBy(j => j.SentTime);
                    var mstUsers = await _repository.MstUser.GetAllUsersAsync("active");
                    var VarSuspect = (from s in queries
                                      join st in mstUsers
                                               on s.SenderID equals st.UserID
                                      select new Querydata
                                      {
                                          MsgID = s.MsgID,
                                          ModuleType = s.ModuleType.Trim(),
                                          ID = (long)s.ID,
                                          SenderID = (long)s.SenderID,
                                          RecieverID = (long)s.ReceiverID,
                                          //SentTime = Convert.ToDateTime(s.SentTime.ToString("dd-MM-yyyy HH:mm")),
                                          // SentTime = Convert.ToDateTime(s.SentTime.ToString("dd-MM-yyyy HH:mm"), CultureInfo.InvariantCulture),
                                          SentTime = DateTime.ParseExact(s.SentTime.ToString("dd-MM-yyyy HH:mm"), "dd-MM-yyyy HH:mm", CultureInfo.InvariantCulture),
                                          MessageDescription = s.MessageDescription,
                                          FullName = st.Name
                                      }).OrderBy(s => s.SentTime).ToList();

                    foreach (var message in VarSuspect)
                    {
                        DateTime strDate = (DateTime)message.SentTime;
                        var datadecimal = "";
                        string strDate1 = strDate.ToString("dd/MM/yyyy h:mm tt");

                        //var FullName = objERPEntities.MstUsers.ToList().Where(p => p.UserID == Convert.ToInt32(message.SenderID)).FirstOrDefault().FullName;
                        //var FullName =  _repository.MstUser.GetUserByIdAsync((int?)message.SenderID).GetAwaiter().GetResult().Name; 
                        var mstUserSender = await _repository.MstUser.GetUserByIdAsync((int?)message.SenderID);
                        var FullName = mstUserSender.Name;

                        //var DesignationID = objERPEntities.MstUsers.ToList().Where(p => p.UserID == Convert.ToInt32(message.SenderID)).FirstOrDefault().DesignationID;

                        //var Designation = objERPEntities.MstDesignations.ToList().Where(p => p.DesignationID == DesignationID).FirstOrDefault().Designation;
                        //var Designation = "";
                        //var FullName1 = objERPEntities.MstUsers.ToList().Where(p => p.UserID == Convert.ToInt32(message.RecieverID)).FirstOrDefault().FullName;
                        var mstUserReceiver = await _repository.MstUser.GetUserByIdAsync((int?)message.RecieverID);
                        var FullName1 = mstUserReceiver.Name;
                        //var Designation1 = "";
                        //var DesignationID1 = objERPEntities.MstUsers.ToList().Where(p => p.UserID == Convert.ToInt32(message.RecieverID)).FirstOrDefault().DesignationID;

                        //var Designation1 = objERPEntities.MstDesignations.ToList().Where(p => p.DesignationID == DesignationID1).FirstOrDefault().Designation;
                        if (message.SenderID == Convert.ToInt32(HttpContext.User.FindFirst("userid").Value))
                        {
                            datadecimal = "R";
                        }
                        else
                        {
                            datadecimal = "L";
                        }
                        result.AddLast(new { Username = FullName, Designation = FullName1, PostDateTime = strDate1, MessageBody = message.MessageDescription, Datadecimal = datadecimal }); //Datadecimal = message.Datadecimal
                                                                                                                                                                                            //result.AddLast(new { Username = FullName + " (" + Designation + ")", Designation = FullName1 + " (" + Designation1 + ")", PostDateTime = strDate1, MessageBody = message.MessageDescription, Datadecimal = datadecimal }); //Datadecimal = message.Datadecimal
                    }
                    return Json(new { SuspectData = result });
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Something went wrong inside CreateDepartment action: {ex.Message}");
                }
                return Json(null);
            }
            else
            {
                try
                {
                    var result = new LinkedList<object>();

                    //   var spoid = Convert.ToInt64(Session["id"]);
                    var smcid = Convert.ToInt32(id);
                    int UserId = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value);
                    ViewBag.userID = UserId;
                    //var queries1 = _context.mstQuery.ToList().Where(j => j.ID == smcid && (j.SenderID == UserId || j.ReceiverID == UserId) && j.ModuleType.ToString().Trim() == "Mileage Claim").OrderBy(j => j.SentTime);
                    var queries = await _repository.MstQuery.GetAllClaimsQueriesAsync(UserId, smcid, "Mileage Claim");
                    //var queries = objERPEntities.MstQueries.ToList().Where(j => j.ID == spoid && (j.SenderID == UserId || j.ReceiverID == UserId) && j.ModuleType.ToString().Trim() == "Purchase Order").OrderBy(j => j.SentTime);
                    var mstUsers = await _repository.MstUser.GetAllUsersAsync("active");
                    var VarSuspect = (from s in queries
                                      join st in mstUsers
                                               on s.SenderID equals st.UserID
                                      select new Querydata
                                      {
                                          MsgID = s.MsgID,
                                          ModuleType = s.ModuleType.Trim(),
                                          ID = (long)s.ID,
                                          SenderID = (long)s.SenderID,
                                          RecieverID = (long)s.ReceiverID,
                                          //SentTime = Convert.ToDateTime(s.SentTime.ToString("dd-MM-yyyy HH:mm")),
                                          // SentTime = Convert.ToDateTime(s.SentTime.ToString("dd-MM-yyyy HH:mm"), CultureInfo.InvariantCulture),
                                          SentTime = DateTime.ParseExact(s.SentTime.ToString("dd-MM-yyyy HH:mm"), "dd-MM-yyyy HH:mm", CultureInfo.InvariantCulture),
                                          MessageDescription = s.MessageDescription,
                                          FullName = st.Name
                                      }).OrderBy(s => s.SentTime).ToList();

                    foreach (var message in VarSuspect)
                    {
                        DateTime strDate = (DateTime)message.SentTime;
                        var datadecimal = "";
                        string strDate1 = strDate.ToString("dd/MM/yyyy h:mm tt");

                        //var FullName = objERPEntities.MstUsers.ToList().Where(p => p.UserID == Convert.ToInt32(message.SenderID)).FirstOrDefault().FullName;
                        //var FullName =  _repository.MstUser.GetUserByIdAsync((int?)message.SenderID).GetAwaiter().GetResult().Name; 
                        var mstUserSender = await _repository.MstUser.GetUserByIdAsync((int?)message.SenderID);
                        var FullName = mstUserSender.Name;

                        //var DesignationID = objERPEntities.MstUsers.ToList().Where(p => p.UserID == Convert.ToInt32(message.SenderID)).FirstOrDefault().DesignationID;

                        //var Designation = objERPEntities.MstDesignations.ToList().Where(p => p.DesignationID == DesignationID).FirstOrDefault().Designation;
                        //var Designation = "";
                        //var FullName1 = objERPEntities.MstUsers.ToList().Where(p => p.UserID == Convert.ToInt32(message.RecieverID)).FirstOrDefault().FullName;
                        var mstUserReceiver = await _repository.MstUser.GetUserByIdAsync((int?)message.RecieverID);
                        var FullName1 = mstUserReceiver.Name;
                        //var Designation1 = "";
                        //var DesignationID1 = objERPEntities.MstUsers.ToList().Where(p => p.UserID == Convert.ToInt32(message.RecieverID)).FirstOrDefault().DesignationID;

                        //var Designation1 = objERPEntities.MstDesignations.ToList().Where(p => p.DesignationID == DesignationID1).FirstOrDefault().Designation;
                        if (message.SenderID == Convert.ToInt32(HttpContext.User.FindFirst("userid").Value))
                        {
                            datadecimal = "R";
                        }
                        else
                        {
                            datadecimal = "L";
                        }
                        result.AddLast(new { Username = FullName, Designation = FullName1, PostDateTime = strDate1, MessageBody = message.MessageDescription, Datadecimal = datadecimal }); //Datadecimal = message.Datadecimal
                                                                                                                                                                                            //result.AddLast(new { Username = FullName + " (" + Designation + ")", Designation = FullName1 + " (" + Designation1 + ")", PostDateTime = strDate1, MessageBody = message.MessageDescription, Datadecimal = datadecimal }); //Datadecimal = message.Datadecimal
                    }
                    return Json(new { SuspectData = result });
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Something went wrong inside CreateDepartment action: {ex.Message}");
                }
                return Json(null);
            }
        }

        #endregion GetMessages
    }
}
