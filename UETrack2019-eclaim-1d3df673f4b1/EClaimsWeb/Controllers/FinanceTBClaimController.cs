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
    public class FinanceTBClaimController : Controller
    {
        private ILoggerManager _logger;
        private IRepositoryWrapper _repository;
        private IMapper _mapper;
        private IConfiguration _configuration;
        private AlternateApproverHelper _alternateApproverHelper;
        private ISendMailServices _sendMailServices;
        private readonly RepositoryContext _context;

        public FinanceTBClaimController(ILoggerManager logger, IRepositoryWrapper repository, IMapper mapper, RepositoryContext context, IConfiguration configuration, ISendMailServices sendMailServices)
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

                var mstTBClaimsWithDetails = await _repository.MstTBClaim.GetAllTBClaimWithDetailsAsync(userID, facilityID, statusID, fromDate, toDate);
                if (mstTBClaimsWithDetails != null && mstTBClaimsWithDetails.Any())
                {
                    mstTBClaimsWithDetails.ToList().ForEach(c => c.IsDelegated = false);
                }

                if (delegatedUserId != null && delegatedUserId.HasValue)
                {
                    var delegatedClaims = await _repository.MstTBClaim.GetAllTBClaimWithDetailsAsync(delegatedUserId.Value, facilityID, statusID, fromDate, toDate);
                    if (delegatedClaims != null && delegatedClaims.Any())
                    {
                        delegatedClaims.ToList().ForEach(c => c.IsDelegated = true);
                        mstTBClaimsWithDetails.ToList().AddRange(delegatedClaims.ToList());
                    }
                }

                _logger.LogInfo($"Returned all Telephone Bill Claims with details from database.");
                List<TBClaimVM> tBClaimVMs = new List<TBClaimVM>();
                foreach (var mc in mstTBClaimsWithDetails)
                {
                    TBClaimVM tBClaimVM = new TBClaimVM();
                    tBClaimVM.TBCID = mc.CID;
                    tBClaimVM.TBCNo = mc.CNO;
                    tBClaimVM.Name = mc.Name;
                    tBClaimVM.CreatedDate = Convert.ToDateTime(mc.CreatedDate).ToString("dd/MM/yyyy", System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
                    tBClaimVM.FacilityName = mc.FacilityName;
                    tBClaimVM.Phone = mc.Phone;
                    tBClaimVM.GrandTotal = mc.GrandTotal;
                    tBClaimVM.ApprovalStatus = mc.ApprovalStatus;
                    tBClaimVM.VoucherNo = mc.VoucherNo;
                    //tBClaimVM.ClaimType = mc.ClaimType;
                    //tBClaimVM.TotalAmount = mc.TotalAmount;

                    if (mc.UserApprovers != "")
                    {
                        tBClaimVM.Approver = mc.UserApprovers.Split(',').First();
                        if ((tBClaimVM.Approver == HttpContext.User.FindFirst("userid").Value || (delegatedUserId.HasValue && tBClaimVM.Approver == delegatedUserId.Value.ToString())) &&
                            (tBClaimVM.ApprovalStatus == 6))
                        {
                            tBClaimVM.IsActionAllowed = false;
                        }
                    }
                    else if (mc.Verifier != "")
                    {
                        tBClaimVM.Approver = mc.Verifier.Split(',').First();
                        if ((tBClaimVM.Approver == HttpContext.User.FindFirst("userid").Value || (delegatedUserId.HasValue && tBClaimVM.Approver == delegatedUserId.Value.ToString())) &&
                            (tBClaimVM.ApprovalStatus == 1 || tBClaimVM.ApprovalStatus == 2))
                        {
                            tBClaimVM.IsActionAllowed = true;
                        }
                        //string VerifierIDs = string.Join(",", MileageverifierIDs.Skip(1));
                    }
                    else if (mc.HODApprover != "")
                    {
                        tBClaimVM.Approver = mc.HODApprover.Split(',').First();
                        if ((tBClaimVM.Approver == HttpContext.User.FindFirst("userid").Value || (delegatedUserId.HasValue && tBClaimVM.Approver == delegatedUserId.Value.ToString())) &&
                            (tBClaimVM.ApprovalStatus == 7))
                        {
                            tBClaimVM.IsActionAllowed = false;
                        }
                    }
                    else if (mc.Approver != "")
                    {
                        tBClaimVM.Approver = mc.Approver.Split(',').First();
                        if ((tBClaimVM.Approver == HttpContext.User.FindFirst("userid").Value || (delegatedUserId.HasValue && tBClaimVM.Approver == delegatedUserId.Value.ToString())) &&
                            (tBClaimVM.ApprovalStatus == 1 || tBClaimVM.ApprovalStatus == 2))
                        {
                            tBClaimVM.IsActionAllowed = true;
                        }
                    }
                    else
                    {
                        tBClaimVM.Approver = "";
                    }

                    if (tBClaimVM.Approver != "")
                    {
                        var alternateUser = await _alternateApproverHelper.IsAlternateApprovalSetForUser(Convert.ToInt32(tBClaimVM.Approver));
                        if (alternateUser.HasValue)
                        {
                            var mstUserApprover = await _repository.MstUser.GetUserByIdAsync(alternateUser.Value);
                            tBClaimVM.Approver = mstUserApprover.Name + " (AA)";
                        }
                        else
                        {
                            var mstUserApprover = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(tBClaimVM.Approver));
                            tBClaimVM.Approver = mstUserApprover.Name;
                        }
                    }

                    // Show actions based on alternate approver settings
                    // Override all the isActionAllowed code above. When alternate approval is set, then no need to show the action on any scenario
                    if (isAlternateApproverSet)
                    {
                        tBClaimVM.IsActionAllowed = false;
                    }

                    tBClaimVMs.Add(tBClaimVM);
                }

                var mstTBClaimVM = new TBClaimSearchViewModel
                {
                    //Screens = new SelectList(await screenQuery.Distinct().ToListAsync()),
                    tBClaimVMs = tBClaimVMs,
                    Statuses = new SelectList(status, "Value", "Text"),
                    Facilities = new SelectList(facilities, "Value", "Text"),
                    Users = new SelectList(users, "Value", "Text"),
                    FromDate = fromDate,
                    ToDate = toDate
                };

                return View(mstTBClaimVM);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Something went wrong inside GetAllTBClaimWithDetailsAsync action: {ex.Message}");
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
            long TBCID = Convert.ToInt64(id);

            if (User != null && User.Identity.IsAuthenticated)
            {
                var mstTBClaim = await _repository.MstTBClaim.GetTBClaimByIdAsync(id);

                if (mstTBClaim == null)
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

                tBClaimDetailVM.TBClaimFileUploads = _repository.DtTBClaimFileUpload.GetDtTBClaimAuditByIdAsync(id).GetAwaiter().GetResult().ToList();

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
                if (mstTBClaim.Verifier != "")
                {
                    string[] verifierIDs = mstTBClaim.Verifier.Split(',');
                    TempData["QueryMCVerifierIDs"] = string.Join(",", verifierIDs);
                    foreach (string verifierID in verifierIDs)
                    {
                        if ((verifierID != "" && verifierID == HttpContext.User.FindFirst("userid").Value || (delegatedUserId.HasValue && verifierID == delegatedUserId.Value.ToString())) && User.IsInRole("Finance"))
                        {
                            TempData["ApprovedStatus"] = mstTBClaim.ApprovalStatus;
                            //Session["ApprovedStatus"] = oSupplierPO.Approvalstatus;
                            TempData["VerifierIDs"] = string.Join(",", verifierIDs.Skip(1));
                            tBClaimVM.IsActionAllowed = true;
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
                    TempData["VerifierIDs"] = mstTBClaim.Verifier;
                    TempData["ApproverIDs"] = mstTBClaim.Approver;
                }

                if (mstTBClaim.HODApprover != "" && mstTBClaim.Verifier == "")
                {
                    string[] hodApproverIDs = mstTBClaim.HODApprover.Split(',');
                    TempData["QueryMCHODApproverIDs"] = string.Join(",", hodApproverIDs);
                    foreach (string approverID in hodApproverIDs)
                    {
                        if (approverID != "" && approverID == HttpContext.User.FindFirst("userid").Value && User.IsInRole("Finance"))
                        {
                            TempData["ApprovedStatus"] = mstTBClaim.ApprovalStatus;
                            //Session["ApprovedStatus"] = oSupplierPO.Approvalstatus;
                            TempData["HODApproverIDs"] = string.Join(",", hodApproverIDs.Skip(1));
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

                //Approval Process code
                if (mstTBClaim.Approver != "" && mstTBClaim.Verifier == "")
                {
                    string[] approverIDs = mstTBClaim.Approver.Split(',');
                    TempData["QueryMCApproverIDs"] = string.Join(",", approverIDs);
                    foreach (string approverID in approverIDs)
                    {
                        if ((approverID != "" && approverID == HttpContext.User.FindFirst("userid").Value || (delegatedUserId.HasValue && approverID == delegatedUserId.Value.ToString())) && User.IsInRole("Finance"))
                        {
                            TempData["ApprovedStatus"] = mstTBClaim.ApprovalStatus;
                            //Session["ApprovedStatus"] = oSupplierPO.Approvalstatus;
                            TempData["ApproverIDs"] = string.Join(",", approverIDs.Skip(1));
                            tBClaimVM.IsActionAllowed = true;
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

        public async Task<JsonResult> UpdateStatusforVoid(string id, string reason, string approvedStatus)
        {
            if (User != null && User.Identity.IsAuthenticated)
            {
                int TBCID = Convert.ToInt32(id);

                var mstTBClaim = await _repository.MstTBClaim.GetTBClaimByIdAsync(TBCID);

                if (mstTBClaim == null)
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
                    var approvalType = "Voided";
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

        public async Task<JsonResult> UpdateStatus(string id)
        {
            bool isAlternateApprover = false;
            if (User != null && User.Identity.IsAuthenticated)
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
                    string usapprover = _repository.MstTBClaim.GetApproverVerifier(TBCID.ToString(), ApprovedStatus, HttpContext.User.FindFirst("userid").Value, "TelephoneBill");
                    int loggedInUserId = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value);
                    var delegatedUserId = await _alternateApproverHelper.IsUserHasAnyAlternateApprovalSet(loggedInUserId);
                    if (!string.IsNullOrEmpty(usapprover))
                    {
                        if ((usapprover == delegatedUserId.Value.ToString()))
                        {
                            excute = true;
                            isAlternateApprover = true;
                        }
                    }
                }

                if (excute == true)
                {
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
                            HODApproverID = mstTBClaim.HODApprover;

                            //Mail Code Implementation for Verifiers
                            foreach (string verifierID in verifierIDs)
                            {
                                if (verifierID != "")
                                {
                                    string domainUrl = HttpContext.Request.Scheme + "://" + HttpContext.Request.Host;
                                    string clickUrl = domainUrl + "/" + "FinanceTBClaim/Details/" + TBCID;

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
                                    var claimNo = mstTBClaim.TBCNo;
                                    var screen = "Telephone Bill Claim";
                                    var approvalType = "Verification Request";
                                    int userID = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value);
                                    var subject = "Telephone Bill Claim for Verification " + claimNo;

                                    BackgroundJob.Enqueue(() => _sendMailServices.SendEmail("EmailTemplate.html", screen, subject, senderName, receiverName, claimNo, approvalType, userID, toEmail, clickUrl));
                                }
                                else if (HODApproverID != "")
                                {
                                    string domainUrl = HttpContext.Request.Scheme + "://" + HttpContext.Request.Host;
                                    string clickUrl = domainUrl + "/" + "HodSummary/TBCDetails/" + TBCID;

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
                                    var claimNo = mstTBClaim.TBCNo;
                                    var screen = "Telephone Bill Claim";
                                    var approvalType = "Approval Request";
                                    int userID = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value);
                                    var subject = "Telephone Bill Claim for Approval " + claimNo;

                                    BackgroundJob.Enqueue(() => _sendMailServices.SendEmail("EmailTemplate.html", screen, subject, senderName, receiverName, claimNo, approvalType, userID, toEmail, clickUrl));
                                }
                                break;
                            }
                        }
                        catch
                        {
                        }
                        await _repository.MstTBClaim.UpdateMstTBClaimStatus(TBCID, 7, int.Parse(HttpContext.User.FindFirst("userid").Value), DateTime.Now, string.Empty, VerifierIDs.ToString(), ApproverIDs.ToString(), UserApproverIDs.ToString(), HODApproverID.ToString(), isAlternateApprover, 0);

                    }
                    #endregion

                    #region TB Approver
                    else if (ApprovedStatus == 2)
                    {
                        string VerifierIDs = "";
                        string ApproverIDs = "";
                        string UserApproverIDs = "";
                        string HODApproverID = "";
                        string DVerifierIDs = "";
                        try
                        {
                            string[] TBapproverIDs = mstTBClaim.Approver.Split(',');
                            ApproverIDs = string.Join(",", TBapproverIDs.Skip(1));
                            string[] approverIDs = ApproverIDs.Split(',');
                            int CreatedBy = Convert.ToInt32(mstTBClaim.CreatedBy);
                            DVerifierIDs = mstTBClaim.DVerifier.Split(',').First();

                            //Mail Code Implementation for Approvers
                            foreach (string approverID in approverIDs)
                            {
                                if (approverID != "")
                                {
                                    string domainUrl = HttpContext.Request.Scheme + "://" + HttpContext.Request.Host;
                                    string clickUrl = domainUrl + "/" + "FinanceTBClaim/Details/" + TBCID;

                                    var mstSenderDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(HttpContext.User.FindFirst("userid").Value));
                                    var senderName = mstSenderDetails.Name;
                                    var mstVerifierDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(approverID));
                                    var toEmail = mstVerifierDetails.EmailAddress;
                                    var receiverName = mstVerifierDetails.Name;
                                    var claimNo = mstTBClaim.TBCNo;
                                    var screen = "Telephone Bill Claim";
                                    var approvalType = "Approval Request";
                                    int userID = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value);
                                    var subject = "Telephone Bill Claim for Approval " + claimNo;

                                    BackgroundJob.Enqueue(() => _sendMailServices.SendEmail("EmailTemplate.html", screen, subject, senderName, receiverName, claimNo, approvalType, userID, toEmail, clickUrl));

                                }

                                break;
                            }
                        }
                        catch
                        {
                        }
                        string financeStartDay = _configuration.GetValue<string>("FinanceStartDay");
                        await _repository.MstTBClaim.UpdateMstTBClaimStatus(TBCID, 3, int.Parse(HttpContext.User.FindFirst("userid").Value), DateTime.Now, string.Empty, VerifierIDs, ApproverIDs, UserApproverIDs.ToString(), HODApproverID.ToString(), isAlternateApprover, int.Parse(financeStartDay));
                        if (ApproverIDs == string.Empty)
                        {
                            string domainUrl = HttpContext.Request.Scheme + "://" + HttpContext.Request.Host;
                            string clickUrl = domainUrl + "/" + "FinanceReports";

                            var mstSenderDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(HttpContext.User.FindFirst("userid").Value));
                            var senderName = mstSenderDetails.Name;
                            var mstVerifierDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(DVerifierIDs));
                            var toEmail = mstVerifierDetails.EmailAddress;
                            var receiverName = mstVerifierDetails.Name;
                            var claimNo = mstTBClaim.TBCNo;
                            var screen = "Telephone Bill Claim";
                            var approvalType = "Export to AccPac/Bank Request";
                            int userID = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value);
                            var subject = "Telephone Bill Claim for Export to AccPac/Bank " + claimNo;

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
            List<DtTBClaimSummary> oDtClaimsSummaryList = new List<DtTBClaimSummary>();

            try
            {
                var dtTBClaimSummaries = await _repository.DtTBClaimSummary.GetDtTBClaimSummaryByIdAsync(Convert.ToInt64(id));

                return Json(new { DtClaimsList = dtTBClaimSummaries });
            }
            catch
            {
                return Json(new { DtClaimsList = oDtClaimsSummaryList });
            }

        }

        [HttpPost]
        public async Task<JsonResult> SaveSummary(string data)
        {
            var tbClaimViewModel = JsonConvert.DeserializeObject<DtTBClaimSummaryVM>(data);
            var tbCSummary = await _repository.DtTBClaimSummary.GetDtTBClaimSummaryByIdAsync(tbClaimViewModel.TBCID);
            foreach (var hr in tbCSummary)
            {
                _repository.DtTBClaimSummary.Delete(hr);
            }

            foreach (var dtItem in tbClaimViewModel.dtClaims)
            {
                if (dtItem.ExpenseCategory != "DBS")
                {
                    dtItem.Description = dtItem.Description.ToUpper();
                    var mstFacility1 = await _repository.MstFacility.GetFacilityWithDepartmentByIdAsync(Convert.ToInt32(dtItem.FacilityID));

                    var mstExpenseCategory = await _repository.MstExpenseCategory.ExpenseCategoriesByClaimType("Telephone Bill");

                    //var mstExpenseCategory = await _repository.MstExpenseCategory.GetExpenseCategoryWithTypesByIdAsync(dtItem.ExpenseCategoryID);

                    dtItem.AccountCode = mstExpenseCategory.ExpenseCode + "-" + mstFacility1.MstDepartment.Code + "-" + mstFacility1.Code + mstExpenseCategory.Default;
                }
            }

            MstTBClaimAudit auditUpdate = new MstTBClaimAudit();
            auditUpdate.TBCID = tbClaimViewModel.TBCID;
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
            var res = await _repository.MstTBClaim.SaveSummary(tbClaimViewModel.TBCID, tbClaimViewModel.dtClaims, auditUpdate);

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
                int TBCID = Convert.ToInt32(id);

                var mstTBClaim = await _repository.MstTBClaim.GetTBClaimByIdAsync(TBCID);

                if (mstTBClaim == null)
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
                        return File(blobStream, file.Properties.ContentType, "TelephoneBillClaims-Export.xlsx");
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
                    CloudBlob file = container.GetBlobReference("FileUploads/TBClaimFiles/" + id);

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
            var mstTBClaimsWithDetails = await _repository.MstTBClaim.GetAllTBClaimWithDetailsAsync(mileageClaimSearch.UserID, mileageClaimSearch.FacilityID, mileageClaimSearch.StatusID, mileageClaimSearch.FromDate, mileageClaimSearch.ToDate);

            List<TBClaimVM> tBClaimVMs = new List<TBClaimVM>();

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

            foreach (var mc in mstTBClaimsWithDetails)
            {
                TBClaimVM tBClaimVM = new TBClaimVM();
                tBClaimVM.ApprovalStatus = mc.ApprovalStatus;
                if (mc.ApprovalStatus == 1)
                {
                    tBClaimVM.TelephoneExpenseName = "Awaiting Verification";

                }
                else if (mc.ApprovalStatus == 2)
                {
                    tBClaimVM.TelephoneExpenseName = "Awaiting Signatory approval";

                }
                else if (mc.ApprovalStatus == 3)
                {
                    tBClaimVM.TelephoneExpenseName = "Approved";

                }
                else if (mc.ApprovalStatus == 4)
                {
                    tBClaimVM.TelephoneExpenseName = "Request to Amend";
                }
                else if (mc.ApprovalStatus == 5)
                {
                    tBClaimVM.TelephoneExpenseName = "Voided";

                }
                else if (mc.ApprovalStatus == -5)
                {
                    tBClaimVM.TelephoneExpenseName = "Requested to Void";

                }
                else if (mc.ApprovalStatus == 6)
                {
                    tBClaimVM.TelephoneExpenseName = "Awaiting approval";

                }
                else if (mc.ApprovalStatus == 7)
                {
                    tBClaimVM.TelephoneExpenseName = "Awaiting HOD approval";

                }
                else if (mc.ApprovalStatus == 9)
                {
                    tBClaimVM.TelephoneExpenseName = "Exported to AccPac";

                }
                else if (mc.ApprovalStatus == 10)
                {
                    tBClaimVM.TelephoneExpenseName = "Exported to Bank";

                }
                else
                {
                    tBClaimVM.TelephoneExpenseName = "New";
                }

                if (mc.UserApprovers != "")
                {
                    tBClaimVM.Approver = mc.UserApprovers.Split(',').First();
                    if (tBClaimVM.Approver == HttpContext.User.FindFirst("userid").Value && (tBClaimVM.ApprovalStatus == 6))
                    {
                        tBClaimVM.IsActionAllowed = true;
                    }
                }
                else if (mc.Verifier != "")
                {
                    tBClaimVM.Approver = mc.Verifier.Split(',').First();
                    if (tBClaimVM.Approver == HttpContext.User.FindFirst("userid").Value && (tBClaimVM.ApprovalStatus == 1 || tBClaimVM.ApprovalStatus == 2))
                    {
                        tBClaimVM.IsActionAllowed = true;
                    }
                    //string VerifierIDs = string.Join(",", MileageverifierIDs.Skip(1));
                }
                else if (mc.HODApprover != "")
                {
                    tBClaimVM.Approver = mc.HODApprover.Split(',').First();
                    if (tBClaimVM.Approver == HttpContext.User.FindFirst("userid").Value && (tBClaimVM.ApprovalStatus == 7))
                    {
                        tBClaimVM.IsActionAllowed = true;
                    }
                }
                else if (mc.Approver != "")
                {
                    tBClaimVM.Approver = mc.Approver.Split(',').First();
                    if (tBClaimVM.Approver == HttpContext.User.FindFirst("userid").Value && (tBClaimVM.ApprovalStatus == 1 || tBClaimVM.ApprovalStatus == 2))
                    {
                        tBClaimVM.IsActionAllowed = true;
                    }
                }
                else
                {
                    tBClaimVM.Approver = "";
                }

                if (tBClaimVM.Approver != "")
                {
                    var mstUserApprover = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(tBClaimVM.Approver));
                    if (tBClaimVM.ApprovalStatus != 3 && tBClaimVM.ApprovalStatus != 4 && tBClaimVM.ApprovalStatus != -5 && tBClaimVM.ApprovalStatus != 5)
                        tBClaimVM.Approver = mstUserApprover.Name;
                    else
                        tBClaimVM.Approver = "";
                }

                dt.Rows.Add(tBClaimVM.TBCNo = mc.CNO,
                            tBClaimVM.Name = mc.Name,
                            tBClaimVM.CreatedDate = Convert.ToDateTime(mc.CreatedDate).ToString("dd/MM/yyyy", System.Globalization.CultureInfo.CreateSpecificCulture("en-US")),
                            tBClaimVM.FacilityName = mc.FacilityName,
                            tBClaimVM.Name = mc.Name,
                            tBClaimVM.Phone = mc.Phone,
                            tBClaimVM.GrandTotal = mc.GrandTotal,
                            tBClaimVM.Approver = tBClaimVM.Approver,
                            tBClaimVM.TelephoneExpenseName = tBClaimVM.TelephoneExpenseName);
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

            string filename = "TelephoneBillClaims" + DateTime.Now.ToString("ddMMyyyyss") + ".xlsx";
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
            long TBCID = Convert.ToInt64(id);
            TBClaimDetailVM tBClaimDetailVM = new TBClaimDetailVM();
            if (User != null && User.Identity.IsAuthenticated)
            {
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
                ViewBag.TBCID = id;
                tBClaimVM.TBCNo = mstTBClaim.TBCNo;
                tBClaimVM.VoucherNo = mstTBClaim.VoucherNo;
                tBClaimDetailVM.TBClaimVM = tBClaimVM;
            }
            return PartialView("GetTBDetailsPrint", tBClaimDetailVM);
        }
        public async Task<IActionResult> GetPrint(string data)
        {
            var mileageClaimSearch = JsonConvert.DeserializeObject<MileageClaimSearch>(data);
            var mstTBClaimsWithDetails = await _repository.MstTBClaim.GetAllTBClaimWithDetailsAsync(mileageClaimSearch.UserID, mileageClaimSearch.FacilityID, mileageClaimSearch.StatusID, mileageClaimSearch.FromDate, mileageClaimSearch.ToDate);

            List<TBClaimVM> tBClaimVMs = new List<TBClaimVM>();


            foreach (var mc in mstTBClaimsWithDetails)
            {
                TBClaimVM tBClaimVM = new TBClaimVM();
                tBClaimVM.TBCNo = mc.CNO;
                tBClaimVM.Name = mc.Name;
                tBClaimVM.CreatedDate = Convert.ToDateTime(mc.CreatedDate).ToString("dd/MM/yyyy", System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
                tBClaimVM.FacilityName = mc.FacilityName;
                tBClaimVM.Name = mc.Name;
                tBClaimVM.Phone = mc.Phone;
                tBClaimVM.GrandTotal = mc.GrandTotal;
                tBClaimVM.ApprovalStatus = mc.ApprovalStatus;

                if (mc.ApprovalStatus == 1)
                {
                    tBClaimVM.TelephoneExpenseName = "Awaiting Verification";

                }
                else if (mc.ApprovalStatus == 2)
                {
                    tBClaimVM.TelephoneExpenseName = "Awaiting Signatory approval";

                }
                else if (mc.ApprovalStatus == 3)
                {
                    tBClaimVM.TelephoneExpenseName = "Approved";

                }
                else if (mc.ApprovalStatus == 4)
                {
                    tBClaimVM.TelephoneExpenseName = "Request to Amend";
                }
                else if (mc.ApprovalStatus == 5)
                {
                    tBClaimVM.TelephoneExpenseName = "Voided";

                }
                else if (mc.ApprovalStatus == -5)
                {
                    tBClaimVM.TelephoneExpenseName = "Requested to Void";

                }
                else if (mc.ApprovalStatus == 6)
                {
                    tBClaimVM.TelephoneExpenseName = "Awaiting approval";

                }
                else if (mc.ApprovalStatus == 7)
                {
                    tBClaimVM.TelephoneExpenseName = "Awaiting HOD approval";

                }
                else if (mc.ApprovalStatus == 9)
                {
                    tBClaimVM.TelephoneExpenseName = "Exported to AccPac";

                }
                else if (mc.ApprovalStatus == 10)
                {
                    tBClaimVM.TelephoneExpenseName = "Exported to Bank";

                }
                else
                {
                    tBClaimVM.TelephoneExpenseName = "New";
                }


                if (mc.UserApprovers != "")
                {
                    tBClaimVM.Approver = mc.UserApprovers.Split(',').First();
                    if (tBClaimVM.Approver == HttpContext.User.FindFirst("userid").Value && (tBClaimVM.ApprovalStatus == 6))
                    {
                        tBClaimVM.IsActionAllowed = true;
                    }
                }
                else if (mc.Verifier != "")
                {
                    tBClaimVM.Approver = mc.Verifier.Split(',').First();
                    if (tBClaimVM.Approver == HttpContext.User.FindFirst("userid").Value && (tBClaimVM.ApprovalStatus == 1 || tBClaimVM.ApprovalStatus == 2))
                    {
                        tBClaimVM.IsActionAllowed = true;
                    }
                    //string VerifierIDs = string.Join(",", MileageverifierIDs.Skip(1));
                }
                else if (mc.HODApprover != "")
                {
                    tBClaimVM.Approver = mc.HODApprover.Split(',').First();
                    if (tBClaimVM.Approver == HttpContext.User.FindFirst("userid").Value && (tBClaimVM.ApprovalStatus == 7))
                    {
                        tBClaimVM.IsActionAllowed = true;
                    }
                }
                else if (mc.Approver != "")
                {
                    tBClaimVM.Approver = mc.Approver.Split(',').First();
                    if (tBClaimVM.Approver == HttpContext.User.FindFirst("userid").Value && (tBClaimVM.ApprovalStatus == 1 || tBClaimVM.ApprovalStatus == 2))
                    {
                        tBClaimVM.IsActionAllowed = true;
                    }
                }
                else
                {
                    tBClaimVM.Approver = "";
                }

                if (tBClaimVM.Approver != "")
                {
                    var mstUserApprover = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(tBClaimVM.Approver));
                    tBClaimVM.Approver = mstUserApprover.Name;
                }
                tBClaimVMs.Add(tBClaimVM);
            }
            return PartialView("GetTBPrint", tBClaimVMs);
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
        #endregion SendMessage

        #region -- GetMessages --

        public async Task<JsonResult> GetMessages(string id)
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

        #endregion GetMessages
    }
}
