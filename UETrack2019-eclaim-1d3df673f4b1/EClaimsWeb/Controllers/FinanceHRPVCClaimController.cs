using AutoMapper;
using ClosedXML.Excel;
using EClaimsEntities;
using EClaimsEntities.Models;
using EClaimsRepository.Contracts;
using EClaimsWeb.Helpers;
using EClaimsWeb.Models;
using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
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
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace EClaimsWeb.Controllers
{
    [Authorize(Roles = "Admin,Finance")]
    public class FinanceHRPVCClaimController : Controller
    {
        private ILoggerManager _logger;
        private IRepositoryWrapper _repository;
        private IMapper _mapper;
        private IConfiguration _configuration;
        private AlternateApproverHelper _alternateApproverHelper;
        private ISendMailServices _sendMailServices;
        private readonly RepositoryContext _context;

        public FinanceHRPVCClaimController(ILoggerManager logger, IRepositoryWrapper repository, IMapper mapper, RepositoryContext context, IConfiguration configuration, ISendMailServices sendMailServices)
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

                var mstHRPVCClaimsWithDetails = await _repository.MstHRPVCClaim.GetAllHRPVCClaimWithDetailsByFacilityIDAsync(userID, facilityID, statusID, fromDate, toDate);
                if (mstHRPVCClaimsWithDetails != null && mstHRPVCClaimsWithDetails.Any())
                {
                    mstHRPVCClaimsWithDetails.ToList().ForEach(c => c.IsDelegated = false);
                }

                if (delegatedUserId != null && delegatedUserId.HasValue)
                {
                    var delegatedClaims = await _repository.MstHRPVCClaim.GetAllHRPVCClaimWithDetailsByFacilityIDAsync(delegatedUserId.Value, facilityID, statusID, fromDate, toDate);
                    if (delegatedClaims != null && delegatedClaims.Any())
                    {
                        delegatedClaims.ToList().ForEach(c => c.IsDelegated = true);
                        mstHRPVCClaimsWithDetails.ToList().AddRange(delegatedClaims.ToList());
                    }
                }

                _logger.LogInfo($"Returned all HRPVC Claims with details from database.");
                List<CustomHRPVCClaim> hRPVCClaimVMs = new List<CustomHRPVCClaim>();
                foreach (var mc in mstHRPVCClaimsWithDetails)
                {
                    CustomHRPVCClaim hRPVCClaimVM = new CustomHRPVCClaim();
                    hRPVCClaimVM.HRPVCCID = mc.HRPVCCID;
                    hRPVCClaimVM.HRPVCCNo = mc.HRPVCCNo;
                    hRPVCClaimVM.Name = mc.Name;
                    hRPVCClaimVM.ParticularsOfPayment = mc.ParticularsOfPayment;
                    hRPVCClaimVM.CreatedDate = DateTime.ParseExact(mc.CreatedDate, "MM/dd/yyyy HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture)
                                                             .ToString("dd/MM/yyyy", System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
                    hRPVCClaimVM.FacilityName = mc.FacilityName;
                    hRPVCClaimVM.Phone = mc.Phone;
                    hRPVCClaimVM.GrandTotal = mc.GrandTotal;
                    hRPVCClaimVM.ApprovalStatus = mc.ApprovalStatus;
                    hRPVCClaimVM.TotalAmount = mc.TotalAmount;
                    hRPVCClaimVM.PayeeName = mc.PayeeName;
                    hRPVCClaimVM.ChequeNo = mc.ChequeNo;
                    hRPVCClaimVM.Amount = mc.Amount;
                    hRPVCClaimVM.VoucherNo = mc.VoucherNo;

                    if (mc.UserApprovers != "")
                    {
                        hRPVCClaimVM.Approver = mc.UserApprovers.Split(',').First();
                        if ((hRPVCClaimVM.Approver == HttpContext.User.FindFirst("userid").Value || (delegatedUserId.HasValue && hRPVCClaimVM.Approver == delegatedUserId.Value.ToString())) &&
                            (hRPVCClaimVM.ApprovalStatus == 6))
                        {
                            hRPVCClaimVM.IsActionAllowed = false;
                        }
                    }
                    else if (mc.HODApprover != "")
                    {
                        hRPVCClaimVM.Approver = mc.HODApprover.Split(',').First();
                        if ((hRPVCClaimVM.Approver == HttpContext.User.FindFirst("userid").Value || (delegatedUserId.HasValue && hRPVCClaimVM.Approver == delegatedUserId.Value.ToString())) &&
                            (hRPVCClaimVM.ApprovalStatus == 7))
                        {
                            hRPVCClaimVM.IsActionAllowed = false;
                        }
                    }
                    else if (mc.Verifier != "")
                    {
                        hRPVCClaimVM.Approver = mc.Verifier.Split(',').First();
                        if ((hRPVCClaimVM.Approver == HttpContext.User.FindFirst("userid").Value || (delegatedUserId.HasValue && hRPVCClaimVM.Approver == delegatedUserId.Value.ToString())) &&
                            (hRPVCClaimVM.ApprovalStatus == 1 || hRPVCClaimVM.ApprovalStatus == 2))
                        {
                            hRPVCClaimVM.IsActionAllowed = true;
                        }
                        //string VerifierIDs = string.Join(",", PVCverifierIDs.Skip(1));
                    }
                    else if (mc.Approver != "")
                    {
                        hRPVCClaimVM.Approver = mc.Approver.Split(',').First();
                        if ((hRPVCClaimVM.Approver == HttpContext.User.FindFirst("userid").Value || (delegatedUserId.HasValue && hRPVCClaimVM.Approver == delegatedUserId.Value.ToString())) &&
                            (hRPVCClaimVM.ApprovalStatus == 1 || hRPVCClaimVM.ApprovalStatus == 2))
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

                    // Show actions based on alternate approver settings
                    // Override all the isActionAllowed code above. When alternate approval is set, then no need to show the action on any scenario
                    if (isAlternateApproverSet)
                    {
                        hRPVCClaimVM.IsActionAllowed = false;
                    }

                    hRPVCClaimVMs.Add(hRPVCClaimVM);
                }

                var mstHRPVCClaimVM = new HRPVCClaimSearchViewModel
                {
                    //Screens = new SelectList(await screenQuery.Distinct().ToListAsync()),
                    customHRPVCClaimVMs = hRPVCClaimVMs,
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
                _logger.LogError($"Something went wrong inside GetAllHRPVCClaimWithDetailsAsync action: {ex.Message}");
                return View();
            }
        }

        public async Task<IActionResult> Create(string id, string Updatestatus)
        {
            //TempData["CBRID"] = 0;
            TempData["Updatestatus"] = "Add";
            HRPVCClaimDetailVM hrpvcClaimDetailVM = new HRPVCClaimDetailVM();
            hrpvcClaimDetailVM.DtHRPVCClaimVMs = new List<DtHRPVCClaimVM>();
            hrpvcClaimDetailVM.HRPVCClaimAudits = new List<HRPVCClaimAuditVM>();

            if (User != null && User.Identity.IsAuthenticated)
            {
                if (!string.IsNullOrEmpty(id))
                {
                    long idd = Convert.ToInt64(id);
                    ViewBag.CID = idd;
                    var dtHRPVCClaims = await _repository.DtHRPVCClaim.GetDtHRPVCClaimByIdAsync(idd);

                    // List<clsDtSupplierPO> oclsDtSupplierPO = new List<clsDtSupplierPO>();
                    foreach (var item in dtHRPVCClaims)
                    {
                        DtHRPVCClaimVM dtHRPVCClaimVM = new DtHRPVCClaimVM();

                        dtHRPVCClaimVM.HRPVCCItemID = item.HRPVCCItemID;
                        dtHRPVCClaimVM.HRPVCCID = item.HRPVCCID;
                        dtHRPVCClaimVM.StaffName = item.StaffName;
                        dtHRPVCClaimVM.Reason = item.Reason;
                        dtHRPVCClaimVM.EmployeeNo = item.EmployeeNo;
                        dtHRPVCClaimVM.ChequeNo = item.ChequeNo;
                        dtHRPVCClaimVM.Amount = item.Amount;
                        dtHRPVCClaimVM.GST = item.GST;
                        dtHRPVCClaimVM.AmountWithGST = item.Amount + item.GST;
                        dtHRPVCClaimVM.Facility = item.Facility;
                        dtHRPVCClaimVM.AccountCode = item.AccountCode;
                        //dtHRPVCClaimVM.FacilityID = item.FacilityID;
                        hrpvcClaimDetailVM.DtHRPVCClaimVMs.Add(dtHRPVCClaimVM);
                    }

                    hrpvcClaimDetailVM.HRPVCClaimFileUploads = new List<DtHRPVCClaimFileUpload>();

                    hrpvcClaimDetailVM.HRPVCClaimFileUploads = await _repository.DtHRPVCClaimFileUpload.GetDtHRPVCClaimAuditByIdAsync(idd);

                    var mstHRPVCClaim = await _repository.MstHRPVCClaim.GetHRPVCClaimByIdAsync(idd);


                    HRPVCClaimVM hrpvcClaimVM = new HRPVCClaimVM();
                    hrpvcClaimVM.VoucherNo = mstHRPVCClaim.VoucherNo;
                    hrpvcClaimVM.ChequeNo = mstHRPVCClaim.ChequeNo;
                    hrpvcClaimVM.ParticularsOfPayment = mstHRPVCClaim.ParticularsOfPayment;
                    hrpvcClaimVM.Amount = mstHRPVCClaim.Amount;
                    hrpvcClaimVM.GrandTotal = mstHRPVCClaim.GrandTotal;
                    hrpvcClaimVM.TotalAmount = mstHRPVCClaim.TotalAmount;
                    //hrpvcClaimVM.Company = mstHRPVCClaim.Company;
                    hrpvcClaimVM.Name = mstHRPVCClaim.MstUser.Name;
                    hrpvcClaimVM.DepartmentName = mstHRPVCClaim.MstDepartment.Department;
                    hrpvcClaimVM.FacilityName = mstHRPVCClaim.MstFacility.FacilityName;
                    hrpvcClaimVM.CreatedDate = mstHRPVCClaim.CreatedDate.ToString("d");
                    hrpvcClaimVM.Verifier = mstHRPVCClaim.Verifier;
                    hrpvcClaimVM.Approver = mstHRPVCClaim.Approver;
                    hrpvcClaimVM.HRPVCCNo = mstHRPVCClaim.HRPVCCNo;

                    hrpvcClaimDetailVM.HRPVCClaimVM = hrpvcClaimVM;

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
                    hrpvcClaimDetailVM.HRPVCClaimAudits = new List<HRPVCClaimAuditVM>();
                    hrpvcClaimDetailVM.HRPVCClaimFileUploads = new List<DtHRPVCClaimFileUpload>();
                    HRPVCClaimVM hrpvcClaimVM = new HRPVCClaimVM();
                    hrpvcClaimVM.GrandTotal = 0;
                    hrpvcClaimVM.TotalAmount = 0;
                    hrpvcClaimVM.Company = "";
                    hrpvcClaimVM.Name = "";
                    hrpvcClaimVM.DepartmentName = "";
                    hrpvcClaimVM.FacilityName = "";
                    hrpvcClaimVM.CreatedDate = "";
                    hrpvcClaimVM.Verifier = "";
                    hrpvcClaimVM.Approver = "";
                    hrpvcClaimVM.HRPVCCNo = "";

                    DtHRPVCClaimVM dtHRPVCClaimVM = new DtHRPVCClaimVM();

                    dtHRPVCClaimVM.HRPVCCItemID = 0;
                    dtHRPVCClaimVM.HRPVCCID = 0;
                    //dtHRPVCClaimVM.DateOfJourney = "";
                    dtHRPVCClaimVM.StaffName = "";
                    dtHRPVCClaimVM.Reason = "";
                    dtHRPVCClaimVM.EmployeeNo = "";
                    dtHRPVCClaimVM.ChequeNo = "";
                    dtHRPVCClaimVM.ChequeNo = "";
                    dtHRPVCClaimVM.Amount = 0;
                    dtHRPVCClaimVM.GST = 0;
                    dtHRPVCClaimVM.AmountWithGST = 0;
                    dtHRPVCClaimVM.Facility = "";
                    dtHRPVCClaimVM.AccountCode = "";
                    //dtHRPVCClaimVM.FacilityID = "";

                    hrpvcClaimDetailVM.DtHRPVCClaimVMs.Add(dtHRPVCClaimVM);
                    hrpvcClaimDetailVM.HRPVCClaimVM = hrpvcClaimVM;


                    TempData["status"] = "Add";
                }
                int userFacilityId = Convert.ToInt32(User.Claims.FirstOrDefault(c => c.Type == "facilityid").Value);
                var currFacility = await _repository.MstFacility.GetFacilityWithDepartmentByIdAsync(userFacilityId);
                ViewData["ExpenseCategoryID"] = new SelectList(await _repository.MstExpenseCategory.GetAllExpenseCategoriesByClaimTypesAsync("hRPVC", "active"), "ExpenseCategoryID", "Description");
                var mstUsersWithDetails = await _repository.MstUser.GetUserWithDetailsByIdAsync(Convert.ToInt32(HttpContext.User.FindFirst("userid").Value));
                ViewData["Name"] = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.GivenName).Value;
                ViewData["FacilityName"] = currFacility.FacilityName;
                ViewData["Department"] = currFacility.MstDepartment.Department;
                ViewData["UserFacilityID"] = mstUsersWithDetails.MstFacility.FacilityID;

                SelectList facilities = new SelectList(await _repository.MstFacility.GetAllFacilityAsync("active"), "FacilityID", "FacilityName");
                var userFacility = facilities.Where(x => x.Value == userFacilityId.ToString()).FirstOrDefault();
                if (userFacility != null)
                {
                    facilities.Where(x => x.Value == userFacilityId.ToString()).FirstOrDefault().Selected = true;
                }
                ViewData["FacilityID"] = facilities;
            }
            return View(hrpvcClaimDetailVM);

        }

        public async Task<JsonResult> GetTextValuesSGSummary(string id)
        {
            List<DtHRPVCClaimSummary> oDtClaimsSummaryList = new List<DtHRPVCClaimSummary>();

            try
            {
                var dtHRPVCClaimSummaries = await _repository.DtHRPVCClaimSummary.GetDtHRPVCClaimSummaryByIdAsync(Convert.ToInt64(id));

                // List<clsDtSupplierPO> oclsDtSupplierPO = new List<clsDtSupplierPO>();
                //foreach (var item in dtHRPVCClaimSummaries)
                //{
                //    DtHRPVCClaimVM dtHRPVCClaimVM = new DtHRPVCClaimVM();

                //    dtHRPVCClaimVM.HRPVCCItemID = item.HRPVCCItemID;
                //    dtHRPVCClaimVM.HRPVCCID = item.HRPVCCID;
                //    dtHRPVCClaimVM.StaffName = item.StaffName;
                //    dtHRPVCClaimVM.Reason = item.Reason;
                //    dtHRPVCClaimVM.EmployeeNo = item.EmployeeNo;
                //    dtHRPVCClaimVM.ChequeNo = item.ChequeNo;
                //    dtHRPVCClaimVM.Amount = item.Amount;
                //    dtHRPVCClaimVM.GST = item.GST;
                //    dtHRPVCClaimVM.AmountWithGST = item.Amount + item.GST;
                //    dtHRPVCClaimVM.Facility = item.Facility;
                //    dtHRPVCClaimVM.AccountCode = item.AccountCode;
                //    //dtHRPVCClaimVM.FacilityID = item.FacilityID;
                //    oDtClaimsList.Add(dtHRPVCClaimVM);
                //}
                return Json(new { DtClaimsList = dtHRPVCClaimSummaries });
            }
            catch
            {
                return Json(new { DtClaimsList = oDtClaimsSummaryList });
            }

        }

        [HttpPost]
        public async Task<JsonResult> SaveSummary(string data)
        {
            var hRPVCClaimViewModel = JsonConvert.DeserializeObject<DtHRPVCClaimSummaryVM>(data);
            var hRPVCCSummary = await _repository.DtHRPVCClaimSummary.GetDtHRPVCClaimSummaryByIdAsync(hRPVCClaimViewModel.HRPVCCID);
            foreach (var hr in hRPVCCSummary)
            {
                _repository.DtHRPVCClaimSummary.Delete(hr);
            }
            //await _repository.SaveAsync();

            foreach (var dtItem in hRPVCClaimViewModel.dtClaims)
            {
                if (dtItem.ExpenseCategory != "DBS")
                {
                    dtItem.Description = dtItem.Description.ToUpper();
                    var mstExpenseCategory = await _repository.MstExpenseCategory.ExpenseCategoriesByClaimType("HR PV-Cheque");
                    dtItem.AccountCode = mstExpenseCategory.ExpenseCode;
                }
            }

            //MstHRPVCClaimAudit mstHRPVCClaimAudit = new MstHRPVCClaimAudit();
            //mstHRPVCClaimAudit.Action = "1";
            //mstHRPVCClaimAudit.HRPVCCID = hRPVCClaimViewModel.HRPVCCID;
            //mstHRPVCClaimAudit.AuditDate = DateTime.Now;
            //mstHRPVCClaimAudit.AuditBy = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value);
            //mstHRPVCClaimAudit.SentTo = "";
            //mstHRPVCClaimAudit.Description = "Summary of Accounts Allocation Amended by " + User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.GivenName).Value + " on" + DateTime.Now;

            MstHRPVCClaimAudit auditUpdate = new MstHRPVCClaimAudit();
            auditUpdate.HRPVCCID = hRPVCClaimViewModel.HRPVCCID;
            auditUpdate.Action = "1";
            auditUpdate.AuditDate = DateTime.Now;
            auditUpdate.AuditBy = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value);
            //auditUpdate.InstanceID = 1;
            string time = DateTime.Now.ToString("tt", System.Globalization.CultureInfo.InvariantCulture);
            DateTime date = DateTime.Now;
            string formattedDate = date.ToString("dd'/'MM'/'yyyy hh:mm:ss");
            auditUpdate.Description = "Summary of Accounts Allocation Amended by " + User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.GivenName).Value.ToString() + " on " + formattedDate + " " + time + " ";
            auditUpdate.SentTo = "";
            //await _repository.MstHRPVCClaimAudit.CreateHRPVCClaimAudit(auditUpdate);
            //await _repository.SaveAsync();
            var res = await _repository.MstHRPVCClaim.SaveSummary(hRPVCClaimViewModel.HRPVCCID, hRPVCClaimViewModel.dtClaims, auditUpdate);

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

        [HttpPost]
        public async Task<JsonResult> SaveItems(string data)
        {
            var hRPVCClaimViewModel = JsonConvert.DeserializeObject<HRPVCClaimViewModel>(data);

            var mstFacility = await _repository.MstFacility.GetFacilityWithDepartmentByIdAsync(Convert.ToInt32(HttpContext.User.FindFirst("facilityid").Value));



            MstHRPVCClaim mstHRPVCClaim = new MstHRPVCClaim();
            mstHRPVCClaim.HRPVCCNo = hRPVCClaimViewModel.HRPVCCNo;
            mstHRPVCClaim.UserID = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value);
            mstHRPVCClaim.Verifier = "";
            mstHRPVCClaim.Approver = "";
            mstHRPVCClaim.FinalApprover = "";
            mstHRPVCClaim.ApprovalStatus = 1;
            mstHRPVCClaim.Amount = hRPVCClaimViewModel.Amount;
            mstHRPVCClaim.VoucherNo = hRPVCClaimViewModel.VoucherNo;
            mstHRPVCClaim.ParticularsOfPayment = hRPVCClaimViewModel.ParticularsOfPayment;
            mstHRPVCClaim.ChequeNo = hRPVCClaimViewModel.ChequeNo;
            mstHRPVCClaim.GrandTotal = hRPVCClaimViewModel.GrandTotal;
            mstHRPVCClaim.TotalAmount = hRPVCClaimViewModel.TotalAmount;
            //mstHRPVCClaim.Company = hRPVCClaimViewModel.Company;
            mstHRPVCClaim.FacilityID = Convert.ToInt32(HttpContext.User.FindFirst("facilityid").Value);
            mstHRPVCClaim.DepartmentID = mstFacility.MstDepartment.DepartmentID;
            mstHRPVCClaim.CreatedDate = DateTime.Now;
            mstHRPVCClaim.ModifiedDate = DateTime.Now;
            mstHRPVCClaim.CreatedBy = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value);
            mstHRPVCClaim.ModifiedBy = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value);
            mstHRPVCClaim.ApprovalDate = DateTime.Now;
            mstHRPVCClaim.ApprovalBy = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value);
            mstHRPVCClaim.TnC = true;

            foreach (var dtItem in hRPVCClaimViewModel.dtClaims)
            {
                /*
                var mstExpenseCategory = await _repository.MstExpenseCategory.GetExpenseCategoryWithTypesByIdAsync(dtItem.ExpenseCategoryID);

                if (mstExpenseCategory.MstCostType.CostType.ToLower().Contains("indirect cost"))
                {
                    dtItem.AccountCode = mstExpenseCategory.ExpenseCode + "-" + mstFacility.MstDepartment.Code + "-" + mstFacility.Code + mstExpenseCategory.Default;
                }
                else if (mstExpenseCategory.MstCostType.CostType.ToLower().Contains("direct cost"))
                {
                    dtItem.AccountCode = mstExpenseCategory.MstCostStructure.Code + "-" + mstFacility.MstDepartment.Code + "-" + mstFacility.Code + mstExpenseCategory.Default + mstExpenseCategory.ExpenseCode;
                }
                else if (mstExpenseCategory.MstCostType.CostType.ToLower().Contains("hq"))
                {
                    dtItem.AccountCode = mstExpenseCategory.ExpenseCode + "-" + mstFacility.MstDepartment.Code + "-" + mstFacility.Code + mstExpenseCategory.Default;
                }
                else
                {
                    dtItem.AccountCode = mstExpenseCategory.ExpenseCode + "-" + mstFacility.MstDepartment.Code + "-" + mstFacility.Code + mstExpenseCategory.Default;
                }*/
                var mstExpenseCategory = await _repository.MstExpenseCategory.ExpenseCategoriesByClaimType("HR PV-Cheque");

                //var mstExpenseCategory = await _repository.MstExpenseCategory.GetExpenseCategoryWithTypesByIdAsync(dtItem.ExpenseCategoryID);

                dtItem.AccountCode = mstExpenseCategory.ExpenseCode;
            }

            string ClaimStatus = "";
            long HRPVCCID = 0;
            try
            {
                //CBRID = Convert.ToInt32(Session["CBRID"].ToString());
                HRPVCCID = Convert.ToInt64(hRPVCClaimViewModel.HRPVCCID);
                if (HRPVCCID == 0)
                    ClaimStatus = "Add";
                else
                    ClaimStatus = "Update";
                mstHRPVCClaim.HRPVCCID = HRPVCCID;
                //mstHRPVCClaim.HRPVCCNo = hPVVCClaimViewModel.;
            }
            catch { }

            HRPVCClaimDetailVM hRPVCClaimDetailVM = new HRPVCClaimDetailVM();
            //List<DtMileageClaimVM> dtMileageClaimVMs = new List<DtMileageClaimVM>();
            hRPVCClaimDetailVM.DtHRPVCClaimVMs = new List<DtHRPVCClaimVM>();
            // List<clsDtSupplierPO> oclsDtSupplierPO = new List<clsDtSupplierPO>();
            foreach (var item in hRPVCClaimViewModel.dtClaims)
            {
                DtHRPVCClaimVM dtHRPVCClaimVM = new DtHRPVCClaimVM();

                dtHRPVCClaimVM.HRPVCCItemID = item.HRPVCCItemID;
                dtHRPVCClaimVM.HRPVCCID = item.HRPVCCID;
                dtHRPVCClaimVM.StaffName = item.StaffName;
                dtHRPVCClaimVM.Reason = item.Reason;
                dtHRPVCClaimVM.EmployeeNo = item.EmployeeNo;
                dtHRPVCClaimVM.ChequeNo = item.ChequeNo;
                dtHRPVCClaimVM.Amount = item.Amount;
                dtHRPVCClaimVM.GST = item.GST;
                dtHRPVCClaimVM.AmountWithGST = item.Amount + item.GST;
                dtHRPVCClaimVM.Facility = item.Facility;
                dtHRPVCClaimVM.FacilityID = item.FacilityID;
                dtHRPVCClaimVM.AccountCode = item.AccountCode;
                dtHRPVCClaimVM.Date = item.Date;
                hRPVCClaimDetailVM.DtHRPVCClaimVMs.Add(dtHRPVCClaimVM);
            }

            var GroupByQS = hRPVCClaimDetailVM.DtHRPVCClaimVMs.GroupBy(s => s.ExpenseCategoryID);

            hRPVCClaimDetailVM.DtHRPVCClaimVMSummary = new List<DtHRPVCClaimVM>();

            foreach (var group in GroupByQS)
            {
                DtHRPVCClaimVM dtHRPVCClaimVM = new DtHRPVCClaimVM();
                decimal amount = 0;
                decimal gst = 0;
                decimal sumamount = 0;
                string Facility = string.Empty;
                string ExpenseDesc = string.Empty;
                string ExpenseCat = string.Empty;
                string AccountCode = string.Empty;
                int? facilityID = 0;
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
                    facilityID = dtExpense.FacilityID;
                    if (dtExpense.FacilityID != null)
                    {
                        var mstFacility1 = await _repository.MstFacility.GetFacilityByIdAsync(dtExpense.FacilityID);
                        Facility = mstFacility1.FacilityName;
                    }
                    AccountCode = dtExpense.AccountCode;
                }
                gst = gst / group.Count();
                dtHRPVCClaimVM.Particulars = ExpenseDesc;
                dtHRPVCClaimVM.ExpenseCategory = ExpenseCat;
                dtHRPVCClaimVM.FacilityID = facilityID;
                dtHRPVCClaimVM.Facility = Facility;
                dtHRPVCClaimVM.AccountCode = AccountCode;
                dtHRPVCClaimVM.Amount = amount;
                //dtMileageClaimVM.Gst = gst;
                //dtTBClaimVM.AmountWithGST = sumamount;
                hRPVCClaimDetailVM.DtHRPVCClaimVMSummary.Add(dtHRPVCClaimVM);
            }
            List<DtHRPVCClaimSummary> lstHRPVCClaimSummary = new List<DtHRPVCClaimSummary>();
            foreach (var item in hRPVCClaimDetailVM.DtHRPVCClaimVMSummary)
            {
                DtHRPVCClaimSummary dtHRPVCClaimSummary1 = new DtHRPVCClaimSummary();
                dtHRPVCClaimSummary1.AccountCode = item.AccountCode;
                dtHRPVCClaimSummary1.Amount = item.Amount;
                dtHRPVCClaimSummary1.TaxClass = 4;
                dtHRPVCClaimSummary1.ExpenseCategory = item.ExpenseCategory;
                dtHRPVCClaimSummary1.FacilityID = item.FacilityID;
                dtHRPVCClaimSummary1.Facility = item.Facility;
                dtHRPVCClaimSummary1.Description = item.Particulars.ToUpper();
                lstHRPVCClaimSummary.Add(dtHRPVCClaimSummary1);
            }

            DtHRPVCClaimSummary dtHRPVCClaimSummary = new DtHRPVCClaimSummary();
            dtHRPVCClaimSummary.AccountCode = "425000";
            dtHRPVCClaimSummary.Amount = mstHRPVCClaim.TotalAmount;
            dtHRPVCClaimSummary.TaxClass = 0;
            dtHRPVCClaimSummary.ExpenseCategory = "DBS";
            dtHRPVCClaimSummary.Description = "";
            lstHRPVCClaimSummary.Add(dtHRPVCClaimSummary);

            var res = await _repository.MstHRPVCClaim.SaveItems(mstHRPVCClaim, hRPVCClaimViewModel.dtClaims, lstHRPVCClaimSummary);
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
                    TempData["Message"] = "HR PV-Cheque Claim added successfully";
                else
                    TempData["Message"] = "HR PV-Cheque Claim updated successfully";

                return Json(new { res });
            }
            else
                return Json(new { res });
        }

        public async Task<JsonResult> UploadECFiles(List<IFormFile> files)
        {
            var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "FileUploads", "HRPVCClaimFiles");

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            // var id1 = Request.Form["Id"];
            //var id = Request.Form["Id"].ToString();

            foreach (IFormFile formFile in files)
            {
                int HRPVCCID = Convert.ToInt32(Request.Form["Id"]);
                //int MCID = Convert.ToInt32(id);
                if (formFile.Length > 0)
                {
                    int fileSize = formFile.ContentDisposition.Length;
                    string fileName = ContentDispositionHeaderValue.Parse(formFile.ContentDisposition).FileName.Trim('"');
                    string mimeType = formFile.ContentType;
                    var filePath = Path.Combine(path, formFile.FileName);
                    //System.IO.Stream fileContent = file.InputStream;
                    // string pathToFilesold = Server.MapPath("~/Fileuploads/SupplierPOFiles/") + fileName;
                    string ext = Path.GetExtension(filePath);
                    string result = Path.GetFileNameWithoutExtension(filePath);
                    string pathToFiles = Regex.Replace(result, @"[^0-9a-zA-Z]+", "_") + "-" + HRPVCCID.ToString() + "-" + DateTime.Now.ToString("ddMMyyyyss") + ext;

                    DtHRPVCClaimFileUpload dtHRPVCClaimFileUpload = new DtHRPVCClaimFileUpload();
                    dtHRPVCClaimFileUpload.HRPVCCID = HRPVCCID;
                    dtHRPVCClaimFileUpload.FileName = fileName;
                    dtHRPVCClaimFileUpload.FilePath = pathToFiles;
                    dtHRPVCClaimFileUpload.CreatedDate = DateTime.Now;
                    dtHRPVCClaimFileUpload.ModifiedDate = DateTime.Now;
                    dtHRPVCClaimFileUpload.CreatedBy = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value);
                    dtHRPVCClaimFileUpload.ModifiedBy = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value);
                    dtHRPVCClaimFileUpload.IsDeleted = false;
                    dtHRPVCClaimFileUpload.DocumentType = "2";
                    _repository.DtHRPVCClaimFileUpload.CreateDtHRPVCClaimFileUpload(dtHRPVCClaimFileUpload);
                    await _repository.SaveAsync();

                    //await _context.dtMileageClaimFileUpload.AddAsync(dtMileageClaimFileUpload);
                    //await _context.SaveChangesAsync(default);
                    //var filename = ContentDispositionHeaderValue.Parse(formFile.ContentDisposition).FileName.Trim('"');

                    //var filePath = Path.Combine(path, formFile.FileName);
                    filePath = Path.Combine(path, pathToFiles);
                    using (System.IO.Stream stream = new FileStream(filePath, FileMode.Create))
                    {
                        await formFile.CopyToAsync(stream);
                    }
                }


            }

            return Json("success");
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
            long HRPVCCID = Convert.ToInt64(id);

            if (User != null && User.Identity.IsAuthenticated)
            {
                var mstHRPVCClaim = await _repository.MstHRPVCClaim.GetHRPVCClaimByIdAsync(id);

                if (mstHRPVCClaim == null)
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
                    dtHRPVCClaimVM.Amount = item.Amount;
                    dtHRPVCClaimVM.GST = item.GST;
                    dtHRPVCClaimVM.ChequeNo = item.ChequeNo;
                    dtHRPVCClaimVM.AmountWithGST = item.Amount + item.GST;
                    //dtHRPVCClaimVM.ExpenseCategory = item.MstExpenseCategory.Description;
                    dtHRPVCClaimVM.AccountCode = item.AccountCode;
                    if (item.FacilityID != null)
                    {
                        var mstFacility = await _repository.MstFacility.GetFacilityByIdAsync(item.FacilityID);
                        dtHRPVCClaimVM.Facility = mstFacility.FacilityName;
                    }
                    //dtHRPVCClaimVM.ExpenseCategoryID = item.ExpenseCategoryID;

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

                hRPVCClaimDetailVM.HRPVCClaimFileUploads = _repository.DtHRPVCClaimFileUpload.GetDtHRPVCClaimAuditByIdAsync(id).GetAwaiter().GetResult().ToList();

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
                if (mstHRPVCClaim.Verifier != "")
                {
                    string[] verifierIDs = mstHRPVCClaim.Verifier.Split(',');
                    TempData["QueryMCVerifierIDs"] = string.Join(",", verifierIDs);
                    foreach (string verifierID in verifierIDs)
                    {
                        if ((verifierID != "" && verifierID == HttpContext.User.FindFirst("userid").Value || (delegatedUserId.HasValue && verifierID == delegatedUserId.Value.ToString())) && User.IsInRole("Finance"))
                        {
                            TempData["ApprovedStatus"] = mstHRPVCClaim.ApprovalStatus;
                            //Session["ApprovedStatus"] = oSupplierPO.Approvalstatus;
                            TempData["VerifierIDs"] = string.Join(",", verifierIDs.Skip(1));
                            hRPVCClaimVM.IsActionAllowed = true;
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
                    TempData["VerifierIDs"] = mstHRPVCClaim.Verifier;
                    TempData["ApproverIDs"] = mstHRPVCClaim.Approver;
                }

                //Approval Process code
                if (mstHRPVCClaim.Approver != "" && mstHRPVCClaim.Verifier == "")
                {
                    string[] approverIDs = mstHRPVCClaim.Approver.Split(',');
                    TempData["QueryMCApproverIDs"] = string.Join(",", approverIDs);
                    foreach (string approverID in approverIDs)
                    {
                        if ((approverID != "" && approverID == HttpContext.User.FindFirst("userid").Value || (delegatedUserId.HasValue && approverID == delegatedUserId.Value.ToString())) && User.IsInRole("Finance"))
                        {
                            TempData["ApprovedStatus"] = mstHRPVCClaim.ApprovalStatus;
                            //Session["ApprovedStatus"] = oSupplierPO.Approvalstatus;
                            TempData["ApproverIDs"] = string.Join(",", approverIDs.Skip(1));
                            hRPVCClaimVM.IsActionAllowed = true;
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

        public async Task<JsonResult> GetTextValuesSG(string id)
        {
            List<DtHRPVCClaimVM> oDtClaimsList = new List<DtHRPVCClaimVM>();

            try
            {
                var dtHRPVCClaims = await _repository.DtHRPVCClaim.GetDtHRPVCClaimByIdAsync(Convert.ToInt64(id));

                // List<clsDtSupplierPO> oclsDtSupplierPO = new List<clsDtSupplierPO>();
                foreach (var item in dtHRPVCClaims)
                {
                    DtHRPVCClaimVM dtHRPVCClaimVM = new DtHRPVCClaimVM();

                    dtHRPVCClaimVM.HRPVCCItemID = item.HRPVCCItemID;
                    dtHRPVCClaimVM.HRPVCCID = item.HRPVCCID;
                    dtHRPVCClaimVM.StaffName = item.StaffName;
                    dtHRPVCClaimVM.Reason = item.Reason;
                    dtHRPVCClaimVM.EmployeeNo = item.EmployeeNo;
                    dtHRPVCClaimVM.ChequeNo = item.ChequeNo;
                    dtHRPVCClaimVM.Amount = item.Amount;
                    dtHRPVCClaimVM.GST = item.GST;
                    dtHRPVCClaimVM.AmountWithGST = item.Amount + item.GST;
                    dtHRPVCClaimVM.Facility = item.Facility;
                    dtHRPVCClaimVM.AccountCode = item.AccountCode;
                    dtHRPVCClaimVM.FacilityID = item.FacilityID;
                    oDtClaimsList.Add(dtHRPVCClaimVM);
                }
                return Json(new { DtClaimsList = oDtClaimsList });
            }
            catch
            {
                return Json(new { DtClaimsList = oDtClaimsList });
            }

        }

        public async Task<JsonResult> UpdateStatusforVoid(string id, string reason, string approvedStatus)
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

                string financeStartDay = _configuration.GetValue<string>("FinanceStartDay");
                if (Convert.ToInt32(approvedStatus) == 3 || Convert.ToInt32(approvedStatus) == 9 || Convert.ToInt32(approvedStatus) == 10)
                {
                    await _repository.MstHRPVCClaim.UpdateMstHRPVCClaimStatus(HRPVCCID, -5, int.Parse(HttpContext.User.FindFirst("userid").Value), DateTime.Now, reason, string.Empty, string.Empty, string.Empty, string.Empty, isAlternateApprover, int.Parse(financeStartDay));
                }
                else
                {
                    await _repository.MstHRPVCClaim.UpdateMstHRPVCClaimStatus(HRPVCCID, 5, int.Parse(HttpContext.User.FindFirst("userid").Value), DateTime.Now, reason, string.Empty, string.Empty, string.Empty, string.Empty, isAlternateApprover, int.Parse(financeStartDay));
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

        public async Task<JsonResult> ExporttoExcel(string data)
        {
            var mileageClaimSearch = JsonConvert.DeserializeObject<MileageClaimSearch>(data);

            var mstHRPVCClaimsWithDetails = await _repository.MstHRPVCClaim.GetAllHRPVCClaimWithDetailsByFacilityIDAsync(mileageClaimSearch.UserID, mileageClaimSearch.FacilityID, mileageClaimSearch.StatusID, mileageClaimSearch.FromDate, mileageClaimSearch.ToDate);

            List<CustomHRPVCClaim> hRPVCClaimVMs = new List<CustomHRPVCClaim>();

            DataTable dt = new DataTable("Grid");
            dt.Columns.AddRange(new DataColumn[11] { new DataColumn("Claim"),
                                        new DataColumn("Requester"),
                                        new DataColumn("Date Created"),
                                        new DataColumn("Facility"),
                                        new DataColumn("Contact Number"),
                                        new DataColumn("Payee Name"),
                                        new DataColumn("Cheque No"),
                                        new DataColumn("Amount"),
                                        new DataColumn("Total Claim"),
                                        new DataColumn("Approver"),
                                        new DataColumn("Status")});





            foreach (var mc in mstHRPVCClaimsWithDetails)
            {
                HRPVCClaimVM hRPVCClaimVM = new HRPVCClaimVM();
                hRPVCClaimVM.ApprovalStatus = mc.ApprovalStatus;

                if (mc.ApprovalStatus == 1)
                {
                    hRPVCClaimVM.ExpenseStatusName = "Awaiting Verification";

                }
                else if (mc.ApprovalStatus == 2)
                {
                    hRPVCClaimVM.ExpenseStatusName = "Awaiting Signatory approval";

                }
                else if (mc.ApprovalStatus == 3)
                {
                    hRPVCClaimVM.ExpenseStatusName = "Approved";

                }
                else if (mc.ApprovalStatus == 4)
                {
                    hRPVCClaimVM.ExpenseStatusName = "Request to Amend";
                }
                else if (mc.ApprovalStatus == 5)
                {
                    hRPVCClaimVM.ExpenseStatusName = "Voided";

                }
                else if (mc.ApprovalStatus == -5)
                {
                    hRPVCClaimVM.ExpenseStatusName = "Requested to Void";

                }
                else if (mc.ApprovalStatus == 6)
                {
                    hRPVCClaimVM.ExpenseStatusName = "Awaiting approval";

                }
                else if (mc.ApprovalStatus == 7)
                {
                    hRPVCClaimVM.ExpenseStatusName = "Awaiting HOD approval";

                }
                else if (mc.ApprovalStatus == 9)
                {
                    hRPVCClaimVM.ExpenseStatusName = "Exported to AccPac";

                }
                else if (mc.ApprovalStatus == 10)
                {
                    hRPVCClaimVM.ExpenseStatusName = "Exported to Bank";

                }
                else
                {
                    hRPVCClaimVM.ExpenseStatusName = "New";
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
                    //string VerifierIDs = string.Join(",", PVCverifierIDs.Skip(1));
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


                dt.Rows.Add(hRPVCClaimVM.HRPVCCNo = mc.HRPVCCNo,
                            hRPVCClaimVM.Name = mc.Name,
                            hRPVCClaimVM.CreatedDate = Convert.ToDateTime(mc.CreatedDate).ToString("dd/MM/yyyy", System.Globalization.CultureInfo.CreateSpecificCulture("en-US")),
                            hRPVCClaimVM.FacilityName = mc.FacilityName,
                            hRPVCClaimVM.Phone = mc.Phone,
                            hRPVCClaimVM.Name = mc.PayeeName,
                            hRPVCClaimVM.ChequeNo = mc.ChequeNo,
                            hRPVCClaimVM.Amount = mc.Amount,
                            hRPVCClaimVM.TotalAmount = mc.TotalAmount,
                            hRPVCClaimVM.Approver = hRPVCClaimVM.Approver,
                            hRPVCClaimVM.ExpenseStatusName = hRPVCClaimVM.ExpenseStatusName);
            }

            string filename = "HRPVCClaims-Export" + DateTime.Now.ToString("ddMMyyyyss") + ".xlsx";
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
                    //dtHRPVCClaimVM.ExpenseCategoryID = item.ExpenseCategoryID;

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
            var mileageClaimSearch = JsonConvert.DeserializeObject<MileageClaimSearch>(data);
            var mstHRPVCClaimsWithDetails = await _repository.MstHRPVCClaim.GetAllHRPVCClaimWithDetailsByFacilityIDAsync(mileageClaimSearch.UserID, mileageClaimSearch.FacilityID, mileageClaimSearch.StatusID, mileageClaimSearch.FromDate, mileageClaimSearch.ToDate);
            List<CustomHRPVCClaim> hRPVCClaimVMs = new List<CustomHRPVCClaim>();


            foreach (var mc in mstHRPVCClaimsWithDetails)
            {
                CustomHRPVCClaim hRPVCClaimVM = new CustomHRPVCClaim();

                hRPVCClaimVM.HRPVCCID = mc.HRPVCCID;
                hRPVCClaimVM.HRPVCCNo = mc.HRPVCCNo;
                hRPVCClaimVM.Name = mc.Name;
                hRPVCClaimVM.ParticularsOfPayment = mc.ParticularsOfPayment;
                hRPVCClaimVM.CreatedDate = Convert.ToDateTime(mc.CreatedDate).ToString("dd/MM/yyyy", System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
                hRPVCClaimVM.FacilityName = mc.FacilityName;
                hRPVCClaimVM.Phone = mc.Phone;
                hRPVCClaimVM.GrandTotal = mc.GrandTotal;
                hRPVCClaimVM.ApprovalStatus = mc.ApprovalStatus;
                hRPVCClaimVM.TotalAmount = mc.TotalAmount;
                hRPVCClaimVM.PayeeName = mc.PayeeName;
                hRPVCClaimVM.ChequeNo = mc.ChequeNo;
                hRPVCClaimVM.Amount = mc.Amount;

                if (mc.ApprovalStatus == 1)
                {
                    hRPVCClaimVM.ExpenseStatusName = "Awaiting Verification";

                }
                else if (mc.ApprovalStatus == 2)
                {
                    hRPVCClaimVM.ExpenseStatusName = "Awaiting Signatory approval";

                }
                else if (mc.ApprovalStatus == 3)
                {
                    hRPVCClaimVM.ExpenseStatusName = "Approved";

                }
                else if (mc.ApprovalStatus == 4)
                {
                    hRPVCClaimVM.ExpenseStatusName = "Request to Amend";
                }
                else if (mc.ApprovalStatus == 5)
                {
                    hRPVCClaimVM.ExpenseStatusName = "Voided";

                }
                else if (mc.ApprovalStatus == -5)
                {
                    hRPVCClaimVM.ExpenseStatusName = "Requested to Void";

                }
                else if (mc.ApprovalStatus == 6)
                {
                    hRPVCClaimVM.ExpenseStatusName = "Awaiting approval";

                }
                else if (mc.ApprovalStatus == 7)
                {
                    hRPVCClaimVM.ExpenseStatusName = "Awaiting HOD approval";

                }
                else if (mc.ApprovalStatus == 9)
                {
                    hRPVCClaimVM.ExpenseStatusName = "Exported to AccPac";

                }
                else if (mc.ApprovalStatus == 10)
                {
                    hRPVCClaimVM.ExpenseStatusName = "Exported to Bank";

                }
                else
                {
                    hRPVCClaimVM.ExpenseStatusName = "New";
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
                    //string VerifierIDs = string.Join(",", PVCverifierIDs.Skip(1));
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
                    hRPVCClaimVM.Approver = mstUserApprover.Name;
                }
                hRPVCClaimVMs.Add(hRPVCClaimVM);
            }
            return PartialView("GetHRPVCPrint", hRPVCClaimVMs);
        }

        public async Task<JsonResult> UpdateStatus(string id)
        {
            if (User != null && User.Identity.IsAuthenticated)
            {
                int HRPVCCID = Convert.ToInt32(id);

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
                                    var mstVerifierDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(verifierID));
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
                                    var mstVerifierDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(ApproverIDs.ToString().Split(',')[0].ToString()));
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
                                    var mstVerifierDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(approverID));
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
                string financeStartDay = _configuration.GetValue<string>("FinanceStartDay");
                await _repository.MstHRPVCClaim.UpdateMstHRPVCClaimStatus(HRPVCCID, 4, int.Parse(HttpContext.User.FindFirst("userid").Value), DateTime.Now, reason, string.Empty, string.Empty, string.Empty, string.Empty, isAlternateApprover, int.Parse(financeStartDay));
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

        public async Task<IActionResult> Download(string id, string name)
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
                        return File(blobStream, file.Properties.ContentType, "HRPVCClaims-Export.xlsx");
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
                    CloudBlob file = container.GetBlobReference("FileUploads/HRPVCClaimFiles/" + id);

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

        public async Task<ActionResult> DeleteHRPVCClaimFile(string fileID, string filepath, string HRPVCCID)
        {
            DtHRPVCClaimFileUpload dtHRPVCClaimFileUpload = new DtHRPVCClaimFileUpload();
            if (CloudStorageAccount.TryParse(_configuration.GetSection("ConnectionStrings")["BlobConnectionString"], out CloudStorageAccount storageAccount))
            {
                CloudBlobClient BlobClient = storageAccount.CreateCloudBlobClient();
                CloudBlobContainer container = BlobClient.GetContainerReference(_configuration.GetSection("ConnectionStrings")["BlobContainerName"]);

                if (await container.ExistsAsync())
                {
                    CloudBlob file = container.GetBlobReference("FileUploads/HRPVCClaimFiles/" + filepath);

                    if (await file.ExistsAsync())
                    {
                        await file.DeleteIfExistsAsync();
                        dtHRPVCClaimFileUpload = await _repository.DtHRPVCClaimFileUpload.GetDtHRPVCClaimFileUploadByIdAsync(Convert.ToInt64(fileID));
                        _repository.DtHRPVCClaimFileUpload.DeleteDtHRPVCClaimFileUpload(dtHRPVCClaimFileUpload);
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

            return RedirectToAction("Create", "FinanceHRPVCClaim", new
            {
                id = HRPVCCID,
                Updatestatus = "Edit"
            });
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
                    long HRPVCCID = Convert.ToInt64(queryParamViewModel.Cid);
                    int UserID = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value);
                    // newly Added Code
                    var hRPVCClaim = await _repository.MstHRPVCClaim.GetHRPVCClaimByIdAsync(HRPVCCID);
                    for (int i = 0; i < UserIds.Length; i++)
                    {
                        MstQuery clsdtPVCQuery = new MstQuery();
                        // if (data["MessageDescription"] != null)               
                        clsdtPVCQuery.ModuleType = "HRPVC Claim";
                        //  clsdtSupplierQuery.ID = Convert.ToInt64(data["SPOID"]);
                        clsdtPVCQuery.ID = HRPVCCID;
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
                        MstHRPVCClaimAudit auditUpdate = new MstHRPVCClaimAudit();
                        auditUpdate.HRPVCCID = HRPVCCID;
                        auditUpdate.Action = "0";
                        auditUpdate.AuditDate = DateTime.Now;
                        auditUpdate.AuditBy = UserID;
                        //auditUpdate.InstanceID = 1;
                        string time = DateTime.Now.ToString("tt", System.Globalization.CultureInfo.InvariantCulture);
                        DateTime date = DateTime.Now;
                        string formattedDate = date.ToString("dd'/'MM'/'yyyy hh:mm:ss");
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
                    _logger.LogError($"Something went wrong inside CreateHRPVCClaimAudit action: {ex.Message}");
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
                var queries = await _repository.MstQuery.GetAllClaimsQueriesAsync(UserId, ecid, "HRPVC Claim");
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
