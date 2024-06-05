using AutoMapper;
using EClaimsEntities;
using EClaimsEntities.Models;
using EClaimsRepository.Contracts;
using EClaimsWeb.Helpers;
using EClaimsWeb.Models;
using Hangfire;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using NToastNotify;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System.IO;

namespace EClaimsWeb.Controllers
{
    public class PendingApprovalsController : Controller
    {
        private ILoggerManager _logger;
        private IRepositoryWrapper _repository;
        private readonly IToastNotification _toastNotification;
        private AlternateApproverHelper _alternateApproverHelper;
        private IConfiguration _configuration;
        private IMapper _mapper;
        private readonly RepositoryContext _context;
        private ISendMailServices _sendMailServices;
        public PendingApprovalsController(ILoggerManager logger, IRepositoryWrapper repository, IMapper mapper, RepositoryContext context, IConfiguration configuration, ISendMailServices sendMailServices)
        {
            _logger = logger;
            _repository = repository;
            _mapper = mapper;
            _context = context;
            _configuration = configuration;
            _sendMailServices = sendMailServices;
            _alternateApproverHelper = new AlternateApproverHelper(logger, repository, context);
        }
        public async Task<IActionResult> Index(string moduleName, int facilityID, int statusID, string fromDate, string toDate,string membershipRadios)
        {
            try
            {
                List<clsModule> oclsModule = new List<clsModule>();
                //oclsModule.Add(new clsModule() { ModuleName = "Admin Settings", ModuleId = "Admin Settings" });
                oclsModule.Add(new clsModule() { ModuleName = "Mileage", ModuleId = "MileageClaim" });
                oclsModule.Add(new clsModule() { ModuleName = "Expense", ModuleId = "ExpenseClaim" });
                oclsModule.Add(new clsModule() { ModuleName = "TelephoneBill", ModuleId = "TelephoneBillClaim" });
                oclsModule.Add(new clsModule() { ModuleName = "PV-Cheque", ModuleId = "PV-ChequeClaim" });
                oclsModule.Add(new clsModule() { ModuleName = "HR PV-Cheque", ModuleId = "HRPV-ChequeClaim" });
                oclsModule.Add(new clsModule() { ModuleName = "PV-Giro", ModuleId = "PV-GiroClaim" });
                oclsModule.Add(new clsModule() { ModuleName = "HRPV-Giro", ModuleId = "HRPV-GiroClaim" });

                List<SelectListItem> reports = (from t in oclsModule
                                                select new SelectListItem
                                                {
                                                    Text = t.ModuleName.ToString(),
                                                    Value = t.ModuleId.ToString(),
                                                }).ToList();
                var selectedItem = reports.Find(p => p.Value == membershipRadios);
                if(selectedItem != null)
                {
                    selectedItem.Selected = true;
                }

                if (string.IsNullOrEmpty(moduleName))
                {
                    moduleName = "ExpenseClaim";
                    membershipRadios = "ExpenseClaim";
                }
                else
                {
                    membershipRadios = moduleName;
                }

                ViewBag.ModuleName = membershipRadios;
                //ViewData["FacilityID"] = new SelectList(await _repository.MstFacility.GetAllFacilityAsync("active"), "FacilityID", "FacilityName");
                ViewData["UserID"] = new SelectList(await _repository.MstUser.GetAllUsersAsync("active"), "UserID", "Name");

                List<CustomClaim> customClaimVMs = new List<CustomClaim>();

                var mstUsers = await _repository.MstUser.GetAllUsersAsync("active");

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

                if (!isAlternateApproverSet)
                {
                    var mstPendingApprovalClaimsWithDetails = await _repository.MstUser.GetAllPendingApprovalClaimsAsync(Int32.Parse(HttpContext.User.FindFirst("userid").Value), membershipRadios);
                    if (mstPendingApprovalClaimsWithDetails != null && mstPendingApprovalClaimsWithDetails.Any())
                    {
                        mstPendingApprovalClaimsWithDetails.ToList().ForEach(c => c.IsDelegated = false);
                    }

                    if (delegatedUserId != null && delegatedUserId.HasValue)
                    {
                        var delegatedClaims = await _repository.MstUser.GetAllPendingApprovalClaimsAsync(delegatedUserId.Value, membershipRadios);
                        if (delegatedClaims != null && delegatedClaims.Any())
                        {
                            delegatedClaims.ToList().ForEach(c => c.IsDelegated = true);
                            delegatedClaims.ForEach(pendingApprovals => mstPendingApprovalClaimsWithDetails.Add(pendingApprovals));
                            // mstPendingApprovalClaimsWithDetails.ToList().Concat(delegatedClaims.ToList());
                        }
                    }

                    //var mstMileageClaimsWithDetails = await _repository.MstMileageClaim.GetAllMileageClaimForExportToBankAsync("", moduleName, facilityID, statusID, fromDate, toDate);
                    foreach (var mc in mstPendingApprovalClaimsWithDetails)
                    {
                        CustomClaim mileageClaimVM = new CustomClaim();
                        mileageClaimVM.PVGCItemID = mc.PVGCItemID;
                        mileageClaimVM.CID = mc.CID;
                        mileageClaimVM.CNO = mc.CNO;
                        mileageClaimVM.Name = mc.Name;
                        mileageClaimVM.CreatedDate = Convert.ToDateTime(mc.CreatedDate).ToString("dd/MM/yyyy", System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
                        mileageClaimVM.ExportAccPacDate = Convert.ToDateTime(mc.ExportAccPacDate).ToString("dd/MM/yyyy", System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
                        mileageClaimVM.ExportBankDate = Convert.ToDateTime(mc.ExportBankDate).ToString("dd/MM/yyyy", System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
                        mileageClaimVM.FacilityName = mc.FacilityName;
                        mileageClaimVM.Phone = mc.Phone;
                        mileageClaimVM.GrandTotal = mc.GrandTotal;
                        mileageClaimVM.TotalAmount = mc.TotalAmount;
                        mileageClaimVM.ApprovalStatus = mc.ApprovalStatus;
                        mileageClaimVM.PayeeName = mc.PayeeName;
                        mileageClaimVM.VoucherNo = mc.VoucherNo;
                        mileageClaimVM.ParticularsOfPayment = mc.ParticularsOfPayment;

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
                            //string VerifierIDs = string.Join(",", ExpenseverifierIDs.Skip(1));
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

                        customClaimVMs.Add(mileageClaimVM);
                    }
                }
                _logger.LogInfo($"Returned all Mileage Claims with details from database.");

                var mstMileageClaimVM = new APReportViewModel
                {
                    ModuleName = membershipRadios,
                    customClaimVMs = customClaimVMs,
                    ReportTypes = new SelectList(reports, "Value", "Text")
                    
                };

                return View(mstMileageClaimVM);

                //var mstMileageCategoriesWithTypesResult = _mapper.Map<IEnumerable<MstMileageCategory>>(mstMileageCategoriesWithTypes);
                //return View(mileageClaimVMs);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Something went wrong inside GetAllMileageClaimWithDetailsAsync action: {ex.Message}");
                return View();
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

                    TempData["ApprovedStatus"] = mstMileageClaim.ApprovalStatus;
                    TempData["FinalApproverID"] = mstMileageClaim.FinalApprover;
                    ViewBag.VoidReason = mstMileageClaim.VoidReason == null ? "" : mstMileageClaim.VoidReason;

                    if (TempData["ApprovedStatus"].ToString() == "1" || TempData["ApprovedStatus"].ToString() == "2" || TempData["ApprovedStatus"].ToString() == "3" || TempData["ApprovedStatus"].ToString() == "-5" || TempData["ApprovedStatus"].ToString() == "6" || TempData["ApprovedStatus"].ToString() == "7" || TempData["ApprovedStatus"].ToString() == "9" || TempData["ApprovedStatus"].ToString() == "10")
                    {
                        ViewBag.ShowVoidBtn = 1;

                        if (mstUserDetails.IsActive && (mstUserDetails.IsHOD || (User.FindFirst("issuperior") is null ? false : User.FindFirst("issuperior").Value.ToString().ToLower() == "true")) || User.IsInRole("Finance"))
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
                            if ((verifierID != "" && verifierID == HttpContext.User.FindFirst("userid").Value || (delegatedUserId.HasValue && verifierID == delegatedUserId.Value.ToString())) && mstUserDetails.IsActive )
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
                            if ((approverID != "" && approverID == HttpContext.User.FindFirst("userid").Value || (delegatedUserId.HasValue && approverID == delegatedUserId.Value.ToString())) && mstUserDetails.IsActive)
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
                return RedirectToAction("ECDetails", "PendingApprovals", new { id = id });
            }
            else if (cno.ToLower().Contains("tb"))
            {
                return RedirectToAction("TBCDetails", "PendingApprovals", new { id = id });
            }
            else if (cno.ToLower().StartsWith("pvg"))
            {
                return RedirectToAction("PVGCDetails", "PendingApprovals", new { id = id });
            }
            else if (cno.ToLower().StartsWith("pvc"))
            {
                return RedirectToAction("PVCCDetails", "PendingApprovals", new { id = id });
            }
            else if (cno.ToLower().StartsWith("hpvc"))
            {
                return RedirectToAction("HRPVCCDetails", "PendingApprovals", new { id = id });
            }
            else if (cno.ToLower().StartsWith("hpvg"))
            {
                return RedirectToAction("HRPVGCDetails", "PendingApprovals", new { id = id });
            }
            else
            {
                return RedirectToAction("TBCDetails", "PendingApprovals", new { id = id });
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

                TempData["ApprovedStatus"] = mstExpenseClaim.ApprovalStatus;
                TempData["FinalApproverID"] = mstExpenseClaim.FinalApprover;
                ViewBag.VoidReason = mstExpenseClaim.VoidReason == null ? "" : mstExpenseClaim.VoidReason;

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
                        if ((verifierID != "" && verifierID == HttpContext.User.FindFirst("userid").Value || (delegatedUserId.HasValue && verifierID == delegatedUserId.Value.ToString())) && User.IsInRole("Finance"))
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
                        if ((approverID != "" && approverID == HttpContext.User.FindFirst("userid").Value || (delegatedUserId.HasValue && approverID == delegatedUserId.Value.ToString())) && User.IsInRole("Finance"))
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

                TempData["ApprovedStatus"] = mstTBClaim.ApprovalStatus;
                TempData["FinalApproverID"] = mstTBClaim.FinalApprover;
                ViewBag.VoidReason = mstTBClaim.VoidReason == null ? "" : mstTBClaim.VoidReason;
                if (TempData["ApprovedStatus"].ToString() == "1" || TempData["ApprovedStatus"].ToString() == "2" || TempData["ApprovedStatus"].ToString() == "3" || TempData["ApprovedStatus"].ToString() == "-5" || TempData["ApprovedStatus"].ToString() == "6" || TempData["ApprovedStatus"].ToString() == "7" || TempData["ApprovedStatus"].ToString() == "9" || TempData["ApprovedStatus"].ToString() == "10")
                {
                    ViewBag.ShowVoidBtn = 1;

                    if (mstUserDetails.IsActive && (mstUserDetails.IsHOD || (User.FindFirst("issuperior") is null ? false : User.FindFirst("issuperior").Value.ToString().ToLower() == "true")) || User.IsInRole("Finance"))
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
                        if ((verifierID != "" && verifierID == HttpContext.User.FindFirst("userid").Value || (delegatedUserId.HasValue && verifierID == delegatedUserId.Value.ToString())) && mstUserDetails.IsActive)
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
                        if ((approverID != "" && approverID == HttpContext.User.FindFirst("userid").Value || (delegatedUserId.HasValue && approverID == delegatedUserId.Value.ToString())) && mstUserDetails.IsActive)
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

                TempData["ApprovedStatus"] = mstPVGClaim.ApprovalStatus;
                TempData["FinalApproverID"] = mstPVGClaim.FinalApprover;
                ViewBag.VoidReason = mstPVGClaim.VoidReason == null ? "" : mstPVGClaim.VoidReason;

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
                        if ((verifierID != "" && verifierID == HttpContext.User.FindFirst("userid").Value || (delegatedUserId.HasValue && verifierID == delegatedUserId.Value.ToString())) && mstUserDetails.IsActive)
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
                        if ((approverID != "" && approverID == HttpContext.User.FindFirst("userid").Value || (delegatedUserId.HasValue && approverID == delegatedUserId.Value.ToString())) && mstUserDetails.IsActive)
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

                TempData["ApprovedStatus"] = mstPVCClaim.ApprovalStatus;
                TempData["FinalApproverID"] = mstPVCClaim.FinalApprover;
                ViewBag.VoidReason = mstPVCClaim.VoidReason == null ? "" : mstPVCClaim.VoidReason;

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
                        if ((verifierID != "" && verifierID == HttpContext.User.FindFirst("userid").Value || (delegatedUserId.HasValue && verifierID == delegatedUserId.Value.ToString())) && mstUserDetails.IsActive)
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
                        if ((approverID != "" && approverID == HttpContext.User.FindFirst("userid").Value || (delegatedUserId.HasValue && approverID == delegatedUserId.Value.ToString())) && mstUserDetails.IsActive)
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

                TempData["ApprovedStatus"] = mstHRPVGClaim.ApprovalStatus;
                TempData["FinalApproverID"] = mstHRPVGClaim.FinalApprover;
                ViewBag.VoidReason = mstHRPVGClaim.VoidReason == null ? "" : mstHRPVGClaim.VoidReason;

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
                        if ((verifierID != "" && verifierID == HttpContext.User.FindFirst("userid").Value || (delegatedUserId.HasValue && verifierID == delegatedUserId.Value.ToString())) || (mstUserDetails.IsHOD || (User.FindFirst("issuperior") is null ? false : User.FindFirst("issuperior").Value.ToString().ToLower() == "true")))
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
                        if ((approverID != "" && approverID == HttpContext.User.FindFirst("userid").Value || (delegatedUserId.HasValue && approverID == delegatedUserId.Value.ToString())) && mstUserDetails.IsActive || (mstUserDetails.IsHOD || (User.FindFirst("issuperior") is null ? false : User.FindFirst("issuperior").Value.ToString().ToLower() == "true")))
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

        public async Task<IActionResult> HRPVCCDetails(long? id, string cno)
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
                            if ((verifierID != "" && verifierID == HttpContext.User.FindFirst("userid").Value || (delegatedUserId.HasValue && verifierID == delegatedUserId.Value.ToString())))
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
                            if ((approverID != "" && approverID == HttpContext.User.FindFirst("userid").Value || (delegatedUserId.HasValue && approverID == delegatedUserId.Value.ToString())))
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
        public async Task<JsonResult> UpdateStatus(string data)
        {
            var aPExportSearch = JsonConvert.DeserializeObject<APExportSearch>(data);

            if (aPExportSearch != null)
            {
                if (aPExportSearch.ModuleName == "MileageClaim")
                {
                    if (!string.IsNullOrEmpty(aPExportSearch.ClaimIds))
                    {
                        int[] CIDs = aPExportSearch.ClaimIds.Split(',').Select(int.Parse).ToArray();
                        for (int i = 0; i < CIDs.Length; i++)
                        {
                            if (User != null && User.Identity.IsAuthenticated)
                            {
                                int MCID = Convert.ToInt32(CIDs[i]);

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
                                    if (!string.IsNullOrEmpty(hodapprover) && delegatedUserId != null)
                                    {
                                        if ((hodapprover.Contains(delegatedUserId.Value.ToString())))
                                        {
                                            excute = true;
                                            isAlternateApprover = true;
                                        }
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

                                                    BackgroundJob.Enqueue(() => _sendMailServices.SendEmail("EmailTemplate.html", screen, subject, senderName, receiverName, claimNo, approvalType, userID, toEmail, clickUrl));

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

                                                    BackgroundJob.Enqueue(() => _sendMailServices.SendEmail("EmailTemplate.html", screen, subject, senderName, receiverName, claimNo, approvalType, userID, toEmail, clickUrl));

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
                                                    int? approverId = await _alternateApproverHelper.IsAlternateApprovalSetForUser(Convert.ToInt32(ApproverIDs.ToString().Split(',')[0].ToString()));
                                                    var mstVerifierDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(ApproverIDs.ToString().Split(',')[0].ToString()));
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
                                                    int? approverId = await _alternateApproverHelper.IsAlternateApprovalSetForUser(Convert.ToInt32(approverID));
                                                    var mstVerifierDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(approverID));
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

                                    //return Json(new { res = "Done" });
                                }
                                else
                                {
                                    //TempData["Status_Invocie"] = "Approval";
                                    //return Json(new { res = "0" });
                                }
                            }
                            else
                            {
                                //return Json(new { res = "Done" });
                            }
                        }
                        return Json(new { res = "0" });
                    }
                }

                else if (aPExportSearch.ModuleName == "ExpenseClaim")
                {
                    if (!string.IsNullOrEmpty(aPExportSearch.ClaimIds))
                    {
                        int[] CIDs = aPExportSearch.ClaimIds.Split(',').Select(int.Parse).ToArray();
                        for (int i = 0; i < CIDs.Length; i++)
                        {
                            if (User != null && User.Identity.IsAuthenticated)
                            {
                                int ECID = Convert.ToInt32(CIDs[i]);

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
                                    if (!string.IsNullOrEmpty(hodapprover) && delegatedUserId != null)
                                    {
                                        if ((hodapprover.Contains(delegatedUserId.Value.ToString())))
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

                                                    //var mstVerifierDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(userApproverID));
                                                    var toEmail = mstVerifierDetails.EmailAddress;
                                                    var receiverName = mstVerifierDetails.Name;
                                                    var claimNo = mstExpenseClaim.ECNo;
                                                    var screen = "Expense Claim";
                                                    var approvalType = "Approval Request";
                                                    int userID = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value);
                                                    var subject = "Expense Claim for Approval " + claimNo;

                                                    BackgroundJob.Enqueue(() => _sendMailServices.SendEmail("EmailTemplate.html", screen, subject, senderName, receiverName, claimNo, approvalType, userID, toEmail, clickUrl));

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

                                                    //var mstVerifierDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(VerifierIDs.ToString().Split(',')[0].ToString()));
                                                    var toEmail = mstVerifierDetails.EmailAddress;
                                                    var receiverName = mstVerifierDetails.Name;
                                                    var claimNo = mstExpenseClaim.ECNo;
                                                    var screen = "Expense Claim";
                                                    var approvalType = "Verification Request";
                                                    int userID = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value);
                                                    var subject = "Expense Claim for Verification " + claimNo;

                                                    BackgroundJob.Enqueue(() => _sendMailServices.SendEmail("EmailTemplate.html", screen, subject, senderName, receiverName, claimNo, approvalType, userID, toEmail, clickUrl));

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

                                                    //var mstVerifierDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(verifierID));
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

                                                    //var mstVerifierDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(HODApproverID));
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

                                                    int? approverId = await _alternateApproverHelper.IsAlternateApprovalSetForUser(Convert.ToInt32(ApproverIDs.ToString().Split(',')[0].ToString()));
                                                    var mstVerifierDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(ApproverIDs.ToString().Split(',')[0].ToString()));
                                                    bool isAlternateApproverSet = false;
                                                    if (approverId.HasValue)
                                                    {
                                                        mstVerifierDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(approverId.Value));
                                                        // Alternate approver is configured for the current user. So, do not show actions
                                                        isAlternateApproverSet = true;
                                                    }

                                                    //var mstVerifierDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(ApproverIDs.ToString().Split(',')[0].ToString()));
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

                                                    int? approverId = await _alternateApproverHelper.IsAlternateApprovalSetForUser(Convert.ToInt32(approverID));
                                                    var mstVerifierDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(approverID));
                                                    bool isAlternateApproverSet = false;
                                                    if (approverId.HasValue)
                                                    {
                                                        mstVerifierDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(approverId.Value));
                                                        // Alternate approver is configured for the current user. So, do not show actions
                                                        isAlternateApproverSet = true;
                                                    }

                                                    //var mstVerifierDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(approverID));
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

                                    //return Json(new { res = "Done" });
                                }
                                else
                                {
                                    //TempData["Status_Invocie"] = "Approval";
                                    //return Json(new { res = "0" });
                                }
                            }
                            else
                            {
                                //return Json(new { res = "Done" });
                            }
                        }
                        return Json(new { res = "0" });
                    }
                }
                else if (aPExportSearch.ModuleName == "TelephoneBillClaim")
                {
                    if (!string.IsNullOrEmpty(aPExportSearch.ClaimIds))
                    {
                        int[] CIDs = aPExportSearch.ClaimIds.Split(',').Select(int.Parse).ToArray();
                        for (int i = 0; i < CIDs.Length; i++)
                        {
                            bool isAlternateApprover = false;
                            if (User != null && User.Identity.IsAuthenticated)
                            {
                                int TBCID = Convert.ToInt32(CIDs[i]);

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
                                    if (!string.IsNullOrEmpty(usapprover) && delegatedUserId != null)
                                    {
                                        if ((usapprover.Contains(delegatedUserId.Value.ToString())))
                                        {
                                            excute = true;
                                            isAlternateApprover = true;
                                        }
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

                                                    BackgroundJob.Enqueue(() => _sendMailServices.SendEmail("EmailTemplate.html", screen, subject, senderName, receiverName, claimNo, approvalType, userID, toEmail, clickUrl));

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

                                                    BackgroundJob.Enqueue(() => _sendMailServices.SendEmail("EmailTemplate.html", screen, subject, senderName, receiverName, claimNo, approvalType, userID, toEmail, clickUrl));

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
                                                    int? approverId = await _alternateApproverHelper.IsAlternateApprovalSetForUser(Convert.ToInt32(ApproverIDs.ToString().Split(',')[0].ToString()));
                                                    var mstVerifierDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(ApproverIDs.ToString().Split(',')[0].ToString()));
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
                                                    int? approverId = await _alternateApproverHelper.IsAlternateApprovalSetForUser(Convert.ToInt32(approverID));
                                                    var mstVerifierDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(approverID));
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

                                    //return Json(new { res = "Done" });
                                }
                                else
                                {
                                    //TempData["Status_Invocie"] = "Approval";
                                    //return Json(new { res = "0" });
                                }
                            }
                            else
                            {
                                //return Json(new { res = "Done" });
                            }
                        }
                        return Json(new { res = "0" });
                    }
                }
                else if (aPExportSearch.ModuleName == "PV-ChequeClaim")
                {
                    if (!string.IsNullOrEmpty(aPExportSearch.ClaimIds))
                    {
                        int[] CIDs = aPExportSearch.ClaimIds.Split(',').Select(int.Parse).ToArray();
                        for (int i = 0; i < CIDs.Length; i++)
                        {
                            bool isAlternateApprover = false;
                            if (User != null && User.Identity.IsAuthenticated)
                            {
                                int PVCCID = Convert.ToInt32(CIDs[i]);

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
                                    if (!string.IsNullOrEmpty(hodapprover) && delegatedUserId != null)
                                    {
                                        if ((hodapprover.Contains(delegatedUserId.Value.ToString())))
                                        {
                                            excute = true;
                                            isAlternateApprover = true;
                                        }
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

                                                    BackgroundJob.Enqueue(() => _sendMailServices.SendEmail("EmailTemplate.html", screen, subject, senderName, receiverName, claimNo, approvalType, userID, toEmail, clickUrl));

                                                }
                                                else
                                                {
                                                    string domainUrl = HttpContext.Request.Scheme + "://" + HttpContext.Request.Host;
                                                    string clickUrl = domainUrl + "/" + "HodSummary/PVCCDetails/" + PVCCID;

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

                                                    BackgroundJob.Enqueue(() => _sendMailServices.SendEmail("EmailTemplate.html", screen, subject, senderName, receiverName, claimNo, approvalType, userID, toEmail, clickUrl));

                                                }

                                                break;
                                            }
                                        }
                                        catch
                                        {
                                        }
                                        await _repository.MstPVCClaim.UpdateMstPVCClaimStatus(PVCCID, 7, int.Parse(HttpContext.User.FindFirst("userid").Value), DateTime.Now, string.Empty, VerifierIDs.ToString(), ApproverIDs.ToString(), UserApproverIDs.ToString(), HODApproverID.ToString(), isAlternateApprover, 0);

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

                                            //Mail Code Implementation for Verifiers
                                            foreach (string verifierID in verifierIDs)
                                            {
                                                if (verifierID != "")
                                                {
                                                    string domainUrl = HttpContext.Request.Scheme + "://" + HttpContext.Request.Host;
                                                    string clickUrl = domainUrl + "/" + "FinancePVCClaim/Details/" + PVCCID;

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
                                                    var claimNo = mstPVCClaim.PVCCNo;
                                                    var screen = "PV-Cheque Claim";
                                                    var approvalType = "Verification Request";
                                                    int userID = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value);
                                                    var subject = "PV-Cheque Claim for Verification " + claimNo;

                                                    BackgroundJob.Enqueue(() => _sendMailServices.SendEmail("EmailTemplate.html", screen, subject, senderName, receiverName, claimNo, approvalType, userID, toEmail, clickUrl));
                                                }
                                                else
                                                {
                                                    string domainUrl = HttpContext.Request.Scheme + "://" + HttpContext.Request.Host;
                                                    string clickUrl = domainUrl + "/" + "FinancePVCClaim/Details/" + PVCCID;

                                                    var mstSenderDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(HttpContext.User.FindFirst("userid").Value));
                                                    var senderName = mstSenderDetails.Name;
                                                    int? approverId = await _alternateApproverHelper.IsAlternateApprovalSetForUser(Convert.ToInt32(ApproverIDs.ToString().Split(',')[0].ToString()));
                                                    var mstVerifierDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(ApproverIDs.ToString().Split(',')[0].ToString()));
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

                                                    BackgroundJob.Enqueue(() => _sendMailServices.SendEmail("EmailTemplate.html", screen, subject, senderName, receiverName, claimNo, approvalType, userID, toEmail, clickUrl));
                                                }
                                                break;
                                            }
                                        }
                                        catch
                                        {
                                        }
                                        await _repository.MstPVCClaim.UpdateMstPVCClaimStatus(PVCCID, 2, int.Parse(HttpContext.User.FindFirst("userid").Value), DateTime.Now, string.Empty, VerifierIDs.ToString(), ApproverIDs.ToString(), UserApproverIDs.ToString(), HODApproverID.ToString(), isAlternateApprover, 0);

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
                                                    var claimNo = mstPVCClaim.PVCCNo;
                                                    var screen = "PV-Cheque Claim";
                                                    var approvalType = "Verification Request";
                                                    int userID = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value);
                                                    var subject = "PV-Cheque Claim for Verification " + claimNo;

                                                    BackgroundJob.Enqueue(() => _sendMailServices.SendEmail("EmailTemplate.html", screen, subject, senderName, receiverName, claimNo, approvalType, userID, toEmail, clickUrl));
                                                }

                                                break;
                                            }
                                        }
                                        catch
                                        {
                                        }
                                        await _repository.MstPVCClaim.UpdateMstPVCClaimStatus(PVCCID, 1, int.Parse(HttpContext.User.FindFirst("userid").Value), DateTime.Now, string.Empty, VerifierIDs.ToString(), ApproverIDs.ToString(), UserApproverIDs.ToString(), HODApproverID.ToString(), isAlternateApprover, 0);

                                    }
                                    #endregion

                                    #region PVC Approver
                                    else if (ApprovedStatus == 2)
                                    {
                                        string VerifierIDs = "";
                                        string ApproverIDs = "";
                                        string UserApproverIDs = "";
                                        string HODApproverID = "";
                                        string DVerifierIDs = "";
                                        try
                                        {
                                            string[] PVCapproverIDs = mstPVCClaim.Approver.Split(',');
                                            ApproverIDs = string.Join(",", PVCapproverIDs.Skip(1));
                                            string[] approverIDs = ApproverIDs.Split(',');
                                            int CreatedBy = Convert.ToInt32(mstPVCClaim.CreatedBy);
                                            DVerifierIDs = mstPVCClaim.DVerifier.Split(',').First();

                                            //Mail Code Implementation for Approvers
                                            foreach (string approverID in approverIDs)
                                            {
                                                if (approverID != "")
                                                {
                                                    string domainUrl = HttpContext.Request.Scheme + "://" + HttpContext.Request.Host;
                                                    string clickUrl = domainUrl + "/" + "FinancePVCClaim/Details/" + PVCCID;

                                                    var mstSenderDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(HttpContext.User.FindFirst("userid").Value));
                                                    var senderName = mstSenderDetails.Name;
                                                    int? approverId = await _alternateApproverHelper.IsAlternateApprovalSetForUser(Convert.ToInt32(approverID));
                                                    var mstVerifierDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(approverID));
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

                                                    BackgroundJob.Enqueue(() => _sendMailServices.SendEmail("EmailTemplate.html", screen, subject, senderName, receiverName, claimNo, approvalType, userID, toEmail, clickUrl));

                                                }

                                                break;
                                            }
                                        }
                                        catch
                                        {
                                        }
                                        string financeStartDay = _configuration.GetValue<string>("FinanceStartDay");
                                        await _repository.MstPVCClaim.UpdateMstPVCClaimStatus(PVCCID, 3, int.Parse(HttpContext.User.FindFirst("userid").Value), DateTime.Now, string.Empty, VerifierIDs, ApproverIDs, UserApproverIDs.ToString(), HODApproverID.ToString(), isAlternateApprover, int.Parse(financeStartDay));
                                        if (ApproverIDs == string.Empty)
                                        {
                                            string domainUrl = HttpContext.Request.Scheme + "://" + HttpContext.Request.Host;
                                            string clickUrl = domainUrl + "/" + "FinanceReports";

                                            var mstSenderDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(HttpContext.User.FindFirst("userid").Value));
                                            var senderName = mstSenderDetails.Name;
                                            var mstVerifierDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(DVerifierIDs));
                                            var toEmail = mstVerifierDetails.EmailAddress;
                                            var receiverName = mstVerifierDetails.Name;
                                            var claimNo = mstPVCClaim.PVCCNo;
                                            var screen = "PV-Cheque Claim";
                                            var approvalType = "Export to AccPac/Bank Request";
                                            int userID = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value);
                                            var subject = "PV-Cheque Claim for Export to AccPac/Bank " + claimNo;

                                            BackgroundJob.Enqueue(() => _sendMailServices.SendEmail("ExportToBankTemplate.html", screen, subject, senderName, receiverName, claimNo, approvalType, userID, toEmail, clickUrl));
                                        }
                                    }

                                    #endregion

                                    //return Json(new { res = "Done" });
                                }
                                else
                                {
                                    //TempData["Status_Invocie"] = "Approval";
                                    //return Json(new { res = "0" });
                                }
                            }
                        }
                        return Json(new { res = "0" });
                    }
                }
                else if (aPExportSearch.ModuleName == "PV-GiroClaim")
                {
                    if (!string.IsNullOrEmpty(aPExportSearch.ClaimIds))
                    {
                        int[] CIDs = aPExportSearch.ClaimIds.Split(',').Select(int.Parse).ToArray();
                        for (int i = 0; i < CIDs.Length; i++)
                        {
                            long PVGCID = Convert.ToInt64(CIDs[i]);
                            bool isAlternateApprover = false;
                            if (User != null && User.Identity.IsAuthenticated)
                            {
                                if (!string.IsNullOrEmpty(aPExportSearch.FromPage))
                                {
                                    long PVGCItemID = Convert.ToInt64(CIDs[i]);
                                    var id = await _repository.DtPVGClaim.GetDtPVGClaimByPVGCItemIDAsync(PVGCItemID);
                                    PVGCID = id.PVGCID;
                                }

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
                                    if (!string.IsNullOrEmpty(hodapprover) && delegatedUserId != null)
                                    {
                                        if ((hodapprover.Contains(delegatedUserId.Value.ToString())))
                                        {
                                            excute = true;
                                            isAlternateApprover = true;
                                        }
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

                                                    BackgroundJob.Enqueue(() => _sendMailServices.SendEmail("EmailTemplate.html", screen, subject, senderName, receiverName, claimNo, approvalType, userID, toEmail, clickUrl));

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

                                                    BackgroundJob.Enqueue(() => _sendMailServices.SendEmail("EmailTemplate.html", screen, subject, senderName, receiverName, claimNo, approvalType, userID, toEmail, clickUrl));

                                                }

                                                break;
                                            }
                                        }
                                        catch
                                        {
                                        }
                                        await _repository.MstPVGClaim.UpdateMstPVGClaimStatus(PVGCID, 7, int.Parse(HttpContext.User.FindFirst("userid").Value), DateTime.Now, string.Empty, VerifierIDs.ToString(), ApproverIDs.ToString(), UserApproverIDs.ToString(), HODApproverID.ToString(), isAlternateApprover, 0);

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

                                            //Mail Code Implementation for Verifiers
                                            foreach (string verifierID in verifierIDs)
                                            {
                                                if (verifierID != "")
                                                {
                                                    string domainUrl = HttpContext.Request.Scheme + "://" + HttpContext.Request.Host;
                                                    string clickUrl = domainUrl + "/" + "FinancePVGClaim/Details/" + PVGCID;

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
                                                    var claimNo = mstPVGClaim.PVGCNo;
                                                    var screen = "PV-GIRO Claim";
                                                    var approvalType = "Verification Request";
                                                    int userID = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value);
                                                    var subject = "PV-GIRO Claim for Verification " + claimNo;

                                                    BackgroundJob.Enqueue(() => _sendMailServices.SendEmail("EmailTemplate.html", screen, subject, senderName, receiverName, claimNo, approvalType, userID, toEmail, clickUrl));
                                                }
                                                else
                                                {
                                                    string domainUrl = HttpContext.Request.Scheme + "://" + HttpContext.Request.Host;
                                                    string clickUrl = domainUrl + "/" + "FinancePVGClaim/Details/" + PVGCID;

                                                    var mstSenderDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(HttpContext.User.FindFirst("userid").Value));
                                                    var senderName = mstSenderDetails.Name;
                                                    int? approverId = await _alternateApproverHelper.IsAlternateApprovalSetForUser(Convert.ToInt32(ApproverIDs.ToString().Split(',')[0].ToString()));
                                                    var mstVerifierDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(ApproverIDs.ToString().Split(',')[0].ToString()));
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

                                                    BackgroundJob.Enqueue(() => _sendMailServices.SendEmail("EmailTemplate.html", screen, subject, senderName, receiverName, claimNo, approvalType, userID, toEmail, clickUrl));
                                                }
                                                break;
                                            }
                                        }
                                        catch
                                        {
                                        }
                                        await _repository.MstPVGClaim.UpdateMstPVGClaimStatus(PVGCID, 2, int.Parse(HttpContext.User.FindFirst("userid").Value), DateTime.Now, string.Empty, VerifierIDs.ToString(), ApproverIDs.ToString(), UserApproverIDs.ToString(), HODApproverID.ToString(), isAlternateApprover, 0);

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
                                                    var claimNo = mstPVGClaim.PVGCNo;
                                                    var screen = "PV-GIRO Claim";
                                                    var approvalType = "Verification Request";
                                                    int userID = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value);
                                                    var subject = "PV-GIRO Claim for Verification " + claimNo;

                                                    BackgroundJob.Enqueue(() => _sendMailServices.SendEmail("EmailTemplate.html", screen, subject, senderName, receiverName, claimNo, approvalType, userID, toEmail, clickUrl));
                                                }

                                                break;
                                            }
                                        }
                                        catch
                                        {
                                        }
                                        await _repository.MstPVGClaim.UpdateMstPVGClaimStatus(PVGCID, 1, int.Parse(HttpContext.User.FindFirst("userid").Value), DateTime.Now, string.Empty, VerifierIDs.ToString(), ApproverIDs.ToString(), UserApproverIDs.ToString(), HODApproverID.ToString(), isAlternateApprover, 0);

                                    }
                                    #endregion

                                    #region PVG Approver
                                    else if (ApprovedStatus == 2)
                                    {
                                        string VerifierIDs = "";
                                        string ApproverIDs = "";
                                        string UserApproverIDs = "";
                                        string HODApproverID = "";
                                        string DVerifierIDs = "";
                                        try
                                        {
                                            string[] PVGapproverIDs = mstPVGClaim.Approver.Split(',');
                                            ApproverIDs = string.Join(",", PVGapproverIDs.Skip(1));
                                            string[] approverIDs = ApproverIDs.Split(',');
                                            int CreatedBy = Convert.ToInt32(mstPVGClaim.CreatedBy);
                                            DVerifierIDs = mstPVGClaim.DVerifier.Split(',').First();

                                            //Mail Code Implementation for Approvers
                                            foreach (string approverID in approverIDs)
                                            {
                                                if (approverID != "")
                                                {
                                                    string domainUrl = HttpContext.Request.Scheme + "://" + HttpContext.Request.Host;
                                                    string clickUrl = domainUrl + "/" + "FinancePVGClaim/Details/" + PVGCID;

                                                    var mstSenderDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(HttpContext.User.FindFirst("userid").Value));
                                                    var senderName = mstSenderDetails.Name;
                                                    int? approverId = await _alternateApproverHelper.IsAlternateApprovalSetForUser(Convert.ToInt32(approverID));
                                                    var mstVerifierDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(approverID));
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

                                                    BackgroundJob.Enqueue(() => _sendMailServices.SendEmail("EmailTemplate.html", screen, subject, senderName, receiverName, claimNo, approvalType, userID, toEmail, clickUrl));

                                                }

                                                break;
                                            }
                                        }
                                        catch
                                        {
                                        }
                                        string financeStartDay = _configuration.GetValue<string>("FinanceStartDay");
                                        await _repository.MstPVGClaim.UpdateMstPVGClaimStatus(PVGCID, 3, int.Parse(HttpContext.User.FindFirst("userid").Value), DateTime.Now, string.Empty, VerifierIDs, ApproverIDs, UserApproverIDs.ToString(), HODApproverID.ToString(), isAlternateApprover, int.Parse(financeStartDay));
                                        if (ApproverIDs == string.Empty)
                                        {
                                            string domainUrl = HttpContext.Request.Scheme + "://" + HttpContext.Request.Host;
                                            string clickUrl = domainUrl + "/" + "FinanceReports";

                                            var mstSenderDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(HttpContext.User.FindFirst("userid").Value));
                                            var senderName = mstSenderDetails.Name;
                                            var mstVerifierDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(DVerifierIDs));
                                            var toEmail = mstVerifierDetails.EmailAddress;
                                            var receiverName = mstVerifierDetails.Name;
                                            var claimNo = mstPVGClaim.PVGCNo;
                                            var screen = "PV-GIRO Claim";
                                            var approvalType = "Export to AccPac/Bank Request";
                                            int userID = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value);
                                            var subject = "PV-GIRO Claim for Export to AccPac/Bank " + claimNo;

                                            BackgroundJob.Enqueue(() => _sendMailServices.SendEmail("ExportToBankTemplate.html", screen, subject, senderName, receiverName, claimNo, approvalType, userID, toEmail, clickUrl));
                                        }
                                    }

                                    #endregion

                                    //return Json(new { res = "Done" });
                                }
                                else
                                {
                                    //TempData["Status_Invocie"] = "Approval";
                                    //return Json(new { res = "0" });
                                }
                            }
                        }
                        return Json(new { res = "0" });
                    }
                }
                else if (aPExportSearch.ModuleName == "HRPV-ChequeClaim")
                {
                    if (!string.IsNullOrEmpty(aPExportSearch.ClaimIds))
                    {
                        int[] CIDs = aPExportSearch.ClaimIds.Split(',').Select(int.Parse).ToArray();
                        for (int i = 0; i < CIDs.Length; i++)
                        {
                            if (User != null && User.Identity.IsAuthenticated)
                            {
                                int HRPVCCID = Convert.ToInt32(CIDs[i]);

                                var mstHRPVCClaim = await _repository.MstHRPVCClaim.GetHRPVCClaimByIdAsync(HRPVCCID);

                                if (mstHRPVCClaim == null)
                                {
                                    // return NotFound();
                                }

                                bool isAlternateApprover = false;
                                int ApprovedStatus = Convert.ToInt32(mstHRPVCClaim.ApprovalStatus);
                                bool excute = _repository.MstHRPVCClaim.ExistsApproval(HRPVCCID.ToString(), ApprovedStatus, HttpContext.User.FindFirst("userid").Value, "HRPVC");
                                // If execute is false, Check if the current user is alternate user for this claim
                                if (excute == false)
                                {
                                    string hodapprover = _repository.MstHRPVCClaim.GetApproval(HRPVCCID.ToString(), ApprovedStatus, HttpContext.User.FindFirst("userid").Value, "HRPVC");
                                    int loggedInUserId = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value);
                                    var delegatedUserId = await _alternateApproverHelper.IsUserHasAnyAlternateApprovalSet(loggedInUserId);
                                    if (!string.IsNullOrEmpty(hodapprover) && delegatedUserId != null)
                                    {
                                        if ((hodapprover.Contains(delegatedUserId.Value.ToString())))
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
                                                    var claimNo = mstHRPVCClaim.HRPVCCNo;
                                                    var screen = "HR PV-Cheque Claim";
                                                    var approvalType = "Approval Request";
                                                    int userID = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value);
                                                    var subject = "HR PV-Cheque Claim for Approval " + claimNo;

                                                    BackgroundJob.Enqueue(() => _sendMailServices.SendEmail("EmailTemplate.html", screen, subject, senderName, receiverName, claimNo, approvalType, userID, toEmail, clickUrl));

                                                }
                                                else
                                                {
                                                    string domainUrl = HttpContext.Request.Scheme + "://" + HttpContext.Request.Host;
                                                    string clickUrl = domainUrl + "/" + "FinanceHRPVCClaim/Details/" + HRPVCCID;

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
                                                    var claimNo = mstHRPVCClaim.HRPVCCNo;
                                                    var screen = "HR PV-Cheque Claim";
                                                    var approvalType = "Verification Request";
                                                    int userID = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value);
                                                    var subject = "HR PV-Cheque Claim for Verification " + claimNo;

                                                    BackgroundJob.Enqueue(() => _sendMailServices.SendEmail("EmailTemplate.html", screen, subject, senderName, receiverName, claimNo, approvalType, userID, toEmail, clickUrl));
                                                }
                                                break;
                                            }
                                        }
                                        catch
                                        {
                                        }
                                        await _repository.MstHRPVCClaim.UpdateMstHRPVCClaimStatus(HRPVCCID, 1, int.Parse(HttpContext.User.FindFirst("userid").Value), DateTime.Now, string.Empty, VerifierIDs.ToString(), ApproverIDs.ToString(), UserApproverIDs.ToString(), HODApproverID.ToString(), isAlternateApprover, 0);

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
                                            string[] PVCverifierIDs = mstHRPVCClaim.Verifier.Split(',');
                                            VerifierIDs = string.Join(",", PVCverifierIDs.Skip(1));
                                            string[] verifierIDs = VerifierIDs.ToString().Split(',');
                                            ApproverIDs = mstHRPVCClaim.Approver;

                                            //Mail Code Implementation for Verifiers
                                            foreach (string verifierID in verifierIDs)
                                            {
                                                if (verifierID != "")
                                                {
                                                    string domainUrl = HttpContext.Request.Scheme + "://" + HttpContext.Request.Host;
                                                    string clickUrl = domainUrl + "/" + "FinanceHRPVCClaim/Details/" + HRPVCCID;

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
                                                    var claimNo = mstHRPVCClaim.HRPVCCNo;
                                                    var screen = "HR PV-Cheque Claim";
                                                    var approvalType = "Verification Request";
                                                    int userID = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value);
                                                    var subject = "HR PV-Cheque Claim for Verification " + claimNo;

                                                    BackgroundJob.Enqueue(() => _sendMailServices.SendEmail("EmailTemplate.html", screen, subject, senderName, receiverName, claimNo, approvalType, userID, toEmail, clickUrl));
                                                }
                                                else
                                                {
                                                    string domainUrl = HttpContext.Request.Scheme + "://" + HttpContext.Request.Host;
                                                    string clickUrl = domainUrl + "/" + "FinanceHRPVCClaim/Details/" + HRPVCCID;

                                                    var mstSenderDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(HttpContext.User.FindFirst("userid").Value));
                                                    var senderName = mstSenderDetails.Name;
                                                    int? approverId = await _alternateApproverHelper.IsAlternateApprovalSetForUser(Convert.ToInt32(ApproverIDs.ToString().Split(',')[0].ToString()));
                                                    var mstVerifierDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(ApproverIDs.ToString().Split(',')[0].ToString()));
                                                    bool isAlternateApproverSet = false;
                                                    if (approverId.HasValue)
                                                    {
                                                        mstVerifierDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(approverId.Value));
                                                        // Alternate approver is configured for the current user. So, do not show actions
                                                        isAlternateApproverSet = true;
                                                    }
                                                    var toEmail = mstVerifierDetails.EmailAddress;
                                                    var receiverName = mstVerifierDetails.Name;
                                                    var claimNo = mstHRPVCClaim.HRPVCCNo;
                                                    var screen = "HR PV-Cheque Claim";
                                                    var approvalType = "Approval Request";
                                                    int userID = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value);
                                                    var subject = "HR PV-Cheque Claim for Approval " + claimNo;

                                                    BackgroundJob.Enqueue(() => _sendMailServices.SendEmail("EmailTemplate.html", screen, subject, senderName, receiverName, claimNo, approvalType, userID, toEmail, clickUrl));
                                                }
                                                break;
                                            }
                                        }
                                        catch
                                        {
                                        }
                                        string financeStartDay = _configuration.GetValue<string>("FinanceStartDay");
                                        await _repository.MstHRPVCClaim.UpdateMstHRPVCClaimStatus(HRPVCCID, 2, int.Parse(HttpContext.User.FindFirst("userid").Value), DateTime.Now, string.Empty, VerifierIDs.ToString(), ApproverIDs.ToString(), UserApproverIDs.ToString(), HODApproverID.ToString(), isAlternateApprover, int.Parse(financeStartDay));

                                    }
                                    #endregion

                                    #region PVC Approver
                                    else if (ApprovedStatus == 2)
                                    {
                                        string VerifierIDs = "";
                                        string ApproverIDs = "";
                                        string UserApproverIDs = "";
                                        string HODApproverID = "";
                                        string DVerifierIDs = "";
                                        try
                                        {
                                            string[] PVCapproverIDs = mstHRPVCClaim.Approver.Split(',');
                                            ApproverIDs = string.Join(",", PVCapproverIDs.Skip(1));
                                            string[] approverIDs = ApproverIDs.Split(',');
                                            int CreatedBy = Convert.ToInt32(mstHRPVCClaim.CreatedBy);
                                            DVerifierIDs = mstHRPVCClaim.DVerifier.Split(',').First();

                                            //Mail Code Implementation for Approvers
                                            foreach (string approverID in approverIDs)
                                            {
                                                if (approverID != "")
                                                {
                                                    string domainUrl = HttpContext.Request.Scheme + "://" + HttpContext.Request.Host;
                                                    string clickUrl = domainUrl + "/" + "FinanceHRPVCClaim/Details/" + HRPVCCID;

                                                    var mstSenderDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(HttpContext.User.FindFirst("userid").Value));
                                                    var senderName = mstSenderDetails.Name;
                                                    int? approverId = await _alternateApproverHelper.IsAlternateApprovalSetForUser(Convert.ToInt32(approverID));
                                                    var mstVerifierDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(approverID));
                                                    bool isAlternateApproverSet = false;
                                                    if (approverId.HasValue)
                                                    {
                                                        mstVerifierDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(approverId.Value));
                                                        // Alternate approver is configured for the current user. So, do not show actions
                                                        isAlternateApproverSet = true;
                                                    }
                                                    var toEmail = mstVerifierDetails.EmailAddress;
                                                    var receiverName = mstVerifierDetails.Name;
                                                    var claimNo = mstHRPVCClaim.HRPVCCNo;
                                                    var screen = "HR PV-Cheque Claim";
                                                    var approvalType = "Approval Request";
                                                    int userID = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value);
                                                    var subject = "HR PV-Cheque Claim for Approval " + claimNo;

                                                    BackgroundJob.Enqueue(() => _sendMailServices.SendEmail("EmailTemplate.html", screen, subject, senderName, receiverName, claimNo, approvalType, userID, toEmail, clickUrl));

                                                }

                                                break;
                                            }
                                        }
                                        catch
                                        {
                                        }
                                        string financeStartDay = _configuration.GetValue<string>("FinanceStartDay");
                                        await _repository.MstHRPVCClaim.UpdateMstHRPVCClaimStatus(HRPVCCID, 3, int.Parse(HttpContext.User.FindFirst("userid").Value), DateTime.Now, string.Empty, VerifierIDs, ApproverIDs, UserApproverIDs.ToString(), HODApproverID.ToString(), isAlternateApprover, int.Parse(financeStartDay));
                                        if (ApproverIDs == string.Empty)
                                        {
                                            string domainUrl = HttpContext.Request.Scheme + "://" + HttpContext.Request.Host;
                                            string clickUrl = domainUrl + "/" + "FinanceReports";

                                            var mstSenderDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(HttpContext.User.FindFirst("userid").Value));
                                            var senderName = mstSenderDetails.Name;
                                            var mstVerifierDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(DVerifierIDs));
                                            var toEmail = mstVerifierDetails.EmailAddress;
                                            var receiverName = mstVerifierDetails.Name;
                                            var claimNo = mstHRPVCClaim.HRPVCCNo;
                                            var screen = "HR PV-Cheque Claim";
                                            var approvalType = "Export to AccPac/Bank Request";
                                            int userID = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value);
                                            var subject = "HR PV-Cheque Claim for Export to AccPac/Bank " + claimNo;

                                            BackgroundJob.Enqueue(() => _sendMailServices.SendEmail("ExportToBankTemplate.html", screen, subject, senderName, receiverName, claimNo, approvalType, userID, toEmail, clickUrl));
                                        }
                                    }

                                    #endregion

                                    //return Json(new { res = "Done" });
                                }
                                else
                                {
                                    //TempData["Status_Invocie"] = "Approval";
                                    //return Json(new { res = "0" });
                                }
                            }
                        }
                        return Json(new { res = "0" });
                    }
                }
                else
                {
                    if (!string.IsNullOrEmpty(aPExportSearch.ClaimIds))
                    {
                        int[] CIDs = aPExportSearch.ClaimIds.Split(',').Select(int.Parse).ToArray();
                        for (int i = 0; i < CIDs.Length; i++)
                        {
                            if (User != null && User.Identity.IsAuthenticated)
                            {
                                long HRPVGCID = Convert.ToInt64(CIDs[i]);
                                if (!string.IsNullOrEmpty(aPExportSearch.FromPage))
                                {
                                    long HRPVGCItemID = Convert.ToInt64(CIDs[i]);
                                    var id = await _repository.DtHRPVGClaim.GetDtHRPVGClaimByHRPVGCItemIDAsync(HRPVGCItemID);
                                    HRPVGCID = id.HRPVGCID;
                                }


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
                                    if (!string.IsNullOrEmpty(hodapprover) && delegatedUserId != null)
                                    {
                                        if ((hodapprover.Contains(delegatedUserId.Value.ToString())))
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

                                                    BackgroundJob.Enqueue(() => _sendMailServices.SendEmail("EmailTemplate.html", screen, subject, senderName, receiverName, claimNo, approvalType, userID, toEmail, clickUrl));

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

                                                    BackgroundJob.Enqueue(() => _sendMailServices.SendEmail("EmailTemplate.html", screen, subject, senderName, receiverName, claimNo, approvalType, userID, toEmail, clickUrl));
                                                }
                                                break;
                                            }
                                        }
                                        catch
                                        {
                                        }
                                        await _repository.MstHRPVGClaim.UpdateMstHRPVGClaimStatus(HRPVGCID, 1, int.Parse(HttpContext.User.FindFirst("userid").Value), DateTime.Now, string.Empty, VerifierIDs.ToString(), ApproverIDs.ToString(), UserApproverIDs.ToString(), HODApproverID.ToString(), isAlternateApprover, 0);

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

                                            //Mail Code Implementation for Verifiers
                                            foreach (string verifierID in verifierIDs)
                                            {
                                                if (verifierID != "")
                                                {
                                                    string domainUrl = HttpContext.Request.Scheme + "://" + HttpContext.Request.Host;
                                                    string clickUrl = domainUrl + "/" + "FinanceHRPVGClaim/Details/" + HRPVGCID;

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
                                                    int? approverId = await _alternateApproverHelper.IsAlternateApprovalSetForUser(Convert.ToInt32(ApproverIDs.ToString().Split(',')[0].ToString()));
                                                    var mstVerifierDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(ApproverIDs.ToString().Split(',')[0].ToString()));
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
                                                    int? approverId = await _alternateApproverHelper.IsAlternateApprovalSetForUser(Convert.ToInt32(ApproverIDs.ToString().Split(',')[0].ToString()));
                                                    var mstVerifierDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(ApproverIDs.ToString().Split(',')[0].ToString()));
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

                                    //return Json(new { res = "Done" });
                                }
                                else
                                {
                                    //TempData["Status_Invocie"] = "Approval";
                                    //return Json(new { res = "0" });
                                }
                            }
                        }
                    }
                    return Json(new { res = "0" });
                }
            }
            return Json(new { res = "Done" });
        }

        public async Task<JsonResult> UpdateRejectedStatus(string data)
        {
            var aPExportSearch = JsonConvert.DeserializeObject<APExportSearch>(data);

            if (aPExportSearch != null)
            {
                if (aPExportSearch.ModuleName == "MileageClaim")
                {
                    if (!string.IsNullOrEmpty(aPExportSearch.ClaimIds))
                    {
                        int[] CIDs = aPExportSearch.ClaimIds.Split(',').Select(int.Parse).ToArray();
                        for (int i = 0; i < CIDs.Length; i++)
                        {
                            if (User != null && User.Identity.IsAuthenticated)
                            {
                                int MCID = Convert.ToInt32(CIDs[i]);

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

                                await _repository.MstMileageClaim.UpdateMstMileageClaimStatus(MCID, 4, int.Parse(HttpContext.User.FindFirst("userid").Value), DateTime.Now, aPExportSearch.Reason, string.Empty, string.Empty, string.Empty, string.Empty, isAlternateApprover, 0);
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

                                var rejectReason = aPExportSearch.Reason;
                                var lastApprover = string.Empty;
                                var nextApprover = senderName;

                                BackgroundJob.Enqueue(() => _sendMailServices.SendEmail("Rejected.html", screen, subject, senderName, receiverName, claimNo, approvalType, userID, toEmail, clickUrl, lastApprover, nextApprover, rejectReason));

                                return Json(new { res = "Done" });
                            }
                        }
                        return Json(new { res = "0" });
                    }
                }
                else if (aPExportSearch.ModuleName == "ExpenseClaim")
                {
                    if (!string.IsNullOrEmpty(aPExportSearch.ClaimIds))
                    {
                        int[] CIDs = aPExportSearch.ClaimIds.Split(',').Select(int.Parse).ToArray();
                        for (int i = 0; i < CIDs.Length; i++)
                        {
                            if (User != null && User.Identity.IsAuthenticated)
                            {
                                int ECID = Convert.ToInt32(CIDs[i]);

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

                                await _repository.MstExpenseClaim.UpdateMstExpenseClaimStatus(ECID, 4, int.Parse(HttpContext.User.FindFirst("userid").Value), DateTime.Now, aPExportSearch.Reason, string.Empty, string.Empty, string.Empty, string.Empty, isAlternateApprover, 0);
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

                                var rejectReason = aPExportSearch.Reason;
                                var lastApprover = string.Empty;
                                var nextApprover = senderName;

                                BackgroundJob.Enqueue(() => _sendMailServices.SendEmail("Rejected.html", screen, subject, senderName, receiverName, claimNo, approvalType, userID, toEmail, clickUrl, lastApprover, nextApprover, rejectReason));

                                return Json(new { res = "Done" });
                            }
                            else
                            {
                                //return Json(new { res = "Done" });
                            }
                        }
                    }
                }
                else if (aPExportSearch.ModuleName == "TelephoneBillClaim")
                {
                    if (!string.IsNullOrEmpty(aPExportSearch.ClaimIds))
                    {
                        int[] CIDs = aPExportSearch.ClaimIds.Split(',').Select(int.Parse).ToArray();
                        for (int i = 0; i < CIDs.Length; i++)
                        {
                            bool isAlternateApprover = false;
                            if (User != null && User.Identity.IsAuthenticated)
                            {
                                int TBCID = Convert.ToInt32(CIDs[i]);

                                var mstTBClaim = await _repository.MstTBClaim.GetTBClaimByIdAsync(TBCID);

                                if (mstTBClaim == null)
                                {
                                    // return NotFound();
                                }

                                int loggedInUserId = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value);
                                //bool isAlternateApprover = false;
                                var delegatedUserId = await _alternateApproverHelper.IsUserHasAnyAlternateApprovalSet(loggedInUserId);
                                if (delegatedUserId.HasValue)
                                {
                                    isAlternateApprover = true;
                                }

                                await _repository.MstTBClaim.UpdateMstTBClaimStatus(TBCID, 4, int.Parse(HttpContext.User.FindFirst("userid").Value), DateTime.Now, aPExportSearch.Reason, string.Empty, string.Empty, string.Empty, string.Empty, isAlternateApprover, 0);
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

                                var rejectReason = aPExportSearch.Reason;
                                var lastApprover = string.Empty;
                                var nextApprover = senderName;

                                BackgroundJob.Enqueue(() => _sendMailServices.SendEmail("Rejected.html", screen, subject, senderName, receiverName, claimNo, approvalType, userID, toEmail, clickUrl, lastApprover, nextApprover, rejectReason));

                                return Json(new { res = "Done" });
                            }
                        }
                        return Json(new { res = "0" });
                    }
                }
                else if (aPExportSearch.ModuleName == "PV-ChequeClaim")
                {
                    if (!string.IsNullOrEmpty(aPExportSearch.ClaimIds))
                    {
                        int[] CIDs = aPExportSearch.ClaimIds.Split(',').Select(int.Parse).ToArray();
                        for (int i = 0; i < CIDs.Length; i++)
                        {
                            bool isAlternateApprover = false;
                            if (User != null && User.Identity.IsAuthenticated)
                            {
                                int PVCCID = Convert.ToInt32(CIDs[i]);

                                var mstPVCClaim = await _repository.MstPVCClaim.GetPVCClaimByIdAsync(PVCCID);

                                if (mstPVCClaim == null)
                                {
                                    // return NotFound();
                                }

                                int loggedInUserId = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value);
                                //bool isAlternateApprover = false;
                                var delegatedUserId = await _alternateApproverHelper.IsUserHasAnyAlternateApprovalSet(loggedInUserId);
                                if (delegatedUserId.HasValue)
                                {
                                    isAlternateApprover = true;
                                }

                                await _repository.MstPVCClaim.UpdateMstPVCClaimStatus(PVCCID, 4, int.Parse(HttpContext.User.FindFirst("userid").Value), DateTime.Now, aPExportSearch.Reason, string.Empty, string.Empty, string.Empty, string.Empty, isAlternateApprover, 0);
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

                                var rejectReason = aPExportSearch.Reason;
                                var lastApprover = string.Empty;
                                var nextApprover = senderName;

                                BackgroundJob.Enqueue(() => _sendMailServices.SendEmail("Rejected.html", screen, subject, senderName, receiverName, claimNo, approvalType, userID, toEmail, clickUrl, lastApprover, nextApprover, rejectReason));

                                return Json(new { res = "Done" });
                            }
                        }
                        return Json(new { res = "0" });
                    }
                }
                else if (aPExportSearch.ModuleName == "PV-GiroClaim")
                {
                    if (!string.IsNullOrEmpty(aPExportSearch.ClaimIds))
                    {
                        int[] CIDs = aPExportSearch.ClaimIds.Split(',').Select(int.Parse).ToArray();
                        for (int i = 0; i < CIDs.Length; i++)
                        {
                            long PVGCID = Convert.ToInt64(CIDs[i]);
                            bool isAlternateApprover = false;
                            if (User != null && User.Identity.IsAuthenticated)
                            {
                                //int PVGCID = Convert.ToInt32(id);

                                var mstPVGClaim = await _repository.MstPVGClaim.GetPVGClaimByIdAsync(PVGCID);

                                if (mstPVGClaim == null)
                                {
                                    // return NotFound();
                                }

                                int loggedInUserId = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value);
                                //bool isAlternateApprover = false;
                                var delegatedUserId = await _alternateApproverHelper.IsUserHasAnyAlternateApprovalSet(loggedInUserId);
                                if (delegatedUserId.HasValue)
                                {
                                    isAlternateApprover = true;
                                }

                                await _repository.MstPVGClaim.UpdateMstPVGClaimStatus(PVGCID, 4, int.Parse(HttpContext.User.FindFirst("userid").Value), DateTime.Now, aPExportSearch.Reason, string.Empty, string.Empty, string.Empty, string.Empty, isAlternateApprover, 0);
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

                                var rejectReason = aPExportSearch.Reason;
                                var lastApprover = string.Empty;
                                var nextApprover = senderName;

                                BackgroundJob.Enqueue(() => _sendMailServices.SendEmail("Rejected.html", screen, subject, senderName, receiverName, claimNo, approvalType, userID, toEmail, clickUrl, lastApprover, nextApprover, rejectReason));

                                return Json(new { res = "Done" });
                            }
                        }
                        return Json(new { res = "0" });
                    }
                }
                else if (aPExportSearch.ModuleName == "HRPV-ChequeClaim")
                {
                    if (!string.IsNullOrEmpty(aPExportSearch.ClaimIds))
                    {
                        int[] CIDs = aPExportSearch.ClaimIds.Split(',').Select(int.Parse).ToArray();
                        for (int i = 0; i < CIDs.Length; i++)
                        {
                            if (User != null && User.Identity.IsAuthenticated)
                            {
                                int HRPVCCID = Convert.ToInt32(CIDs[i]);

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
                                string financeStartDay = _configuration.GetValue<string>("FinanceStartDay");
                                await _repository.MstHRPVCClaim.UpdateMstHRPVCClaimStatus(HRPVCCID, 4, int.Parse(HttpContext.User.FindFirst("userid").Value), DateTime.Now, aPExportSearch.Reason, string.Empty, string.Empty, string.Empty, string.Empty, isAlternateApprover, int.Parse(financeStartDay));
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

                                var rejectReason = aPExportSearch.Reason;
                                var lastApprover = string.Empty;
                                var nextApprover = senderName;

                                BackgroundJob.Enqueue(() => _sendMailServices.SendEmail("Rejected.html", screen, subject, senderName, receiverName, claimNo, approvalType, userID, toEmail, clickUrl, lastApprover, nextApprover, rejectReason));

                                return Json(new { res = "Done" });
                            }
                        }
                        return Json(new { res = "0" });
                    }
                }
                else
                {
                    if (!string.IsNullOrEmpty(aPExportSearch.ClaimIds))
                    {
                        int[] CIDs = aPExportSearch.ClaimIds.Split(',').Select(int.Parse).ToArray();
                        for (int i = 0; i < CIDs.Length; i++)
                        {
                            if (User != null && User.Identity.IsAuthenticated)
                            {
                                int HRPVGCID = Convert.ToInt32(CIDs[i]);

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

                                await _repository.MstHRPVGClaim.UpdateMstHRPVGClaimStatus(HRPVGCID, 4, int.Parse(HttpContext.User.FindFirst("userid").Value), DateTime.Now, aPExportSearch.Reason, string.Empty, string.Empty, string.Empty, string.Empty, isAlternateApprover, 0);
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

                                var rejectReason = aPExportSearch.Reason;
                                var lastApprover = string.Empty;
                                var nextApprover = senderName;

                                BackgroundJob.Enqueue(() => _sendMailServices.SendEmail("Rejected.html", screen, subject, senderName, receiverName, claimNo, approvalType, userID, toEmail, clickUrl, lastApprover, nextApprover, rejectReason));

                                return Json(new { res = "Done" });
                            }
                        }
                    }
                    return Json(new { res = "0" });
                }
            }
            return Json(new { res = "Done" });
        }

        public async Task<JsonResult> UpdateStatusforVoid(string data)
        {
            var aPExportSearch = JsonConvert.DeserializeObject<APExportSearch>(data);
            if (aPExportSearch != null)
            {
                if (aPExportSearch.ModuleName == "MileageClaim")
                {
                    if (!string.IsNullOrEmpty(aPExportSearch.ClaimIds))
                    {
                        int[] CIDs = aPExportSearch.ClaimIds.Split(',').Select(int.Parse).ToArray();
                        for (int i = 0; i < CIDs.Length; i++)
                        {
                            if (User != null && User.Identity.IsAuthenticated)
                            {
                                int MCID = Convert.ToInt32(CIDs[i]);

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

                                if (Convert.ToInt32(aPExportSearch.ApprovedStatus) >= 3)
                                {
                                    await _repository.MstMileageClaim.UpdateMstMileageClaimStatus(MCID, -5, int.Parse(HttpContext.User.FindFirst("userid").Value), DateTime.Now, aPExportSearch.Reason, string.Empty, string.Empty, string.Empty, string.Empty, isAlternateApprover, 0);
                                }
                                else
                                {
                                    await _repository.MstMileageClaim.UpdateMstMileageClaimStatus(MCID, 5, int.Parse(HttpContext.User.FindFirst("userid").Value), DateTime.Now, aPExportSearch.Reason, string.Empty, string.Empty, string.Empty, string.Empty, isAlternateApprover, 0);
                                }
                                return Json(new { res = "Done" });
                            }
                        }
                        return Json(new { res = "0" });
                    }
                }
                else if (aPExportSearch.ModuleName == "ExpenseClaim")
                {
                    if (!string.IsNullOrEmpty(aPExportSearch.ClaimIds))
                    {
                        int[] CIDs = aPExportSearch.ClaimIds.Split(',').Select(int.Parse).ToArray();
                        for (int i = 0; i < CIDs.Length; i++)
                        {
                            if (User != null && User.Identity.IsAuthenticated)
                            {
                                int ECID = Convert.ToInt32(CIDs[i]);

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

                                if (Convert.ToInt32(aPExportSearch.ApprovedStatus) >= 3)
                                {
                                    await _repository.MstExpenseClaim.UpdateMstExpenseClaimStatus(ECID, -5, int.Parse(HttpContext.User.FindFirst("userid").Value), DateTime.Now, aPExportSearch.Reason, string.Empty, string.Empty, string.Empty, string.Empty, isAlternateApprover, 0);
                                }
                                else
                                {
                                    await _repository.MstExpenseClaim.UpdateMstExpenseClaimStatus(ECID, 5, int.Parse(HttpContext.User.FindFirst("userid").Value), DateTime.Now, aPExportSearch.Reason, string.Empty, string.Empty, string.Empty, string.Empty, isAlternateApprover, 0);
                                }
                                return Json(new { res = "Done" });
                            }
                            else
                            {
                                //return Json(new { res = "Done" });
                            }
                        }
                    }
                }
                else if (aPExportSearch.ModuleName == "TelephoneBillClaim")
                {
                    if (!string.IsNullOrEmpty(aPExportSearch.ClaimIds))
                    {
                        int[] CIDs = aPExportSearch.ClaimIds.Split(',').Select(int.Parse).ToArray();
                        for (int i = 0; i < CIDs.Length; i++)
                        {
                            bool isAlternateApprover = false;
                            if (User != null && User.Identity.IsAuthenticated)
                            {
                                int TBCID = Convert.ToInt32(CIDs[i]);

                                var mstTBClaim = await _repository.MstTBClaim.GetTBClaimByIdAsync(TBCID);

                                if (mstTBClaim == null)
                                {
                                    // return NotFound();
                                }

                                int loggedInUserId = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value);
                                //bool isAlternateApprover = false;
                                var delegatedUserId = await _alternateApproverHelper.IsUserHasAnyAlternateApprovalSet(loggedInUserId);
                                if (delegatedUserId.HasValue)
                                {
                                    isAlternateApprover = true;
                                }

                                if (Convert.ToInt32(aPExportSearch.ApprovedStatus) >= 3)
                                {
                                    await _repository.MstTBClaim.UpdateMstTBClaimStatus(TBCID, -5, int.Parse(HttpContext.User.FindFirst("userid").Value), DateTime.Now, aPExportSearch.Reason, string.Empty, string.Empty, string.Empty, string.Empty, isAlternateApprover, 0);
                                }
                                else
                                {
                                    await _repository.MstTBClaim.UpdateMstTBClaimStatus(TBCID, 5, int.Parse(HttpContext.User.FindFirst("userid").Value), DateTime.Now, aPExportSearch.Reason, string.Empty, string.Empty, string.Empty, string.Empty, isAlternateApprover, 0);
                                }
                                return Json(new { res = "Done" });
                            }
                        }
                        return Json(new { res = "0" });
                    }
                }
                else if (aPExportSearch.ModuleName == "PV-ChequeClaim")
                {
                    if (!string.IsNullOrEmpty(aPExportSearch.ClaimIds))
                    {
                        int[] CIDs = aPExportSearch.ClaimIds.Split(',').Select(int.Parse).ToArray();
                        for (int i = 0; i < CIDs.Length; i++)
                        {
                            bool isAlternateApprover = false;
                            if (User != null && User.Identity.IsAuthenticated)
                            {
                                int PVCCID = Convert.ToInt32(CIDs[i]);

                                var mstPVCClaim = await _repository.MstPVCClaim.GetPVCClaimByIdAsync(PVCCID);

                                if (mstPVCClaim == null)
                                {
                                    // return NotFound();
                                }
                                int loggedInUserId = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value);
                                //bool isAlternateApprover = false;
                                var delegatedUserId = await _alternateApproverHelper.IsUserHasAnyAlternateApprovalSet(loggedInUserId);
                                if (delegatedUserId.HasValue)
                                {
                                    isAlternateApprover = true;
                                }

                                if (Convert.ToInt32(aPExportSearch.ApprovedStatus) >= 3)
                                {
                                    await _repository.MstPVCClaim.UpdateMstPVCClaimStatus(PVCCID, -5, int.Parse(HttpContext.User.FindFirst("userid").Value), DateTime.Now, aPExportSearch.Reason, string.Empty, string.Empty, string.Empty, string.Empty, isAlternateApprover, 0);
                                }
                                else
                                {
                                    await _repository.MstPVCClaim.UpdateMstPVCClaimStatus(PVCCID, 5, int.Parse(HttpContext.User.FindFirst("userid").Value), DateTime.Now, aPExportSearch.Reason, string.Empty, string.Empty, string.Empty, string.Empty, isAlternateApprover, 0);
                                }
                                return Json(new { res = "Done" });
                            }
                        }
                        return Json(new { res = "0" });
                    }
                }
                else if (aPExportSearch.ModuleName == "PV-GiroClaim")
                {
                    if (!string.IsNullOrEmpty(aPExportSearch.ClaimIds))
                    {
                        int[] CIDs = aPExportSearch.ClaimIds.Split(',').Select(int.Parse).ToArray();
                        for (int i = 0; i < CIDs.Length; i++)
                        {
                            long PVGCID = Convert.ToInt64(CIDs[i]);
                            bool isAlternateApprover = false;
                            if (User != null && User.Identity.IsAuthenticated)
                            {
                                //int PVGCID = Convert.ToInt32(id);

                                var mstPVGClaim = await _repository.MstPVGClaim.GetPVGClaimByIdAsync(PVGCID);

                                if (mstPVGClaim == null)
                                {
                                    // return NotFound();
                                }

                                int loggedInUserId = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value);
                                //bool isAlternateApprover = false;
                                var delegatedUserId = await _alternateApproverHelper.IsUserHasAnyAlternateApprovalSet(loggedInUserId);
                                if (delegatedUserId.HasValue)
                                {
                                    isAlternateApprover = true;
                                }

                                if (Convert.ToInt32(aPExportSearch.ApprovedStatus) >= 3)
                                {
                                    await _repository.MstPVGClaim.UpdateMstPVGClaimStatus(PVGCID, -5, int.Parse(HttpContext.User.FindFirst("userid").Value), DateTime.Now, aPExportSearch.Reason, string.Empty, string.Empty, string.Empty, string.Empty, isAlternateApprover, 0);
                                }
                                else
                                {
                                    await _repository.MstPVGClaim.UpdateMstPVGClaimStatus(PVGCID, 5, int.Parse(HttpContext.User.FindFirst("userid").Value), DateTime.Now, aPExportSearch.Reason, string.Empty, string.Empty, string.Empty, string.Empty, isAlternateApprover, 0);
                                }
                                return Json(new { res = "Done" });
                            }
                        }
                        return Json(new { res = "0" });
                    }
                }
                else if (aPExportSearch.ModuleName == "HRPV-ChequeClaim")
                {
                    if (!string.IsNullOrEmpty(aPExportSearch.ClaimIds))
                    {
                        int[] CIDs = aPExportSearch.ClaimIds.Split(',').Select(int.Parse).ToArray();
                        for (int i = 0; i < CIDs.Length; i++)
                        {
                            if (User != null && User.Identity.IsAuthenticated)
                            {
                                int HRPVCCID = Convert.ToInt32(CIDs[i]);

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

                                string financeStartDay = _configuration.GetValue<string>("FinanceStartDay");
                                if (Convert.ToInt32(aPExportSearch.ApprovedStatus) >= 3)
                                {
                                    await _repository.MstHRPVCClaim.UpdateMstHRPVCClaimStatus(HRPVCCID, -5, int.Parse(HttpContext.User.FindFirst("userid").Value), DateTime.Now, aPExportSearch.Reason, string.Empty, string.Empty, string.Empty, string.Empty, isAlternateApprover, int.Parse(financeStartDay));
                                }
                                else
                                {
                                    await _repository.MstHRPVCClaim.UpdateMstHRPVCClaimStatus(HRPVCCID, 5, int.Parse(HttpContext.User.FindFirst("userid").Value), DateTime.Now, aPExportSearch.Reason, string.Empty, string.Empty, string.Empty, string.Empty, isAlternateApprover, int.Parse(financeStartDay));
                                }
                                return Json(new { res = "Done" });
                            }
                        }
                        return Json(new { res = "0" });
                    }
                }
                else
                {
                    if (!string.IsNullOrEmpty(aPExportSearch.ClaimIds))
                    {
                        int[] CIDs = aPExportSearch.ClaimIds.Split(',').Select(int.Parse).ToArray();
                        for (int i = 0; i < CIDs.Length; i++)
                        {
                            if (User != null && User.Identity.IsAuthenticated)
                            {
                                int HRPVGCID = Convert.ToInt32(CIDs[i]);

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

                                if (Convert.ToInt32(aPExportSearch.ApprovedStatus) >= 3)
                                {
                                    await _repository.MstHRPVGClaim.UpdateMstHRPVGClaimStatus(HRPVGCID, -5, int.Parse(HttpContext.User.FindFirst("userid").Value), DateTime.Now, aPExportSearch.Reason, string.Empty, string.Empty, string.Empty, string.Empty, isAlternateApprover, 0);
                                }
                                else
                                {
                                    await _repository.MstHRPVGClaim.UpdateMstHRPVGClaimStatus(HRPVGCID, 5, int.Parse(HttpContext.User.FindFirst("userid").Value), DateTime.Now, aPExportSearch.Reason, string.Empty, string.Empty, string.Empty, string.Empty, isAlternateApprover, 0);
                                }
                                return Json(new { res = "Done" });
                            }
                        }
                    }
                    return Json(new { res = "0" });
                }
            }
            return Json(new { res = "Done" });
        }
        public ActionResult DownloadFile(string fileName)
        {
            byte[] fileByteArray = System.IO.File.ReadAllBytes(fileName);
            System.IO.File.Delete(fileName);
            return File(fileByteArray, "application/vnd.ms-excel", "ExpenseClaims-Export.xlsx");
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
            else if (claim != "" && claim.Equals("pvc"))
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
            else if (claim != "" && claim.Equals("pvg"))
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
            else if (claim != "" && claim.Equals("hrpvc"))
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
            else if (claim != "" && claim.Equals("hrpvg"))
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

    }
}
