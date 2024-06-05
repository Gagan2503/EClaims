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
    public class FinancePVGClaimController : Controller
    {
        private ILoggerManager _logger;
        private IRepositoryWrapper _repository;
        private IMapper _mapper;
        private IConfiguration _configuration;
        private AlternateApproverHelper _alternateApproverHelper;
        private ISendMailServices _sendMailServices;

        private readonly RepositoryContext _context;

        public FinancePVGClaimController(ILoggerManager logger, IRepositoryWrapper repository, IMapper mapper, RepositoryContext context, IConfiguration configuration, ISendMailServices sendMailServices)
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

                var mstPVGClaimsWithDetails = await _repository.MstPVGClaim.GetAllPVGClaimWithDetailsAsync(userID, facilityID, statusID, fromDate, toDate);
                if (mstPVGClaimsWithDetails != null && mstPVGClaimsWithDetails.Any())
                {
                    mstPVGClaimsWithDetails.ToList().ForEach(c => c.IsDelegated = false);
                }

                if (delegatedUserId != null && delegatedUserId.HasValue)
                {
                    var delegatedClaims = await _repository.MstPVGClaim.GetAllPVGClaimWithDetailsAsync(delegatedUserId.Value, facilityID, statusID, fromDate, toDate);
                    if (delegatedClaims != null && delegatedClaims.Any())
                    {
                        delegatedClaims.ToList().ForEach(c => c.IsDelegated = true);
                        mstPVGClaimsWithDetails.ToList().AddRange(delegatedClaims.ToList());
                    }
                }
                _logger.LogInfo($"Returned all PVG Claims with details from database.");
                List<PVGClaimVM> pVGClaimVMs = new List<PVGClaimVM>();
                foreach (var mc in mstPVGClaimsWithDetails)
                {
                    PVGClaimVM pVGClaimVM = new PVGClaimVM();
                    pVGClaimVM.PVGCID = mc.CID;
                    pVGClaimVM.PVGCNo = mc.CNO;
                    pVGClaimVM.Name = mc.Name;
                    pVGClaimVM.CreatedDate = DateTime.ParseExact(mc.CreatedDate, "MM/dd/yyyy HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture)
                                                             .ToString("dd/MM/yyyy", System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
                    pVGClaimVM.FacilityName = mc.FacilityName;
                    pVGClaimVM.Phone = mc.Phone;
                    pVGClaimVM.GrandTotal = mc.GrandTotal;
                    pVGClaimVM.ApprovalStatus = mc.ApprovalStatus;
                    pVGClaimVM.TotalAmount = mc.TotalAmount;
                    pVGClaimVM.StaffName = mc.PayeeName;
                    pVGClaimVM.VoucherNo = mc.VoucherNo;
                    pVGClaimVM.PayeeName = mc.PayeeName;

                    if (mc.UserApprovers != "")
                    {
                        pVGClaimVM.Approver = mc.UserApprovers.Split(',').First();
                        if ((pVGClaimVM.Approver == HttpContext.User.FindFirst("userid").Value || (delegatedUserId.HasValue && pVGClaimVM.Approver == delegatedUserId.Value.ToString())) &&
                            (pVGClaimVM.ApprovalStatus == 6))
                        {
                            pVGClaimVM.IsActionAllowed = false;
                        }
                    }
                    else if (mc.HODApprover != "")
                    {
                        pVGClaimVM.Approver = mc.HODApprover.Split(',').First();
                        if ((pVGClaimVM.Approver == HttpContext.User.FindFirst("userid").Value || (delegatedUserId.HasValue && pVGClaimVM.Approver == delegatedUserId.Value.ToString())) &&
                            (pVGClaimVM.ApprovalStatus == 7))
                        {
                            pVGClaimVM.IsActionAllowed = false;
                        }
                    }
                    else if (mc.Verifier != "")
                    {
                        pVGClaimVM.Approver = mc.Verifier.Split(',').First();
                        if ((pVGClaimVM.Approver == HttpContext.User.FindFirst("userid").Value || (delegatedUserId.HasValue && pVGClaimVM.Approver == delegatedUserId.Value.ToString())) &&
                            (pVGClaimVM.ApprovalStatus == 1 || pVGClaimVM.ApprovalStatus == 2))
                        {
                            pVGClaimVM.IsActionAllowed = true;
                        }
                        //string VerifierIDs = string.Join(",", PVGverifierIDs.Skip(1));
                    }
                    else if (mc.Approver != "")
                    {
                        pVGClaimVM.Approver = mc.Approver.Split(',').First();
                        if ((pVGClaimVM.Approver == HttpContext.User.FindFirst("userid").Value || (delegatedUserId.HasValue && pVGClaimVM.Approver == delegatedUserId.Value.ToString())) &&
                            (pVGClaimVM.ApprovalStatus == 1 || pVGClaimVM.ApprovalStatus == 2))
                        {
                            pVGClaimVM.IsActionAllowed = true;
                        }
                    }
                    else
                    {
                        pVGClaimVM.Approver = "";
                    }

                    if (pVGClaimVM.Approver != "")
                    {
                        var alternateUser = await _alternateApproverHelper.IsAlternateApprovalSetForUser(Convert.ToInt32(pVGClaimVM.Approver));
                        if (alternateUser.HasValue)
                        {
                            var mstUserApprover = await _repository.MstUser.GetUserByIdAsync(alternateUser.Value);
                            pVGClaimVM.Approver = mstUserApprover.Name + " (AA)";
                        }
                        else
                        {
                            var mstUserApprover = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(pVGClaimVM.Approver));
                            pVGClaimVM.Approver = mstUserApprover.Name;
                        }
                    }

                    // Show actions based on alternate approver settings
                    // Override all the isActionAllowed code above. When alternate approval is set, then no need to show the action on any scenario
                    if (isAlternateApproverSet)
                    {
                        pVGClaimVM.IsActionAllowed = false;
                    }

                    pVGClaimVMs.Add(pVGClaimVM);
                }

                var mstPVGClaimVM = new PVGClaimSearchViewModel
                {
                    //Screens = new SelectList(await screenQuery.Distinct().ToListAsync()),
                    pVGClaimVMs = pVGClaimVMs,
                    Statuses = new SelectList(status, "Value", "Text"),
                    Facilities = new SelectList(facilities, "Value", "Text"),
                    Users = new SelectList(users, "Value", "Text"),
                    FromDate = fromDate,
                    ToDate = toDate
                };

                return View(mstPVGClaimVM);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Something went wrong inside GetAllPVGClaimWithDetailsAsync action: {ex.Message}");
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
            long PVGCID = Convert.ToInt64(id);

            if (User != null && User.Identity.IsAuthenticated)
            {
                var mstPVGClaim = await _repository.MstPVGClaim.GetPVGClaimByIdAsync(id);

                if (mstPVGClaim == null)
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
                    dtPVGClaimVM.GSTPercentage = item.GSTPercentage;
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

                pVGClaimDetailVM.PVGClaimFileUploads = _repository.DtPVGClaimFileUpload.GetDtPVGClaimAuditByIdAsync(id).GetAwaiter().GetResult().ToList();

                PVGClaimVM pVGClaimVM = new PVGClaimVM();
                //pVGClaimVM.ClaimType = mstPVGClaim.ClaimType;
                pVGClaimVM.VoucherNo = mstPVGClaim.VoucherNo;
                pVGClaimVM.GrandTotal = mstPVGClaim.GrandTotal;
                pVGClaimVM.TotalAmount = mstPVGClaim.TotalAmount;
                pVGClaimVM.GrandGST = mstPVGClaim.TotalAmount - mstPVGClaim.GrandTotal;
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
                if (mstPVGClaim.Verifier != "")
                {
                    string[] verifierIDs = mstPVGClaim.Verifier.Split(',');
                    TempData["QueryMCVerifierIDs"] = string.Join(",", verifierIDs);
                    foreach (string verifierID in verifierIDs)
                    {
                        if ((verifierID != "" && verifierID == HttpContext.User.FindFirst("userid").Value || (delegatedUserId.HasValue && verifierID == delegatedUserId.Value.ToString())) && User.IsInRole("Finance"))
                        {
                            TempData["ApprovedStatus"] = mstPVGClaim.ApprovalStatus;
                            //Session["ApprovedStatus"] = oSupplierPO.Approvalstatus;
                            TempData["VerifierIDs"] = string.Join(",", verifierIDs.Skip(1));
                            pVGClaimVM.IsActionAllowed = true;
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
                    TempData["VerifierIDs"] = mstPVGClaim.Verifier;
                    TempData["ApproverIDs"] = mstPVGClaim.Approver;
                }

                //Approval Process code
                if (mstPVGClaim.Approver != "" && mstPVGClaim.Verifier == "")
                {
                    string[] approverIDs = mstPVGClaim.Approver.Split(',');
                    TempData["QueryMCApproverIDs"] = string.Join(",", approverIDs);
                    foreach (string approverID in approverIDs)
                    {
                        if ((approverID != "" && approverID == HttpContext.User.FindFirst("userid").Value || (delegatedUserId.HasValue && approverID == delegatedUserId.Value.ToString())) && User.IsInRole("Finance"))
                        {
                            TempData["ApprovedStatus"] = mstPVGClaim.ApprovalStatus;
                            //Session["ApprovedStatus"] = oSupplierPO.Approvalstatus;
                            TempData["ApproverIDs"] = string.Join(",", approverIDs.Skip(1));
                            pVGClaimVM.IsActionAllowed = true;
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


                BindGSTDropdown();
                return View(pVGClaimDetailVM);
            }
            else
            {
                return Redirect("~/Login/Login");
            }
        }

        [HttpPost]
        public async Task<JsonResult> SaveItems(string data)
        {
            //var pVGClaimViewModel = JsonConvert.DeserializeObject<PVGClaimViewModel>(data,
            //    new IsoDateTimeConverter { DateTimeFormat = "dd/MM/yyyy" });

            var pVGClaimViewModel = JsonConvert.DeserializeObject<PVGClaimViewModel>(data);

            var mstFacility = await _repository.MstFacility.GetFacilityWithDepartmentByIdAsync(Convert.ToInt32(HttpContext.User.FindFirst("delegatefacilityid") is null ? HttpContext.User.FindFirst("facilityid").Value : HttpContext.User.FindFirst("delegatefacilityid").Value));



            MstPVGClaim mstPVGClaim = new MstPVGClaim();
            mstPVGClaim.PVGCNo = pVGClaimViewModel.PVGCNo;
            mstPVGClaim.UserID = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
            mstPVGClaim.Verifier = "";
            mstPVGClaim.Approver = "";
            mstPVGClaim.FinalApprover = "";
            mstPVGClaim.ApprovalStatus = 1;
            mstPVGClaim.GrandTotal = pVGClaimViewModel.GrandTotal;
            mstPVGClaim.TotalAmount = pVGClaimViewModel.TotalAmount;
            mstPVGClaim.PaymentMode = pVGClaimViewModel.PaymentMode;
            mstPVGClaim.Company = pVGClaimViewModel.Company;
            mstPVGClaim.FacilityID = Convert.ToInt32(HttpContext.User.FindFirst("delegatefacilityid") is null ? HttpContext.User.FindFirst("facilityid").Value : HttpContext.User.FindFirst("delegatefacilityid").Value);
            mstPVGClaim.DepartmentID = mstFacility.MstDepartment.DepartmentID;
            mstPVGClaim.CreatedDate = DateTime.Now;
            mstPVGClaim.ModifiedDate = DateTime.Now;
            mstPVGClaim.CreatedBy = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
            mstPVGClaim.ModifiedBy = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
            mstPVGClaim.ApprovalDate = DateTime.Now;
            mstPVGClaim.ApprovalBy = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
            mstPVGClaim.TnC = true;

            foreach (var dtItem in pVGClaimViewModel.dtClaims)
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
            long PVGCID = 0;
            try
            {
                //CBRID = Convert.ToInt32(Session["CBRID"].ToString());
                PVGCID = Convert.ToInt64(pVGClaimViewModel.PVGCID);
                if (PVGCID == 0 || TempData["Updatestatus"].ToString() == "Recreate")
                {
                    ClaimStatus = "Recreate";
                    PVGCID = 0;
                }
                else if (PVGCID == 0)
                    ClaimStatus = "Add";
                else
                    ClaimStatus = "Update";
                mstPVGClaim.PVGCID = PVGCID;
                if (pVGClaimViewModel.ClaimAddCondition == "claimDraft")
                {
                    mstPVGClaim.PVGCID = 0;
                }
                else
                {
                    mstPVGClaim.PVGCID = PVGCID;
                }
                //mstPVGClaim.PVGCNo = pVGClaimViewModel.;
            }
            catch { }

            PVGClaimDetailVM pVGClaimDetailVM = new PVGClaimDetailVM();
            //List<DtMileageClaimVM> dtMileageClaimVMs = new List<DtMileageClaimVM>();
            pVGClaimDetailVM.DtPVGClaimVMs = new List<DtPVGClaimVM>();
            // List<clsDtSupplierPO> oclsDtSupplierPO = new List<clsDtSupplierPO>();
            foreach (var item in pVGClaimViewModel.dtClaims)
            {
                DtPVGClaimVM dtPVGClaimVM = new DtPVGClaimVM();

                dtPVGClaimVM.PVGCItemID = item.PVGCItemID;
                if (pVGClaimViewModel.ClaimAddCondition == "claimDraft")
                {
                    dtPVGClaimVM.PVGCID = 0;
                }
                else
                {
                    dtPVGClaimVM.PVGCID = item.PVGCID;
                }

                if (PVGCID == 0 || TempData["Updatestatus"].ToString() == "Recreate")
                {
                    dtPVGClaimVM.PVGCID = 0;
                    dtPVGClaimVM.PVGCItemID = 0;
                }

                dtPVGClaimVM.Payee = item.Payee;
                dtPVGClaimVM.Particulars = item.Particulars;
                dtPVGClaimVM.ExpenseCategory = item.MstExpenseCategory.Description;
                dtPVGClaimVM.ExpenseCategoryID = item.MstExpenseCategory.ExpenseCategoryID;
                dtPVGClaimVM.FacilityID = item.FacilityID;
                //dtPVGClaimVM.Reason = item.Reason;
                //dtPVGClaimVM.EmployeeNo = item.EmployeeNo;
                dtPVGClaimVM.ChequeNo = item.ChequeNo;
                dtPVGClaimVM.Amount = item.Amount;
                dtPVGClaimVM.GST = item.GST;
                dtPVGClaimVM.GSTPercentage = item.GSTPercentage;
                dtPVGClaimVM.AmountWithGST = item.Amount + item.GST;
                //dtPVGClaimVM.Facility = item.Facility;
                dtPVGClaimVM.AccountCode = item.AccountCode;
                dtPVGClaimVM.Date = item.Date;
                pVGClaimDetailVM.DtPVGClaimVMs.Add(dtPVGClaimVM);
            }

            var GroupByQS = pVGClaimDetailVM.DtPVGClaimVMs.GroupBy(s => new { s.AccountCode, s.ExpenseCategory, s.FacilityID, s.GST });

            pVGClaimDetailVM.DtPVGClaimVMSummary = new List<DtPVGClaimVM>();

            foreach (var group in GroupByQS)
            {
                DtPVGClaimVM dtPVGClaimVM = new DtPVGClaimVM();
                decimal amount = 0;
                decimal gst = 0;
                decimal gstpercentage = 0;
                decimal sumamount = 0;
                string ExpenseDesc = string.Empty;
                string Facility = string.Empty;
                string ExpenseCat = string.Empty;
                string AccountCode = string.Empty;
                int? ExpenseCatID = 0;
                int? facilityID = 0;
                int i = 0;
                foreach (var dtExpense in group)
                {
                    if (i == 0)
                        ExpenseDesc = dtExpense.Particulars;
                    i++;
                    amount = amount + dtExpense.Amount;
                    gst = gst + dtExpense.GST;
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
                dtPVGClaimVM.Particulars = ExpenseDesc;
                dtPVGClaimVM.ExpenseCategory = ExpenseCat;
                dtPVGClaimVM.ExpenseCategoryID = ExpenseCatID;
                dtPVGClaimVM.FacilityID = facilityID;
                dtPVGClaimVM.Facility = Facility;
                dtPVGClaimVM.AccountCode = AccountCode;
                dtPVGClaimVM.Amount = amount;
                dtPVGClaimVM.GST = gst;
                dtPVGClaimVM.GSTPercentage = gstpercentage;
                dtPVGClaimVM.AmountWithGST = sumamount;
                pVGClaimDetailVM.DtPVGClaimVMSummary.Add(dtPVGClaimVM);
            }
            List<DtPVGClaimSummary> lstPVGClaimSummary = new List<DtPVGClaimSummary>();
            foreach (var item in pVGClaimDetailVM.DtPVGClaimVMSummary)
            {
                DtPVGClaimSummary dtPVGClaimSummary1 = new DtPVGClaimSummary();
                dtPVGClaimSummary1.AccountCode = item.AccountCode;
                dtPVGClaimSummary1.Amount = item.Amount;
                dtPVGClaimSummary1.ExpenseCategory = item.ExpenseCategory;
                dtPVGClaimSummary1.Description = item.Particulars.ToUpper();
                dtPVGClaimSummary1.ExpenseCategoryID = item.ExpenseCategoryID;
                dtPVGClaimSummary1.FacilityID = item.FacilityID;
                dtPVGClaimSummary1.Facility = item.Facility;
                dtPVGClaimSummary1.GST = item.GST;
                dtPVGClaimSummary1.GSTPercentage = item.GSTPercentage;
                if (item.GST != 0)
                {
                    dtPVGClaimSummary1.TaxClass = Math.Round((decimal)item.GSTPercentage, (int)1);
                }
                else
                {
                    dtPVGClaimSummary1.TaxClass = 4;
                }
                dtPVGClaimSummary1.AmountWithGST = item.AmountWithGST;
                lstPVGClaimSummary.Add(dtPVGClaimSummary1);
            }

            DtPVGClaimSummary dtPVGClaimSummary = new DtPVGClaimSummary();
            dtPVGClaimSummary.AccountCode = "425000";
            dtPVGClaimSummary.Amount = mstPVGClaim.GrandTotal;
            dtPVGClaimSummary.GST = mstPVGClaim.TotalAmount - mstPVGClaim.GrandTotal;
            dtPVGClaimSummary.AmountWithGST = mstPVGClaim.TotalAmount;
            dtPVGClaimSummary.TaxClass = 0;
            dtPVGClaimSummary.ExpenseCategory = "DBS";
            dtPVGClaimSummary.Description = "";
            lstPVGClaimSummary.Add(dtPVGClaimSummary);

            var res = await _repository.MstPVGClaim.SaveItems(mstPVGClaim, pVGClaimViewModel.dtClaims, lstPVGClaimSummary);
            /*
            _context.Connection.Open();
            using (var transaction = _context.Connection.BeginTransaction())
            {
                try
                {
                    _context.Database.UseTransaction(transaction as DbTransaction);
                    //Check if Department Exists (By Name)
                    /*
                    bool DepartmentExists = await _dbContext.Departments.AnyAsync(a => a.Name == employeeDto.Department.Name);
                    if (DepartmentExists)
                    {
                        throw new Exception("Department Already Exists");
                    }

                    //Add Department
                    var addMstClaimQuery = $"INSERT INTO MstMileageClaim( MCNo, UserID, TravelMode, Verifier, Approver, FinalApprover, ApprovalStatus, GrandTotal, Company, DepartmentID, FacilityID, CreatedDate, ModifiedDate, CreatedBy, ModifiedBy, ApprovalDate, ApprovalBy, TnC) VALUES('MC000001',2,'{mileageClaimViewModel.TravelMode}','1,2','2,8',8,1,'{mileageClaimViewModel.GrandTotal}','{mileageClaimViewModel.Company}',1,1,'{DateTime.Now}','{DateTime.Now}',2,2,'{DateTime.Now}',2,1);SELECT CAST(SCOPE_IDENTITY() as int)";
                    var mstClaimId = await _writeDbConnection.QuerySingleAsync<int>(addMstClaimQuery, transaction: transaction);
                    //Check if Department Id is not Zero.
                    if (mstClaimId == 0)
                    {
                        throw new Exception("Mileage Id");
                    }

                    foreach(var dtMileageClaim1 in mileageClaimViewModel.dtClaims)
                    {
                        dtMileageClaim1.MCID = mstClaimId;
                        await _context.dtMileageClaim.AddAsync(dtMileageClaim1);
                        await _context.SaveChangesAsync(default);
                        transaction.Commit();
                        return Json(new { res = true });
                    }
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    throw;
                }
                finally
                {
                    _context.Connection.Close();
                }
            }
                */
            if (res != 0)
            {
                if (ClaimStatus == "Add" || ClaimStatus == "Recreate")
                {
                    mstPVGClaim = await _repository.MstPVGClaim.GetPVGClaimByIdAsync(res);
                    if (mstPVGClaim.ApprovalStatus == 7)
                    {
                        string VerifierIDs = "";
                        string ApproverIDs = "";
                        string UserApproverIDs = "";
                        string HODApproverID = "";
                        try
                        {
                            //VerifierIDs = mstPVGClaim.Verifier.Split(',');
                            //VerifierIDs = string.Join(",", ExpenseverifierIDs.Skip(1));
                            string[] hODApproverIDs = mstPVGClaim.HODApprover.Split(',');
                            ApproverIDs = mstPVGClaim.Approver;
                            //HODApproverID = mstPVGClaim.HODApprover;



                            //BackgroundJob.Enqueue(() => _sendMailServices.SendEmail());
                            //Mail Code Implementation for Verifiers

                            foreach (string hODApproverID in hODApproverIDs)
                            {
                                if (hODApproverID != "")
                                {
                                    string domainUrl = HttpContext.Request.Scheme + "://" + HttpContext.Request.Host;
                                    string clickUrl = domainUrl + "/" + "HodSummary/PVGCDetails/" + mstPVGClaim.PVGCID;

                                    var mstSenderDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value));
                                    var senderName = mstSenderDetails.Name;
                                    var mstVerifierDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(hODApproverID));
                                    var toEmail = mstVerifierDetails.EmailAddress;
                                    var receiverName = mstVerifierDetails.Name;
                                    var claimNo = mstPVGClaim.PVGCNo;
                                    var screen = "PV-GIRO Claim";
                                    var approvalType = "Approval Request";
                                    int userID = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
                                    var subject = "PV-GIRO Claim for Approval " + claimNo;

                                    BackgroundJob.Enqueue(() => _sendMailServices.SendEmail("EmailTemplate.html", screen, subject, senderName, receiverName, claimNo, approvalType, userID, toEmail, clickUrl));
                                }
                                break;
                            }
                        }
                        catch
                        {
                        }
                    }
                    else
                    {
                        string[] userApproverIDs = mstPVGClaim.UserApprovers.ToString().Split(',');
                        foreach (string userApproverID in userApproverIDs)
                        {
                            if (userApproverID != "")
                            {
                                string domainUrl = HttpContext.Request.Scheme + "://" + HttpContext.Request.Host;
                                string clickUrl = domainUrl + "/" + "HodSummary/PVGCDetails/" + mstPVGClaim.PVGCID;

                                var mstSenderDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value));
                                var senderName = mstSenderDetails.Name;
                                var mstVerifierDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(userApproverID));
                                var toEmail = mstVerifierDetails.EmailAddress;
                                var receiverName = mstVerifierDetails.Name;
                                var claimNo = mstPVGClaim.PVGCNo;
                                var screen = "PV-GIRO Claim";
                                var approvalType = "Approval Request";
                                int userID = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
                                var subject = "PV-GIRO Claim for Approval " + claimNo;

                                BackgroundJob.Enqueue(() => _sendMailServices.SendEmail("EmailTemplate.html", screen, subject, senderName, receiverName, claimNo, approvalType, userID, toEmail, clickUrl));
                            }
                            break;
                        }
                    }
                    TempData["Message"] = "PV-Giro Claim added successfully";
                }
                else
                {
                    mstPVGClaim = await _repository.MstPVGClaim.GetPVGClaimByIdAsync(res);
                    if (mstPVGClaim.ApprovalStatus == 1)
                    {
                        string VerifierIDs = "";
                        string ApproverIDs = "";
                        string UserApproverIDs = "";
                        string HODApproverID = "";
                        try
                        {
                            //VerifierIDs = mstPVGClaim.Verifier.Split(',');
                            //VerifierIDs = string.Join(",", ExpenseverifierIDs.Skip(1));
                            string[] verifierIDs = mstPVGClaim.Verifier.Split(',');
                            ApproverIDs = mstPVGClaim.Approver;
                            HODApproverID = mstPVGClaim.HODApprover;



                            //BackgroundJob.Enqueue(() => _sendMailServices.SendEmail());
                            //Mail Code Implementation for Verifiers

                            foreach (string verifierID in verifierIDs)
                            {
                                if (verifierID != "")
                                {
                                    string domainUrl = HttpContext.Request.Scheme + "://" + HttpContext.Request.Host;
                                    string clickUrl = domainUrl + "/" + "FinancePVGClaim/Details/" + mstPVGClaim.PVGCID;

                                    var mstSenderDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value));
                                    var senderName = mstSenderDetails.Name;
                                    var mstVerifierDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(verifierID));
                                    var toEmail = mstVerifierDetails.EmailAddress;
                                    var receiverName = mstVerifierDetails.Name;
                                    var claimNo = mstPVGClaim.PVGCNo;
                                    var screen = "PV-GIRO Claim";
                                    var approvalType = "Verification Request";
                                    int userID = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
                                    var subject = "PV-GIRO Claim for Verification " + claimNo;

                                    BackgroundJob.Enqueue(() => _sendMailServices.SendEmail("EmailTemplate.html", screen, subject, senderName, receiverName, claimNo, approvalType, userID, toEmail, clickUrl));
                                }
                                break;
                            }
                        }
                        catch
                        {
                        }
                    }
                    else if (mstPVGClaim.ApprovalStatus == 6)
                    {
                        string[] userApproverIDs = mstPVGClaim.UserApprovers.ToString().Split(',');
                        foreach (string userApproverID in userApproverIDs)
                        {
                            if (userApproverID != "")
                            {
                                string domainUrl = HttpContext.Request.Scheme + "://" + HttpContext.Request.Host;
                                string clickUrl = domainUrl + "/" + "HodSummary/PVGCDetails/" + mstPVGClaim.PVGCID;

                                var mstSenderDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value));
                                var senderName = mstSenderDetails.Name;
                                var mstVerifierDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(userApproverID));
                                var toEmail = mstVerifierDetails.EmailAddress;
                                var receiverName = mstVerifierDetails.Name;
                                var claimNo = mstPVGClaim.PVGCNo;
                                var screen = "PV-GIRO Claim";
                                var approvalType = "Approval Request";
                                int userID = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
                                var subject = "PV-GIRO Claim for Approval " + claimNo;

                                BackgroundJob.Enqueue(() => _sendMailServices.SendEmail("EmailTemplate.html", screen, subject, senderName, receiverName, claimNo, approvalType, userID, toEmail, clickUrl));
                            }
                            break;
                        }
                    }
                    else if (mstPVGClaim.ApprovalStatus == 7)
                    {
                        string[] hODApproverIDs = mstPVGClaim.HODApprover.ToString().Split(',');
                        foreach (string hODApproverID in hODApproverIDs)
                        {
                            if (hODApproverID != "")
                            {
                                string domainUrl = HttpContext.Request.Scheme + "://" + HttpContext.Request.Host;
                                string clickUrl = domainUrl + "/" + "HodSummary/PVGCDetails/" + mstPVGClaim.PVGCID;

                                var mstSenderDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value));
                                var senderName = mstSenderDetails.Name;
                                var mstVerifierDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(hODApproverID));
                                var toEmail = mstVerifierDetails.EmailAddress;
                                var receiverName = mstVerifierDetails.Name;
                                var claimNo = mstPVGClaim.PVGCNo;
                                var screen = "PV-GIRO Claim";
                                var approvalType = "Approval Request";
                                int userID = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
                                var subject = "PV-GIRO Claim for Approval " + claimNo;

                                BackgroundJob.Enqueue(() => _sendMailServices.SendEmail("EmailTemplate.html", screen, subject, senderName, receiverName, claimNo, approvalType, userID, toEmail, clickUrl));
                            }
                            break;
                        }
                    }
                    else
                    {
                        string[] ExpenseapproverIDs = mstPVGClaim.Approver.ToString().Split(',');
                        foreach (string approverID in ExpenseapproverIDs)
                        {
                            if (approverID != "")
                            {
                                string domainUrl = HttpContext.Request.Scheme + "://" + HttpContext.Request.Host;
                                string clickUrl = domainUrl + "/" + "FinancePVGClaim/Details/" + mstPVGClaim.PVGCID;

                                var mstSenderDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value));
                                var senderName = mstSenderDetails.Name;
                                var mstVerifierDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(approverID));
                                var toEmail = mstVerifierDetails.EmailAddress;
                                var receiverName = mstVerifierDetails.Name;
                                var claimNo = mstPVGClaim.PVGCNo;
                                var screen = "PV-GIRO Claim";
                                var approvalType = "Approval Request";
                                int userID = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
                                var subject = "PV-GIRO Claim for Approval " + claimNo;

                                BackgroundJob.Enqueue(() => _sendMailServices.SendEmail("EmailTemplate.html", screen, subject, senderName, receiverName, claimNo, approvalType, userID, toEmail, clickUrl));
                            }
                            break;
                        }
                    }
                    TempData["Message"] = "PV-Giro Claim updated successfully";
                }

                return Json(new { res });
            }
            else
                return Json(new { res });
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

        public async Task<IActionResult> Create(string id, string Updatestatus)
        {
            //TempData["CBRID"] = 0;
            TempData["Updatestatus"] = "Add";
            PVGClaimDetailVM pVGClaimDetailVM = new PVGClaimDetailVM();
            pVGClaimDetailVM.DtPVGClaimVMs = new List<DtPVGClaimVM>();
            pVGClaimDetailVM.PVGClaimAudits = new List<PVGClaimAuditVM>();
            TempData["claimaddcondition"] = "claimnew";
            long idd = 0;
            if (User != null && User.Identity.IsAuthenticated)
            {
                if (!string.IsNullOrEmpty(id))
                {
                    idd = Convert.ToInt64(id);
                    ViewBag.CID = idd;
                    var dtPVGClaims = await _repository.DtPVGClaim.GetDtPVGClaimByIdAsync(idd);

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
                        dtPVGClaimVM.GSTPercentage = item.GSTPercentage;
                        dtPVGClaimVM.AmountWithGST = item.Amount + item.GST;
                        dtPVGClaimVM.ExpenseCategory = item.MstExpenseCategory.Description;
                        dtPVGClaimVM.AccountCode = item.AccountCode;
                        if (Updatestatus == "Recreate")
                        {
                            ViewBag.UpdateStatus = "Recreate";
                            dtPVGClaimVM.PVGCItemID = 0;
                        }
                        dtPVGClaimVM.Date = item.Date;
                        dtPVGClaimVM.Bank = item.Bank;
                        dtPVGClaimVM.BankCode = item.BankCode;
                        dtPVGClaimVM.BranchCode = item.BranchCode;
                        dtPVGClaimVM.BankAccount = item.BankAccount;
                        dtPVGClaimVM.Mobile = item.Mobile;
                        pVGClaimDetailVM.DtPVGClaimVMs.Add(dtPVGClaimVM);
                    }

                    pVGClaimDetailVM.PVGClaimFileUploads = new List<DtPVGClaimFileUpload>();

                    pVGClaimDetailVM.PVGClaimFileUploads = await _repository.DtPVGClaimFileUpload.GetDtPVGClaimAuditByIdAsync(idd);

                    //pVGClaimDetailVM.PVGClaimFileUploads = new List<DtPVGClaimFileUpload>();
                    //var fileUploads = await _repository.DtPVGClaimFileUpload.GetDtPVGClaimAuditByIdAsync(idd);
                    //if (Updatestatus == "Recreate" && fileUploads != null && fileUploads.Count > 0)
                    //{
                    //    foreach (var uploaddata in fileUploads)
                    //    {
                    //        uploaddata.PVGCID = 0;
                    //        pVGClaimDetailVM.PVGClaimFileUploads.Add(uploaddata);
                    //    }
                    //}

                    var mstPVGClaim = await _repository.MstPVGClaim.GetPVGClaimByIdAsync(idd);


                    PVGClaimVM pVGClaimVM = new PVGClaimVM();
                    pVGClaimVM.GrandTotal = mstPVGClaim.GrandTotal;
                    pVGClaimVM.TotalAmount = mstPVGClaim.TotalAmount;
                    pVGClaimVM.GrandGST = pVGClaimVM.TotalAmount - pVGClaimVM.GrandTotal;
                    pVGClaimVM.Company = mstPVGClaim.Company;
                    pVGClaimVM.Name = mstPVGClaim.MstUser.Name;
                    pVGClaimVM.DepartmentName = mstPVGClaim.MstDepartment.Department;
                    pVGClaimVM.FacilityName = mstPVGClaim.MstFacility.FacilityName;
                    pVGClaimVM.CreatedDate = mstPVGClaim.CreatedDate.ToString("d");
                    pVGClaimVM.Verifier = mstPVGClaim.Verifier;
                    pVGClaimVM.Approver = mstPVGClaim.Approver;
                    pVGClaimVM.PVGCNo = mstPVGClaim.PVGCNo;
                    pVGClaimVM.PaymentMode = mstPVGClaim.PaymentMode;

                    pVGClaimDetailVM.PVGClaimVM = pVGClaimVM;

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
                    pVGClaimDetailVM.PVGClaimAudits = new List<PVGClaimAuditVM>();
                    pVGClaimDetailVM.PVGClaimFileUploads = new List<DtPVGClaimFileUpload>();
                    PVGClaimVM pVGClaimVM = new PVGClaimVM();
                    pVGClaimVM.GrandTotal = 0;
                    pVGClaimVM.TotalAmount = 0;
                    pVGClaimVM.GrandGST = 0;
                    pVGClaimVM.Company = "";
                    pVGClaimVM.Name = "";
                    pVGClaimVM.DepartmentName = "";
                    pVGClaimVM.FacilityName = "";
                    pVGClaimVM.CreatedDate = "";
                    pVGClaimVM.Verifier = "";
                    pVGClaimVM.Approver = "";
                    pVGClaimVM.PVGCNo = "";

                    DtPVGClaimVM dtPVGClaimVM = new DtPVGClaimVM();

                    dtPVGClaimVM.PVGCItemID = 0;
                    dtPVGClaimVM.PVGCID = 0;
                    //dtPVGClaimVM.DateOfJourney = "";

                    dtPVGClaimVM.ChequeNo = "";
                    dtPVGClaimVM.Particulars = "";
                    dtPVGClaimVM.Payee = "";
                    dtPVGClaimVM.InvoiceNo = "";
                    dtPVGClaimVM.Amount = 0;
                    dtPVGClaimVM.GST = 0;
                    dtPVGClaimVM.AmountWithGST = 0;
                    dtPVGClaimVM.ExpenseCategory = "";
                    dtPVGClaimVM.AccountCode = "";
                    dtPVGClaimVM.Bank = "";
                    dtPVGClaimVM.BankCode = "";
                    dtPVGClaimVM.BranchCode = "";
                    dtPVGClaimVM.BankAccount = "";
                    dtPVGClaimVM.Mobile = "";

                    pVGClaimDetailVM.DtPVGClaimVMs.Add(dtPVGClaimVM);
                    pVGClaimDetailVM.PVGClaimVM = pVGClaimVM;


                    TempData["status"] = "Add";
                }
                //int userFacilityId = mstUsersWithDetails.MstFacility.FacilityID;
                int userFacilityId = Convert.ToInt32(HttpContext.User.FindFirst("delegatefacilityid") is null ? HttpContext.User.FindFirst("facilityid").Value : HttpContext.User.FindFirst("delegatefacilityid").Value);
                var currFacility = await _repository.MstFacility.GetFacilityWithDepartmentByIdAsync(userFacilityId);
                ViewData["ExpenseCategoryID"] = new SelectList(await _repository.MstExpenseCategory.GetAllExpenseCategoriesByClaimTypesAsync("expense/pv-cheque/pv-giro", "active"), "ExpenseCategoryID", "Description");
                var mstUsersWithDetails = await _repository.MstUser.GetUserWithDetailsByIdAsync(Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value));
                var delegatedUserName = string.Empty;
                if (HttpContext.User.FindFirst("delegateuserid") is not null)
                {
                    var delUserDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid").Value));
                    delegatedUserName = delUserDetails.Name;
                }

                ViewData["Name"] = string.IsNullOrEmpty(delegatedUserName) ? User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.GivenName).Value : delegatedUserName + "(" + User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.GivenName).Value + ")";
                ViewData["FacilityName"] = currFacility.FacilityName;
                ViewData["Department"] = currFacility.MstDepartment.Department;
                SelectList facilities = new SelectList(await _repository.MstFacility.GetAllFacilityAsync("active"), "FacilityID", "FacilityName");
                var userFacility = facilities.Where(x => x.Value == userFacilityId.ToString()).FirstOrDefault();
                if (userFacility != null)
                {
                    facilities.Where(x => x.Value == userFacilityId.ToString()).FirstOrDefault().Selected = true;
                }
                ViewData["FacilityID"] = facilities;

                SelectList bankSwiftBICs = new SelectList(await _repository.MstBankSwiftBIC.GetAllBankSwiftBICAsync(), "BankCode", "BankName");
                ViewData["BankSwiftBICs"] = bankSwiftBICs;
                BindGSTDropdown();
                string financeGstValueBuffer = _configuration.GetValue<string>("FinanceGstValueBuffer");
                ViewBag.FinanceGstValueBuffer = financeGstValueBuffer;
            }
            return View(pVGClaimDetailVM);

        }

        public async Task<string> GetBankSwiftBIC(long bankCode)
        {
            var mstBankSwiftBIC = await _repository.MstBankSwiftBIC.GetBankSwiftBICByBankCodeAsync(bankCode);
            if (mstBankSwiftBIC != null)
                return mstBankSwiftBIC.BankSwiftBIC;
            else
                return string.Empty;
        }

        public async Task<ActionResult> DeletePVGClaimFile(string fileID, string filepath, string PVGCID)
        {
            DtPVGClaimFileUpload dtPVGClaimFileUpload = new DtPVGClaimFileUpload();
            if (CloudStorageAccount.TryParse(_configuration.GetSection("ConnectionStrings")["BlobConnectionString"], out CloudStorageAccount storageAccount))
            {
                CloudBlobClient BlobClient = storageAccount.CreateCloudBlobClient();
                CloudBlobContainer container = BlobClient.GetContainerReference(_configuration.GetSection("ConnectionStrings")["BlobContainerName"]);

                if (await container.ExistsAsync())
                {
                    CloudBlob file = container.GetBlobReference("FileUploads/PVGClaimFiles/" + filepath);

                    if (await file.ExistsAsync())
                    {
                        await file.DeleteIfExistsAsync();
                        dtPVGClaimFileUpload = await _repository.DtPVGClaimFileUpload.GetDtPVGClaimFileUploadByIdAsync(Convert.ToInt64(fileID));
                        _repository.DtPVGClaimFileUpload.DeleteDtPVGClaimFileUpload(dtPVGClaimFileUpload);
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

            return RedirectToAction("Create", "FinancePVGClaim", new
            {
                id = PVGCID,
                Updatestatus = "Edit"
            });
        }
        public async Task<JsonResult> GetTextValuesSG(string id)
        {
            List<DtPVGClaimVM> oDtClaimsList = new List<DtPVGClaimVM>();

            try
            {
                var dtPVGClaims = await _repository.DtPVGClaim.GetDtPVGClaimByIdAsync(Convert.ToInt64(id));
                foreach (var item in dtPVGClaims)
                {
                    DtPVGClaimVM dtPVGClaimVM = new DtPVGClaimVM();
                    dtPVGClaimVM.Date = item.Date;
                    dtPVGClaimVM.PVGCItemID = item.PVGCItemID;
                    dtPVGClaimVM.PVGCID = item.PVGCID;
                    dtPVGClaimVM.InvoiceNo = item.InvoiceNo;
                    dtPVGClaimVM.Particulars = item.Particulars;
                    dtPVGClaimVM.Payee = item.Payee;
                    dtPVGClaimVM.ChequeNo = item.ChequeNo;
                    dtPVGClaimVM.Amount = item.Amount;
                    dtPVGClaimVM.GST = item.GST;
                    dtPVGClaimVM.GSTPercentage = item.GSTPercentage;
                    dtPVGClaimVM.AmountWithGST = item.Amount + item.GST;
                    dtPVGClaimVM.ExpenseCategoryID = item.ExpenseCategoryID;
                    dtPVGClaimVM.AccountCode = item.AccountCode;
                    dtPVGClaimVM.Date = item.Date;
                    dtPVGClaimVM.Bank = item.Bank;
                    dtPVGClaimVM.BankCode = item.BankCode;
                    dtPVGClaimVM.BankSWIFTBIC = item.BankSwiftBIC;
                    dtPVGClaimVM.BranchCode = item.BranchCode;
                    dtPVGClaimVM.BankAccount = item.BankAccount;
                    dtPVGClaimVM.Mobile = item.Mobile;
                    dtPVGClaimVM.FacilityID = item.FacilityID;
                    oDtClaimsList.Add(dtPVGClaimVM);
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
            List<DtPVGClaimVM> oDtClaimsList = new List<DtPVGClaimVM>();

            try
            {
                var dtPVGClaims = await _repository.DtPVGClaimDraft.GetDtPVGClaimDraftByIdAsync(Convert.ToInt64(id));


                // List<clsDtSupplierPO> oclsDtSupplierPO = new List<clsDtSupplierPO>();
                foreach (var item in dtPVGClaims)
                {
                    DtPVGClaimVM dtPVGClaimVM = new DtPVGClaimVM();
                    dtPVGClaimVM.Date = item.Date;
                    dtPVGClaimVM.PVGCItemID = item.PVGCItemID;
                    dtPVGClaimVM.PVGCID = item.PVGCID;
                    dtPVGClaimVM.InvoiceNo = item.InvoiceNo;
                    dtPVGClaimVM.Particulars = item.Particulars;
                    dtPVGClaimVM.Payee = item.Payee;
                    dtPVGClaimVM.ChequeNo = item.ChequeNo;
                    dtPVGClaimVM.Amount = item.Amount;
                    dtPVGClaimVM.GST = item.GST;
                    dtPVGClaimVM.AmountWithGST = item.Amount + item.GST;
                    dtPVGClaimVM.ExpenseCategoryID = item.ExpenseCategoryID;
                    dtPVGClaimVM.AccountCode = item.AccountCode;
                    dtPVGClaimVM.Date = item.Date;
                    dtPVGClaimVM.Bank = item.Bank;
                    dtPVGClaimVM.BankCode = item.BankCode;
                    dtPVGClaimVM.BankSWIFTBIC = item.BankSwiftBIC;
                    dtPVGClaimVM.BranchCode = item.BranchCode;
                    dtPVGClaimVM.BankAccount = item.BankAccount;
                    dtPVGClaimVM.Mobile = item.Mobile;
                    dtPVGClaimVM.FacilityID = item.FacilityID;
                    oDtClaimsList.Add(dtPVGClaimVM);
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
            List<DtPVGClaimSummary> oDtClaimsSummaryList = new List<DtPVGClaimSummary>();

            try
            {
                var dtPVGClaimSummaries = await _repository.DtPVGClaimSummary.GetDtPVGClaimSummaryByIdAsync(Convert.ToInt64(id));

                // List<clsDtSupplierPO> oclsDtSupplierPO = new List<clsDtSupplierPO>();
                //foreach (var item in dtPVGClaimSummaries)
                //{
                //    DtPVGClaimVM dtPVGClaimVM = new DtPVGClaimVM();

                //    dtPVGClaimVM.PVGCItemID = item.PVGCItemID;
                //    dtPVGClaimVM.PVGCID = item.PVGCID;
                //    dtPVGClaimVM.StaffName = item.StaffName;
                //    dtPVGClaimVM.Reason = item.Reason;
                //    dtPVGClaimVM.EmployeeNo = item.EmployeeNo;
                //    dtPVGClaimVM.ChequeNo = item.ChequeNo;
                //    dtPVGClaimVM.Amount = item.Amount;
                //    dtPVGClaimVM.GST = item.GST;
                //    dtPVGClaimVM.AmountWithGST = item.Amount + item.GST;
                //    dtPVGClaimVM.Facility = item.Facility;
                //    dtPVGClaimVM.AccountCode = item.AccountCode;
                //    //dtPVGClaimVM.FacilityID = item.FacilityID;
                //    oDtClaimsList.Add(dtPVGClaimVM);
                //}
                return Json(new { DtClaimsList = dtPVGClaimSummaries });
            }
            catch
            {
                return Json(new { DtClaimsList = oDtClaimsSummaryList });
            }

        }

        [HttpPost]
        public async Task<JsonResult> SaveSummary(string data)
        {
            var pVGClaimViewModel = JsonConvert.DeserializeObject<DtPVGClaimSummaryVM>(data);
            var pVGCSummary = await _repository.DtPVGClaimSummary.GetDtPVGClaimSummaryByIdAsync(pVGClaimViewModel.PVGCID);
            foreach (var hr in pVGCSummary)
            {
                _repository.DtPVGClaimSummary.Delete(hr);
            }

            foreach (var dtItem in pVGClaimViewModel.dtClaims)
            {
                if (dtItem.ExpenseCategory != "DBS")
                {
                    dtItem.Description = dtItem.Description.ToUpper();
                    var mstFacility1 = await _repository.MstFacility.GetFacilityWithDepartmentByIdAsync(Convert.ToInt32(dtItem.FacilityID));

                    var mstExpenseCategory = await _repository.MstExpenseCategory.GetExpenseCategoryWithTypesByIdAsync(dtItem.ExpenseCategoryID);

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

            MstPVGClaimAudit auditUpdate = new MstPVGClaimAudit();
            auditUpdate.PVGCID = pVGClaimViewModel.PVGCID;
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
            var res = await _repository.MstPVGClaim.SaveSummary(pVGClaimViewModel.PVGCID, pVGClaimViewModel.dtClaims, auditUpdate);

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
                int PVGCID = Convert.ToInt32(id);

                var mstPVGClaim = await _repository.MstPVGClaim.GetPVGClaimByIdAsync(PVGCID);

                if (mstPVGClaim == null)
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
                    await _repository.MstPVGClaim.UpdateMstPVGClaimStatus(PVGCID, -5, int.Parse(HttpContext.User.FindFirst("userid").Value), DateTime.Now, reason, string.Empty, string.Empty, string.Empty, string.Empty, isAlternateApprover, 0);
                }
                else
                {
                    await _repository.MstPVGClaim.UpdateMstPVGClaimStatus(PVGCID, 5, int.Parse(HttpContext.User.FindFirst("userid").Value), DateTime.Now, reason, string.Empty, string.Empty, string.Empty, string.Empty, isAlternateApprover, 0);
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

        public async Task<JsonResult> ExporttoExcel(string data)
        {
            var mileageClaimSearch = JsonConvert.DeserializeObject<MileageClaimSearch>(data);

            var mstPVGClaimsWithDetails = await _repository.MstPVGClaim.GetAllPVGClaimWithDetailsAsync(mileageClaimSearch.UserID, mileageClaimSearch.FacilityID, mileageClaimSearch.StatusID, mileageClaimSearch.FromDate, mileageClaimSearch.ToDate);

            List<PVGClaimVM> pVGClaimVMs = new List<PVGClaimVM>();

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





            foreach (var mc in mstPVGClaimsWithDetails)
            {
                PVGClaimVM pVGClaimVM = new PVGClaimVM();
                pVGClaimVM.ApprovalStatus = mc.ApprovalStatus;

                if (mc.ApprovalStatus == 1)
                {
                    pVGClaimVM.ExpenseStatusName = "Awaiting Verification";

                }
                else if (mc.ApprovalStatus == 2)
                {
                    pVGClaimVM.ExpenseStatusName = "Awaiting Signatory approval";

                }
                else if (mc.ApprovalStatus == 3)
                {
                    pVGClaimVM.ExpenseStatusName = "Approved";

                }
                else if (mc.ApprovalStatus == 4)
                {
                    pVGClaimVM.ExpenseStatusName = "Request to Amend";
                }
                else if (mc.ApprovalStatus == 5)
                {
                    pVGClaimVM.ExpenseStatusName = "Voided";

                }
                else if (mc.ApprovalStatus == -5)
                {
                    pVGClaimVM.ExpenseStatusName = "Requested to Void";

                }
                else if (mc.ApprovalStatus == 6)
                {
                    pVGClaimVM.ExpenseStatusName = "Awaiting approval";

                }
                else if (mc.ApprovalStatus == 7)
                {
                    pVGClaimVM.ExpenseStatusName = "Awaiting HOD approval";
                }
                else
                {
                    pVGClaimVM.ExpenseStatusName = "New";
                }


                if (mc.UserApprovers != "")
                {
                    pVGClaimVM.Approver = mc.UserApprovers.Split(',').First();
                    if (pVGClaimVM.Approver == HttpContext.User.FindFirst("userid").Value && (pVGClaimVM.ApprovalStatus == 6))
                    {
                        pVGClaimVM.IsActionAllowed = true;
                    }
                }
                else if (mc.HODApprover != "")
                {
                    pVGClaimVM.Approver = mc.HODApprover.Split(',').First();
                    if (pVGClaimVM.Approver == HttpContext.User.FindFirst("userid").Value && (pVGClaimVM.ApprovalStatus == 7))
                    {
                        pVGClaimVM.IsActionAllowed = true;
                    }
                }
                else if (mc.Verifier != "")
                {
                    pVGClaimVM.Approver = mc.Verifier.Split(',').First();
                    if (pVGClaimVM.Approver == HttpContext.User.FindFirst("userid").Value && (pVGClaimVM.ApprovalStatus == 1 || pVGClaimVM.ApprovalStatus == 2))
                    {
                        pVGClaimVM.IsActionAllowed = true;
                    }
                    //string VerifierIDs = string.Join(",", PVGverifierIDs.Skip(1));
                }
                else if (mc.Approver != "")
                {
                    pVGClaimVM.Approver = mc.Approver.Split(',').First();
                    if (pVGClaimVM.Approver == HttpContext.User.FindFirst("userid").Value && (pVGClaimVM.ApprovalStatus == 1 || pVGClaimVM.ApprovalStatus == 2))
                    {
                        pVGClaimVM.IsActionAllowed = true;
                    }
                }
                else
                {
                    pVGClaimVM.Approver = "";
                }

                if (pVGClaimVM.Approver != "")
                {
                    var mstUserApprover = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(pVGClaimVM.Approver));
                    if (pVGClaimVM.ApprovalStatus != 3 && pVGClaimVM.ApprovalStatus != 4 && pVGClaimVM.ApprovalStatus != -5 && pVGClaimVM.ApprovalStatus != 5)
                        pVGClaimVM.Approver = mstUserApprover.Name;
                    else
                        pVGClaimVM.Approver = "";
                }


                dt.Rows.Add(pVGClaimVM.PVGCNo = mc.CNO,
                            pVGClaimVM.Name = mc.Name,
                            pVGClaimVM.CreatedDate = Convert.ToDateTime(mc.CreatedDate).ToString("dd/MM/yyyy", System.Globalization.CultureInfo.CreateSpecificCulture("en-US")),
                            pVGClaimVM.FacilityName = mc.FacilityName,
                            pVGClaimVM.Name = mc.Name,
                            pVGClaimVM.Phone = mc.Phone,
                            pVGClaimVM.TotalAmount = mc.TotalAmount,
                            pVGClaimVM.Approver = pVGClaimVM.Approver,
                            pVGClaimVM.ExpenseStatusName = pVGClaimVM.ExpenseStatusName);
            }

            string filename = "PVGClaims-Export" + DateTime.Now.ToString("ddMMyyyyss") + ".xlsx";
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
                        return File(blobStream, file.Properties.ContentType, "PVGClaims-Export.xlsx");
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
                    CloudBlob file = container.GetBlobReference("FileUploads/PVGClaimFiles/" + id);

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
        public async Task<IActionResult> GetPrintClaimDetails(long? id)
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
                pVGClaimVM.GrandGST = mstPVGClaim.TotalAmount - mstPVGClaim.GrandTotal;
                pVGClaimVM.Company = mstPVGClaim.Company;
                pVGClaimVM.Name = mstPVGClaim.MstUser.Name;
                pVGClaimVM.DepartmentName = mstPVGClaim.MstDepartment.Department;
                pVGClaimVM.FacilityName = mstPVGClaim.MstFacility.FacilityName;
                pVGClaimVM.CreatedDate = Convert.ToDateTime(mstPVGClaim.CreatedDate).ToString("dd/MM/yyyy", System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
                pVGClaimVM.Verifier = mstPVGClaim.Verifier;
                pVGClaimVM.Approver = mstPVGClaim.Approver;
                pVGClaimVM.PVGCNo = mstPVGClaim.PVGCNo;
                pVGClaimVM.PaymentMode = mstPVGClaim.PaymentMode;
                pVGClaimVM.VoucherNo = mstPVGClaim.VoucherNo;
                ViewBag.PVGCID = id;
                pVGClaimDetailVM.PVGClaimVM = pVGClaimVM;
                //mileageClaimDetailVM.DtMileageClaimVMs = dtMileageClaimVMs;
            }
            return PartialView("GetPVGDetailsPrint", pVGClaimDetailVM);
        }
        public async Task<IActionResult> GetPrint(string data)
        {
            var mileageClaimSearch = JsonConvert.DeserializeObject<MileageClaimSearch>(data);
            var mstPVGClaimsWithDetails = await _repository.MstPVGClaim.GetAllPVGClaimWithDetailsAsync(mileageClaimSearch.UserID, mileageClaimSearch.FacilityID, mileageClaimSearch.StatusID, mileageClaimSearch.FromDate, mileageClaimSearch.ToDate);
            List<PVGClaimVM> pVGClaimVMs = new List<PVGClaimVM>();


            foreach (var mc in mstPVGClaimsWithDetails)
            {
                PVGClaimVM pVGClaimVM = new PVGClaimVM();

                pVGClaimVM.PVGCNo = mc.CNO;
                pVGClaimVM.Name = mc.Name;
                pVGClaimVM.CreatedDate = Convert.ToDateTime(mc.CreatedDate).ToString("dd/MM/yyyy", System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
                pVGClaimVM.FacilityName = mc.FacilityName;
                pVGClaimVM.Name = mc.Name;
                pVGClaimVM.Phone = mc.Phone;
                pVGClaimVM.TotalAmount = mc.TotalAmount;
                pVGClaimVM.ApprovalStatus = mc.ApprovalStatus;

                if (mc.ApprovalStatus == 1)
                {
                    pVGClaimVM.ExpenseStatusName = "Awaiting Verification";

                }
                else if (mc.ApprovalStatus == 2)
                {
                    pVGClaimVM.ExpenseStatusName = "Awaiting Signatory approval";

                }
                else if (mc.ApprovalStatus == 3)
                {
                    pVGClaimVM.ExpenseStatusName = "Approved";

                }
                else if (mc.ApprovalStatus == 4)
                {
                    pVGClaimVM.ExpenseStatusName = "Request to Amend";
                }
                else if (mc.ApprovalStatus == 5)
                {
                    pVGClaimVM.ExpenseStatusName = "Voided";

                }
                else if (mc.ApprovalStatus == -5)
                {
                    pVGClaimVM.ExpenseStatusName = "Requested to Void";

                }
                else if (mc.ApprovalStatus == 6)
                {
                    pVGClaimVM.ExpenseStatusName = "Awaiting approval";

                }
                else if (mc.ApprovalStatus == 7)
                {
                    pVGClaimVM.ExpenseStatusName = "Awaiting HOD approval";
                }
                else
                {
                    pVGClaimVM.ExpenseStatusName = "New";
                }


                if (mc.UserApprovers != "")
                {
                    pVGClaimVM.Approver = mc.UserApprovers.Split(',').First();
                    if (pVGClaimVM.Approver == HttpContext.User.FindFirst("userid").Value && (pVGClaimVM.ApprovalStatus == 6))
                    {
                        pVGClaimVM.IsActionAllowed = true;
                    }
                }
                else if (mc.HODApprover != "")
                {
                    pVGClaimVM.Approver = mc.HODApprover.Split(',').First();
                    if (pVGClaimVM.Approver == HttpContext.User.FindFirst("userid").Value && (pVGClaimVM.ApprovalStatus == 7))
                    {
                        pVGClaimVM.IsActionAllowed = true;
                    }
                }
                else if (mc.Verifier != "")
                {
                    pVGClaimVM.Approver = mc.Verifier.Split(',').First();
                    if (pVGClaimVM.Approver == HttpContext.User.FindFirst("userid").Value && (pVGClaimVM.ApprovalStatus == 1 || pVGClaimVM.ApprovalStatus == 2))
                    {
                        pVGClaimVM.IsActionAllowed = true;
                    }
                    //string VerifierIDs = string.Join(",", PVGverifierIDs.Skip(1));
                }
                else if (mc.Approver != "")
                {
                    pVGClaimVM.Approver = mc.Approver.Split(',').First();
                    if (pVGClaimVM.Approver == HttpContext.User.FindFirst("userid").Value && (pVGClaimVM.ApprovalStatus == 1 || pVGClaimVM.ApprovalStatus == 2))
                    {
                        pVGClaimVM.IsActionAllowed = true;
                    }
                }
                else
                {
                    pVGClaimVM.Approver = "";
                }

                if (pVGClaimVM.Approver != "")
                {
                    var mstUserApprover = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(pVGClaimVM.Approver));
                    pVGClaimVM.Approver = mstUserApprover.Name;
                }
                pVGClaimVMs.Add(pVGClaimVM);
            }
            return PartialView("GetPVGPrint", pVGClaimVMs);
        }

        public async Task<JsonResult> UpdateStatus(string id)
        {
            bool isAlternateApprover = false;
            if (User != null && User.Identity.IsAuthenticated)
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
                                    var mstVerifierDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(approverID));
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
                int PVGCID = Convert.ToInt32(id);

                var mstPVGClaim = await _repository.MstPVGClaim.GetPVGClaimByIdAsync(PVGCID);

                if (mstPVGClaim == null)
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

                await _repository.MstPVGClaim.UpdateMstPVGClaimStatus(PVGCID, 4, int.Parse(HttpContext.User.FindFirst("userid").Value), DateTime.Now, reason, string.Empty, string.Empty, string.Empty, string.Empty, isAlternateApprover, 0);
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

        public async Task<IActionResult> Download(string id, string name)
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
                        //clsdtExpenseQuery.NotificationStatus = false;
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

        #endregion GetMessages
    }
}
