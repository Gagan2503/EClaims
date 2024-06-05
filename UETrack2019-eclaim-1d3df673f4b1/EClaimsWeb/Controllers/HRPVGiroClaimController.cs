using AutoMapper;
using Microsoft.Extensions.Configuration;
using ClosedXML.Excel;
using EClaimsEntities;
using EClaimsEntities.Models;
using EClaimsRepository.Contracts;
using EClaimsWeb.Helpers;
using EClaimsWeb.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Newtonsoft.Json.Converters;
using NToastNotify;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace EClaimsWeb.Controllers
{
    [Authorize(Roles = "Admin,Finance,User,HR")]
    public class HRPVGiroClaimController : Controller
    {
        private readonly IToastNotification _toastNotification;
        private ILoggerManager _logger;
        private IRepositoryWrapper _repository;
        private IMapper _mapper;
        private IConfiguration _configuration;
        private AlternateApproverHelper _alternateApproverHelper;
        private ISendMailServices _sendMailServices;

        private readonly RepositoryContext _context;

        public HRPVGiroClaimController(IToastNotification toastNotification, ILoggerManager logger, IRepositoryWrapper repository, IMapper mapper, RepositoryContext context, IConfiguration configuration, ISendMailServices sendMailServices)
        {
            _logger = logger;
            _repository = repository;
            _mapper = mapper;
            _context = context;
            _configuration = configuration;
            _sendMailServices = sendMailServices;
            _toastNotification = toastNotification;
            _alternateApproverHelper = new AlternateApproverHelper(logger, repository, context);
        }

        //// GET: Facility
        //public async Task<IActionResult> Index()
        //{
        //    try
        //    {
        //        var mstPVCClaimsWithDetails = await _repository.MstPVCClaim.GetAllPVCClaimWithDetailsAsync();
        //        _logger.LogInfo($"Returned all PV Giro Claims with details from database.");

        //        //var mstExpenseCategoriesWithTypesResult = _mapper.Map<IEnumerable<MstExpenseCategory>>(mstExpenseCategoriesWithTypes);
        //        return View(mstPVCClaimsWithDetails);
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError($"Something went wrong inside GetAllPVCClaimWithDetailsAsync action: {ex.Message}");
        //        return View();
        //    }
        //}


        public async Task<IActionResult> Index()
        {
            try
            {
                var approverDetails = await _repository.MstUserApprovers.GetUserApproversByUserIdAsync(Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value));
                if (approverDetails.Count() == 0)
                    ViewBag.Settings = "true";
                else
                    ViewBag.Settings = "false";

                var mstHRPVGClaimsWithDetails = await _repository.MstHRPVGClaim.GetAllHRPVGClaimWithDetailsByFacilityIDAsync(Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value), 0,0,"","");
                //List<CustomHRPVGClaim> hRPVGClaimVMs = new List<CustomHRPVGClaim>();
                HRPVGClaimsVM hRPVGClaimVMs = new HRPVGClaimsVM();
                foreach (var mc in mstHRPVGClaimsWithDetails)
                {
                    CustomHRPVGClaim hRPVGClaimVM = new CustomHRPVGClaim();
                    hRPVGClaimVM.HRPVGCID = mc.HRPVGCID;
                    hRPVGClaimVM.HRPVGCNo = mc.HRPVGCNo;
                    hRPVGClaimVM.Name = mc.Name;
                    hRPVGClaimVM.ParticularsOfPayment = mc.ParticularsOfPayment;
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
                    hRPVGClaimVM.VoucherNo = mc.VoucherNo;

                    hRPVGClaimVM.AVerifier = mc.Verifier;
                    hRPVGClaimVM.AApprover = mc.Approver;
                    hRPVGClaimVM.AUserApprovers = mc.UserApprovers;
                    hRPVGClaimVM.AHODApprover = mc.HODApprover;

                    hRPVGClaimVM.DVerifier = mc.DVerifier;
                    hRPVGClaimVM.DApprover = mc.DApprover;
                    hRPVGClaimVM.DUserApprovers = mc.DUserApprovers;
                    hRPVGClaimVM.DHODApprover = mc.DHODApprover;

                    if (mc.UserApprovers != "")
                    {
                        hRPVGClaimVM.Approver = mc.UserApprovers.Split(',').First();
                    }
                    else if (mc.HODApprover != "")
                    {
                        hRPVGClaimVM.Approver = mc.HODApprover.Split(',').First();
                    }
                    else if (mc.Verifier != "")
                    {
                        hRPVGClaimVM.Approver = mc.Verifier.Split(',').First();
                        //string VerifierIDs = string.Join(",", PVCverifierIDs.Skip(1));
                    }
                    else if (mc.Approver != "")
                    {
                        hRPVGClaimVM.Approver = mc.Approver.Split(',').First();
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

                    hRPVGClaimVMs.hRPvcClaims.Add(hRPVGClaimVM);
                    _logger.LogInfo($"Returned all PV Giro Claims with details from database.");
                }
                var mstHRPVGClaimsWithDraftDetails = await _repository.MstHRPVGClaimDraft.GetAllHRPVGClaimWithDraftDetailsByFacilityIDAsync(Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value), 0, 0, "", "");
                foreach (var mc in mstHRPVGClaimsWithDraftDetails)
                {
                    CustomHRPVGClaim hRPVGClaimVM = new CustomHRPVGClaim();
                    hRPVGClaimVM.HRPVGCID = mc.HRPVGCID;
                    hRPVGClaimVM.HRPVGCNo = mc.HRPVGCNo;
                    hRPVGClaimVM.Name = mc.Name;
                    hRPVGClaimVM.ParticularsOfPayment = mc.ParticularsOfPayment;
                    hRPVGClaimVM.CreatedDate = Convert.ToDateTime(mc.CreatedDate).ToString("dd/MM/yyyy", System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
                    hRPVGClaimVM.FacilityName = mc.FacilityName;
                    hRPVGClaimVM.Phone = mc.Phone;
                    hRPVGClaimVM.GrandTotal = mc.GrandTotal;
                    hRPVGClaimVM.ApprovalStatus = mc.ApprovalStatus;
                    hRPVGClaimVM.TotalAmount = mc.TotalAmount;
                    hRPVGClaimVM.Amount = mc.Amount;
                    hRPVGClaimVM.PayeeName = mc.PayeeName;
                    hRPVGClaimVM.PaymentMode = mc.PaymentMode;
                    hRPVGClaimVM.VoucherNo = mc.VoucherNo;

                    if (mc.UserApprovers != "")
                    {
                        hRPVGClaimVM.Approver = mc.UserApprovers.Split(',').First();
                    }
                    else if (mc.HODApprover != "")
                    {
                        hRPVGClaimVM.Approver = mc.HODApprover.Split(',').First();
                    }
                    else if (mc.Verifier != "")
                    {
                        hRPVGClaimVM.Approver = mc.Verifier.Split(',').First();
                        //string VerifierIDs = string.Join(",", PVCverifierIDs.Skip(1));
                    }
                    else if (mc.Approver != "")
                    {
                        hRPVGClaimVM.Approver = mc.Approver.Split(',').First();
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

                    hRPVGClaimVMs.hRPvcClaimsDrafts.Add(hRPVGClaimVM);
                    _logger.LogInfo($"Returned all PV Giro Claims with details from database.");
                }
                //var mstExpenseCategoriesWithTypesResult = _mapper.Map<IEnumerable<MstExpenseCategory>>(mstExpenseCategoriesWithTypes);
                return View(hRPVGClaimVMs);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Something went wrong inside GetAllHRPVGClaimWithDetailsAsync action: {ex.Message}");
                return View();
            }

        }

        public async Task<IActionResult> FinanceHRPVGiro()
        {
            try
            {
                var mstHRPVGClaimsWithDetails = await _repository.MstHRPVGClaim.GetAllHRPVGClaimWithDetailsAsync();
                _logger.LogInfo($"Returned all HRPV Giro Claims with details from database.");

                //var mstExpenseCategoriesWithTypesResult = _mapper.Map<IEnumerable<MstExpenseCategory>>(mstExpenseCategoriesWithTypes);
                return View(mstHRPVGClaimsWithDetails);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Something went wrong inside GetAllHRPVGClaimWithDetailsAsync action: {ex.Message}");
                return View();
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

            return RedirectToAction("Create", "HRPVGiroClaim", new
            {
                id = HRPVGCID,
                Updatestatus = "Edit"
            });
        }
        public async Task<IActionResult> Create(string id, string Updatestatus)
        {
            //TempData["CBRID"] = 0;
            TempData["Updatestatus"] = "Add";
            HRPVGClaimDetailVM hRPVGClaimDetailVM = new HRPVGClaimDetailVM();
            hRPVGClaimDetailVM.DtHRPVGClaimVMs = new List<DtHRPVGClaimVM>();
            hRPVGClaimDetailVM.HRPVGClaimAudits = new List<HRPVGClaimAuditVM>();

            TempData["claimaddcondition"] = "claimnew";

            if (User != null && User.Identity.IsAuthenticated)
            {
                if (!string.IsNullOrEmpty(id))
                {
                    long idd = Convert.ToInt64(id);
                    ViewBag.CID = idd;
                    var dtHRPVGClaims = await _repository.DtHRPVGClaim.GetDtHRPVGClaimByIdAsync(idd);

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
                        dtHRPVGClaimVM.Date = item.Date;
                        dtHRPVGClaimVM.Bank = item.Bank;
                        dtHRPVGClaimVM.BankCode = item.BankCode;
                        dtHRPVGClaimVM.BranchCode = item.BranchCode;
                        dtHRPVGClaimVM.BankAccount = item.BankAccount;
                        dtHRPVGClaimVM.Mobile = item.Mobile;
                        if (Updatestatus == "Recreate")
                        {
                            ViewBag.UpdateStatus = "Recreate";
                            dtHRPVGClaimVM.HRPVGCItemID = 0;
                        }
                        //dtHRPVGClaimVM.FacilityID = item.FacilityID;
                        hRPVGClaimDetailVM.DtHRPVGClaimVMs.Add(dtHRPVGClaimVM);
                    }

                    hRPVGClaimDetailVM.HRPVGClaimFileUploads = new List<DtHRPVGClaimFileUpload>();
                    var fileUploads = await _repository.DtHRPVGClaimFileUpload.GetDtHRPVGClaimAuditByIdAsync(idd);
                    if (Updatestatus == "Recreate" && fileUploads != null && fileUploads.Count > 0)
                    {
                        foreach (var uploaddata in fileUploads)
                        {
                            uploaddata.HRPVGCID = 0;
                            hRPVGClaimDetailVM.HRPVGClaimFileUploads.Add(uploaddata);
                        }
                    }
                    else
                        hRPVGClaimDetailVM.HRPVGClaimFileUploads = fileUploads;

                    var mstHRPVGClaim = await _repository.MstHRPVGClaim.GetHRPVGClaimByIdAsync(idd);

                    
                    HRPVGClaimVM hRPVGClaimVM = new HRPVGClaimVM();
                    hRPVGClaimVM.VoucherNo = mstHRPVGClaim.VoucherNo;
                    hRPVGClaimVM.ChequeNo = mstHRPVGClaim.ChequeNo;
                    hRPVGClaimVM.ParticularsOfPayment = mstHRPVGClaim.ParticularsOfPayment;
                    hRPVGClaimVM.Amount = mstHRPVGClaim.Amount;
                    hRPVGClaimVM.GrandTotal = mstHRPVGClaim.GrandTotal;
                    hRPVGClaimVM.TotalAmount = mstHRPVGClaim.TotalAmount;
                    //hRPVGClaimVM.Company = mstHRPVGClaim.Company;
                    hRPVGClaimVM.Name = mstHRPVGClaim.MstUser.Name;
                    hRPVGClaimVM.DepartmentName = mstHRPVGClaim.MstDepartment.Department;
                    hRPVGClaimVM.FacilityName = mstHRPVGClaim.MstFacility.FacilityName;
                    hRPVGClaimVM.CreatedDate = mstHRPVGClaim.CreatedDate.ToString("d");
                    hRPVGClaimVM.Verifier = mstHRPVGClaim.Verifier;
                    hRPVGClaimVM.Approver = mstHRPVGClaim.Approver;
                    hRPVGClaimVM.HRPVGCNo = mstHRPVGClaim.HRPVGCNo;
                    hRPVGClaimVM.PaymentMode = mstHRPVGClaim.PaymentMode;

                    hRPVGClaimDetailVM.HRPVGClaimVM = hRPVGClaimVM;

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
                    hRPVGClaimDetailVM.HRPVGClaimAudits = new List<HRPVGClaimAuditVM>();
                    hRPVGClaimDetailVM.HRPVGClaimFileUploads = new List<DtHRPVGClaimFileUpload>();
                    HRPVGClaimVM hRPVGClaimVM = new HRPVGClaimVM();
                    hRPVGClaimVM.GrandTotal = 0;
                    hRPVGClaimVM.TotalAmount = 0;
                    hRPVGClaimVM.Company = "";
                    hRPVGClaimVM.Name = "";
                    hRPVGClaimVM.DepartmentName = "";
                    hRPVGClaimVM.FacilityName = "";
                    hRPVGClaimVM.CreatedDate = "";
                    hRPVGClaimVM.Verifier = "";
                    hRPVGClaimVM.Approver = "";
                    hRPVGClaimVM.HRPVGCNo = "";

                    DtHRPVGClaimVM dtHRPVGClaimVM = new DtHRPVGClaimVM();

                    dtHRPVGClaimVM.HRPVGCItemID = 0;
                    dtHRPVGClaimVM.HRPVGCID = 0;
                    //dtHRPVGClaimVM.DateOfJourney = "";
                    dtHRPVGClaimVM.StaffName = "";
                    dtHRPVGClaimVM.Reason = "";
                    dtHRPVGClaimVM.EmployeeNo = "";
                    dtHRPVGClaimVM.ChequeNo = "";
                    dtHRPVGClaimVM.ChequeNo = "";
                    dtHRPVGClaimVM.Amount = 0;
                    dtHRPVGClaimVM.GST = 0;
                    dtHRPVGClaimVM.AmountWithGST = 0;
                    dtHRPVGClaimVM.Facility = "";
                    dtHRPVGClaimVM.AccountCode = "";
                    dtHRPVGClaimVM.Bank = "";
                    dtHRPVGClaimVM.BankCode = "";
                    dtHRPVGClaimVM.BranchCode = "";
                    dtHRPVGClaimVM.BankAccount = "";
                    dtHRPVGClaimVM.Mobile = "";
                    //dtHRPVGClaimVM.FacilityID = "";

                    hRPVGClaimDetailVM.DtHRPVGClaimVMs.Add(dtHRPVGClaimVM);
                    hRPVGClaimDetailVM.HRPVGClaimVM = hRPVGClaimVM;


                    TempData["status"] = "Add";
                }
                //int userFacilityId = mstUsersWithDetails.MstFacility.FacilityID;
                int userFacilityId = Convert.ToInt32(HttpContext.User.FindFirst("delegatefacilityid") is null ? HttpContext.User.FindFirst("facilityid").Value : HttpContext.User.FindFirst("delegatefacilityid").Value);
                var currFacility = await _repository.MstFacility.GetFacilityWithDepartmentByIdAsync(userFacilityId);
                ViewData["ExpenseCategoryID"] = new SelectList(await _repository.MstExpenseCategory.GetAllExpenseCategoriesByClaimTypesAsync("hRPVG", "active"), "ExpenseCategoryID", "Description");
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

            }
            return View(hRPVGClaimDetailVM);

        }
        
        public async Task<IActionResult> CreateDraft(string id, string Updatestatus)
        {
            //TempData["CBRID"] = 0;
            TempData["Updatestatus"] = "Add";
            TempData["claimaddcondition"] = "claimDraft";
            HRPVGClaimDetailVM hRPVGClaimDetailVM = new HRPVGClaimDetailVM();
            hRPVGClaimDetailVM.DtHRPVGClaimVMs = new List<DtHRPVGClaimVM>();
            hRPVGClaimDetailVM.HRPVGClaimAudits = new List<HRPVGClaimAuditVM>();

            if (User != null && User.Identity.IsAuthenticated)
            {
                if (!string.IsNullOrEmpty(id))
                {
                    long idd = Convert.ToInt64(id);
                    ViewBag.CID = idd;
                    var dtHRPVGClaims = await _repository.DtHRPVGClaimDraft.GetDtHRPVGClaimByIdAsync(idd);

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
                        dtHRPVGClaimVM.Date = item.Date;
                        dtHRPVGClaimVM.Bank = item.Bank;
                        dtHRPVGClaimVM.BankCode = item.BankCode;
                        dtHRPVGClaimVM.BranchCode = item.BranchCode;
                        dtHRPVGClaimVM.BankAccount = item.BankAccount;
                        dtHRPVGClaimVM.Mobile = item.Mobile;
                        //dtHRPVGClaimVM.FacilityID = item.FacilityID;
                        hRPVGClaimDetailVM.DtHRPVGClaimVMs.Add(dtHRPVGClaimVM);
                    }

                    hRPVGClaimDetailVM.HRPVGClaimFileUploads = new List<DtHRPVGClaimFileUpload>();

                    hRPVGClaimDetailVM.HRPVGClaimFileUploads = await _repository.DtHRPVGClaimFileUpload.GetDtHRPVGClaimAuditByIdAsync(idd);

                    var mstHRPVGClaim = await _repository.MstHRPVGClaimDraft.GetHRPVGClaimByIdAsync(idd);


                    HRPVGClaimVM hRPVGClaimVM = new HRPVGClaimVM();
                    hRPVGClaimVM.VoucherNo = mstHRPVGClaim.VoucherNo;
                    hRPVGClaimVM.ChequeNo = mstHRPVGClaim.ChequeNo;
                    hRPVGClaimVM.ParticularsOfPayment = mstHRPVGClaim.ParticularsOfPayment;
                    hRPVGClaimVM.Amount = mstHRPVGClaim.Amount;
                    hRPVGClaimVM.GrandTotal = mstHRPVGClaim.GrandTotal;
                    hRPVGClaimVM.TotalAmount = mstHRPVGClaim.TotalAmount;
                    //hRPVGClaimVM.Company = mstHRPVGClaim.Company;
                    hRPVGClaimVM.Name = mstHRPVGClaim.MstUser.Name;
                    hRPVGClaimVM.DepartmentName = mstHRPVGClaim.MstDepartment.Department;
                    hRPVGClaimVM.FacilityName = mstHRPVGClaim.MstFacility.FacilityName;
                    hRPVGClaimVM.CreatedDate = mstHRPVGClaim.CreatedDate.ToString("d");
                    hRPVGClaimVM.Verifier = mstHRPVGClaim.Verifier;
                    hRPVGClaimVM.Approver = mstHRPVGClaim.Approver;
                    hRPVGClaimVM.HRPVGCNo = mstHRPVGClaim.HRPVGCNo;
                    hRPVGClaimVM.PaymentMode = mstHRPVGClaim.PaymentMode;

                    hRPVGClaimDetailVM.HRPVGClaimVM = hRPVGClaimVM;

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
                    hRPVGClaimDetailVM.HRPVGClaimAudits = new List<HRPVGClaimAuditVM>();
                    hRPVGClaimDetailVM.HRPVGClaimFileUploads = new List<DtHRPVGClaimFileUpload>();
                    HRPVGClaimVM hRPVGClaimVM = new HRPVGClaimVM();
                    hRPVGClaimVM.GrandTotal = 0;
                    hRPVGClaimVM.TotalAmount = 0;
                    hRPVGClaimVM.Company = "";
                    hRPVGClaimVM.Name = "";
                    hRPVGClaimVM.DepartmentName = "";
                    hRPVGClaimVM.FacilityName = "";
                    hRPVGClaimVM.CreatedDate = "";
                    hRPVGClaimVM.Verifier = "";
                    hRPVGClaimVM.Approver = "";
                    hRPVGClaimVM.HRPVGCNo = "";

                    DtHRPVGClaimVM dtHRPVGClaimVM = new DtHRPVGClaimVM();

                    dtHRPVGClaimVM.HRPVGCItemID = 0;
                    dtHRPVGClaimVM.HRPVGCID = 0;
                    //dtHRPVGClaimVM.DateOfJourney = "";
                    dtHRPVGClaimVM.StaffName = "";
                    dtHRPVGClaimVM.Reason = "";
                    dtHRPVGClaimVM.EmployeeNo = "";
                    dtHRPVGClaimVM.ChequeNo = "";
                    dtHRPVGClaimVM.ChequeNo = "";
                    dtHRPVGClaimVM.Amount = 0;
                    dtHRPVGClaimVM.GST = 0;
                    dtHRPVGClaimVM.AmountWithGST = 0;
                    dtHRPVGClaimVM.Facility = "";
                    dtHRPVGClaimVM.AccountCode = "";
                    dtHRPVGClaimVM.Bank = "";
                    dtHRPVGClaimVM.BankCode = "";
                    dtHRPVGClaimVM.BranchCode = "";
                    dtHRPVGClaimVM.BankAccount = "";
                    dtHRPVGClaimVM.Mobile = "";
                    //dtHRPVGClaimVM.FacilityID = "";

                    hRPVGClaimDetailVM.DtHRPVGClaimVMs.Add(dtHRPVGClaimVM);
                    hRPVGClaimDetailVM.HRPVGClaimVM = hRPVGClaimVM;


                    TempData["status"] = "Add";
                }
                ViewData["ExpenseCategoryID"] = new SelectList(await _repository.MstExpenseCategory.GetAllExpenseCategoriesByClaimTypesAsync("hRPVG", "active"), "ExpenseCategoryID", "Description");
                var mstUsersWithDetails = await _repository.MstUser.GetUserWithDetailsByIdAsync(Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value));
                var delegatedUserName = string.Empty;
                if (HttpContext.User.FindFirst("delegateuserid") is not null)
                {
                    var delUserDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid").Value));
                    delegatedUserName = delUserDetails.Name;
                }

                ViewData["Name"] = string.IsNullOrEmpty(delegatedUserName) ? User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.GivenName).Value : delegatedUserName + "(" + User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.GivenName).Value + ")";
                ViewData["FacilityName"] = mstUsersWithDetails.MstFacility.FacilityName;
                ViewData["Department"] = mstUsersWithDetails.MstFacility.MstDepartment.Department;

                SelectList facilities = new SelectList(await _repository.MstFacility.GetAllFacilityAsync("active"), "FacilityID", "FacilityName");
                int userFacilityId = mstUsersWithDetails.MstFacility.FacilityID;
                var userFacility = facilities.Where(x => x.Value == userFacilityId.ToString()).FirstOrDefault();
                if (userFacility != null)
                {
                    facilities.Where(x => x.Value == userFacilityId.ToString()).FirstOrDefault().Selected = true;
                }
                ViewData["FacilityID"] = facilities;
                SelectList bankSwiftBICs = new SelectList(await _repository.MstBankSwiftBIC.GetAllBankSwiftBICAsync(), "BankCode", "BankName");
                ViewData["BankSwiftBICs"] = bankSwiftBICs;
            }
           
            return View("Create", hRPVGClaimDetailVM);

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
                    dtHRPVGClaimVM.Date = item.Date;
                    dtHRPVGClaimVM.Bank = item.Bank;
                    dtHRPVGClaimVM.BankCode = item.BankCode;
                    dtHRPVGClaimVM.BankSWIFTBIC = item.BankSwiftBIC;
                    dtHRPVGClaimVM.BranchCode = item.BranchCode;
                    dtHRPVGClaimVM.BankAccount = item.BankAccount;
                    dtHRPVGClaimVM.Mobile = item.Mobile;
                    dtHRPVGClaimVM.FacilityID = item.FacilityID;
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
        public async Task<JsonResult> GetTextValuesSGDraft(string id)
        {
            List<DtHRPVGClaimVM> oDtClaimsList = new List<DtHRPVGClaimVM>();

            try
            {
                var dtHRPVGClaims = await _repository.DtHRPVGClaimDraft.GetDtHRPVGClaimByIdAsync(Convert.ToInt64(id));

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
                    dtHRPVGClaimVM.Date = item.Date;
                    dtHRPVGClaimVM.Bank = item.Bank;
                    dtHRPVGClaimVM.BankCode = item.BankCode;
                    dtHRPVGClaimVM.BankSWIFTBIC = item.BankSwiftBIC;
                    dtHRPVGClaimVM.BranchCode = item.BranchCode;
                    dtHRPVGClaimVM.BankAccount = item.BankAccount;
                    dtHRPVGClaimVM.Mobile = item.Mobile;
                    dtHRPVGClaimVM.FacilityID = item.FacilityID;
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
        public async Task<IActionResult> DeleteHRPVGClaimDraft(string id)
        {
            try
            {
                long idd = Convert.ToInt64(id);
                var pvcClaimsDraft = await _repository.MstHRPVGClaimDraft.GetHRPVGClaimByIdAsync(idd);
                _repository.MstHRPVGClaimDraft.DeleteHRPVGClaim(pvcClaimsDraft);
                await _repository.SaveAsync();
                TempData["Message"] = "Draft deleted successfully";
                Content("<script language='javascript' type='text/javascript'>alert('Draft deleted successfully');</script>");
                return RedirectToAction("Index", "HRPVChequeClaim");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Something went wrong inside DeletePVCClaimDraft action: {ex.Message}");
            }
            return Json(null);
        }
        public async Task<IActionResult> Details(long? id)
        {
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
                    dtHRPVGClaimVM.Facility = item.Facility;
                    dtHRPVGClaimVM.AccountCode = item.AccountCode;
                    dtHRPVGClaimVM.Date = item.Date;
                    dtHRPVGClaimVM.Bank = item.Bank;
                    dtHRPVGClaimVM.BankCode = item.BankCode;
                    dtHRPVGClaimVM.BankSWIFTBIC = item.BankSwiftBIC;
                    dtHRPVGClaimVM.BranchCode = item.BranchCode;
                    dtHRPVGClaimVM.BankAccount = item.BankAccount;
                    dtHRPVGClaimVM.Mobile = item.Mobile;

                    if (item.FacilityID != null)
                    {
                        var mstFacility = await _repository.MstFacility.GetFacilityByIdAsync(item.FacilityID);
                        dtHRPVGClaimVM.Facility = mstFacility.FacilityName;
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

                    hRPVGClaimDetailVM.DtHRPVGClaimVMs.Add(dtHRPVGClaimVM);
                }

                var GroupByQS = hRPVGClaimDetailVM.DtHRPVGClaimVMs.GroupBy(s => s.AccountCode);
                hRPVGClaimDetailVM.DtHRPVGClaimSummaries = dtHRPVGSummaries;
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

                HRPVGClaimVM HRPVGClaimVM = new HRPVGClaimVM();
                HRPVGClaimVM.VoucherNo = mstHRPVGClaim.VoucherNo;
                HRPVGClaimVM.ChequeNo = mstHRPVGClaim.ChequeNo;
                HRPVGClaimVM.ParticularsOfPayment = mstHRPVGClaim.ParticularsOfPayment;
                HRPVGClaimVM.Amount = mstHRPVGClaim.Amount;
                HRPVGClaimVM.GrandTotal = mstHRPVGClaim.GrandTotal;
                HRPVGClaimVM.TotalAmount = mstHRPVGClaim.TotalAmount;
                HRPVGClaimVM.Company = "UEMS";
                HRPVGClaimVM.Name = mstHRPVGClaim.MstUser.Name;
                HRPVGClaimVM.DepartmentName = mstHRPVGClaim.MstDepartment.Department;
                HRPVGClaimVM.FacilityName = mstHRPVGClaim.MstFacility.FacilityName;
                HRPVGClaimVM.CreatedDate = Convert.ToDateTime(mstHRPVGClaim.CreatedDate).ToString("dd/MM/yyyy", System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
                HRPVGClaimVM.Verifier = mstHRPVGClaim.Verifier;
                HRPVGClaimVM.Approver = mstHRPVGClaim.Approver;
                HRPVGClaimVM.HRPVGCNo = mstHRPVGClaim.HRPVGCNo;
                HRPVGClaimVM.PaymentMode = mstHRPVGClaim.PaymentMode;
                ViewBag.HRPVGCID = id;
                TempData["CreatedBy"] = mstHRPVGClaim.CreatedBy;
                ViewBag.Approvalstatus = mstHRPVGClaim.ApprovalStatus;

                if (mstHRPVGClaim.Verifier == mstHRPVGClaim.DVerifier && mstHRPVGClaim.Approver == mstHRPVGClaim.DApprover && mstHRPVGClaim.UserApprovers == mstHRPVGClaim.DUserApprovers && mstHRPVGClaim.HODApprover == mstHRPVGClaim.DHODApprover)
                {
                    ViewBag.UserEditStatus = 4;
                }
                else
                {
                    ViewBag.UserEditStatus = 0;
                }

                TempData["ApprovedStatus"] = mstHRPVGClaim.ApprovalStatus;
                TempData["FinalApproverID"] = mstHRPVGClaim.FinalApprover;
                ViewBag.VoidReason = mstHRPVGClaim.VoidReason == null ? "" : mstHRPVGClaim.VoidReason;

                if (TempData["ApprovedStatus"].ToString() == "1" || TempData["ApprovedStatus"].ToString() == "2" || TempData["ApprovedStatus"].ToString() == "3" || TempData["ApprovedStatus"].ToString() == "-5" || TempData["ApprovedStatus"].ToString() == "6" || TempData["ApprovedStatus"].ToString() == "7" || TempData["ApprovedStatus"].ToString() == "9" || TempData["ApprovedStatus"].ToString() == "10")
                {
                    ViewBag.ShowVoidBtn = 1;
                        if (int.Parse(TempData["ApprovedStatus"].ToString()) < 3 || TempData["ApprovedStatus"].ToString() == "6" || TempData["ApprovedStatus"].ToString() == "7")
                        {
                            ViewBag.ShowVoidText = "Void";
                        }
                        else
                        {
                            ViewBag.ShowVoidText = "Request for Void";
                        }

                        if (TempData["ApprovedStatus"].ToString() == "-5" && TempData["FinalApproverID"].ToString() != (HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value))
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
                TempData["QueryMCUserApproverIDs"] = "";
                TempData["QueryMCHODUserApproverIDs"] = "";
                if (mstHRPVGClaim.Verifier != "")
                {
                    string[] verifierIDs = mstHRPVGClaim.Verifier.Split(',');
                    TempData["QueryMCVerifierIDs"] = string.Join(",", verifierIDs);
                    foreach (string verifierID in verifierIDs)
                    {
                        if (verifierID != "" && verifierID == (HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value) && User.IsInRole("Finance"))
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
                        if (approverID != "" && approverID == (HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value) && User.IsInRole("Finance"))
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

                if (mstHRPVGClaim.UserApprovers != "" && mstHRPVGClaim.Verifier == "")
                {
                    string[] userApproverIDs = mstHRPVGClaim.UserApprovers.Split(',');
                    TempData["QueryMCUserApproverIDs"] = string.Join(",", userApproverIDs);
                    foreach (string approverID in userApproverIDs)
                    {
                        if (approverID != "" && approverID == (HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value))
                        {
                            TempData["ApprovedStatus"] = mstHRPVGClaim.ApprovalStatus;
                            //Session["ApprovedStatus"] = oSupplierPO.Approvalstatus;
                            TempData["HODApproverIDs"] = string.Join(",", userApproverIDs.Skip(1));
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

                if (mstHRPVGClaim.HODApprover != "" && mstHRPVGClaim.Verifier == "")
                {
                    string[] hodApproverIDs = mstHRPVGClaim.HODApprover.Split(',');
                    TempData["QueryMCHODApproverIDs"] = string.Join(",", hodApproverIDs);
                    foreach (string approverID in hodApproverIDs)
                    {
                        if (approverID != "" && approverID == (HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value))
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


                int UserId = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
                ViewBag.userID = UserId;
                //var Userlist = objERPEntities.MstUsers.ToList().Where(i => i.UserID != UserId);
                var UserIds = new List<string>();
                //var Userlist1 = _context.users.ToList().Where(i => i.UserID != UserId);
                var Userlist = await _repository.MstUser.GetAllMCUsersForQueryAsync(UserId, UserIds);
                var Creater = TempData["CreatedBy"];
                var Verifiers = TempData["QueryMCVerifierIDs"];
                var Approvers = TempData["QueryMCApproverIDs"];
                var UserApprovers = TempData["QueryMCUserApproverIDs"];
                var HODApprovers = TempData["QueryMCHODApproverIDs"];

                string[] CreaterId = Creater.ToString().Split(',');
                string[] VerifiersId = Verifiers.ToString().Split(',');
                string[] ApproversId = Approvers.ToString().Split(',');
                string[] UserApproversId = UserApprovers.ToString().Split(',');
                string[] HODApproversId = HODApprovers.ToString().Split(',');

                UserIds.AddRange(CreaterId);
                UserIds.AddRange(UserApproversId);
                UserIds.AddRange(VerifiersId);
                UserIds.AddRange(HODApproversId);
                UserIds.AddRange(ApproversId);
                // Audit users
                //var AuditIDs = objERPEntities.MstSupplierPOAudits.ToList().Where(p => p.SPOID == SPOID).Select(p => p.AuditBy.ToString()).Distinct();
                //var AuditIDs1 = _context.MstMileageClaimAudit.ToList().Where(m => m.MCID == MCID).Select(m => m.AuditBy.ToString()).Distinct();
                //var AuditIDs = _repository.MstMileageClaimAudit.GetMstMileageClaimAuditByIdAsync(MCID).GetAwaiter().GetResult().Select(m => m.AuditBy.ToString()).Distinct();
                var mstPVGClaimAudits = await _repository.MstHRPVGClaimAudit.GetMstHRPVGClaimAuditByIdAsync(HRPVGCID);
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


                hRPVGClaimDetailVM.HRPVGClaimVM = HRPVGClaimVM;
                //mileageClaimDetailVM.DtMileageClaimVMs = dtMileageClaimVMs;



                return View(hRPVGClaimDetailVM);
            }
            else
            {
                return Redirect("~/Login/Login");
            }
        }

        public async Task<string> GetBankSwiftBIC(long bankCode)
        {
            var mstBankSwiftBIC = await _repository.MstBankSwiftBIC.GetBankSwiftBICByBankCodeAsync(bankCode);
            if (mstBankSwiftBIC != null)
                return mstBankSwiftBIC.BankSwiftBIC;
            else
                return string.Empty;
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
                    //dtHRPVGClaimVM.Facility = item.Facility;
                    dtHRPVGClaimVM.AccountCode = item.AccountCode;
                    dtHRPVGClaimVM.Date = item.Date;
                    dtHRPVGClaimVM.Bank = item.Bank;
                    dtHRPVGClaimVM.BankCode = item.BankCode;
                    dtHRPVGClaimVM.BranchCode = item.BranchCode;
                    dtHRPVGClaimVM.BankAccount = item.BankAccount;
                    dtHRPVGClaimVM.Mobile = item.Mobile;

                    if (item.FacilityID != null)
                    {
                        var mstFacility = await _repository.MstFacility.GetFacilityByIdAsync(item.FacilityID);
                        dtHRPVGClaimVM.Facility = mstFacility.FacilityName;
                    }

                    //dtHRPVGClaimVM.FacilityID = item.FacilityID;

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
                hRPVGClaimVM.VoucherNo = mstHRPVGClaim.VoucherNo;
                hRPVGClaimVM.ChequeNo = mstHRPVGClaim.ChequeNo;
                hRPVGClaimVM.ParticularsOfPayment = mstHRPVGClaim.ParticularsOfPayment;
                hRPVGClaimVM.Amount = mstHRPVGClaim.Amount;
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
                ViewBag.PVCCID = id;
                hRPVGClaimDetailVM.HRPVGClaimVM = hRPVGClaimVM;
                //mileageClaimDetailVM.DtMileageClaimVMs = dtMileageClaimVMs;
            }
            return PartialView("GetHRPVGDetailsPrint", hRPVGClaimDetailVM);
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

                int loggedInUserId = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
                bool isAlternateApprover = false;
                var delegatedUserId = await _alternateApproverHelper.IsUserHasAnyAlternateApprovalSet(loggedInUserId);
                if (delegatedUserId.HasValue)
                {
                    isAlternateApprover = true;
                }

                if (Convert.ToInt32(approvedStatus) == 3 || Convert.ToInt32(approvedStatus) == 9 || Convert.ToInt32(approvedStatus) == 10)
                {
                    await _repository.MstHRPVGClaim.UpdateMstHRPVGClaimStatus(HRPVGCID, -5, int.Parse(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value), DateTime.Now, reason, string.Empty, string.Empty, string.Empty, string.Empty, isAlternateApprover,0);
                }
                else
                {
                    await _repository.MstHRPVGClaim.UpdateMstHRPVGClaimStatus(HRPVGCID, 5, int.Parse(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value), DateTime.Now, reason, string.Empty, string.Empty, string.Empty, string.Empty, isAlternateApprover,0);
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
                bool excute = _repository.MstPVCClaim.ExistsApproval(HRPVGCID.ToString(), ApprovedStatus, HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value, "HRPVG");

                // If execute is false, Check if the current user is alternate user for this claim
                if (excute == false)
                {
                    string hodapprover = _repository.MstExpenseClaim.GetApproval(HRPVGCID.ToString(), ApprovedStatus, HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value, "Expense");
                    int loggedInUserId = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
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
                        await _repository.MstHRPVGClaim.UpdateMstHRPVGClaimStatus(HRPVGCID, 2, int.Parse(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value), DateTime.Now, string.Empty, VerifierIDs.ToString(), ApproverIDs.ToString(), UserApproverIDs.ToString(), HODApproverID.ToString(), isAlternateApprover,0);

                    }
                    #endregion

                    #region HRPVG Approver
                    else if (ApprovedStatus == 2)
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
                        await _repository.MstHRPVGClaim.UpdateMstHRPVGClaimStatus(HRPVGCID, 3, int.Parse(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value), DateTime.Now, string.Empty, VerifierIDs, ApproverIDs, UserApproverIDs.ToString(), HODApproverID.ToString(), isAlternateApprover,int.Parse(financeStartDay));
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

                int loggedInUserId = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
                bool isAlternateApprover = false;
                var delegatedUserId = await _alternateApproverHelper.IsUserHasAnyAlternateApprovalSet(loggedInUserId);
                if (delegatedUserId.HasValue)
                {
                    isAlternateApprover = true;
                }

                await _repository.MstHRPVGClaim.UpdateMstHRPVGClaimStatus(HRPVGCID, 4, int.Parse(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value), DateTime.Now, reason, string.Empty, string.Empty, string.Empty, string.Empty, isAlternateApprover,0);

                return Json(new { res = "Done" });
            }
            else
            {
                return Json(new { res = "Done" });
            }
        }
        public FileResult ExcelDownload()
        {
            /*
            DataTable dt = new DataTable("Grid");
            dt.Columns.AddRange(new DataColumn[13] {new DataColumn("Claimid"),
                                            new DataColumn("Username"),
                                            new DataColumn("Payment Mode"),
                                            new DataColumn("Facility"),
                                            new DataColumn("Payee Name"),
                                            new DataColumn("Particulars of payment"),
                                            new DataColumn("Amount"),
                                            new DataColumn("Employee No"),
                                            new DataColumn("Mobile/UEN No"),
                                            new DataColumn("Bank Name"),
                                            new DataColumn("Bank Code"),
                                            new DataColumn("Branch Code"),
                                            new DataColumn("Bank Account")
            });
            using (XLWorkbook wb = new XLWorkbook())
            {
                wb.Worksheets.Add(dt);
                using (MemoryStream stream = new MemoryStream())
                {
                    wb.SaveAs(stream);
                    return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "HRPVGiroTemplate.xlsx");
                }
            }
            */
            string id = "HRPVGiroTemplate.xlsm";

            var file = ("~/ExcelTemplates/" + id);
            return File(file, "application/octet-stream", id);
        }

        private IHostingEnvironment _hostingEnv;
        public void HomeController(IHostingEnvironment hostingEnv)
        {
            _hostingEnv = hostingEnv;
        }


        [HttpPost]
        public async Task<IActionResult> ImportExcelFile(IFormFile FormFile, List<IFormFile> FileInput)
        {

            try
            {
                var filename = ContentDispositionHeaderValue.Parse(FormFile.ContentDisposition).FileName.Trim('"');
                var MainPath = "Uploads/";
                var filePath = Path.Combine(MainPath, FormFile.FileName);
                string ext = Path.GetExtension(filePath);
                string result = Path.GetFileNameWithoutExtension(filePath);
                string pathToFiles = Regex.Replace(result, @"[^0-9a-zA-Z]+", "_") + DateTime.Now.ToString("ddMMyyyyss") + ext;
                filePath = Path.Combine(MainPath, pathToFiles);
                if (CloudStorageAccount.TryParse(_configuration.GetSection("ConnectionStrings")["BlobConnectionString"], out CloudStorageAccount storageAccount))
                {
                    CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

                    CloudBlobContainer container = blobClient.GetContainerReference(_configuration.GetSection("ConnectionStrings")["BlobContainerName"]);

                    CloudBlockBlob blockBlob = container.GetBlockBlobReference(filePath);

                    await blockBlob.UploadFromStreamAsync(FormFile.OpenReadStream());

                }
                string conString = string.Empty;
                MemoryStream ms = new MemoryStream();
                CloudBlobClient BlobClient = storageAccount.CreateCloudBlobClient();
                CloudBlobContainer containerRead = BlobClient.GetContainerReference(_configuration.GetSection("ConnectionStrings")["BlobContainerName"]);
                ExcelPackage package = null;
                if (await containerRead.ExistsAsync())
                {
                    CloudBlob file = containerRead.GetBlobReference(filePath);

                    if (await file.ExistsAsync())
                    {
                        await file.DownloadToStreamAsync(ms);
                        Stream blobStream = file.OpenReadAsync().Result;
                        package = new ExcelPackage(blobStream);
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

                //create a new Excel package in a memorystream
                DataTable dt = new DataTable();
                dt = ExcelPackageToDataTable(package);

                DataRow[] drows = dt.Select();

                for (int i = 0; i < drows.Length; i++)
                {
                    dt.Rows[i]["UserName"] = User.FindFirstValue("username");
                    dt.Rows[i]["Userid"] = HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value;
                    dt.Rows[i]["FacilityID"] = HttpContext.User.FindFirst("delegatefacilityid") is null ? HttpContext.User.FindFirst("facilityid").Value : HttpContext.User.FindFirst("delegatefacilityid").Value;
                    dt.Rows[i]["Status"] = "Load";
                    dt.Rows[i].EndEdit();
                    dt.AcceptChanges();
                }

                SqlCommand cmd = new SqlCommand();
                using (SqlConnection con = new SqlConnection(_context.Connection.ConnectionString))
                {
                    using (SqlBulkCopy sqlBulkCopy = new SqlBulkCopy(con))
                    {

                        //cmd = new SqlCommand("delete from MstHRPVGClaimtemp", con);
                        con.Open();
                        //cmd.ExecuteNonQuery();

                        sqlBulkCopy.DestinationTableName = "dbo.MstHRPVGClaimtemp";

                        sqlBulkCopy.ColumnMappings.Add("UserName", "UserName");
                        sqlBulkCopy.ColumnMappings.Add("Payment Mode", "PaymentMode");
                        //sqlBulkCopy.ColumnMappings.Add("EmailAddress", "EmailAddress");
                        //sqlBulkCopy.ColumnMappings.Add("Company", "Company");
                        //sqlBulkCopy.ColumnMappings.Add("Department", "Department");
                        //sqlBulkCopy.ColumnMappings.Add("Facility", "Facility");
                        //sqlBulkCopy.ColumnMappings.Add("DateofCreated", "DateofCreated");       
                        //sqlBulkCopy.ColumnMappings.Add("Date", "DateofJourney");
                        sqlBulkCopy.ColumnMappings.Add("Particulars Of Payment", "ParticularsOfPayment");
                        sqlBulkCopy.ColumnMappings.Add("Amount", "Amount");
                        sqlBulkCopy.ColumnMappings.Add("Payee Name", "StaffName");
                        sqlBulkCopy.ColumnMappings.Add("Employee No", "EmployeeNo");
                        sqlBulkCopy.ColumnMappings.Add("Facility", "FacilityName");
                        sqlBulkCopy.ColumnMappings.Add("Claimid", "Claimid");
                        sqlBulkCopy.ColumnMappings.Add("Mobile/UEN No", "MobileNo");
                        sqlBulkCopy.ColumnMappings.Add("Bank Name", "BankName");
                        sqlBulkCopy.ColumnMappings.Add("Bank Code", "BankCode");
                        sqlBulkCopy.ColumnMappings.Add("Branch Code", "BranchCode");
                        sqlBulkCopy.ColumnMappings.Add("Bank Account", "BankAccount");
                        sqlBulkCopy.ColumnMappings.Add("Userid", "Userid");
                        sqlBulkCopy.ColumnMappings.Add("Facilityid", "FacilityID");
                        sqlBulkCopy.ColumnMappings.Add("Status", "Status");
                        sqlBulkCopy.WriteToServer(dt);
                    }
                }

                DataTable InvaildData = _repository.MstHRPVGClaim.InsertExcel(Convert.ToInt32((HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value)), Convert.ToInt32(HttpContext.User.FindFirst("userid").Value));

                int count = 0;
                long cid;

                if (InvaildData.Rows.Count > 0)
                {

                    count = int.Parse(InvaildData.Rows[0]["Invalid"].ToString());
                    cid = int.Parse(InvaildData.Rows[0]["CID"].ToString());

                    if (cid != 0)
                    {
                        if (FileInput.Count >= 1)
                        {
                            TempData["CID"] = cid;
                            var fileResult = await UploadECFiles(FileInput);
                        }
                        var mstHRPVGClaim = await _repository.MstHRPVGClaim.GetHRPVGClaimByIdAsync(cid);
                        if (mstHRPVGClaim.ApprovalStatus == 6)
                        {
                            string VerifierIDs = "";
                            string ApproverIDs = "";
                            string UserApproverIDs = "";
                            string HODApproverID = "";
                            try
                            {
                                string[] userApproverIDs = mstHRPVGClaim.UserApprovers.ToString().Split(',');
                                foreach (string userApproverID in userApproverIDs)
                                {
                                    if (userApproverID != "")
                                    {
                                        string domainUrl = HttpContext.Request.Scheme + "://" + HttpContext.Request.Host;
                                        string clickUrl = domainUrl + "/" + "HRSummary/HRPVGCDetails/" + mstHRPVGClaim.HRPVGCID;

                                        var mstSenderDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value));
                                        var senderName = mstSenderDetails.Name;
                                        var mstVerifierDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(userApproverID));
                                        var toEmail = mstVerifierDetails.EmailAddress;
                                        var receiverName = mstVerifierDetails.Name;
                                        var claimNo = mstHRPVGClaim.HRPVGCNo;
                                        var screen = "HR PV-GIRO Claim";
                                        var approvalType = "Approval Request";
                                        int userID = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
                                        var subject = "HR PV-GIRO Claim for Approval " + claimNo;

                                        BackgroundJob.Enqueue(() => _sendMailServices.SendEmail("EmailTemplate.html",screen, subject, senderName, receiverName, claimNo, approvalType, userID, toEmail, clickUrl));
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

                            //VerifierIDs = mstPVGClaim.Verifier.Split(',');
                            //VerifierIDs = string.Join(",", ExpenseverifierIDs.Skip(1));
                            string[] verifierIDs = mstHRPVGClaim.Verifier.Split(',');
                            //ApproverIDs = mstPVGClaim.Approver;
                            //HODApproverID = mstPVGClaim.HODApprover;



                            //BackgroundJob.Enqueue(() => _sendMailServices.SendEmail());
                            //Mail Code Implementation for Verifiers

                            foreach (string verifierID in verifierIDs)
                            {
                                if (verifierID != "")
                                {
                                    string domainUrl = HttpContext.Request.Scheme + "://" + HttpContext.Request.Host;
                                    string clickUrl = domainUrl + "/" + "FinanceHRPVGClaim/Details/" + mstHRPVGClaim.HRPVGCID;

                                    var mstSenderDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value));
                                    var senderName = mstSenderDetails.Name;
                                    var mstVerifierDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(verifierID));
                                    var toEmail = mstVerifierDetails.EmailAddress;
                                    var receiverName = mstVerifierDetails.Name;
                                    var claimNo = mstHRPVGClaim.HRPVGCNo;
                                    var screen = "HR PV-GIRO Claim";
                                    var approvalType = "Verification Request";
                                    int userID = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
                                    var subject = "HR PV-GIRO Claim for Verification " + claimNo;

                                    BackgroundJob.Enqueue(() => _sendMailServices.SendEmail("EmailTemplate.html",screen, subject, senderName, receiverName, claimNo, approvalType, userID, toEmail, clickUrl));
                                }
                                break;
                            }
                        }
                    }
                    if (count == 0)
                    {
                        Content("<script language='javascript' type='text/javascript'>alert('File has imported.Please check the downloaded file.');</script>");
                        return RedirectToAction("Index", "HRPVGiroClaim", "File has imported.Please check the downloaded file.");

                    }
                    else
                    {
                        using (XLWorkbook wb = new XLWorkbook())
                        {
                            wb.Worksheets.Add(InvaildData);
                            using (MemoryStream stream = new MemoryStream())
                            {
                                wb.SaveAs(stream);
                                Content("<script language='javascript' type='text/javascript'>alert('File has imported.Please check the downloaded file.');</script>");
                                _toastNotification.AddSuccessToastMessage($"Import process completed. Please check the downloaded file to verify if the data has been successfully imported");
                                return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "HRPVGiroTemplateValidate.xlsx");


                            }
                        }

                    }


                }





            }

            catch (Exception ex)
            {
                _logger.LogError($"Something went wrong while uploading file: {ex.Message}");
                _toastNotification.AddErrorToastMessage($"Failed while uploading file. Error: {ex.Message}");
                return RedirectToAction("Index");
            }


            return RedirectToAction("Index", "HRPVGClaim");

        }

        public static DataTable ExcelPackageToDataTable(ExcelPackage excelPackage)
        {
            DataTable dt = new DataTable();
            var currentSheet = excelPackage.Workbook.Worksheets;
            ExcelWorksheet worksheet = currentSheet.First();

            //check if the worksheet is completely empty
            if (worksheet.Dimension == null)
            {
                return dt;
            }

            //create a list to hold the column names
            List<string> columnNames = new List<string>();

            //needed to keep track of empty column headers
            int currentColumn = 1;

            //loop all columns in the sheet and add them to the datatable
            foreach (var cell in worksheet.Cells[1, 1, 1, worksheet.Dimension.End.Column])
            {
                string columnName = cell.Text.Trim();

                //check if the previous header was empty and add it if it was
                if (cell.Start.Column != currentColumn)
                {
                    columnNames.Add("Header_" + currentColumn);
                    dt.Columns.Add("Header_" + currentColumn);
                    currentColumn++;
                }

                //add the column name to the list to count the duplicates
                columnNames.Add(columnName);

                //count the duplicate column names and make them unique to avoid the exception
                //A column named 'Name' already belongs to this DataTable
                int occurrences = columnNames.Count(x => x.Equals(columnName));
                if (occurrences > 1)
                {
                    columnName = columnName + "_" + occurrences;
                }

                //add the column to the datatable
                dt.Columns.Add(columnName);

                currentColumn++;
            }
            dt.Columns.Add("UserName");
            dt.Columns.Add("Userid");
            dt.Columns.Add("Status");
            dt.Columns.Add("FacilityID");

            //start adding the contents of the excel file to the datatable
            for (int i = 2; i <= worksheet.Dimension.End.Row; i++)
            {
                var row = worksheet.Cells[i, 1, i, worksheet.Dimension.End.Column];
                DataRow newRow = dt.NewRow();

                //loop all cells in the row
                foreach (var cell in row)
                {
                    if (cell.Address.Contains("F"))
                    {
                        newRow[cell.Start.Column - 1] = Decimal.Parse(string.IsNullOrEmpty(cell.Text) ? "0" : cell.Text, NumberStyles.Currency);
                    }
                    else
                    {
                        newRow[cell.Start.Column - 1] = cell.Text;
                    }
                }

                dt.Rows.Add(newRow);
            }

            return dt;
        }



        [HttpPost]
        public async Task<JsonResult> SaveItems(string data)
        {
            //var hRPVGClaimViewModel = JsonConvert.DeserializeObject<HRPVGClaimViewModel>(data,
            //    new IsoDateTimeConverter { DateTimeFormat = "dd/MM/yyyy" });

            var hRPVGClaimViewModel = JsonConvert.DeserializeObject<HRPVGClaimViewModel>(data);

            string claimsCondition = Request.Form["claimAddCondition"];

            var mstFacility = await _repository.MstFacility.GetFacilityWithDepartmentByIdAsync(Convert.ToInt32(HttpContext.User.FindFirst("delegatefacilityid") is null ? HttpContext.User.FindFirst("facilityid").Value : HttpContext.User.FindFirst("delegatefacilityid").Value));



            MstHRPVGClaim mstHRPVGClaim = new MstHRPVGClaim();
            mstHRPVGClaim.HRPVGCNo = hRPVGClaimViewModel.HRPVGCNo;
            mstHRPVGClaim.UserID = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
            mstHRPVGClaim.Verifier = "";
            mstHRPVGClaim.Approver = "";
            mstHRPVGClaim.FinalApprover = "";
            mstHRPVGClaim.ApprovalStatus = 1;
            mstHRPVGClaim.Amount = hRPVGClaimViewModel.Amount;
            mstHRPVGClaim.VoucherNo = hRPVGClaimViewModel.VoucherNo;
            mstHRPVGClaim.ParticularsOfPayment = hRPVGClaimViewModel.ParticularsOfPayment;
            mstHRPVGClaim.ChequeNo = hRPVGClaimViewModel.ChequeNo;
            mstHRPVGClaim.GrandTotal = hRPVGClaimViewModel.GrandTotal;
            mstHRPVGClaim.TotalAmount = hRPVGClaimViewModel.TotalAmount;
            //mstHRPVGClaim.Company = hRPVGClaimViewModel.Company;
            mstHRPVGClaim.PaymentMode = hRPVGClaimViewModel.PaymentMode;
            mstHRPVGClaim.FacilityID = Convert.ToInt32(HttpContext.User.FindFirst("delegatefacilityid") is null ? HttpContext.User.FindFirst("facilityid").Value : HttpContext.User.FindFirst("delegatefacilityid").Value);
            mstHRPVGClaim.DepartmentID = mstFacility.MstDepartment.DepartmentID;
            mstHRPVGClaim.CreatedDate = DateTime.Now;
            mstHRPVGClaim.ModifiedDate = DateTime.Now;
            mstHRPVGClaim.CreatedBy = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value); // Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
            mstHRPVGClaim.ModifiedBy = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value); // Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
            mstHRPVGClaim.ApprovalDate = DateTime.Now;
            mstHRPVGClaim.ApprovalBy = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
            mstHRPVGClaim.DelegatedBy = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? 0 : HttpContext.User.FindFirst("delegateuserid").Value);
            mstHRPVGClaim.TnC = true;


            foreach (var dtItem in hRPVGClaimViewModel.dtClaims)
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

                var mstExpenseCategory = await _repository.MstExpenseCategory.ExpenseCategoriesByClaimType("HR PV-Giro");
                if (hRPVGClaimViewModel.PaymentMode != "PayNow")
                {
                    var mstBankSwiftBIC = await _repository.MstBankSwiftBIC.GetBankSwiftBICByBankCodeAsync(Convert.ToInt64(dtItem.BankCode));
                    dtItem.Bank = mstBankSwiftBIC.BankName;
                }
                //var mstExpenseCategory = await _repository.MstExpenseCategory.GetExpenseCategoryWithTypesByIdAsync(dtItem.ExpenseCategoryID);

                dtItem.AccountCode = mstExpenseCategory.ExpenseCode;
            }

            string ClaimStatus = "";
            long HRPVGCID = 0;
            try
            {
                //CBRID = Convert.ToInt32(Session["CBRID"].ToString());
                HRPVGCID = Convert.ToInt64(hRPVGClaimViewModel.HRPVGCID);
                if (HRPVGCID == 0 || TempData["Updatestatus"].ToString() == "Recreate")
                {
                    ClaimStatus = "Recreate";
                    HRPVGCID = 0;
                }
                else if (HRPVGCID == 0)
                    ClaimStatus = "Add";
                else
                    ClaimStatus = "Update";
                mstHRPVGClaim.HRPVGCID = HRPVGCID;
                if (hRPVGClaimViewModel.ClaimAddCondition == "claimDraft")
                {
                    mstHRPVGClaim.HRPVGCID = 0;
                }
                else
                {
                    mstHRPVGClaim.HRPVGCID = HRPVGCID;
                }
                //mstHRPVGClaim.HRPVGCNo = hPVVCClaimViewModel.;
            }
            catch { }

            HRPVGClaimDetailVM hRPVGClaimDetailVM = new HRPVGClaimDetailVM();
            //List<DtMileageClaimVM> dtMileageClaimVMs = new List<DtMileageClaimVM>();
            hRPVGClaimDetailVM.DtHRPVGClaimVMs = new List<DtHRPVGClaimVM>();
            // List<clsDtSupplierPO> oclsDtSupplierPO = new List<clsDtSupplierPO>();
            foreach (var item in hRPVGClaimViewModel.dtClaims)
            {
                DtHRPVGClaimVM dtHRPVGClaimVM = new DtHRPVGClaimVM();
                if (HRPVGCID == 0 || TempData["Updatestatus"].ToString() == "Recreate")
                {
                    dtHRPVGClaimVM.HRPVGCItemID = 0;
                    dtHRPVGClaimVM.HRPVGCID = 0;
                }

                dtHRPVGClaimVM.StaffName = item.StaffName;
                dtHRPVGClaimVM.Reason = item.Reason;
                dtHRPVGClaimVM.EmployeeNo = item.EmployeeNo;
                dtHRPVGClaimVM.ChequeNo = item.ChequeNo;
                dtHRPVGClaimVM.Amount = item.Amount;
                dtHRPVGClaimVM.GST = item.GST;
                dtHRPVGClaimVM.AmountWithGST = item.Amount + item.GST;
                dtHRPVGClaimVM.Facility = item.Facility;
                dtHRPVGClaimVM.FacilityID = item.FacilityID;
                dtHRPVGClaimVM.AccountCode = item.AccountCode;
                dtHRPVGClaimVM.Date = item.Date;
                hRPVGClaimDetailVM.DtHRPVGClaimVMs.Add(dtHRPVGClaimVM);
            }

            var GroupByQS = hRPVGClaimDetailVM.DtHRPVGClaimVMs.GroupBy(s => s.AccountCode);

            hRPVGClaimDetailVM.DtHRPVGClaimVMSummary = new List<DtHRPVGClaimVM>();

            foreach (var group in GroupByQS)
            {
                DtHRPVGClaimVM dtHRPVGClaimVM = new DtHRPVGClaimVM();
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
                dtHRPVGClaimVM.Particulars = ExpenseDesc;
                dtHRPVGClaimVM.ExpenseCategory = ExpenseCat;
                dtHRPVGClaimVM.FacilityID = facilityID;
                dtHRPVGClaimVM.Facility = Facility;
                dtHRPVGClaimVM.AccountCode = AccountCode;
                dtHRPVGClaimVM.Amount = amount;
                //dtMileageClaimVM.Gst = gst;
                //dtTBClaimVM.AmountWithGST = sumamount;
                hRPVGClaimDetailVM.DtHRPVGClaimVMSummary.Add(dtHRPVGClaimVM);
            }
            List<DtHRPVGClaimSummary> lstHRPVGClaimSummary = new List<DtHRPVGClaimSummary>();
            foreach (var item in hRPVGClaimDetailVM.DtHRPVGClaimVMSummary)
            {
                DtHRPVGClaimSummary dtHRPVGClaimSummary1 = new DtHRPVGClaimSummary();
                dtHRPVGClaimSummary1.AccountCode = item.AccountCode;
                dtHRPVGClaimSummary1.Amount = item.Amount;
                dtHRPVGClaimSummary1.TaxClass = 4;
                dtHRPVGClaimSummary1.ExpenseCategory = item.ExpenseCategory;
                dtHRPVGClaimSummary1.FacilityID = item.FacilityID;
                dtHRPVGClaimSummary1.Facility = item.Facility;
                dtHRPVGClaimSummary1.Description = item.Particulars.ToUpper();
                lstHRPVGClaimSummary.Add(dtHRPVGClaimSummary1);
            }

            DtHRPVGClaimSummary dtHRPVGClaimSummary = new DtHRPVGClaimSummary();
            dtHRPVGClaimSummary.AccountCode = "425000";
            dtHRPVGClaimSummary.Amount = mstHRPVGClaim.TotalAmount;
            dtHRPVGClaimSummary.TaxClass = 0;
            dtHRPVGClaimSummary.ExpenseCategory = "DBS";
            dtHRPVGClaimSummary.Description = "";
            lstHRPVGClaimSummary.Add(dtHRPVGClaimSummary);


            var res = await _repository.MstHRPVGClaim.SaveItems(mstHRPVGClaim, hRPVGClaimViewModel.dtClaims, lstHRPVGClaimSummary);

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
                    mstHRPVGClaim = await _repository.MstHRPVGClaim.GetHRPVGClaimByIdAsync(res);
                    if (mstHRPVGClaim.ApprovalStatus == 6)
                    {
                        string VerifierIDs = "";
                        string ApproverIDs = "";
                        string UserApproverIDs = "";
                        string HODApproverID = "";
                        try
                        {
                            string[] userApproverIDs = mstHRPVGClaim.UserApprovers.ToString().Split(',');
                            foreach (string userApproverID in userApproverIDs)
                            {
                                if (userApproverID != "")
                                {
                                    string domainUrl = HttpContext.Request.Scheme + "://" + HttpContext.Request.Host;
                                    string clickUrl = domainUrl + "/" + "HRSummary/HRPVGCDetails/" + mstHRPVGClaim.HRPVGCID;

                                    var mstSenderDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value));
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
                                    int userID = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
                                    var subject = "HR PV-GIRO Claim for Approval " + claimNo;

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

                        //VerifierIDs = mstPVGClaim.Verifier.Split(',');
                        //VerifierIDs = string.Join(",", ExpenseverifierIDs.Skip(1));
                        string[] verifierIDs = mstHRPVGClaim.Verifier.Split(',');
                        //ApproverIDs = mstPVGClaim.Approver;
                        //HODApproverID = mstPVGClaim.HODApprover;



                        //BackgroundJob.Enqueue(() => _sendMailServices.SendEmail());
                        //Mail Code Implementation for Verifiers

                        foreach (string verifierID in verifierIDs)
                        {
                            if (verifierID != "")
                            {
                                string domainUrl = HttpContext.Request.Scheme + "://" + HttpContext.Request.Host;
                                string clickUrl = domainUrl + "/" + "FinanceHRPVGClaim/Details/" + mstHRPVGClaim.HRPVGCID;

                                var mstSenderDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value));
                                var senderName = mstSenderDetails.Name;
                                var mstVerifierDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(verifierID));
                                var toEmail = mstVerifierDetails.EmailAddress;
                                var receiverName = mstVerifierDetails.Name;
                                var claimNo = mstHRPVGClaim.HRPVGCNo;
                                var screen = "HR PV-GIRO Claim";
                                var approvalType = "Verification Request";
                                int userID = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
                                var subject = "HR PV-GIRO Claim for Verification " + claimNo;

                                BackgroundJob.Enqueue(() => _sendMailServices.SendEmail("EmailTemplate.html", screen, subject, senderName, receiverName, claimNo, approvalType, userID, toEmail, clickUrl));
                            }
                            break;
                        }
                    }
                    TempData["Message"] = "HR PV-Giro Claim added successfully";
                }
                else
                {
                    mstHRPVGClaim = await _repository.MstHRPVGClaim.GetHRPVGClaimByIdAsync(res);
                    if (mstHRPVGClaim.ApprovalStatus == 1)
                    {
                        string VerifierIDs = "";
                        string ApproverIDs = "";
                        string UserApproverIDs = "";
                        string HODApproverID = "";
                        try
                        {
                            //VerifierIDs = mstHRPVGClaim.Verifier.Split(',');
                            //VerifierIDs = string.Join(",", ExpenseverifierIDs.Skip(1));
                            string[] verifierIDs = mstHRPVGClaim.Verifier.Split(',');
                            ApproverIDs = mstHRPVGClaim.Approver;
                            HODApproverID = mstHRPVGClaim.HODApprover;



                            //BackgroundJob.Enqueue(() => _sendMailServices.SendEmail());
                            //Mail Code Implementation for Verifiers

                            foreach (string verifierID in verifierIDs)
                            {
                                if (verifierID != "")
                                {
                                    string domainUrl = HttpContext.Request.Scheme + "://" + HttpContext.Request.Host;
                                    string clickUrl = domainUrl + "/" + "FinanceHRPVGClaim/Details/" + mstHRPVGClaim.HRPVGCID;

                                    var mstSenderDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value));
                                    var senderName = mstSenderDetails.Name;
                                    var mstVerifierDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(verifierID));
                                    var toEmail = mstVerifierDetails.EmailAddress;
                                    var receiverName = mstVerifierDetails.Name;
                                    var claimNo = mstHRPVGClaim.HRPVGCNo;
                                    var screen = "HR PV-GIRO Claim";
                                    var approvalType = "Verification Request";
                                    int userID = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
                                    var subject = "HR PV-GIRO Claim for Verification " + claimNo;

                                    BackgroundJob.Enqueue(() => _sendMailServices.SendEmail("EmailTemplate.html", screen, subject, senderName, receiverName, claimNo, approvalType, userID, toEmail, clickUrl));
                                }
                                break;
                            }
                        }
                        catch
                        {
                        }
                    }
                    else if (mstHRPVGClaim.ApprovalStatus == 6)
                    {
                        string[] userApproverIDs = mstHRPVGClaim.UserApprovers.ToString().Split(',');
                        foreach (string userApproverID in userApproverIDs)
                        {
                            if (userApproverID != "")
                            {
                                string domainUrl = HttpContext.Request.Scheme + "://" + HttpContext.Request.Host;
                                string clickUrl = domainUrl + "/" + "HRSummary/HRPVGCDetails/" + mstHRPVGClaim.HRPVGCID;

                                var mstSenderDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value));
                                var senderName = mstSenderDetails.Name;
                                var mstVerifierDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(userApproverID));
                                var toEmail = mstVerifierDetails.EmailAddress;
                                var receiverName = mstVerifierDetails.Name;
                                var claimNo = mstHRPVGClaim.HRPVGCNo;
                                var screen = "HR PV-GIRO Claim";
                                var approvalType = "Approval Request";
                                int userID = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
                                var subject = "HR PV-GIRO Claim for Approval " + claimNo;

                                BackgroundJob.Enqueue(() => _sendMailServices.SendEmail("EmailTemplate.html", screen, subject, senderName, receiverName, claimNo, approvalType, userID, toEmail, clickUrl));
                            }
                            break;
                        }
                    }
                    else if (mstHRPVGClaim.ApprovalStatus == 7)
                    {
                        string[] hODApproverIDs = mstHRPVGClaim.HODApprover.ToString().Split(',');
                        foreach (string hODApproverID in hODApproverIDs)
                        {
                            if (hODApproverID != "")
                            {
                                string domainUrl = HttpContext.Request.Scheme + "://" + HttpContext.Request.Host;
                                string clickUrl = domainUrl + "/" + "HRSummary/HRPVGCDetails/" + mstHRPVGClaim.HRPVGCID;

                                var mstSenderDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value));
                                var senderName = mstSenderDetails.Name;
                                var mstVerifierDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(hODApproverID));
                                var toEmail = mstVerifierDetails.EmailAddress;
                                var receiverName = mstVerifierDetails.Name;
                                var claimNo = mstHRPVGClaim.HRPVGCNo;
                                var screen = "HR PV-GIRO Claim";
                                var approvalType = "Approval Request";
                                int userID = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
                                var subject = "HR PV-GIRO Claim for Approval " + claimNo;

                                BackgroundJob.Enqueue(() => _sendMailServices.SendEmail("EmailTemplate.html", screen, subject, senderName, receiverName, claimNo, approvalType, userID, toEmail, clickUrl));
                            }
                            break;
                        }
                    }
                    else
                    {
                        string[] ExpenseapproverIDs = mstHRPVGClaim.Approver.ToString().Split(',');
                        foreach (string approverID in ExpenseapproverIDs)
                        {
                            if (approverID != "")
                            {
                                string domainUrl = HttpContext.Request.Scheme + "://" + HttpContext.Request.Host;
                                string clickUrl = domainUrl + "/" + "FinanceHRPVGClaim/Details/" + mstHRPVGClaim.HRPVGCID;

                                var mstSenderDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value));
                                var senderName = mstSenderDetails.Name;
                                var mstVerifierDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(approverID));
                                var toEmail = mstVerifierDetails.EmailAddress;
                                var receiverName = mstVerifierDetails.Name;
                                var claimNo = mstHRPVGClaim.HRPVGCNo;
                                var screen = "HR PV-GIRO Claim";
                                var approvalType = "Approval Request";
                                int userID = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
                                var subject = "HR PV-GIRO Claim for Approval " + claimNo;

                                BackgroundJob.Enqueue(() => _sendMailServices.SendEmail("EmailTemplate.html", screen, subject, senderName, receiverName, claimNo, approvalType, userID, toEmail, clickUrl));
                            }
                            break;
                        }
                    }
                    TempData["Message"] = "HR PV-Giro Claim updated successfully";
                }

                return Json(new { res });
            }
            else
                return Json(new { res });
        }

        [HttpPost]
        public async Task<JsonResult> SaveItemsDraft(string data)
        {
            //var hRPVGClaimViewModel = JsonConvert.DeserializeObject<HRPVGClaimViewModel>(data,
            //    new IsoDateTimeConverter { DateTimeFormat = "dd/MM/yyyy" });

            var hRPVGClaimViewModel = JsonConvert.DeserializeObject<HRPVGClaimViewModel>(data);

            var mstFacility = await _repository.MstFacility.GetFacilityWithDepartmentByIdAsync(Convert.ToInt32(HttpContext.User.FindFirst("delegatefacilityid") is null ? HttpContext.User.FindFirst("facilityid").Value : HttpContext.User.FindFirst("delegatefacilityid").Value));



            MstHRPVGClaimDraft mstHRPVGClaim = new MstHRPVGClaimDraft();
            mstHRPVGClaim.HRPVGCNo = hRPVGClaimViewModel.HRPVGCNo;
            mstHRPVGClaim.UserID = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
            mstHRPVGClaim.Verifier = "";
            mstHRPVGClaim.Approver = "";
            mstHRPVGClaim.FinalApprover = "";
            mstHRPVGClaim.ApprovalStatus = 1;
            mstHRPVGClaim.Amount = hRPVGClaimViewModel.Amount;
            mstHRPVGClaim.VoucherNo = hRPVGClaimViewModel.VoucherNo;
            mstHRPVGClaim.ParticularsOfPayment = hRPVGClaimViewModel.ParticularsOfPayment;
            mstHRPVGClaim.ChequeNo = hRPVGClaimViewModel.ChequeNo;
            mstHRPVGClaim.GrandTotal = hRPVGClaimViewModel.GrandTotal;
            mstHRPVGClaim.TotalAmount = hRPVGClaimViewModel.TotalAmount;
            //mstHRPVGClaim.Company = hRPVGClaimViewModel.Company;
            mstHRPVGClaim.PaymentMode = hRPVGClaimViewModel.PaymentMode;
            mstHRPVGClaim.FacilityID = Convert.ToInt32(HttpContext.User.FindFirst("delegatefacilityid") is null ? HttpContext.User.FindFirst("facilityid").Value : HttpContext.User.FindFirst("delegatefacilityid").Value);
            mstHRPVGClaim.DepartmentID = mstFacility.MstDepartment.DepartmentID;
            mstHRPVGClaim.CreatedDate = DateTime.Now;
            mstHRPVGClaim.ModifiedDate = DateTime.Now;
            mstHRPVGClaim.CreatedBy = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
            mstHRPVGClaim.ModifiedBy = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
            mstHRPVGClaim.ApprovalDate = DateTime.Now;
            mstHRPVGClaim.ApprovalBy = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
            mstHRPVGClaim.TnC = true;
            List<DtHRPVGClaimDraft> dtHRPVGClaimsDraft = new List<DtHRPVGClaimDraft>();

            foreach (var dtItem in hRPVGClaimViewModel.dtClaims)
            {

                DtHRPVGClaimDraft dtHRPVGClaimDraft = new DtHRPVGClaimDraft();
                dtHRPVGClaimDraft.HRPVGCItemID = dtItem.HRPVGCItemID;
                dtHRPVGClaimDraft.HRPVGCID = dtItem.HRPVGCID;
                dtHRPVGClaimDraft.Date = dtItem.Date;
                var mstFacility1 = await _repository.MstFacility.GetFacilityWithDepartmentByIdAsync(Convert.ToInt32(dtItem.FacilityID));
                dtHRPVGClaimDraft.FacilityID = Convert.ToInt32(mstFacility1.FacilityID);
                dtHRPVGClaimDraft.ChequeNo = dtItem.ChequeNo;
                dtHRPVGClaimDraft.StaffName = dtItem.StaffName;
                dtHRPVGClaimDraft.Reason = dtItem.Reason;
                dtHRPVGClaimDraft.EmployeeNo = dtItem.EmployeeNo;
                dtHRPVGClaimDraft.Amount = dtItem.Amount;
                dtHRPVGClaimDraft.GST = dtItem.GST;
                dtHRPVGClaimDraft.Bank = dtItem.Bank;
                dtHRPVGClaimDraft.BankCode = dtItem.BankCode;
                dtHRPVGClaimDraft.BranchCode = dtItem.BranchCode;
                dtHRPVGClaimDraft.BankAccount = dtItem.BankAccount;
                dtHRPVGClaimDraft.Mobile = dtItem.Mobile;
                var mstExpenseCategory = await _repository.MstExpenseCategory.ExpenseCategoriesByClaimType("HR PV-Giro");
                dtItem.AccountCode = mstExpenseCategory.ExpenseCode;
                dtHRPVGClaimDraft.AccountCode = dtItem.AccountCode;
                dtHRPVGClaimDraft.BankSwiftBIC = dtItem.BankSwiftBIC;
                dtHRPVGClaimsDraft.Add(dtHRPVGClaimDraft);
                dtItem.AccountCode = mstExpenseCategory.ExpenseCode;
            }

            string ClaimStatus = "";
            long HRPVGCID = 0;
            try
            {
                //CBRID = Convert.ToInt32(Session["CBRID"].ToString());
                HRPVGCID = Convert.ToInt64(hRPVGClaimViewModel.HRPVGCID);
                if (HRPVGCID == 0 || TempData["Updatestatus"].ToString() == "Recreate")
                {
                    ClaimStatus = "Recreate";
                    HRPVGCID = 0;
                }
                else if (HRPVGCID == 0)
                    ClaimStatus = "Add";
                else
                    ClaimStatus = "Update";
                mstHRPVGClaim.HRPVGCID = HRPVGCID;
                //mstHRPVGClaim.HRPVGCNo = hPVVCClaimViewModel.;
            }
            catch { }

            HRPVGClaimDetailVM hRPVGClaimDetailVM = new HRPVGClaimDetailVM();
            //List<DtMileageClaimVM> dtMileageClaimVMs = new List<DtMileageClaimVM>();
            hRPVGClaimDetailVM.DtHRPVGClaimDraftVMs = new List<DtHRPVGClaimDraftVM>();
            // List<clsDtSupplierPO> oclsDtSupplierPO = new List<clsDtSupplierPO>();
            foreach (var item in hRPVGClaimViewModel.dtClaims)
            {
                DtHRPVGClaimDraftVM dtHRPVGClaimVM = new DtHRPVGClaimDraftVM();

                if (HRPVGCID == 0 || TempData["Updatestatus"].ToString() == "Recreate")
                {
                    dtHRPVGClaimVM.HRPVGCItemID = 0;
                    dtHRPVGClaimVM.HRPVGCID = 0;
                }
                dtHRPVGClaimVM.StaffName = item.StaffName;
                dtHRPVGClaimVM.Reason = item.Reason;
                dtHRPVGClaimVM.EmployeeNo = item.EmployeeNo;
                dtHRPVGClaimVM.ChequeNo = item.ChequeNo;
                dtHRPVGClaimVM.Amount = item.Amount;
                dtHRPVGClaimVM.GST = item.GST;
                dtHRPVGClaimVM.AmountWithGST = item.Amount + item.GST;
                dtHRPVGClaimVM.Facility = item.Facility;
                dtHRPVGClaimVM.FacilityID = item.FacilityID;
                dtHRPVGClaimVM.AccountCode = item.AccountCode;
                dtHRPVGClaimVM.Date = item.Date;
                hRPVGClaimDetailVM.DtHRPVGClaimDraftVMs.Add(dtHRPVGClaimVM);
            }

            var GroupByQS = hRPVGClaimDetailVM.DtHRPVGClaimDraftVMs.GroupBy(s => s.AccountCode);

            hRPVGClaimDetailVM.DtHRPVGClaimDraftVMSummary = new List<DtHRPVGClaimDraftVM>();

            foreach (var group in GroupByQS)
            {
                DtHRPVGClaimDraftVM dtHRPVGClaimVM = new DtHRPVGClaimDraftVM();
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
                dtHRPVGClaimVM.Particulars = ExpenseDesc;
                dtHRPVGClaimVM.ExpenseCategory = ExpenseCat;
                dtHRPVGClaimVM.FacilityID = facilityID;
                dtHRPVGClaimVM.Facility = Facility;
                dtHRPVGClaimVM.AccountCode = AccountCode;
                dtHRPVGClaimVM.Amount = amount;
                //dtMileageClaimVM.Gst = gst;
                //dtTBClaimVM.AmountWithGST = sumamount;
                hRPVGClaimDetailVM.DtHRPVGClaimDraftVMSummary.Add(dtHRPVGClaimVM);
            }
            List<DtHRPVGClaimDraftSummary> lstHRPVGClaimSummary = new List<DtHRPVGClaimDraftSummary>();
            foreach (var item in hRPVGClaimDetailVM.DtHRPVGClaimDraftVMSummary)
            {
                DtHRPVGClaimDraftSummary dtHRPVGClaimSummary1 = new DtHRPVGClaimDraftSummary();
                dtHRPVGClaimSummary1.AccountCode = item.AccountCode;
                dtHRPVGClaimSummary1.Amount = item.Amount;
                dtHRPVGClaimSummary1.TaxClass = 4;
                dtHRPVGClaimSummary1.ExpenseCategory = item.ExpenseCategory;
                dtHRPVGClaimSummary1.FacilityID = item.FacilityID;
                dtHRPVGClaimSummary1.Facility = item.Facility;
                dtHRPVGClaimSummary1.Description = item.Particulars.ToUpper();
                lstHRPVGClaimSummary.Add(dtHRPVGClaimSummary1);
            }

            DtHRPVGClaimDraftSummary dtHRPVGClaimSummary = new DtHRPVGClaimDraftSummary();
            dtHRPVGClaimSummary.AccountCode = "425000";
            dtHRPVGClaimSummary.Amount = mstHRPVGClaim.TotalAmount;
            dtHRPVGClaimSummary.TaxClass = 0;
            dtHRPVGClaimSummary.ExpenseCategory = "DBS";
            dtHRPVGClaimSummary.Description = "";
            lstHRPVGClaimSummary.Add(dtHRPVGClaimSummary);


            var res = await _repository.MstHRPVGClaimDraft.SaveItemsDraft(mstHRPVGClaim, dtHRPVGClaimsDraft, lstHRPVGClaimSummary);


            if (res != 0)
            {
                if (ClaimStatus == "Add" || ClaimStatus == "Recreate")
                    TempData["Message"] = "HR PV-GIRO Claim draft added successfully";
                else
                    TempData["Message"] = "HR PV-GIRO Claim draft updated successfully";

                return Json(new { res });
            }
            else
                return Json(new { res });
        }
        public async Task<JsonResult> UploadECFiles(List<IFormFile> files)
        {
            var path = "FileUploads/HRPVGClaimFiles/";
            //var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "FileUploads", "HRPVGClaimFiles");

            //if (!Directory.Exists(path))
            //{
            //    Directory.CreateDirectory(path);
            //}

            // var id1 = Request.Form["Id"];
            //var id = Request.Form["Id"].ToString();
            string claimsCondition = Request.Form["claimAddCondition"];
            int ecIDValue = Convert.ToInt32(Request.Form["ecIDValue"]);
            int HRPVGCID = Convert.ToInt32(Request.Form["Id"]);
            //int HRPVGCID = Convert.ToInt32(Request.Form["ecIDValue"]);
            if (HRPVGCID == 0)
            {
                if (TempData.ContainsKey("CID"))
                    HRPVGCID = Convert.ToInt32(TempData["CID"].ToString());
            }
            long idd = Convert.ToInt64(ecIDValue);
            foreach (IFormFile formFile in files)
            {
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
                    string pathToFiles = Regex.Replace(result, @"[^0-9a-zA-Z]+", "_") + "-" + HRPVGCID.ToString() + "-" + DateTime.Now.ToString("ddMMyyyyss") + ext;

                    DtHRPVGClaimFileUpload dtHRPVGClaimFileUpload = new DtHRPVGClaimFileUpload();
                    dtHRPVGClaimFileUpload.HRPVGCID = HRPVGCID;
                    dtHRPVGClaimFileUpload.FileName = fileName;
                    dtHRPVGClaimFileUpload.FilePath = pathToFiles;
                    dtHRPVGClaimFileUpload.CreatedDate = DateTime.Now;
                    dtHRPVGClaimFileUpload.ModifiedDate = DateTime.Now;
                    dtHRPVGClaimFileUpload.CreatedBy = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
                    dtHRPVGClaimFileUpload.ModifiedBy = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
                    dtHRPVGClaimFileUpload.IsDeleted = false;
                    _repository.DtHRPVGClaimFileUpload.CreateDtHRPVGClaimFileUpload(dtHRPVGClaimFileUpload);
                    await _repository.SaveAsync();

                    //await _context.dtMileageClaimFileUpload.AddAsync(dtMileageClaimFileUpload);
                    //await _context.SaveChangesAsync(default);
                    //var filename = ContentDispositionHeaderValue.Parse(formFile.ContentDisposition).FileName.Trim('"');

                    //var filePath = Path.Combine(path, formFile.FileName);
                    filePath = Path.Combine(path, pathToFiles);
                    var extension = Path.GetExtension(filePath).ToLowerInvariant();
                    if (CloudStorageAccount.TryParse(_configuration.GetSection("ConnectionStrings")["BlobConnectionString"], out CloudStorageAccount storageAccount))
                    {
                        CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

                        CloudBlobContainer container = blobClient.GetContainerReference(_configuration.GetSection("ConnectionStrings")["BlobContainerName"]);

                        CloudBlockBlob blockBlob = container.GetBlockBlobReference(filePath);
                        blockBlob.Properties.ContentType = GetMimeTypes()[extension];

                        await blockBlob.UploadFromStreamAsync(formFile.OpenReadStream());

                    }
                }


            }
            if (claimsCondition == "claimDraft")
            {
                // Delete the draft claim
                try
                {
                    var hrpvGiroDraft = await _repository.MstHRPVGClaimDraft.GetHRPVGClaimByIdAsync(HRPVGCID);
                    if (hrpvGiroDraft != null)
                    {
                        _repository.MstHRPVGClaimDraft.DeleteHRPVGClaim(hrpvGiroDraft);
                        await _repository.SaveAsync();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Something went wrong while deleting HRPV Cheque claim draft after submit. error: {ex.Message}");
                }
            }

            return Json("success");
        }

        private Dictionary<string, string> GetMimeTypes()
        {
            return new Dictionary<string, string>
            {
                {".txt", "text/plain"},
                {".pdf", "application/pdf"},
                {".doc", "application/vnd.ms-word"},
                {".docx", "application/vnd.ms-word"},
                {".png", "image/png"},
                {".jpg", "image/jpeg"},
                {".csv","text/csv" },
            };
        }

        public async Task<JsonResult> UploadECFilesDraft(List<IFormFile> files)
        {
            var path = "FileUploads/HRPVGClaimFiles/";
            
            int HRPVGCID = Convert.ToInt32(Request.Form["Id"]);
            if (HRPVGCID == 0)
            {
                if (TempData.ContainsKey("CID"))
                    HRPVGCID = Convert.ToInt32(TempData["CID"].ToString());
            }

            foreach (IFormFile formFile in files)
            {
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
                    string pathToFiles = Regex.Replace(result, @"[^0-9a-zA-Z]+", "_") + "-" + HRPVGCID.ToString() + "-" + DateTime.Now.ToString("ddMMyyyyss") + ext;

                    DtHRPVGClaimFileUploadDraft dtHRPVGClaimFileUpload = new DtHRPVGClaimFileUploadDraft();
                    dtHRPVGClaimFileUpload.HRPVGCID = HRPVGCID;
                    dtHRPVGClaimFileUpload.FileName = fileName;
                    dtHRPVGClaimFileUpload.FilePath = pathToFiles;
                    dtHRPVGClaimFileUpload.CreatedDate = DateTime.Now;
                    dtHRPVGClaimFileUpload.ModifiedDate = DateTime.Now;
                    dtHRPVGClaimFileUpload.CreatedBy = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
                    dtHRPVGClaimFileUpload.ModifiedBy = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
                    dtHRPVGClaimFileUpload.IsDeleted = false;
                    _repository.DtHRPVCGlaimFileUploadDraft.CreateDtHRPVGClaimFileUpload(dtHRPVGClaimFileUpload);
                    await _repository.SaveAsync();

                    //await _context.dtMileageClaimFileUpload.AddAsync(dtMileageClaimFileUpload);
                    //await _context.SaveChangesAsync(default);
                    //var filename = ContentDispositionHeaderValue.Parse(formFile.ContentDisposition).FileName.Trim('"');

                    //var filePath = Path.Combine(path, formFile.FileName);
                    filePath = Path.Combine(path, pathToFiles);
                    if (CloudStorageAccount.TryParse(_configuration.GetSection("ConnectionStrings")["BlobConnectionString"], out CloudStorageAccount storageAccount))
                    {
                        CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

                        CloudBlobContainer container = blobClient.GetContainerReference(_configuration.GetSection("ConnectionStrings")["BlobContainerName"]);

                        CloudBlockBlob blockBlob = container.GetBlockBlobReference(filePath);

                        await blockBlob.UploadFromStreamAsync(formFile.OpenReadStream());

                    }
                }


            }

            return Json("success");
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
                    int UserID = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
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
                        var delegatedUserName = string.Empty;
                        if (HttpContext.User.FindFirst("delegateuserid") is not null)
                        {
                            var delUserDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid").Value));
                            delegatedUserName = delUserDetails.Name;
                        }

                        auditUpdate.Description = "" + (string.IsNullOrEmpty(delegatedUserName) ? User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.GivenName).Value : delegatedUserName) + " Sent Query to " + receiver.Name + " on " + formattedDate + " " + time + " ";
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

                        //var mstSenderDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value));
                        var senderName = (string.IsNullOrEmpty(delegatedUserName) ? User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.GivenName).Value : delegatedUserName);
                        //var mstVerifierDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(approverID));
                        var toEmail = receiver.EmailAddress;
                        var receiverName = receiver.Name;
                        var claimNo = hRPVGClaim.HRPVGCNo;
                        var screen = "HR PV-GIRO Claim";
                        var approvalType = "Query";
                        int userID = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
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
                var hRPVGcid = Convert.ToInt32(id);
                int UserId = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
                ViewBag.userID = UserId;
                //var queries1 = _context.mstQuery.ToList().Where(j => j.ID == smcid && (j.SenderID == UserId || j.ReceiverID == UserId) && j.ModuleType.ToString().Trim() == "Expense Claim").OrderBy(j => j.SentTime);
                var queries = await _repository.MstQuery.GetAllClaimsQueriesAsync(UserId, hRPVGcid, "HRPVG Claim");
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
                    if (message.SenderID == Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value))
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
