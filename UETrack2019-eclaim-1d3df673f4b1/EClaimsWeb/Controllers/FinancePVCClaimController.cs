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
    public class FinancePVCClaimController : Controller
    {
        private ILoggerManager _logger;
        private IRepositoryWrapper _repository;
        private IMapper _mapper;
        private IConfiguration _configuration;
        private AlternateApproverHelper _alternateApproverHelper;
        private ISendMailServices _sendMailServices;
        private readonly RepositoryContext _context;

        public FinancePVCClaimController(ILoggerManager logger, IRepositoryWrapper repository, IMapper mapper, RepositoryContext context, IConfiguration configuration, ISendMailServices sendMailServices)
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

                var mstPVCClaimsWithDetails = await _repository.MstPVCClaim.GetAllPVCClaimWithDetailsAsync(userID, facilityID, statusID, fromDate, toDate);
                if (mstPVCClaimsWithDetails != null && mstPVCClaimsWithDetails.Any())
                {
                    mstPVCClaimsWithDetails.ToList().ForEach(c => c.IsDelegated = false);
                }

                if (delegatedUserId != null && delegatedUserId.HasValue)
                {
                    var delegatedClaims = await _repository.MstPVCClaim.GetAllPVCClaimWithDetailsAsync(delegatedUserId.Value, facilityID, statusID, fromDate, toDate);
                    if (delegatedClaims != null && delegatedClaims.Any())
                    {
                        delegatedClaims.ToList().ForEach(c => c.IsDelegated = true);
                        mstPVCClaimsWithDetails.ToList().AddRange(delegatedClaims.ToList());
                    }
                }

                _logger.LogInfo($"Returned all PVC Claims with details from database.");
                List<PVCClaimVM> pVCClaimVMs = new List<PVCClaimVM>();
                foreach (var mc in mstPVCClaimsWithDetails)
                {
                    PVCClaimVM pVCClaimVM = new PVCClaimVM();
                    pVCClaimVM.PVCCID = mc.CID;
                    pVCClaimVM.PVCCNo = mc.CNO;
                    pVCClaimVM.Name = mc.Name;
                    pVCClaimVM.CreatedDate = DateTime.ParseExact(mc.CreatedDate, "MM/dd/yyyy HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture)
                                                             .ToString("dd/MM/yyyy", System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
                    pVCClaimVM.FacilityName = mc.FacilityName;
                    pVCClaimVM.Phone = mc.Phone;
                    pVCClaimVM.GrandTotal = mc.GrandTotal;
                    pVCClaimVM.ApprovalStatus = mc.ApprovalStatus;
                    pVCClaimVM.TotalAmount = mc.TotalAmount;
                    pVCClaimVM.VoucherNo = mc.VoucherNo;
                    pVCClaimVM.PayeeName = mc.PayeeName;

                    if (mc.UserApprovers != "")
                    {
                        pVCClaimVM.Approver = mc.UserApprovers.Split(',').First();
                        if ((pVCClaimVM.Approver == HttpContext.User.FindFirst("userid").Value || (delegatedUserId.HasValue && pVCClaimVM.Approver == delegatedUserId.Value.ToString())) &&
                            (pVCClaimVM.ApprovalStatus == 6))
                        {
                            pVCClaimVM.IsActionAllowed = false;
                        }
                    }
                    else if (mc.HODApprover != "")
                    {
                        pVCClaimVM.Approver = mc.HODApprover.Split(',').First();
                        if ((pVCClaimVM.Approver == HttpContext.User.FindFirst("userid").Value || (delegatedUserId.HasValue && pVCClaimVM.Approver == delegatedUserId.Value.ToString())) &&
                            (pVCClaimVM.ApprovalStatus == 7))
                        {
                            pVCClaimVM.IsActionAllowed = false;
                        }
                    }
                    else if (mc.Verifier != "")
                    {
                        pVCClaimVM.Approver = mc.Verifier.Split(',').First();
                        if ((pVCClaimVM.Approver == HttpContext.User.FindFirst("userid").Value || (delegatedUserId.HasValue && pVCClaimVM.Approver == delegatedUserId.Value.ToString())) &&
                            (pVCClaimVM.ApprovalStatus == 1 || pVCClaimVM.ApprovalStatus == 2))
                        {
                            pVCClaimVM.IsActionAllowed = true;
                        }
                        //string VerifierIDs = string.Join(",", PVCverifierIDs.Skip(1));
                    }
                    else if (mc.Approver != "")
                    {
                        pVCClaimVM.Approver = mc.Approver.Split(',').First();
                        if ((pVCClaimVM.Approver == HttpContext.User.FindFirst("userid").Value || (delegatedUserId.HasValue && pVCClaimVM.Approver == delegatedUserId.Value.ToString())) &&
                            (pVCClaimVM.ApprovalStatus == 1 || pVCClaimVM.ApprovalStatus == 2))
                        {
                            pVCClaimVM.IsActionAllowed = true;
                        }
                    }
                    else
                    {
                        pVCClaimVM.Approver = "";
                    }

                    if (pVCClaimVM.Approver != "")
                    {
                        var alternateUser = await _alternateApproverHelper.IsAlternateApprovalSetForUser(Convert.ToInt32(pVCClaimVM.Approver));
                        if (alternateUser.HasValue)
                        {
                            var mstUserApprover = await _repository.MstUser.GetUserByIdAsync(alternateUser.Value);
                            pVCClaimVM.Approver = mstUserApprover.Name + " (AA)";
                        }
                        else
                        {
                            var mstUserApprover = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(pVCClaimVM.Approver));
                            pVCClaimVM.Approver = mstUserApprover.Name;
                        }
                    }

                    // Show actions based on alternate approver settings
                    // Override all the isActionAllowed code above. When alternate approval is set, then no need to show the action on any scenario
                    if (isAlternateApproverSet)
                    {
                        pVCClaimVM.IsActionAllowed = false;
                    }

                    pVCClaimVMs.Add(pVCClaimVM);
                }

                var mstPVCClaimVM = new PVCClaimSearchViewModel
                {
                    //Screens = new SelectList(await screenQuery.Distinct().ToListAsync()),
                    pVCClaimVMs = pVCClaimVMs,
                    Statuses = new SelectList(status, "Value", "Text"),
                    Facilities = new SelectList(facilities, "Value", "Text"),
                    Users = new SelectList(users, "Value", "Text"),
                    FromDate = fromDate,
                    ToDate = toDate
                };
                return View(mstPVCClaimVM);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Something went wrong inside GetAllPVCClaimWithDetailsAsync action: {ex.Message}");
                return View();
            }
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
            long PVCCID = Convert.ToInt64(id);

            if (User != null && User.Identity.IsAuthenticated)
            {
                var mstPVCClaim = await _repository.MstPVCClaim.GetPVCClaimByIdAsync(id);

                if (mstPVCClaim == null)
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
                    dtPVCClaimVM.GSTPercentage = item.GSTPercentage;
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

                pVCClaimDetailVM.PVCClaimFileUploads = _repository.DtPVCClaimFileUpload.GetDtPVCClaimAuditByIdAsync(id).GetAwaiter().GetResult().ToList();

                PVCClaimVM pVCClaimVM = new PVCClaimVM();
                //pVCClaimVM.ClaimType = mstPVCClaim.ClaimType;
                pVCClaimVM.VoucherNo = mstPVCClaim.VoucherNo;
                pVCClaimVM.GrandTotal = mstPVCClaim.GrandTotal;
                pVCClaimVM.TotalAmount = mstPVCClaim.TotalAmount;
                pVCClaimVM.GrandGST = mstPVCClaim.TotalAmount - mstPVCClaim.GrandTotal;
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
                if (mstPVCClaim.Verifier != "")
                {
                    string[] verifierIDs = mstPVCClaim.Verifier.Split(',');
                    TempData["QueryMCVerifierIDs"] = string.Join(",", verifierIDs);
                    foreach (string verifierID in verifierIDs)
                    {
                        if ((verifierID != "" && verifierID == HttpContext.User.FindFirst("userid").Value || (delegatedUserId.HasValue && verifierID == delegatedUserId.Value.ToString())) && User.IsInRole("Finance"))
                        {
                            TempData["ApprovedStatus"] = mstPVCClaim.ApprovalStatus;
                            //Session["ApprovedStatus"] = oSupplierPO.Approvalstatus;
                            TempData["VerifierIDs"] = string.Join(",", verifierIDs.Skip(1));
                            pVCClaimVM.IsActionAllowed = true;
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
                    TempData["VerifierIDs"] = mstPVCClaim.Verifier;
                    TempData["ApproverIDs"] = mstPVCClaim.Approver;
                }

                //Approval Process code
                if (mstPVCClaim.Approver != "" && mstPVCClaim.Verifier == "")
                {
                    string[] approverIDs = mstPVCClaim.Approver.Split(',');
                    TempData["QueryMCApproverIDs"] = string.Join(",", approverIDs);
                    foreach (string approverID in approverIDs)
                    {
                        if ((approverID != "" && approverID == HttpContext.User.FindFirst("userid").Value || (delegatedUserId.HasValue && approverID == delegatedUserId.Value.ToString())) && User.IsInRole("Finance"))
                        {
                            TempData["ApprovedStatus"] = mstPVCClaim.ApprovalStatus;
                            //Session["ApprovedStatus"] = oSupplierPO.Approvalstatus;
                            TempData["ApproverIDs"] = string.Join(",", approverIDs.Skip(1));
                            pVCClaimVM.IsActionAllowed = true;
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


                BindGSTDropdown();
                return View(pVCClaimDetailVM);
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
                int PVCCID = Convert.ToInt32(id);

                var mstPVCClaim = await _repository.MstPVCClaim.GetPVCClaimByIdAsync(PVCCID);

                if (mstPVCClaim == null)
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
                    await _repository.MstPVCClaim.UpdateMstPVCClaimStatus(PVCCID, -5, int.Parse(HttpContext.User.FindFirst("userid").Value), DateTime.Now, reason, string.Empty, string.Empty, string.Empty, string.Empty, isAlternateApprover, 0);
                }
                else
                {
                    await _repository.MstPVCClaim.UpdateMstPVCClaimStatus(PVCCID, 5, int.Parse(HttpContext.User.FindFirst("userid").Value), DateTime.Now, reason, string.Empty, string.Empty, string.Empty, string.Empty, isAlternateApprover, 0);
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

        public async Task<JsonResult> GetTextValuesSG(string id)
        {
            List<DtPVCClaimVM> oDtClaimsList = new List<DtPVCClaimVM>();

            try
            {
                var dtPVCClaims = await _repository.DtPVCClaim.GetDtPVCClaimByIdAsync(Convert.ToInt64(id));

                // List<clsDtSupplierPO> oclsDtSupplierPO = new List<clsDtSupplierPO>();
                foreach (var item in dtPVCClaims)
                {
                    DtPVCClaimVM dtPVCClaimVM = new DtPVCClaimVM();
                    dtPVCClaimVM.Date = item.Date;
                    dtPVCClaimVM.PVCCItemID = item.PVCCItemID;
                    dtPVCClaimVM.PVCCID = item.PVCCID;
                    dtPVCClaimVM.InvoiceNo = item.InvoiceNo;
                    dtPVCClaimVM.Particulars = item.Particulars;
                    dtPVCClaimVM.Payee = item.Payee;
                    dtPVCClaimVM.ChequeNo = item.ChequeNo;
                    dtPVCClaimVM.Amount = item.Amount;
                    dtPVCClaimVM.GST = item.GST;
                    dtPVCClaimVM.AmountWithGST = item.Amount + item.GST;
                    dtPVCClaimVM.ExpenseCategoryID = item.ExpenseCategoryID;
                    dtPVCClaimVM.AccountCode = item.AccountCode;
                    dtPVCClaimVM.FacilityID = item.FacilityID;
                    oDtClaimsList.Add(dtPVCClaimVM);
                }
                return Json(new { DtClaimsList = oDtClaimsList });
            }
            catch
            {
                return Json(new { DtClaimsList = oDtClaimsList });
            }

        }

        [HttpPost]
        public async Task<JsonResult> SaveItems(string data)
        {
            var pVCClaimViewModel = JsonConvert.DeserializeObject<PVCClaimViewModel>(data);

            var mstFacility = await _repository.MstFacility.GetFacilityWithDepartmentByIdAsync(Convert.ToInt32(HttpContext.User.FindFirst("facilityid").Value));



            MstPVCClaim mstPVCClaim = new MstPVCClaim();
            mstPVCClaim.PVCCNo = pVCClaimViewModel.PVCCNo;
            mstPVCClaim.UserID = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value);
            mstPVCClaim.Verifier = "";
            mstPVCClaim.Approver = "";
            mstPVCClaim.FinalApprover = "";
            mstPVCClaim.ApprovalStatus = 1;
            mstPVCClaim.GrandTotal = pVCClaimViewModel.GrandTotal;
            mstPVCClaim.TotalAmount = pVCClaimViewModel.TotalAmount;
            mstPVCClaim.Company = pVCClaimViewModel.Company;
            mstPVCClaim.FacilityID = Convert.ToInt32(HttpContext.User.FindFirst("facilityid").Value);
            mstPVCClaim.DepartmentID = mstFacility.MstDepartment.DepartmentID;
            mstPVCClaim.CreatedDate = DateTime.Now;
            mstPVCClaim.ModifiedDate = DateTime.Now;
            mstPVCClaim.CreatedBy = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value);
            mstPVCClaim.ModifiedBy = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value);
            mstPVCClaim.ApprovalDate = DateTime.Now;
            mstPVCClaim.ApprovalBy = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value);
            mstPVCClaim.TnC = true;

            foreach (var dtItem in pVCClaimViewModel.dtClaims)
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
            long PVCCID = 0;
            try
            {
                //CBRID = Convert.ToInt32(Session["CBRID"].ToString());
                PVCCID = Convert.ToInt64(pVCClaimViewModel.PVCCID);
                if (PVCCID == 0)
                    ClaimStatus = "Add";
                else
                    ClaimStatus = "Update";
                mstPVCClaim.PVCCID = PVCCID;
                //mstPVCClaim.PVCCNo = pVCClaimViewModel.;
            }
            catch { }

            PVCClaimDetailVM pVCClaimDetailVM = new PVCClaimDetailVM();
            //List<DtMileageClaimVM> dtMileageClaimVMs = new List<DtMileageClaimVM>();
            pVCClaimDetailVM.DtPVCClaimVMs = new List<DtPVCClaimVM>();
            // List<clsDtSupplierPO> oclsDtSupplierPO = new List<clsDtSupplierPO>();
            foreach (var item in pVCClaimViewModel.dtClaims)
            {
                DtPVCClaimVM dtPVCClaimVM = new DtPVCClaimVM();

                dtPVCClaimVM.PVCCItemID = item.PVCCItemID;
                if (pVCClaimViewModel.ClaimAddCondition == "claimDraft")
                {
                    dtPVCClaimVM.PVCCID = 0;
                }
                else
                {
                    dtPVCClaimVM.PVCCID = item.PVCCID;
                }
                dtPVCClaimVM.Payee = item.Payee;
                dtPVCClaimVM.Particulars = item.Particulars;
                dtPVCClaimVM.ExpenseCategory = item.MstExpenseCategory.Description;
                dtPVCClaimVM.ExpenseCategoryID = item.MstExpenseCategory.ExpenseCategoryID;
                dtPVCClaimVM.FacilityID = item.FacilityID;
                //dtPVCClaimVM.EmployeeNo = item.EmployeeNo;
                dtPVCClaimVM.ChequeNo = item.ChequeNo;
                dtPVCClaimVM.Amount = item.Amount;
                dtPVCClaimVM.GST = item.GST;
                dtPVCClaimVM.GSTPercentage = item.GSTPercentage;
                dtPVCClaimVM.AmountWithGST = item.Amount + item.GST;
                //dtPVCClaimVM.Facility = item.Facility;
                dtPVCClaimVM.AccountCode = item.AccountCode;
                dtPVCClaimVM.Date = item.Date;
                pVCClaimDetailVM.DtPVCClaimVMs.Add(dtPVCClaimVM);
            }

            var GroupByQS = pVCClaimDetailVM.DtPVCClaimVMs.GroupBy(s => s.AccountCode);

            pVCClaimDetailVM.DtPVCClaimVMSummary = new List<DtPVCClaimVM>();

            foreach (var group in GroupByQS)
            {
                DtPVCClaimVM dtPVCClaimVM = new DtPVCClaimVM();
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
                dtPVCClaimVM.Particulars = ExpenseDesc;
                dtPVCClaimVM.ExpenseCategory = ExpenseCat;
                dtPVCClaimVM.ExpenseCategoryID = ExpenseCatID;
                dtPVCClaimVM.FacilityID = facilityID;
                dtPVCClaimVM.Facility = Facility;
                dtPVCClaimVM.AccountCode = AccountCode;
                dtPVCClaimVM.Amount = amount;
                dtPVCClaimVM.GST = gst;
                dtPVCClaimVM.GSTPercentage = gstpercentage;
                dtPVCClaimVM.AmountWithGST = sumamount;
                pVCClaimDetailVM.DtPVCClaimVMSummary.Add(dtPVCClaimVM);
            }
            List<DtPVCClaimSummary> lstPVCClaimSummary = new List<DtPVCClaimSummary>();
            foreach (var item in pVCClaimDetailVM.DtPVCClaimVMSummary)
            {
                DtPVCClaimSummary dtPVCClaimSummary1 = new DtPVCClaimSummary();
                dtPVCClaimSummary1.AccountCode = item.AccountCode;
                dtPVCClaimSummary1.Amount = item.Amount;
                dtPVCClaimSummary1.ExpenseCategory = item.ExpenseCategory;
                dtPVCClaimSummary1.ExpenseCategoryID = item.ExpenseCategoryID;
                dtPVCClaimSummary1.FacilityID = item.FacilityID;
                dtPVCClaimSummary1.Facility = item.Facility;
                dtPVCClaimSummary1.Description = item.Particulars.ToUpper();
                dtPVCClaimSummary1.GST = item.GST;
                dtPVCClaimSummary1.GSTPercentage = item.GSTPercentage;
                if (item.GST != 0)
                {
                    dtPVCClaimSummary1.TaxClass = Math.Round((decimal)item.GSTPercentage, (int)1);
                }
                else
                {
                    dtPVCClaimSummary1.TaxClass = 4;
                }
                dtPVCClaimSummary1.AmountWithGST = item.AmountWithGST;
                lstPVCClaimSummary.Add(dtPVCClaimSummary1);
            }

            DtPVCClaimSummary dtPVCClaimSummary = new DtPVCClaimSummary();
            dtPVCClaimSummary.AccountCode = "425000";
            dtPVCClaimSummary.Amount = mstPVCClaim.GrandTotal;
            dtPVCClaimSummary.TaxClass = 0;
            dtPVCClaimSummary.GST = mstPVCClaim.TotalAmount - mstPVCClaim.GrandTotal;
            dtPVCClaimSummary.AmountWithGST = mstPVCClaim.TotalAmount;
            dtPVCClaimSummary.ExpenseCategory = "DBS";
            dtPVCClaimSummary.Description = "";
            lstPVCClaimSummary.Add(dtPVCClaimSummary);

            var res = await _repository.MstPVCClaim.SaveItems(mstPVCClaim, pVCClaimViewModel.dtClaims, lstPVCClaimSummary);
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
                if (ClaimStatus == "Add")
                    TempData["Message"] = "PV-Cheque Claim added successfully";
                else
                    TempData["Message"] = "PV-Cheque Claim updated successfully";

                return Json(new { res });
            }
            else
                return Json(new { res });
        }

        public async Task<IActionResult> Create(string id, string Updatestatus)
        {
            //TempData["CBRID"] = 0;
            TempData["Updatestatus"] = "Add";
            PVCClaimDetailVM pvcClaimDetailVM = new PVCClaimDetailVM();
            pvcClaimDetailVM.DtPVCClaimVMs = new List<DtPVCClaimVM>();
            pvcClaimDetailVM.PVCClaimAudits = new List<PVCClaimAuditVM>();

            if (User != null && User.Identity.IsAuthenticated)
            {
                if (!string.IsNullOrEmpty(id))
                {
                    long idd = Convert.ToInt64(id);
                    ViewBag.CID = idd;
                    var dtPVCClaims = await _repository.DtPVCClaim.GetDtPVCClaimByIdAsync(idd);

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
                        dtPVCClaimVM.GSTPercentage = item.GSTPercentage;
                        dtPVCClaimVM.AmountWithGST = item.Amount + item.GST;
                        dtPVCClaimVM.ExpenseCategory = item.MstExpenseCategory.Description;
                        dtPVCClaimVM.AccountCode = item.AccountCode;
                        pvcClaimDetailVM.DtPVCClaimVMs.Add(dtPVCClaimVM);
                    }

                    pvcClaimDetailVM.PVCClaimFileUploads = new List<DtPVCClaimFileUpload>();

                    pvcClaimDetailVM.PVCClaimFileUploads = await _repository.DtPVCClaimFileUpload.GetDtPVCClaimAuditByIdAsync(idd);

                    var mstPVCClaim = await _repository.MstPVCClaim.GetPVCClaimByIdAsync(idd);


                    PVCClaimVM pvcClaimVM = new PVCClaimVM();
                    pvcClaimVM.GrandTotal = mstPVCClaim.GrandTotal;
                    pvcClaimVM.TotalAmount = mstPVCClaim.TotalAmount;
                    pvcClaimVM.Company = mstPVCClaim.Company;
                    pvcClaimVM.Name = mstPVCClaim.MstUser.Name;
                    pvcClaimVM.DepartmentName = mstPVCClaim.MstDepartment.Department;
                    pvcClaimVM.FacilityName = mstPVCClaim.MstFacility.FacilityName;
                    pvcClaimVM.CreatedDate = mstPVCClaim.CreatedDate.ToString("d");
                    pvcClaimVM.Verifier = mstPVCClaim.Verifier;
                    pvcClaimVM.Approver = mstPVCClaim.Approver;
                    pvcClaimVM.PVCCNo = mstPVCClaim.PVCCNo;

                    pvcClaimDetailVM.PVCClaimVM = pvcClaimVM;

                    if (Updatestatus == "New")
                    {
                        TempData["status"] = "Add";
                        ViewBag.ClaimStatus = "Add";
                    }
                    else
                    {
                        TempData["status"] = "Update";
                        ViewBag.ClaimStatus = "Update";
                    }
                }
                else
                {
                    pvcClaimDetailVM.PVCClaimAudits = new List<PVCClaimAuditVM>();
                    pvcClaimDetailVM.PVCClaimFileUploads = new List<DtPVCClaimFileUpload>();
                    PVCClaimVM pvcClaimVM = new PVCClaimVM();
                    pvcClaimVM.GrandTotal = 0;
                    pvcClaimVM.TotalAmount = 0;
                    pvcClaimVM.Company = "";
                    pvcClaimVM.Name = "";
                    pvcClaimVM.DepartmentName = "";
                    pvcClaimVM.FacilityName = "";
                    pvcClaimVM.CreatedDate = "";
                    pvcClaimVM.Verifier = "";
                    pvcClaimVM.Approver = "";
                    pvcClaimVM.PVCCNo = "";

                    DtPVCClaimVM dtPVCClaimVM = new DtPVCClaimVM();

                    dtPVCClaimVM.PVCCItemID = 0;
                    dtPVCClaimVM.PVCCID = 0;
                    //dtPVCClaimVM.DateOfJourney = "";

                    dtPVCClaimVM.ChequeNo = "";
                    dtPVCClaimVM.Particulars = "";
                    dtPVCClaimVM.Payee = "";
                    dtPVCClaimVM.InvoiceNo = "";
                    dtPVCClaimVM.Amount = 0;
                    dtPVCClaimVM.GST = 0;
                    dtPVCClaimVM.AmountWithGST = 0;
                    dtPVCClaimVM.ExpenseCategory = "";
                    dtPVCClaimVM.AccountCode = "";

                    pvcClaimDetailVM.DtPVCClaimVMs.Add(dtPVCClaimVM);
                    pvcClaimDetailVM.PVCClaimVM = pvcClaimVM;


                    TempData["status"] = "Add";
                }
                //int userFacilityId = mstUsersWithDetails.MstFacility.FacilityID;
                int userFacilityId = Convert.ToInt32(User.Claims.FirstOrDefault(c => c.Type == "facilityid").Value);
                var currFacility = await _repository.MstFacility.GetFacilityWithDepartmentByIdAsync(userFacilityId);
                ViewData["ExpenseCategoryID"] = new SelectList(await _repository.MstExpenseCategory.GetAllExpenseCategoriesByClaimTypesAsync("expense/pv-cheque/pv-giro", "active"), "ExpenseCategoryID", "Description");
                var mstUsersWithDetails = await _repository.MstUser.GetUserWithDetailsByIdAsync(Convert.ToInt32(HttpContext.User.FindFirst("userid").Value));
                ViewData["Name"] = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.GivenName).Value;
                ViewData["FacilityName"] = currFacility.FacilityName;
                ViewData["Department"] = currFacility.MstDepartment.Department;
                SelectList facilities = new SelectList(await _repository.MstFacility.GetAllFacilityAsync("active"), "FacilityID", "FacilityName");

                var userFacility = facilities.Where(x => x.Value == userFacilityId.ToString()).FirstOrDefault();
                if (userFacility != null)
                {
                    facilities.Where(x => x.Value == userFacilityId.ToString()).FirstOrDefault().Selected = true;
                }
                ViewData["FacilityID"] = facilities;
                BindGSTDropdown();
                string financeGstValueBuffer = _configuration.GetValue<string>("FinanceGstValueBuffer");
                ViewBag.FinanceGstValueBuffer = financeGstValueBuffer;
            }
            return View(pvcClaimDetailVM);

        }

        public async Task<ActionResult> DeletePVCClaimFile(string fileID, string filepath, string PVCCID)
        {
            DtPVCClaimFileUpload dtPVCClaimFileUpload = new DtPVCClaimFileUpload();
            if (CloudStorageAccount.TryParse(_configuration.GetSection("ConnectionStrings")["BlobConnectionString"], out CloudStorageAccount storageAccount))
            {
                CloudBlobClient BlobClient = storageAccount.CreateCloudBlobClient();
                CloudBlobContainer container = BlobClient.GetContainerReference(_configuration.GetSection("ConnectionStrings")["BlobContainerName"]);

                if (await container.ExistsAsync())
                {
                    CloudBlob file = container.GetBlobReference("FileUploads/PVCClaimFiles/" + filepath);

                    if (await file.ExistsAsync())
                    {
                        await file.DeleteIfExistsAsync();
                        dtPVCClaimFileUpload = await _repository.DtPVCClaimFileUpload.GetDtPVCClaimFileUploadByIdAsync(Convert.ToInt64(fileID));
                        _repository.DtPVCClaimFileUpload.DeleteDtPVCClaimFileUpload(dtPVCClaimFileUpload);
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

            return RedirectToAction("Create", "FinancePVCClaim", new
            {
                id = PVCCID,
                Updatestatus = "Edit"
            });
        }


        public async Task<JsonResult> GetTextValuesSGSummary(string id)
        {
            List<DtPVCClaimSummary> oDtClaimsSummaryList = new List<DtPVCClaimSummary>();

            try
            {
                var dtPVCClaimSummaries = await _repository.DtPVCClaimSummary.GetDtPVCClaimSummaryByIdAsync(Convert.ToInt64(id));

                // List<clsDtSupplierPO> oclsDtSupplierPO = new List<clsDtSupplierPO>();
                //foreach (var item in dtPVCClaimSummaries)
                //{
                //    DtPVCClaimVM dtPVCClaimVM = new DtPVCClaimVM();

                //    dtPVCClaimVM.PVCCItemID = item.PVCCItemID;
                //    dtPVCClaimVM.PVCCID = item.PVCCID;
                //    dtPVCClaimVM.StaffName = item.StaffName;
                //    dtPVCClaimVM.Reason = item.Reason;
                //    dtPVCClaimVM.EmployeeNo = item.EmployeeNo;
                //    dtPVCClaimVM.ChequeNo = item.ChequeNo;
                //    dtPVCClaimVM.Amount = item.Amount;
                //    dtPVCClaimVM.GST = item.GST;
                //    dtPVCClaimVM.AmountWithGST = item.Amount + item.GST;
                //    dtPVCClaimVM.Facility = item.Facility;
                //    dtPVCClaimVM.AccountCode = item.AccountCode;
                //    //dtPVCClaimVM.FacilityID = item.FacilityID;
                //    oDtClaimsList.Add(dtPVCClaimVM);
                //}
                return Json(new { DtClaimsList = dtPVCClaimSummaries });
            }
            catch
            {
                return Json(new { DtClaimsList = oDtClaimsSummaryList });
            }

        }

        [HttpPost]
        public async Task<JsonResult> SaveSummary(string data)
        {
            var pVCClaimViewModel = JsonConvert.DeserializeObject<DtPVCClaimSummaryVM>(data);
            var pVCCSummary = await _repository.DtPVCClaimSummary.GetDtPVCClaimSummaryByIdAsync(pVCClaimViewModel.PVCCID);
            foreach (var hr in pVCCSummary)
            {
                _repository.DtPVCClaimSummary.Delete(hr);
            }

            foreach (var dtItem in pVCClaimViewModel.dtClaims)
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

            MstPVCClaimAudit auditUpdate = new MstPVCClaimAudit();
            auditUpdate.PVCCID = pVCClaimViewModel.PVCCID;
            auditUpdate.Action = "1";
            auditUpdate.AuditDate = DateTime.Now;
            auditUpdate.AuditBy = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value);
            //auditUpdate.InstanceID = 1;
            string time = DateTime.Now.ToString("tt", System.Globalization.CultureInfo.InvariantCulture);
            DateTime date = DateTime.Now;
            string formattedDate = date.ToString("dd'/'MM'/'yyyy hh:mm:ss");
            auditUpdate.Description = "Summary of Accounts Allocation Amended by " + User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.GivenName).Value.ToString() + " on " + formattedDate + " " + time + " ";
            auditUpdate.SentTo = "";
            //await _repository.MstPVCClaimAudit.CreatePVCClaimAudit(auditUpdate);
            //await _repository.SaveAsync();
            var res = await _repository.MstPVCClaim.SaveSummary(pVCClaimViewModel.PVCCID, pVCClaimViewModel.dtClaims, auditUpdate);

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

        public async Task<JsonResult> ExporttoExcel(string data)
        {
            var mileageClaimSearch = JsonConvert.DeserializeObject<MileageClaimSearch>(data);

            var mstPVCClaimsWithDetails = await _repository.MstPVCClaim.GetAllPVCClaimWithDetailsAsync(mileageClaimSearch.UserID, mileageClaimSearch.FacilityID, mileageClaimSearch.StatusID, mileageClaimSearch.FromDate, mileageClaimSearch.ToDate);

            List<PVCClaimVM> pVCClaimVMs = new List<PVCClaimVM>();

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





            foreach (var mc in mstPVCClaimsWithDetails)
            {
                PVCClaimVM pVCClaimVM = new PVCClaimVM();
                pVCClaimVM.ApprovalStatus = mc.ApprovalStatus;

                if (mc.ApprovalStatus == 1)
                {
                    pVCClaimVM.ExpenseStatusName = "Awaiting Verification";

                }
                else if (mc.ApprovalStatus == 2)
                {
                    pVCClaimVM.ExpenseStatusName = "Awaiting Signatory approval";

                }
                else if (mc.ApprovalStatus == 3)
                {
                    pVCClaimVM.ExpenseStatusName = "Approved";

                }
                else if (mc.ApprovalStatus == 4)
                {
                    pVCClaimVM.ExpenseStatusName = "Request to Amend";
                }
                else if (mc.ApprovalStatus == 5)
                {
                    pVCClaimVM.ExpenseStatusName = "Voided";

                }
                else if (mc.ApprovalStatus == -5)
                {
                    pVCClaimVM.ExpenseStatusName = "Requested to Void";

                }
                else if (mc.ApprovalStatus == 6)
                {
                    pVCClaimVM.ExpenseStatusName = "Awaiting approval";

                }
                else if (mc.ApprovalStatus == 7)
                {
                    pVCClaimVM.ExpenseStatusName = "Awaiting HOD approval";
                }
                else
                {
                    pVCClaimVM.ExpenseStatusName = "New";
                }


                if (mc.UserApprovers != "")
                {
                    pVCClaimVM.Approver = mc.UserApprovers.Split(',').First();
                    if (pVCClaimVM.Approver == HttpContext.User.FindFirst("userid").Value && (pVCClaimVM.ApprovalStatus == 6))
                    {
                        pVCClaimVM.IsActionAllowed = true;
                    }
                }
                else if (mc.HODApprover != "")
                {
                    pVCClaimVM.Approver = mc.HODApprover.Split(',').First();
                    if (pVCClaimVM.Approver == HttpContext.User.FindFirst("userid").Value && (pVCClaimVM.ApprovalStatus == 7))
                    {
                        pVCClaimVM.IsActionAllowed = true;
                    }
                }
                else if (mc.Verifier != "")
                {
                    pVCClaimVM.Approver = mc.Verifier.Split(',').First();
                    if (pVCClaimVM.Approver == HttpContext.User.FindFirst("userid").Value && (pVCClaimVM.ApprovalStatus == 1 || pVCClaimVM.ApprovalStatus == 2))
                    {
                        pVCClaimVM.IsActionAllowed = true;
                    }
                    //string VerifierIDs = string.Join(",", PVCverifierIDs.Skip(1));
                }
                else if (mc.Approver != "")
                {
                    pVCClaimVM.Approver = mc.Approver.Split(',').First();
                    if (pVCClaimVM.Approver == HttpContext.User.FindFirst("userid").Value && (pVCClaimVM.ApprovalStatus == 1 || pVCClaimVM.ApprovalStatus == 2))
                    {
                        pVCClaimVM.IsActionAllowed = true;
                    }
                }
                else
                {
                    pVCClaimVM.Approver = "";
                }

                if (pVCClaimVM.Approver != "")
                {
                    var mstUserApprover = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(pVCClaimVM.Approver));
                    if (pVCClaimVM.ApprovalStatus != 3 && pVCClaimVM.ApprovalStatus != 4 && pVCClaimVM.ApprovalStatus != -5 && pVCClaimVM.ApprovalStatus != 5)
                        pVCClaimVM.Approver = mstUserApprover.Name;
                    else
                        pVCClaimVM.Approver = "";
                }


                dt.Rows.Add(pVCClaimVM.PVCCNo = mc.CNO,
                            pVCClaimVM.Name = mc.Name,
                            pVCClaimVM.CreatedDate = Convert.ToDateTime(mc.CreatedDate).ToString("dd/MM/yyyy", System.Globalization.CultureInfo.CreateSpecificCulture("en-US")),
                            pVCClaimVM.FacilityName = mc.FacilityName,
                            pVCClaimVM.Name = mc.Name,
                            pVCClaimVM.Phone = mc.Phone,
                            pVCClaimVM.TotalAmount = mc.TotalAmount,
                            pVCClaimVM.Approver = pVCClaimVM.Approver,
                            pVCClaimVM.ExpenseStatusName = pVCClaimVM.ExpenseStatusName);
            }

            string filename = "PVCClaims-Export" + DateTime.Now.ToString("ddMMyyyyss") + ".xlsx";
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
                        return File(blobStream, file.Properties.ContentType, "PVCClaims-Export.xlsx");
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
                    CloudBlob file = container.GetBlobReference("FileUploads/PVCClaimFiles/" + id);

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
                pVCClaimVM.GrandGST = mstPVCClaim.TotalAmount - mstPVCClaim.GrandTotal;
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
            return PartialView("GetPVCDetailsPrint", pVCClaimDetailVM);
        }
        public async Task<IActionResult> GetPrint(string data)
        {
            var mileageClaimSearch = JsonConvert.DeserializeObject<MileageClaimSearch>(data);
            var mstPVCClaimsWithDetails = await _repository.MstPVCClaim.GetAllPVCClaimWithDetailsAsync(mileageClaimSearch.UserID, mileageClaimSearch.FacilityID, mileageClaimSearch.StatusID, mileageClaimSearch.FromDate, mileageClaimSearch.ToDate);
            List<PVCClaimVM> pVCClaimVMs = new List<PVCClaimVM>();


            foreach (var mc in mstPVCClaimsWithDetails)
            {
                PVCClaimVM pVCClaimVM = new PVCClaimVM();

                pVCClaimVM.PVCCNo = mc.CNO;
                pVCClaimVM.Name = mc.Name;
                pVCClaimVM.CreatedDate = Convert.ToDateTime(mc.CreatedDate).ToString("dd/MM/yyyy", System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
                pVCClaimVM.FacilityName = mc.FacilityName;
                pVCClaimVM.Name = mc.Name;
                pVCClaimVM.Phone = mc.Phone;
                pVCClaimVM.TotalAmount = mc.TotalAmount;
                pVCClaimVM.ApprovalStatus = mc.ApprovalStatus;

                if (mc.ApprovalStatus == 1)
                {
                    pVCClaimVM.ExpenseStatusName = "Awaiting Verification";

                }
                else if (mc.ApprovalStatus == 2)
                {
                    pVCClaimVM.ExpenseStatusName = "Awaiting Signatory approval";

                }
                else if (mc.ApprovalStatus == 3)
                {
                    pVCClaimVM.ExpenseStatusName = "Approved";

                }
                else if (mc.ApprovalStatus == 4)
                {
                    pVCClaimVM.ExpenseStatusName = "Request to Amend";
                }
                else if (mc.ApprovalStatus == 5)
                {
                    pVCClaimVM.ExpenseStatusName = "Voided";

                }
                else if (mc.ApprovalStatus == -5)
                {
                    pVCClaimVM.ExpenseStatusName = "Requested to Void";

                }
                else if (mc.ApprovalStatus == 6)
                {
                    pVCClaimVM.ExpenseStatusName = "Awaiting approval";

                }
                else if (mc.ApprovalStatus == 7)
                {
                    pVCClaimVM.ExpenseStatusName = "Awaiting HOD approval";
                }
                else
                {
                    pVCClaimVM.ExpenseStatusName = "New";
                }


                if (mc.UserApprovers != "")
                {
                    pVCClaimVM.Approver = mc.UserApprovers.Split(',').First();
                    if (pVCClaimVM.Approver == HttpContext.User.FindFirst("userid").Value && (pVCClaimVM.ApprovalStatus == 6))
                    {
                        pVCClaimVM.IsActionAllowed = true;
                    }
                }
                else if (mc.HODApprover != "")
                {
                    pVCClaimVM.Approver = mc.HODApprover.Split(',').First();
                    if (pVCClaimVM.Approver == HttpContext.User.FindFirst("userid").Value && (pVCClaimVM.ApprovalStatus == 7))
                    {
                        pVCClaimVM.IsActionAllowed = true;
                    }
                }
                else if (mc.Verifier != "")
                {
                    pVCClaimVM.Approver = mc.Verifier.Split(',').First();
                    if (pVCClaimVM.Approver == HttpContext.User.FindFirst("userid").Value && (pVCClaimVM.ApprovalStatus == 1 || pVCClaimVM.ApprovalStatus == 2))
                    {
                        pVCClaimVM.IsActionAllowed = true;
                    }
                    //string VerifierIDs = string.Join(",", PVCverifierIDs.Skip(1));
                }
                else if (mc.Approver != "")
                {
                    pVCClaimVM.Approver = mc.Approver.Split(',').First();
                    if (pVCClaimVM.Approver == HttpContext.User.FindFirst("userid").Value && (pVCClaimVM.ApprovalStatus == 1 || pVCClaimVM.ApprovalStatus == 2))
                    {
                        pVCClaimVM.IsActionAllowed = true;
                    }
                }
                else
                {
                    pVCClaimVM.Approver = "";
                }

                if (pVCClaimVM.Approver != "")
                {
                    var mstUserApprover = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(pVCClaimVM.Approver));
                    pVCClaimVM.Approver = mstUserApprover.Name;
                }
                pVCClaimVMs.Add(pVCClaimVM);
            }
            return PartialView("GetPVCPrint", pVCClaimVMs);
        }

        public async Task<JsonResult> UpdateStatus(string id)
        {
            bool isAlternateApprover = false;
            if (User != null && User.Identity.IsAuthenticated)
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
                                    var mstVerifierDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(approverID));
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
                int PVCCID = Convert.ToInt32(id);

                var mstPVCClaim = await _repository.MstPVCClaim.GetPVCClaimByIdAsync(PVCCID);

                if (mstPVCClaim == null)
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

        public async Task<IActionResult> Download(string id, string name)
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
                        //clsdtExpenseQuery.NotificationStatus = false;
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

        #endregion GetMessages
    }
}
