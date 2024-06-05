using AutoMapper;
using ClosedXML.Excel;
using EClaimsEntities;
using EClaimsEntities.Models;
using EClaimsRepository.Contracts;
using EClaimsWeb.Helpers;
using EClaimsWeb.Models;
using Hangfire;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Configuration;
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
using Newtonsoft.Json.Converters;
using DocumentFormat.OpenXml.Office2010.Excel;
using NToastNotify;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace EClaimsWeb.Controllers
{
    [Authorize(Roles = "Admin,Finance,User,HR")]
    public class HRPVChequeClaimController : Controller
    {
        private readonly IToastNotification _toastNotification;
        private ILoggerManager _logger;
        private IRepositoryWrapper _repository;
        private IMapper _mapper;
        private IConfiguration _configuration;
        private AlternateApproverHelper _alternateApproverHelper;
        private ISendMailServices _sendMailServices;

        private readonly RepositoryContext _context;

        public HRPVChequeClaimController(IToastNotification toastNotification, ILoggerManager logger, IRepositoryWrapper repository, IMapper mapper, RepositoryContext context, IConfiguration configuration, ISendMailServices sendMailServices)
        {
            _logger = logger;
            _repository = repository;
            _mapper = mapper;
            _context = context;
            _configuration = configuration;
            _sendMailServices = sendMailServices;
            _alternateApproverHelper = new AlternateApproverHelper(logger, repository, context);
            _toastNotification = toastNotification;
        }

        //// GET: Facility
        //public async Task<IActionResult> Index()
        //{
        //    try
        //    {
        //        var mstPVCClaimsWithDetails = await _repository.MstPVCClaim.GetAllPVCClaimWithDetailsAsync();
        //        _logger.LogInfo($"Returned all PV Cheque Claims with details from database.");

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

                var mstHRPVCClaimsWithDetails = await _repository.MstHRPVCClaim.GetAllHRPVCClaimWithDetailsByFacilityIDAsync(Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value), 0,0,"","");
                //List<CustomHRPVCClaim> hRPVCClaimVMs = new List<CustomHRPVCClaim>();
                HRPVCClaimsVM hRPVCClaimVMs = new HRPVCClaimsVM();
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
                    hRPVCClaimVM.VoucherNo = mc.VoucherNo;

                    hRPVCClaimVM.AVerifier = mc.Verifier;
                    hRPVCClaimVM.AApprover = mc.Approver;
                    hRPVCClaimVM.AUserApprovers = mc.UserApprovers;
                    hRPVCClaimVM.AHODApprover = mc.HODApprover;

                    hRPVCClaimVM.DVerifier = mc.DVerifier;
                    hRPVCClaimVM.DApprover = mc.DApprover;
                    hRPVCClaimVM.DUserApprovers = mc.DUserApprovers;
                    hRPVCClaimVM.DHODApprover = mc.DHODApprover;

                    if (mc.UserApprovers != "")
                    {
                        hRPVCClaimVM.Approver = mc.UserApprovers.Split(',').First();
                    }
                    else if (mc.HODApprover != "")
                    {
                        hRPVCClaimVM.Approver = mc.HODApprover.Split(',').First();
                    }
                    else if (mc.Verifier != "")
                    {
                        hRPVCClaimVM.Approver = mc.Verifier.Split(',').First();
                        //string VerifierIDs = string.Join(",", PVCverifierIDs.Skip(1));
                    }
                    else if (mc.Approver != "")
                    {
                        hRPVCClaimVM.Approver = mc.Approver.Split(',').First();
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

                    hRPVCClaimVMs.hRPvcClaims.Add(hRPVCClaimVM);
                    _logger.LogInfo($"Returned all PV Cheque Claims with details from database.");
                }

                var mstHRPVCClaimsWithDetailsDraft = await _repository.MstHRPVCClaim.GetAllHRPVCClaimWithDraftDetailsByFacilityIDAsync(Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value), 0, 0, "", "");
                foreach (var mc in mstHRPVCClaimsWithDetailsDraft)
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
                    hRPVCClaimVM.VoucherNo = mc.VoucherNo;

                    if (mc.UserApprovers != "")
                    {
                        hRPVCClaimVM.Approver = mc.UserApprovers.Split(',').First();
                    }
                    else if (mc.HODApprover != "")
                    {
                        hRPVCClaimVM.Approver = mc.HODApprover.Split(',').First();
                    }
                    else if (mc.Verifier != "")
                    {
                        hRPVCClaimVM.Approver = mc.Verifier.Split(',').First();
                        //string VerifierIDs = string.Join(",", PVCverifierIDs.Skip(1));
                    }
                    else if (mc.Approver != "")
                    {
                        hRPVCClaimVM.Approver = mc.Approver.Split(',').First();
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

                    hRPVCClaimVMs.hRPvcClaimsDrafts.Add(hRPVCClaimVM);
                    _logger.LogInfo($"Returned all PV Cheque Claims with details from database.");
                }
                //var mstExpenseCategoriesWithTypesResult = _mapper.Map<IEnumerable<MstExpenseCategory>>(mstExpenseCategoriesWithTypes);
                return View(hRPVCClaimVMs);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Something went wrong inside GetAllHRPVCClaimWithDetailsAsync action: {ex.Message}");
                return View();
            }

        }
        public async Task<IActionResult> DeleteHRPVCClaimDraft(string id)
        {
            try
            {
                long idd = Convert.ToInt64(id);
                var pvcClaimsDraft = await _repository.MstHRPVCClaimDraft.GetHRPVCClaimByIdAsync(idd);
                _repository.MstHRPVCClaimDraft.DeleteHRPVCClaim(pvcClaimsDraft);
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
        public async Task<IActionResult> FinanceHRPVCheque()
        {
            try
            {
                var mstHRPVCClaimsWithDetails = await _repository.MstHRPVCClaim.GetAllHRPVCClaimWithDetailsAsync();
                _logger.LogInfo($"Returned all HRPV Cheque Claims with details from database.");

                //var mstExpenseCategoriesWithTypesResult = _mapper.Map<IEnumerable<MstExpenseCategory>>(mstExpenseCategoriesWithTypes);
                return View(mstHRPVCClaimsWithDetails);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Something went wrong inside GetAllHRPVCClaimWithDetailsAsync action: {ex.Message}");
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
            
            return RedirectToAction("Create", "HRPVChequeClaim", new
            {
                id = HRPVCCID,
                Updatestatus = "Edit"
            });
        }
        public async Task<IActionResult> Create(string id, string Updatestatus)
        {
            //TempData["CBRID"] = 0;
            TempData["Updatestatus"] = "Add";
            HRPVCClaimDetailVM hrpvcClaimDetailVM = new HRPVCClaimDetailVM();
            hrpvcClaimDetailVM.DtHRPVCClaimVMs = new List<DtHRPVCClaimVM>();
            hrpvcClaimDetailVM.HRPVCClaimAudits = new List<HRPVCClaimAuditVM>();

            TempData["claimaddcondition"] = "claimnew";

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
                        if (Updatestatus == "Recreate")
                        {
                            ViewBag.UpdateStatus = "Recreate";
                            dtHRPVCClaimVM.HRPVCCItemID = 0;
                        }
                        //dtHRPVCClaimVM.FacilityID = item.FacilityID;
                        hrpvcClaimDetailVM.DtHRPVCClaimVMs.Add(dtHRPVCClaimVM);
                    }

                    hrpvcClaimDetailVM.HRPVCClaimFileUploads = new List<DtHRPVCClaimFileUpload>();
                    var fileUploads = await _repository.DtHRPVCClaimFileUpload.GetDtHRPVCClaimAuditByIdAsync(idd);
                    if (Updatestatus == "Recreate" && fileUploads != null && fileUploads.Count > 0)
                    {
                        foreach (var uploaddata in fileUploads)
                        {
                            uploaddata.HRPVCCID = 0;
                            hrpvcClaimDetailVM.HRPVCClaimFileUploads.Add(uploaddata);
                        }
                    }
                    else
                        hrpvcClaimDetailVM.HRPVCClaimFileUploads = fileUploads;

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
                //int userFacilityId = mstUsersWithDetails.MstFacility.FacilityID;
                int userFacilityId = Convert.ToInt32(HttpContext.User.FindFirst("delegatefacilityid") is null ? HttpContext.User.FindFirst("facilityid").Value : HttpContext.User.FindFirst("delegatefacilityid").Value);
                var currFacility = await _repository.MstFacility.GetFacilityWithDepartmentByIdAsync(userFacilityId);
                ViewData["ExpenseCategoryID"] = new SelectList(await _repository.MstExpenseCategory.GetAllExpenseCategoriesByClaimTypesAsync("hRPVC", "active"), "ExpenseCategoryID", "Description");
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
                ViewData["UserFacilityID"] = currFacility.FacilityID;

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
        public async Task<IActionResult> CreateDraft(string id, string Updatestatus)
        {
            //TempData["CBRID"] = 0;
            TempData["Updatestatus"] = "Add";
            TempData["claimaddcondition"] = "claimDraft";
            HRPVCClaimDetailVM hrpvcClaimDetailVM = new HRPVCClaimDetailVM();
            hrpvcClaimDetailVM.DtHRPVCClaimVMs = new List<DtHRPVCClaimVM>();
            hrpvcClaimDetailVM.HRPVCClaimAudits = new List<HRPVCClaimAuditVM>();

            if (User != null && User.Identity.IsAuthenticated)
            {
                if (!string.IsNullOrEmpty(id))
                {
                    int idd = Convert.ToInt32(id);
                    ViewBag.CID = idd;
                    var dtHRPVCClaims = await _repository.DtHRPVCClaimDraft.GetDtHRPVCClaimByIdAsync(idd);

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

                    hrpvcClaimDetailVM.HRPVCClaimDraftFileUploads = new List<DtHRPVCClaimFileUploadDraft>();

                    hrpvcClaimDetailVM.HRPVCClaimFileUploads = await _repository.DtHRPVCClaimFileUpload.GetDtHRPVCClaimAuditByIdAsync(idd);

                    var mstHRPVCClaim = await _repository.MstHRPVCClaimDraft.GetHRPVCClaimByIdAsync(idd);


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

                ViewData["ExpenseCategoryID"] = new SelectList(await _repository.MstExpenseCategory.GetAllExpenseCategoriesByClaimTypesAsync("hRPVC", "active"), "ExpenseCategoryID", "Description");
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
                ViewData["UserFacilityID"] = mstUsersWithDetails.MstFacility.FacilityID;

                SelectList facilities = new SelectList(await _repository.MstFacility.GetAllFacilityAsync("active"), "FacilityID", "FacilityName");
                int userFacilityId = mstUsersWithDetails.MstFacility.FacilityID;
                var userFacility = facilities.Where(x => x.Value == userFacilityId.ToString()).FirstOrDefault();
                if (userFacility != null)
                {
                    facilities.Where(x => x.Value == userFacilityId.ToString()).FirstOrDefault().Selected = true;
                }
                ViewData["FacilityID"] = facilities;
            }
            return View("Create", hrpvcClaimDetailVM);

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
                    //dtHRPVCClaimVM.FacilityID = item.FacilityID;
                    oDtClaimsList.Add(dtHRPVCClaimVM);
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
            List<DtHRPVCClaimVM> oDtClaimsList = new List<DtHRPVCClaimVM>();

            try
            {
                var dtHRPVCClaims = await _repository.DtHRPVCClaimDraft.GetDtHRPVCClaimByIdAsync(Convert.ToInt64(id));

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
                    //dtHRPVCClaimVM.FacilityID = item.FacilityID;
                    oDtClaimsList.Add(dtHRPVCClaimVM);
                }
                return Json(new { DtClaimsList = oDtClaimsList });
            }
            catch
            {
                return Json(new { DtClaimsList = oDtClaimsList });
            }

        }
        public async Task<IActionResult> Details(long? id)
        {
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
                    dtHRPVCClaimVM.StaffName = item.StaffName;
                    dtHRPVCClaimVM.Reason = item.Reason;
                    dtHRPVCClaimVM.EmployeeNo = item.EmployeeNo;
                    dtHRPVCClaimVM.ChequeNo = item.ChequeNo;
                    dtHRPVCClaimVM.Amount = item.Amount;
                    dtHRPVCClaimVM.GST = item.GST;
                    dtHRPVCClaimVM.AmountWithGST = item.Amount + item.GST;
                    dtHRPVCClaimVM.Facility = item.Facility;
                    dtHRPVCClaimVM.AccountCode = item.AccountCode;
                    dtHRPVCClaimVM.Date = item.Date;
                    if (item.FacilityID != null)
                    {
                        var mstFacility = await _repository.MstFacility.GetFacilityByIdAsync(item.FacilityID);
                        dtHRPVCClaimVM.Facility = mstFacility.FacilityName;
                    }
                    //dtHRPVCClaimVM.FacilityID = item.FacilityID;

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

                var GroupByQS = hRPVCClaimDetailVM.DtHRPVCClaimVMs.GroupBy(s => s.AccountCode);

                hRPVCClaimDetailVM.DtHRPVCClaimSummaries = dtHRPVCSummaries;

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

                HRPVCClaimVM HRPVCClaimVM = new HRPVCClaimVM();
                HRPVCClaimVM.GrandTotal = mstHRPVCClaim.GrandTotal;
                HRPVCClaimVM.TotalAmount = mstHRPVCClaim.TotalAmount;
                HRPVCClaimVM.VoucherNo = mstHRPVCClaim.VoucherNo;
                HRPVCClaimVM.ChequeNo = mstHRPVCClaim.ChequeNo;
                HRPVCClaimVM.ParticularsOfPayment = mstHRPVCClaim.ParticularsOfPayment;
                HRPVCClaimVM.Amount = mstHRPVCClaim.Amount;
                HRPVCClaimVM.Company = "UEMS";
                HRPVCClaimVM.Name = mstHRPVCClaim.MstUser.Name;
                HRPVCClaimVM.DepartmentName = mstHRPVCClaim.MstDepartment.Department;
                HRPVCClaimVM.FacilityName = mstHRPVCClaim.MstFacility.FacilityName;
                HRPVCClaimVM.CreatedDate = Convert.ToDateTime(mstHRPVCClaim.CreatedDate).ToString("dd/MM/yyyy", System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
                HRPVCClaimVM.Verifier = mstHRPVCClaim.Verifier;
                HRPVCClaimVM.Approver = mstHRPVCClaim.Approver;
                HRPVCClaimVM.HRPVCCNo = mstHRPVCClaim.HRPVCCNo;
                ViewBag.HRPVCCID = id;
                TempData["CreatedBy"] = mstHRPVCClaim.CreatedBy;
                ViewBag.Approvalstatus = mstHRPVCClaim.ApprovalStatus;

                if (mstHRPVCClaim.Verifier == mstHRPVCClaim.DVerifier && mstHRPVCClaim.Approver == mstHRPVCClaim.DApprover && mstHRPVCClaim.UserApprovers == mstHRPVCClaim.DUserApprovers && mstHRPVCClaim.HODApprover == mstHRPVCClaim.DHODApprover)
                {
                    ViewBag.UserEditStatus = 4;
                }
                else
                {
                    ViewBag.UserEditStatus = 0;
                }

                TempData["ApprovedStatus"] = mstHRPVCClaim.ApprovalStatus;
                TempData["FinalApproverID"] = mstHRPVCClaim.FinalApprover;
                ViewBag.VoidReason = mstHRPVCClaim.VoidReason == null ? "" : mstHRPVCClaim.VoidReason;

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
                if (mstHRPVCClaim.Verifier != "")
                {
                    string[] verifierIDs = mstHRPVCClaim.Verifier.Split(',');
                    TempData["QueryMCVerifierIDs"] = string.Join(",", verifierIDs);
                    foreach (string verifierID in verifierIDs)
                    {
                        if (verifierID != "" && verifierID == (HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value) && User.IsInRole("Finance"))
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
                        if (approverID != "" && approverID == (HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value) && User.IsInRole("Finance"))
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

                if (mstHRPVCClaim.UserApprovers != "" && mstHRPVCClaim.Verifier == "")
                {
                    string[] userApproverIDs = mstHRPVCClaim.UserApprovers.Split(',');
                    TempData["QueryMCUserApproverIDs"] = string.Join(",", userApproverIDs);
                    foreach (string approverID in userApproverIDs)
                    {
                        if (approverID != "" && approverID == (HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value))
                        {
                            TempData["ApprovedStatus"] = mstHRPVCClaim.ApprovalStatus;
                            //Session["ApprovedStatus"] = oSupplierPO.Approvalstatus;
                            TempData["HODApproverIDs"] = string.Join(",", userApproverIDs.Skip(1));
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

                if (mstHRPVCClaim.HODApprover != "" && mstHRPVCClaim.Verifier == "")
                {
                    string[] hodApproverIDs = mstHRPVCClaim.HODApprover.Split(',');
                    TempData["QueryMCHODApproverIDs"] = string.Join(",", hodApproverIDs);
                    foreach (string approverID in hodApproverIDs)
                    {
                        if (approverID != "" && approverID == (HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value))
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


                hRPVCClaimDetailVM.HRPVCClaimVM = HRPVCClaimVM;
                //mileageClaimDetailVM.DtMileageClaimVMs = dtMileageClaimVMs;



                return View(hRPVCClaimDetailVM);
            }
            else
            {
                return Redirect("~/Login/Login");
            }
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
                    dtHRPVCClaimVM.StaffName = item.StaffName;
                    dtHRPVCClaimVM.Reason = item.Reason;
                    dtHRPVCClaimVM.EmployeeNo = item.EmployeeNo;
                    dtHRPVCClaimVM.ChequeNo = item.ChequeNo;
                    dtHRPVCClaimVM.Amount = item.Amount;
                    dtHRPVCClaimVM.GST = item.GST;
                    dtHRPVCClaimVM.AmountWithGST = item.Amount + item.GST;
                    dtHRPVCClaimVM.Facility = item.Facility;
                    dtHRPVCClaimVM.AccountCode = item.AccountCode;
                    if (item.FacilityID != null)
                    {
                        var mstFacility = await _repository.MstFacility.GetFacilityByIdAsync(item.FacilityID);
                        dtHRPVCClaimVM.Facility = mstFacility.FacilityName;
                    }
                    //dtHRPVCClaimVM.FacilityID = item.FacilityID;

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
                //var GroupByQS = (from std in pVCClaimDetailVM.DtExpenseClaimVMs
                //                                                           group std by std.ExpenseCategoryID);

                //pVCClaimDetailVM.DtHRPVCClaimVMs = new List<DtHRPVCClaimVM>();

                //foreach (var group in GroupByQS)
                //{
                //    DtHRPVCClaimVM dtHRPVCClaimVM = new DtHRPVCClaimVM();
                //    decimal amount = 0;
                //    decimal gst = 0;
                //    decimal sumamount = 0;
                //    string PVCDesc = string.Empty;
                //    string AccountCode = string.Empty;
                //    foreach (var dtPVC in group)
                //    {
                //        amount = amount + dtPVC.Amount;
                //        gst = gst + dtPVC.GST;
                //        sumamount = sumamount + dtPVC.AmountWithGST;
                //        PVCDesc = dtPVC.ExpenseCategory;
                //        AccountCode = dtPVC.AccountCode;
                //    }
                //    gst = gst / group.Count();
                //    dtHRPVCClaimVM.ExpenseCategory = PVCDesc;
                //    dtHRPVCClaimVM.AccountCode = AccountCode;
                //    dtHRPVCClaimVM.Amount = amount;
                //    dtHRPVCClaimVM.GST = gst;
                //    dtHRPVCClaimVM.AmountWithGST = sumamount;
                //    pVCClaimDetailVM.DtHRPVCClaimVMSummary.Add(dtHRPVCClaimVM);
                //}


                //var GroupByQS = (from std in expenseClaimDetailVM.DtExpenseClaimVMs
                //                                                           group std by std.ExpenseCategoryID);

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
                hRPVCClaimVM.VoucherNo = mstHRPVCClaim.VoucherNo;
                hRPVCClaimVM.ChequeNo = mstHRPVCClaim.ChequeNo;
                hRPVCClaimVM.ParticularsOfPayment = mstHRPVCClaim.ParticularsOfPayment;
                hRPVCClaimVM.Amount = mstHRPVCClaim.Amount;
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
                ViewBag.PVCCID = id;
                hRPVCClaimDetailVM.HRPVCClaimVM = hRPVCClaimVM;
                //mileageClaimDetailVM.DtMileageClaimVMs = dtMileageClaimVMs;
            }
            return PartialView("GetHRPVCDetailsPrint", hRPVCClaimDetailVM);
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

                int loggedInUserId = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
                bool isAlternateApprover = false;
                var delegatedUserId = await _alternateApproverHelper.IsUserHasAnyAlternateApprovalSet(loggedInUserId);
                if (delegatedUserId.HasValue)
                {
                    isAlternateApprover = true;
                }

                string financeStartDay = _configuration.GetValue<string>("FinanceStartDay");
                if (Convert.ToInt32(approvedStatus) == 3 || Convert.ToInt32(approvedStatus) == 9 || Convert.ToInt32(approvedStatus) == 10)
                {
                    await _repository.MstHRPVCClaim.UpdateMstHRPVCClaimStatus(HRPVCCID, -5, int.Parse(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value), DateTime.Now, reason, string.Empty, string.Empty, string.Empty, string.Empty, isAlternateApprover, int.Parse(financeStartDay));
                }
                else
                {
                    await _repository.MstHRPVCClaim.UpdateMstHRPVCClaimStatus(HRPVCCID, 5, int.Parse(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value), DateTime.Now, reason, string.Empty, string.Empty, string.Empty, string.Empty, isAlternateApprover, int.Parse(financeStartDay));
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
                int HRPVCCID = Convert.ToInt32(id);

                var mstHRPVCClaim = await _repository.MstHRPVCClaim.GetHRPVCClaimByIdAsync(HRPVCCID);

                if (mstHRPVCClaim == null)
                {
                    // return NotFound();
                }

                bool isAlternateApprover = false;
                int ApprovedStatus = Convert.ToInt32(mstHRPVCClaim.ApprovalStatus);
                bool excute = _repository.MstPVCClaim.ExistsApproval(HRPVCCID.ToString(), ApprovedStatus, HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value, "HRPVC");
                
                // If execute is false, Check if the current user is alternate user for this claim
                if (excute == false)
                {
                    string hodapprover = _repository.MstExpenseClaim.GetApproval(HRPVCCID.ToString(), ApprovedStatus, HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value, "Expense");
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
                        await _repository.MstHRPVCClaim.UpdateMstHRPVCClaimStatus(HRPVCCID, 2, int.Parse(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value), DateTime.Now, string.Empty, VerifierIDs.ToString(), ApproverIDs.ToString(), UserApproverIDs.ToString(), HODApproverID.ToString(), isAlternateApprover,0);

                    }
                    #endregion

                    #region HRPVC Approver
                    else if (ApprovedStatus == 2)
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

                        await _repository.MstHRPVCClaim.UpdateMstHRPVCClaimStatus(HRPVCCID, 3, int.Parse(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value), DateTime.Now, string.Empty, VerifierIDs, ApproverIDs, UserApproverIDs.ToString(), HODApproverID.ToString(), isAlternateApprover, int.Parse(financeStartDay));
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

                int loggedInUserId = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
                bool isAlternateApprover = false;
                var delegatedUserId = await _alternateApproverHelper.IsUserHasAnyAlternateApprovalSet(loggedInUserId);
                if (delegatedUserId.HasValue)
                {
                    isAlternateApprover = true;
                }

                await _repository.MstHRPVCClaim.UpdateMstHRPVCClaimStatus(HRPVCCID, 4, int.Parse(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value), DateTime.Now, reason, string.Empty, string.Empty, string.Empty, string.Empty, isAlternateApprover,0);

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
            dt.Columns.AddRange(new DataColumn[8] {new DataColumn("Claimid"),
                                            new DataColumn("Username"),
                                            new DataColumn("Facility"),
                                            new DataColumn("Payee Name"),
                                            new DataColumn("Particulars of payment"),
                                            new DataColumn("Amount"),
                                            new DataColumn("Employee No"),
                                            new DataColumn("Cheque No")
                                            });
            using (XLWorkbook wb = new XLWorkbook())
            {
                wb.Worksheets.Add(dt);
                using (MemoryStream stream = new MemoryStream())
                {
                    wb.SaveAs(stream);
                    return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "HRPVChequeTemplate.xlsx");
                }
            }
            */
            string id = "HRPVChequeTemplate.xlsm";

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

                        //cmd = new SqlCommand("delete from MstHRPVCClaimtemp", con);
                        con.Open();
                        //cmd.ExecuteNonQuery();

                        sqlBulkCopy.DestinationTableName = "dbo.MstHRPVCClaimtemp";

                        sqlBulkCopy.ColumnMappings.Add("UserName", "UserName");
                        //sqlBulkCopy.ColumnMappings.Add("EmailAddress", "EmailAddress");
                        //sqlBulkCopy.ColumnMappings.Add("Company", "Company");
                        //sqlBulkCopy.ColumnMappings.Add("Department", "Department");
                        //sqlBulkCopy.ColumnMappings.Add("Facility", "Facility");
                        //sqlBulkCopy.ColumnMappings.Add("DateofCreated", "DateofCreated");       
                        //sqlBulkCopy.ColumnMappings.Add("Date", "DateofJourney");
                        //sqlBulkCopy.ColumnMappings.Add("Voucher No", "VoucherNo");
                        sqlBulkCopy.ColumnMappings.Add("Cheque No", "ChequeNo");
                        sqlBulkCopy.ColumnMappings.Add("Particulars Of Payment", "ParticularsOfPayment");
                        sqlBulkCopy.ColumnMappings.Add("Amount", "Amount");
                        sqlBulkCopy.ColumnMappings.Add("Payee Name", "StaffName");
                        //sqlBulkCopy.ColumnMappings.Add("Reason Description", "ReasonDesc");
                        sqlBulkCopy.ColumnMappings.Add("Employee No", "EmployeeNo");
                        sqlBulkCopy.ColumnMappings.Add("Facility", "FacilityName");
                        //sqlBulkCopy.ColumnMappings.Add("Staff Amount", "DtAmount");
                        //sqlBulkCopy.ColumnMappings.Add("GST", "GST");
                        //sqlBulkCopy.ColumnMappings.Add("Staff Cheque No", "DtChequeNo");
                        sqlBulkCopy.ColumnMappings.Add("Claimid", "Claimid");
                        sqlBulkCopy.ColumnMappings.Add("Userid", "Userid");
                        sqlBulkCopy.ColumnMappings.Add("Facilityid", "FacilityID");
                        sqlBulkCopy.ColumnMappings.Add("Status", "Status");
                        sqlBulkCopy.WriteToServer(dt);
                    }
                }

                DataTable InvaildData = _repository.MstHRPVCClaim.InsertExcel(Convert.ToInt32((HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value)), Convert.ToInt32(HttpContext.User.FindFirst("userid").Value));

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
                        var mstHRPVCClaim = await _repository.MstHRPVCClaim.GetHRPVCClaimByIdAsync(cid);
                        if (mstHRPVCClaim.ApprovalStatus == 6)
                        {
                            string VerifierIDs = "";
                            string ApproverIDs = "";
                            string UserApproverIDs = "";
                            string HODApproverID = "";
                            try
                            {
                                string[] userApproverIDs = mstHRPVCClaim.UserApprovers.ToString().Split(',');
                                foreach (string userApproverID in userApproverIDs)
                                {
                                    if (userApproverID != "")
                                    {
                                        string domainUrl = HttpContext.Request.Scheme + "://" + HttpContext.Request.Host;
                                        string clickUrl = domainUrl + "/" + "HRSummary/Details/" + mstHRPVCClaim.HRPVCCID;

                                        var mstSenderDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value));
                                        var senderName = mstSenderDetails.Name;
                                        var mstVerifierDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(userApproverID));
                                        var toEmail = mstVerifierDetails.EmailAddress;
                                        var receiverName = mstVerifierDetails.Name;
                                        var claimNo = mstHRPVCClaim.HRPVCCNo;
                                        var screen = "HR PV-Cheque Claim";
                                        var approvalType = "Approval Request";
                                        int userID = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
                                        var subject = "HR PV-Cheque Claim for Approval " + claimNo;

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

                            //VerifierIDs = mstPVCClaim.Verifier.Split(',');
                            //VerifierIDs = string.Join(",", ExpenseverifierIDs.Skip(1));
                            string[] verifierIDs = mstHRPVCClaim.Verifier.Split(',');
                            //ApproverIDs = mstPVCClaim.Approver;
                            //HODApproverID = mstPVCClaim.HODApprover;



                            //BackgroundJob.Enqueue(() => _sendMailServices.SendEmail());
                            //Mail Code Implementation for Verifiers

                            foreach (string verifierID in verifierIDs)
                            {
                                if (verifierID != "")
                                {
                                    string domainUrl = HttpContext.Request.Scheme + "://" + HttpContext.Request.Host;
                                    string clickUrl = domainUrl + "/" + "FinanceHRPVCClaim/Details/" + mstHRPVCClaim.HRPVCCID;

                                    var mstSenderDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value));
                                    var senderName = mstSenderDetails.Name;
                                    var mstVerifierDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(verifierID));
                                    var toEmail = mstVerifierDetails.EmailAddress;
                                    var receiverName = mstVerifierDetails.Name;
                                    var claimNo = mstHRPVCClaim.HRPVCCNo;
                                    var screen = "HR PV-Cheque Claim";
                                    var approvalType = "Verification Request";
                                    int userID = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
                                    var subject = "HR PV-Cheque Claim for Verification " + claimNo;

                                    BackgroundJob.Enqueue(() => _sendMailServices.SendEmail("EmailTemplate.html",screen, subject, senderName, receiverName, claimNo, approvalType, userID, toEmail, clickUrl));
                                }
                                break;
                            }
                        }
                    }
                    if (count == 0)
                    {
                        Content("<script language='javascript' type='text/javascript'>alert('File has imported.Please check the downloaded file.');</script>");
                        return RedirectToAction("Index", "HRPVChequeClaim", "File has imported.Please check the downloaded file.");

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
                                return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "HRPVChequeTemplateValidate.xlsx");


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

            _toastNotification.AddSuccessToastMessage($"Import process completed. Please check the downloaded file to verify if the data has been successfully imported");
            return RedirectToAction("Index", "HRPVCClaim");

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
                    if (cell.Address.Contains("E"))
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
            //var hRPVCClaimViewModel = JsonConvert.DeserializeObject<HRPVCClaimViewModel>(data,
            //    new IsoDateTimeConverter { DateTimeFormat = "dd/MM/yyyy" });

            var hRPVCClaimViewModel = JsonConvert.DeserializeObject<HRPVCClaimViewModel>(data);

            string claimsCondition = Request.Form["claimAddCondition"];

            var mstFacility = await _repository.MstFacility.GetFacilityWithDepartmentByIdAsync(Convert.ToInt32(HttpContext.User.FindFirst("delegatefacilityid") is null ? HttpContext.User.FindFirst("facilityid").Value : HttpContext.User.FindFirst("delegatefacilityid").Value));



            MstHRPVCClaim mstHRPVCClaim = new MstHRPVCClaim();
            mstHRPVCClaim.HRPVCCNo = hRPVCClaimViewModel.HRPVCCNo;
            mstHRPVCClaim.UserID = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
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
            mstHRPVCClaim.FacilityID = Convert.ToInt32(HttpContext.User.FindFirst("delegatefacilityid") is null ? HttpContext.User.FindFirst("facilityid").Value : HttpContext.User.FindFirst("delegatefacilityid").Value);
            mstHRPVCClaim.DepartmentID = mstFacility.MstDepartment.DepartmentID;
            mstHRPVCClaim.CreatedDate = DateTime.Now;
            mstHRPVCClaim.ModifiedDate = DateTime.Now;
            mstHRPVCClaim.CreatedBy = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value); // Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
            mstHRPVCClaim.ModifiedBy = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value); // Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
            mstHRPVCClaim.ApprovalDate = DateTime.Now;
            mstHRPVCClaim.ApprovalBy = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
            mstHRPVCClaim.DelegatedBy = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? 0 : HttpContext.User.FindFirst("delegateuserid").Value);
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
                if (HRPVCCID == 0 || TempData["Updatestatus"].ToString() == "Recreate")
                {
                    ClaimStatus = "Recreate";
                    HRPVCCID = 0;
                }
                else if (HRPVCCID == 0)
                    ClaimStatus = "Add";
                else
                    ClaimStatus = "Update";
                mstHRPVCClaim.HRPVCCID = HRPVCCID;
                if (hRPVCClaimViewModel.ClaimAddCondition == "claimDraft")
                {
                    mstHRPVCClaim.HRPVCCID = 0;
                }
                else
                {
                    mstHRPVCClaim.HRPVCCID = HRPVCCID;
                }
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
                if (HRPVCCID == 0 || TempData["Updatestatus"].ToString() == "Recreate")
                {
                    dtHRPVCClaimVM.HRPVCCItemID = 0;
                    dtHRPVCClaimVM.HRPVCCID = 0;
                }

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
                if (ClaimStatus == "Add" || ClaimStatus == "Recreate")
                {
                    mstHRPVCClaim = await _repository.MstHRPVCClaim.GetHRPVCClaimByIdAsync(res);
                    if (mstHRPVCClaim.ApprovalStatus == 6)
                    {
                        string VerifierIDs = "";
                        string ApproverIDs = "";
                        string UserApproverIDs = "";
                        string HODApproverID = "";
                        try
                        {
                            string[] userApproverIDs = mstHRPVCClaim.UserApprovers.ToString().Split(',');
                            foreach (string userApproverID in userApproverIDs)
                            {
                                if (userApproverID != "")
                                {
                                    string domainUrl = HttpContext.Request.Scheme + "://" + HttpContext.Request.Host;
                                    string clickUrl = domainUrl + "/" + "HRSummary/Details/" + mstHRPVCClaim.HRPVCCID;

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
                                    var claimNo = mstHRPVCClaim.HRPVCCNo;
                                    var screen = "HR PV-Cheque Claim";
                                    var approvalType = "Approval Request";
                                    int userID = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
                                    var subject = "HR PV-Cheque Claim for Approval " + claimNo;

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

                        //VerifierIDs = mstPVCClaim.Verifier.Split(',');
                        //VerifierIDs = string.Join(",", ExpenseverifierIDs.Skip(1));
                        string[] verifierIDs = mstHRPVCClaim.Verifier.Split(',');
                        //ApproverIDs = mstPVCClaim.Approver;
                        //HODApproverID = mstPVCClaim.HODApprover;



                        //BackgroundJob.Enqueue(() => _sendMailServices.SendEmail());
                        //Mail Code Implementation for Verifiers

                        foreach (string verifierID in verifierIDs)
                        {
                            if (verifierID != "")
                            {
                                string domainUrl = HttpContext.Request.Scheme + "://" + HttpContext.Request.Host;
                                string clickUrl = domainUrl + "/" + "FinanceHRPVCClaim/Details/" + mstHRPVCClaim.HRPVCCID;

                                var mstSenderDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value));
                                var senderName = mstSenderDetails.Name;
                                var mstVerifierDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(verifierID));
                                var toEmail = mstVerifierDetails.EmailAddress;
                                var receiverName = mstVerifierDetails.Name;
                                var claimNo = mstHRPVCClaim.HRPVCCNo;
                                var screen = "HR PV-Cheque Claim";
                                var approvalType = "Verification Request";
                                int userID = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
                                var subject = "HR PV-Cheque Claim for Verification " + claimNo;

                                BackgroundJob.Enqueue(() => _sendMailServices.SendEmail("EmailTemplate.html", screen, subject, senderName, receiverName, claimNo, approvalType, userID, toEmail, clickUrl));
                            }
                            break;
                        }
                    }
                    TempData["Message"] = "HR PV-Cheque Claim added successfully";
                }
                else
                {
                    mstHRPVCClaim = await _repository.MstHRPVCClaim.GetHRPVCClaimByIdAsync(res);
                    if (mstHRPVCClaim.ApprovalStatus == 1)
                    {
                        string VerifierIDs = "";
                        string ApproverIDs = "";
                        string UserApproverIDs = "";
                        string HODApproverID = "";
                        try
                        {
                            //VerifierIDs = mstHRPVCClaim.Verifier.Split(',');
                            //VerifierIDs = string.Join(",", ExpenseverifierIDs.Skip(1));
                            string[] verifierIDs = mstHRPVCClaim.Verifier.Split(',');
                            ApproverIDs = mstHRPVCClaim.Approver;
                            HODApproverID = mstHRPVCClaim.HODApprover;



                            //BackgroundJob.Enqueue(() => _sendMailServices.SendEmail());
                            //Mail Code Implementation for Verifiers

                            foreach (string verifierID in verifierIDs)
                            {
                                if (verifierID != "")
                                {
                                    string domainUrl = HttpContext.Request.Scheme + "://" + HttpContext.Request.Host;
                                    string clickUrl = domainUrl + "/" + "FinanceHRPVCClaim/Details/" + mstHRPVCClaim.HRPVCCID;

                                    var mstSenderDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value));
                                    var senderName = mstSenderDetails.Name;
                                    var mstVerifierDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(verifierID));
                                    var toEmail = mstVerifierDetails.EmailAddress;
                                    var receiverName = mstVerifierDetails.Name;
                                    var claimNo = mstHRPVCClaim.HRPVCCNo;
                                    var screen = "HR PV-Cheque Claim";
                                    var approvalType = "Verification Request";
                                    int userID = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
                                    var subject = "HR PV-Cheque Claim for Verification " + claimNo;

                                    BackgroundJob.Enqueue(() => _sendMailServices.SendEmail("EmailTemplate.html", screen, subject, senderName, receiverName, claimNo, approvalType, userID, toEmail, clickUrl));
                                }
                                break;
                            }
                        }
                        catch
                        {
                        }
                    }
                    else if (mstHRPVCClaim.ApprovalStatus == 6)
                    {
                        string[] userApproverIDs = mstHRPVCClaim.UserApprovers.ToString().Split(',');
                        foreach (string userApproverID in userApproverIDs)
                        {
                            if (userApproverID != "")
                            {
                                string domainUrl = HttpContext.Request.Scheme + "://" + HttpContext.Request.Host;
                                string clickUrl = domainUrl + "/" + "HRSummary/Details/" + mstHRPVCClaim.HRPVCCID;

                                var mstSenderDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value));
                                var senderName = mstSenderDetails.Name;
                                var mstVerifierDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(userApproverID));
                                var toEmail = mstVerifierDetails.EmailAddress;
                                var receiverName = mstVerifierDetails.Name;
                                var claimNo = mstHRPVCClaim.HRPVCCNo;
                                var screen = "HR PV-Cheque Claim";
                                var approvalType = "Approval Request";
                                int userID = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
                                var subject = "HR PV-Cheque Claim for Approval " + claimNo;

                                BackgroundJob.Enqueue(() => _sendMailServices.SendEmail("EmailTemplate.html", screen, subject, senderName, receiverName, claimNo, approvalType, userID, toEmail, clickUrl));
                            }
                            break;
                        }
                    }
                    else if (mstHRPVCClaim.ApprovalStatus == 7)
                    {
                        string[] hODApproverIDs = mstHRPVCClaim.HODApprover.ToString().Split(',');
                        foreach (string hODApproverID in hODApproverIDs)
                        {
                            if (hODApproverID != "")
                            {
                                string domainUrl = HttpContext.Request.Scheme + "://" + HttpContext.Request.Host;
                                string clickUrl = domainUrl + "/" + "HRSummary/Details/" + mstHRPVCClaim.HRPVCCID;

                                var mstSenderDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value));
                                var senderName = mstSenderDetails.Name;
                                var mstVerifierDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(hODApproverID));
                                var toEmail = mstVerifierDetails.EmailAddress;
                                var receiverName = mstVerifierDetails.Name;
                                var claimNo = mstHRPVCClaim.HRPVCCNo;
                                var screen = "HR PV-Cheque Claim";
                                var approvalType = "Approval Request";
                                int userID = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
                                var subject = "HR PV-Cheque Claim for Approval " + claimNo;

                                BackgroundJob.Enqueue(() => _sendMailServices.SendEmail("EmailTemplate.html", screen, subject, senderName, receiverName, claimNo, approvalType, userID, toEmail, clickUrl));
                            }
                            break;
                        }
                    }
                    else
                    {
                        string[] ExpenseapproverIDs = mstHRPVCClaim.Approver.ToString().Split(',');
                        foreach (string approverID in ExpenseapproverIDs)
                        {
                            if (approverID != "")
                            {
                                string domainUrl = HttpContext.Request.Scheme + "://" + HttpContext.Request.Host;
                                string clickUrl = domainUrl + "/" + "FinanceHRPVCClaim/Details/" + mstHRPVCClaim.HRPVCCID;

                                var mstSenderDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value));
                                var senderName = mstSenderDetails.Name;
                                var mstVerifierDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(approverID));
                                var toEmail = mstVerifierDetails.EmailAddress;
                                var receiverName = mstVerifierDetails.Name;
                                var claimNo = mstHRPVCClaim.HRPVCCNo;
                                var screen = "HR PV-Cheque Claim";
                                var approvalType = "Approval Request";
                                int userID = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
                                var subject = "HR PV-Cheque Claim for Approval " + claimNo;

                                BackgroundJob.Enqueue(() => _sendMailServices.SendEmail("EmailTemplate.html", screen, subject, senderName, receiverName, claimNo, approvalType, userID, toEmail, clickUrl));
                            }
                            break;
                        }
                    }
                    TempData["Message"] = "HR PV-Cheque Claim updated successfully";
                }

                return Json(new { res });
            }
            else
                return Json(new { res });
        }

        [HttpPost]
        public async Task<JsonResult> SaveItemsDraft(string data)
        {
            var hRPVCClaimViewModel = JsonConvert.DeserializeObject<HRPVCClaimViewModel>(data);

            var mstFacility = await _repository.MstFacility.GetFacilityWithDepartmentByIdAsync(Convert.ToInt32(HttpContext.User.FindFirst("delegatefacilityid") is null ? HttpContext.User.FindFirst("facilityid").Value : HttpContext.User.FindFirst("delegatefacilityid").Value));



            MstHRPVCClaimDraft mstHRPVCClaim = new MstHRPVCClaimDraft();
            mstHRPVCClaim.HRPVCCNo = hRPVCClaimViewModel.HRPVCCNo;
            mstHRPVCClaim.UserID = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
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
            mstHRPVCClaim.FacilityID = Convert.ToInt32(HttpContext.User.FindFirst("delegatefacilityid") is null ? HttpContext.User.FindFirst("facilityid").Value : HttpContext.User.FindFirst("delegatefacilityid").Value);
            mstHRPVCClaim.DepartmentID = mstFacility.MstDepartment.DepartmentID;
            mstHRPVCClaim.CreatedDate = DateTime.Now;
            mstHRPVCClaim.ModifiedDate = DateTime.Now;
            mstHRPVCClaim.CreatedBy = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
            mstHRPVCClaim.ModifiedBy = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
            mstHRPVCClaim.ApprovalDate = DateTime.Now;
            mstHRPVCClaim.ApprovalBy = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
            mstHRPVCClaim.TnC = true;
            List<DtHRPVCClaimDraft> dtHRPVCClaimsDraft = new List<DtHRPVCClaimDraft>();
            foreach (var dtItem in hRPVCClaimViewModel.dtClaims)
            {
                DtHRPVCClaimDraft dtHRPVCClaimDraft =new DtHRPVCClaimDraft();
                dtHRPVCClaimDraft.HRPVCCItemID = dtItem.HRPVCCItemID;
                dtHRPVCClaimDraft.HRPVCCID=dtItem.HRPVCCID;
                dtHRPVCClaimDraft.Date=dtItem.Date;
                var mstFacility1 = await _repository.MstFacility.GetFacilityWithDepartmentByIdAsync(Convert.ToInt32(dtItem.FacilityID));
                dtHRPVCClaimDraft.FacilityID= Convert.ToInt32(mstFacility1.FacilityID);
                dtHRPVCClaimDraft.ChequeNo=dtItem.ChequeNo;
                dtHRPVCClaimDraft.StaffName=dtItem.StaffName;
                dtHRPVCClaimDraft.Reason=dtItem.Reason;
                dtHRPVCClaimDraft.EmployeeNo=dtItem.EmployeeNo;
                dtHRPVCClaimDraft.Amount=dtItem.Amount;
                dtHRPVCClaimDraft.GST=dtItem.GST;
             

                var mstExpenseCategory = await _repository.MstExpenseCategory.ExpenseCategoriesByClaimType("HR PV-Cheque");

                //var mstExpenseCategory = await _repository.MstExpenseCategory.GetExpenseCategoryWithTypesByIdAsync(dtItem.ExpenseCategoryID);

                dtItem.AccountCode = mstExpenseCategory.ExpenseCode;

                dtHRPVCClaimsDraft.Add(dtHRPVCClaimDraft);
            }

            string ClaimStatus = "";
            long HRPVCCID = 0;
            try
            {
                //CBRID = Convert.ToInt32(Session["CBRID"].ToString());
                HRPVCCID = Convert.ToInt64(hRPVCClaimViewModel.HRPVCCID);
                if (HRPVCCID == 0 || TempData["Updatestatus"].ToString() == "Recreate")
                {
                    ClaimStatus = "Recreate";
                    HRPVCCID = 0;
                }
                else if (HRPVCCID == 0)
                    ClaimStatus = "Add";
                else
                    ClaimStatus = "Update";
                mstHRPVCClaim.HRPVCCID = HRPVCCID;
                //mstHRPVCClaim.HRPVCCNo = hPVVCClaimViewModel.;
            }
            catch { }

            HRPVCClaimDetailVM hRPVCClaimDetailVM = new HRPVCClaimDetailVM();
            //List<DtMileageClaimVM> dtMileageClaimVMs = new List<DtMileageClaimVM>();
            hRPVCClaimDetailVM.DtHRPVCClaimDraftVMs = new List<DtHRPVCClaimVM>();
            // List<clsDtSupplierPO> oclsDtSupplierPO = new List<clsDtSupplierPO>();
            foreach (var item in hRPVCClaimViewModel.dtClaims)
            {
                DtHRPVCClaimVM dtHRPVCClaimVM = new DtHRPVCClaimVM();

                if (HRPVCCID == 0 || TempData["Updatestatus"].ToString() == "Recreate")
                {
                    dtHRPVCClaimVM.HRPVCCItemID = 0;
                    dtHRPVCClaimVM.HRPVCCID = 0;
                }
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
                hRPVCClaimDetailVM.DtHRPVCClaimDraftVMs.Add(dtHRPVCClaimVM);
            }

            var GroupByQS = hRPVCClaimDetailVM.DtHRPVCClaimDraftVMs.GroupBy(s => s.ExpenseCategoryID);

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
            List<DtHRPVCClaimSummaryDraft> lstHRPVCClaimSummary = new List<DtHRPVCClaimSummaryDraft>();
            foreach (var item in hRPVCClaimDetailVM.DtHRPVCClaimVMSummary)
            {
                DtHRPVCClaimSummaryDraft dtHRPVCClaimSummary1 = new DtHRPVCClaimSummaryDraft();
                dtHRPVCClaimSummary1.AccountCode = item.AccountCode;
                dtHRPVCClaimSummary1.Amount = item.Amount;
                dtHRPVCClaimSummary1.TaxClass = 4;
                dtHRPVCClaimSummary1.ExpenseCategory = item.ExpenseCategory;
                dtHRPVCClaimSummary1.FacilityID = item.FacilityID;
                dtHRPVCClaimSummary1.Facility = item.Facility;
                dtHRPVCClaimSummary1.Description = item.Particulars.ToUpper();
                lstHRPVCClaimSummary.Add(dtHRPVCClaimSummary1);
            }

            DtHRPVCClaimSummaryDraft dtHRPVCClaimSummary = new DtHRPVCClaimSummaryDraft();
            dtHRPVCClaimSummary.AccountCode = "425000";
            dtHRPVCClaimSummary.Amount = mstHRPVCClaim.TotalAmount;
            dtHRPVCClaimSummary.TaxClass = 0;
            dtHRPVCClaimSummary.ExpenseCategory = "DBS";
            dtHRPVCClaimSummary.Description = "";
            lstHRPVCClaimSummary.Add(dtHRPVCClaimSummary);


            var res = await _repository.MstHRPVCClaim.SaveItemsDraft(mstHRPVCClaim,dtHRPVCClaimsDraft, lstHRPVCClaimSummary);
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
                    TempData["Message"] = "PVCheque Claim draft added successfully";
                else
                    TempData["Message"] = "PVCheque Claim draft updated successfully";

                return Json(new { res });
            }
            else
                return Json(new { res });
        }
        public async Task<JsonResult> UploadECFiles(List<IFormFile> files)
        {
            var path = "FileUploads/HRPVCClaimFiles/";
            //var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "FileUploads", "HRPVCClaimFiles");

            //if (!Directory.Exists(path))
            //{
            //    Directory.CreateDirectory(path);
            //}

            // var id1 = Request.Form["Id"];
            //var id = Request.Form["Id"].ToString();
            string claimsCondition = Request.Form["claimAddCondition"];
            int ecIDValue = Convert.ToInt32(Request.Form["ecIDValue"]);
            int HRPVCCID = Convert.ToInt32(Request.Form["Id"]);
            //int HRPVCCID = Convert.ToInt32(Request.Form["ecIDValue"]);
            if (HRPVCCID == 0)
            {
                if (TempData.ContainsKey("CID"))
                    HRPVCCID = Convert.ToInt32(TempData["CID"].ToString());
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
                    string pathToFiles = Regex.Replace(result, @"[^0-9a-zA-Z]+", "_") + "-" + HRPVCCID.ToString() + "-" + DateTime.Now.ToString("ddMMyyyyss") + ext;

                    DtHRPVCClaimFileUpload dtHRPVCClaimFileUpload = new DtHRPVCClaimFileUpload();
                    dtHRPVCClaimFileUpload.HRPVCCID = HRPVCCID;
                    dtHRPVCClaimFileUpload.FileName = fileName;
                    dtHRPVCClaimFileUpload.FilePath = pathToFiles;
                    dtHRPVCClaimFileUpload.CreatedDate = DateTime.Now;
                    dtHRPVCClaimFileUpload.ModifiedDate = DateTime.Now;
                    dtHRPVCClaimFileUpload.CreatedBy = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
                    dtHRPVCClaimFileUpload.ModifiedBy = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
                    dtHRPVCClaimFileUpload.IsDeleted = false;
                    dtHRPVCClaimFileUpload.DocumentType = "2";
                    _repository.DtHRPVCClaimFileUpload.CreateDtHRPVCClaimFileUpload(dtHRPVCClaimFileUpload);
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
                    var hrpvChequeDraft = await _repository.MstHRPVCClaimDraft.GetHRPVCClaimByIdAsync(HRPVCCID);
                    if (hrpvChequeDraft != null)
                    {
                        _repository.MstHRPVCClaimDraft.DeleteHRPVCClaim(hrpvChequeDraft);
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
            var path = "FileUploads/HRPVCClaimFiles/";

            int HRPVCCID = Convert.ToInt32(Request.Form["Id"]);
            if (HRPVCCID == 0)
            {
                if (TempData.ContainsKey("CID"))
                    HRPVCCID = Convert.ToInt32(TempData["CID"].ToString());
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
                    string pathToFiles = Regex.Replace(result, @"[^0-9a-zA-Z]+", "_") + "-" + HRPVCCID.ToString() + "-" + DateTime.Now.ToString("ddMMyyyyss") + ext;

                    DtHRPVCClaimFileUploadDraft dtHRPVCClaimFileUpload = new DtHRPVCClaimFileUploadDraft();
                    dtHRPVCClaimFileUpload.HRPVCCID = HRPVCCID;
                    dtHRPVCClaimFileUpload.FileName = fileName;
                    dtHRPVCClaimFileUpload.FilePath = pathToFiles;
                    dtHRPVCClaimFileUpload.CreatedDate = DateTime.Now;
                    dtHRPVCClaimFileUpload.ModifiedDate = DateTime.Now;
                    dtHRPVCClaimFileUpload.CreatedBy = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
                    dtHRPVCClaimFileUpload.ModifiedBy = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
                    dtHRPVCClaimFileUpload.IsDeleted = false;
                    dtHRPVCClaimFileUpload.DocumentType = "2";
                    _repository.DtHRPVCClaimFileUploadDraft.CreateDtHRPVCClaimFileUpload(dtHRPVCClaimFileUpload);
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
        public async Task<JsonResult> UploadSubmitHRPVCFiles(List<IFormFile> files)
        {
            var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "FileUploads", "HRPVCClaimFiles","Submit");

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
                    dtHRPVCClaimFileUpload.CreatedBy = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
                    dtHRPVCClaimFileUpload.ModifiedBy = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
                    dtHRPVCClaimFileUpload.IsDeleted = false;
                    dtHRPVCClaimFileUpload.DocumentType = "1";
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

        public async Task<JsonResult> UploadSupportHRPVCFiles(List<IFormFile> files)
        {
            var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "FileUploads", "HRPVCClaimFiles", "Support");

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
                    dtHRPVCClaimFileUpload.CreatedBy = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
                    dtHRPVCClaimFileUpload.ModifiedBy = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
                    dtHRPVCClaimFileUpload.IsDeleted = false;
                    dtHRPVCClaimFileUpload.DocumentType = "3";
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
                    int UserID = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
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
                        var delegatedUserName = string.Empty;
                        if (HttpContext.User.FindFirst("delegateuserid") is not null)
                        {
                            var delUserDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid").Value));
                            delegatedUserName = delUserDetails.Name;
                        }

                        auditUpdate.Description = "" + (string.IsNullOrEmpty(delegatedUserName) ? User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.GivenName).Value : delegatedUserName) + " Sent Query to " + receiver.Name + " on " + formattedDate + " " + time + " ";
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

                        //var mstSenderDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value));
                        var senderName = (string.IsNullOrEmpty(delegatedUserName) ? User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.GivenName).Value : delegatedUserName);
                        //var mstVerifierDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(approverID));
                        var toEmail = receiver.EmailAddress;
                        var receiverName = receiver.Name;
                        var claimNo = hRPVCClaim.HRPVCCNo;
                        var screen = "HR PV-Cheque Claim";
                        var approvalType = "Query";
                        int userID = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
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
                var hRPVCcid = Convert.ToInt32(id);
                int UserId = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
                ViewBag.userID = UserId;
                //var queries1 = _context.mstQuery.ToList().Where(j => j.ID == smcid && (j.SenderID == UserId || j.ReceiverID == UserId) && j.ModuleType.ToString().Trim() == "Expense Claim").OrderBy(j => j.SentTime);
                var queries = await _repository.MstQuery.GetAllClaimsQueriesAsync(UserId, hRPVCcid, "HRPVC Claim");
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
