using AutoMapper;
using ClosedXML.Excel;
using EClaimsEntities;
using EClaimsEntities.Models;
using EClaimsRepository.Contracts;
using EClaimsWeb.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.OleDb;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NToastNotify;
using EClaimsWeb.Helpers;
using Microsoft.Extensions.Configuration;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using OfficeOpenXml;
using Hangfire;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Converters;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace EClaimsWeb.Controllers
{
    [Authorize(Roles = "Admin,Finance,User,HR")]
    public class MileageClaimController : Controller
    {
        private ILoggerManager _logger;
        private IRepositoryWrapper _repository;
        private IMapper _mapper;
        private readonly IToastNotification _toastNotification;
        private readonly RepositoryContext _context;
        private IConfiguration _configuration;
        private AlternateApproverHelper _alternateApproverHelper;
        private ISendMailServices _sendMailServices;
        //public IRepositoryContext _context { get; }
        //public IApplicationReadDbConnection _readDbConnection { get; }
        //public IApplicationWriteDbConnection _writeDbConnection { get; }

        public MileageClaimController(ILoggerManager logger, IRepositoryWrapper repository, IMapper mapper, RepositoryContext context, IToastNotification toastNotification, IConfiguration configuration, ISendMailServices sendMailServices)
        {
            _logger = logger;
            _repository = repository;
            _mapper = mapper;
            _context = context;
            _toastNotification = toastNotification;
            _configuration = configuration;
            _sendMailServices = sendMailServices;
            _alternateApproverHelper = new AlternateApproverHelper(logger, repository, context);
        }

        // GET: Facility
        public async Task<IActionResult> Index()
        {
            try
            {
                var approverDetails = await _repository.MstUserApprovers.GetUserApproversByUserIdAsync(Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value));
                if (approverDetails.Count() == 0)
                    ViewBag.Settings = "true";
                else
                    ViewBag.Settings = "false";

                var mstMileageClaimsWithDetails = await _repository.MstMileageClaim.GetAllMileageClaimWithDetailsByFacilityIDAsync(Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value), Convert.ToInt32(HttpContext.User.FindFirst("delegatefacilityid") is null ? HttpContext.User.FindFirst("facilityid").Value : HttpContext.User.FindFirst("delegatefacilityid").Value));
                MileageClaimsVM mileageClaimsVMs = new MileageClaimsVM();
                foreach (var mc in mstMileageClaimsWithDetails)
                {
                    MileageClaimVM mileageClaimVM = new MileageClaimVM();
                    mileageClaimVM.MCID = mc.MCID;
                    mileageClaimVM.MCNo = mc.MCNo;
                    mileageClaimVM.Name = mc.MstUser.Name;
                    mileageClaimVM.CreatedDate = Convert.ToDateTime(mc.CreatedDate).ToString("dd/MM/yyyy", System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
                    mileageClaimVM.FacilityName = mc.MstFacility.FacilityName;
                    mileageClaimVM.Phone = mc.MstUser.Phone;
                    mileageClaimVM.GrandTotal = mc.GrandTotal;
                    mileageClaimVM.ApprovalStatus = mc.ApprovalStatus;
                    mileageClaimVM.TravelMode = mc.TravelMode;
                    mileageClaimVM.VoucherNo = mc.VoucherNo;

                    mileageClaimVM.AVerifier = mc.Verifier;
                    mileageClaimVM.AApprover = mc.Approver;
                    mileageClaimVM.AUserApprovers = mc.UserApprovers;
                    mileageClaimVM.AHODApprover = mc.HODApprover;

                    mileageClaimVM.DVerifier = mc.DVerifier;
                    mileageClaimVM.DApprover = mc.DApprover;
                    mileageClaimVM.DUserApprovers = mc.DUserApprovers;
                    mileageClaimVM.DHODApprover = mc.DHODApprover;

                    if (mc.UserApprovers != "")
                    {
                        mileageClaimVM.Approver = mc.UserApprovers.Split(',').First();
                    }
                    else if (mc.Verifier != "")
                    {
                        mileageClaimVM.Approver = mc.Verifier.Split(',').First();
                        //string VerifierIDs = string.Join(",", MileageverifierIDs.Skip(1));
                    }
                    else if (mc.HODApprover != "")
                    {
                        mileageClaimVM.Approver = mc.HODApprover.Split(',').First();
                    }
                    else if (mc.Approver != "")
                    {
                        mileageClaimVM.Approver = mc.Approver.Split(',').First();
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

                    mileageClaimsVMs.mileageClaims.Add(mileageClaimVM);
                }

                var mstMileageClaimsDraftWithDetails = await _repository.MstMileageClaimDraft.GetAllMileageClaimDraftWithDetailsByFacilityIDAsync(Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value), Convert.ToInt32(HttpContext.User.FindFirst("delegatefacilityid") is null ? HttpContext.User.FindFirst("facilityid").Value : HttpContext.User.FindFirst("delegatefacilityid").Value));
                if (mstMileageClaimsDraftWithDetails != null && mstMileageClaimsDraftWithDetails.Any())
                {
                    mstMileageClaimsDraftWithDetails = mstMileageClaimsDraftWithDetails.OrderByDescending(x => x.CreatedDate).ToList();
                }
                foreach (var mc in mstMileageClaimsDraftWithDetails)
                {
                    MileageClaimVM mileageClaimDraftVM = new MileageClaimVM();
                    mileageClaimDraftVM.MCID = mc.MCID;
                    mileageClaimDraftVM.MCNo = mc.MCNo;
                    mileageClaimDraftVM.Name = mc.MstUser.Name;
                    mileageClaimDraftVM.CreatedDate = Convert.ToDateTime(mc.CreatedDate).ToString("dd/MM/yyyy", System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
                    mileageClaimDraftVM.FacilityName = mc.MstFacility.FacilityName;
                    mileageClaimDraftVM.Phone = mc.MstUser.Phone;
                    mileageClaimDraftVM.GrandTotal = mc.GrandTotal;
                    mileageClaimDraftVM.ApprovalStatus = mc.ApprovalStatus;
                    mileageClaimDraftVM.TravelMode = mc.TravelMode;

                    if (mc.UserApprovers != "")
                    {
                        mileageClaimDraftVM.Approver = mc.UserApprovers.Split(',').First();
                    }
                    else if (mc.Verifier != "")
                    {
                        mileageClaimDraftVM.Approver = mc.Verifier.Split(',').First();
                        //string VerifierIDs = string.Join(",", MileageverifierIDs.Skip(1));
                    }
                    else if (mc.HODApprover != "")
                    {
                        mileageClaimDraftVM.Approver = mc.HODApprover.Split(',').First();
                    }
                    else if (mc.Approver != "")
                    {
                        mileageClaimDraftVM.Approver = mc.Approver.Split(',').First();
                    }
                    else
                    {
                        mileageClaimDraftVM.Approver = "";
                    }

                    if (mileageClaimDraftVM.Approver != "")
                    {
                        var mstUserApprover = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(mileageClaimDraftVM.Approver));
                        mileageClaimDraftVM.Approver = mstUserApprover.Name;
                    }

                    mileageClaimsVMs.mileageClaimsDrafts.Add(mileageClaimDraftVM);
                }
                _logger.LogInfo($"Returned all Mileage Claims with details from database.");

                //var mstExpenseCategoriesWithTypesResult = _mapper.Map<IEnumerable<MstExpenseCategory>>(mstExpenseCategoriesWithTypes);
                return View(mileageClaimsVMs);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Something went wrong inside GetAllMileageClaimWithDetailsByFacilityIDAsync action: {ex.Message}");
                _toastNotification.AddErrorToastMessage($"Failed to retrieve Mileage claims. Error: {ex.Message}");
                return View();
            }
        }

        public async Task<ActionResult> DeleteMileageClaimFile(string fileID, string filepath, string MCID)
        {
            DtMileageClaimFileUpload dtMileageClaimFileUpload = new DtMileageClaimFileUpload();

            if (CloudStorageAccount.TryParse(_configuration.GetSection("ConnectionStrings")["BlobConnectionString"], out CloudStorageAccount storageAccount))
            {
                CloudBlobClient BlobClient = storageAccount.CreateCloudBlobClient();
                CloudBlobContainer container = BlobClient.GetContainerReference(_configuration.GetSection("ConnectionStrings")["BlobContainerName"]);

                if (await container.ExistsAsync())
                {
                    CloudBlob file = container.GetBlobReference("FileUploads/MileageClaimFiles/" + filepath);

                    if (await file.ExistsAsync())
                    {
                        await file.DeleteIfExistsAsync();
                        dtMileageClaimFileUpload = await _repository.DtMileageClaimFileUpload.GetDtMileageClaimFileUploadByIdAsync(Convert.ToInt64(fileID));
                        _repository.DtMileageClaimFileUpload.DeleteDtMileageClaim(dtMileageClaimFileUpload);
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

            return RedirectToAction("Create", "MileageClaim", new
            {
                id = MCID,
                Updatestatus = "Edit"
            });
        }

        public async Task<ActionResult> DeleteMileageClaimDraftFile(string fileID, string filepath, string MCID)
        {
            DtMileageClaimFileUploadDraft dtMileageClaimFileUpload = new DtMileageClaimFileUploadDraft();
            if (CloudStorageAccount.TryParse(_configuration.GetSection("ConnectionStrings")["BlobConnectionString"], out CloudStorageAccount storageAccount))
            {
                CloudBlobClient BlobClient = storageAccount.CreateCloudBlobClient();
                CloudBlobContainer container = BlobClient.GetContainerReference(_configuration.GetSection("ConnectionStrings")["BlobContainerName"]);

                if (await container.ExistsAsync())
                {
                    CloudBlob file = container.GetBlobReference("FileUploads/MileageClaimFiles/" + filepath);

                    if (await file.ExistsAsync())
                    {
                        await file.DeleteIfExistsAsync();
                        dtMileageClaimFileUpload = await _repository.DtMileageClaimFileUploadDraft.GetDtMileageClaimFileUploadDraftByIdAsync(Convert.ToInt64(fileID));
                        _repository.DtMileageClaimFileUploadDraft.DeleteDtMileageClaimDraft(dtMileageClaimFileUpload);
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
           
            return RedirectToAction("CreateDraft", "MileageClaim", new
            {
                id = MCID,
                Updatestatus = "Edit"
            });
        }


        public async Task<JsonResult> GetTextValuesSG(string id)
        {
            List<DtMileageClaimVM> oDtClaimsList = new List<DtMileageClaimVM>();

            try
            {
                var dtMileageClaims = await _repository.DtMileageClaim.GetDtMileageClaimByIdAsync(Convert.ToInt64(id));

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
                    dtMileageClaimVM.FacilityID = item.FacilityID;
                    dtMileageClaimVM.FromFacilityID = item.FromFacilityID;
                    dtMileageClaimVM.ToFacilityID = item.ToFacilityID;


                    if (item.FacilityID != null)
                    {
                        var mstFacility = await _repository.MstFacility.GetFacilityByIdAsync(item.FacilityID);
                        dtMileageClaimVM.FacilityName = mstFacility.FacilityName;
                    }
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


                    oDtClaimsList.Add(dtMileageClaimVM);
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
            List<DtMileageClaimVM> oDtClaimsList = new List<DtMileageClaimVM>();

            try
            {
                var dtMileageClaims = await _repository.DtMileageClaimDraft.GetDtMileageClaimDraftByIdAsync(Convert.ToInt64(id));

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
                    dtMileageClaimVM.FacilityID = item.FacilityID;
                    dtMileageClaimVM.FromFacilityID = item.FromFacilityID;
                    dtMileageClaimVM.ToFacilityID = item.ToFacilityID;


                    if (item.FacilityID != null)
                    {
                        var mstFacility = await _repository.MstFacility.GetFacilityByIdAsync(item.FacilityID);
                        dtMileageClaimVM.FacilityName = mstFacility.FacilityName;
                    }
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
                    oDtClaimsList.Add(dtMileageClaimVM);
                }
                return Json(new { DtClaimsList = oDtClaimsList });
            }
            catch
            {
                return Json(new { DtClaimsList = oDtClaimsList });
            }

        }

        public async Task<IActionResult> DeleteMileageDraft(string id)
        {
            try
            {
                long idd = Convert.ToInt64(id);
                var mileageClaimsDraft = await _repository.MstMileageClaimDraft.GetMileageClaimDraftByIdAsync(idd);
                _repository.MstMileageClaimDraft.DeleteMileageClaimDraft(mileageClaimsDraft);
                await _repository.SaveAsync();
                TempData["Message"] = "Draft deleted successfully";
                Content("<script language='javascript' type='text/javascript'>alert('Draft deleted successfully');</script>");
                return RedirectToAction("Index", "MileageClaim");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Something went wrong inside DeleteMileageDraft action: {ex.Message}");
            }
            return Json(null);
        }

        [HttpPost]
        public async Task<JsonResult> SaveItems(string data)
        {
            //    var dateformats = new string[] { "yyyyMMdd", "yyyy/MM/dd", "dd/MM/yyyy", "dd-MM-yyyy",
            //"yyyyMMddHHmmss", "yyyy/MM/dd HH:mm:ss", "dd/MM/yyyy HH:mm:ss", "dd-MM-yyyy HH:mm:ss" , "yyyy-mm-dd HH:mm:ss", "dd/mm/yyyy" };
            //    MileageClaimViewModel mileageClaimViewModel = JsonConvert.DeserializeObject<MileageClaimViewModel>(data, new CustomDateTimeConverter(dateformats, "dd/mm/yyyy"));
            var mileageClaimViewModel = JsonConvert.DeserializeObject<MileageClaimViewModel>(data);
            var mstFacility = await _repository.MstFacility.GetFacilityWithDepartmentByIdAsync(Convert.ToInt32(HttpContext.User.FindFirst("delegatefacilityid") is null ? HttpContext.User.FindFirst("facilityid").Value : HttpContext.User.FindFirst("delegatefacilityid").Value));

            MstMileageClaim mstMileageClaim = new MstMileageClaim();
            mstMileageClaim.MCNo = mileageClaimViewModel.MCNo;
            mstMileageClaim.UserID = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
            mstMileageClaim.TravelMode = mileageClaimViewModel.TravelMode;
            mstMileageClaim.Verifier = "";
            mstMileageClaim.Approver = "";
            mstMileageClaim.FinalApprover = "";
            mstMileageClaim.ApprovalStatus = 1;
            mstMileageClaim.GrandTotal = mileageClaimViewModel.GrandTotal;
            mstMileageClaim.TotalKm = mileageClaimViewModel.TotalKm;
            mstMileageClaim.Company = mileageClaimViewModel.Company;
            mstMileageClaim.FacilityID = Convert.ToInt32(HttpContext.User.FindFirst("delegatefacilityid") is null ? HttpContext.User.FindFirst("facilityid").Value : HttpContext.User.FindFirst("delegatefacilityid").Value);
            mstMileageClaim.DepartmentID = mstFacility.MstDepartment.DepartmentID;
            mstMileageClaim.CreatedDate = DateTime.Now;
            mstMileageClaim.ModifiedDate = DateTime.Now;
            mstMileageClaim.CreatedBy = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value); // Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
            mstMileageClaim.ModifiedBy = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value); // Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
            mstMileageClaim.ApprovalDate = DateTime.Now;
            mstMileageClaim.ApprovalBy = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
            mstMileageClaim.DelegatedBy = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? 0 : HttpContext.User.FindFirst("delegateuserid").Value);
            mstMileageClaim.TnC = true;

            List<DtMileageClaim> dtMileageClaims = new List<DtMileageClaim>();
            foreach (var dtItem in mileageClaimViewModel.dtClaims)
            {
                var mstFacility1 = await _repository.MstFacility.GetFacilityWithDepartmentByIdAsync(Convert.ToInt32(dtItem.FacilityID));

                DtMileageClaim dtMileageClaim = new DtMileageClaim();
                dtMileageClaim.MCItemID = dtItem.MCItemID;
                dtMileageClaim.Amount = dtItem.Amount;
                dtMileageClaim.DateOfJourney = dtItem.DateOfJourney;

                dtMileageClaim.DateOfJourney = dtItem.DateOfJourney;
                dtMileageClaim.FacilityID = dtItem.FacilityID;
                dtMileageClaim.FromFacilityID = dtItem.FromFacilityID;
                dtMileageClaim.ToFacilityID = dtItem.ToFacilityID;
                dtMileageClaim.InTime = DateTime.Parse(dtItem.InTimeTime);
                dtMileageClaim.OutTime = DateTime.Parse(dtItem.OutTimeTime); ;
                dtMileageClaim.StartReading = dtItem.StartReading;
                dtMileageClaim.EndReading = dtItem.EndReading;
                dtMileageClaim.Kms = dtItem.Kms;
                dtMileageClaim.Remark = dtItem.Remark;
                dtMileageClaim.Amount = dtItem.Amount;

                var mstExpenseCategory = await _repository.MstExpenseCategory.ExpenseCategoriesByClaimType("Mileage");

                //var mstExpenseCategory = await _repository.MstExpenseCategory.GetExpenseCategoryWithTypesByIdAsync(dtItem.ExpenseCategoryID);

                dtMileageClaim.AccountCode = mstExpenseCategory.ExpenseCode + "-" + mstFacility1.MstDepartment.Code + "-" + mstFacility1.Code + mstExpenseCategory.Default;
                dtMileageClaims.Add(dtMileageClaim);
            }

            string ClaimStatus = "";
            //var mstExpenseCategory = await _repository.MstExpenseCategory.ExpenseCategoriesByClaimTypeID(1);
            long MCID = 0;
            try
            {
                //CBRID = Convert.ToInt32(Session["CBRID"].ToString());
                MCID = Convert.ToInt64(mileageClaimViewModel.MCID);
                if (MCID == 0 || TempData["Updatestatus"].ToString() == "Recreate")
                {
                    ClaimStatus = "Recreate";
                    MCID = 0;
                }
                else if (MCID == 0)
                    ClaimStatus = "Add";
                else
                    ClaimStatus = "Update";

                if (mileageClaimViewModel.ClaimAddCondition == "claimDraft")
                {
                    mstMileageClaim.MCID = 0;
                }
                else
                {
                    mstMileageClaim.MCID = MCID;
                }
                //mstExpenseClaim.ECNo = expenseClaimViewModel.;
            }
            catch { }

            MileageClaimDetailVM mileageClaimDetailVM = new MileageClaimDetailVM();
            //List<DtMileageClaimVM> dtMileageClaimVMs = new List<DtMileageClaimVM>();
            mileageClaimDetailVM.DtMileageClaimVMs = new List<DtMileageClaimVM>();
            // List<clsDtSupplierPO> oclsDtSupplierPO = new List<clsDtSupplierPO>();
            foreach (var item in dtMileageClaims)
            {
                DtMileageClaimVM dtMileageClaimVM = new DtMileageClaimVM();
                if (MCID == 0 || TempData["Updatestatus"].ToString() == "Recreate")
                {
                    dtMileageClaimVM.MCItemID = 0;
                    dtMileageClaimVM.MCID = 0;
                }

                dtMileageClaimVM.FacilityID = item.FacilityID;
                //dtMileageClaimVM.Payee = item.Payee;
                //dtMileageClaimVM.Particulars = item.Particulars;
                //dtMileageClaimVM.ExpenseCategory = item.MstExpenseCategory.Description;
                //dtMileageClaimVM.ExpenseCategoryID = item.MstExpenseCategory.ExpenseCategoryID;
                //dtMileageClaimVM.Reason = item.Reason;
                //dtMileageClaimVM.EmployeeNo = item.EmployeeNo;
                //dtMileageClaimVM.ChequeNo = item.ChequeNo;
                dtMileageClaimVM.Amount = item.Amount;
                //dtMileageClaimVM.Gst = item.GST;
                //dtMileageClaimVM.AmountWithGST = item.Amount + item.GST;
                //dtMileageClaimVM.Facility = item.Facility;
                dtMileageClaimVM.AccountCode = item.AccountCode;
                dtMileageClaimVM.DateOfJourney = item.DateOfJourney;
                mileageClaimDetailVM.DtMileageClaimVMs.Add(dtMileageClaimVM);
            }

            var GroupByQS = mileageClaimDetailVM.DtMileageClaimVMs.GroupBy(s => s.AccountCode);

            mileageClaimDetailVM.DtMileageClaimVMSummary = new List<DtMileageClaimVM>();

            foreach (var group in GroupByQS)
            {
                DtMileageClaimVM dtMileageClaimVM = new DtMileageClaimVM();
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
                        ExpenseDesc = "Mileage Claim " + Convert.ToDateTime(dtExpense.DateOfJourney).ToString("dd/MM/yyyy", System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
                    i++;
                    amount = amount + dtExpense.Amount;
                    //gst = gst + dtExpense.Gst;
                    //sumamount = sumamount + dtExpense.AmountWithGST;
                    ExpenseCat = "Travelling Private Car";
                    facilityID = dtExpense.FacilityID;
                    if (dtExpense.FacilityID != null)
                    {
                        var mstFacility1 = await _repository.MstFacility.GetFacilityByIdAsync(dtExpense.FacilityID);
                        Facility = mstFacility1.FacilityName;
                    }
                    AccountCode = dtExpense.AccountCode;
                }
                //gst = gst / group.Count();
                dtMileageClaimVM.Description = ExpenseDesc;
                dtMileageClaimVM.ExpenseCategory = ExpenseCat;
                dtMileageClaimVM.FacilityID = facilityID;
                dtMileageClaimVM.FacilityName = Facility;
                dtMileageClaimVM.AccountCode = AccountCode;
                dtMileageClaimVM.Amount = amount;
                //dtMileageClaimVM.Gst = gst;
                //dtMileageClaimVM.AmountWithGST = sumamount;
                mileageClaimDetailVM.DtMileageClaimVMSummary.Add(dtMileageClaimVM);
            }
            List<DtMileageClaimSummary> lstMileageClaimSummary = new List<DtMileageClaimSummary>();
            foreach (var item in mileageClaimDetailVM.DtMileageClaimVMSummary)
            {
                DtMileageClaimSummary dtMileageClaimSummary1 = new DtMileageClaimSummary();
                dtMileageClaimSummary1.AccountCode = item.AccountCode;
                dtMileageClaimSummary1.Amount = item.Amount;
                dtMileageClaimSummary1.ExpenseCategory = item.ExpenseCategory;
                dtMileageClaimSummary1.FacilityID = item.FacilityID;
                dtMileageClaimSummary1.Facility = item.FacilityName;
                dtMileageClaimSummary1.Description = item.Description.ToUpper();
                dtMileageClaimSummary1.TaxClass = 4;
                //dtMileageClaimSummary1.GST = item.Gst;
                //dtMileageClaimSummary1.AmountWithGST = item.AmountWithGST;
                lstMileageClaimSummary.Add(dtMileageClaimSummary1);
            }

            DtMileageClaimSummary dtMileageClaimSummary = new DtMileageClaimSummary();
            dtMileageClaimSummary.AccountCode = "425000";
            dtMileageClaimSummary.Amount = mstMileageClaim.GrandTotal;
            //dtMileageClaimSummary.GST = mstMileageClaim.TotalAmount - mstMileageClaim.GrandTotal;
            //dtMileageClaimSummary.AmountWithGST = mstMileageClaim.TotalAmount;
            dtMileageClaimSummary.TaxClass = 0;
            dtMileageClaimSummary.ExpenseCategory = "DBS";
            dtMileageClaimSummary.Description = "";
            lstMileageClaimSummary.Add(dtMileageClaimSummary);

            var res = await _repository.MstMileageClaim.SaveItems(mstMileageClaim, dtMileageClaims, lstMileageClaimSummary);

            //var res = await _repository.MstMileageClaim.SaveItems(mstMileageClaim, dtMileageClaims);
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
                    mstMileageClaim = await _repository.MstMileageClaim.GetMileageClaimByIdAsync(res);
                    if (mstMileageClaim.ApprovalStatus == 1)
                    {
                        string VerifierIDs = "";
                        string ApproverIDs = "";
                        string UserApproverIDs = "";
                        string HODApproverID = "";
                        try
                        {
                            //VerifierIDs = mstMileageClaim.Verifier.Split(',');
                            //VerifierIDs = string.Join(",", ExpenseverifierIDs.Skip(1));
                            string[] verifierIDs = mstMileageClaim.Verifier.Split(',');
                            ApproverIDs = mstMileageClaim.Approver;
                            HODApproverID = mstMileageClaim.HODApprover;



                            //BackgroundJob.Enqueue(() => _sendMailServices.SendEmail());
                            //Mail Code Implementation for Verifiers

                            foreach (string verifierID in verifierIDs)
                            {
                                if (verifierID != "")
                                {
                                    string domainUrl = HttpContext.Request.Scheme + "://" + HttpContext.Request.Host;
                                    string clickUrl = domainUrl + "/" + "FinanceMileageClaim/Details/" + mstMileageClaim.MCID;

                                    var mstSenderDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value));
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
                                    int userID = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
                                    var subject = "Mileage Claim for Verification " + claimNo;

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
                        string[] userApproverIDs = mstMileageClaim.UserApprovers.ToString().Split(',');
                        foreach (string userApproverID in userApproverIDs)
                        {
                            if (userApproverID != "")
                            {
                                string domainUrl = HttpContext.Request.Scheme + "://" + HttpContext.Request.Host;
                                string clickUrl = domainUrl + "/" + "HodSummary/Details/" + mstMileageClaim.MCID;

                                var mstSenderDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value));
                                var senderName = mstSenderDetails.Name;
                                var mstVerifierDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(userApproverID));
                                var toEmail = mstVerifierDetails.EmailAddress;
                                var receiverName = mstVerifierDetails.Name;
                                var claimNo = mstMileageClaim.MCNo;
                                var screen = "Mileage Claim";
                                var approvalType = "Approval Request";
                                int userID = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
                                var subject = "Mileage Claim for Approval " + claimNo;

                                BackgroundJob.Enqueue(() => _sendMailServices.SendEmail("EmailTemplate.html", screen, subject, senderName, receiverName, claimNo, approvalType, userID, toEmail, clickUrl));
                            }
                            break;
                        }
                    }
                    TempData["Message"] = "Mileage Claim added successfully";
                }
                else
                {
                    mstMileageClaim = await _repository.MstMileageClaim.GetMileageClaimByIdAsync(res);
                    if (mstMileageClaim.ApprovalStatus == 1)
                    {
                        string VerifierIDs = "";
                        string ApproverIDs = "";
                        string UserApproverIDs = "";
                        string HODApproverID = "";
                        try
                        {
                            //VerifierIDs = mstMileageClaim.Verifier.Split(',');
                            //VerifierIDs = string.Join(",", ExpenseverifierIDs.Skip(1));
                            string[] verifierIDs = mstMileageClaim.Verifier.Split(',');
                            ApproverIDs = mstMileageClaim.Approver;
                            HODApproverID = mstMileageClaim.HODApprover;



                            //BackgroundJob.Enqueue(() => _sendMailServices.SendEmail());
                            //Mail Code Implementation for Verifiers

                            foreach (string verifierID in verifierIDs)
                            {
                                if (verifierID != "")
                                {
                                    string domainUrl = HttpContext.Request.Scheme + "://" + HttpContext.Request.Host;
                                    string clickUrl = domainUrl + "/" + "FinanceMileageClaim/Details/" + mstMileageClaim.MCID;

                                    var mstSenderDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value));
                                    var senderName = mstSenderDetails.Name;
                                    var mstVerifierDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(verifierID));
                                    var toEmail = mstVerifierDetails.EmailAddress;
                                    var receiverName = mstVerifierDetails.Name;
                                    var claimNo = mstMileageClaim.MCNo;
                                    var screen = "Mileage Claim";
                                    var approvalType = "Verification Request";
                                    int userID = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
                                    var subject = "Mileage Claim for Verification " + claimNo;

                                    BackgroundJob.Enqueue(() => _sendMailServices.SendEmail("EmailTemplate.html", screen, subject, senderName, receiverName, claimNo, approvalType, userID, toEmail, clickUrl));
                                }
                                break;
                            }
                        }
                        catch
                        {
                        }
                    }
                    else if (mstMileageClaim.ApprovalStatus == 6)
                    {
                        string[] userApproverIDs = mstMileageClaim.UserApprovers.ToString().Split(',');
                        foreach (string userApproverID in userApproverIDs)
                        {
                            if (userApproverID != "")
                            {
                                string domainUrl = HttpContext.Request.Scheme + "://" + HttpContext.Request.Host;
                                string clickUrl = domainUrl + "/" + "HodSummary/Details/" + mstMileageClaim.MCID;

                                var mstSenderDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value));
                                var senderName = mstSenderDetails.Name;
                                var mstVerifierDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(userApproverID));
                                var toEmail = mstVerifierDetails.EmailAddress;
                                var receiverName = mstVerifierDetails.Name;
                                var claimNo = mstMileageClaim.MCNo;
                                var screen = "Mileage Claim";
                                var approvalType = "Approval Request";
                                int userID = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
                                var subject = "Mileage Claim for Approval " + claimNo;

                                BackgroundJob.Enqueue(() => _sendMailServices.SendEmail("EmailTemplate.html", screen, subject, senderName, receiverName, claimNo, approvalType, userID, toEmail, clickUrl));
                            }
                            break;
                        }
                    }
                    else if (mstMileageClaim.ApprovalStatus == 7)
                    {
                        string[] hODApproverIDs = mstMileageClaim.HODApprover.ToString().Split(',');
                        foreach (string hODApproverID in hODApproverIDs)
                        {
                            if (hODApproverID != "")
                            {
                                string domainUrl = HttpContext.Request.Scheme + "://" + HttpContext.Request.Host;
                                string clickUrl = domainUrl + "/" + "HodSummary/Details/" + mstMileageClaim.MCID;

                                var mstSenderDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value));
                                var senderName = mstSenderDetails.Name;
                                var mstVerifierDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(hODApproverID));
                                var toEmail = mstVerifierDetails.EmailAddress;
                                var receiverName = mstVerifierDetails.Name;
                                var claimNo = mstMileageClaim.MCNo;
                                var screen = "Mileage Claim";
                                var approvalType = "Approval Request";
                                int userID = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
                                var subject = "Mileage Claim for Approval " + claimNo;

                                BackgroundJob.Enqueue(() => _sendMailServices.SendEmail("EmailTemplate.html", screen, subject, senderName, receiverName, claimNo, approvalType, userID, toEmail, clickUrl));
                            }
                            break;
                        }
                    }
                    else
                    {
                        string[] ExpenseapproverIDs = mstMileageClaim.Approver.ToString().Split(',');
                        foreach (string approverID in ExpenseapproverIDs)
                        {
                            if (approverID != "")
                            {
                                string domainUrl = HttpContext.Request.Scheme + "://" + HttpContext.Request.Host;
                                string clickUrl = domainUrl + "/" + "FinanceMileageClaim/Details/" + mstMileageClaim.MCID;

                                var mstSenderDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value));
                                var senderName = mstSenderDetails.Name;
                                var mstVerifierDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(approverID));
                                var toEmail = mstVerifierDetails.EmailAddress;
                                var receiverName = mstVerifierDetails.Name;
                                var claimNo = mstMileageClaim.MCNo;
                                var screen = "Mileage Claim";
                                var approvalType = "Approval Request";
                                int userID = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
                                var subject = "Mileage Claim for Approval " + claimNo;

                                BackgroundJob.Enqueue(() => _sendMailServices.SendEmail("EmailTemplate.html", screen, subject, senderName, receiverName, claimNo, approvalType, userID, toEmail, clickUrl));
                            }
                            break;
                        }
                    }
                    TempData["Message"] = "Mileage Claim updated successfully";
                }

                return Json(new { res });
            }
            else
                return Json(new { res });
        }

        [HttpPost]
        public async Task<JsonResult> SaveDraftItems(string data)
        {
        //    var dateformats = new string[] { "yyyyMMdd", "yyyy/MM/dd", "dd/MM/yyyy", "dd-MM-yyyy",
        //"yyyyMMddHHmmss", "yyyy/MM/dd HH:mm:ss", "dd/MM/yyyy HH:mm:ss", "dd-MM-yyyy HH:mm:ss" , "yyyy-mm-dd HH:mm:ss", "dd/mm/yyyy" };
        //    MileageClaimViewModel mileageClaimViewModel = JsonConvert.DeserializeObject<MileageClaimViewModel>(data, new CustomDateTimeConverter(dateformats,"dd/mm/yyyy"));

            var mileageClaimViewModel = JsonConvert.DeserializeObject<MileageClaimViewModel>(data);
            
            var mstFacility = await _repository.MstFacility.GetFacilityWithDepartmentByIdAsync(Convert.ToInt32(HttpContext.User.FindFirst("delegatefacilityid") is null ? HttpContext.User.FindFirst("facilityid").Value : HttpContext.User.FindFirst("delegatefacilityid").Value));

            MstMileageClaimDraft mstMileageClaim = new MstMileageClaimDraft();
            mstMileageClaim.MCNo = mileageClaimViewModel.MCNo;
            mstMileageClaim.UserID = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
            mstMileageClaim.TravelMode = mileageClaimViewModel.TravelMode;
            mstMileageClaim.Verifier = "";
            mstMileageClaim.Approver = "";
            mstMileageClaim.FinalApprover = "";
            mstMileageClaim.ApprovalStatus = 1;
            mstMileageClaim.GrandTotal = mileageClaimViewModel.GrandTotal;
            mstMileageClaim.TotalKm = mileageClaimViewModel.TotalKm;
            mstMileageClaim.Company = mileageClaimViewModel.Company;
            mstMileageClaim.FacilityID = Convert.ToInt32(HttpContext.User.FindFirst("delegatefacilityid") is null ? HttpContext.User.FindFirst("facilityid").Value : HttpContext.User.FindFirst("delegatefacilityid").Value);
            mstMileageClaim.DepartmentID = mstFacility.MstDepartment.DepartmentID;
            mstMileageClaim.CreatedDate = DateTime.Now;
            mstMileageClaim.ModifiedDate = DateTime.Now;
            mstMileageClaim.CreatedBy = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
            mstMileageClaim.ModifiedBy = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
            mstMileageClaim.ApprovalDate = DateTime.Now;
            mstMileageClaim.ApprovalBy = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
            mstMileageClaim.TnC = true;

            List<DtMileageClaimDraft> dtMileageClaims = new List<DtMileageClaimDraft>();
            foreach (var dtItem in mileageClaimViewModel.dtClaims)
            {
                var mstFacility1 = await _repository.MstFacility.GetFacilityWithDepartmentByIdAsync(Convert.ToInt32(dtItem.FacilityID));

                DtMileageClaimDraft dtMileageClaim = new DtMileageClaimDraft();
                dtMileageClaim.MCItemID = dtItem.MCItemID;
                dtMileageClaim.Amount = dtItem.Amount;
                dtMileageClaim.DateOfJourney = dtItem.DateOfJourney;

                dtMileageClaim.DateOfJourney = dtItem.DateOfJourney;
                dtMileageClaim.FacilityID = dtItem.FacilityID;
                dtMileageClaim.FromFacilityID = dtItem.FromFacilityID;
                dtMileageClaim.ToFacilityID = dtItem.ToFacilityID;
                dtMileageClaim.InTime = DateTime.Parse(dtItem.InTimeTime);
                dtMileageClaim.OutTime = DateTime.Parse(dtItem.OutTimeTime); ;
                dtMileageClaim.StartReading = dtItem.StartReading;
                dtMileageClaim.EndReading = dtItem.EndReading;
                dtMileageClaim.Kms = dtItem.Kms;
                dtMileageClaim.Remark = dtItem.Remark;
                dtMileageClaim.Amount = dtItem.Amount;
                dtMileageClaim.OrderBy = dtItem.OrderBy;

                var mstExpenseCategory = await _repository.MstExpenseCategory.ExpenseCategoriesByClaimType("Mileage");

                //var mstExpenseCategory = await _repository.MstExpenseCategory.GetExpenseCategoryWithTypesByIdAsync(dtItem.ExpenseCategoryID);

                dtMileageClaim.AccountCode = mstExpenseCategory.ExpenseCode + "-" + mstFacility1.MstDepartment.Code + "-" + mstFacility1.Code + mstExpenseCategory.Default;
                dtMileageClaims.Add(dtMileageClaim);
            }

            string ClaimStatus = "";
            //var mstExpenseCategory = await _repository.MstExpenseCategory.ExpenseCategoriesByClaimTypeID(1);
            long MCID = 0;
            try
            {
                //CBRID = Convert.ToInt32(Session["CBRID"].ToString());
                MCID = Convert.ToInt64(mileageClaimViewModel.MCID);
                if (MCID == 0 || TempData["Updatestatus"].ToString() == "Recreate")
                {
                    ClaimStatus = "Recreate";
                    MCID = 0;
                }
                else if (MCID == 0)
                    ClaimStatus = "Add";
                else
                    ClaimStatus = "Update";
                mstMileageClaim.MCID = MCID;
                //mstExpenseClaim.ECNo = expenseClaimViewModel.;
            }
            catch { }

            MileageClaimDetailVM mileageClaimDetailVM = new MileageClaimDetailVM();
            //List<DtMileageClaimVM> dtMileageClaimVMs = new List<DtMileageClaimVM>();
            mileageClaimDetailVM.DtMileageClaimVMs = new List<DtMileageClaimVM>();
            // List<clsDtSupplierPO> oclsDtSupplierPO = new List<clsDtSupplierPO>();
            foreach (var item in dtMileageClaims)
            {
                DtMileageClaimVM dtMileageClaimVM = new DtMileageClaimVM();
                if (MCID == 0 || TempData["Updatestatus"].ToString() == "Recreate")
                {
                    dtMileageClaimVM.MCItemID = 0;
                    dtMileageClaimVM.MCID = 0;
                }
                //dtMileageClaimVM.Payee = item.Payee;
                //dtMileageClaimVM.Particulars = item.Particulars;
                //dtMileageClaimVM.ExpenseCategory = item.MstExpenseCategory.Description;
                //dtMileageClaimVM.ExpenseCategoryID = item.MstExpenseCategory.ExpenseCategoryID;
                //dtMileageClaimVM.Reason = item.Reason;
                //dtMileageClaimVM.EmployeeNo = item.EmployeeNo;
                //dtMileageClaimVM.ChequeNo = item.ChequeNo;
                dtMileageClaimVM.Amount = item.Amount;
                //dtMileageClaimVM.Gst = item.GST;
                //dtMileageClaimVM.AmountWithGST = item.Amount + item.GST;
                //dtMileageClaimVM.Facility = item.Facility;
                dtMileageClaimVM.AccountCode = item.AccountCode;
                dtMileageClaimVM.DateOfJourney = item.DateOfJourney;
                mileageClaimDetailVM.DtMileageClaimVMs.Add(dtMileageClaimVM);
            }

            var GroupByQS = mileageClaimDetailVM.DtMileageClaimVMs.GroupBy(s => s.AccountCode);

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
                        ExpenseDesc = "Mileage Claim " + Convert.ToDateTime(dtExpense.DateOfJourney).ToString("dd/MM/yyyy", System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
                    i++;
                    amount = amount + dtExpense.Amount;
                    //gst = gst + dtExpense.Gst;
                    //sumamount = sumamount + dtExpense.AmountWithGST;
                    ExpenseCat = "Travelling Private Car";
                    AccountCode = dtExpense.AccountCode;
                }
                //gst = gst / group.Count();
                dtMileageClaimVM.Description = ExpenseDesc;
                dtMileageClaimVM.ExpenseCategory = ExpenseCat;
                dtMileageClaimVM.AccountCode = AccountCode;
                dtMileageClaimVM.Amount = amount;
                //dtMileageClaimVM.Gst = gst;
                //dtMileageClaimVM.AmountWithGST = sumamount;
                mileageClaimDetailVM.DtMileageClaimVMSummary.Add(dtMileageClaimVM);
            }
            List<DtMileageClaimSummaryDraft> lstMileageClaimSummary = new List<DtMileageClaimSummaryDraft>();
            foreach (var item in mileageClaimDetailVM.DtMileageClaimVMSummary)
            {
                DtMileageClaimSummaryDraft dtMileageClaimSummary1 = new DtMileageClaimSummaryDraft();
                dtMileageClaimSummary1.AccountCode = item.AccountCode;
                dtMileageClaimSummary1.Amount = item.Amount;
                dtMileageClaimSummary1.ExpenseCategory = item.ExpenseCategory;
                dtMileageClaimSummary1.Description = item.Description.ToUpper();
                dtMileageClaimSummary1.TaxClass = 4;
                //dtMileageClaimSummary1.GST = item.Gst;
                //dtMileageClaimSummary1.AmountWithGST = item.AmountWithGST;
                lstMileageClaimSummary.Add(dtMileageClaimSummary1);
            }

            DtMileageClaimSummaryDraft dtMileageClaimSummary = new DtMileageClaimSummaryDraft();
            dtMileageClaimSummary.AccountCode = "425000";
            dtMileageClaimSummary.Amount = mstMileageClaim.GrandTotal;
            //dtMileageClaimSummary.GST = mstMileageClaim.TotalAmount - mstMileageClaim.GrandTotal;
            //dtMileageClaimSummary.AmountWithGST = mstMileageClaim.TotalAmount;
            dtMileageClaimSummary.TaxClass = 0;
            dtMileageClaimSummary.ExpenseCategory = "DBS";
            dtMileageClaimSummary.Description = "";
            lstMileageClaimSummary.Add(dtMileageClaimSummary);

            var res = await _repository.MstMileageClaimDraft.SaveDraftItems(mstMileageClaim, dtMileageClaims, lstMileageClaimSummary);

            //var res = await _repository.MstMileageClaim.SaveItems(mstMileageClaim, dtMileageClaims);
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
                    TempData["Message"] = "Mileage Claim draft added successfully";
                else
                    TempData["Message"] = "Mileage Claim draft updated successfully";

                return Json(new { res });
            }
            else
                return Json(new { res });
        }

        

        public async Task<IActionResult> Create(string id, string Updatestatus)
        {
            TempData["Updatestatus"] = "Add";
            TempData["claimaddcondition"] = "claimnew";
            MileageClaimDetailVM mileageClaimDetailVM = new MileageClaimDetailVM();
            mileageClaimDetailVM.DtMileageClaimVMs = new List<DtMileageClaimVM>();
            mileageClaimDetailVM.MileageClaimAudits = new List<MileageClaimAuditVM>();

            if (User != null && User.Identity.IsAuthenticated)
            {
                if (!string.IsNullOrEmpty(id))
                {
                    long idd = Convert.ToInt64(id);
                    ViewBag.CID = idd;
                    var dtMileageClaims = await _repository.DtMileageClaim.GetDtMileageClaimByIdAsync(idd);

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
                        if (Updatestatus == "Recreate")
                        {
                            ViewBag.UpdateStatus = "Recreate";
                            dtMileageClaimVM.MCItemID = 0;
                        }
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

                    mileageClaimDetailVM.MileageClaimFileUploads = new List<DtMileageClaimFileUpload>();
                    var fileUploads = await _repository.DtMileageClaimFileUpload.GetDtMileageClaimAuditByIdAsync(idd);
                    if (Updatestatus == "Recreate" && fileUploads != null && fileUploads.Count > 0)
                    {
                        foreach (var uploaddata in fileUploads)
                        {
                            uploaddata.MCID = 0;
                            mileageClaimDetailVM.MileageClaimFileUploads.Add(uploaddata);
                        }
                    }
                    else
                        mileageClaimDetailVM.MileageClaimFileUploads = fileUploads;

                    var mstMileageClaim = await _repository.MstMileageClaim.GetMileageClaimByIdAsync(idd);

                    //if (mstExpenseClaim == null)
                    //{
                    //    return NotFound();
                    //}
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

                    //    expenseClaimDetailVM.DtExpenseClaimVMs.Add(dtExpenseClaimVM);
                    //}
                    //expenseClaimDetailVM.ExpenseClaimAudits = new List<MstExpenseClaimAudit>();

                    //expenseClaimDetailVM.ExpenseClaimAudits = _repository.MstExpenseClaimAudit.GetMstExpenseClaimAuditByIdAsync(id).Result.ToList();

                    //expenseClaimDetailVM.ExpenseClaimFileUploads = new List<DtExpenseClaimFileUpload>();

                    //expenseClaimDetailVM.ExpenseClaimFileUploads = _repository.DtExpenseClaimFileUpload.GetDtExpenseClaimAuditByIdAsync(id).Result.ToList();

                    MileageClaimVM mileageClaimVM = new MileageClaimVM();
                    //tBClaimVM.ClaimType = mstExpenseClaim.ClaimType;
                    mileageClaimVM.GrandTotal = mstMileageClaim.GrandTotal;
                    mileageClaimVM.TotalKm = mstMileageClaim.TotalKm;
                    //tBClaimVM.TotalAmount = mstExpenseClaim.TotalAmount;
                    mileageClaimVM.Company = mstMileageClaim.Company;
                    mileageClaimVM.Name = mstMileageClaim.MstUser.Name;
                    mileageClaimVM.DepartmentName = mstMileageClaim.MstDepartment.Department;
                    mileageClaimVM.FacilityName = mstMileageClaim.MstFacility.FacilityName;
                    mileageClaimVM.CreatedDate = mstMileageClaim.CreatedDate.ToString("d");
                    mileageClaimVM.Verifier = mstMileageClaim.Verifier;
                    mileageClaimVM.Approver = mstMileageClaim.Approver;
                    mileageClaimVM.MCNo = mstMileageClaim.MCNo;
                    mileageClaimVM.TravelMode = mstMileageClaim.TravelMode;

                    mileageClaimDetailVM.MileageClaimVM = mileageClaimVM;

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
                    mileageClaimDetailVM.MileageClaimAudits = new List<MileageClaimAuditVM>();
                    mileageClaimDetailVM.MileageClaimFileUploads = new List<DtMileageClaimFileUpload>();
                    MileageClaimVM mileageClaimVM = new MileageClaimVM();
                    //tBClaimVM.ClaimType = "";
                    mileageClaimVM.GrandTotal = 0;
                    mileageClaimVM.TotalKm = 0;
                    //tBClaimVM.TotalAmount = 0;
                    mileageClaimVM.Company = "";
                    mileageClaimVM.Name = "";
                    mileageClaimVM.DepartmentName = "";
                    mileageClaimVM.FacilityName = "";
                    mileageClaimVM.CreatedDate = "";
                    mileageClaimVM.Verifier = "";
                    mileageClaimVM.Approver = "";
                    mileageClaimVM.MCNo = "";
                    mileageClaimVM.TravelMode = "";

                    DtMileageClaimVM dtMileageClaimVM = new DtMileageClaimVM();
                    dtMileageClaimVM.MCItemID = 0;
                    dtMileageClaimVM.MCID = 0;
                    //dtMileageClaimVM.DateOfJourney = "";
                    //dtMileageClaimVM.InTime = "";
                    //dtMileageClaimVM.OutTime = "";
                    dtMileageClaimVM.StartReading = 0;
                    dtMileageClaimVM.EndReading = 0;
                    dtMileageClaimVM.Kms = 0;
                    dtMileageClaimVM.Remark = "";
                    dtMileageClaimVM.Amount = 0;
                    dtMileageClaimVM.AccountCode = "";

                    mileageClaimDetailVM.DtMileageClaimVMs.Add(dtMileageClaimVM);
                    mileageClaimDetailVM.MileageClaimVM = mileageClaimVM;


                    TempData["status"] = "Add";
                }
                var mstUsersWithDetails = await _repository.MstUser.GetUserWithDetailsByIdAsync(Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value));

                SelectList facilities = new SelectList(await _repository.MstFacility.GetAllFacilityAsync("active"), "FacilityID", "FacilityName");
                //int userFacilityId = mstUsersWithDetails.MstFacility.FacilityID;
                int userFacilityId = Convert.ToInt32(HttpContext.User.FindFirst("delegatefacilityid") is null ? HttpContext.User.FindFirst("facilityid").Value : HttpContext.User.FindFirst("delegatefacilityid").Value);
                var currFacility = await _repository.MstFacility.GetFacilityWithDepartmentByIdAsync(userFacilityId);
                var userFacility = facilities.Where(x => x.Value == userFacilityId.ToString()).FirstOrDefault();
                if (userFacility != null)
                {
                    facilities.Where(x => x.Value == userFacilityId.ToString()).FirstOrDefault().Selected = true;
                }
                ViewData["FacilityID"] = facilities;

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
                string privateCar = _configuration.GetValue<string>("PrivateCar");
                string motorCycle = _configuration.GetValue<string>("MotorCycle");
                ViewBag.MileageClaimLimit = mstUsersWithDetails.MileageLimit;
                ViewBag.PrivateCar = privateCar;
                ViewBag.MotorCycle = motorCycle;

            }
            return View(mileageClaimDetailVM);
        }

        public async Task<IActionResult> CreateDraft(string id, string Updatestatus)
        {
            TempData["Updatestatus"] = "Add";
            TempData["claimaddcondition"] = "claimDraft";
            MileageClaimDetailVM mileageClaimDetailVM = new MileageClaimDetailVM();
            mileageClaimDetailVM.DtMileageClaimVMs = new List<DtMileageClaimVM>();
            mileageClaimDetailVM.MileageClaimAudits = new List<MileageClaimAuditVM>();

            if (User != null && User.Identity.IsAuthenticated)
            {
                if (!string.IsNullOrEmpty(id))
                {
                    long idd = Convert.ToInt64(id);
                    ViewBag.CID = idd;
                    var dtMileageClaims = await _repository.DtMileageClaimDraft.GetDtMileageClaimDraftByIdAsync(idd);

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

                    mileageClaimDetailVM.MileageClaimFileUploads = new List<DtMileageClaimFileUpload>();

                    var mcFuploads = await _repository.DtMileageClaimFileUploadDraft.GetDtMileageClaimDraftAuditByIdAsync(idd);

                    mileageClaimDetailVM.MileageClaimFileUploads = new List<DtMileageClaimFileUpload>();


                    foreach (var item in mcFuploads)
                    {
                        MstMileageClaim mstMileageClaim1 = new MstMileageClaim();
                        if (item.MstMileageClaim != null)
                        {
                            mstMileageClaim1 = new MstMileageClaim()
                            {
                                ApprovalBy = item.MstMileageClaim.ApprovalBy,
                                ApprovalDate = item.MstMileageClaim.ApprovalDate,
                                ApprovalStatus = item.MstMileageClaim.ApprovalStatus,
                                ModifiedDate = item.MstMileageClaim.ModifiedDate,
                                ModifiedBy = item.MstMileageClaim.ModifiedBy,
                                Approver = item.MstMileageClaim.Approver,
                                Company = item.MstMileageClaim.Company,
                                CreatedBy = item.MstMileageClaim.CreatedBy,
                                CreatedDate = item.MstMileageClaim.CreatedDate,
                                DepartmentID = item.MstMileageClaim.DepartmentID,
                                MCID = item.MstMileageClaim.MCID,
                                MCNo = item.MstMileageClaim.MCNo,
                                FacilityID = item.MstMileageClaim.FacilityID,
                                FinalApprover = item.MstMileageClaim.FinalApprover,
                                GrandTotal = item.MstMileageClaim.GrandTotal,
                                HODApprover = item.MstMileageClaim.HODApprover,
                                MstDepartment = item.MstMileageClaim.MstDepartment,
                                MstFacility = item.MstMileageClaim.MstFacility,
                                MstUser = item.MstMileageClaim.MstUser,
                                TnC = item.MstMileageClaim.TnC,
                                TotalKm = item.MstMileageClaim.TotalKm,
                                UserApprovers = item.MstMileageClaim.UserApprovers,
                                UserID = item.MstMileageClaim.UserID,
                                Verifier = item.MstMileageClaim.Verifier,
                                VoidReason = item.MstMileageClaim.VoidReason,
                                TravelMode = item.MstMileageClaim.TravelMode
                            };
                        }

                        mileageClaimDetailVM.MileageClaimFileUploads.Add(new DtMileageClaimFileUpload()
                        {
                            CreatedBy = item.CreatedBy,
                            CreatedDate = item.CreatedDate,
                            MCID = item.MCID,
                            FileID = item.FileID,
                            FileName = item.FileName,
                            FilePath = item.FilePath,
                            IsDeleted = item.IsDeleted,
                            ModifiedBy = item.ModifiedBy,
                            ModifiedDate = item.ModifiedDate,
                            MstMileageClaim = mstMileageClaim1
                        });
                    }

                    var mstMileageClaim = await _repository.MstMileageClaimDraft.GetMileageClaimDraftByIdAsync(idd);

                    MileageClaimVM mileageClaimVM = new MileageClaimVM();
                    //tBClaimVM.ClaimType = mstExpenseClaim.ClaimType;
                    mileageClaimVM.GrandTotal = mstMileageClaim.GrandTotal;
                    mileageClaimVM.TotalKm = mstMileageClaim.TotalKm;
                    //tBClaimVM.TotalAmount = mstExpenseClaim.TotalAmount;
                    mileageClaimVM.Company = mstMileageClaim.Company;
                    mileageClaimVM.Name = mstMileageClaim.MstUser.Name;
                    mileageClaimVM.DepartmentName = mstMileageClaim.MstDepartment.Department;
                    mileageClaimVM.FacilityName = mstMileageClaim.MstFacility.FacilityName;
                    mileageClaimVM.CreatedDate = mstMileageClaim.CreatedDate.ToString("d");
                    mileageClaimVM.Verifier = mstMileageClaim.Verifier;
                    mileageClaimVM.Approver = mstMileageClaim.Approver;
                    mileageClaimVM.MCNo = mstMileageClaim.MCNo;
                    mileageClaimVM.TravelMode = mstMileageClaim.TravelMode;

                    mileageClaimDetailVM.MileageClaimVM = mileageClaimVM;

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
                    mileageClaimDetailVM.MileageClaimAudits = new List<MileageClaimAuditVM>();
                    mileageClaimDetailVM.MileageClaimFileUploads = new List<DtMileageClaimFileUpload>();
                    MileageClaimVM mileageClaimVM = new MileageClaimVM();
                    //tBClaimVM.ClaimType = "";
                    mileageClaimVM.GrandTotal = 0;
                    mileageClaimVM.TotalKm = 0;
                    //tBClaimVM.TotalAmount = 0;
                    mileageClaimVM.Company = "";
                    mileageClaimVM.Name = "";
                    mileageClaimVM.DepartmentName = "";
                    mileageClaimVM.FacilityName = "";
                    mileageClaimVM.CreatedDate = "";
                    mileageClaimVM.Verifier = "";
                    mileageClaimVM.Approver = "";
                    mileageClaimVM.MCNo = "";
                    mileageClaimVM.TravelMode = "";

                    DtMileageClaimVM dtMileageClaimVM = new DtMileageClaimVM();
                    dtMileageClaimVM.MCItemID = 0;
                    dtMileageClaimVM.MCID = 0;
                    //dtMileageClaimVM.DateOfJourney = "";
                    //dtMileageClaimVM.InTime = "";
                    //dtMileageClaimVM.OutTime = "";
                    dtMileageClaimVM.StartReading = 0;
                    dtMileageClaimVM.EndReading = 0;
                    dtMileageClaimVM.Kms = 0;
                    dtMileageClaimVM.Remark = "";
                    dtMileageClaimVM.Amount = 0;
                    dtMileageClaimVM.AccountCode = "";

                    mileageClaimDetailVM.DtMileageClaimVMs.Add(dtMileageClaimVM);
                    mileageClaimDetailVM.MileageClaimVM = mileageClaimVM;


                    TempData["status"] = "Add";
                }
                var mstUsersWithDetails = await _repository.MstUser.GetUserWithDetailsByIdAsync(Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value));

                SelectList facilities = new SelectList(await _repository.MstFacility.GetAllFacilityAsync("active"), "FacilityID", "FacilityName");
                //int userFacilityId = mstUsersWithDetails.MstFacility.FacilityID;
                int userFacilityId = Convert.ToInt32(HttpContext.User.FindFirst("delegatefacilityid") is null ? HttpContext.User.FindFirst("facilityid").Value : HttpContext.User.FindFirst("delegatefacilityid").Value);
                var currFacility = await _repository.MstFacility.GetFacilityWithDepartmentByIdAsync(userFacilityId);
                var userFacility = facilities.Where(x => x.Value == userFacilityId.ToString()).FirstOrDefault();
                if (userFacility != null)
                {
                    facilities.Where(x => x.Value == userFacilityId.ToString()).FirstOrDefault().Selected = true;
                }
                ViewData["FacilityID"] = facilities;

                ViewData["Name"] = mstUsersWithDetails.Name;
                ViewData["FacilityName"] = currFacility.FacilityName;
                ViewData["Department"] = currFacility.MstDepartment.Department;
                ViewData["UserFacilityID"] = mstUsersWithDetails.MstFacility.FacilityID;
                string privateCar = _configuration.GetValue<string>("PrivateCar");
                string motorCycle = _configuration.GetValue<string>("MotorCycle");
                ViewBag.MileageClaimLimit = mstUsersWithDetails.MileageLimit;
                ViewBag.PrivateCar = privateCar;
                ViewBag.MotorCycle = motorCycle;

            }
            return View("Create", mileageClaimDetailVM);
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

        public async Task<IActionResult> Details(long? id)
        {
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
                    //dtMileageClaimVM.AmountWithGST = sumamount;
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
                mileageClaimVM.CreatedDate = Convert.ToDateTime(mstMileageClaim.CreatedDate).ToString("dd/MM/yyyy", System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
                mileageClaimVM.Verifier = mstMileageClaim.Verifier;
                mileageClaimVM.Approver = mstMileageClaim.Approver;
                ViewBag.MCID = id;
                mileageClaimVM.MCNo = mstMileageClaim.MCNo;
                TempData["CreatedBy"] = mstMileageClaim.CreatedBy;
                ViewBag.Approvalstatus = mstMileageClaim.ApprovalStatus;

                if (mstMileageClaim.Verifier == mstMileageClaim.DVerifier && mstMileageClaim.Approver == mstMileageClaim.DApprover && mstMileageClaim.UserApprovers == mstMileageClaim.DUserApprovers && mstMileageClaim.HODApprover == mstMileageClaim.DHODApprover)
                {
                    ViewBag.UserEditStatus = 4;
                }
                else
                {
                    ViewBag.UserEditStatus = 0;
                }

                TempData["ApprovedStatus"] = mstMileageClaim.ApprovalStatus;
                TempData["FinalApproverID"] = mstMileageClaim.FinalApprover;
                ViewBag.VoidReason = mstMileageClaim.VoidReason == null ? "" : mstMileageClaim.VoidReason;

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
                if (mstMileageClaim.Verifier != "")
                {
                    string[] verifierIDs = mstMileageClaim.Verifier.Split(',');
                    TempData["QueryMCVerifierIDs"] = string.Join(",", verifierIDs);
                    foreach (string verifierID in verifierIDs)
                    {
                        if (verifierID != "" && verifierID == (HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value) && User.IsInRole("Finance"))
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
                    TempData["VerifierIDs"] = mstMileageClaim.Verifier;
                    TempData["ApproverIDs"] = mstMileageClaim.Approver;
                }

                //Approval Process code
                if (mstMileageClaim.Approver != "" && mstMileageClaim.Verifier == "")
                {
                    string[] approverIDs = mstMileageClaim.Approver.Split(',');
                    TempData["QueryMCApproverIDs"] = string.Join(",", approverIDs);
                    foreach (string approverID in approverIDs)
                    {
                        if (approverID != "" && approverID == (HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value) && User.IsInRole("Finance"))
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

                if (mstMileageClaim.UserApprovers != "" && mstMileageClaim.Verifier == "")
                {
                    string[] userApproverIDs = mstMileageClaim.UserApprovers.Split(',');
                    TempData["QueryMCUserApproverIDs"] = string.Join(",", userApproverIDs);
                    foreach (string approverID in userApproverIDs)
                    {
                        if (approverID != "" && approverID == (HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value))
                        {
                            TempData["ApprovedStatus"] = mstMileageClaim.ApprovalStatus;
                            //Session["ApprovedStatus"] = oSupplierPO.Approvalstatus;
                            TempData["HODApproverIDs"] = string.Join(",", userApproverIDs.Skip(1));
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

                if (mstMileageClaim.HODApprover != "" && mstMileageClaim.Verifier == "")
                {
                    string[] hodApproverIDs = mstMileageClaim.HODApprover.Split(',');
                    TempData["QueryMCHODApproverIDs"] = string.Join(",", hodApproverIDs);
                    foreach (string approverID in hodApproverIDs)
                    {
                        if (approverID != "" && approverID == (HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value))
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


                int UserId = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
                ViewBag.userID = UserId;
                //var Userlist = objERPEntities.MstUsers.ToList().Where(i => i.UserID != UserId);
                var UserIds = new List<string>();
                //var Userlist1 = _context.users.ToList().Where(i => i.UserID != UserId);
                var Userlist = await _repository.MstUser.GetAllMCUsersForQueryAsync(UserId, UserIds);
                var Creater = TempData["CreatedBy"];
                var Verifiers = TempData["QueryMCVerifierIDs"];
                var UserApprovers = TempData["QueryMCUserApproverIDs"];
                var HODApprovers = TempData["QueryMCHODApproverIDs"];
                var Approvers = TempData["QueryMCApproverIDs"];

                string[] CreaterId = Creater.ToString().Split(',');
                string[] VerifiersId = Verifiers.ToString().Split(',');
                string[] UserApproversId = UserApprovers.ToString().Split(',');
                string[] HODApproversId = HODApprovers.ToString().Split(',');
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

        public async Task<IActionResult> DraftDetails(long? id)
        {
            TempData["claimaddcondition"] = "claimDraft";
            if (id == null)
            {
                return NotFound();
            }
            long MCID = Convert.ToInt64(id);

            if (User != null && User.Identity.IsAuthenticated)
            {
                var mstMileageClaim = await _repository.MstMileageClaimDraft.GetMileageClaimDraftByIdAsync(id);

                if (mstMileageClaim == null)
                {
                    return NotFound();
                }

                var dtMileageSummaries = await _repository.DtMileageClaimSummaryDraft.GetDtMileageClaimSummaryDraftByIdAsync(id);
                var dtMileageClaims = await _repository.DtMileageClaimDraft.GetDtMileageClaimDraftByIdAsync(id);
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
                mileageClaimDetailVM.DtMileageClaimSummaries = new List<DtMileageClaimSummary>();

                foreach (var item in dtMileageSummaries)
                {

                    mileageClaimDetailVM.DtMileageClaimSummaries.Add(new DtMileageClaimSummary()
                    {
                        AccountCode = item.AccountCode,
                        Amount = item.Amount,
                        AmountWithGST = item.AmountWithGST,
                        CItemID = item.CItemID,
                        Date = item.Date,
                        Description = item.Description,
                        MCID = item.MCID,
                        ExpenseCategory = item.ExpenseCategory,
                        GST = item.GST,
                        MstMileageClaim = item.MstMileageClaim,
                        TaxClass = item.TaxClass
                    });
                }

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
                    //dtMileageClaimVM.AmountWithGST = sumamount;
                    mileageClaimDetailVM.DtMileageClaimVMSummary.Add(dtMileageClaimVM);
                }

                mileageClaimDetailVM.MileageClaimAudits = new List<MileageClaimAuditVM>();
                //var dtMileageClaimAudits = await _repository.MstMileageClaimAudit.GetMstMileageClaimAuditByIdAsync(id);

                //foreach (var item in dtMileageClaimAudits)
                //{
                //    MileageClaimAuditVM mstMileageClaimAuditVM = new MileageClaimAuditVM();
                //    mstMileageClaimAuditVM.Action = item.Action;
                //    mstMileageClaimAuditVM.Description = item.Description;
                //    mstMileageClaimAuditVM.AuditDateTickle = Helper.RelativeDate(item.AuditDate);
                //    mileageClaimDetailVM.MileageClaimAudits.Add(mstMileageClaimAuditVM);
                //}

                mileageClaimDetailVM.MileageClaimFileUploads = new List<DtMileageClaimFileUpload>();

                var mcFileUploads = _repository.DtMileageClaimFileUploadDraft.GetDtMileageClaimDraftAuditByIdAsync(id).Result.ToList();

                foreach (var item in mcFileUploads)
                {
                    MstMileageClaim mstMileageClaim1 = new MstMileageClaim();
                    if (item.MstMileageClaim != null)
                    {
                        mstMileageClaim1 = new MstMileageClaim()
                        {
                            ApprovalBy = item.MstMileageClaim.ApprovalBy,
                            ApprovalDate = item.MstMileageClaim.ApprovalDate,
                            ApprovalStatus = item.MstMileageClaim.ApprovalStatus,
                            ModifiedDate = item.MstMileageClaim.ModifiedDate,
                            ModifiedBy = item.MstMileageClaim.ModifiedBy,
                            Approver = item.MstMileageClaim.Approver,
                            Company = item.MstMileageClaim.Company,
                            CreatedBy = item.MstMileageClaim.CreatedBy,
                            CreatedDate = item.MstMileageClaim.CreatedDate,
                            DepartmentID = item.MstMileageClaim.DepartmentID,
                            MCID = item.MstMileageClaim.MCID,
                            MCNo = item.MstMileageClaim.MCNo,
                            FacilityID = item.MstMileageClaim.FacilityID,
                            FinalApprover = item.MstMileageClaim.FinalApprover,
                            GrandTotal = item.MstMileageClaim.GrandTotal,
                            HODApprover = item.MstMileageClaim.HODApprover,
                            MstDepartment = item.MstMileageClaim.MstDepartment,
                            MstFacility = item.MstMileageClaim.MstFacility,
                            MstUser = item.MstMileageClaim.MstUser,
                            TnC = item.MstMileageClaim.TnC,
                            TotalKm = item.MstMileageClaim.TotalKm,
                            UserApprovers = item.MstMileageClaim.UserApprovers,
                            UserID = item.MstMileageClaim.UserID,
                            Verifier = item.MstMileageClaim.Verifier,
                            VoidReason = item.MstMileageClaim.VoidReason,
                            TravelMode = item.MstMileageClaim.TravelMode
                        };
                    }

                    mileageClaimDetailVM.MileageClaimFileUploads.Add(new DtMileageClaimFileUpload()
                    {
                        CreatedBy = item.CreatedBy,
                        CreatedDate = item.CreatedDate,
                        MCID = item.MCID,
                        FileID = item.FileID,
                        FileName = item.FileName,
                        FilePath = item.FilePath,
                        IsDeleted = item.IsDeleted,
                        ModifiedBy = item.ModifiedBy,
                        ModifiedDate = item.ModifiedDate,
                        MstMileageClaim = mstMileageClaim1
                    });
                }

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
                ViewBag.MCID = id;
                mileageClaimVM.MCNo = mstMileageClaim.MCNo;
                TempData["CreatedBy"] = mstMileageClaim.CreatedBy;
                ViewBag.Approvalstatus = mstMileageClaim.ApprovalStatus;


                TempData["ApprovedStatus"] = mstMileageClaim.ApprovalStatus;
                TempData["FinalApproverID"] = mstMileageClaim.FinalApprover;
                ViewBag.VoidReason = mstMileageClaim.VoidReason == null ? "" : mstMileageClaim.VoidReason;

                if (TempData["ApprovedStatus"].ToString() == "1" || TempData["ApprovedStatus"].ToString() == "2" || TempData["ApprovedStatus"].ToString() == "3" || TempData["ApprovedStatus"].ToString() == "-5" || TempData["ApprovedStatus"].ToString() == "6" || TempData["ApprovedStatus"].ToString() == "9")
                {
                    ViewBag.ShowVoidBtn = 1;

                    if (User.IsInRole("Finance"))
                    {
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
                if (mstMileageClaim.Verifier != "")
                {
                    string[] verifierIDs = mstMileageClaim.Verifier.Split(',');
                    TempData["QueryMCVerifierIDs"] = string.Join(",", verifierIDs);
                    foreach (string verifierID in verifierIDs)
                    {
                        if (verifierID != "" && verifierID == (HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value) && User.IsInRole("Finance"))
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
                    TempData["VerifierIDs"] = mstMileageClaim.Verifier;
                    TempData["ApproverIDs"] = mstMileageClaim.Approver;
                }

                //Approval Process code
                if (mstMileageClaim.Approver != "" && mstMileageClaim.Verifier == "")
                {
                    string[] approverIDs = mstMileageClaim.Approver.Split(',');
                    TempData["QueryMCApproverIDs"] = string.Join(",", approverIDs);
                    foreach (string approverID in approverIDs)
                    {
                        if (approverID != "" && approverID == (HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value) && User.IsInRole("Finance"))
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

                if (mstMileageClaim.UserApprovers != "" && mstMileageClaim.Verifier == "")
                {
                    string[] userApproverIDs = mstMileageClaim.UserApprovers.Split(',');
                    TempData["QueryMCUserApproverIDs"] = string.Join(",", userApproverIDs);
                    foreach (string approverID in userApproverIDs)
                    {
                        if (approverID != "" && approverID == (HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value))
                        {
                            TempData["ApprovedStatus"] = mstMileageClaim.ApprovalStatus;
                            //Session["ApprovedStatus"] = oSupplierPO.Approvalstatus;
                            TempData["HODApproverIDs"] = string.Join(",", userApproverIDs.Skip(1));
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

                if (mstMileageClaim.HODApprover != "" && mstMileageClaim.Verifier == "")
                {
                    string[] hodApproverIDs = mstMileageClaim.HODApprover.Split(',');
                    TempData["QueryMCHODApproverIDs"] = string.Join(",", hodApproverIDs);
                    foreach (string approverID in hodApproverIDs)
                    {
                        if (approverID != "" && approverID == (HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value))
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


                int UserId = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
                ViewBag.userID = UserId;
                //var Userlist = objERPEntities.MstUsers.ToList().Where(i => i.UserID != UserId);
                var UserIds = new List<string>();
                //var Userlist1 = _context.users.ToList().Where(i => i.UserID != UserId);
                var Userlist = await _repository.MstUser.GetAllMCUsersForQueryAsync(UserId, UserIds);
                var Creater = TempData["CreatedBy"];
                var Verifiers = TempData["QueryMCVerifierIDs"];
                var UserApprovers = TempData["QueryMCUserApproverIDs"];
                var HODApprovers = TempData["QueryMCHODApproverIDs"];
                var Approvers = TempData["QueryMCApproverIDs"];

                string[] CreaterId = Creater.ToString().Split(',');
                string[] VerifiersId = Verifiers.ToString().Split(',');
                string[] UserApproversId = UserApprovers.ToString().Split(',');
                string[] HODApproversId = HODApprovers.ToString().Split(',');
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
                //var mstMileageClaimAudits = await _repository.MstMileageClaimAudit.GetMstMileageClaimAuditByIdAsync(MCID);
                //var AuditIDs = mstMileageClaimAudits.Select(m => m.AuditBy.ToString()).Distinct();
                //foreach (var item in AuditIDs)
                //{
                //    string d = item;
                //    UserIds.Add(d);
                //}
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

                //return View(mileageClaimDetailVM);
                return View("Details", mileageClaimDetailVM);
            }
            else
            {
                return Redirect("~/Login/Login");
            }

        }

        // Excel Downlaod
        public FileResult ExcelDownload()
        {
            /*
            DataTable dt = new DataTable("Grid");
            dt.Columns.AddRange(new DataColumn[14] { new DataColumn("Claimid"),
                                            new DataColumn("UserName"),
                                            new DataColumn("TravelMode"),
                                            new DataColumn("DateofJourney"),
                                            new DataColumn("Facility"),
                                            new DataColumn("FromFacility"),
                                            new DataColumn("ToFacility"),
                                            new DataColumn("InTime"),
                                            new DataColumn("OutTime"),
                                            new DataColumn("StartReading"),
                                            new DataColumn("EndReading"),
                                            new DataColumn("KMS"),
                                            new DataColumn("Remarks"),
                                            new DataColumn("Amount")});
            using (XLWorkbook wb = new XLWorkbook())
            {
                wb.Worksheets.Add(dt);
                using (MemoryStream stream = new MemoryStream())
                {
                    wb.SaveAs(stream);
                    return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "MileageTemplate.xlsx");
                }
            }
            */
            string id = "MileageTemplate.xlsm";

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
                if (FormFile == null)
                {
                    Content("<script language='javascript' type='text/javascript'>alert('Please select a valid file!');</script>");
                    return RedirectToAction("Index", "MileageClaim");
                }
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

                        //cmd = new SqlCommand("delete from MstMileageClaimtemp", con);
                        con.Open();
                        //cmd.ExecuteNonQuery();

                        sqlBulkCopy.DestinationTableName = "dbo.MstMileageClaimtemp";

                        sqlBulkCopy.ColumnMappings.Add("UserName", "UserName");
                        sqlBulkCopy.ColumnMappings.Add("TravelMode", "TravelMode");
                        sqlBulkCopy.ColumnMappings.Add("DateofJourney", "DateofJourney");
                        sqlBulkCopy.ColumnMappings.Add("Facility", "Facility");
                        sqlBulkCopy.ColumnMappings.Add("From", "FromFacility");
                        sqlBulkCopy.ColumnMappings.Add("To", "ToFacility");
                        sqlBulkCopy.ColumnMappings.Add("InTime", "InTime");
                        sqlBulkCopy.ColumnMappings.Add("OutTime", "OutTime");
                        sqlBulkCopy.ColumnMappings.Add("StartReading", "StartReading");
                        sqlBulkCopy.ColumnMappings.Add("EndReading", "EndReading");
                        sqlBulkCopy.ColumnMappings.Add("KMS", "KMS");
                        sqlBulkCopy.ColumnMappings.Add("Remarks", "Remarks");
                        //sqlBulkCopy.ColumnMappings.Add("Amount", "Amount");
                        sqlBulkCopy.ColumnMappings.Add("Claimid", "Claimid");
                        sqlBulkCopy.ColumnMappings.Add("Userid", "UserID");
                        sqlBulkCopy.ColumnMappings.Add("Status", "Status");
                        sqlBulkCopy.ColumnMappings.Add("Facilityid", "FacilityID");
                        sqlBulkCopy.WriteToServer(dt);
                    }
                }

                DataTable InvaildData = _repository.MstMileageClaim.InsertExcel(Convert.ToInt32((HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value)), Convert.ToInt32(HttpContext.User.FindFirst("userid").Value));

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
                            var fileResult = await UploadMCFiles(FileInput);
                        }
                        var mstMileageClaim = await _repository.MstMileageClaim.GetMileageClaimByIdAsync(cid);
                        if (mstMileageClaim.ApprovalStatus == 1)
                        {
                            string VerifierIDs = "";
                            string ApproverIDs = "";
                            string UserApproverIDs = "";
                            string HODApproverID = "";
                            try
                            {
                                //VerifierIDs = mstMileageClaim.Verifier.Split(',');
                                //VerifierIDs = string.Join(",", ExpenseverifierIDs.Skip(1));
                                string[] verifierIDs = mstMileageClaim.Verifier.Split(',');
                                ApproverIDs = mstMileageClaim.Approver;
                                HODApproverID = mstMileageClaim.HODApprover;



                                //BackgroundJob.Enqueue(() => _sendMailServices.SendEmail());
                                //Mail Code Implementation for Verifiers

                                foreach (string verifierID in verifierIDs)
                                {
                                    if (verifierID != "")
                                    {
                                        string domainUrl = HttpContext.Request.Scheme + "://" + HttpContext.Request.Host;
                                        string clickUrl = domainUrl + "/" + "FinanceMileageClaim/Details/" + mstMileageClaim.MCID;

                                        var mstSenderDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value));
                                        var senderName = mstSenderDetails.Name;
                                        var mstVerifierDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(verifierID));
                                        var toEmail = mstVerifierDetails.EmailAddress;
                                        var receiverName = mstVerifierDetails.Name;
                                        var claimNo = mstMileageClaim.MCNo;
                                        var screen = "Mileage Claim";
                                        var approvalType = "Verification Request";
                                        int userID = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
                                        var subject = "Mileage Claim for Verification " + claimNo;

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
                            string[] userApproverIDs = mstMileageClaim.UserApprovers.ToString().Split(',');
                            foreach (string userApproverID in userApproverIDs)
                            {
                                if (userApproverID != "")
                                {
                                    string domainUrl = HttpContext.Request.Scheme + "://" + HttpContext.Request.Host;
                                    string clickUrl = domainUrl + "/" + "HodSummary/Details/" + mstMileageClaim.MCID;

                                    var mstSenderDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value));
                                    var senderName = mstSenderDetails.Name;
                                    var mstVerifierDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(userApproverID));
                                    var toEmail = mstVerifierDetails.EmailAddress;
                                    var receiverName = mstVerifierDetails.Name;
                                    var claimNo = mstMileageClaim.MCNo;
                                    var screen = "Mileage Claim";
                                    var approvalType = "Approval Request";
                                    int userID = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
                                    var subject = "Mileage Claim for Approval " + claimNo;

                                    BackgroundJob.Enqueue(() => _sendMailServices.SendEmail("EmailTemplate.html",screen, subject, senderName, receiverName, claimNo, approvalType, userID, toEmail, clickUrl));
                                }
                                break;
                            }
                        }
                    }
                    if (count == 0)
                    {
                        Content("<script language='javascript' type='text/javascript'>alert('File Uploaded Sucessfully!');</script>");
                        return RedirectToAction("Index", "MileageClaim");

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
                                return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "MileageTemplateValidate.xlsx");


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


            return RedirectToAction("Index", "MileageClaim");

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

                if (columnName == "DateofJourney")
                {
                    DataColumn colDateTime = new DataColumn("DateofJourney");
                    colDateTime.DataType = System.Type.GetType("System.DateTime");
                    dt.Columns.Add(colDateTime);
                }
                else if (columnName == "InTime")
                {
                    DataColumn colDateTime = new DataColumn("InTime");
                    colDateTime.DataType = System.Type.GetType("System.DateTime");
                    dt.Columns.Add(colDateTime);
                }
                else if (columnName == "OutTime")
                {
                    DataColumn colDateTime = new DataColumn("OutTime");
                    colDateTime.DataType = System.Type.GetType("System.DateTime");
                    dt.Columns.Add(colDateTime);
                }
                else
                {
                    //add the column to the datatable
                    dt.Columns.Add(columnName);
                }

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
                DateTime dtEnteredDate = DateTime.Today;
                //loop all cells in the row
                foreach (var cell in row)
                {
                    if (cell.Address.Contains("C"))
                    {
                        if (cell.Text != string.Empty)
                        {
                            newRow[cell.Start.Column - 1] = DateTime.Parse(cell.Text, new System.Globalization.CultureInfo("pt-BR"));
                            dtEnteredDate = DateTime.Parse(cell.Text, new System.Globalization.CultureInfo("pt-BR"));
                        }

                    }
                    else if (cell.Address.Contains("G") || cell.Address.Contains("H"))
                    {
                        if (cell.Text != string.Empty)
                        {
                            TimeSpan time = DateTime.Parse(cell.Text).TimeOfDay;
                            newRow[cell.Start.Column - 1] = dtEnteredDate + time;
                        }
                    }
                    else if (cell.Address.Contains("M"))
                    {
                        newRow[cell.Start.Column - 1] = cell.Value;
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

        private string GetValue(SpreadsheetDocument doc, Cell cell)
        {
            string value = cell.CellValue != null ? cell.CellValue.InnerText : "";
            if (cell.DataType != null && cell.DataType.Value == CellValues.SharedString)
            {
                return doc.WorkbookPart.SharedStringTablePart.SharedStringTable.ChildElements.GetItem(int.Parse(value)).InnerText;
            }
            else if (cell.CellReference.Value.StartsWith("D"))
            {
                return DateTime.FromOADate(double.Parse(value)).ToShortDateString();
            }
            else if (cell.CellReference.Value.StartsWith("G") || cell.CellReference.Value.StartsWith("H"))
            {
                return DateTime.FromOADate(double.Parse(value)).ToString("M/d/yyyy H:mm", CultureInfo.InvariantCulture);
            }

            return value;
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

                int loggedInUserId = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
                bool isAlternateApprover = false;
                var delegatedUserId = await _alternateApproverHelper.IsUserHasAnyAlternateApprovalSet(loggedInUserId);
                if (delegatedUserId.HasValue)
                {
                    isAlternateApprover = true;
                }

                if (Convert.ToInt32(approvedStatus) == 3 || Convert.ToInt32(approvedStatus) == 9 || Convert.ToInt32(approvedStatus) == 10)
                {
                    await _repository.MstMileageClaim.UpdateMstMileageClaimStatus(MCID, -5, int.Parse(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value), DateTime.Now, reason, string.Empty, string.Empty, string.Empty, string.Empty, isAlternateApprover, 0);
                }
                else
                {
                    await _repository.MstMileageClaim.UpdateMstMileageClaimStatus(MCID, 5, int.Parse(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value), DateTime.Now, reason, string.Empty, string.Empty, string.Empty, string.Empty, isAlternateApprover, 0);
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

                int loggedInUserId = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
                bool isAlternateApprover = false;
                var delegatedUserId = await _alternateApproverHelper.IsUserHasAnyAlternateApprovalSet(loggedInUserId);
                if (delegatedUserId.HasValue)
                {
                    isAlternateApprover = true;
                }

                await _repository.MstMileageClaim.UpdateMstMileageClaimStatus(MCID, 4, int.Parse(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value), DateTime.Now, reason, string.Empty, string.Empty, string.Empty, string.Empty, isAlternateApprover, 0);

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

        public async Task<JsonResult> UploadMCFiles(List<IFormFile> files)
        {
            var path = "FileUploads/MileageClaimFiles/";
            //var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "FileUploads", "MileageClaimFiles");

            //if (!Directory.Exists(path))
            //{
            //    Directory.CreateDirectory(path);
            //}

            // var id1 = Request.Form["Id"];
            //var id = Request.Form["Id"].ToString();

            string claimsCondition = Request.Form["claimAddCondition"];
            int MCID = Convert.ToInt32(Request.Form["Id"]);
            int mcIDValue = Convert.ToInt32(Request.Form["mcIDValue"]);

            if (MCID == 0)
            {
                if (TempData.ContainsKey("CID"))
                    MCID = Convert.ToInt32(TempData["CID"].ToString());
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
                    string pathToFiles = Regex.Replace(result, @"[^0-9a-zA-Z]+", "_") + "-" + MCID.ToString() + "-" + DateTime.Now.ToString("ddMMyyyyss") + ext;

                    DtMileageClaimFileUpload dtMileageClaimFileUpload = new DtMileageClaimFileUpload();
                    dtMileageClaimFileUpload.MCID = MCID;
                    dtMileageClaimFileUpload.FileName = fileName;
                    dtMileageClaimFileUpload.FilePath = pathToFiles;
                    dtMileageClaimFileUpload.CreatedDate = DateTime.Now;
                    dtMileageClaimFileUpload.ModifiedDate = DateTime.Now;
                    dtMileageClaimFileUpload.CreatedBy = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
                    dtMileageClaimFileUpload.ModifiedBy = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
                    dtMileageClaimFileUpload.IsDeleted = false;
                    _repository.DtMileageClaimFileUpload.Create(dtMileageClaimFileUpload);
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

            // Check if any previous files and move them from draft and save
            long idd = Convert.ToInt64(mcIDValue);

            var dtFiles = await _repository.DtMileageClaimFileUploadDraft.GetDtMileageClaimDraftAuditByIdAsync(idd);
            if (dtFiles != null)
            {
                foreach (var dtFile in dtFiles)
                {
                    DtMileageClaimFileUpload dtMileageClaimFileUpload = new DtMileageClaimFileUpload()
                    {
                        CreatedBy = dtFile.CreatedBy,
                        CreatedDate = dtFile.CreatedDate,
                        FileID = 0,
                        FileName = dtFile.FileName,
                        FilePath = dtFile.FilePath,
                        IsDeleted = dtFile.IsDeleted,
                        ModifiedBy = dtFile.ModifiedBy,
                        ModifiedDate = dtFile.ModifiedDate,
                        MCID = MCID
                    };
                    try
                    {
                        _repository.DtMileageClaimFileUpload.Create(dtMileageClaimFileUpload);
                        await _repository.SaveAsync();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Something went wrong inside DeleteMileageDraft action: {ex.Message}");
                    }

                }
            }

            if (claimsCondition == "claimDraft")
            {
                // Delete the draft claim
                try
                {

                    var mileageClaimsDraft = await _repository.MstMileageClaimDraft.GetMileageClaimDraftByIdAsync(idd);
                    if (mileageClaimsDraft != null)
                    {
                        _repository.MstMileageClaimDraft.DeleteMileageClaimDraft(mileageClaimsDraft);
                        await _repository.SaveAsync();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Something went wrong inside DeleteMileageDraft action: {ex.Message}");
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
        public async Task<JsonResult> UploadMCFilesDraft(List<IFormFile> files)
        {
            var path = "FileUploads/MileageClaimFiles/";

            // var id1 = Request.Form["Id"];
            //var id = Request.Form["Id"].ToString();

            foreach (IFormFile formFile in files)
            {
                int MCID = Convert.ToInt32(Request.Form["Id"]);
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
                    string pathToFiles = Regex.Replace(result, @"[^0-9a-zA-Z]+", "_") + "-" + MCID.ToString() + "-" + DateTime.Now.ToString("ddMMyyyyss") + ext;

                    DtMileageClaimFileUploadDraft dtMileageClaimFileUpload = new DtMileageClaimFileUploadDraft();
                    dtMileageClaimFileUpload.MCID = MCID;
                    dtMileageClaimFileUpload.FileName = fileName;
                    dtMileageClaimFileUpload.FilePath = pathToFiles;
                    dtMileageClaimFileUpload.CreatedDate = DateTime.Now;
                    dtMileageClaimFileUpload.ModifiedDate = DateTime.Now;
                    dtMileageClaimFileUpload.CreatedBy = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
                    dtMileageClaimFileUpload.ModifiedBy = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
                    dtMileageClaimFileUpload.IsDeleted = false;
                    _repository.DtMileageClaimFileUploadDraft.Create(dtMileageClaimFileUpload);
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
                    int UserID = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
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
                        var delegatedUserName = string.Empty;
                        if (HttpContext.User.FindFirst("delegateuserid") is not null)
                        {
                            var delUserDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid").Value));
                            delegatedUserName = delUserDetails.Name;
                        }
                        auditUpdate.Description = "" + (string.IsNullOrEmpty(delegatedUserName) ? User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.GivenName).Value : delegatedUserName) + " Sent Query to " + receiver.Name + " on " + formattedDate + " " + time + " ";
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

                        //var mstSenderDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value));
                        var senderName = (string.IsNullOrEmpty(delegatedUserName) ? User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.GivenName).Value : delegatedUserName);
                        //var mstVerifierDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(approverID));
                        var toEmail = receiver.EmailAddress;
                        var receiverName = receiver.Name;
                        var claimNo = mileageClaim.MCNo;
                        var screen = "Mileage Claim";
                        var approvalType = "Query";
                        int userID = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
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
                int UserId = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
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
