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
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace EClaimsWeb.Controllers
{
    [Authorize(Roles = "Admin,Finance")]
    public class FinanceExpenseClaimController : Controller
    {
        private ILoggerManager _logger;
        private IRepositoryWrapper _repository;
        private IMapper _mapper;
        private AlternateApproverHelper _alternateApproverHelper;
        private IConfiguration _configuration;
        private readonly RepositoryContext _context;
        private ISendMailServices _sendMailServices;

        public FinanceExpenseClaimController(ILoggerManager logger, IRepositoryWrapper repository, IMapper mapper, RepositoryContext context, IConfiguration configuration, ISendMailServices sendMailServices)
        {
            _logger = logger;
            _repository = repository;
            _mapper = mapper;
            _context = context;
            _configuration = configuration;
            _sendMailServices = sendMailServices;
            _alternateApproverHelper = new AlternateApproverHelper(logger, repository, context);
        }

        public async Task<IActionResult> Index(string expenseID, int userID, int facilityID, int statusID, string fromDate, string toDate)
        {
            try
            {
                if (string.IsNullOrEmpty(expenseID))
                    expenseID = "";
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

                List<clsModule> oclsExpenseType = new List<clsModule>();
                //oclsModule.Add(new clsModule() { ModuleName = "Admin Settings", ModuleId = "Admin Settings" });
                oclsExpenseType.Add(new clsModule() { ModuleName = "Expense", ModuleId = "Expense" });
                oclsExpenseType.Add(new clsModule() { ModuleName = "Petty Cash", ModuleId = "Petty Cash" });


                List<SelectListItem> expenses = (from t in oclsExpenseType
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

                var mstExpenseClaimsWithDetails = await _repository.MstExpenseClaim.GetAllExpenseClaimWithDetailsAsync(expenseID, userID, facilityID, statusID, fromDate, toDate);
                if (mstExpenseClaimsWithDetails != null && mstExpenseClaimsWithDetails.Any())
                {
                    mstExpenseClaimsWithDetails.ToList().ForEach(c => c.IsDelegated = false);
                }

                if (delegatedUserId != null && delegatedUserId.HasValue)
                {
                    var delegatedClaims = await _repository.MstExpenseClaim.GetAllExpenseClaimWithDetailsAsync(expenseID, delegatedUserId.Value, facilityID, statusID, fromDate, toDate);
                    if (delegatedClaims != null && delegatedClaims.Any())
                    {
                        delegatedClaims.ToList().ForEach(c => c.IsDelegated = true);
                        mstExpenseClaimsWithDetails.ToList().AddRange(delegatedClaims.ToList());
                    }
                }

                _logger.LogInfo($"Returned all Expense Claims with details from database.");
                List<ExpenseClaimVM> expenseClaimVMs = new List<ExpenseClaimVM>();
                foreach (var mc in mstExpenseClaimsWithDetails)
                {
                    ExpenseClaimVM expenseClaimVM = new ExpenseClaimVM();
                    expenseClaimVM.ECID = mc.CID;
                    expenseClaimVM.ECNo = mc.CNO;
                    expenseClaimVM.Name = mc.Name;
                    expenseClaimVM.CreatedDate = DateTime.ParseExact(mc.CreatedDate, "MM/dd/yyyy HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture)
                                                      .ToString("dd/MM/yyyy", System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
                    expenseClaimVM.FacilityName = mc.FacilityName;
                    expenseClaimVM.Phone = mc.Phone;
                    expenseClaimVM.GrandTotal = mc.GrandTotal;
                    expenseClaimVM.ApprovalStatus = mc.ApprovalStatus;
                    expenseClaimVM.ClaimType = mc.ClaimType;
                    expenseClaimVM.TotalAmount = mc.TotalAmount;
                    expenseClaimVM.VoucherNo = mc.VoucherNo;
                    var mstDtExpenseClaim = await _repository.DtExpenseClaim.GetTopDtExpenseClaimByIdAsync(mc.CID);
                    if (mstDtExpenseClaim != null)
                        expenseClaimVM.Description = mstDtExpenseClaim.Description;
                    else
                        expenseClaimVM.Description = "";

                    if (mc.UserApprovers != "")
                    {
                        expenseClaimVM.Approver = mc.UserApprovers.Split(',').First();
                        if ((expenseClaimVM.Approver == HttpContext.User.FindFirst("userid").Value || (delegatedUserId.HasValue && expenseClaimVM.Approver == delegatedUserId.Value.ToString())) &&
                            (expenseClaimVM.ApprovalStatus == 6))
                        {
                            expenseClaimVM.IsActionAllowed = false;
                        }
                    }
                    else if (mc.Verifier != "")
                    {
                        expenseClaimVM.Approver = mc.Verifier.Split(',').First();
                        if ((expenseClaimVM.Approver == HttpContext.User.FindFirst("userid").Value || (delegatedUserId.HasValue && expenseClaimVM.Approver == delegatedUserId.Value.ToString())) &&
                            (expenseClaimVM.ApprovalStatus == 1 || expenseClaimVM.ApprovalStatus == 2))
                        {
                            expenseClaimVM.IsActionAllowed = true;
                        }
                        //string VerifierIDs = string.Join(",", ExpenseverifierIDs.Skip(1));
                    }
                    else if (mc.HODApprover != "")
                    {
                        expenseClaimVM.Approver = mc.HODApprover.Split(',').First();
                        if ((expenseClaimVM.Approver == HttpContext.User.FindFirst("userid").Value || (delegatedUserId.HasValue && expenseClaimVM.Approver == delegatedUserId.Value.ToString())) &&
                            (expenseClaimVM.ApprovalStatus == 7))
                        {
                            expenseClaimVM.IsActionAllowed = false;
                        }
                    }
                    else if (mc.Approver != "")
                    {
                        expenseClaimVM.Approver = mc.Approver.Split(',').First();
                        if ((expenseClaimVM.Approver == HttpContext.User.FindFirst("userid").Value || (delegatedUserId.HasValue && expenseClaimVM.Approver == delegatedUserId.Value.ToString())) &&
                            (expenseClaimVM.ApprovalStatus == 1 || expenseClaimVM.ApprovalStatus == 2))
                        {
                            expenseClaimVM.IsActionAllowed = true;
                        }
                    }
                    else
                    {
                        expenseClaimVM.Approver = "";
                    }

                    if (expenseClaimVM.Approver != "")
                    {
                        var alternateUser = await _alternateApproverHelper.IsAlternateApprovalSetForUser(Convert.ToInt32(expenseClaimVM.Approver));
                        if (alternateUser.HasValue)
                        {
                            var mstUserApprover = await _repository.MstUser.GetUserByIdAsync(alternateUser.Value);
                            expenseClaimVM.Approver = mstUserApprover.Name + " (AA)";
                        }
                        else
                        {
                            var mstUserApprover = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(expenseClaimVM.Approver));
                            expenseClaimVM.Approver = mstUserApprover.Name;
                        }
                    }

                    // Show actions based on alternate approver settings
                    // Override all the isActionAllowed code above. When alternate approval is set, then no need to show the action on any scenario
                    if (isAlternateApproverSet)
                    {
                        expenseClaimVM.IsActionAllowed = false;
                    }

                    expenseClaimVMs.Add(expenseClaimVM);
                }

                var mstExpenseClaimVM = new ExpenseClaimSearchViewModel
                {
                    //Screens = new SelectList(await screenQuery.Distinct().ToListAsync()),
                    expenseClaimVMs = expenseClaimVMs,
                    Statuses = new SelectList(status, "Value", "Text"),
                    Facilities = new SelectList(facilities, "Value", "Text"),
                    Users = new SelectList(users, "Value", "Text"),
                    ExpenseTypes = new SelectList(expenses, "Value", "Text"),
                    FromDate = fromDate,
                    ToDate = toDate
                };

                return View(mstExpenseClaimVM);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Something went wrong inside GetAllExpenseClaimWithDetailsAsync action: {ex.Message}");
                return View();
            }
        }

        public async Task<JsonResult> GetTextValuesSG(string id)
        {
            List<DtExpenseClaimVM> oDtClaimsList = new List<DtExpenseClaimVM>();

            try
            {
                var dtExpenseClaims = await _repository.DtExpenseClaim.GetDtExpenseClaimByIdAsync(Convert.ToInt64(id));

                // List<clsDtSupplierPO> oclsDtSupplierPO = new List<clsDtSupplierPO>();
                foreach (var item in dtExpenseClaims)
                {
                    DtExpenseClaimVM dtExpenseClaimVM = new DtExpenseClaimVM();

                    dtExpenseClaimVM.ECItemID = item.ECItemID;
                    dtExpenseClaimVM.ECID = item.ECID;
                    dtExpenseClaimVM.DateOfJourney = item.Date;
                    dtExpenseClaimVM.FacilityID = item.FacilityID;
                    dtExpenseClaimVM.Description = item.Description;
                    dtExpenseClaimVM.Amount = item.Amount;
                    dtExpenseClaimVM.Gst = item.GST;
                    dtExpenseClaimVM.AmountWithGST = item.Amount + item.GST;
                    dtExpenseClaimVM.ExpenseCategoryID = item.ExpenseCategoryID;
                    dtExpenseClaimVM.AccountCode = item.AccountCode;
                    oDtClaimsList.Add(dtExpenseClaimVM);
                }
                return Json(new { DtClaimsList = oDtClaimsList });
            }
            catch
            {
                return Json(new { DtClaimsList = oDtClaimsList });
            }

        }

        public async Task<JsonResult> GetTextValuesSGDraft(string id)
        {
            List<DtExpenseClaimVM> oDtClaimsList = new List<DtExpenseClaimVM>();

            try
            {
                var dtExpenseClaims = await _repository.DtExpenseClaimDraft.GetDtExpenseClaimDraftByIdAsync(Convert.ToInt64(id));

                // List<clsDtSupplierPO> oclsDtSupplierPO = new List<clsDtSupplierPO>();
                foreach (var item in dtExpenseClaims)
                {
                    DtExpenseClaimVM dtExpenseClaimVM = new DtExpenseClaimVM();

                    dtExpenseClaimVM.ECItemID = item.ECItemID;
                    dtExpenseClaimVM.ECID = item.ECID;
                    dtExpenseClaimVM.DateOfJourney = item.Date;
                    dtExpenseClaimVM.FacilityID = item.FacilityID;
                    dtExpenseClaimVM.Description = item.Description;
                    dtExpenseClaimVM.Amount = item.Amount;
                    dtExpenseClaimVM.Gst = item.GST;
                    dtExpenseClaimVM.AmountWithGST = item.Amount + item.GST;
                    dtExpenseClaimVM.ExpenseCategoryID = item.ExpenseCategoryID;
                    dtExpenseClaimVM.AccountCode = item.AccountCode;
                    oDtClaimsList.Add(dtExpenseClaimVM);
                }
                return Json(new { DtClaimsList = oDtClaimsList });
            }
            catch
            {
                return Json(new { DtClaimsList = oDtClaimsList });
            }

        }

        public async Task<IActionResult> Details(string expenseID, string userID, string facilityID, string statusId, string FromDate, string ToDate, long? id)
        {
            ViewData["filterexpenseID"] = expenseID;
            ViewData["filteruserId"] = userID;
            ViewData["filterfacilityID"] = facilityID;
            ViewData["filterstatusId"] = statusId;
            ViewData["filterFromDate"] = FromDate;
            ViewData["filterToDate"] = ToDate;
            if (id == null)
            {
                return NotFound();
            }
            long ECID = Convert.ToInt64(id);

            if (User != null && User.Identity.IsAuthenticated)
            {
                var mstExpenseClaim = await _repository.MstExpenseClaim.GetExpenseClaimByIdAsync(id);

                if (mstExpenseClaim == null)
                {
                    return NotFound();
                }

                ViewData["ExpenseCategoryID"] = new SelectList(await _repository.MstExpenseCategory.GetAllExpenseCategoriesByClaimTypesAsync("expense/pv-cheque/pv-giro", "active"), "ExpenseCategoryID", "Description");
                var mstUsersWithDetails = await _repository.MstUser.GetUserWithDetailsByIdAsync(Convert.ToInt32(HttpContext.User.FindFirst("userid").Value));
                SelectList facilities = new SelectList(await _repository.MstFacility.GetAllFacilityAsync("active"), "FacilityID", "FacilityName");
                ViewData["FacilityID"] = facilities;
                string financeGstValueBuffer = _configuration.GetValue<string>("FinanceGstValueBuffer");
                ViewBag.FinanceGstValueBuffer = financeGstValueBuffer;
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
                    dtExpenseClaimVM.GSTPercentage = item.GSTPercentage;
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

                expenseClaimDetailVM.ExpenseClaimFileUploads = _repository.DtExpenseClaimFileUpload.GetDtExpenseClaimAuditByIdAsync(id).GetAwaiter().GetResult().ToList();

                ExpenseClaimVM expenseClaimVM = new ExpenseClaimVM();
                expenseClaimVM.VoucherNo = mstExpenseClaim.VoucherNo;
                expenseClaimVM.ClaimType = mstExpenseClaim.ClaimType;
                expenseClaimVM.GrandTotal = mstExpenseClaim.GrandTotal;
                expenseClaimVM.TotalAmount = mstExpenseClaim.TotalAmount;
                expenseClaimVM.GrandGST = mstExpenseClaim.TotalAmount - expenseClaimVM.GrandTotal;
                expenseClaimVM.Company = mstExpenseClaim.Company;
                expenseClaimVM.Name = mstExpenseClaim.MstUser.Name;
                expenseClaimVM.DepartmentName = mstExpenseClaim.MstDepartment.Department;
                expenseClaimVM.FacilityName = mstExpenseClaim.MstFacility.FacilityName;
                expenseClaimVM.CreatedDate = Convert.ToDateTime(mstExpenseClaim.CreatedDate).ToString("dd/MM/yyyy", System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
                expenseClaimVM.Verifier = mstExpenseClaim.Verifier;
                expenseClaimVM.Approver = mstExpenseClaim.Approver;
                expenseClaimVM.ECNo = mstExpenseClaim.ECNo;
                ViewBag.ECID = id;
                TempData["CreatedBy"] = mstExpenseClaim.CreatedBy;
                ViewBag.Approvalstatus = mstExpenseClaim.ApprovalStatus;


                TempData["ApprovedStatus"] = mstExpenseClaim.ApprovalStatus;
                TempData["FinalApproverID"] = mstExpenseClaim.FinalApprover;
                ViewBag.VoidReason = mstExpenseClaim.VoidReason == null ? "" : mstExpenseClaim.VoidReason;

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
                if (mstExpenseClaim.Verifier != "")
                {
                    string[] verifierIDs = mstExpenseClaim.Verifier.Split(',');
                    TempData["QueryMCVerifierIDs"] = string.Join(",", verifierIDs);
                    foreach (string verifierID in verifierIDs)
                    {
                        if ((verifierID != "" && verifierID == HttpContext.User.FindFirst("userid").Value || (delegatedUserId.HasValue && verifierID == delegatedUserId.Value.ToString())) && User.IsInRole("Finance"))
                        {
                            TempData["ApprovedStatus"] = mstExpenseClaim.ApprovalStatus;
                            //Session["ApprovedStatus"] = oSupplierPO.Approvalstatus;
                            TempData["VerifierIDs"] = string.Join(",", verifierIDs.Skip(1));
                            expenseClaimVM.IsActionAllowed = true;
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
                    TempData["VerifierIDs"] = mstExpenseClaim.Verifier;
                    TempData["ApproverIDs"] = mstExpenseClaim.Approver;
                }

                if (mstExpenseClaim.HODApprover != "" && mstExpenseClaim.Verifier == "")
                {
                    string[] hodApproverIDs = mstExpenseClaim.HODApprover.Split(',');
                    TempData["QueryMCHODApproverIDs"] = string.Join(",", hodApproverIDs);
                    foreach (string approverID in hodApproverIDs)
                    {
                        if (approverID != "" && approverID == HttpContext.User.FindFirst("userid").Value && User.IsInRole("Finance"))
                        {
                            TempData["ApprovedStatus"] = mstExpenseClaim.ApprovalStatus;
                            //Session["ApprovedStatus"] = oSupplierPO.Approvalstatus;
                            TempData["HODApproverIDs"] = string.Join(",", hodApproverIDs.Skip(1));
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

                //Approval Process code
                if (mstExpenseClaim.Approver != "" && mstExpenseClaim.Verifier == "")
                {
                    string[] approverIDs = mstExpenseClaim.Approver.Split(',');
                    TempData["QueryMCApproverIDs"] = string.Join(",", approverIDs);
                    foreach (string approverID in approverIDs)
                    {
                        if ((approverID != "" && approverID == HttpContext.User.FindFirst("userid").Value || (delegatedUserId.HasValue && approverID == delegatedUserId.Value.ToString())) && User.IsInRole("Finance"))
                        {
                            TempData["ApprovedStatus"] = mstExpenseClaim.ApprovalStatus;
                            //Session["ApprovedStatus"] = oSupplierPO.Approvalstatus;
                            TempData["ApproverIDs"] = string.Join(",", approverIDs.Skip(1));
                            expenseClaimVM.IsActionAllowed = true;
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
                var Approvers = TempData["QueryMCApproverIDs"];
                var HODApprovers = TempData["QueryMCHODApproverIDs"];

                string[] CreaterId = Creater.ToString().Split(',');
                string[] VerifiersId = Verifiers.ToString().Split(',');
                string[] ApproversId = Approvers.ToString().Split(',');
                string[] HODApproversId = HODApprovers.ToString().Split(',');

                UserIds.AddRange(CreaterId);
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


                BindGSTDropdown();
                return View(expenseClaimDetailVM);
            }
            else
            {
                return Redirect("~/Login/Login");
            }
        }

        public async Task<JsonResult> UpdateStatusforVoid(string id, string reason, string approvedStatus)
        {
            if (User != null && User.Identity.IsAuthenticated)
            {
                int ECID = Convert.ToInt32(id);

                var mstExpenseClaim = await _repository.MstExpenseClaim.GetExpenseClaimByIdAsync(ECID);

                if (mstExpenseClaim == null)
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

        [HttpPost]
        public async Task<JsonResult> SaveItems(string data)
        {
            //var expenseClaimViewModel = JsonConvert.DeserializeObject<ExpenseClaimViewModel>(data,
            //    new IsoDateTimeConverter { DateTimeFormat = "dd/MM/yyyy" });   

            var expenseClaimViewModel = JsonConvert.DeserializeObject<ExpenseClaimViewModel>(data);

            var mstFacility = await _repository.MstFacility.GetFacilityWithDepartmentByIdAsync(Convert.ToInt32(HttpContext.User.FindFirst("delegatefacilityid") is null ? HttpContext.User.FindFirst("facilityid").Value : HttpContext.User.FindFirst("delegatefacilityid").Value));



            MstExpenseClaim mstExpenseClaim = new MstExpenseClaim();
            mstExpenseClaim.ECNo = expenseClaimViewModel.ECNo;
            mstExpenseClaim.UserID = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
            mstExpenseClaim.ClaimType = expenseClaimViewModel.ClaimType;
            mstExpenseClaim.Verifier = "";
            mstExpenseClaim.Approver = "";
            mstExpenseClaim.FinalApprover = "";
            mstExpenseClaim.ApprovalStatus = 1;
            mstExpenseClaim.GrandTotal = expenseClaimViewModel.GrandTotal;
            mstExpenseClaim.TotalAmount = expenseClaimViewModel.TotalAmount;
            mstExpenseClaim.Company = expenseClaimViewModel.Company;
            mstExpenseClaim.FacilityID = Convert.ToInt32(HttpContext.User.FindFirst("delegatefacilityid") is null ? HttpContext.User.FindFirst("facilityid").Value : HttpContext.User.FindFirst("delegatefacilityid").Value);
            mstExpenseClaim.DepartmentID = mstFacility.MstDepartment.DepartmentID;
            mstExpenseClaim.CreatedDate = DateTime.Now;
            mstExpenseClaim.ModifiedDate = DateTime.Now;
            mstExpenseClaim.CreatedBy = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
            mstExpenseClaim.ModifiedBy = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
            mstExpenseClaim.ApprovalDate = DateTime.Now;
            mstExpenseClaim.ApprovalBy = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
            mstExpenseClaim.TnC = true;

            foreach (var dtItem in expenseClaimViewModel.dtClaims)
            {
                var mstFacility1 = await _repository.MstFacility.GetFacilityWithDepartmentByIdAsync(Convert.ToInt32(dtItem.FacilityID));

                var mstExpenseCategory = await _repository.MstExpenseCategory.GetExpenseCategoryWithTypesByIdAsync(dtItem.ExpenseCategoryID);
                dtItem.MstExpenseCategory = mstExpenseCategory;
                if (mstExpenseCategory.MstCostType.CostType.ToLower().Contains("indirect cost"))
                {
                    dtItem.AccountCode = mstExpenseCategory.ExpenseCode + "-" + mstFacility1.MstDepartment.Code + "-" + mstFacility1.Code + mstExpenseCategory.Default;
                }
                else if (mstExpenseCategory.MstCostType.CostType.ToLower().Contains("direct cost"))
                {
                    dtItem.AccountCode = mstExpenseCategory.MstCostStructure.Code + "-" + mstFacility1.MstDepartment.Code + "-" + mstFacility1.Code + mstExpenseCategory.Default + mstExpenseCategory.ExpenseCode;
                }
                else if (mstExpenseCategory.MstCostType.CostType.ToLower().Contains("hq"))
                {
                    dtItem.AccountCode = mstExpenseCategory.ExpenseCode + "-" + mstFacility1.MstDepartment.Code + "-" + mstFacility1.Code + mstExpenseCategory.Default;
                }
                else
                {
                    dtItem.AccountCode = mstExpenseCategory.ExpenseCode;
                }
            }

            string ClaimStatus = "";
            long ECID = 0;
            try
            {
                //CBRID = Convert.ToInt32(Session["CBRID"].ToString());
                ECID = Convert.ToInt64(expenseClaimViewModel.ECID);
                if (ECID == 0)
                    ClaimStatus = "Add";
                else
                    ClaimStatus = "Update";

                mstExpenseClaim.ECID = ECID;
            }

            catch { }

            ExpenseClaimDetailVM expenseClaimDetailVM = new ExpenseClaimDetailVM();
            //List<DtMileageClaimVM> dtMileageClaimVMs = new List<DtMileageClaimVM>();
            expenseClaimDetailVM.DtExpenseClaimVMs = new List<DtExpenseClaimVM>();
            // List<clsDtSupplierPO> oclsDtSupplierPO = new List<clsDtSupplierPO>();
            foreach (var item in expenseClaimViewModel.dtClaims)
            {
                DtExpenseClaimVM dtExpenseClaimVM = new DtExpenseClaimVM();
                if (expenseClaimViewModel.ClaimAddCondition == "claimDraft")
                {
                    dtExpenseClaimVM.ECID = 0;
                }
                else
                {
                    dtExpenseClaimVM.ECID = item.ECID;
                }
                dtExpenseClaimVM.FacilityID = item.FacilityID;
                //dtExpenseClaimVM.Payee = item.Payee;
                //dtExpenseClaimVM.Particulars = item.Particulars;
                dtExpenseClaimVM.Description = item.Description;
                dtExpenseClaimVM.ExpenseCategory = item.MstExpenseCategory.Description;
                dtExpenseClaimVM.ExpenseCategoryID = item.MstExpenseCategory.ExpenseCategoryID;
                //dtExpenseClaimVM.Reason = item.Reason;
                //dtExpenseClaimVM.EmployeeNo = item.EmployeeNo;
                //dtExpenseClaimVM.ChequeNo = item.ChequeNo;
                dtExpenseClaimVM.Amount = item.Amount;
                dtExpenseClaimVM.Gst = item.GST;
                dtExpenseClaimVM.GSTPercentage = item.GSTPercentage;
                dtExpenseClaimVM.AmountWithGST = item.Amount + item.GST;
                //dtExpenseClaimVM.Facility = item.Facility;
                dtExpenseClaimVM.AccountCode = item.AccountCode;
                dtExpenseClaimVM.DateOfJourney = item.Date;
                expenseClaimDetailVM.DtExpenseClaimVMs.Add(dtExpenseClaimVM);
            }

            var GroupByQS = expenseClaimDetailVM.DtExpenseClaimVMs.GroupBy(s => new { s.AccountCode, s.ExpenseCategory, s.FacilityID, s.Gst });

            expenseClaimDetailVM.DtExpenseClaimVMSummary = new List<DtExpenseClaimVM>();

            foreach (var group in GroupByQS)
            {
                DtExpenseClaimVM dtExpenseClaimVM = new DtExpenseClaimVM();
                decimal amount = 0;
                decimal gst = 0;
                decimal gstpercentage = 0;
                decimal sumamount = 0;
                string ExpenseDesc = string.Empty;
                string ExpenseCat = string.Empty;
                string Facility = string.Empty;
                string AccountCode = string.Empty;
                int? ExpenseCatID = 0;
                int? facilityID = 0;
                int i = 0;
                foreach (var dtExpense in group)
                {
                    if (i == 0)
                        ExpenseDesc = dtExpense.Description;
                    i++;
                    amount = amount + dtExpense.Amount;
                    gst = gst + dtExpense.Gst;
                    gstpercentage = dtExpense.GSTPercentage;
                    sumamount = sumamount + dtExpense.AmountWithGST;
                    ExpenseCat = dtExpense.ExpenseCategory;
                    ExpenseCatID = dtExpense.ExpenseCategoryID;
                    facilityID = dtExpense.FacilityID;
                    if (dtExpense.FacilityID != null)
                    {
                        var mstFacility1 = await _repository.MstFacility.GetFacilityByIdAsync(dtExpense.FacilityID);
                        Facility = mstFacility1.FacilityName;
                    }
                    AccountCode = dtExpense.AccountCode;
                }
                //gst = gst / group.Count();
                dtExpenseClaimVM.Description = ExpenseDesc;
                dtExpenseClaimVM.ExpenseCategory = ExpenseCat;
                dtExpenseClaimVM.ExpenseCategoryID = ExpenseCatID;
                dtExpenseClaimVM.FacilityID = facilityID;
                dtExpenseClaimVM.Facility = Facility;
                dtExpenseClaimVM.AccountCode = AccountCode;
                dtExpenseClaimVM.Amount = amount;
                dtExpenseClaimVM.Gst = gst;
                dtExpenseClaimVM.GSTPercentage = gstpercentage;
                dtExpenseClaimVM.AmountWithGST = sumamount;
                expenseClaimDetailVM.DtExpenseClaimVMSummary.Add(dtExpenseClaimVM);
            }
            List<DtExpenseClaimSummary> lstExpenseClaimSummary = new List<DtExpenseClaimSummary>();
            foreach (var item in expenseClaimDetailVM.DtExpenseClaimVMSummary)
            {
                DtExpenseClaimSummary dtExpenseClaimSummary1 = new DtExpenseClaimSummary();
                dtExpenseClaimSummary1.AccountCode = item.AccountCode;
                dtExpenseClaimSummary1.Amount = item.Amount;
                dtExpenseClaimSummary1.ExpenseCategory = item.ExpenseCategory;
                dtExpenseClaimSummary1.ExpenseCategoryID = item.ExpenseCategoryID;
                dtExpenseClaimSummary1.FacilityID = item.FacilityID;
                dtExpenseClaimSummary1.Facility = item.Facility;
                dtExpenseClaimSummary1.Description = item.Description.ToUpper();
                dtExpenseClaimSummary1.GST = item.Gst;
                dtExpenseClaimSummary1.GSTPercentage = item.GSTPercentage;
                if (item.Gst != 0)
                {
                    dtExpenseClaimSummary1.TaxClass = Math.Round((decimal)item.GSTPercentage, (int)1);
                }
                else
                {
                    dtExpenseClaimSummary1.TaxClass = 4;
                }
                dtExpenseClaimSummary1.AmountWithGST = item.AmountWithGST;
                lstExpenseClaimSummary.Add(dtExpenseClaimSummary1);
            }

            DtExpenseClaimSummary dtExpenseClaimSummary = new DtExpenseClaimSummary();
            dtExpenseClaimSummary.AccountCode = "425000";
            dtExpenseClaimSummary.Amount = mstExpenseClaim.GrandTotal;
            dtExpenseClaimSummary.GST = mstExpenseClaim.TotalAmount - mstExpenseClaim.GrandTotal;
            dtExpenseClaimSummary.AmountWithGST = mstExpenseClaim.TotalAmount;
            dtExpenseClaimSummary.TaxClass = 0;
            dtExpenseClaimSummary.ExpenseCategory = "DBS";
            dtExpenseClaimSummary.Description = "";
            lstExpenseClaimSummary.Add(dtExpenseClaimSummary);

            var res = await _repository.MstExpenseClaim.SaveItems(mstExpenseClaim, expenseClaimViewModel.dtClaims, lstExpenseClaimSummary);

            if (res != 0)
            {
                if (ClaimStatus == "Add")
                    TempData["Message"] = "Expense Claim added successfully";
                else
                    TempData["Message"] = "Expense Claim updated successfully";

                return Json(new { res });
            }
            else
                return Json(new { res });
        }

        public async Task<IActionResult> Create(string id, string Updatestatus)
        {
            //TempData["CBRID"] = 0;
            TempData["Updatestatus"] = "Add";
            ExpenseClaimDetailVM expenseClaimDetailVM = new ExpenseClaimDetailVM();
            expenseClaimDetailVM.DtExpenseClaimVMs = new List<DtExpenseClaimVM>();
            expenseClaimDetailVM.ExpenseClaimAudits = new List<ExpenseClaimAuditVM>();

            TempData["claimaddcondition"] = "claimnew";

            if (User != null && User.Identity.IsAuthenticated)
            {
                if (!string.IsNullOrEmpty(id))
                {
                    long idd = Convert.ToInt64(id);
                    ViewBag.CID = idd;
                    var dtExpenseClaims = await _repository.DtExpenseClaim.GetDtExpenseClaimByIdAsync(idd);

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
                        dtExpenseClaimVM.GSTPercentage = item.GSTPercentage;
                        dtExpenseClaimVM.AmountWithGST = item.Amount + item.GST;
                        dtExpenseClaimVM.ExpenseCategory = item.MstExpenseCategory.Description;
                        dtExpenseClaimVM.AccountCode = item.AccountCode;
                        if (Updatestatus == "Recreate")
                        {
                            ViewBag.UpdateStatus = "Recreate";
                            dtExpenseClaimVM.ECItemID = 0;
                        }
                        expenseClaimDetailVM.DtExpenseClaimVMs.Add(dtExpenseClaimVM);
                    }

                    expenseClaimDetailVM.ExpenseClaimFileUploads = new List<DtExpenseClaimFileUpload>();
                    expenseClaimDetailVM.ExpenseClaimFileUploads = await _repository.DtExpenseClaimFileUpload.GetDtExpenseClaimAuditByIdAsync(idd);

                    //expenseClaimDetailVM.ExpenseClaimFileUploads = new List<DtExpenseClaimFileUpload>();
                    //var fileUploads = await _repository.DtExpenseClaimFileUpload.GetDtExpenseClaimAuditByIdAsync(idd);
                    //if (Updatestatus == "Recreate" && fileUploads != null && fileUploads.Count > 0)
                    //{
                    //    foreach (var uploaddata in fileUploads)
                    //    {
                    //        uploaddata.ECID = 0;
                    //        expenseClaimDetailVM.ExpenseClaimFileUploads.Add(uploaddata);
                    //    }
                    //}

                    var mstExpenseClaim = await _repository.MstExpenseClaim.GetExpenseClaimByIdAsync(idd);

                    ExpenseClaimVM expenseClaimVM = new ExpenseClaimVM();
                    expenseClaimVM.ClaimType = mstExpenseClaim.ClaimType;
                    expenseClaimVM.GrandTotal = mstExpenseClaim.GrandTotal;
                    expenseClaimVM.TotalAmount = mstExpenseClaim.TotalAmount;
                    expenseClaimVM.GrandGST = mstExpenseClaim.TotalAmount - mstExpenseClaim.GrandTotal;
                    expenseClaimVM.Company = mstExpenseClaim.Company;
                    expenseClaimVM.Name = mstExpenseClaim.MstUser.Name;
                    expenseClaimVM.DepartmentName = mstExpenseClaim.MstDepartment.Department;
                    expenseClaimVM.FacilityName = mstExpenseClaim.MstFacility.FacilityName;
                    expenseClaimVM.CreatedDate = Convert.ToDateTime(mstExpenseClaim.CreatedDate).ToString("dd/MM/yyyy", System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
                    expenseClaimVM.Verifier = mstExpenseClaim.Verifier;
                    expenseClaimVM.Approver = mstExpenseClaim.Approver;
                    expenseClaimVM.ECNo = mstExpenseClaim.ECNo;

                    expenseClaimDetailVM.ExpenseClaimVM = expenseClaimVM;

                    if (Updatestatus == "New")
                    {
                        TempData["status"] = "Add";
                        TempData["Updatestatus"] = "Add";
                        ViewBag.ClaimStatus = "Add";
                    }
                    else if (Updatestatus == "Recreate")
                    {
                        TempData["status"] = "Recreate";
                        TempData["Updatestatus"] = "Recreate";
                        ViewBag.ClaimStatus = "Recreate";
                    }
                    else
                    {
                        TempData["status"] = "Update";
                        TempData["Updatestatus"] = "Update";
                        ViewBag.ClaimStatus = "Update";
                    }
                }
                else
                {
                    expenseClaimDetailVM.ExpenseClaimAudits = new List<ExpenseClaimAuditVM>();
                    expenseClaimDetailVM.ExpenseClaimFileUploads = new List<DtExpenseClaimFileUpload>();
                    ExpenseClaimVM expenseClaimVM = new ExpenseClaimVM();
                    expenseClaimVM.ClaimType = "";
                    expenseClaimVM.GrandTotal = 0;
                    expenseClaimVM.GrandGST = 0;
                    expenseClaimVM.TotalAmount = 0;
                    expenseClaimVM.Company = "";
                    expenseClaimVM.Name = "";
                    expenseClaimVM.DepartmentName = "";
                    expenseClaimVM.FacilityName = "";
                    expenseClaimVM.CreatedDate = "";
                    expenseClaimVM.Verifier = "";
                    expenseClaimVM.Approver = "";
                    expenseClaimVM.ECNo = "";

                    DtExpenseClaimVM dtExpenseClaimVM = new DtExpenseClaimVM();

                    dtExpenseClaimVM.ECItemID = 0;
                    dtExpenseClaimVM.ECID = 0;
                    //dtExpenseClaimVM.DateOfJourney = "";

                    dtExpenseClaimVM.Description = "";
                    dtExpenseClaimVM.Amount = 0;
                    dtExpenseClaimVM.Gst = 0;
                    dtExpenseClaimVM.AmountWithGST = 0;
                    dtExpenseClaimVM.ExpenseCategory = "";
                    dtExpenseClaimVM.AccountCode = "";

                    expenseClaimDetailVM.DtExpenseClaimVMs.Add(dtExpenseClaimVM);
                    expenseClaimDetailVM.ExpenseClaimVM = expenseClaimVM;


                    TempData["status"] = "Add";
                }
                ViewData["ExpenseCategoryID"] = new SelectList(await _repository.MstExpenseCategory.GetAllExpenseCategoriesByClaimTypesAsync("expense/pv-cheque/pv-giro", "active"), "ExpenseCategoryID", "Description");
                var mstUsersWithDetails = await _repository.MstUser.GetUserWithDetailsByIdAsync(Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value));

                SelectList facilities = new SelectList(await _repository.MstFacility.GetAllFacilityAsync("active"), "FacilityID", "FacilityName");
                //int userFacilityId = mstUsersWithDetails.MstFacility.FacilityID;
                int userFacilityId = Convert.ToInt32(HttpContext.User.FindFirst("delegatefacilityid") is null ? HttpContext.User.FindFirst("facilityid").Value : HttpContext.User.FindFirst("delegatefacilityid").Value);
                var userFacility = facilities.Where(x => x.Value == userFacilityId.ToString()).FirstOrDefault();
                if (userFacility != null)
                {
                    facilities.Where(x => x.Value == userFacilityId.ToString()).FirstOrDefault().Selected = true;
                }
                ViewData["FacilityID"] = facilities;

                var currFacility = await _repository.MstFacility.GetFacilityWithDepartmentByIdAsync(userFacilityId);

                var delegatedUserName = string.Empty;
                if (HttpContext.User.FindFirst("delegateuserid") is not null)
                {
                    var delUserDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid").Value));
                    delegatedUserName = delUserDetails.Name;
                }

                ViewData["Name"] = string.IsNullOrEmpty(delegatedUserName) ? User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.GivenName).Value : delegatedUserName + "(" + User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.GivenName).Value + ")";
                ViewData["FacilityName"] = currFacility.FacilityName;
                ViewData["Department"] = currFacility.MstDepartment.Department;
                string pettyCashLimit = _configuration.GetValue<string>("PettyCashLimit");
                ViewBag.PettyCashLimit = pettyCashLimit;
                BindGSTDropdown();
                string financeGstValueBuffer = _configuration.GetValue<string>("FinanceGstValueBuffer");
                ViewBag.FinanceGstValueBuffer = financeGstValueBuffer;
            }
            return View(expenseClaimDetailVM);

        }

        public async Task<ActionResult> DeleteExpenseClaimFile(string fileID, string filepath, string ECID)
        {
            DtExpenseClaimFileUpload dtExpenseClaimFileUpload = new DtExpenseClaimFileUpload();

            if (CloudStorageAccount.TryParse(_configuration.GetSection("ConnectionStrings")["BlobConnectionString"], out CloudStorageAccount storageAccount))
            {
                CloudBlobClient BlobClient = storageAccount.CreateCloudBlobClient();
                CloudBlobContainer container = BlobClient.GetContainerReference(_configuration.GetSection("ConnectionStrings")["BlobContainerName"]);

                if (await container.ExistsAsync())
                {
                    CloudBlob file = container.GetBlobReference("FileUploads/ExpenseClaimFiles/" + filepath);

                    if (await file.ExistsAsync())
                    {
                        await file.DeleteIfExistsAsync();
                        dtExpenseClaimFileUpload = await _repository.DtExpenseClaimFileUpload.GetDtExpenseClaimFileUploadByIdAsync(Convert.ToInt64(fileID));
                        _repository.DtExpenseClaimFileUpload.DeleteDtExpenseClaimFileUpload(dtExpenseClaimFileUpload);
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

            return RedirectToAction("Create", "FinanceExpenseClaim", new
            {
                id = ECID,
                Updatestatus = "Edit"
            });
        }

        private void BindGSTDropdown()
        {
            var IsDefaultItems = _repository.MstTaxClass.GetAllTaxClass()
                           .Where(p => p.IsDefault == true).ToList();
            var DefaultSelectedItems = (from taxclass in IsDefaultItems
                                        select new SelectListItem
                                        {
                                            Text = taxclass.TaxClass.ToString(),
                                            Value = taxclass.TaxClass.ToString(),
                                            Selected = true
                                        }).OrderBy(p => p.Text);

            var IsOptionalSelectedItems = (from a in _repository.MstTaxClass.GetAllTaxClass()
                                           from b in _repository.MstTaxClass.GetAllTaxClass().Where(x => x.TaxClassID == a.OptionalTaxClassID).DefaultIfEmpty()
                                           where (a.IsDefault == true && a.IsOptional == true)
                                           select new
                                           {
                                               b.TaxClass
                                           }).ToList().AsEnumerable();

            List<SelectListItem> items = new List<SelectListItem>();
            foreach (var item in DefaultSelectedItems)
            {
                items.Add(new SelectListItem()
                {
                    Text = item.Text.ToString(),
                    Value = item.Value.ToString(),
                    Selected = true
                });
            }
            foreach (var item in IsOptionalSelectedItems)
            {
                items.Add(new SelectListItem()
                {
                    Text = item.TaxClass.ToString(),
                    Value = item.TaxClass.ToString()
                });
            }
            items.Add(new SelectListItem() { Text = "0.00", Value = "0.00" });
            ViewBag.dllGST = items;
        }

        public async Task<JsonResult> ExporttoExcel(string data)
        {
            var expenseClaimSearch = JsonConvert.DeserializeObject<ExpenseClaimSearch>(data);

            var mstExpenseClaimsWithDetails = await _repository.MstExpenseClaim.GetAllExpenseClaimWithDetailsAsync(expenseClaimSearch.ExpenseID, expenseClaimSearch.UserID, expenseClaimSearch.FacilityID, expenseClaimSearch.StatusID, expenseClaimSearch.FromDate, expenseClaimSearch.ToDate);
            List<ExpenseClaimVM> expenseClaimVMs = new List<ExpenseClaimVM>();

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

            foreach (var mc in mstExpenseClaimsWithDetails)
            {
                ExpenseClaimVM expenseClaimVM = new ExpenseClaimVM();
                expenseClaimVM.ApprovalStatus = mc.ApprovalStatus;

                if (mc.ApprovalStatus == 1)
                {
                    expenseClaimVM.ExpenseStatusName = "Awaiting Verification";

                }
                else if (mc.ApprovalStatus == 2)
                {
                    expenseClaimVM.ExpenseStatusName = "Awaiting Signatory approval";

                }
                else if (mc.ApprovalStatus == 3)
                {
                    expenseClaimVM.ExpenseStatusName = "Approved";

                }
                else if (mc.ApprovalStatus == 4)
                {
                    expenseClaimVM.ExpenseStatusName = "Request to Amend";
                }
                else if (mc.ApprovalStatus == 5)
                {
                    expenseClaimVM.ExpenseStatusName = "Voided";

                }
                else if (mc.ApprovalStatus == -5)
                {
                    expenseClaimVM.ExpenseStatusName = "Requested to Void";

                }
                else if (mc.ApprovalStatus == 6)
                {
                    expenseClaimVM.ExpenseStatusName = "Awaiting approval";

                }
                else if (mc.ApprovalStatus == 7)
                {
                    expenseClaimVM.ExpenseStatusName = "Awaiting HOD approval";

                }
                else if (mc.ApprovalStatus == 9)
                {
                    expenseClaimVM.ExpenseStatusName = "Exported to AccPac";

                }
                else if (mc.ApprovalStatus == 10)
                {
                    expenseClaimVM.ExpenseStatusName = "Exported to Bank";

                }
                else
                {
                    expenseClaimVM.ExpenseStatusName = "New";
                }


                if (mc.UserApprovers != "")
                {
                    expenseClaimVM.Approver = mc.UserApprovers.Split(',').First();
                    if (expenseClaimVM.Approver == HttpContext.User.FindFirst("userid").Value && (expenseClaimVM.ApprovalStatus == 6))
                    {
                        expenseClaimVM.IsActionAllowed = true;
                    }
                }
                else if (mc.Verifier != "")
                {
                    expenseClaimVM.Approver = mc.Verifier.Split(',').First();
                    if (expenseClaimVM.Approver == HttpContext.User.FindFirst("userid").Value && (expenseClaimVM.ApprovalStatus == 1 || expenseClaimVM.ApprovalStatus == 2))
                    {
                        expenseClaimVM.IsActionAllowed = true;
                    }
                    //string VerifierIDs = string.Join(",", ExpenseverifierIDs.Skip(1));
                }
                else if (mc.Approver != "")
                {
                    expenseClaimVM.Approver = mc.Approver.Split(',').First();
                    if (expenseClaimVM.Approver == HttpContext.User.FindFirst("userid").Value && (expenseClaimVM.ApprovalStatus == 1 || expenseClaimVM.ApprovalStatus == 2))
                    {
                        expenseClaimVM.IsActionAllowed = true;
                    }
                }
                else if (mc.HODApprover != "")
                {
                    expenseClaimVM.Approver = mc.HODApprover.Split(',').First();
                    if (expenseClaimVM.Approver == HttpContext.User.FindFirst("userid").Value && (expenseClaimVM.ApprovalStatus == 7))
                    {
                        expenseClaimVM.IsActionAllowed = true;
                    }
                }
                else
                {
                    expenseClaimVM.Approver = "";
                }

                if (expenseClaimVM.Approver != "")
                {
                    var mstUserApprover = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(expenseClaimVM.Approver));
                    if (expenseClaimVM.ApprovalStatus != 3 && expenseClaimVM.ApprovalStatus != 4 && expenseClaimVM.ApprovalStatus != -5 && expenseClaimVM.ApprovalStatus != 5)
                        expenseClaimVM.Approver = mstUserApprover.Name;
                    else
                        expenseClaimVM.Approver = "";
                }


                dt.Rows.Add(expenseClaimVM.ECNo = mc.CNO,
                            expenseClaimVM.Name = mc.Name,
                            expenseClaimVM.CreatedDate = Convert.ToDateTime(mc.CreatedDate).ToString("dd/MM/yyyy", System.Globalization.CultureInfo.CreateSpecificCulture("en-US")),
                            expenseClaimVM.FacilityName = mc.FacilityName,
                            expenseClaimVM.Name = mc.Name,
                            expenseClaimVM.Phone = mc.Phone,
                            expenseClaimVM.TotalAmount = mc.TotalAmount,
                            expenseClaimVM.Approver = expenseClaimVM.Approver,
                            expenseClaimVM.ExpenseStatusName = expenseClaimVM.ExpenseStatusName);
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

            string filename = "ExpenseClaims" + DateTime.Now.ToString("ddMMyyyyss") + ".xlsx";
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
                        return File(blobStream, file.Properties.ContentType, "ExpenseClaims-Export.xlsx");
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
                expenseClaimVM.GrandGST = mstExpenseClaim.TotalAmount - mstExpenseClaim.GrandTotal;
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
        public async Task<IActionResult> GetPrint(string data)
        {
            var expenseClaimSearch = JsonConvert.DeserializeObject<ExpenseClaimSearch>(data);
            var mstExpenseClaimsWithDetails = await _repository.MstExpenseClaim.GetAllExpenseClaimWithDetailsAsync(expenseClaimSearch.ExpenseID, expenseClaimSearch.UserID, expenseClaimSearch.FacilityID, expenseClaimSearch.StatusID, expenseClaimSearch.FromDate, expenseClaimSearch.ToDate);

            List<ExpenseClaimVM> expenseClaimVMs = new List<ExpenseClaimVM>();


            foreach (var mc in mstExpenseClaimsWithDetails)
            {
                ExpenseClaimVM expenseClaimVM = new ExpenseClaimVM();

                expenseClaimVM.ECNo = mc.CNO;
                expenseClaimVM.Name = mc.Name;
                expenseClaimVM.CreatedDate = Convert.ToDateTime(mc.CreatedDate).ToString("dd/MM/yyyy", System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
                expenseClaimVM.FacilityName = mc.FacilityName;
                expenseClaimVM.Name = mc.Name;
                expenseClaimVM.Phone = mc.Phone;
                expenseClaimVM.TotalAmount = mc.TotalAmount;
                expenseClaimVM.ApprovalStatus = mc.ApprovalStatus;
                expenseClaimVM.VoucherNo = mc.VoucherNo;

                var mstDtExpenseClaim = await _repository.DtExpenseClaim.GetTopDtExpenseClaimByIdAsync(mc.CID);
                if (mstDtExpenseClaim != null)
                    expenseClaimVM.Description = mstDtExpenseClaim.Description;
                else
                    expenseClaimVM.Description = "";

                if (mc.ApprovalStatus == 1)
                {
                    expenseClaimVM.ExpenseStatusName = "Awaiting Verification";

                }
                else if (mc.ApprovalStatus == 2)
                {
                    expenseClaimVM.ExpenseStatusName = "Awaiting Signatory approval";

                }
                else if (mc.ApprovalStatus == 3)
                {
                    expenseClaimVM.ExpenseStatusName = "Approved";

                }
                else if (mc.ApprovalStatus == 4)
                {
                    expenseClaimVM.ExpenseStatusName = "Request to Amend";
                }
                else if (mc.ApprovalStatus == 5)
                {
                    expenseClaimVM.ExpenseStatusName = "Voided";

                }
                else if (mc.ApprovalStatus == -5)
                {
                    expenseClaimVM.ExpenseStatusName = "Requested to Void";

                }
                else if (mc.ApprovalStatus == 6)
                {
                    expenseClaimVM.ExpenseStatusName = "Awaiting approval";

                }
                else if (mc.ApprovalStatus == 7)
                {
                    expenseClaimVM.ExpenseStatusName = "Awaiting HOD approval";

                }
                else if (mc.ApprovalStatus == 9)
                {
                    expenseClaimVM.ExpenseStatusName = "Exported to AccPac";

                }
                else if (mc.ApprovalStatus == 10)
                {
                    expenseClaimVM.ExpenseStatusName = "Exported to Bank";

                }
                else
                {
                    expenseClaimVM.ExpenseStatusName = "New";
                }


                if (mc.UserApprovers != "")
                {
                    expenseClaimVM.Approver = mc.UserApprovers.Split(',').First();
                    if (expenseClaimVM.Approver == HttpContext.User.FindFirst("userid").Value && (expenseClaimVM.ApprovalStatus == 6))
                    {
                        expenseClaimVM.IsActionAllowed = true;
                    }
                }
                else if (mc.Verifier != "")
                {
                    expenseClaimVM.Approver = mc.Verifier.Split(',').First();
                    if (expenseClaimVM.Approver == HttpContext.User.FindFirst("userid").Value && (expenseClaimVM.ApprovalStatus == 1 || expenseClaimVM.ApprovalStatus == 2))
                    {
                        expenseClaimVM.IsActionAllowed = true;
                    }
                    //string VerifierIDs = string.Join(",", ExpenseverifierIDs.Skip(1));
                }
                else if (mc.Approver != "")
                {
                    expenseClaimVM.Approver = mc.Approver.Split(',').First();
                    if (expenseClaimVM.Approver == HttpContext.User.FindFirst("userid").Value && (expenseClaimVM.ApprovalStatus == 1 || expenseClaimVM.ApprovalStatus == 2))
                    {
                        expenseClaimVM.IsActionAllowed = true;
                    }
                }
                else if (mc.HODApprover != "")
                {
                    expenseClaimVM.Approver = mc.HODApprover.Split(',').First();
                    if (expenseClaimVM.Approver == HttpContext.User.FindFirst("userid").Value && (expenseClaimVM.ApprovalStatus == 7))
                    {
                        expenseClaimVM.IsActionAllowed = true;
                    }
                }
                else
                {
                    expenseClaimVM.Approver = "";
                }

                if (expenseClaimVM.Approver != "")
                {
                    var mstUserApprover = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(expenseClaimVM.Approver));
                    expenseClaimVM.Approver = mstUserApprover.Name;
                }
                expenseClaimVMs.Add(expenseClaimVM);
            }
            return PartialView("GetExpensePrint", expenseClaimVMs);
        }

        public async Task<JsonResult> UpdateStatus(string id)
        {
            if (User != null && User.Identity.IsAuthenticated)
            {
                int ECID = Convert.ToInt32(id);

                var mstExpenseClaim = await _repository.MstExpenseClaim.GetExpenseClaimByIdAsync(ECID);

                if (mstExpenseClaim == null)
                {
                    // return NotFound();
                }


                bool isAlternateApprover = false;
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
                            HODApproverID = mstExpenseClaim.HODApprover;



                            //BackgroundJob.Enqueue(() => _sendMailServices.SendEmail());
                            //Mail Code Implementation for Verifiers

                            foreach (string verifierID in verifierIDs)
                            {
                                if (verifierID != "")
                                {
                                    string domainUrl = HttpContext.Request.Scheme + "://" + HttpContext.Request.Host;
                                    string clickUrl = domainUrl + "/" + "FinanceExpenseClaim/Details/" + ECID;

                                    var mstSenderDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(HttpContext.User.FindFirst("userid").Value));
                                    var senderName = mstSenderDetails.Name;
                                    int? approverId = await _alternateApproverHelper.IsAlternateApprovalSetForUser(Convert.ToInt32(verifierID));
                                    var mstVerifierDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(verifierID));
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

                                    BackgroundJob.Enqueue(() => _sendMailServices.SendEmail("EmailTemplate.html", screen, subject, senderName, receiverName, claimNo, approvalType, userID, toEmail, clickUrl));
                                }
                                else if (HODApproverID != "")
                                {
                                    string domainUrl = HttpContext.Request.Scheme + "://" + HttpContext.Request.Host;
                                    string clickUrl = domainUrl + "/" + "HodSummary/ECDetails/" + ECID;

                                    var mstSenderDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(HttpContext.User.FindFirst("userid").Value));
                                    var senderName = mstSenderDetails.Name;
                                    int? approverId = await _alternateApproverHelper.IsAlternateApprovalSetForUser(Convert.ToInt32(HODApproverID));
                                    var mstVerifierDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(HODApproverID));
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

                                    BackgroundJob.Enqueue(() => _sendMailServices.SendEmail("EmailTemplate.html", screen, subject, senderName, receiverName, claimNo, approvalType, userID, toEmail, clickUrl));
                                    _logger.LogInfo($"Inside UpdateStatus after Enqueued the SendEmail in  FinanceExpenseClaim HoDApproval");
                                }
                                break;
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError($"Something went wrong inside FinanceExpenseClaim UpdateStatus : {ex.Message},{ex.StackTrace}");
                        }
                        await _repository.MstExpenseClaim.UpdateMstExpenseClaimStatus(ECID, 7, int.Parse(HttpContext.User.FindFirst("userid").Value), DateTime.Now, string.Empty, VerifierIDs.ToString(), ApproverIDs.ToString(), UserApproverIDs.ToString(), HODApproverID.ToString(), isAlternateApprover, 0);

                    }
                    #endregion

                    #region Expense Approver
                    else if (ApprovedStatus == 2)
                    {
                        string VerifierIDs = "";
                        string ApproverIDs = "";
                        string UserApproverIDs = "";
                        string HODApproverID = "";
                        string DVerifierIDs = "";
                        try
                        {
                            string[] ExpenseapproverIDs = mstExpenseClaim.Approver.Split(',');
                            ApproverIDs = string.Join(",", ExpenseapproverIDs.Skip(1));
                            string[] approverIDs = ApproverIDs.Split(',');
                            int CreatedBy = Convert.ToInt32(mstExpenseClaim.CreatedBy);

                            DVerifierIDs = mstExpenseClaim.DVerifier.Split(',').First();

                            //BackgroundJob.Enqueue(() => _sendMailServices.SendEmail("EmailTemplate.html",screen,subject,message,CreatedBy,toEmail));
                            //Mail Code Implementation for Approvers

                            foreach (string approverID in approverIDs)
                            {
                                if (approverID != "")
                                {
                                    string domainUrl = HttpContext.Request.Scheme + "://" + HttpContext.Request.Host;
                                    string clickUrl = domainUrl + "/" + "FinanceExpenseClaim/Details/" + ECID;

                                    var mstSenderDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(HttpContext.User.FindFirst("userid").Value));
                                    var senderName = mstSenderDetails.Name;
                                    var mstVerifierDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(approverID));
                                    var toEmail = mstVerifierDetails.EmailAddress;
                                    var receiverName = mstVerifierDetails.Name;
                                    var claimNo = mstExpenseClaim.ECNo;
                                    var screen = "Expense Claim";
                                    var approvalType = "Approval Request";
                                    int userID = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value);
                                    var subject = "Expense Claim for Approval " + claimNo;

                                    BackgroundJob.Enqueue(() => _sendMailServices.SendEmail("EmailTemplate.html", screen, subject, senderName, receiverName, claimNo, approvalType, userID, toEmail, clickUrl));

                                }

                                break;
                            }

                        }
                        catch
                        {
                        }
                        string financeStartDay = _configuration.GetValue<string>("FinanceStartDay");
                        await _repository.MstExpenseClaim.UpdateMstExpenseClaimStatus(ECID, 3, int.Parse(HttpContext.User.FindFirst("userid").Value), DateTime.Now, string.Empty, VerifierIDs, ApproverIDs, UserApproverIDs.ToString(), HODApproverID.ToString(), isAlternateApprover, int.Parse(financeStartDay));
                        if (ApproverIDs == string.Empty)
                        {
                            string domainUrl = HttpContext.Request.Scheme + "://" + HttpContext.Request.Host;
                            string clickUrl = domainUrl + "/" + "FinanceReports";

                            var mstSenderDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(HttpContext.User.FindFirst("userid").Value));
                            var senderName = mstSenderDetails.Name;
                            var mstVerifierDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(DVerifierIDs));
                            var toEmail = mstVerifierDetails.EmailAddress;
                            var receiverName = mstVerifierDetails.Name;
                            var claimNo = mstExpenseClaim.ECNo;
                            var screen = "Expense Claim";
                            var approvalType = "Export to AccPac/Bank Request";
                            int userID = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value);
                            var subject = "Expense Claim for Export to AccPac/Bank " + claimNo;

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

        public async Task<JsonResult> GetTextValuesSGSummary(string id)
        {
            List<DtExpenseClaimSummary> oDtClaimsSummaryList = new List<DtExpenseClaimSummary>();

            try
            {
                var dtExpenseClaimSummaries = await _repository.DtExpenseClaimSummary.GetDtExpenseClaimSummaryByIdAsync(Convert.ToInt64(id));

                return Json(new { DtClaimsList = dtExpenseClaimSummaries });
            }
            catch
            {
                return Json(new { DtClaimsList = oDtClaimsSummaryList });
            }

        }

        [HttpPost]
        public async Task<JsonResult> SaveSummary(string data)
        {
            var expenseClaimViewModel = JsonConvert.DeserializeObject<DtExpenseClaimSummaryVM>(data);
            var expenseCSummary = await _repository.DtExpenseClaimSummary.GetDtExpenseClaimSummaryByIdAsync(expenseClaimViewModel.ECID);
            foreach (var hr in expenseCSummary)
            {
                _repository.DtExpenseClaimSummary.Delete(hr);
            }

            foreach (var dtItem in expenseClaimViewModel.dtClaims)
            {
                if (dtItem.ExpenseCategory != "DBS")
                {
                    dtItem.Description = dtItem.Description.ToUpper();
                    var mstFacility1 = await _repository.MstFacility.GetFacilityWithDepartmentByIdAsync(Convert.ToInt32(dtItem.FacilityID));

                    var mstExpenseCategory = await _repository.MstExpenseCategory.GetExpenseCategoryWithTypesByIdAsync(dtItem.ExpenseCategoryID);
                    //dtItem.MstExpenseCategory = mstExpenseCategory;
                    if (mstExpenseCategory.MstCostType.CostType.ToLower().Contains("indirect cost"))
                    {
                        dtItem.AccountCode = mstExpenseCategory.ExpenseCode + "-" + mstFacility1.MstDepartment.Code + "-" + mstFacility1.Code + mstExpenseCategory.Default;
                    }
                    else if (mstExpenseCategory.MstCostType.CostType.ToLower().Contains("direct cost"))
                    {
                        dtItem.AccountCode = mstExpenseCategory.MstCostStructure.Code + "-" + mstFacility1.MstDepartment.Code + "-" + mstFacility1.Code + mstExpenseCategory.Default + mstExpenseCategory.ExpenseCode;
                    }
                    else if (mstExpenseCategory.MstCostType.CostType.ToLower().Contains("hq"))
                    {
                        dtItem.AccountCode = mstExpenseCategory.ExpenseCode + "-" + mstFacility1.MstDepartment.Code + "-" + mstFacility1.Code + mstExpenseCategory.Default;
                    }
                    else
                    {
                        dtItem.AccountCode = mstExpenseCategory.ExpenseCode;
                    }
                }
            }

            MstExpenseClaimAudit auditUpdate = new MstExpenseClaimAudit();
            auditUpdate.ECID = expenseClaimViewModel.ECID;
            auditUpdate.Action = "1";
            auditUpdate.AuditDate = DateTime.Now;
            auditUpdate.AuditBy = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value);
            //auditUpdate.InstanceID = 1;
            string time = DateTime.Now.ToString("tt", System.Globalization.CultureInfo.InvariantCulture);
            DateTime date = DateTime.Now;
            string formattedDate = date.ToString("dd'/'MM'/'yyyy hh:mm:ss");
            auditUpdate.Description = "Summary of Accounts Allocation Amended by " + User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.GivenName).Value.ToString() + " on " + formattedDate + " " + time + " ";
            auditUpdate.SentTo = "";
            //await _repository.MstPVGClaimAudit.CreatePVGClaimAudit(auditUpdate);
            //await _repository.SaveAsync();
            var res = await _repository.MstExpenseClaim.SaveSummary(expenseClaimViewModel.ECID, expenseClaimViewModel.dtClaims, auditUpdate);

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

        public async Task<JsonResult> UpdateRejectedStatus(string id, string reason)
        {
            if (User != null && User.Identity.IsAuthenticated)
            {
                int ECID = Convert.ToInt32(id);

                var mstExpenseClaim = await _repository.MstExpenseClaim.GetExpenseClaimByIdAsync(ECID);

                if (mstExpenseClaim == null)
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

        public async Task<IActionResult> Download(string id, string name)
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

        public async Task<IActionResult> DownloadView(string id, string name)
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

        #endregion GetMessages
    }
}
