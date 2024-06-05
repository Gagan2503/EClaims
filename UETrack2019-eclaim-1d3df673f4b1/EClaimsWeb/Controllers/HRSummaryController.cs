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
    [Authorize(Roles = "User,HR,Admin")]
   // [Authorize(Policy = "ShouldBeOnlyHODPolicy")]
    public class HRSummaryController : Controller
    {
        private ILoggerManager _logger;
        private IRepositoryWrapper _repository;
        private IMapper _mapper;
        private IConfiguration _configuration;
        private AlternateApproverHelper _alternateApproverHelper;
        private ISendMailServices _sendMailServices;
        private readonly RepositoryContext _context;
        public HRSummaryController(ILoggerManager logger, IRepositoryWrapper repository, IMapper mapper, RepositoryContext context, IConfiguration configuration, ISendMailServices sendMailServices)
        {
            _logger = logger;
            _repository = repository;
            _mapper = mapper;
            _context = context;
            _sendMailServices = sendMailServices;
            _configuration = configuration;
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
                oclsModule.Add(new clsModule() { ModuleName = "PV-Cheque", ModuleId = "PV-Cheque" });
                oclsModule.Add(new clsModule() { ModuleName = "PV-Giro", ModuleId = "PV-Giro" });

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


                var mstHRPVCClaimsWithDetails = await _repository.MstUser.GetAllHRSummaryClaimsAsync(Convert.ToInt32(HttpContext.User.FindFirst("userid").Value), moduleName, statusID, fromDate, toDate);
                List<CustomClaim> hRPVCClaimVMs = new List<CustomClaim>();
                foreach (var mc in mstHRPVCClaimsWithDetails)
                {
                    CustomClaim hRPVCClaimVM = new CustomClaim();
                    hRPVCClaimVM.CID = mc.CID;
                    hRPVCClaimVM.CNO = mc.CNO;
                    hRPVCClaimVM.Name = mc.Name;
                    hRPVCClaimVM.CreatedDate = Convert.ToDateTime(mc.CreatedDate).ToString("dd/MM/yyyy", System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
                    hRPVCClaimVM.ParticularsOfPayment = mc.ParticularsOfPayment;
                    hRPVCClaimVM.Phone = mc.Phone;
                    hRPVCClaimVM.GrandTotal = mc.GrandTotal;
                    hRPVCClaimVM.TotalAmount = mc.TotalAmount;
                    hRPVCClaimVM.ApprovalStatus = mc.ApprovalStatus;
                    hRPVCClaimVM.PayeeName = mc.PayeeName;
                    hRPVCClaimVM.ChequeNo = mc.ChequeNo;
                    hRPVCClaimVM.Amount = mc.Amount;
                    hRPVCClaimVM.VoucherNo = mc.VoucherNo;
                    //TempData["ApprovedStatus"] = mc.ApprovalStatus;

                    if (mc.UserApprovers != "")
                    {
                        hRPVCClaimVM.Approver = mc.UserApprovers.Split(',').First();
                        if ((hRPVCClaimVM.Approver == HttpContext.User.FindFirst("userid").Value || (delegatedUserId.HasValue && hRPVCClaimVM.Approver == delegatedUserId.Value.ToString())) && (hRPVCClaimVM.ApprovalStatus == 6))
                        {
                            hRPVCClaimVM.IsActionAllowed = true;
                        }
                    }
                    //else if (mc.HODApprover != "")
                    //{
                    //    hRPVCClaimVM.Approver = mc.HODApprover.Split(',').First();
                    //    if (hRPVCClaimVM.Approver == HttpContext.User.FindFirst("userid").Value && (hRPVCClaimVM.ApprovalStatus == 7))
                    //    {
                    //        hRPVCClaimVM.IsActionAllowed = true;
                    //    }
                    //}
                    else if (mc.Verifier != "")
                    {
                        hRPVCClaimVM.Approver = mc.Verifier.Split(',').First();
                        if((hRPVCClaimVM.Approver == HttpContext.User.FindFirst("userid").Value || (delegatedUserId.HasValue && hRPVCClaimVM.Approver == delegatedUserId.Value.ToString())) && (hRPVCClaimVM.ApprovalStatus == 1 || hRPVCClaimVM.ApprovalStatus == 2))
                        {
                            hRPVCClaimVM.IsActionAllowed = false;
                        }
                        //string VerifierIDs = string.Join(",", MileageverifierIDs.Skip(1));
                    }
                    else if (mc.Approver != "")
                    {
                        hRPVCClaimVM.Approver = mc.Approver.Split(',').First();
                        if ((hRPVCClaimVM.Approver == HttpContext.User.FindFirst("userid").Value || (delegatedUserId.HasValue && hRPVCClaimVM.Approver == delegatedUserId.Value.ToString())) && (hRPVCClaimVM.ApprovalStatus == 1 || hRPVCClaimVM.ApprovalStatus == 2))
                        {
                            hRPVCClaimVM.IsActionAllowed = false;
                        }
                    }
                    else
                    {
                        hRPVCClaimVM.Approver = "";
                    }

                    if (hRPVCClaimVM.Approver != "")
                    {
                        var alternateUser = await _alternateApproverHelper.IsAlternateApprovalSetForUser(Convert.ToInt32(hRPVCClaimVM.Approver));
                        if (alternateUser.HasValue)
                        {
                            var mstUserApprover = await _repository.MstUser.GetUserByIdAsync(alternateUser.Value);
                            hRPVCClaimVM.Approver = mstUserApprover.Name + " (AA)";
                        }
                        else
                        {
                            var mstUserApprover = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(hRPVCClaimVM.Approver));
                            hRPVCClaimVM.Approver = mstUserApprover.Name;
                        }
                    }

                    hRPVCClaimVMs.Add(hRPVCClaimVM);
                }
                _logger.LogInfo($"Returned all Mileage Claims with details from database.");

                var mstMileageClaimVM = new HRClaimSearchViewModel
                {
                    //Screens = new SelectList(await screenQuery.Distinct().ToListAsync()),
                    customClaimVMs = hRPVCClaimVMs,
                    ReportTypes = new SelectList(reports, "Value", "Text"),
                    Statuses = new SelectList(status, "Value", "Text"),
                    FromDate = fromDate,
                    ToDate = toDate
                }; 
                
                return View(mstMileageClaimVM);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Something went wrong inside GetAllHRPVCClaimWithDetailsAsync action: {ex.Message}");
                return View();
            }
        }

        public async Task<IActionResult> HRPVGCDetails(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            long HRPVGCID = Convert.ToInt64(id);

            if (User != null && User.Identity.IsAuthenticated)
            {
                var mstUserDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(HttpContext.User.FindFirst("userid").Value));
                var mstHRPVGClaim = await _repository.MstHRPVGClaim.GetHRPVGClaimByIdAsync(id);

                if (mstHRPVGClaim == null)
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

                var dtHRPVGSummaries = await _repository.DtHRPVGClaimSummary.GetDtHRPVGClaimSummaryByIdAsync(id);

                var dtHRPVGClaims = await _repository.DtHRPVGClaim.GetDtHRPVGClaimByIdAsync(id);
                HRPVGClaimDetailVM hRPVGClaimDetailVM = new HRPVGClaimDetailVM();
                //List<DtMileageClaimVM> dtMileageClaimVMs = new List<DtMileageClaimVM>();
                hRPVGClaimDetailVM.DtHRPVGClaimVMs = new List<DtHRPVGClaimVM>();
                // List<clsDtSupplierPO> oclsDtSupplierPO = new List<clsDtSupplierPO>();
                foreach (var item in dtHRPVGClaims)
                {
                    DtHRPVGClaimVM dtHRPVGClaimVM = new DtHRPVGClaimVM();

                    dtHRPVGClaimVM.HRPVGCItemID = item.HRPVGCItemID;
                    dtHRPVGClaimVM.HRPVGCID = item.HRPVGCID;
                    dtHRPVGClaimVM.StaffName = item.StaffName;
                    dtHRPVGClaimVM.Reason = item.Reason;
                    dtHRPVGClaimVM.EmployeeNo = item.EmployeeNo;
                    dtHRPVGClaimVM.ChequeNo = item.ChequeNo;
                    dtHRPVGClaimVM.Amount = item.Amount;
                    dtHRPVGClaimVM.GST = item.GST;
                    dtHRPVGClaimVM.AmountWithGST = item.Amount + item.GST;
                    if (item.FacilityID != null)
                    {
                        var mstFacility = await _repository.MstFacility.GetFacilityByIdAsync(item.FacilityID);
                        dtHRPVGClaimVM.Facility = mstFacility.FacilityName;
                    }
                    dtHRPVGClaimVM.AccountCode = item.AccountCode;
                    dtHRPVGClaimVM.Date = item.Date;
                    dtHRPVGClaimVM.Bank = item.Bank;
                    dtHRPVGClaimVM.BankCode = item.BankCode;
                    dtHRPVGClaimVM.BranchCode = item.BranchCode;
                    dtHRPVGClaimVM.BankAccount = item.BankAccount;
                    dtHRPVGClaimVM.Mobile = item.Mobile;


                    hRPVGClaimDetailVM.DtHRPVGClaimVMs.Add(dtHRPVGClaimVM);
                }

                hRPVGClaimDetailVM.DtHRPVGClaimSummaries = dtHRPVGSummaries;

                var GroupByQS = hRPVGClaimDetailVM.DtHRPVGClaimVMs.GroupBy(s => s.ExpenseCategoryID);
                hRPVGClaimDetailVM.DtHRPVGClaimVMSummary = new List<DtHRPVGClaimVM>();

                foreach (var group in GroupByQS)
                {
                    DtHRPVGClaimVM dtHRPVGClaimVM = new DtHRPVGClaimVM();
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
                            ExpenseDesc = dtExpense.Reason;
                        i++;
                        amount = amount + dtExpense.Amount;
                        //gst = gst + dtExpense.Gst;
                        //sumamount = sumamount + dtExpense.AmountWithGST;
                        ExpenseCat = "Payroll Control";
                        AccountCode = dtExpense.AccountCode;
                    }
                    gst = gst / group.Count();
                    dtHRPVGClaimVM.Particulars = ExpenseDesc;
                    dtHRPVGClaimVM.ExpenseCategory = ExpenseCat;
                    dtHRPVGClaimVM.AccountCode = AccountCode;
                    dtHRPVGClaimVM.Amount = amount;
                    //dtMileageClaimVM.Gst = gst;
                    //dtTBClaimVM.AmountWithGST = sumamount;
                    hRPVGClaimDetailVM.DtHRPVGClaimVMSummary.Add(dtHRPVGClaimVM);
                }

                hRPVGClaimDetailVM.HRPVGClaimAudits = new List<HRPVGClaimAuditVM>();

                var dtHRPVGClaimAudits = await _repository.MstHRPVGClaimAudit.GetMstHRPVGClaimAuditByIdAsync(id);

                foreach (var item in dtHRPVGClaimAudits)
                {
                    HRPVGClaimAuditVM mstHRPVGClaimAuditVM = new HRPVGClaimAuditVM();
                    mstHRPVGClaimAuditVM.Action = item.Action;
                    mstHRPVGClaimAuditVM.Description = item.Description;
                    mstHRPVGClaimAuditVM.AuditDateTickle = Helper.RelativeDate(item.AuditDate);
                    hRPVGClaimDetailVM.HRPVGClaimAudits.Add(mstHRPVGClaimAuditVM);
                }

                hRPVGClaimDetailVM.HRPVGClaimFileUploads = new List<DtHRPVGClaimFileUpload>();

                hRPVGClaimDetailVM.HRPVGClaimFileUploads = _repository.DtHRPVGClaimFileUpload.GetDtHRPVGClaimAuditByIdAsync(id).Result.ToList();

                HRPVGClaimVM hRPVGClaimVM = new HRPVGClaimVM();
                //hRPVGClaimVM.ClaimType = mstHRPVGClaim.ClaimType;
                hRPVGClaimVM.VoucherNo = mstHRPVGClaim.VoucherNo;
                hRPVGClaimVM.ParticularsOfPayment = mstHRPVGClaim.ParticularsOfPayment;
                hRPVGClaimVM.Amount = mstHRPVGClaim.Amount;
                hRPVGClaimVM.ChequeNo = mstHRPVGClaim.ChequeNo;
                hRPVGClaimVM.GrandTotal = mstHRPVGClaim.GrandTotal;
                hRPVGClaimVM.TotalAmount = mstHRPVGClaim.TotalAmount;
                hRPVGClaimVM.Company = "UEMS";
                hRPVGClaimVM.Name = mstHRPVGClaim.MstUser.Name;
                hRPVGClaimVM.DepartmentName = mstHRPVGClaim.MstDepartment.Department;
                hRPVGClaimVM.FacilityName = mstHRPVGClaim.MstFacility.FacilityName;
                hRPVGClaimVM.CreatedDate = Convert.ToDateTime(mstHRPVGClaim.CreatedDate).ToString("dd/MM/yyyy", System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
                hRPVGClaimVM.Verifier = mstHRPVGClaim.Verifier;
                hRPVGClaimVM.Approver = mstHRPVGClaim.Approver;
                hRPVGClaimVM.HRPVGCNo = mstHRPVGClaim.HRPVGCNo;
                hRPVGClaimVM.PaymentMode = mstHRPVGClaim.PaymentMode;
                ViewBag.HRPVGCID = id;
                TempData["CreatedBy"] = mstHRPVGClaim.CreatedBy;
                ViewBag.Approvalstatus = mstHRPVGClaim.ApprovalStatus;


                TempData["ApprovedStatus"] = mstHRPVGClaim.ApprovalStatus;
                TempData["FinalApproverID"] = mstHRPVGClaim.FinalApprover;
                ViewBag.VoidReason = mstHRPVGClaim.VoidReason == null ? "" : mstHRPVGClaim.VoidReason;

                if (TempData["ApprovedStatus"].ToString() == "1" || TempData["ApprovedStatus"].ToString() == "2" || TempData["ApprovedStatus"].ToString() == "3" || TempData["ApprovedStatus"].ToString() == "-5" || TempData["ApprovedStatus"].ToString() == "6" || TempData["ApprovedStatus"].ToString() == "7" || TempData["ApprovedStatus"].ToString() == "9")
                {
                    ViewBag.ShowVoidBtn = 1;

                    if (User.IsInRole("HR") && (mstUserDetails.IsHOD || (User.FindFirst("issuperior") is null ? false : User.FindFirst("issuperior").Value.ToString().ToLower() == "true")))
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
                if (mstHRPVGClaim.UserApprovers != "")
                {
                    string[] userApproverIDs = mstHRPVGClaim.UserApprovers.Split(',');
                    TempData["QueryMCUserApproverIDs"] = string.Join(",", userApproverIDs);
                    foreach (string approverID in userApproverIDs)
                    {
                        if ((approverID != "" && approverID == HttpContext.User.FindFirst("userid").Value || (delegatedUserId.HasValue && approverID == delegatedUserId.Value.ToString())) && mstUserDetails.IsActive && (mstUserDetails.IsHOD || (User.FindFirst("issuperior") is null ? false : User.FindFirst("issuperior").Value.ToString().ToLower() == "true")))
                        {
                            TempData["ApprovedStatus"] = mstHRPVGClaim.ApprovalStatus;
                            //Session["ApprovedStatus"] = oSupplierPO.Approvalstatus;
                            TempData["UserApproverIDs"] = string.Join(",", userApproverIDs.Skip(1));
                            hRPVGClaimVM.IsActionAllowed = true;
                        }
                        else
                        {
                            TempData["ApprovedStatus"] = "";
                            //Session["ApprovedStatus"] = oSupplierPO.Approvalstatus;
                            TempData["UserApproverIDs"] = mstHRPVGClaim.UserApprovers;
                        }
                        break;
                    }
                }
                else
                {
                    string[] userApproverIDs = mstHRPVGClaim.UserApprovers.Split(',');
                    TempData["QueryMCUserApproverIDs"] = string.Join(",", userApproverIDs);
                }

                if (mstHRPVGClaim.Verifier != "" && mstHRPVGClaim.UserApprovers == "")
                {
                    string[] verifierIDs = mstHRPVGClaim.Verifier.Split(',');
                    TempData["QueryMCVerifierIDs"] = string.Join(",", verifierIDs);
                    foreach (string verifierID in verifierIDs)
                    {
                        if (verifierID != "" && verifierID == HttpContext.User.FindFirst("userid").Value && User.IsInRole("HR") && (mstUserDetails.IsHOD || (User.FindFirst("issuperior") is null ? false : User.FindFirst("issuperior").Value.ToString().ToLower() == "true")))
                        {
                            TempData["ApprovedStatus"] = mstHRPVGClaim.ApprovalStatus;
                            //Session["ApprovedStatus"] = oSupplierPO.Approvalstatus;
                            TempData["VerifierIDs"] = string.Join(",", verifierIDs.Skip(1));
                        }
                        else
                        {
                            TempData["ApprovedStatus"] = "";
                            //Session["ApprovedStatus"] = oSupplierPO.Approvalstatus;
                            TempData["VerifierIDs"] = mstHRPVGClaim.Verifier;
                        }
                        TempData["ApproverIDs"] = mstHRPVGClaim.Approver;
                        break;
                    }
                }
                else
                {
                    //TempData["VerifierIDs"] = mstHRPVGClaim.Verifier;
                    //TempData["ApproverIDs"] = mstHRPVGClaim.Approver;
                    string[] verifierIDs = mstHRPVGClaim.Verifier.Split(',');
                    TempData["QueryMCVerifierIDs"] = string.Join(",", verifierIDs);
                }

                //Approval Process code
                if (mstHRPVGClaim.Approver != "" && mstHRPVGClaim.Verifier == "")
                {
                    string[] approverIDs = mstHRPVGClaim.Approver.Split(',');
                    TempData["QueryMCApproverIDs"] = string.Join(",", approverIDs);
                    foreach (string approverID in approverIDs)
                    {
                        if (approverID != "" && approverID == HttpContext.User.FindFirst("userid").Value && User.IsInRole("HR") && (mstUserDetails.IsHOD || (User.FindFirst("issuperior") is null ? false : User.FindFirst("issuperior").Value.ToString().ToLower() == "true")))
                        {
                            TempData["ApprovedStatus"] = mstHRPVGClaim.ApprovalStatus;
                            //Session["ApprovedStatus"] = oSupplierPO.Approvalstatus;
                            TempData["ApproverIDs"] = string.Join(",", approverIDs.Skip(1));
                        }
                        else
                        {
                            TempData["ApprovedStatus"] = "";
                            //Session["ApprovedStatus"] = oSupplierPO.Approvalstatus;
                            TempData["ApproverIDs"] = mstHRPVGClaim.Approver;
                        }
                        break;
                    }
                }
                else
                {
                    string[] approverIDs = mstHRPVGClaim.Approver.Split(',');
                    TempData["QueryMCApproverIDs"] = string.Join(",", approverIDs);
                }

                if (mstHRPVGClaim.HODApprover != "" && mstHRPVGClaim.UserApprovers == "")
                {
                    string[] hodApproverIDs = mstHRPVGClaim.HODApprover.Split(',');
                    TempData["QueryMCHODApproverIDs"] = string.Join(",", hodApproverIDs);
                    foreach (string approverID in hodApproverIDs)
                    {
                        if ((approverID != "" && approverID == HttpContext.User.FindFirst("userid").Value || (delegatedUserId.HasValue && approverID == delegatedUserId.Value.ToString())) && mstUserDetails.IsActive && (mstUserDetails.IsHOD || (User.FindFirst("issuperior") is null ? false : User.FindFirst("issuperior").Value.ToString().ToLower() == "true")))
                        {
                            TempData["ApprovedStatus"] = mstHRPVGClaim.ApprovalStatus;
                            //Session["ApprovedStatus"] = oSupplierPO.Approvalstatus;
                            TempData["HODApproverIDs"] = string.Join(",", hodApproverIDs.Skip(1));
                            hRPVGClaimVM.IsActionAllowed = true;
                        }
                        else
                        {
                            TempData["ApprovedStatus"] = "";
                            //Session["ApprovedStatus"] = oSupplierPO.Approvalstatus;
                            TempData["HODApproverIDs"] = mstHRPVGClaim.HODApprover;
                        }
                        break;
                    }
                }
                else
                {
                    string[] hodApproverIDs = mstHRPVGClaim.HODApprover.Split(',');
                    TempData["QueryMCHODApproverIDs"] = string.Join(",", hodApproverIDs);
                }

                // Show actions based on alternate approver settings
                // Override all the isActionAllowed code above. When alternate approval is set, then no need to show the action on any scenario
                if (isAlternateApproverSet)
                {
                    hRPVGClaimVM.IsActionAllowed = false;
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
                var mstHRPVGClaimAudits = await _repository.MstHRPVGClaimAudit.GetMstHRPVGClaimAuditByIdAsync(HRPVGCID);
                var AuditIDs = mstHRPVGClaimAudits.Select(m => m.AuditBy.ToString()).Distinct();
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


                hRPVGClaimDetailVM.HRPVGClaimVM = hRPVGClaimVM;
                //mileageClaimDetailVM.DtMileageClaimVMs = dtMileageClaimVMs;



                return View(hRPVGClaimDetailVM);
            }
            else
            {
                return Redirect("~/Login/Login");
            }
        }

        public async Task<IActionResult> Details(long? id,string cno)
        {
            if (id == null)
            {
                return NotFound();
            }
            if ((cno == null && id != null) || cno.ToLower().Contains("hpvc"))
            {
                long HRPVCCID = Convert.ToInt64(id);

                if (User != null && User.Identity.IsAuthenticated)
                {
                    var mstUserDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(HttpContext.User.FindFirst("userid").Value));

                    var mstHRPVCClaim = await _repository.MstHRPVCClaim.GetHRPVCClaimByIdAsync(id);

                    if (mstHRPVCClaim == null)
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

                    var dtHRPVCSummaries = await _repository.DtHRPVCClaimSummary.GetDtHRPVCClaimSummaryByIdAsync(id);

                    var dtHRPVCClaims = await _repository.DtHRPVCClaim.GetDtHRPVCClaimByIdAsync(id);
                    HRPVCClaimDetailVM hRPVCClaimDetailVM = new HRPVCClaimDetailVM();
                    //List<DtMileageClaimVM> dtMileageClaimVMs = new List<DtMileageClaimVM>();
                    hRPVCClaimDetailVM.DtHRPVCClaimVMs = new List<DtHRPVCClaimVM>();
                    // List<clsDtSupplierPO> oclsDtSupplierPO = new List<clsDtSupplierPO>();
                    foreach (var item in dtHRPVCClaims)
                    {
                        DtHRPVCClaimVM dtHRPVCClaimVM = new DtHRPVCClaimVM();


                        dtHRPVCClaimVM.HRPVCCItemID = item.HRPVCCItemID;
                        dtHRPVCClaimVM.HRPVCCID = item.HRPVCCID;
                        dtHRPVCClaimVM.Date = item.Date;
                        dtHRPVCClaimVM.StaffName = item.StaffName;
                        dtHRPVCClaimVM.Reason = item.Reason;
                        dtHRPVCClaimVM.EmployeeNo = item.EmployeeNo;
                        if (item.FacilityID != null)
                        {
                            var mstFacility = await _repository.MstFacility.GetFacilityByIdAsync(item.FacilityID);
                            dtHRPVCClaimVM.Facility = mstFacility.FacilityName;
                        }
                        dtHRPVCClaimVM.Amount = item.Amount;
                        dtHRPVCClaimVM.GST = item.GST;
                        dtHRPVCClaimVM.ChequeNo = item.ChequeNo;
                        dtHRPVCClaimVM.AmountWithGST = item.Amount + item.GST;
                        dtHRPVCClaimVM.AccountCode = item.AccountCode;

                        hRPVCClaimDetailVM.DtHRPVCClaimVMs.Add(dtHRPVCClaimVM);
                    }
                    hRPVCClaimDetailVM.DtHRPVCClaimSummaries = dtHRPVCSummaries;
                    var GroupByQS = hRPVCClaimDetailVM.DtHRPVCClaimVMs.GroupBy(s => s.ExpenseCategoryID);

                    hRPVCClaimDetailVM.DtHRPVCClaimVMSummary = new List<DtHRPVCClaimVM>();

                    foreach (var group in GroupByQS)
                    {
                        DtHRPVCClaimVM dtHRPVCClaimVM = new DtHRPVCClaimVM();
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
                                ExpenseDesc = dtExpense.Reason;
                            i++;
                            amount = amount + dtExpense.Amount;
                            //gst = gst + dtExpense.Gst;
                            //sumamount = sumamount + dtExpense.AmountWithGST;
                            ExpenseCat = "Payroll Control";
                            AccountCode = dtExpense.AccountCode;
                        }
                        gst = gst / group.Count();
                        dtHRPVCClaimVM.Particulars = ExpenseDesc;
                        dtHRPVCClaimVM.ExpenseCategory = ExpenseCat;
                        dtHRPVCClaimVM.AccountCode = AccountCode;
                        dtHRPVCClaimVM.Amount = amount;
                        //dtMileageClaimVM.Gst = gst;
                        //dtTBClaimVM.AmountWithGST = sumamount;
                        hRPVCClaimDetailVM.DtHRPVCClaimVMSummary.Add(dtHRPVCClaimVM);
                    }

                    hRPVCClaimDetailVM.HRPVCClaimAudits = new List<HRPVCClaimAuditVM>();

                    var dtHRPVCClaimAudits = await _repository.MstHRPVCClaimAudit.GetMstHRPVCClaimAuditByIdAsync(id);

                    foreach (var item in dtHRPVCClaimAudits)
                    {
                        HRPVCClaimAuditVM mstHRPVCClaimAuditVM = new HRPVCClaimAuditVM();
                        mstHRPVCClaimAuditVM.Action = item.Action;
                        mstHRPVCClaimAuditVM.Description = item.Description;
                        mstHRPVCClaimAuditVM.AuditDateTickle = Helper.RelativeDate(item.AuditDate);
                        hRPVCClaimDetailVM.HRPVCClaimAudits.Add(mstHRPVCClaimAuditVM);
                    }

                    hRPVCClaimDetailVM.HRPVCClaimFileUploads = new List<DtHRPVCClaimFileUpload>();

                    hRPVCClaimDetailVM.HRPVCClaimFileUploads = _repository.DtHRPVCClaimFileUpload.GetDtHRPVCClaimAuditByIdAsync(id).Result.ToList();

                    HRPVCClaimVM hRPVCClaimVM = new HRPVCClaimVM();
                    //hRPVCClaimVM.ClaimType = mstHRPVCClaim.ClaimType;
                    hRPVCClaimVM.GrandTotal = mstHRPVCClaim.GrandTotal;
                    hRPVCClaimVM.TotalAmount = mstHRPVCClaim.TotalAmount;
                    hRPVCClaimVM.VoucherNo = mstHRPVCClaim.VoucherNo;
                    hRPVCClaimVM.ParticularsOfPayment = mstHRPVCClaim.ParticularsOfPayment;
                    hRPVCClaimVM.Amount = mstHRPVCClaim.Amount;
                    hRPVCClaimVM.ChequeNo = mstHRPVCClaim.ChequeNo;
                    hRPVCClaimVM.Company = "UEMS";
                    hRPVCClaimVM.Name = mstHRPVCClaim.MstUser.Name;
                    hRPVCClaimVM.DepartmentName = mstHRPVCClaim.MstDepartment.Department;
                    hRPVCClaimVM.FacilityName = mstHRPVCClaim.MstFacility.FacilityName;
                    hRPVCClaimVM.CreatedDate = Convert.ToDateTime(mstHRPVCClaim.CreatedDate).ToString("dd/MM/yyyy", System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
                    hRPVCClaimVM.Verifier = mstHRPVCClaim.Verifier;
                    hRPVCClaimVM.Approver = mstHRPVCClaim.Approver;
                    hRPVCClaimVM.HRPVCCNo = mstHRPVCClaim.HRPVCCNo;
                    ViewBag.HRPVCCID = id;
                    TempData["CreatedBy"] = mstHRPVCClaim.CreatedBy;
                    ViewBag.Approvalstatus = mstHRPVCClaim.ApprovalStatus;


                    TempData["ApprovedStatus"] = mstHRPVCClaim.ApprovalStatus;
                    TempData["FinalApproverID"] = mstHRPVCClaim.FinalApprover;
                    ViewBag.VoidReason = mstHRPVCClaim.VoidReason == null ? "" : mstHRPVCClaim.VoidReason;

                    if (TempData["ApprovedStatus"].ToString() == "1" || TempData["ApprovedStatus"].ToString() == "2" || TempData["ApprovedStatus"].ToString() == "3" || TempData["ApprovedStatus"].ToString() == "-5" || TempData["ApprovedStatus"].ToString() == "6" || TempData["ApprovedStatus"].ToString() == "7" || TempData["ApprovedStatus"].ToString() == "9" || TempData["ApprovedStatus"].ToString() == "10")
                    {
                        ViewBag.ShowVoidBtn = 1;

                        if (User.IsInRole("HR") && (mstUserDetails.IsHOD || (User.FindFirst("issuperior") is null ? false : User.FindFirst("issuperior").Value.ToString().ToLower() == "true")))
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
                    if (mstHRPVCClaim.UserApprovers != "")
                    {
                        string[] userApproverIDs = mstHRPVCClaim.UserApprovers.Split(',');
                        TempData["QueryMCUserApproverIDs"] = string.Join(",", userApproverIDs);
                        foreach (string approverID in userApproverIDs)
                        {
                            if ((approverID != "" && approverID == HttpContext.User.FindFirst("userid").Value || (delegatedUserId.HasValue && approverID == delegatedUserId.Value.ToString())) && mstUserDetails.IsActive && (mstUserDetails.IsHOD || (User.FindFirst("issuperior") is null ? false : User.FindFirst("issuperior").Value.ToString().ToLower() == "true")))
                            {
                                TempData["ApprovedStatus"] = mstHRPVCClaim.ApprovalStatus;
                                //Session["ApprovedStatus"] = oSupplierPO.Approvalstatus;
                                TempData["UserApproverIDs"] = string.Join(",", userApproverIDs.Skip(1));
                                hRPVCClaimVM.IsActionAllowed = true;
                            }
                            else
                            {
                                TempData["ApprovedStatus"] = "";
                                //Session["ApprovedStatus"] = oSupplierPO.Approvalstatus;
                                TempData["UserApproverIDs"] = mstHRPVCClaim.UserApprovers;
                            }
                            break;
                        }
                    }
                    else
                    {
                        string[] userApproverIDs = mstHRPVCClaim.UserApprovers.Split(',');
                        TempData["QueryMCUserApproverIDs"] = string.Join(",", userApproverIDs);
                    }

                    if (mstHRPVCClaim.Verifier != "" && mstHRPVCClaim.UserApprovers == "")
                    {
                        string[] verifierIDs = mstHRPVCClaim.Verifier.Split(',');
                        TempData["QueryMCVerifierIDs"] = string.Join(",", verifierIDs);
                        foreach (string verifierID in verifierIDs)
                        {
                            if (verifierID != "" && verifierID == HttpContext.User.FindFirst("userid").Value && User.IsInRole("HR") && (mstUserDetails.IsHOD || (User.FindFirst("issuperior") is null ? false : User.FindFirst("issuperior").Value.ToString().ToLower() == "true")))
                            {
                                TempData["ApprovedStatus"] = mstHRPVCClaim.ApprovalStatus;
                                //Session["ApprovedStatus"] = oSupplierPO.Approvalstatus;
                                TempData["VerifierIDs"] = string.Join(",", verifierIDs.Skip(1));
                            }
                            else
                            {
                                TempData["ApprovedStatus"] = "";
                                //Session["ApprovedStatus"] = oSupplierPO.Approvalstatus;
                                TempData["VerifierIDs"] = mstHRPVCClaim.Verifier;
                            }
                            TempData["ApproverIDs"] = mstHRPVCClaim.Approver;
                            break;
                        }
                    }
                    else
                    {
                        //TempData["VerifierIDs"] = mstHRPVCClaim.Verifier;
                        //TempData["ApproverIDs"] = mstHRPVCClaim.Approver;
                        string[] verifierIDs = mstHRPVCClaim.Verifier.Split(',');
                        TempData["QueryMCVerifierIDs"] = string.Join(",", verifierIDs);
                    }

                    //Approval Process code
                    if (mstHRPVCClaim.Approver != "" && mstHRPVCClaim.Verifier == "" && mstHRPVCClaim.UserApprovers == "" && mstHRPVCClaim.HODApprover == "")
                    {
                        string[] approverIDs = mstHRPVCClaim.Approver.Split(',');
                        TempData["QueryMCApproverIDs"] = string.Join(",", approverIDs);
                        foreach (string approverID in approverIDs)
                        {
                            if (approverID != "" && approverID == HttpContext.User.FindFirst("userid").Value && User.IsInRole("HR") && (mstUserDetails.IsHOD || (User.FindFirst("issuperior") is null ? false : User.FindFirst("issuperior").Value.ToString().ToLower() == "true")))
                            {
                                TempData["ApprovedStatus"] = mstHRPVCClaim.ApprovalStatus;
                                //Session["ApprovedStatus"] = oSupplierPO.Approvalstatus;
                                TempData["ApproverIDs"] = string.Join(",", approverIDs.Skip(1));
                            }
                            else
                            {
                                TempData["ApprovedStatus"] = "";
                                //Session["ApprovedStatus"] = oSupplierPO.Approvalstatus;
                                TempData["ApproverIDs"] = mstHRPVCClaim.Approver;
                            }
                            break;
                        }
                    }
                    else
                    {
                        string[] approverIDs = mstHRPVCClaim.Approver.Split(',');
                        TempData["QueryMCApproverIDs"] = string.Join(",", approverIDs);
                    }

                    if (mstHRPVCClaim.HODApprover != "" && mstHRPVCClaim.UserApprovers == "")
                    {
                        string[] hodApproverIDs = mstHRPVCClaim.HODApprover.Split(',');
                        TempData["QueryMCHODApproverIDs"] = string.Join(",", hodApproverIDs);
                        foreach (string approverID in hodApproverIDs)
                        {
                            if ((approverID != "" && approverID == HttpContext.User.FindFirst("userid").Value || (delegatedUserId.HasValue && approverID == delegatedUserId.Value.ToString())) && mstUserDetails.IsActive && (mstUserDetails.IsHOD || (User.FindFirst("issuperior") is null ? false : User.FindFirst("issuperior").Value.ToString().ToLower() == "true")))
                            {
                                TempData["ApprovedStatus"] = mstHRPVCClaim.ApprovalStatus;
                                //Session["ApprovedStatus"] = oSupplierPO.Approvalstatus;
                                TempData["HODApproverIDs"] = string.Join(",", hodApproverIDs.Skip(1));
                                hRPVCClaimVM.IsActionAllowed = true;
                            }
                            else
                            {
                                TempData["ApprovedStatus"] = "";
                                //Session["ApprovedStatus"] = oSupplierPO.Approvalstatus;
                                TempData["HODApproverIDs"] = mstHRPVCClaim.HODApprover;
                            }
                            break;
                        }
                    }
                    else
                    {
                        string[] hodApproverIDs = mstHRPVCClaim.HODApprover.Split(',');
                        TempData["QueryMCHODApproverIDs"] = string.Join(",", hodApproverIDs);
                    }

                    // Show actions based on alternate approver settings
                    // Override all the isActionAllowed code above. When alternate approval is set, then no need to show the action on any scenario
                    if (isAlternateApproverSet)
                    {
                        hRPVCClaimVM.IsActionAllowed = false;
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
                    var mstHRPVCClaimAudits = await _repository.MstHRPVCClaimAudit.GetMstHRPVCClaimAuditByIdAsync(HRPVCCID);
                    var AuditIDs = mstHRPVCClaimAudits.Select(m => m.AuditBy.ToString()).Distinct();
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


                    hRPVCClaimDetailVM.HRPVCClaimVM = hRPVCClaimVM;
                    //mileageClaimDetailVM.DtMileageClaimVMs = dtMileageClaimVMs;



                    return View(hRPVCClaimDetailVM);
                }
                else
                {
                    return Redirect("~/Login/Login");
                }
            }
            else
            {
                return RedirectToAction("HRPVGCDetails", "HRSummary", new { id = id });
            }
        }

        public async Task<JsonResult> UpdateStatusforVoid(string id, string reason, string approvedStatus,string claim)
        {
            if (claim != "" && claim.Contains("hpvg"))
            {
                if (User != null && User.Identity.IsAuthenticated)
                {
                    int HRPVGCID = Convert.ToInt32(id);

                    var mstHRPVGClaim = await _repository.MstHRPVGClaim.GetHRPVGClaimByIdAsync(HRPVGCID);

                    if (mstHRPVGClaim == null)
                    {
                        // return NotFound();
                    }

                    int loggedInUserId = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value);
                    bool isAlternateApprover = false;
                    var delegatedUserId = await _alternateApproverHelper.IsUserHasAnyAlternateApprovalSet(loggedInUserId);
                    if (delegatedUserId.HasValue)
                    {
                        isAlternateApprover = true;
                    }

                    if (Convert.ToInt32(approvedStatus) == 3 || Convert.ToInt32(approvedStatus) == 9 || Convert.ToInt32(approvedStatus) == 10)
                    {
                        await _repository.MstHRPVGClaim.UpdateMstHRPVGClaimStatus(HRPVGCID, -5, int.Parse(HttpContext.User.FindFirst("userid").Value), DateTime.Now, reason, string.Empty, string.Empty, string.Empty, string.Empty, isAlternateApprover,0);
                    }
                    else
                    {
                        await _repository.MstHRPVGClaim.UpdateMstHRPVGClaimStatus(HRPVGCID, 5, int.Parse(HttpContext.User.FindFirst("userid").Value), DateTime.Now, reason, string.Empty, string.Empty, string.Empty, string.Empty, isAlternateApprover,0);
                        string domainUrl = HttpContext.Request.Scheme + "://" + HttpContext.Request.Host;
                        string clickUrl = domainUrl + "/" + "HRPVGiroClaim/Details/" + HRPVGCID;

                        var mstSenderDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(HttpContext.User.FindFirst("userid").Value));
                        var senderName = mstSenderDetails.Name;
                        var mstVerifierDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(mstHRPVGClaim.UserID));
                        var toEmail = mstVerifierDetails.EmailAddress;
                        var receiverName = mstVerifierDetails.Name;
                        var claimNo = mstHRPVGClaim.HRPVGCNo;
                        var screen = "HR PV-GIRO Claim";
                        var approvalType = "Voided ";
                        int userID = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value);
                        var subject = "HR PV-GIRO Claim " + claimNo + " has been Voided ";

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
                    int HRPVCCID = Convert.ToInt32(id);

                    var mstHRPVCClaim = await _repository.MstHRPVCClaim.GetHRPVCClaimByIdAsync(HRPVCCID);

                    if (mstHRPVCClaim == null)
                    {
                        // return NotFound();
                    }

                    int loggedInUserId = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value);
                    bool isAlternateApprover = false;
                    var delegatedUserId = await _alternateApproverHelper.IsUserHasAnyAlternateApprovalSet(loggedInUserId);
                    if (delegatedUserId.HasValue)
                    {
                        isAlternateApprover = true;
                    }

                    if (Convert.ToInt32(approvedStatus) == 3 || Convert.ToInt32(approvedStatus) == 9 || Convert.ToInt32(approvedStatus) == 10)
                    {
                        await _repository.MstHRPVCClaim.UpdateMstHRPVCClaimStatus(HRPVCCID, -5, int.Parse(HttpContext.User.FindFirst("userid").Value), DateTime.Now, reason, string.Empty, string.Empty, string.Empty, string.Empty, isAlternateApprover,0);
                    }
                    else
                    {
                        await _repository.MstHRPVCClaim.UpdateMstHRPVCClaimStatus(HRPVCCID, 5, int.Parse(HttpContext.User.FindFirst("userid").Value), DateTime.Now, reason, string.Empty, string.Empty, string.Empty, string.Empty, isAlternateApprover,0);
                        string domainUrl = HttpContext.Request.Scheme + "://" + HttpContext.Request.Host;
                        string clickUrl = domainUrl + "/" + "HRPVChequeClaim/Details/" + HRPVCCID;

                        var mstSenderDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(HttpContext.User.FindFirst("userid").Value));
                        var senderName = mstSenderDetails.Name;
                        var mstVerifierDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(mstHRPVCClaim.UserID));
                        var toEmail = mstVerifierDetails.EmailAddress;
                        var receiverName = mstVerifierDetails.Name;
                        var claimNo = mstHRPVCClaim.HRPVCCNo;
                        var screen = "HR PV Cheque Claim";
                        var approvalType = "Voided ";
                        int userID = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value);
                        var subject = "HR PV Cheque Claim " + claimNo + " has been Voided ";

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

        public async Task<JsonResult> UpdateRejectedStatus(string id, string reason,string claim)
        {
            if (claim != "" && claim.Contains("hpvg"))
            {
                if (User != null && User.Identity.IsAuthenticated)
                {
                    int HRPVGCID = Convert.ToInt32(id);

                    var mstHRPVGClaim = await _repository.MstHRPVGClaim.GetHRPVGClaimByIdAsync(HRPVGCID);

                    if (mstHRPVGClaim == null)
                    {
                        // return NotFound();
                    }

                    int loggedInUserId = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value);
                    bool isAlternateApprover = false;
                    var delegatedUserId = await _alternateApproverHelper.IsUserHasAnyAlternateApprovalSet(loggedInUserId);
                    if (delegatedUserId.HasValue)
                    {
                        isAlternateApprover = true;
                    }

                    await _repository.MstHRPVGClaim.UpdateMstHRPVGClaimStatus(HRPVGCID, 4, int.Parse(HttpContext.User.FindFirst("userid").Value), DateTime.Now, reason, string.Empty, string.Empty, string.Empty, string.Empty, isAlternateApprover,0);
                    string domainUrl = HttpContext.Request.Scheme + "://" + HttpContext.Request.Host;
                    string clickUrl = domainUrl + "/" + "HRPVGiroClaim/Details/" + HRPVGCID;

                    var mstSenderDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(HttpContext.User.FindFirst("userid").Value));
                    var senderName = mstSenderDetails.Name;
                    var mstVerifierDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(mstHRPVGClaim.UserID));
                    var toEmail = mstVerifierDetails.EmailAddress;
                    var receiverName = mstVerifierDetails.Name;
                    var claimNo = mstHRPVGClaim.HRPVGCNo;
                    var screen = "HR PV-GIRO Claim";
                    var approvalType = "Rejected Request";
                    int userID = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value);
                    var subject = "HR PV-GIRO Claim " + claimNo + " has been Rejected ";

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
                    int HRPVCCID = Convert.ToInt32(id);

                    var mstHRPVCClaim = await _repository.MstHRPVCClaim.GetHRPVCClaimByIdAsync(HRPVCCID);

                    if (mstHRPVCClaim == null)
                    {
                        // return NotFound();
                    }

                    int loggedInUserId = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value);
                    bool isAlternateApprover = false;
                    var delegatedUserId = await _alternateApproverHelper.IsUserHasAnyAlternateApprovalSet(loggedInUserId);
                    if (delegatedUserId.HasValue)
                    {
                        isAlternateApprover = true;
                    }

                    await _repository.MstHRPVCClaim.UpdateMstHRPVCClaimStatus(HRPVCCID, 4, int.Parse(HttpContext.User.FindFirst("userid").Value), DateTime.Now, reason, string.Empty, string.Empty, string.Empty, string.Empty, isAlternateApprover,0);
                    string domainUrl = HttpContext.Request.Scheme + "://" + HttpContext.Request.Host;
                    string clickUrl = domainUrl + "/" + "HRPVChequeClaim/Details/" + HRPVCCID;

                    var mstSenderDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(HttpContext.User.FindFirst("userid").Value));
                    var senderName = mstSenderDetails.Name;
                    var mstVerifierDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(mstHRPVCClaim.UserID));
                    var toEmail = mstVerifierDetails.EmailAddress;
                    var receiverName = mstVerifierDetails.Name;
                    var claimNo = mstHRPVCClaim.HRPVCCNo;
                    var screen = "HR PV Cheque Claim";
                    var approvalType = "Rejected Request";
                    int userID = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value);
                    var subject = "HR PV Cheque Claim " + claimNo + " has been Rejected ";

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

        public async Task<JsonResult> UpdateStatus(string id,string claim)
        {
            bool isAlternateApprover = false;
            if (User != null && User.Identity.IsAuthenticated)
            {
                if (claim!="" && claim.Contains("hpvg"))
                {
                    int HRPVGCID = Convert.ToInt32(id);

                    var mstHRPVGClaim = await _repository.MstHRPVGClaim.GetHRPVGClaimByIdAsync(HRPVGCID);

                    if (mstHRPVGClaim == null)
                    {
                        // return NotFound();
                    }

                    int ApprovedStatus = Convert.ToInt32(mstHRPVGClaim.ApprovalStatus);
                    bool excute = _repository.MstHRPVGClaim.ExistsApproval(HRPVGCID.ToString(), ApprovedStatus, HttpContext.User.FindFirst("userid").Value, "HRPVG");
                    
                    // If execute is false, Check if the current user is alternate user for this claim
                    if (excute == false)
                    {
                        string hodapprover = _repository.MstExpenseClaim.GetApproval(HRPVGCID.ToString(), ApprovedStatus, HttpContext.User.FindFirst("userid").Value, "Expense");
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
                        #region HRPVG UserApprovers
                        if (ApprovedStatus == 6)
                        {
                            string VerifierIDs = "";
                            string ApproverIDs = "";
                            string UserApproverIDs = "";
                            string HODApproverID = "";
                            try
                            {
                                string[] MileageuserApproverIDs = mstHRPVGClaim.UserApprovers.Split(',');
                                UserApproverIDs = string.Join(",", MileageuserApproverIDs.Skip(1));
                                string[] userApproverIDs = UserApproverIDs.ToString().Split(',');
                                ApproverIDs = mstHRPVGClaim.Approver;
                                VerifierIDs = mstHRPVGClaim.Verifier;
                                HODApproverID = mstHRPVGClaim.HODApprover;
                                //Mail Code Implementation for Verifiers
                                foreach (string userApproverID in userApproverIDs)
                                {
                                    if (userApproverID != "")
                                    {
                                        string domainUrl = HttpContext.Request.Scheme + "://" + HttpContext.Request.Host;
                                        string clickUrl = domainUrl + "/" + "HRSummary/HRPVGCDetails/" + HRPVGCID;

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
                                        var claimNo = mstHRPVGClaim.HRPVGCNo;
                                        var screen = "HR PV-GIRO Claim";
                                        var approvalType = "Approval Request";
                                        int userID = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value);
                                        var subject = "HR PV-GIRO Claim for Approval " + claimNo;

                                        BackgroundJob.Enqueue(() => _sendMailServices.SendEmail("EmailTemplate.html",screen, subject, senderName, receiverName, claimNo, approvalType, userID, toEmail, clickUrl));

                                    }
                                    else
                                    {
                                        string domainUrl = HttpContext.Request.Scheme + "://" + HttpContext.Request.Host;
                                        string clickUrl = domainUrl + "/" + "FinanceHRPVGClaim/Details/" + HRPVGCID;

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
                                        var claimNo = mstHRPVGClaim.HRPVGCNo;
                                        var screen = "HR PV-GIRO Claim";
                                        var approvalType = "Verification Request";
                                        int userID = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value);
                                        var subject = "HR PV-GIRO Claim for Verification " + claimNo;

                                        BackgroundJob.Enqueue(() => _sendMailServices.SendEmail("EmailTemplate.html",screen, subject, senderName, receiverName, claimNo, approvalType, userID, toEmail, clickUrl));
                                    }
                                    break;
                                }
                            }
                            catch
                            {
                            }
                            await _repository.MstHRPVGClaim.UpdateMstHRPVGClaimStatus(HRPVGCID, 1, int.Parse(HttpContext.User.FindFirst("userid").Value), DateTime.Now, string.Empty, VerifierIDs.ToString(), ApproverIDs.ToString(), UserApproverIDs.ToString(), HODApproverID.ToString(), isAlternateApprover,0);

                        }
                        #endregion

                        #region HRPVG Verifier
                        if (ApprovedStatus == 1)
                        {
                            string VerifierIDs = "";
                            string ApproverIDs = "";
                            string UserApproverIDs = "";
                            string HODApproverID = "";
                            try
                            {
                                string[] HRPVGverifierIDs = mstHRPVGClaim.Verifier.Split(',');
                                VerifierIDs = string.Join(",", HRPVGverifierIDs.Skip(1));
                                string[] verifierIDs = VerifierIDs.ToString().Split(',');
                                ApproverIDs = mstHRPVGClaim.Approver;
                                UserApproverIDs = mstHRPVGClaim.UserApprovers;
                                HODApproverID = mstHRPVGClaim.HODApprover;

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
                            await _repository.MstHRPVGClaim.UpdateMstHRPVGClaimStatus(HRPVGCID, 2, int.Parse(HttpContext.User.FindFirst("userid").Value), DateTime.Now, string.Empty, VerifierIDs.ToString(), ApproverIDs.ToString(), UserApproverIDs.ToString(), HODApproverID.ToString(), isAlternateApprover,0);

                        }
                        #endregion

                        #region HRPVG Approver
                        if (ApprovedStatus == 2)
                        {
                            string VerifierIDs = "";
                            string ApproverIDs = "";
                            string UserApproverIDs = "";
                            string HODApproverID = "";
                            try
                            {
                                string[] HRPVGapproverIDs = mstHRPVGClaim.Approver.Split(',');
                                ApproverIDs = string.Join(",", HRPVGapproverIDs.Skip(1));
                                string[] approverIDs = ApproverIDs.Split(',');
                                int CreatedBy = Convert.ToInt32(mstHRPVGClaim.CreatedBy);

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
                            await _repository.MstHRPVGClaim.UpdateMstHRPVGClaimStatus(HRPVGCID, 3, int.Parse(HttpContext.User.FindFirst("userid").Value), DateTime.Now, string.Empty, VerifierIDs, ApproverIDs, UserApproverIDs.ToString(), HODApproverID.ToString(), isAlternateApprover, int.Parse(financeStartDay));
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

                    int HRPVCCID = Convert.ToInt32(id);


                    var mstHRPVCClaim = await _repository.MstHRPVCClaim.GetHRPVCClaimByIdAsync(HRPVCCID);

                    if (mstHRPVCClaim == null)
                    {
                        // return NotFound();
                    }


                    int ApprovedStatus = Convert.ToInt32(mstHRPVCClaim.ApprovalStatus);
                    bool excute = _repository.MstHRPVCClaim.ExistsApproval(HRPVCCID.ToString(), ApprovedStatus, HttpContext.User.FindFirst("userid").Value, "HRPVC");

                    // If execute is false, Check if the current user is alternate user for this claim
                    if (excute == false)
                    {
                        string hodapprover = _repository.MstExpenseClaim.GetApproval(HRPVCCID.ToString(), ApprovedStatus, HttpContext.User.FindFirst("userid").Value, "Expense");
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
                        #region HRPVC UserApprovers
                        if (ApprovedStatus == 6)
                        {
                            string VerifierIDs = "";
                            string ApproverIDs = "";
                            string UserApproverIDs = "";
                            string HODApproverID = "";
                            try
                            {
                                string[] MileageuserApproverIDs = mstHRPVCClaim.UserApprovers.Split(',');
                                UserApproverIDs = string.Join(",", MileageuserApproverIDs.Skip(1));
                                string[] userApproverIDs = UserApproverIDs.ToString().Split(',');
                                ApproverIDs = mstHRPVCClaim.Approver;
                                VerifierIDs = mstHRPVCClaim.Verifier;
                                HODApproverID = mstHRPVCClaim.HODApprover;
                                //Mail Code Implementation for Verifiers
                                foreach (string userApproverID in userApproverIDs)
                                {
                                    if (userApproverID != "")
                                    {
                                        string domainUrl = HttpContext.Request.Scheme + "://" + HttpContext.Request.Host;
                                        string clickUrl = domainUrl + "/" + "HRSummary/Details/" + HRPVCCID;

                                        var mstSenderDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(HttpContext.User.FindFirst("userid").Value));
                                        var senderName = mstSenderDetails.Name;
                                        var mstVerifierDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(userApproverID));
                                        var toEmail = mstVerifierDetails.EmailAddress;
                                        var receiverName = mstVerifierDetails.Name;
                                        var claimNo = mstHRPVCClaim.HRPVCCNo;
                                        var screen = "HR PV-Cheque Claim";
                                        var approvalType = "Approval Request";
                                        int userID = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value);
                                        var subject = "HR PV-Cheque Claim for Approval " + claimNo;

                                        BackgroundJob.Enqueue(() => _sendMailServices.SendEmail("EmailTemplate.html",screen, subject, senderName, receiverName, claimNo, approvalType, userID, toEmail, clickUrl));

                                    }
                                    else
                                    {
                                        string domainUrl = HttpContext.Request.Scheme + "://" + HttpContext.Request.Host;
                                        string clickUrl = domainUrl + "/" + "FinanceHRPVCClaim/Details/" + HRPVCCID;

                                        var mstSenderDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(HttpContext.User.FindFirst("userid").Value));
                                        var senderName = mstSenderDetails.Name;
                                        var mstVerifierDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(VerifierIDs.ToString().Split(',')[0].ToString()));
                                        var toEmail = mstVerifierDetails.EmailAddress;
                                        var receiverName = mstVerifierDetails.Name;
                                        var claimNo = mstHRPVCClaim.HRPVCCNo;
                                        var screen = "HR PV-Cheque Claim";
                                        var approvalType = "Verification Request";
                                        int userID = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value);
                                        var subject = "HR PV-Cheque Claim for Verification " + claimNo;

                                        BackgroundJob.Enqueue(() => _sendMailServices.SendEmail("EmailTemplate.html",screen, subject, senderName, receiverName, claimNo, approvalType, userID, toEmail, clickUrl));
                                    }
                                    break;
                                }
                            }
                            catch
                            {
                            }
                            await _repository.MstHRPVCClaim.UpdateMstHRPVCClaimStatus(HRPVCCID, 1, int.Parse(HttpContext.User.FindFirst("userid").Value), DateTime.Now, string.Empty, VerifierIDs.ToString(), ApproverIDs.ToString(), UserApproverIDs.ToString(), HODApproverID.ToString(), isAlternateApprover,0);

                        }
                        #endregion
                        #region HRPVC Verifier
                        if (ApprovedStatus == 1)
                        {
                            string VerifierIDs = "";
                            string ApproverIDs = "";
                            string UserApproverIDs = "";
                            string HODApproverID = "";
                            try
                            {
                                string[] HRPVCverifierIDs = mstHRPVCClaim.Verifier.Split(',');
                                VerifierIDs = string.Join(",", HRPVCverifierIDs.Skip(1));
                                string[] verifierIDs = VerifierIDs.ToString().Split(',');
                                ApproverIDs = mstHRPVCClaim.Approver;
                                UserApproverIDs = mstHRPVCClaim.UserApprovers;
                                HODApproverID = mstHRPVCClaim.HODApprover;
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
                            await _repository.MstHRPVCClaim.UpdateMstHRPVCClaimStatus(HRPVCCID, 2, int.Parse(HttpContext.User.FindFirst("userid").Value), DateTime.Now, string.Empty, VerifierIDs.ToString(), ApproverIDs.ToString(), UserApproverIDs.ToString(), HODApproverID.ToString(), isAlternateApprover,0);

                        }
                        #endregion

                        #region HRPVC Approver
                        if (ApprovedStatus == 2)
                        {
                            string VerifierIDs = "";
                            string ApproverIDs = "";
                            string UserApproverIDs = "";
                            string HODApproverID = "";
                            try
                            {
                                string[] HRPVCapproverIDs = mstHRPVCClaim.Approver.Split(',');
                                ApproverIDs = string.Join(",", HRPVCapproverIDs.Skip(1));
                                string[] approverIDs = ApproverIDs.Split(',');
                                int CreatedBy = Convert.ToInt32(mstHRPVCClaim.CreatedBy);

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
                            await _repository.MstHRPVCClaim.UpdateMstHRPVCClaimStatus(HRPVCCID, 3, int.Parse(HttpContext.User.FindFirst("userid").Value), DateTime.Now, string.Empty, VerifierIDs, ApproverIDs, UserApproverIDs.ToString(), HODApproverID.ToString(), isAlternateApprover, int.Parse(financeStartDay));
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
            var mstHRPVCClaimsWithDetails = await _repository.MstUser.GetAllHRSummaryClaimsAsync(Convert.ToInt32(HttpContext.User.FindFirst("userid").Value), hodClaimSearch.ModuleName, hodClaimSearch.StatusID, hodClaimSearch.FromDate, hodClaimSearch.ToDate);
            List<CustomClaim> hRPVCClaimVMs = new List<CustomClaim>();

            DataTable dt = new DataTable("Grid");
            dt.Columns.AddRange(new DataColumn[11] { new DataColumn("Item"),
                                        new DataColumn("Requester"),
                                        new DataColumn("Date"),
                                        new DataColumn("Particulars of payment"),
                                        new DataColumn("Contact Number"),
                                        new DataColumn("Payee Name"),
                                        new DataColumn("Cheque No"),
                                        new DataColumn("Amount"),
                                        new DataColumn("Total Claim"),
                                        new DataColumn("Approver"),
                                        new DataColumn("Status")});

            foreach (var mc in mstHRPVCClaimsWithDetails)
            {
                CustomClaim hRPVCClaimVM = new CustomClaim();
                hRPVCClaimVM.ApprovalStatus = mc.ApprovalStatus;
                if (mc.ApprovalStatus == 1)
                {
                    hRPVCClaimVM.ClaimStatusName = "Awaiting Verification";

                }
                else if (mc.ApprovalStatus == 2)
                {
                    hRPVCClaimVM.ClaimStatusName = "Awaiting Signatory approval";

                }
                else if (mc.ApprovalStatus == 3)
                {
                    hRPVCClaimVM.ClaimStatusName = "Approved";

                }
                else if (mc.ApprovalStatus == 4)
                {
                    hRPVCClaimVM.ClaimStatusName = "Request to Amend";
                }
                else if (mc.ApprovalStatus == 5)
                {
                    hRPVCClaimVM.ClaimStatusName = "Voided";

                }
                else if (mc.ApprovalStatus == -5)
                {
                    hRPVCClaimVM.ClaimStatusName = "Requested to Void";

                }
                else if (mc.ApprovalStatus == 6)
                {
                    hRPVCClaimVM.ClaimStatusName = "Awaiting approval";

                }
                else if (mc.ApprovalStatus == 7)
                {
                    hRPVCClaimVM.ClaimStatusName = "Awaiting HOD approval";

                }
                else
                {
                    hRPVCClaimVM.ClaimStatusName = "New";
                }

                if (mc.UserApprovers != "")
                {
                    hRPVCClaimVM.Approver = mc.UserApprovers.Split(',').First();
                    if (hRPVCClaimVM.Approver == HttpContext.User.FindFirst("userid").Value && (hRPVCClaimVM.ApprovalStatus == 6))
                    {
                        hRPVCClaimVM.IsActionAllowed = true;
                    }
                }
                else if (mc.HODApprover != "")
                {
                    hRPVCClaimVM.Approver = mc.HODApprover.Split(',').First();
                    if (hRPVCClaimVM.Approver == HttpContext.User.FindFirst("userid").Value && (hRPVCClaimVM.ApprovalStatus == 7))
                    {
                        hRPVCClaimVM.IsActionAllowed = true;
                    }
                }
                else if (mc.Verifier != "")
                {
                    hRPVCClaimVM.Approver = mc.Verifier.Split(',').First();
                    if (hRPVCClaimVM.Approver == HttpContext.User.FindFirst("userid").Value && (hRPVCClaimVM.ApprovalStatus == 1 || hRPVCClaimVM.ApprovalStatus == 2))
                    {
                        hRPVCClaimVM.IsActionAllowed = true;
                    }
                    //string VerifierIDs = string.Join(",", MileageverifierIDs.Skip(1));
                }
                else if (mc.Approver != "")
                {
                    hRPVCClaimVM.Approver = mc.Approver.Split(',').First();
                    if (hRPVCClaimVM.Approver == HttpContext.User.FindFirst("userid").Value && (hRPVCClaimVM.ApprovalStatus == 1 || hRPVCClaimVM.ApprovalStatus == 2))
                    {
                        hRPVCClaimVM.IsActionAllowed = true;
                    }
                }
                else
                {
                    hRPVCClaimVM.Approver = "";
                }

                if (hRPVCClaimVM.Approver != "")
                {
                    var mstUserApprover = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(hRPVCClaimVM.Approver));
                    if (hRPVCClaimVM.ApprovalStatus != 3 && hRPVCClaimVM.ApprovalStatus != 4 && hRPVCClaimVM.ApprovalStatus != -5 && hRPVCClaimVM.ApprovalStatus != 5)
                        hRPVCClaimVM.Approver = mstUserApprover.Name;
                    else
                        hRPVCClaimVM.Approver = "";
                }

                //mileageClaimVMs.Add(mileageClaimVM);
                dt.Rows.Add(hRPVCClaimVM.CNO = mc.CNO,
                            hRPVCClaimVM.Name = mc.Name,
                            hRPVCClaimVM.CreatedDate = Convert.ToDateTime(mc.CreatedDate).ToString("dd/MM/yyyy", System.Globalization.CultureInfo.CreateSpecificCulture("en-US")),
                            hRPVCClaimVM.ParticularsOfPayment = mc.ParticularsOfPayment,
                            hRPVCClaimVM.Phone = mc.Phone,
                            hRPVCClaimVM.Name = mc.PayeeName,
                            hRPVCClaimVM.ChequeNo = mc.ChequeNo,
                            hRPVCClaimVM.Amount = mc.Amount,
                            hRPVCClaimVM.GrandTotal = mc.GrandTotal,
                            hRPVCClaimVM.Approver = hRPVCClaimVM.Approver,
                            hRPVCClaimVM.ClaimStatusName = hRPVCClaimVM.ClaimStatusName);
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

            string filename = "HRSummary-Export" + DateTime.Now.ToString("ddMMyyyyss") + ".xlsx";
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
                        return File(blobStream, file.Properties.ContentType, "HRSummary-Export.xlsx");
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

        public async Task<IActionResult> GetPrintHRPVGClaimDetails(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            long HRPVGCID = Convert.ToInt64(id);
            HRPVGClaimDetailVM hRPVGClaimDetailVM = new HRPVGClaimDetailVM();
            if (User != null && User.Identity.IsAuthenticated)
            {
                var mstHRPVGClaim = await _repository.MstHRPVGClaim.GetHRPVGClaimByIdAsync(id);

                if (mstHRPVGClaim == null)
                {
                    return NotFound();
                }

                var dtHRPVGSummaries = await _repository.DtHRPVGClaimSummary.GetDtHRPVGClaimSummaryByIdAsync(id);

                var dtHRPVGClaims = await _repository.DtHRPVGClaim.GetDtHRPVGClaimByIdAsync(id);

                //List<DtMileageClaimVM> dtMileageClaimVMs = new List<DtMileageClaimVM>();
                hRPVGClaimDetailVM.DtHRPVGClaimVMs = new List<DtHRPVGClaimVM>();
                // List<clsDtSupplierPO> oclsDtSupplierPO = new List<clsDtSupplierPO>();
                foreach (var item in dtHRPVGClaims)
                {
                    DtHRPVGClaimVM dtHRPVGClaimVM = new DtHRPVGClaimVM();

                    dtHRPVGClaimVM.HRPVGCItemID = item.HRPVGCItemID;
                    dtHRPVGClaimVM.HRPVGCID = item.HRPVGCID;
                    dtHRPVGClaimVM.StaffName = item.StaffName;
                    dtHRPVGClaimVM.Reason = item.Reason;
                    dtHRPVGClaimVM.EmployeeNo = item.EmployeeNo;
                    dtHRPVGClaimVM.ChequeNo = item.ChequeNo;
                    dtHRPVGClaimVM.Amount = item.Amount;
                    dtHRPVGClaimVM.GST = item.GST;
                    dtHRPVGClaimVM.AmountWithGST = item.Amount + item.GST;
                    if (item.FacilityID != null)
                    {
                        var mstFacility = await _repository.MstFacility.GetFacilityByIdAsync(item.FacilityID);
                        dtHRPVGClaimVM.Facility = mstFacility.FacilityName;
                    }
                    dtHRPVGClaimVM.AccountCode = item.AccountCode;
                    dtHRPVGClaimVM.Date = item.Date;
                    dtHRPVGClaimVM.Bank = item.Bank;
                    dtHRPVGClaimVM.BankCode = item.BankCode;
                    dtHRPVGClaimVM.BranchCode = item.BranchCode;
                    dtHRPVGClaimVM.BankAccount = item.BankAccount;
                    dtHRPVGClaimVM.Mobile = item.Mobile;


                    hRPVGClaimDetailVM.DtHRPVGClaimVMs.Add(dtHRPVGClaimVM);
                }

                hRPVGClaimDetailVM.DtHRPVGClaimSummaries = dtHRPVGSummaries;

                var GroupByQS = hRPVGClaimDetailVM.DtHRPVGClaimVMs.GroupBy(s => s.ExpenseCategoryID);
                hRPVGClaimDetailVM.DtHRPVGClaimVMSummary = new List<DtHRPVGClaimVM>();

                foreach (var group in GroupByQS)
                {
                    DtHRPVGClaimVM dtHRPVGClaimVM = new DtHRPVGClaimVM();
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
                            ExpenseDesc = dtExpense.Reason;
                        i++;
                        amount = amount + dtExpense.Amount;
                        //gst = gst + dtExpense.Gst;
                        //sumamount = sumamount + dtExpense.AmountWithGST;
                        ExpenseCat = "Payroll Control";
                        AccountCode = dtExpense.AccountCode;
                    }
                    gst = gst / group.Count();
                    dtHRPVGClaimVM.Particulars = ExpenseDesc;
                    dtHRPVGClaimVM.ExpenseCategory = ExpenseCat;
                    dtHRPVGClaimVM.AccountCode = AccountCode;
                    dtHRPVGClaimVM.Amount = amount;
                    //dtMileageClaimVM.Gst = gst;
                    //dtTBClaimVM.AmountWithGST = sumamount;
                    hRPVGClaimDetailVM.DtHRPVGClaimVMSummary.Add(dtHRPVGClaimVM);
                }


                hRPVGClaimDetailVM.HRPVGClaimAudits = new List<HRPVGClaimAuditVM>();

                var dtHRPVGClaimAudits = await _repository.MstHRPVGClaimAudit.GetMstHRPVGClaimAuditByIdAsync(id);

                foreach (var item in dtHRPVGClaimAudits)
                {
                    HRPVGClaimAuditVM mstHRPVGClaimAuditVM = new HRPVGClaimAuditVM();
                    mstHRPVGClaimAuditVM.Action = item.Action;
                    mstHRPVGClaimAuditVM.Description = item.Description;
                    mstHRPVGClaimAuditVM.AuditDateTickle = Helper.RelativeDate(item.AuditDate);
                    hRPVGClaimDetailVM.HRPVGClaimAudits.Add(mstHRPVGClaimAuditVM);
                }

                hRPVGClaimDetailVM.HRPVGClaimFileUploads = new List<DtHRPVGClaimFileUpload>();

                hRPVGClaimDetailVM.HRPVGClaimFileUploads = _repository.DtHRPVGClaimFileUpload.GetDtHRPVGClaimAuditByIdAsync(id).Result.ToList();

                HRPVGClaimVM hRPVGClaimVM = new HRPVGClaimVM();
                hRPVGClaimVM.VoucherNo = mstHRPVGClaim.VoucherNo;
                hRPVGClaimVM.ParticularsOfPayment = mstHRPVGClaim.ParticularsOfPayment;
                hRPVGClaimVM.Amount = mstHRPVGClaim.Amount;
                hRPVGClaimVM.ChequeNo = mstHRPVGClaim.ChequeNo;
                hRPVGClaimVM.GrandTotal = mstHRPVGClaim.GrandTotal;
                hRPVGClaimVM.TotalAmount = mstHRPVGClaim.TotalAmount;
                hRPVGClaimVM.Name = mstHRPVGClaim.MstUser.Name;
                hRPVGClaimVM.DepartmentName = mstHRPVGClaim.MstDepartment.Department;
                hRPVGClaimVM.FacilityName = mstHRPVGClaim.MstFacility.FacilityName;
                hRPVGClaimVM.CreatedDate = Convert.ToDateTime(mstHRPVGClaim.CreatedDate).ToString("dd/MM/yyyy", System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
                hRPVGClaimVM.Verifier = mstHRPVGClaim.Verifier;
                hRPVGClaimVM.Approver = mstHRPVGClaim.Approver;
                hRPVGClaimVM.HRPVGCNo = mstHRPVGClaim.HRPVGCNo;
                hRPVGClaimVM.Company = "UEMS";
                hRPVGClaimVM.PaymentMode = mstHRPVGClaim.PaymentMode;
                ViewBag.HRPVGCID = id;
                hRPVGClaimDetailVM.HRPVGClaimVM = hRPVGClaimVM;
            }
            return PartialView("GetHRPVGDetailsPrint", hRPVGClaimDetailVM);
        }

        public async Task<IActionResult> GetPrintClaimDetails(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            long HRPVCCID = Convert.ToInt64(id);
            HRPVCClaimDetailVM hRPVCClaimDetailVM = new HRPVCClaimDetailVM();
            if (User != null && User.Identity.IsAuthenticated)
            {
                var mstHRPVCClaim = await _repository.MstHRPVCClaim.GetHRPVCClaimByIdAsync(id);

                if (mstHRPVCClaim == null)
                {
                    return NotFound();
                }

                var dtHRPVCSummaries = await _repository.DtHRPVCClaimSummary.GetDtHRPVCClaimSummaryByIdAsync(id);
                var dtHRPVCClaims = await _repository.DtHRPVCClaim.GetDtHRPVCClaimByIdAsync(id);

                //List<DtMileageClaimVM> dtMileageClaimVMs = new List<DtMileageClaimVM>();
                hRPVCClaimDetailVM.DtHRPVCClaimVMs = new List<DtHRPVCClaimVM>();
                // List<clsDtSupplierPO> oclsDtSupplierPO = new List<clsDtSupplierPO>();
                foreach (var item in dtHRPVCClaims)
                {
                    DtHRPVCClaimVM dtHRPVCClaimVM = new DtHRPVCClaimVM();

                    dtHRPVCClaimVM.HRPVCCItemID = item.HRPVCCItemID;
                    dtHRPVCClaimVM.HRPVCCID = item.HRPVCCID;
                    dtHRPVCClaimVM.Date = item.Date;
                    dtHRPVCClaimVM.StaffName = item.StaffName;
                    dtHRPVCClaimVM.Reason = item.Reason;
                    dtHRPVCClaimVM.EmployeeNo = item.EmployeeNo;
                    if (item.FacilityID != null)
                    {
                        var mstFacility = await _repository.MstFacility.GetFacilityByIdAsync(item.FacilityID);
                        dtHRPVCClaimVM.Facility = mstFacility.FacilityName;
                    }
                    dtHRPVCClaimVM.Amount = item.Amount;
                    dtHRPVCClaimVM.GST = item.GST;
                    dtHRPVCClaimVM.ChequeNo = item.ChequeNo;
                    dtHRPVCClaimVM.AmountWithGST = item.Amount + item.GST;
                    //dtHRPVCClaimVM.ExpenseCategory = item.MstExpenseCategory.Description;
                    dtHRPVCClaimVM.AccountCode = item.AccountCode;


                    hRPVCClaimDetailVM.DtHRPVCClaimVMs.Add(dtHRPVCClaimVM);
                }

                hRPVCClaimDetailVM.DtHRPVCClaimSummaries = dtHRPVCSummaries;
                var GroupByQS = hRPVCClaimDetailVM.DtHRPVCClaimVMs.GroupBy(s => s.ExpenseCategoryID);

                hRPVCClaimDetailVM.DtHRPVCClaimVMSummary = new List<DtHRPVCClaimVM>();

                foreach (var group in GroupByQS)
                {
                    DtHRPVCClaimVM dtHRPVCClaimVM = new DtHRPVCClaimVM();
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
                            ExpenseDesc = dtExpense.Reason;
                        i++;
                        amount = amount + dtExpense.Amount;
                        //gst = gst + dtExpense.Gst;
                        //sumamount = sumamount + dtExpense.AmountWithGST;
                        ExpenseCat = "Payroll Control";
                        AccountCode = dtExpense.AccountCode;
                    }
                    gst = gst / group.Count();
                    dtHRPVCClaimVM.Particulars = ExpenseDesc;
                    dtHRPVCClaimVM.ExpenseCategory = ExpenseCat;
                    dtHRPVCClaimVM.AccountCode = AccountCode;
                    dtHRPVCClaimVM.Amount = amount;
                    //dtMileageClaimVM.Gst = gst;
                    //dtTBClaimVM.AmountWithGST = sumamount;
                    hRPVCClaimDetailVM.DtHRPVCClaimVMSummary.Add(dtHRPVCClaimVM);
                }

                hRPVCClaimDetailVM.HRPVCClaimAudits = new List<HRPVCClaimAuditVM>();

                var dtHRPVCClaimAudits = await _repository.MstHRPVCClaimAudit.GetMstHRPVCClaimAuditByIdAsync(id);

                foreach (var item in dtHRPVCClaimAudits)
                {
                    HRPVCClaimAuditVM mstHRPVCClaimAuditVM = new HRPVCClaimAuditVM();
                    mstHRPVCClaimAuditVM.Action = item.Action;
                    mstHRPVCClaimAuditVM.Description = item.Description;
                    mstHRPVCClaimAuditVM.AuditDateTickle = Helper.RelativeDate(item.AuditDate);
                    hRPVCClaimDetailVM.HRPVCClaimAudits.Add(mstHRPVCClaimAuditVM);
                }

                hRPVCClaimDetailVM.HRPVCClaimFileUploads = new List<DtHRPVCClaimFileUpload>();

                hRPVCClaimDetailVM.HRPVCClaimFileUploads = _repository.DtHRPVCClaimFileUpload.GetDtHRPVCClaimAuditByIdAsync(id).Result.ToList();

                HRPVCClaimVM hRPVCClaimVM = new HRPVCClaimVM();
                //hRPVCClaimVM.ClaimType = mstHRPVCClaim.ClaimType;
                hRPVCClaimVM.VoucherNo = mstHRPVCClaim.VoucherNo;
                hRPVCClaimVM.ParticularsOfPayment = mstHRPVCClaim.ParticularsOfPayment;
                hRPVCClaimVM.Amount = mstHRPVCClaim.Amount;
                hRPVCClaimVM.ChequeNo = mstHRPVCClaim.ChequeNo;
                hRPVCClaimVM.GrandTotal = mstHRPVCClaim.GrandTotal;
                hRPVCClaimVM.TotalAmount = mstHRPVCClaim.TotalAmount;
                hRPVCClaimVM.Company = "UEMS";
                hRPVCClaimVM.Name = mstHRPVCClaim.MstUser.Name;
                hRPVCClaimVM.DepartmentName = mstHRPVCClaim.MstDepartment.Department;
                hRPVCClaimVM.FacilityName = mstHRPVCClaim.MstFacility.FacilityName;
                hRPVCClaimVM.CreatedDate = Convert.ToDateTime(mstHRPVCClaim.CreatedDate).ToString("dd/MM/yyyy", System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
                hRPVCClaimVM.Verifier = mstHRPVCClaim.Verifier;
                hRPVCClaimVM.Approver = mstHRPVCClaim.Approver;
                hRPVCClaimVM.HRPVCCNo = mstHRPVCClaim.HRPVCCNo;
                ViewBag.HRPVCCID = id;
                hRPVCClaimDetailVM.HRPVCClaimVM = hRPVCClaimVM;
                //mileageClaimDetailVM.DtMileageClaimVMs = dtMileageClaimVMs;
            }
            return PartialView("GetHRPVCDetailsPrint", hRPVCClaimDetailVM);
        }
        public async Task<IActionResult> GetPrint(string data)
        {
            var hodClaimSearch = JsonConvert.DeserializeObject<HodClaimSearch>(data);

            var mstHRPVCClaimsWithDetails = await _repository.MstUser.GetAllHRSummaryClaimsAsync(Convert.ToInt32(HttpContext.User.FindFirst("userid").Value), hodClaimSearch.ModuleName, hodClaimSearch.StatusID, hodClaimSearch.FromDate, hodClaimSearch.ToDate);
            List<CustomClaim> hRPVCClaimVMs = new List<CustomClaim>();

            foreach (var mc in mstHRPVCClaimsWithDetails)
            {
                CustomClaim hRPVCClaimVM = new CustomClaim();

                hRPVCClaimVM.CNO = mc.CNO;
                hRPVCClaimVM.Name = mc.Name;
                hRPVCClaimVM.CreatedDate = Convert.ToDateTime(mc.CreatedDate).ToString("dd/MM/yyyy", System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
                hRPVCClaimVM.ParticularsOfPayment = mc.ParticularsOfPayment;
                hRPVCClaimVM.Phone = mc.Phone;
                hRPVCClaimVM.GrandTotal = mc.GrandTotal;
                hRPVCClaimVM.TotalAmount = mc.TotalAmount;
                hRPVCClaimVM.ApprovalStatus = mc.ApprovalStatus;
                hRPVCClaimVM.PayeeName = mc.PayeeName;
                hRPVCClaimVM.ChequeNo = mc.ChequeNo;
                hRPVCClaimVM.Amount = mc.Amount;

                if (mc.ApprovalStatus == 1)
                {
                    hRPVCClaimVM.ClaimStatusName = "Awaiting Verification";

                }
                else if (mc.ApprovalStatus == 2)
                {
                    hRPVCClaimVM.ClaimStatusName = "Awaiting Signatory approval";

                }
                else if (mc.ApprovalStatus == 3)
                {
                    hRPVCClaimVM.ClaimStatusName = "Approved";

                }
                else if (mc.ApprovalStatus == 4)
                {
                    hRPVCClaimVM.ClaimStatusName = "Request to Amend";
                }
                else if (mc.ApprovalStatus == 5)
                {
                    hRPVCClaimVM.ClaimStatusName = "Voided";

                }
                else if (mc.ApprovalStatus == -5)
                {
                    hRPVCClaimVM.ClaimStatusName = "Requested to Void";

                }
                else if (mc.ApprovalStatus == 6)
                {
                    hRPVCClaimVM.ClaimStatusName = "Awaiting approval";

                }
                else if (mc.ApprovalStatus == 7)
                {
                    hRPVCClaimVM.ClaimStatusName = "Awaiting HOD approval";

                }
                else
                {
                    hRPVCClaimVM.ClaimStatusName = "New";
                }

                if (mc.UserApprovers != "")
                {
                    hRPVCClaimVM.Approver = mc.UserApprovers.Split(',').First();
                    if (hRPVCClaimVM.Approver == HttpContext.User.FindFirst("userid").Value && (hRPVCClaimVM.ApprovalStatus == 6))
                    {
                        hRPVCClaimVM.IsActionAllowed = true;
                    }
                }
                else if (mc.HODApprover != "")
                {
                    hRPVCClaimVM.Approver = mc.HODApprover.Split(',').First();
                    if (hRPVCClaimVM.Approver == HttpContext.User.FindFirst("userid").Value && (hRPVCClaimVM.ApprovalStatus == 7))
                    {
                        hRPVCClaimVM.IsActionAllowed = true;
                    }
                }
                else if (mc.Verifier != "")
                {
                    hRPVCClaimVM.Approver = mc.Verifier.Split(',').First();
                    if (hRPVCClaimVM.Approver == HttpContext.User.FindFirst("userid").Value && (hRPVCClaimVM.ApprovalStatus == 1 || hRPVCClaimVM.ApprovalStatus == 2))
                    {
                        hRPVCClaimVM.IsActionAllowed = true;
                    }
                    //string VerifierIDs = string.Join(",", MileageverifierIDs.Skip(1));
                }
                else if (mc.Approver != "")
                {
                    hRPVCClaimVM.Approver = mc.Approver.Split(',').First();
                    if (hRPVCClaimVM.Approver == HttpContext.User.FindFirst("userid").Value && (hRPVCClaimVM.ApprovalStatus == 1 || hRPVCClaimVM.ApprovalStatus == 2))
                    {
                        hRPVCClaimVM.IsActionAllowed = true;
                    }
                }
                else
                {
                    hRPVCClaimVM.Approver = "";
                }

                if (hRPVCClaimVM.Approver != "")
                {
                    var mstUserApprover = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(hRPVCClaimVM.Approver));
                    if (hRPVCClaimVM.ApprovalStatus != 3 && hRPVCClaimVM.ApprovalStatus != 4 && hRPVCClaimVM.ApprovalStatus != -5 && hRPVCClaimVM.ApprovalStatus != 5)
                        hRPVCClaimVM.Approver = mstUserApprover.Name;
                    else
                        hRPVCClaimVM.Approver = "";
                }
                hRPVCClaimVMs.Add(hRPVCClaimVM);
            }
            return PartialView("GetSummaryPrint", hRPVCClaimVMs);
        }


        public async Task<IActionResult> Download(string id, string name,string claim)
        {
            if (claim != "" && claim.Contains("hrpvg"))
            {
                MemoryStream ms = new MemoryStream();
                if (CloudStorageAccount.TryParse(_configuration.GetSection("ConnectionStrings")["BlobConnectionString"], out CloudStorageAccount storageAccount))
                {
                    CloudBlobClient BlobClient = storageAccount.CreateCloudBlobClient();
                    CloudBlobContainer container = BlobClient.GetContainerReference(_configuration.GetSection("ConnectionStrings")["BlobContainerName"]);

                    if (await container.ExistsAsync())
                    {
                        CloudBlob file = container.GetBlobReference("FileUploads/HRPVGClaimFiles/" + id);

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
                        CloudBlob file = container.GetBlobReference("FileUploads/HRPVCClaimFiles/" + id);

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
            if (queryParamViewModel.Claim != "" && queryParamViewModel.Claim.Contains("hpvg"))
            {
                var UserIds = queryParamViewModel.RecieverId.Select(s => int.Parse(s)).ToArray();
                if (HttpContext.User.FindFirst("userid").Value != null)
                {
                    var result = "";
                    try
                    {
                        long HRPVGCID = Convert.ToInt64(queryParamViewModel.Cid);
                        int UserID = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value);
                        // newly Added Code
                        var hRPVGClaim = await _repository.MstHRPVGClaim.GetHRPVGClaimByIdAsync(HRPVGCID);
                        for (int i = 0; i < UserIds.Length; i++)
                        {
                            MstQuery clsdtHRPVGQuery = new MstQuery();
                            // if (data["MessageDescription"] != null)               
                            clsdtHRPVGQuery.ModuleType = "HRPVG Claim";
                            //  clsdtSupplierQuery.ID = Convert.ToInt64(data["SPOID"]);
                            clsdtHRPVGQuery.ID = HRPVGCID;
                            clsdtHRPVGQuery.SenderID = UserID;
                            //var recieverId = data["queryusers"];       
                            clsdtHRPVGQuery.ReceiverID = Convert.ToInt32(UserIds[i]);
                            clsdtHRPVGQuery.MessageDescription = queryParamViewModel.Message;
                            clsdtHRPVGQuery.SentTime = DateTime.Now;
                            //clsdtExpenseQuery.NotificationStatus = false;
                            await _repository.MstQuery.CreateQuery(clsdtHRPVGQuery);
                            //await _repository.SaveAsync();
                            //objERPEntities.AddToMstQueries(clsdtSupplierQuery);
                            //objERPEntities.SaveChanges();
                            result = "Success";

                            var receiver = await _repository.MstUser.GetUserByIdAsync(UserIds[i]);
                            //var reciever = objERPEntities.MstUsers.ToList().Where(p => p.UserID == Convert.ToInt32(UserIds[i]) && p.InstanceID == int.Parse(Session["InstanceID"].ToString())).ToList().FirstOrDefault();
                            MstHRPVGClaimAudit auditUpdate = new MstHRPVGClaimAudit();
                            auditUpdate.HRPVGCID = HRPVGCID;
                            auditUpdate.Action = "0";
                            auditUpdate.AuditDate = DateTime.Now;
                            auditUpdate.AuditBy = UserID;
                            //auditUpdate.InstanceID = 1;
                            string time = DateTime.Now.ToString("tt", System.Globalization.CultureInfo.InvariantCulture);
                            DateTime date = DateTime.Now;
                            string formattedDate = date.ToString("dd'/'MM'/'yyyy hh:mm:ss");
                            auditUpdate.Description = "" + User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.GivenName).Value.ToString() + " Sent Query to " + receiver.Name + " on " + formattedDate + " " + time + " ";
                            auditUpdate.SentTo = receiver.Name;
                            await _repository.MstHRPVGClaimAudit.CreateHRPVGClaimAudit(auditUpdate);
                            await _repository.SaveAsync();

                            string domainUrl = HttpContext.Request.Scheme + "://" + HttpContext.Request.Host;
                            string clickUrl = string.Empty;

                            if (hRPVGClaim.CreatedBy.ToString().Contains(UserIds[i].ToString()))
                                clickUrl = domainUrl + "/" + "HRPVGiroClaim/Details/" + HRPVGCID;
                            else if (hRPVGClaim.DApprover.Contains(UserIds[i].ToString()) || hRPVGClaim.DVerifier.Contains(UserIds[i].ToString()))
                                clickUrl = domainUrl + "/" + "FinanceHRPVGClaim/Details/" + HRPVGCID;
                            else
                                clickUrl = domainUrl + "/" + "HRSummary/HRPVGCDetails/" + HRPVGCID;
                            //if (hRPVGClaim.DUserApprovers.Contains(UserIds[i].ToString()) || hRPVGClaim.DHODApprover.Contains(UserIds[i].ToString()))

                            //var mstSenderDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(HttpContext.User.FindFirst("userid").Value));
                            var senderName = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.GivenName).Value.ToString();
                            //var mstVerifierDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(approverID));
                            var toEmail = receiver.EmailAddress;
                            var receiverName = receiver.Name;
                            var claimNo = hRPVGClaim.HRPVGCNo;
                            var screen = "HR PV-GIRO Claim";
                            var approvalType = "Query";
                            int userID = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value);
                            var subject = "HR PV-GIRO Claim Query " + claimNo;
                            BackgroundJob.Enqueue(() => _sendMailServices.SendEmail("QueryTemplate.html", screen, subject, senderName, receiverName, claimNo, approvalType, userID, toEmail, clickUrl, string.Empty, string.Empty, queryParamViewModel.Message));

                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Something went wrong inside CreateHRPVGClaimAudit action: {ex.Message}");
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
            else
            {
                var UserIds = queryParamViewModel.RecieverId.Select(s => int.Parse(s)).ToArray();
                if (HttpContext.User.FindFirst("userid").Value != null)
                {
                    var result = "";
                    try
                    {
                        long HRPVCCID = Convert.ToInt64(queryParamViewModel.Cid);
                        int UserID = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value);
                        // newly Added Code
                        var hRPVCClaim = await _repository.MstHRPVCClaim.GetHRPVCClaimByIdAsync(HRPVCCID);
                        for (int i = 0; i < UserIds.Length; i++)
                        {
                            MstQuery clsdtHRPVCQuery = new MstQuery();
                            // if (data["MessageDescription"] != null)               
                            clsdtHRPVCQuery.ModuleType = "HRPVC Claim";
                            //  clsdtSupplierQuery.ID = Convert.ToInt64(data["SPOID"]);
                            clsdtHRPVCQuery.ID = HRPVCCID;
                            clsdtHRPVCQuery.SenderID = UserID;
                            //var recieverId = data["queryusers"];       
                            clsdtHRPVCQuery.ReceiverID = Convert.ToInt32(UserIds[i]);
                            clsdtHRPVCQuery.MessageDescription = queryParamViewModel.Message;
                            clsdtHRPVCQuery.SentTime = DateTime.Now;
                            //clsdtMileageQuery.NotificationStatus = false;
                            await _repository.MstQuery.CreateQuery(clsdtHRPVCQuery);
                            //await _repository.SaveAsync();
                            //objERPEntities.AddToMstQueries(clsdtSupplierQuery);
                            //objERPEntities.SaveChanges();
                            result = "Success";

                            var receiver = await _repository.MstUser.GetUserByIdAsync(UserIds[i]);
                            //var reciever = objERPEntities.MstUsers.ToList().Where(p => p.UserID == Convert.ToInt32(UserIds[i]) && p.InstanceID == int.Parse(Session["InstanceID"].ToString())).ToList().FirstOrDefault();
                            MstHRPVCClaimAudit auditUpdate = new MstHRPVCClaimAudit();
                            auditUpdate.HRPVCCID = HRPVCCID;
                            auditUpdate.Action = "0";
                            auditUpdate.AuditDate = DateTime.Now;
                            auditUpdate.AuditBy = UserID;
                            //auditUpdate.InstanceID = 1;
                            string time = DateTime.Now.ToString("tt", System.Globalization.CultureInfo.InvariantCulture);
                            DateTime date = DateTime.Now;
                            string formattedDate = date.ToString("dd'/'MM'/'yyyy hh:mm:ss A");
                            auditUpdate.Description = "" + User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.GivenName).Value.ToString() + " Sent Query to " + receiver.Name + " on " + formattedDate + " " + time + " ";
                            auditUpdate.SentTo = receiver.Name;
                            await _repository.MstHRPVCClaimAudit.CreateHRPVCClaimAudit(auditUpdate);
                            await _repository.SaveAsync();

                            string domainUrl = HttpContext.Request.Scheme + "://" + HttpContext.Request.Host;
                            string clickUrl = string.Empty;

                            if (hRPVCClaim.CreatedBy.ToString().Contains(UserIds[i].ToString()))
                                clickUrl = domainUrl + "/" + "HRPVChequeClaim/Details/" + HRPVCCID;
                            else if (hRPVCClaim.DApprover.Contains(UserIds[i].ToString()) || hRPVCClaim.DVerifier.Contains(UserIds[i].ToString()))
                                clickUrl = domainUrl + "/" + "FinanceHRPVCClaim/Details/" + HRPVCCID;
                            else
                                clickUrl = domainUrl + "/" + "HRSummary/HRPVCCDetails/" + HRPVCCID;
                            //if (hRPVCClaim.DUserApprovers.Contains(UserIds[i].ToString()) || hRPVCClaim.DHODApprover.Contains(UserIds[i].ToString()))

                            //var mstSenderDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(HttpContext.User.FindFirst("userid").Value));
                            var senderName = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.GivenName).Value.ToString();
                            //var mstVerifierDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(approverID));
                            var toEmail = receiver.EmailAddress;
                            var receiverName = receiver.Name;
                            var claimNo = hRPVCClaim.HRPVCCNo;
                            var screen = "HR PV-Cheque Claim";
                            var approvalType = "Query";
                            int userID = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value);
                            var subject = "HR PV-Cheque Claim Query " + claimNo;
                            BackgroundJob.Enqueue(() => _sendMailServices.SendEmail("QueryTemplate.html", screen, subject, senderName, receiverName, claimNo, approvalType, userID, toEmail, clickUrl, string.Empty, string.Empty, queryParamViewModel.Message));

                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Something went wrong inside CreateHRPVCClaim action: {ex.Message}");
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
        }
        #endregion SendMessage

        #region -- GetMessages --

        public async Task<JsonResult> GetMessages(string id,string claim)
        {
            if (claim != "" && claim.Contains("hpvg"))
            {
                try
                {
                    var result = new LinkedList<object>();

                    //   var spoid = Convert.ToInt64(Session["id"]);
                    var hrpvgcid = Convert.ToInt32(id);
                    int UserId = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value);
                    ViewBag.userID = UserId;
                    //var queries1 = _context.mstQuery.ToList().Where(j => j.ID == smcid && (j.SenderID == UserId || j.ReceiverID == UserId) && j.ModuleType.ToString().Trim() == "Expense Claim").OrderBy(j => j.SentTime);
                    var queries = await _repository.MstQuery.GetAllClaimsQueriesAsync(UserId, hrpvgcid, "HRPVG Claim");
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
                    _logger.LogError($"Something went wrong inside CreateHRPVGClaimAudit action: {ex.Message}");
                }
                return Json(null);
            }
            else
            {
                try
                {
                    var result = new LinkedList<object>();

                    //   var spoid = Convert.ToInt64(Session["id"]);
                    var shrpvccid = Convert.ToInt32(id);
                    int UserId = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value);
                    ViewBag.userID = UserId;
                    //var queries1 = _context.mstQuery.ToList().Where(j => j.ID == smcid && (j.SenderID == UserId || j.ReceiverID == UserId) && j.ModuleType.ToString().Trim() == "Mileage Claim").OrderBy(j => j.SentTime);
                    var queries = await _repository.MstQuery.GetAllClaimsQueriesAsync(UserId, shrpvccid, "HRPVC Claim");
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
                    _logger.LogError($"Something went wrong inside CreateHRPVCClaimAudit action: {ex.Message}");
                }
                return Json(null);
            }
        }

        #endregion GetMessages
    }
}
