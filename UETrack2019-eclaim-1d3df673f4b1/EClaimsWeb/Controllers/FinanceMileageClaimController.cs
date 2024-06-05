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
    public class FinanceMileageClaimController : Controller
    {
        private ILoggerManager _logger;
        private IRepositoryWrapper _repository;
        private IMapper _mapper;
        private IConfiguration _configuration;
        private readonly RepositoryContext _context;
        private AlternateApproverHelper _alternateApproverHelper;
        private ISendMailServices _sendMailServices;
        public FinanceMileageClaimController(ILoggerManager logger, IRepositoryWrapper repository, IMapper mapper, RepositoryContext context, IConfiguration configuration, ISendMailServices sendMailServices)
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

                //ViewData["FacilityID"] = new SelectList(await _repository.MstFacility.GetAllFacilityAsync("active"), "FacilityID", "FacilityName");
                //ViewData["UserID"] = new SelectList(await _repository.MstUser.GetAllUsersAsync("active"), "UserID", "Name");

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

                //var mstUsers = await _repository.MstUser.GetAllUsersAsync("active");
                var mstMileageClaimsWithDetails = await _repository.MstMileageClaim.GetAllMileageClaimWithDetailsAsync(userID, facilityID, statusID, fromDate, toDate);
                if (mstMileageClaimsWithDetails != null && mstMileageClaimsWithDetails.Any())
                {
                    mstMileageClaimsWithDetails.ToList().ForEach(c => c.IsDelegated = false);
                }

                if (delegatedUserId != null && delegatedUserId.HasValue)
                {
                    var delegatedClaims = await _repository.MstMileageClaim.GetAllMileageClaimWithDetailsAsync(delegatedUserId.Value, facilityID, statusID, fromDate, toDate);
                    if (delegatedClaims != null && delegatedClaims.Any())
                    {
                        delegatedClaims.ToList().ForEach(c => c.IsDelegated = true);
                        mstMileageClaimsWithDetails.ToList().AddRange(delegatedClaims.ToList());
                    }
                }

                List<MileageClaimVM> mileageClaimVMs = new List<MileageClaimVM>();
                foreach (var mc in mstMileageClaimsWithDetails)
                {
                    MileageClaimVM mileageClaimVM = new MileageClaimVM();
                    mileageClaimVM.MCID = mc.CID;
                    mileageClaimVM.MCNo = mc.CNO;
                    mileageClaimVM.Name = mc.Name;
                    mileageClaimVM.CreatedDate = Convert.ToDateTime(mc.CreatedDate).ToString("dd/MM/yyyy", System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
                    mileageClaimVM.FacilityName = mc.FacilityName;
                    mileageClaimVM.Phone = mc.Phone;
                    mileageClaimVM.GrandTotal = mc.GrandTotal;
                    mileageClaimVM.ApprovalStatus = mc.ApprovalStatus;
                    mileageClaimVM.VoucherNo = mc.VoucherNo;

                    if (mc.UserApprovers != "")
                    {
                        mileageClaimVM.Approver = mc.UserApprovers.Split(',').First();
                        if ((mileageClaimVM.Approver == HttpContext.User.FindFirst("userid").Value || (delegatedUserId.HasValue && mileageClaimVM.Approver == delegatedUserId.Value.ToString())) &&
                            (mileageClaimVM.ApprovalStatus == 6))
                        {
                            mileageClaimVM.IsActionAllowed = false;
                        }
                    }
                    else if (mc.Verifier != "")
                    {
                        mileageClaimVM.Approver = mc.Verifier.Split(',').First();
                        if ((mileageClaimVM.Approver == HttpContext.User.FindFirst("userid").Value || (delegatedUserId.HasValue && mileageClaimVM.Approver == delegatedUserId.Value.ToString())) &&
                            (mileageClaimVM.ApprovalStatus == 1 || mileageClaimVM.ApprovalStatus == 2))
                        {
                            mileageClaimVM.IsActionAllowed = true;
                        }

                        //string VerifierIDs = string.Join(",", MileageverifierIDs.Skip(1));
                    }
                    else if (mc.HODApprover != "")
                    {
                        mileageClaimVM.Approver = mc.HODApprover.Split(',').First();
                        if ((mileageClaimVM.Approver == HttpContext.User.FindFirst("userid").Value || (delegatedUserId.HasValue && mileageClaimVM.Approver == delegatedUserId.Value.ToString())) &&
                            (mileageClaimVM.ApprovalStatus == 7))
                        {
                            mileageClaimVM.IsActionAllowed = false;
                        }
                    }
                    else if (mc.Approver != "")
                    {
                        mileageClaimVM.Approver = mc.Approver.Split(',').First();
                        if ((mileageClaimVM.Approver == HttpContext.User.FindFirst("userid").Value || (delegatedUserId.HasValue && mileageClaimVM.Approver == delegatedUserId.Value.ToString())) &&
                            (mileageClaimVM.ApprovalStatus == 1 || mileageClaimVM.ApprovalStatus == 2))
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

                //var mstMileageCategoriesWithTypesResult = _mapper.Map<IEnumerable<MstMileageCategory>>(mstMileageCategoriesWithTypes);
                var mstMileageClaimVM = new MileageClaimSearchViewModel
                {
                    //Screens = new SelectList(await screenQuery.Distinct().ToListAsync()),
                    mileageClaimVMs = mileageClaimVMs,
                    Statuses = new SelectList(status, "Value", "Text"),
                    Facilities = new SelectList(facilities, "Value", "Text"),
                    Users = new SelectList(users, "Value", "Text"),
                    FromDate = fromDate,
                    ToDate = toDate
                };

                return View(mstMileageClaimVM);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Something went wrong inside GetAllMileageClaimWithDetailsAsync action: {ex.Message}");
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
            long MCID = Convert.ToInt64(id);

            if (User != null && User.Identity.IsAuthenticated)
            {

                var mstMileageClaim = await _repository.MstMileageClaim.GetMileageClaimByIdAsync(id);

                if (mstMileageClaim == null)
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
                    ////Need to change to not null
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
                //
                //_repository.MstMileageClaimAudit.GetMstMileageClaimAuditByIdAsync(id).Result.ToList();

                mileageClaimDetailVM.MileageClaimFileUploads = new List<DtMileageClaimFileUpload>();

                mileageClaimDetailVM.MileageClaimFileUploads = _repository.DtMileageClaimFileUpload.GetDtMileageClaimAuditByIdAsync(id).GetAwaiter().GetResult().ToList();

                MileageClaimVM mileageClaimVM = new MileageClaimVM();
                mileageClaimVM.VoucherNo = mstMileageClaim.VoucherNo;
                mileageClaimVM.TravelMode = mstMileageClaim.TravelMode;
                mileageClaimVM.GrandTotal = mstMileageClaim.GrandTotal;
                mileageClaimVM.TotalKm = mstMileageClaim.TotalKm;
                mileageClaimVM.Company = mstMileageClaim.Company;
                mileageClaimVM.Name = mstMileageClaim.MstUser.Name;
                mileageClaimVM.DepartmentName = mstMileageClaim.MstDepartment.Department;
                mileageClaimVM.FacilityName = mstMileageClaim.MstFacility.FacilityName;
                mileageClaimVM.CreatedDate = Convert.ToDateTime(mstMileageClaim.CreatedDate).ToString("dd/MM/yyyy", System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
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
                if (mstMileageClaim.Verifier != "")
                {
                    string[] verifierIDs = mstMileageClaim.Verifier.Split(',');
                    TempData["QueryMCVerifierIDs"] = string.Join(",", verifierIDs);
                    foreach (string verifierID in verifierIDs)
                    {
                        if ((verifierID != "" && verifierID == HttpContext.User.FindFirst("userid").Value || (delegatedUserId.HasValue && verifierID == delegatedUserId.Value.ToString())) && User.IsInRole("Finance"))
                        {
                            TempData["ApprovedStatus"] = mstMileageClaim.ApprovalStatus;
                            //Session["ApprovedStatus"] = oSupplierPO.Approvalstatus;
                            TempData["VerifierIDs"] = string.Join(",", verifierIDs.Skip(1));
                            mileageClaimVM.IsActionAllowed = true;
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
                    TempData["VerifierIDs"] = mstMileageClaim.Verifier;
                    TempData["ApproverIDs"] = mstMileageClaim.Approver;
                }

                if (mstMileageClaim.HODApprover != "" && mstMileageClaim.Verifier == "")
                {
                    string[] hodApproverIDs = mstMileageClaim.HODApprover.Split(',');
                    TempData["QueryMCHODApproverIDs"] = string.Join(",", hodApproverIDs);
                    foreach (string approverID in hodApproverIDs)
                    {
                        if (approverID != "" && approverID == HttpContext.User.FindFirst("userid").Value && User.IsInRole("Finance"))
                        {
                            TempData["ApprovedStatus"] = mstMileageClaim.ApprovalStatus;
                            //Session["ApprovedStatus"] = oSupplierPO.Approvalstatus;
                            TempData["HODApproverIDs"] = string.Join(",", hodApproverIDs.Skip(1));
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


                //Approval Process code
                if (mstMileageClaim.Approver != "" && mstMileageClaim.Verifier == "")
                {
                    string[] approverIDs = mstMileageClaim.Approver.Split(',');
                    TempData["QueryMCApproverIDs"] = string.Join(",", approverIDs);
                    foreach (string approverID in approverIDs)
                    {
                        if ((approverID != "" && approverID == HttpContext.User.FindFirst("userid").Value || (delegatedUserId.HasValue && approverID == delegatedUserId.Value.ToString())) && User.IsInRole("Finance"))
                        {
                            TempData["ApprovedStatus"] = mstMileageClaim.ApprovalStatus;
                            //Session["ApprovedStatus"] = oSupplierPO.Approvalstatus;
                            TempData["ApproverIDs"] = string.Join(",", approverIDs.Skip(1));
                            mileageClaimVM.IsActionAllowed = true;
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

        public async Task<JsonResult> GetTextValuesSGSummary(string id)
        {
            List<DtMileageClaimSummary> oDtClaimsSummaryList = new List<DtMileageClaimSummary>();

            try
            {
                var dtMileageClaimSummaries = await _repository.DtMileageClaimSummary.GetDtMileageClaimSummaryByIdAsync(Convert.ToInt64(id));

                return Json(new { DtClaimsList = dtMileageClaimSummaries });
            }
            catch
            {
                return Json(new { DtClaimsList = oDtClaimsSummaryList });
            }

        }

        [HttpPost]
        public async Task<JsonResult> SaveSummary(string data)
        {
            var mileageClaimViewModel = JsonConvert.DeserializeObject<DtMileageClaimSummaryVM>(data);
            var mileageCSummary = await _repository.DtMileageClaimSummary.GetDtMileageClaimSummaryByIdAsync(mileageClaimViewModel.MCID);
            foreach (var hr in mileageCSummary)
            {
                _repository.DtMileageClaimSummary.Delete(hr);
            }

            foreach (var dtItem in mileageClaimViewModel.dtClaims)
            {
                if (dtItem.ExpenseCategory != "DBS")
                {
                    dtItem.Description = dtItem.Description.ToUpper();
                    var mstFacility1 = await _repository.MstFacility.GetFacilityWithDepartmentByIdAsync(Convert.ToInt32(dtItem.FacilityID));
                    var mstExpenseCategory = await _repository.MstExpenseCategory.ExpenseCategoriesByClaimType("Mileage");

                    dtItem.AccountCode = mstExpenseCategory.ExpenseCode + "-" + mstFacility1.MstDepartment.Code + "-" + mstFacility1.Code + mstExpenseCategory.Default;
                }
            }
            MstMileageClaimAudit auditUpdate = new MstMileageClaimAudit();
            auditUpdate.MCID = mileageClaimViewModel.MCID;
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
            var res = await _repository.MstMileageClaim.SaveSummary(mileageClaimViewModel.MCID, mileageClaimViewModel.dtClaims, auditUpdate);

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
                int MCID = Convert.ToInt32(id);

                var mstMileageClaim = await _repository.MstMileageClaim.GetMileageClaimByIdAsync(MCID);

                if (mstMileageClaim == null)
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

        public async Task<JsonResult> UpdateRejectedStatus(string id, string reason)
        {
            if (User != null && User.Identity.IsAuthenticated)
            {
                int MCID = Convert.ToInt32(id);

                var mstMileageClaim = await _repository.MstMileageClaim.GetMileageClaimByIdAsync(MCID);

                if (mstMileageClaim == null)
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

                BackgroundJob.Enqueue(() => _sendMailServices.SendEmail("Rejected.html", screen, subject, senderName, receiverName, claimNo, approvalType, userID, toEmail, clickUrl,lastApprover,nextApprover,rejectReason));

                return Json(new { res = "Done" });
            }
            else
            {
                return Json(new { res = "Done" });
            }
        }

        public async Task<JsonResult> UpdateStatus(string id)
        {
            if (User != null && User.Identity.IsAuthenticated)
            {
                int MCID = Convert.ToInt32(id);

                var mstMileageClaim = await _repository.MstMileageClaim.GetMileageClaimByIdAsync(MCID);

                if (mstMileageClaim == null)
                {
                    // return NotFound();
                }

                bool isAlternateApprover = false;
                int ApprovedStatus = Convert.ToInt32(mstMileageClaim.ApprovalStatus);
                bool excute = _repository.MstMileageClaim.ExistsApproval(MCID.ToString(), ApprovedStatus, HttpContext.User.FindFirst("userid").Value, "Mileage");

                // If execute is false, Check if the current user is alternate user for this claim
                if (excute == false)
                {
                    string hodapprover = _repository.MstMileageClaim.GetApproval(MCID.ToString(), ApprovedStatus, HttpContext.User.FindFirst("userid").Value, "Mileage");
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
                    #region Mileage User Approvers
                    if (ApprovedStatus == 6)
                    {
                        string VerifierIDs = "";
                        string ApproverIDs = "";
                        string UserApproverIDs = "";
                        string HODApproverID = "";
                        try
                        {
                            string[] MileageuserapproverIDs = mstMileageClaim.UserApprovers.Split(',');
                            UserApproverIDs = string.Join(",", MileageuserapproverIDs.Skip(1));
                            string[] userApproverIDs = UserApproverIDs.ToString().Split(',');
                            ApproverIDs = mstMileageClaim.Approver;
                            VerifierIDs = mstMileageClaim.Verifier;
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
                            HODApproverID = mstMileageClaim.HODApprover;

                            //Mail Code Implementation for Verifiers
                            foreach (string verifierID in verifierIDs)
                            {
                                if (verifierID != "")
                                {
                                    string domainUrl = HttpContext.Request.Scheme + "://" + HttpContext.Request.Host;
                                    string clickUrl = domainUrl + "/" + "FinanceMileageClaim/Details/" + MCID;

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
                                    var claimNo = mstMileageClaim.MCNo;
                                    var screen = "Mileage Claim";
                                    var approvalType = "Verification Request";
                                    int userID = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value);
                                    var subject = "Mileage Claim for Verification " + claimNo;

                                    BackgroundJob.Enqueue(() => _sendMailServices.SendEmail("EmailTemplate.html", screen, subject, senderName, receiverName, claimNo, approvalType, userID, toEmail, clickUrl));
                                }
                                else if (HODApproverID != "")
                                {
                                    string domainUrl = HttpContext.Request.Scheme + "://" + HttpContext.Request.Host;
                                    string clickUrl = domainUrl + "/" + "HodSummary/Details/" + MCID;

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
                                    var claimNo = mstMileageClaim.MCNo;
                                    var screen = "Mileage Claim";
                                    var approvalType = "Approval Request";
                                    int userID = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value);
                                    var subject = "Mileage Claim for Approval " + claimNo;

                                    BackgroundJob.Enqueue(() => _sendMailServices.SendEmail("EmailTemplate.html", screen, subject, senderName, receiverName, claimNo, approvalType, userID, toEmail, clickUrl));
                                }
                                break;
                            }
                        }
                        catch
                        {
                        }
                        await _repository.MstMileageClaim.UpdateMstMileageClaimStatus(MCID, 7, int.Parse(HttpContext.User.FindFirst("userid").Value), DateTime.Now, string.Empty, VerifierIDs.ToString(), ApproverIDs.ToString(), UserApproverIDs.ToString(), HODApproverID.ToString(), isAlternateApprover, 0);

                    }
                    #endregion


                    #region Mileage Approver
                    else if (ApprovedStatus == 2)
                    {
                        string VerifierIDs = "";
                        string ApproverIDs = "";
                        string UserApproverIDs = "";
                        string HODApproverID = "";
                        string DVerifierIDs = "";
                        try
                        {
                            string[] MileageapproverIDs = mstMileageClaim.Approver.Split(',');
                            ApproverIDs = string.Join(",", MileageapproverIDs.Skip(1));
                            string[] approverIDs = ApproverIDs.Split(',');
                            int CreatedBy = Convert.ToInt32(mstMileageClaim.CreatedBy);
                            DVerifierIDs = mstMileageClaim.DVerifier.Split(',').First();

                            //Mail Code Implementation for Approvers
                            foreach (string approverID in approverIDs)
                            {
                                if (approverID != "")
                                {
                                    string domainUrl = HttpContext.Request.Scheme + "://" + HttpContext.Request.Host;
                                    string clickUrl = domainUrl + "/" + "FinanceMileageClaim/Details/" + MCID;

                                    var mstSenderDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(HttpContext.User.FindFirst("userid").Value));
                                    var senderName = mstSenderDetails.Name;
                                    var mstVerifierDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(approverID));
                                    var toEmail = mstVerifierDetails.EmailAddress;
                                    var receiverName = mstVerifierDetails.Name;
                                    var claimNo = mstMileageClaim.MCNo;
                                    var screen = "Mileage Claim";
                                    var approvalType = "Approval Request";
                                    int userID = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value);
                                    var subject = "Mileage Claim for Approval " + claimNo;

                                    BackgroundJob.Enqueue(() => _sendMailServices.SendEmail("EmailTemplate.html", screen, subject, senderName, receiverName, claimNo, approvalType, userID, toEmail, clickUrl));

                                }

                                break;
                            }
                        }
                        catch
                        {
                        }
                        string financeStartDay = _configuration.GetValue<string>("FinanceStartDay");
                        await _repository.MstMileageClaim.UpdateMstMileageClaimStatus(MCID, 3, int.Parse(HttpContext.User.FindFirst("userid").Value), DateTime.Now, string.Empty, VerifierIDs, ApproverIDs, UserApproverIDs.ToString(), HODApproverID.ToString(), isAlternateApprover, int.Parse(financeStartDay));
                        if (ApproverIDs == string.Empty)
                        {
                            string domainUrl = HttpContext.Request.Scheme + "://" + HttpContext.Request.Host;
                            string clickUrl = domainUrl + "/" + "FinanceReports";

                            var mstSenderDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(HttpContext.User.FindFirst("userid").Value));
                            var senderName = mstSenderDetails.Name;
                            var mstVerifierDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(DVerifierIDs));
                            var toEmail = mstVerifierDetails.EmailAddress;
                            var receiverName = mstVerifierDetails.Name;
                            var claimNo = mstMileageClaim.MCNo;
                            var screen = "Mileage Claim";
                            var approvalType = "Export to AccPac/Bank Request";
                            int userID = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value);
                            var subject = "Mileage Claim for Export to AccPac/Bank " + claimNo;

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

        public async Task<IActionResult> Download(string id, string name)
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
                        return File(blobStream, file.Properties.ContentType, "MileageClaim-Export.xlsx");
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
                    CloudBlob file = container.GetBlobReference("FileUploads/MileageClaimFiles/" + id);

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
            var mstMileageClaimsWithDetails = await _repository.MstMileageClaim.GetAllMileageClaimWithDetailsAsync(mileageClaimSearch.UserID, mileageClaimSearch.FacilityID, mileageClaimSearch.StatusID, mileageClaimSearch.FromDate, mileageClaimSearch.ToDate);
            List<MileageClaimVM> mileageClaimVMs = new List<MileageClaimVM>();

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
                MileageClaimVM mileageClaimVM = new MileageClaimVM();
                mileageClaimVM.ApprovalStatus = mc.ApprovalStatus;




                if (mc.ApprovalStatus == 1)
                {
                    mileageClaimVM.ApprovalStatusName = "Awaiting Verification";

                }
                else if (mc.ApprovalStatus == 2)
                {
                    mileageClaimVM.ApprovalStatusName = "Awaiting Signatory approval";

                }
                else if (mc.ApprovalStatus == 3)
                {
                    mileageClaimVM.ApprovalStatusName = "Approved";

                }
                else if (mc.ApprovalStatus == 4)
                {
                    mileageClaimVM.ApprovalStatusName = "Request to Amend";
                }
                else if (mc.ApprovalStatus == 5)
                {
                    mileageClaimVM.ApprovalStatusName = "Voided";

                }
                else if (mc.ApprovalStatus == -5)
                {
                    mileageClaimVM.ApprovalStatusName = "Requested to Void";

                }
                else if (mc.ApprovalStatus == 6)
                {
                    mileageClaimVM.ApprovalStatusName = "Awaiting approval";

                }
                else if (mc.ApprovalStatus == 7)
                {
                    mileageClaimVM.ApprovalStatusName = "Awaiting HOD approval";

                }
                else if (mc.ApprovalStatus == 9)
                {
                    mileageClaimVM.ApprovalStatusName = "Exported to AccPac";

                }
                else if (mc.ApprovalStatus == 10)
                {
                    mileageClaimVM.ApprovalStatusName = "Exported to Bank";

                }
                else
                {
                    mileageClaimVM.ApprovalStatusName = "New";
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

                dt.Rows.Add(mileageClaimVM.MCNo = mc.CNO,
                            mileageClaimVM.Name = mc.Name,
                            mileageClaimVM.CreatedDate = Convert.ToDateTime(mc.CreatedDate).ToString("dd/MM/yyyy", System.Globalization.CultureInfo.CreateSpecificCulture("en-US")),
                            mileageClaimVM.FacilityName = mc.FacilityName,
                            mileageClaimVM.Name = mc.Name,
                            mileageClaimVM.Phone = mc.Phone,
                            mileageClaimVM.GrandTotal = mc.GrandTotal,
                            mileageClaimVM.Approver = mileageClaimVM.Approver,
                            mileageClaimVM.ApprovalStatusName = mileageClaimVM.ApprovalStatusName);
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

            string filename = "MileageClaim-Export" + DateTime.Now.ToString("ddMMyyyyss") + ".xlsx";
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
            long MCID = Convert.ToInt64(id);
            MileageClaimDetailVM mileageClaimDetailVM = new MileageClaimDetailVM();
            if (User != null && User.Identity.IsAuthenticated)
            {

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
                //
                //_repository.MstMileageClaimAudit.GetMstMileageClaimAuditByIdAsync(id).Result.ToList();

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
                mileageClaimVM.CreatedDate = Convert.ToDateTime(mstMileageClaim.CreatedDate).ToString("dd/MM/yyyy", System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
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
        public async Task<IActionResult> GetPrint(string data)
        {
            var mileageClaimSearch = JsonConvert.DeserializeObject<MileageClaimSearch>(data);
            var mstMileageClaimsWithDetails = await _repository.MstMileageClaim.GetAllMileageClaimWithDetailsAsync(mileageClaimSearch.UserID, mileageClaimSearch.FacilityID, mileageClaimSearch.StatusID, mileageClaimSearch.FromDate, mileageClaimSearch.ToDate);
            List<MileageClaimVM> mileageClaimVMs = new List<MileageClaimVM>();

            //DataTable dt = new DataTable("Grid");
            //dt.Columns.AddRange(new DataColumn[9] { new DataColumn("Claim"),
            //                            new DataColumn("Requester"),
            //                            new DataColumn("Date Created"),
            //                            new DataColumn("Facility"),
            //                            new DataColumn("Payee"),
            //                            new DataColumn("Contact Number"),
            //                            new DataColumn("Total Claim"),
            //                            new DataColumn("Approver"),
            //                            new DataColumn("Status")});


            foreach (var mc in mstMileageClaimsWithDetails)
            {
                MileageClaimVM mileageClaimVM = new MileageClaimVM();

                mileageClaimVM.MCNo = mc.CNO;
                mileageClaimVM.Name = mc.Name;
                mileageClaimVM.CreatedDate = Convert.ToDateTime(mc.CreatedDate).ToString("dd/MM/yyyy", System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
                mileageClaimVM.FacilityName = mc.FacilityName;
                mileageClaimVM.Name = mc.Name;
                mileageClaimVM.Phone = mc.Phone;
                mileageClaimVM.GrandTotal = mc.GrandTotal;
                mileageClaimVM.ApprovalStatus = mc.ApprovalStatus;

                if (mc.ApprovalStatus == 1)
                {
                    mileageClaimVM.ApprovalStatusName = "Awaiting Verification";

                }
                else if (mc.ApprovalStatus == 2)
                {
                    mileageClaimVM.ApprovalStatusName = "Awaiting Signatory approval";

                }
                else if (mc.ApprovalStatus == 3)
                {
                    mileageClaimVM.ApprovalStatusName = "Approved";

                }
                else if (mc.ApprovalStatus == 4)
                {
                    mileageClaimVM.ApprovalStatusName = "Request to Amend";
                }
                else if (mc.ApprovalStatus == 5)
                {
                    mileageClaimVM.ApprovalStatusName = "Voided";

                }
                else if (mc.ApprovalStatus == -5)
                {
                    mileageClaimVM.ApprovalStatusName = "Requested to Void";

                }
                else if (mc.ApprovalStatus == 6)
                {
                    mileageClaimVM.ApprovalStatusName = "Awaiting approval";

                }
                else if (mc.ApprovalStatus == 7)
                {
                    mileageClaimVM.ApprovalStatusName = "Awaiting HOD approval";

                }
                else if (mc.ApprovalStatus == 9)
                {
                    mileageClaimVM.ApprovalStatusName = "Exported to AccPac";

                }
                else if (mc.ApprovalStatus == 10)
                {
                    mileageClaimVM.ApprovalStatusName = "Exported to Bank";

                }
                else
                {
                    mileageClaimVM.ApprovalStatusName = "New";
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
            return PartialView("GetMileagePrint", mileageClaimVMs);
        }

        #region -- SendMessage --
        public async Task<JsonResult> AddMessage(string data)
        {
            //string Message=""; string recieverId = ""; string Mcid = "";
            //int[] UserIds = recieverId; //recieverId.Split(',').Select(int.Parse).ToArray();
            var queryParamViewModel = JsonConvert.DeserializeObject<QueryParam>(data);

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

        #endregion GetMessages
    }
}
