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
    [Authorize(Roles = "Admin,Finance")]
    public class FinanceHRPVGClaimController : Controller
    {
        private ILoggerManager _logger;
        private IRepositoryWrapper _repository;
        private IMapper _mapper;
        private IConfiguration _configuration;
        private AlternateApproverHelper _alternateApproverHelper;
        private ISendMailServices _sendMailServices;
        private readonly RepositoryContext _context;

        public FinanceHRPVGClaimController(ILoggerManager logger, IRepositoryWrapper repository, IMapper mapper, RepositoryContext context, IConfiguration configuration, ISendMailServices sendMailServices)
        {
            _logger = logger;
            _repository = repository;
            _mapper = mapper;
            _context = context;
            _configuration = configuration;
            _sendMailServices = sendMailServices;
            _alternateApproverHelper = new AlternateApproverHelper(logger, repository, context);
        }

        public async Task<IActionResult> Index(int userID, int facilityID, int statusID, string fromDate, string toDate)
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
                oclsModule.Add(new clsModule() { ModuleName = "Approved", ModuleId = "3" });
                oclsModule.Add(new clsModule() { ModuleName = "Awaiting Approval", ModuleId = "6" });
                oclsModule.Add(new clsModule() { ModuleName = "Awaiting HOD Approval", ModuleId = "7" });
                oclsModule.Add(new clsModule() { ModuleName = "Awaiting Signatory approval", ModuleId = "2" });
                oclsModule.Add(new clsModule() { ModuleName = "Awaiting Verification", ModuleId = "1" });
                oclsModule.Add(new clsModule() { ModuleName = "Exported to AccPac", ModuleId = "9" });
                oclsModule.Add(new clsModule() { ModuleName = "Exported to Bank", ModuleId = "10" });
                oclsModule.Add(new clsModule() { ModuleName = "Requested for Void", ModuleId = "-5" });
                oclsModule.Add(new clsModule() { ModuleName = "Request to Amend", ModuleId = "4" });
                oclsModule.Add(new clsModule() { ModuleName = "Voided", ModuleId = "5" });

                List<SelectListItem> status = (from t in oclsModule
                                               select new SelectListItem
                                               {
                                                   Text = t.ModuleName.ToString(),
                                                   Value = t.ModuleId.ToString(),
                                               }).OrderBy(p => p.Text).ToList();

                var mstFacilities = await _repository.MstFacility.GetAllFacilityAsync("active");
                List<SelectListItem> facilities = (from t in mstFacilities
                                                   select new SelectListItem
                                                   {
                                                       Text = t.FacilityName.ToString(),
                                                       Value = t.FacilityID.ToString(),
                                                   }).OrderBy(p => p.Text).ToList();

                var mstUsers = await _repository.MstUser.GetAllUsersAsync("active");
                List<SelectListItem> users = (from t in mstUsers
                                              select new SelectListItem
                                              {
                                                  Text = t.Name.ToString(),
                                                  Value = t.UserID.ToString(),
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

                var mstHRPVGClaimsWithDetails = await _repository.MstHRPVGClaim.GetAllHRPVGClaimWithDetailsByFacilityIDAsync(userID, facilityID, statusID, fromDate, toDate);

                if (mstHRPVGClaimsWithDetails != null && mstHRPVGClaimsWithDetails.Any())
                {
                    mstHRPVGClaimsWithDetails.ToList().ForEach(c => c.IsDelegated = false);
                }

                if (delegatedUserId != null && delegatedUserId.HasValue)
                {
                    var delegatedClaims = await _repository.MstHRPVGClaim.GetAllHRPVGClaimWithDetailsByFacilityIDAsync(delegatedUserId.Value, facilityID, statusID, fromDate, toDate);
                    if (delegatedClaims != null && delegatedClaims.Any())
                    {
                        delegatedClaims.ToList().ForEach(c => c.IsDelegated = true);
                        mstHRPVGClaimsWithDetails.ToList().AddRange(delegatedClaims.ToList());
                    }
                }

                _logger.LogInfo($"Returned all HRPVG Claims with details from database.");
                List<CustomHRPVGClaim> hRPVGClaimVMs = new List<CustomHRPVGClaim>();
                foreach (var mc in mstHRPVGClaimsWithDetails)
                {
                    CustomHRPVGClaim hRPVGClaimVM = new CustomHRPVGClaim();
                    hRPVGClaimVM.HRPVGCID = mc.HRPVGCID;
                    hRPVGClaimVM.HRPVGCNo = mc.HRPVGCNo;
                    hRPVGClaimVM.Name = mc.Name;
                    hRPVGClaimVM.CreatedDate = DateTime.ParseExact(mc.CreatedDate, "MM/dd/yyyy HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture)
                                                             .ToString("dd/MM/yyyy", System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
                    hRPVGClaimVM.FacilityName = mc.FacilityName;
                    hRPVGClaimVM.Phone = mc.Phone;
                    hRPVGClaimVM.GrandTotal = mc.GrandTotal;
                    hRPVGClaimVM.ApprovalStatus = mc.ApprovalStatus;
                    hRPVGClaimVM.TotalAmount = mc.TotalAmount;
                    hRPVGClaimVM.Amount = mc.Amount;
                    hRPVGClaimVM.PayeeName = mc.PayeeName;
                    hRPVGClaimVM.PaymentMode = mc.PaymentMode;
                    hRPVGClaimVM.ParticularsOfPayment = mc.ParticularsOfPayment;
                    hRPVGClaimVM.VoucherNo = mc.VoucherNo;

                    if (mc.UserApprovers != "")
                    {
                        hRPVGClaimVM.Approver = mc.UserApprovers.Split(',').First();
                        if ((hRPVGClaimVM.Approver == HttpContext.User.FindFirst("userid").Value || (delegatedUserId.HasValue && hRPVGClaimVM.Approver == delegatedUserId.Value.ToString())) &&
                            (hRPVGClaimVM.ApprovalStatus == 6))
                        {
                            hRPVGClaimVM.IsActionAllowed = false;
                        }
                    }
                    else if (mc.HODApprover != "")
                    {
                        hRPVGClaimVM.Approver = mc.HODApprover.Split(',').First();
                        if ((hRPVGClaimVM.Approver == HttpContext.User.FindFirst("userid").Value || (delegatedUserId.HasValue && hRPVGClaimVM.Approver == delegatedUserId.Value.ToString())) &&
                            (hRPVGClaimVM.ApprovalStatus == 7))
                        {
                            hRPVGClaimVM.IsActionAllowed = false;
                        }
                    }
                    else if (mc.Verifier != "")
                    {
                        hRPVGClaimVM.Approver = mc.Verifier.Split(',').First();
                        if ((hRPVGClaimVM.Approver == HttpContext.User.FindFirst("userid").Value || (delegatedUserId.HasValue && hRPVGClaimVM.Approver == delegatedUserId.Value.ToString())) &&
                            (hRPVGClaimVM.ApprovalStatus == 1 || hRPVGClaimVM.ApprovalStatus == 2))
                        {
                            hRPVGClaimVM.IsActionAllowed = true;
                        }
                        //string VerifierIDs = string.Join(",", HRPVGverifierIDs.Skip(1));
                    }
                    else if (mc.Approver != "")
                    {
                        hRPVGClaimVM.Approver = mc.Approver.Split(',').First();
                        if ((hRPVGClaimVM.Approver == HttpContext.User.FindFirst("userid").Value || (delegatedUserId.HasValue && hRPVGClaimVM.Approver == delegatedUserId.Value.ToString())) &&
                            (hRPVGClaimVM.ApprovalStatus == 1 || hRPVGClaimVM.ApprovalStatus == 2))
                        {
                            hRPVGClaimVM.IsActionAllowed = true;
                        }
                    }
                    else
                    {
                        hRPVGClaimVM.Approver = "";
                    }

                    if (hRPVGClaimVM.Approver != "")
                    {
                        var alternateUser = await _alternateApproverHelper.IsAlternateApprovalSetForUser(Convert.ToInt32(hRPVGClaimVM.Approver));
                        if (alternateUser.HasValue)
                        {
                            var mstUserApprover = await _repository.MstUser.GetUserByIdAsync(alternateUser.Value);
                            hRPVGClaimVM.Approver = mstUserApprover.Name + " (AA)";
                        }
                        else
                        {
                            var mstUserApprover = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(hRPVGClaimVM.Approver));
                            hRPVGClaimVM.Approver = mstUserApprover.Name;
                        }
                    }

                    // Show actions based on alternate approver settings
                    // Override all the isActionAllowed code above. When alternate approval is set, then no need to show the action on any scenario
                    if (isAlternateApproverSet)
                    {
                        hRPVGClaimVM.IsActionAllowed = false;
                    }

                    hRPVGClaimVMs.Add(hRPVGClaimVM);
                }

                var mstHRPVCClaimVM = new HRPVGClaimSearchViewModel
                {
                    //Screens = new SelectList(await screenQuery.Distinct().ToListAsync()),
                    customHRPVGClaimVMs = hRPVGClaimVMs,
                    Statuses = new SelectList(status, "Value", "Text"),
                    Facilities = new SelectList(facilities, "Value", "Text"),
                    Users = new SelectList(users, "Value", "Text"),
                    FromDate = fromDate,
                    ToDate = toDate
                };

                return View(mstHRPVCClaimVM);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Something went wrong inside GetAllHRPVGClaimWithDetailsAsync action: {ex.Message}");
                return View();
            }
        }

        public async Task<IActionResult> Details(string userID, string facilityID, string statusId, string FromDate, string ToDate, long? id)
        {
            ViewData["filteruserId"] = userID;
            ViewData["filterfacilityID"] = facilityID;
            ViewData["filterstatusId"] = statusId;
            ViewData["filterFromDate"] = FromDate;
            ViewData["filterToDate"] = ToDate;
            if (id == null)
            {
                return NotFound();
            }
            long HRPVGCID = Convert.ToInt64(id);

            if (User != null && User.Identity.IsAuthenticated)
            {
                var mstHRPVGClaim = await _repository.MstHRPVGClaim.GetHRPVGClaimByIdAsync(id);

                if (mstHRPVGClaim == null)
                {
                    return NotFound();
                }

                SelectList facilities = new SelectList(await _repository.MstFacility.GetAllFacilityAsync("active"), "FacilityID", "FacilityName");
                ViewData["FacilityID"] = facilities;

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

                hRPVGClaimDetailVM.HRPVGClaimFileUploads = _repository.DtHRPVGClaimFileUpload.GetDtHRPVGClaimAuditByIdAsync(id).GetAwaiter().GetResult().ToList();

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

                if (TempData["ApprovedStatus"].ToString() == "1" || TempData["ApprovedStatus"].ToString() == "2" || TempData["ApprovedStatus"].ToString() == "3" || TempData["ApprovedStatus"].ToString() == "-5" || TempData["ApprovedStatus"].ToString() == "6" || TempData["ApprovedStatus"].ToString() == "7" || TempData["ApprovedStatus"].ToString() == "9" || TempData["ApprovedStatus"].ToString() == "10")
                {
                    ViewBag.ShowVoidBtn = 1;

                    if (User.IsInRole("Finance"))
                    {
                        if (int.Parse(TempData["ApprovedStatus"].ToString()) < 3 || int.Parse(TempData["ApprovedStatus"].ToString()) == 6 || int.Parse(TempData["ApprovedStatus"].ToString()) == 7)
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
                if (mstHRPVGClaim.Verifier != "")
                {
                    string[] verifierIDs = mstHRPVGClaim.Verifier.Split(',');
                    TempData["QueryMCVerifierIDs"] = string.Join(",", verifierIDs);
                    foreach (string verifierID in verifierIDs)
                    {
                        if ((verifierID != "" && verifierID == HttpContext.User.FindFirst("userid").Value || (delegatedUserId.HasValue && verifierID == delegatedUserId.Value.ToString())) && User.IsInRole("Finance"))
                        {
                            TempData["ApprovedStatus"] = mstHRPVGClaim.ApprovalStatus;
                            //Session["ApprovedStatus"] = oSupplierPO.Approvalstatus;
                            TempData["VerifierIDs"] = string.Join(",", verifierIDs.Skip(1));
                            hRPVGClaimVM.IsActionAllowed = true;
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
                    TempData["VerifierIDs"] = mstHRPVGClaim.Verifier;
                    TempData["ApproverIDs"] = mstHRPVGClaim.Approver;
                }

                //Approval Process code
                if (mstHRPVGClaim.Approver != "" && mstHRPVGClaim.Verifier == "")
                {
                    string[] approverIDs = mstHRPVGClaim.Approver.Split(',');
                    TempData["QueryMCApproverIDs"] = string.Join(",", approverIDs);
                    foreach (string approverID in approverIDs)
                    {
                        if ((approverID != "" && approverID == HttpContext.User.FindFirst("userid").Value || (delegatedUserId.HasValue && approverID == delegatedUserId.Value.ToString())) && User.IsInRole("Finance"))
                        {
                            TempData["ApprovedStatus"] = mstHRPVGClaim.ApprovalStatus;
                            //Session["ApprovedStatus"] = oSupplierPO.Approvalstatus;
                            TempData["ApproverIDs"] = string.Join(",", approverIDs.Skip(1));
                            hRPVGClaimVM.IsActionAllowed = true;
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

                string[] CreaterId = Creater.ToString().Split(',');
                string[] VerifiersId = Verifiers.ToString().Split(',');
                string[] ApproversId = Approvers.ToString().Split(',');

                UserIds.AddRange(CreaterId);
                UserIds.AddRange(VerifiersId);
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

        public async Task<JsonResult> GetTextValuesSG(string id)
        {
            List<DtHRPVGClaimVM> oDtClaimsList = new List<DtHRPVGClaimVM>();

            try
            {
                var dtHRPVGClaims = await _repository.DtHRPVGClaim.GetDtHRPVGClaimByIdAsync(Convert.ToInt64(id));

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
                    dtHRPVGClaimVM.Facility = item.Facility;
                    dtHRPVGClaimVM.AccountCode = item.AccountCode;
                    //dtHRPVGClaimVM.FacilityID = item.FacilityID;
                    oDtClaimsList.Add(dtHRPVGClaimVM);
                }
                return Json(new { DtClaimsList = oDtClaimsList });
            }
            catch
            {
                return Json(new { DtClaimsList = oDtClaimsList });
            }

        }

        public async Task<JsonResult> GetTextValuesSGSummary(string id)
        {
            List<DtHRPVGClaimSummary> oDtClaimsSummaryList = new List<DtHRPVGClaimSummary>();

            try
            {
                var dtHRPVGClaimSummaries = await _repository.DtHRPVGClaimSummary.GetDtHRPVGClaimSummaryByIdAsync(Convert.ToInt64(id));

                // List<clsDtSupplierPO> oclsDtSupplierPO = new List<clsDtSupplierPO>();
                //foreach (var item in dtHRPVGClaimSummaries)
                //{
                //    DtHRPVGClaimVM dtHRPVGClaimVM = new DtHRPVGClaimVM();

                //    dtHRPVGClaimVM.HRPVGCItemID = item.HRPVGCItemID;
                //    dtHRPVGClaimVM.HRPVGCID = item.HRPVGCID;
                //    dtHRPVGClaimVM.StaffName = item.StaffName;
                //    dtHRPVGClaimVM.Reason = item.Reason;
                //    dtHRPVGClaimVM.EmployeeNo = item.EmployeeNo;
                //    dtHRPVGClaimVM.ChequeNo = item.ChequeNo;
                //    dtHRPVGClaimVM.Amount = item.Amount;
                //    dtHRPVGClaimVM.GST = item.GST;
                //    dtHRPVGClaimVM.AmountWithGST = item.Amount + item.GST;
                //    dtHRPVGClaimVM.Facility = item.Facility;
                //    dtHRPVGClaimVM.AccountCode = item.AccountCode;
                //    //dtHRPVGClaimVM.FacilityID = item.FacilityID;
                //    oDtClaimsList.Add(dtHRPVGClaimVM);
                //}
                return Json(new { DtClaimsList = dtHRPVGClaimSummaries });
            }
            catch
            {
                return Json(new { DtClaimsList = oDtClaimsSummaryList });
            }

        }

        [HttpPost]
        public async Task<JsonResult> SaveSummary(string data)
        {
            var hRPVGClaimViewModel = JsonConvert.DeserializeObject<DtHRPVGClaimSummaryVM>(data);
            var hRPVGCSummary = await _repository.DtHRPVGClaimSummary.GetDtHRPVGClaimSummaryByIdAsync(hRPVGClaimViewModel.HRPVGCID);
            foreach (var hr in hRPVGCSummary)
            {
                _repository.DtHRPVGClaimSummary.Delete(hr);
            }
            //await _repository.SaveAsync();

            foreach (var dtItem in hRPVGClaimViewModel.dtClaims)
            {
                if (dtItem.ExpenseCategory != "DBS")
                {
                    dtItem.Description = dtItem.Description.ToUpper();
                    var mstExpenseCategory = await _repository.MstExpenseCategory.ExpenseCategoriesByClaimType("HR PV-Giro");
                    dtItem.AccountCode = mstExpenseCategory.ExpenseCode;
                }
            }

            //MstHRPVGClaimAudit mstHRPVGClaimAudit = new MstHRPVGClaimAudit();
            //mstHRPVGClaimAudit.Action = "1";
            //mstHRPVGClaimAudit.HRPVGCID = hRPVGClaimViewModel.HRPVGCID;
            //mstHRPVGClaimAudit.AuditDate = DateTime.Now;
            //mstHRPVGClaimAudit.AuditBy = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value);
            //mstHRPVGClaimAudit.SentTo = "";
            //mstHRPVGClaimAudit.Description = "Summary of Accounts Allocation Amended by " + User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.GivenName).Value + " on" + DateTime.Now;

            MstHRPVGClaimAudit auditUpdate = new MstHRPVGClaimAudit();
            auditUpdate.HRPVGCID = hRPVGClaimViewModel.HRPVGCID;
            auditUpdate.Action = "1";
            auditUpdate.AuditDate = DateTime.Now;
            auditUpdate.AuditBy = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value);
            //auditUpdate.InstanceID = 1;
            string time = DateTime.Now.ToString("tt", System.Globalization.CultureInfo.InvariantCulture);
            DateTime date = DateTime.Now;
            string formattedDate = date.ToString("dd'/'MM'/'yyyy hh:mm:ss");
            auditUpdate.Description = "Summary of Accounts Allocation Amended by " + User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.GivenName).Value.ToString() + " on " + formattedDate + " " + time + " ";
            auditUpdate.SentTo = "";
            //await _repository.MstHRPVGClaimAudit.CreateHRPVGClaimAudit(auditUpdate);
            //await _repository.SaveAsync();
            var res = await _repository.MstHRPVGClaim.SaveSummary(hRPVGClaimViewModel.HRPVGCID, hRPVGClaimViewModel.dtClaims, auditUpdate);

            //var mstFacility = await _repository.MstFacility.GetFacilityWithDepartmentByIdAsync(Convert.ToInt32(HttpContext.User.FindFirst("facilityid").Value));
            if (res == 0)
            {
                TempData["Message"] = "Summary of Accounts Allocation updated successfully";

                return Json(new { res });
            }
            else
                return Json(new { res });

            // return Json("success");
        }


        public async Task<JsonResult> UpdateStatusforVoid(string id, string reason, string approvedStatus)
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
                    await _repository.MstHRPVGClaim.UpdateMstHRPVGClaimStatus(HRPVGCID, -5, int.Parse(HttpContext.User.FindFirst("userid").Value), DateTime.Now, reason, string.Empty, string.Empty, string.Empty, string.Empty, isAlternateApprover, 0);
                }
                else
                {
                    await _repository.MstHRPVGClaim.UpdateMstHRPVGClaimStatus(HRPVGCID, 5, int.Parse(HttpContext.User.FindFirst("userid").Value), DateTime.Now, reason, string.Empty, string.Empty, string.Empty, string.Empty, isAlternateApprover, 0);
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

        public async Task<ActionResult> DeleteHRPVGClaimFile(string fileID, string filepath, string HRPVGCID)
        {
            DtHRPVGClaimFileUpload dtHRPVGClaimFileUpload = new DtHRPVGClaimFileUpload();
            if (CloudStorageAccount.TryParse(_configuration.GetSection("ConnectionStrings")["BlobConnectionString"], out CloudStorageAccount storageAccount))
            {
                CloudBlobClient BlobClient = storageAccount.CreateCloudBlobClient();
                CloudBlobContainer container = BlobClient.GetContainerReference(_configuration.GetSection("ConnectionStrings")["BlobContainerName"]);

                if (await container.ExistsAsync())
                {
                    CloudBlob file = container.GetBlobReference("FileUploads/HRPVGClaimFiles/" + filepath);

                    if (await file.ExistsAsync())
                    {
                        await file.DeleteIfExistsAsync();
                        dtHRPVGClaimFileUpload = await _repository.DtHRPVGClaimFileUpload.GetDtHRPVGClaimFileUploadByIdAsync(Convert.ToInt64(fileID));
                        _repository.DtHRPVGClaimFileUpload.DeleteDtHRPVGClaimFileUpload(dtHRPVGClaimFileUpload);
                        await _repository.SaveAsync();
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

            return RedirectToAction("Create", "FinanceHRPVGClaim", new
            {
                id = HRPVGCID,
                Updatestatus = "Edit"
            });
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
                        return File(blobStream, file.Properties.ContentType, "HRPVGClaims-Export.xlsx");
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

        public async Task<IActionResult> DownloadView(string id, string name)
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
                        ms.Seek(0, SeekOrigin.Begin); // Reset the stream position
                        // Set Content-Disposition header to inline, which prompts the browser to display the file
                        Response.Headers["Content-Disposition"] = $"inline; filename={name}";
                        return File(ms, file.Properties.ContentType);
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

        public async Task<JsonResult> ExporttoExcel(string data)
        {
            var mileageClaimSearch = JsonConvert.DeserializeObject<MileageClaimSearch>(data);

            var mstHRPVGClaimsWithDetails = await _repository.MstHRPVGClaim.GetAllHRPVGClaimWithDetailsByFacilityIDAsync(mileageClaimSearch.UserID, mileageClaimSearch.FacilityID, mileageClaimSearch.StatusID, mileageClaimSearch.FromDate, mileageClaimSearch.ToDate);

            List<CustomHRPVGClaim> hRPVGClaimVMs = new List<CustomHRPVGClaim>();

            DataTable dt = new DataTable("Grid");
            dt.Columns.AddRange(new DataColumn[12] { new DataColumn("Claim"),
                                        new DataColumn("Requester"),
                                        new DataColumn("Date Created"),
                                        new DataColumn("Payment Mode"),
                                         new DataColumn("Particulars of Payment"),
                                        new DataColumn("Facility"),
                                        new DataColumn("Contact Number"),
                                        new DataColumn("Payee Name"),
                                        new DataColumn("Amount"),
                                        new DataColumn("Total Claim"),
                                        new DataColumn("Approver"),
                                        new DataColumn("Status")});





            foreach (var mc in mstHRPVGClaimsWithDetails)
            {
                CustomHRPVGClaim hRPVGClaimVM = new CustomHRPVGClaim();
                hRPVGClaimVM.ApprovalStatus = mc.ApprovalStatus;

                if (mc.ApprovalStatus == 1)
                {
                    hRPVGClaimVM.ExpenseStatusName = "Awaiting Verification";

                }
                else if (mc.ApprovalStatus == 2)
                {
                    hRPVGClaimVM.ExpenseStatusName = "Awaiting Signatory approval";

                }
                else if (mc.ApprovalStatus == 3)
                {
                    hRPVGClaimVM.ExpenseStatusName = "Approved";

                }
                else if (mc.ApprovalStatus == 4)
                {
                    hRPVGClaimVM.ExpenseStatusName = "Request to Amend";
                }
                else if (mc.ApprovalStatus == 5)
                {
                    hRPVGClaimVM.ExpenseStatusName = "Voided";

                }
                else if (mc.ApprovalStatus == -5)
                {
                    hRPVGClaimVM.ExpenseStatusName = "Requested to Void";

                }
                else if (mc.ApprovalStatus == 6)
                {
                    hRPVGClaimVM.ExpenseStatusName = "Awaiting approval";

                }
                else if (mc.ApprovalStatus == 7)
                {
                    hRPVGClaimVM.ExpenseStatusName = "Awaiting HOD approval";

                }
                else if (mc.ApprovalStatus == 9)
                {
                    hRPVGClaimVM.ExpenseStatusName = "Exported to AccPac";

                }
                else if (mc.ApprovalStatus == 10)
                {
                    hRPVGClaimVM.ExpenseStatusName = "Exported to Bank";

                }
                else
                {
                    hRPVGClaimVM.ExpenseStatusName = "New";
                }


                if (mc.UserApprovers != "")
                {
                    hRPVGClaimVM.Approver = mc.UserApprovers.Split(',').First();
                    if (hRPVGClaimVM.Approver == HttpContext.User.FindFirst("userid").Value && (hRPVGClaimVM.ApprovalStatus == 6))
                    {
                        hRPVGClaimVM.IsActionAllowed = true;
                    }
                }
                else if (mc.HODApprover != "")
                {
                    hRPVGClaimVM.Approver = mc.HODApprover.Split(',').First();
                    if (hRPVGClaimVM.Approver == HttpContext.User.FindFirst("userid").Value && (hRPVGClaimVM.ApprovalStatus == 7))
                    {
                        hRPVGClaimVM.IsActionAllowed = true;
                    }
                }
                else if (mc.Verifier != "")
                {
                    hRPVGClaimVM.Approver = mc.Verifier.Split(',').First();
                    if (hRPVGClaimVM.Approver == HttpContext.User.FindFirst("userid").Value && (hRPVGClaimVM.ApprovalStatus == 1 || hRPVGClaimVM.ApprovalStatus == 2))
                    {
                        hRPVGClaimVM.IsActionAllowed = true;
                    }
                    //string VerifierIDs = string.Join(",", HRPVGverifierIDs.Skip(1));
                }
                else if (mc.Approver != "")
                {
                    hRPVGClaimVM.Approver = mc.Approver.Split(',').First();
                    if (hRPVGClaimVM.Approver == HttpContext.User.FindFirst("userid").Value && (hRPVGClaimVM.ApprovalStatus == 1 || hRPVGClaimVM.ApprovalStatus == 2))
                    {
                        hRPVGClaimVM.IsActionAllowed = true;
                    }
                }
                else
                {
                    hRPVGClaimVM.Approver = "";
                }

                if (hRPVGClaimVM.Approver != "")
                {
                    var mstUserApprover = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(hRPVGClaimVM.Approver));
                    if (hRPVGClaimVM.ApprovalStatus != 3 && hRPVGClaimVM.ApprovalStatus != 4 && hRPVGClaimVM.ApprovalStatus != -5 && hRPVGClaimVM.ApprovalStatus != 5)
                        hRPVGClaimVM.Approver = mstUserApprover.Name;
                    else
                        hRPVGClaimVM.Approver = "";
                }


                dt.Rows.Add(hRPVGClaimVM.HRPVGCNo = mc.HRPVGCNo,
                            hRPVGClaimVM.Name = mc.Name,
                            hRPVGClaimVM.CreatedDate = Convert.ToDateTime(mc.CreatedDate).ToString("dd/MM/yyyy", System.Globalization.CultureInfo.CreateSpecificCulture("en-US")),
                            hRPVGClaimVM.PaymentMode = mc.PaymentMode,
                            hRPVGClaimVM.ParticularsOfPayment = mc.ParticularsOfPayment,
                            hRPVGClaimVM.FacilityName = mc.FacilityName,
                            hRPVGClaimVM.Phone = mc.Phone,
                            hRPVGClaimVM.PayeeName = mc.PayeeName,
                            hRPVGClaimVM.Amount = mc.Amount,
                            hRPVGClaimVM.TotalAmount = mc.TotalAmount,
                            hRPVGClaimVM.Approver = hRPVGClaimVM.Approver,
                            hRPVGClaimVM.ExpenseStatusName = hRPVGClaimVM.ExpenseStatusName);
            }

            string filename = "HRPVGClaims-Export" + DateTime.Now.ToString("ddMMyyyyss") + ".xlsx";
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

        public async Task<IActionResult> GetPrintClaimDetails(long? id)
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
                hRPVGClaimDetailVM.HRPVGClaimVM = hRPVGClaimVM;
                //mileageClaimDetailVM.DtMileageClaimVMs = dtMileageClaimVMs;
            }
            return PartialView("GetHRPVGDetailsPrint", hRPVGClaimDetailVM);
        }
        public async Task<IActionResult> GetPrint(string data)
        {
            var mileageClaimSearch = JsonConvert.DeserializeObject<MileageClaimSearch>(data);
            var mstHRPVGClaimsWithDetails = await _repository.MstHRPVGClaim.GetAllHRPVGClaimWithDetailsByFacilityIDAsync(mileageClaimSearch.UserID, mileageClaimSearch.FacilityID, mileageClaimSearch.StatusID, mileageClaimSearch.FromDate, mileageClaimSearch.ToDate);
            List<CustomHRPVGClaim> hRPVGClaimVMs = new List<CustomHRPVGClaim>();


            foreach (var mc in mstHRPVGClaimsWithDetails)
            {
                CustomHRPVGClaim hRPVGClaimVM = new CustomHRPVGClaim();

                hRPVGClaimVM.HRPVGCNo = mc.HRPVGCNo;
                hRPVGClaimVM.Name = mc.Name;
                hRPVGClaimVM.CreatedDate = Convert.ToDateTime(mc.CreatedDate).ToString("dd/MM/yyyy", System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
                hRPVGClaimVM.FacilityName = mc.FacilityName;
                hRPVGClaimVM.PaymentMode = mc.PaymentMode;
                hRPVGClaimVM.Phone = mc.Phone;
                hRPVGClaimVM.TotalAmount = mc.TotalAmount;
                hRPVGClaimVM.ApprovalStatus = mc.ApprovalStatus;
                hRPVGClaimVM.PayeeName = mc.PayeeName;
                hRPVGClaimVM.Amount = mc.Amount;
                hRPVGClaimVM.ParticularsOfPayment = mc.ParticularsOfPayment;

                if (mc.ApprovalStatus == 1)
                {
                    hRPVGClaimVM.ExpenseStatusName = "Awaiting Verification";

                }
                else if (mc.ApprovalStatus == 2)
                {
                    hRPVGClaimVM.ExpenseStatusName = "Awaiting Signatory approval";

                }
                else if (mc.ApprovalStatus == 3)
                {
                    hRPVGClaimVM.ExpenseStatusName = "Approved";

                }
                else if (mc.ApprovalStatus == 4)
                {
                    hRPVGClaimVM.ExpenseStatusName = "Request to Amend";
                }
                else if (mc.ApprovalStatus == 5)
                {
                    hRPVGClaimVM.ExpenseStatusName = "Voided";

                }
                else if (mc.ApprovalStatus == -5)
                {
                    hRPVGClaimVM.ExpenseStatusName = "Requested to Void";

                }
                else if (mc.ApprovalStatus == 6)
                {
                    hRPVGClaimVM.ExpenseStatusName = "Awaiting approval";

                }
                else if (mc.ApprovalStatus == 7)
                {
                    hRPVGClaimVM.ExpenseStatusName = "Awaiting HOD approval";

                }
                else if (mc.ApprovalStatus == 9)
                {
                    hRPVGClaimVM.ExpenseStatusName = "Exported to AccPac";

                }
                else if (mc.ApprovalStatus == 10)
                {
                    hRPVGClaimVM.ExpenseStatusName = "Exported to Bank";

                }
                else
                {
                    hRPVGClaimVM.ExpenseStatusName = "New";
                }


                if (mc.UserApprovers != "")
                {
                    hRPVGClaimVM.Approver = mc.UserApprovers.Split(',').First();
                    if (hRPVGClaimVM.Approver == HttpContext.User.FindFirst("userid").Value && (hRPVGClaimVM.ApprovalStatus == 6))
                    {
                        hRPVGClaimVM.IsActionAllowed = true;
                    }
                }
                else if (mc.HODApprover != "")
                {
                    hRPVGClaimVM.Approver = mc.HODApprover.Split(',').First();
                    if (hRPVGClaimVM.Approver == HttpContext.User.FindFirst("userid").Value && (hRPVGClaimVM.ApprovalStatus == 7))
                    {
                        hRPVGClaimVM.IsActionAllowed = true;
                    }
                }
                else if (mc.Verifier != "")
                {
                    hRPVGClaimVM.Approver = mc.Verifier.Split(',').First();
                    if (hRPVGClaimVM.Approver == HttpContext.User.FindFirst("userid").Value && (hRPVGClaimVM.ApprovalStatus == 1 || hRPVGClaimVM.ApprovalStatus == 2))
                    {
                        hRPVGClaimVM.IsActionAllowed = true;
                    }
                    //string VerifierIDs = string.Join(",", HRPVGverifierIDs.Skip(1));
                }
                else if (mc.Approver != "")
                {
                    hRPVGClaimVM.Approver = mc.Approver.Split(',').First();
                    if (hRPVGClaimVM.Approver == HttpContext.User.FindFirst("userid").Value && (hRPVGClaimVM.ApprovalStatus == 1 || hRPVGClaimVM.ApprovalStatus == 2))
                    {
                        hRPVGClaimVM.IsActionAllowed = true;
                    }
                }
                else
                {
                    hRPVGClaimVM.Approver = "";
                }

                if (hRPVGClaimVM.Approver != "")
                {
                    var mstUserApprover = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(hRPVGClaimVM.Approver));
                    hRPVGClaimVM.Approver = mstUserApprover.Name;
                }
                hRPVGClaimVMs.Add(hRPVGClaimVM);
            }
            return PartialView("GetHRPVGPrint", hRPVGClaimVMs);
        }

        public async Task<JsonResult> UpdateStatus(string id)
        {
            if (User != null && User.Identity.IsAuthenticated)
            {
                int HRPVGCID = Convert.ToInt32(id);

                var mstHRPVGClaim = await _repository.MstHRPVGClaim.GetHRPVGClaimByIdAsync(HRPVGCID);

                if (mstHRPVGClaim == null)
                {
                    // return NotFound();
                }

                bool isAlternateApprover = false;
                int ApprovedStatus = Convert.ToInt32(mstHRPVGClaim.ApprovalStatus);
                bool excute = _repository.MstHRPVGClaim.ExistsApproval(HRPVGCID.ToString(), ApprovedStatus, HttpContext.User.FindFirst("userid").Value, "HRPVG");

                // If execute is false, Check if the current user is alternate user for this claim
                if (excute == false)
                {
                    string hodapprover = _repository.MstHRPVGClaim.GetApproval(HRPVGCID.ToString(), ApprovedStatus, HttpContext.User.FindFirst("userid").Value, "HRPVG");
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

                            //Mail Code Implementation for Verifiers
                            foreach (string verifierID in verifierIDs)
                            {
                                if (verifierID != "")
                                {
                                    string domainUrl = HttpContext.Request.Scheme + "://" + HttpContext.Request.Host;
                                    string clickUrl = domainUrl + "/" + "FinanceHRPVGClaim/Details/" + HRPVGCID;

                                    var mstSenderDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(HttpContext.User.FindFirst("userid").Value));
                                    var senderName = mstSenderDetails.Name;
                                    var mstVerifierDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(verifierID));
                                    var toEmail = mstVerifierDetails.EmailAddress;
                                    var receiverName = mstVerifierDetails.Name;
                                    var claimNo = mstHRPVGClaim.HRPVGCNo;
                                    var screen = "HR PV-GIRO Claim";
                                    var approvalType = "Verification Request";
                                    int userID = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value);
                                    var subject = "HR PV-GIRO Claim for Verification " + claimNo;

                                    BackgroundJob.Enqueue(() => _sendMailServices.SendEmail("EmailTemplate.html", screen, subject, senderName, receiverName, claimNo, approvalType, userID, toEmail, clickUrl));
                                }
                                else
                                {
                                    string domainUrl = HttpContext.Request.Scheme + "://" + HttpContext.Request.Host;
                                    string clickUrl = domainUrl + "/" + "FinanceHRPVGClaim/Details/" + HRPVGCID;

                                    var mstSenderDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(HttpContext.User.FindFirst("userid").Value));
                                    var senderName = mstSenderDetails.Name;
                                    var mstVerifierDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(ApproverIDs.ToString().Split(',')[0].ToString()));
                                    var toEmail = mstVerifierDetails.EmailAddress;
                                    var receiverName = mstVerifierDetails.Name;
                                    var claimNo = mstHRPVGClaim.HRPVGCNo;
                                    var screen = "HR PV-GIRO Claim";
                                    var approvalType = "Approval Request";
                                    int userID = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value);
                                    var subject = "HR PV-GIRO Claim for Approval " + claimNo;

                                    BackgroundJob.Enqueue(() => _sendMailServices.SendEmail("EmailTemplate.html", screen, subject, senderName, receiverName, claimNo, approvalType, userID, toEmail, clickUrl));
                                }
                                break;
                            }
                        }
                        catch
                        {
                        }
                        string financeStartDay = _configuration.GetValue<string>("FinanceStartDay");
                        //ViewBag.PettyCashLimit = pettyCashLimit;
                        await _repository.MstHRPVGClaim.UpdateMstHRPVGClaimStatus(HRPVGCID, 2, int.Parse(HttpContext.User.FindFirst("userid").Value), DateTime.Now, string.Empty, VerifierIDs.ToString(), ApproverIDs.ToString(), UserApproverIDs.ToString(), HODApproverID.ToString(), isAlternateApprover, int.Parse(financeStartDay));

                    }
                    #endregion

                    #region HRPVG Approver
                    else if (ApprovedStatus == 2)
                    {
                        string VerifierIDs = "";
                        string ApproverIDs = "";
                        string UserApproverIDs = "";
                        string HODApproverID = "";
                        string DVerifierIDs = "";
                        try
                        {
                            string[] HRPVGapproverIDs = mstHRPVGClaim.Approver.Split(',');
                            ApproverIDs = string.Join(",", HRPVGapproverIDs.Skip(1));
                            string[] approverIDs = ApproverIDs.Split(',');
                            int CreatedBy = Convert.ToInt32(mstHRPVGClaim.CreatedBy);
                            DVerifierIDs = mstHRPVGClaim.DVerifier.Split(',').First();

                            //Mail Code Implementation for Approvers
                            foreach (string approverID in approverIDs)
                            {
                                if (approverID != "")
                                {
                                    string domainUrl = HttpContext.Request.Scheme + "://" + HttpContext.Request.Host;
                                    string clickUrl = domainUrl + "/" + "FinanceHRPVGClaim/Details/" + HRPVGCID;

                                    var mstSenderDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(HttpContext.User.FindFirst("userid").Value));
                                    var senderName = mstSenderDetails.Name;
                                    var mstVerifierDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(approverID));
                                    var toEmail = mstVerifierDetails.EmailAddress;
                                    var receiverName = mstVerifierDetails.Name;
                                    var claimNo = mstHRPVGClaim.HRPVGCNo;
                                    var screen = "HR PV-GIRO Claim";
                                    var approvalType = "Approval Request";
                                    int userID = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value);
                                    var subject = "HR PV-GIRO Claim for Approval " + claimNo;

                                    BackgroundJob.Enqueue(() => _sendMailServices.SendEmail("EmailTemplate.html", screen, subject, senderName, receiverName, claimNo, approvalType, userID, toEmail, clickUrl));

                                }

                                break;
                            }
                        }
                        catch
                        {
                        }
                        string financeStartDay = _configuration.GetValue<string>("FinanceStartDay");
                        await _repository.MstHRPVGClaim.UpdateMstHRPVGClaimStatus(HRPVGCID, 3, int.Parse(HttpContext.User.FindFirst("userid").Value), DateTime.Now, string.Empty, VerifierIDs, ApproverIDs, UserApproverIDs.ToString(), HODApproverID.ToString(), isAlternateApprover, int.Parse(financeStartDay));
                        if (ApproverIDs == string.Empty)
                        {
                            string domainUrl = HttpContext.Request.Scheme + "://" + HttpContext.Request.Host;
                            string clickUrl = domainUrl + "/" + "FinanceReports";

                            var mstSenderDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(HttpContext.User.FindFirst("userid").Value));
                            var senderName = mstSenderDetails.Name;
                            var mstVerifierDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(DVerifierIDs));
                            var toEmail = mstVerifierDetails.EmailAddress;
                            var receiverName = mstVerifierDetails.Name;
                            var claimNo = mstHRPVGClaim.HRPVGCNo;
                            var screen = "HR PV-GIRO Claim";
                            var approvalType = "Export to AccPac/Bank Request";
                            int userID = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value);
                            var subject = "HR PV-GIRO Claim for Export to AccPac/Bank " + claimNo;

                            BackgroundJob.Enqueue(() => _sendMailServices.SendEmail("ExportToBankTemplate.html", screen, subject, senderName, receiverName, claimNo, approvalType, userID, toEmail, clickUrl));
                        }
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
                return Json(new { res = "Done" });
            }

        }

        public async Task<JsonResult> UpdateRejectedStatus(string id, string reason)
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

                await _repository.MstHRPVGClaim.UpdateMstHRPVGClaimStatus(HRPVGCID, 4, int.Parse(HttpContext.User.FindFirst("userid").Value), DateTime.Now, reason, string.Empty, string.Empty, string.Empty, string.Empty, isAlternateApprover, 0);
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

        public async Task<IActionResult> Download(string id, string name)
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



        #region -- SendMessage --

        // public ActionResult SendMessage(FormCollection data)
        public async Task<JsonResult> AddMessage(string data)
        {
            var queryParamViewModel = JsonConvert.DeserializeObject<QueryParam>(data);

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
                    _logger.LogError($"Something went wrong inside CreatePVGClaimAudit action: {ex.Message}");
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
        #endregion SendMessage

        #region -- GetMessages --

        public async Task<JsonResult> GetMessages(string id)
        {
            try
            {
                var result = new LinkedList<object>();

                //   var spoid = Convert.ToInt64(Session["id"]);
                var ecid = Convert.ToInt32(id);
                int UserId = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value);
                ViewBag.userID = UserId;
                //var queries1 = _context.mstQuery.ToList().Where(j => j.ID == smcid && (j.SenderID == UserId || j.ReceiverID == UserId) && j.ModuleType.ToString().Trim() == "HRPVG Claim").OrderBy(j => j.SentTime);
                var queries = await _repository.MstQuery.GetAllClaimsQueriesAsync(UserId, ecid, "HRPVG Claim");
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

        #endregion GetMessages
    }
}
