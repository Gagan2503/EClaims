using AutoMapper;
using ClosedXML.Excel;
using EClaimsEntities;
using EClaimsEntities.Models;
using EClaimsRepository.Contracts;
using EClaimsWeb.Helpers;
using EClaimsWeb.Models;
using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using NToastNotify;
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
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace EClaimsWeb.Controllers
{
    [Authorize(Roles = "Admin,Finance,User,HR")]
    public class PVGIROClaimController : Controller
    {
        private ILoggerManager _logger;
        private IRepositoryWrapper _repository;
        private IMapper _mapper;
        private IConfiguration _configuration;
        private AlternateApproverHelper _alternateApproverHelper;
        private ISendMailServices _sendMailServices;
        private readonly RepositoryContext _context;
        private readonly IToastNotification _toastNotification;

        public PVGIROClaimController(IToastNotification toastNotification, ILoggerManager logger, IRepositoryWrapper repository, IMapper mapper, RepositoryContext context, IConfiguration configuration, ISendMailServices sendMailServices)
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

                //var mstPVGClaimsWithDetails = await _repository.MstPVGClaim.GetAllPVGClaimWithDetailsByFacilityIDAsync(Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value), Convert.ToInt32(HttpContext.User.FindFirst("delegatefacilityid") is null ? HttpContext.User.FindFirst("facilityid").Value : HttpContext.User.FindFirst("delegatefacilityid").Value));
                var mstPVGClaimsWithDetails = await _repository.MstPVGClaim.GetAllPVGClaimWithDetailsAsync(Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value), 0, 0, "", "");
                // List<PVGClaimVM> pVGClaimVMs = new List<PVGClaimVM>();
                PVGClaimsVM pVGClaimVMs = new PVGClaimsVM();
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

                    pVGClaimVM.AVerifier = mc.Verifier;
                    pVGClaimVM.AApprover = mc.Approver;
                    pVGClaimVM.AUserApprovers = mc.UserApprovers;
                    pVGClaimVM.AHODApprover = mc.HODApprover;

                    pVGClaimVM.DVerifier = mc.DVerifier;
                    pVGClaimVM.DApprover = mc.DApprover;
                    pVGClaimVM.DUserApprovers = mc.DUserApprovers;
                    pVGClaimVM.DHODApprover = mc.DHODApprover;


                    if (mc.UserApprovers != "")
                    {
                        pVGClaimVM.Approver = mc.UserApprovers.Split(',').First();
                    }
                    else if (mc.HODApprover != "")
                    {
                        pVGClaimVM.Approver = mc.HODApprover.Split(',').First();
                    }
                    else if (mc.Verifier != "")
                    {
                        pVGClaimVM.Approver = mc.Verifier.Split(',').First();
                        //string VerifierIDs = string.Join(",", PVGverifierIDs.Skip(1));
                    }
                    else if (mc.Approver != "")
                    {
                        pVGClaimVM.Approver = mc.Approver.Split(',').First();
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

                    pVGClaimVMs.pvgClaims.Add(pVGClaimVM);
                    _logger.LogInfo($"Returned all PV Cheque Claims with details from database.");
                }
                //var mstExpenseCategoriesWithTypesResult = _mapper.Map<IEnumerable<MstExpenseCategory>>(mstExpenseCategoriesWithTypes);
                var mstPVCClaimsDraftWithDetails = await _repository.MstPVGClaimDraft.GetAllPVGClaimDraftWithDetailsByFacilityIDAsync(Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value), Convert.ToInt32(HttpContext.User.FindFirst("delegatefacilityid") is null ? HttpContext.User.FindFirst("facilityid").Value : HttpContext.User.FindFirst("delegatefacilityid").Value));

                // List<PVCClaimVM> pVCClaimVMs = new List<PVCClaimVM>();
                foreach (var mc in mstPVCClaimsDraftWithDetails)
                {
                    PVGClaimVM pVGClaimDraftVM = new PVGClaimVM();
                    pVGClaimDraftVM.PVGCID = mc.PVGCID;
                    pVGClaimDraftVM.PVGCNo = mc.PVGCNo;
                    pVGClaimDraftVM.Name = mc.MstUser.Name;
                    pVGClaimDraftVM.CreatedDate = Convert.ToDateTime(mc.CreatedDate).ToString("dd/MM/yyyy", System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
                    pVGClaimDraftVM.FacilityName = mc.MstFacility.FacilityName;
                    pVGClaimDraftVM.Phone = mc.MstUser.Phone;
                    pVGClaimDraftVM.GrandTotal = mc.GrandTotal;
                    pVGClaimDraftVM.ApprovalStatus = mc.ApprovalStatus;
                    pVGClaimDraftVM.TotalAmount = mc.TotalAmount;


                    if (mc.UserApprovers != "")
                    {
                        pVGClaimDraftVM.Approver = mc.UserApprovers.Split(',').First();
                    }
                    else if (mc.HODApprover != "")
                    {
                        pVGClaimDraftVM.Approver = mc.HODApprover.Split(',').First();
                    }
                    else if (mc.Verifier != "")
                    {
                        pVGClaimDraftVM.Approver = mc.Verifier.Split(',').First();
                        //string VerifierIDs = string.Join(",", PVGverifierIDs.Skip(1));
                    }
                    else if (mc.Approver != "")
                    {
                        pVGClaimDraftVM.Approver = mc.Approver.Split(',').First();
                    }
                    else
                    {
                        pVGClaimDraftVM.Approver = "";
                    }

                    if (pVGClaimDraftVM.Approver != "")
                    {
                        var alternateUser = await _alternateApproverHelper.IsAlternateApprovalSetForUser(Convert.ToInt32(pVGClaimDraftVM.Approver));
                        if (alternateUser.HasValue)
                        {
                            var mstUserApprover = await _repository.MstUser.GetUserByIdAsync(alternateUser.Value);
                            pVGClaimDraftVM.Approver = mstUserApprover.Name + " (AA)";
                        }
                        else
                        {
                            var mstUserApprover = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(pVGClaimDraftVM.Approver));
                            pVGClaimDraftVM.Approver = mstUserApprover.Name;
                        }
                    }
                    pVGClaimVMs.pvgClaimsDrafts.Add(pVGClaimDraftVM);
                    //  pVCClaimVMs.Add(pVCClaimVM);
                    _logger.LogInfo($"Returned all PV Cheque GIRO with details from database.");
                }
                return View(pVGClaimVMs);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Something went wrong inside GetAllPVGClaimWithDetailsAsync action: {ex.Message}");
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
        public async Task<ActionResult> DeletePVGClaimDraftFile(string fileID, string filepath, string PVGCID)
        {
            DtPVGClaimFileUploadDraft dtPVGClaimFileUpload = new DtPVGClaimFileUploadDraft();

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
                        dtPVGClaimFileUpload = await _repository.DtPVGClaimFileUploadDraft.GetDtPVGClaimFileUploadDraftByIdAsync(Convert.ToInt64(fileID));
                        _repository.DtPVGClaimFileUploadDraft.DeleteDtPVGClaimFileUploadDraft(dtPVGClaimFileUpload);
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
 
            return RedirectToAction("CreateDraft", "PVGiroClaim", new
            {
                id = PVGCID,
                Updatestatus = "Edit"
            });
        }
        public async Task<IActionResult> CreateDraft(string id, string Updatestatus)
        {
            //TempData["CBRID"] = 0;
            TempData["Updatestatus"] = "Add";
            TempData["claimaddcondition"] = "claimDraft";
            PVGClaimDetailVM pVGClaimDetailVM = new PVGClaimDetailVM();
            pVGClaimDetailVM.DtPVGClaimVMs = new List<DtPVGClaimVM>();
            pVGClaimDetailVM.PVGClaimAudits = new List<PVGClaimAuditVM>();

            if (User != null && User.Identity.IsAuthenticated)
            {
                if (!string.IsNullOrEmpty(id))
                {
                    long idd = Convert.ToInt64(id);
                    ViewBag.CID = idd;
                    var dtPVGClaims = await _repository.DtPVGClaimDraft.GetDtPVGClaimDraftByIdAsync(idd);

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
                        dtPVGClaimVM.Date = item.Date;
                        dtPVGClaimVM.Bank = item.Bank;
                        dtPVGClaimVM.BankCode = item.BankCode;
                        dtPVGClaimVM.BranchCode = item.BranchCode;
                        dtPVGClaimVM.BankAccount = item.BankAccount;
                        dtPVGClaimVM.Mobile = item.Mobile;
                        pVGClaimDetailVM.DtPVGClaimVMs.Add(dtPVGClaimVM);
                    }
                    var ecFuploads = await _repository.DtPVGClaimFileUploadDraft.GetDtPVGClaimDraftAuditByIdAsync(idd);
                    pVGClaimDetailVM.PVGClaimFileUploads = new List<DtPVGClaimFileUpload>();

                    foreach (var item in ecFuploads)
                    {
                        MstPVGClaim mstPVGClaim1 = new MstPVGClaim();
                        if (item.MstPVGClaimDraft != null)
                        {
                            mstPVGClaim1 = new MstPVGClaim()
                            {
                                ApprovalBy = item.MstPVGClaimDraft.ApprovalBy,
                                ApprovalDate = item.MstPVGClaimDraft.ApprovalDate,
                                ApprovalStatus = item.MstPVGClaimDraft.ApprovalStatus,
                                ModifiedDate = item.MstPVGClaimDraft.ModifiedDate,
                                ModifiedBy = item.MstPVGClaimDraft.ModifiedBy,
                                Approver = item.MstPVGClaimDraft.Approver,
                                //ClaimType = item.MstExpenseClaimDraft.ClaimType,
                                Company = item.MstPVGClaimDraft.Company,
                                CreatedBy = item.MstPVGClaimDraft.CreatedBy,
                                CreatedDate = item.MstPVGClaimDraft.CreatedDate,
                                DepartmentID = item.MstPVGClaimDraft.DepartmentID,
                                PVGCID = item.MstPVGClaimDraft.PVGCID,
                                PVGCNo = item.MstPVGClaimDraft.PVGCNo,
                                FacilityID = item.MstPVGClaimDraft.FacilityID,
                                FinalApprover = item.MstPVGClaimDraft.FinalApprover,
                                GrandTotal = item.MstPVGClaimDraft.GrandTotal,
                                HODApprover = item.MstPVGClaimDraft.HODApprover,
                                MstDepartment = item.MstPVGClaimDraft.MstDepartment,
                                MstFacility = item.MstPVGClaimDraft.MstFacility,
                                MstUser = item.MstPVGClaimDraft.MstUser,
                                TnC = item.MstPVGClaimDraft.TnC,
                                TotalAmount = item.MstPVGClaimDraft.TotalAmount,
                                UserApprovers = item.MstPVGClaimDraft.UserApprovers,
                                UserID = item.MstPVGClaimDraft.UserID,
                                Verifier = item.MstPVGClaimDraft.Verifier,
                                VoidReason = item.MstPVGClaimDraft.VoidReason
                            };
                        }

                        pVGClaimDetailVM.PVGClaimFileUploads.Add(new DtPVGClaimFileUpload()
                        {
                            CreatedBy = item.CreatedBy,
                            CreatedDate = item.CreatedDate,
                            PVGCID = item.PVGCID,
                            FileID = item.FileID,
                            FileName = item.FileName,
                            FilePath = item.FilePath,
                            IsDeleted = item.IsDeleted,
                            ModifiedBy = item.ModifiedBy,
                            ModifiedDate = item.ModifiedDate,
                            MstPVGClaim = mstPVGClaim1
                        });
                    }

                    // pVGClaimDetailVM.PVGClaimFileUploads = await _repository.DtPVGClaimFileUpload.GetDtPVGClaimAuditByIdAsync(idd);

                    var mstPVGClaim = await _repository.MstPVGClaimDraft.GetPVGClaimDraftByIdAsync(idd);


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
                    TempData["claimaddcondition"] = "claimDraft";
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

            }
            return View("Create", pVGClaimDetailVM);

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
                        //return File(blobStream, file.Properties.ContentType, name);
                        return File(blobStream, file.Properties.ContentType);
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

            return RedirectToAction("Create", "PVGiroClaim", new
            {
                id = PVGCID,
                Updatestatus = "Edit"
            });
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
                    var fileUploads = await _repository.DtPVGClaimFileUpload.GetDtPVGClaimAuditByIdAsync(idd);
                    if (Updatestatus == "Recreate" && fileUploads != null && fileUploads.Count > 0)
                    {
                        foreach (var uploaddata in fileUploads)
                        {
                            uploaddata.PVGCID = 0;
                            pVGClaimDetailVM.PVGClaimFileUploads.Add(uploaddata);
                        }
                    }
                    else
                        pVGClaimDetailVM.PVGClaimFileUploads = fileUploads;

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
            }
            return View(pVGClaimDetailVM);

        }

        public async Task<bool> IsGSTRequired(string expenseCategoryID)
        {
            var mstExpenseCategory = await _repository.MstExpenseCategory.GetExpenseCategoryByIdAsync(Convert.ToInt32(expenseCategoryID));
            if (mstExpenseCategory.IsGSTRequired)
                return true;
            else
                return false;
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
        public async Task<IActionResult> Details(long? id)
        {
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

                var dtPVGSummaries = await _repository.DtPVGClaimSummary.GetDtPVGClaimSummaryByIdAsync(id);

                var dtPVGClaims = await _repository.DtPVGClaim.GetDtPVGClaimByIdAsync(id);
                PVGClaimDetailVM pVGClaimDetailVM = new PVGClaimDetailVM();
                //List<DtMileageClaimVM> dtMileageClaimVMs = new List<DtMileageClaimVM>();
                pVGClaimDetailVM.DtPVGClaimVMs = new List<DtPVGClaimVM>();
                // List<clsDtSupplierPO> oclsDtSupplierPO = new List<clsDtSupplierPO>();
                foreach (var item in dtPVGClaims)
                {
                    DtPVGClaimVM dtPVGClaimVM = new DtPVGClaimVM();
                    dtPVGClaimVM.Payee = item.Payee;
                    dtPVGClaimVM.PVGCItemID = item.PVGCItemID;
                    dtPVGClaimVM.PVGCID = item.PVGCID;
                    dtPVGClaimVM.Date = item.Date;
                    dtPVGClaimVM.Particulars = item.Particulars;
                    dtPVGClaimVM.InvoiceNo = item.InvoiceNo;

                    dtPVGClaimVM.ChequeNo = item.ChequeNo;
                    dtPVGClaimVM.Amount = item.Amount;
                    dtPVGClaimVM.GST = item.GST;
                    dtPVGClaimVM.GSTPercentage = item.GSTPercentage;
                    dtPVGClaimVM.AmountWithGST = item.Amount + item.GST;
                    dtPVGClaimVM.ExpenseCategory = item.MstExpenseCategory.Description;
                    dtPVGClaimVM.AccountCode = item.AccountCode;
                    dtPVGClaimVM.ExpenseCategoryID = item.ExpenseCategoryID;
                    dtPVGClaimVM.Bank = item.Bank;
                    dtPVGClaimVM.BankCode = item.BankCode;
                    dtPVGClaimVM.BankSWIFTBIC = item.BankSwiftBIC;
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
                var GroupByQS = pVGClaimDetailVM.DtPVGClaimVMs.GroupBy(s => s.AccountCode);
                //var GroupByQS = (from std in pVGClaimDetailVM.DtExpenseClaimVMs
                //                                                           group std by std.ExpenseCategoryID);

                //pVGClaimDetailVM.DtPVGClaimVMs = new List<DtPVGClaimVM>();

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

                PVGClaimVM PVGClaimVM = new PVGClaimVM();
                PVGClaimVM.VoucherNo = mstPVGClaim.VoucherNo;
                PVGClaimVM.GrandTotal = mstPVGClaim.GrandTotal;
                PVGClaimVM.TotalAmount = mstPVGClaim.TotalAmount;
                PVGClaimVM.GrandGST = mstPVGClaim.TotalAmount - mstPVGClaim.GrandTotal;
                PVGClaimVM.Company = mstPVGClaim.Company;
                PVGClaimVM.Name = mstPVGClaim.MstUser.Name;
                PVGClaimVM.DepartmentName = mstPVGClaim.MstDepartment.Department;
                PVGClaimVM.FacilityName = mstPVGClaim.MstFacility.FacilityName;
                PVGClaimVM.CreatedDate = mstPVGClaim.CreatedDate.ToString("d");
                PVGClaimVM.Verifier = mstPVGClaim.Verifier;
                PVGClaimVM.Approver = mstPVGClaim.Approver;
                PVGClaimVM.PVGCNo = mstPVGClaim.PVGCNo;
                PVGClaimVM.PaymentMode = mstPVGClaim.PaymentMode;
                ViewBag.PVGCID = id;
                TempData["CreatedBy"] = mstPVGClaim.CreatedBy;
                ViewBag.Approvalstatus = mstPVGClaim.ApprovalStatus;

                if (mstPVGClaim.Verifier == mstPVGClaim.DVerifier && mstPVGClaim.Approver == mstPVGClaim.DApprover && mstPVGClaim.UserApprovers == mstPVGClaim.DUserApprovers && mstPVGClaim.HODApprover == mstPVGClaim.DHODApprover)
                {
                    ViewBag.UserEditStatus = 4;
                }
                else
                {
                    ViewBag.UserEditStatus = 0;
                }

                TempData["ApprovedStatus"] = mstPVGClaim.ApprovalStatus;
                TempData["FinalApproverID"] = mstPVGClaim.FinalApprover;
                ViewBag.VoidReason = mstPVGClaim.VoidReason == null ? "" : mstPVGClaim.VoidReason;

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
                if (mstPVGClaim.Verifier != "")
                {
                    string[] verifierIDs = mstPVGClaim.Verifier.Split(',');
                    TempData["QueryMCVerifierIDs"] = string.Join(",", verifierIDs);
                    foreach (string verifierID in verifierIDs)
                    {
                        if (verifierID != "" && verifierID == (HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value) && User.IsInRole("Finance"))
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
                        if (approverID != "" && approverID == (HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value) && User.IsInRole("Finance"))
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

                if (mstPVGClaim.UserApprovers != "" && mstPVGClaim.Verifier == "")
                {
                    string[] userApproverIDs = mstPVGClaim.UserApprovers.Split(',');
                    TempData["QueryMCUserApproverIDs"] = string.Join(",", userApproverIDs);
                    foreach (string approverID in userApproverIDs)
                    {
                        if (approverID != "" && approverID == (HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value))
                        {
                            TempData["ApprovedStatus"] = mstPVGClaim.ApprovalStatus;
                            //Session["ApprovedStatus"] = oSupplierPO.Approvalstatus;
                            TempData["HODApproverIDs"] = string.Join(",", userApproverIDs.Skip(1));
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

                if (mstPVGClaim.HODApprover != "" && mstPVGClaim.Verifier == "")
                {
                    string[] hodApproverIDs = mstPVGClaim.HODApprover.Split(',');
                    TempData["QueryMCHODApproverIDs"] = string.Join(",", hodApproverIDs);
                    foreach (string approverID in hodApproverIDs)
                    {
                        if (approverID != "" && approverID == (HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value))
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


                pVGClaimDetailVM.PVGClaimVM = PVGClaimVM;
                //mileageClaimDetailVM.DtMileageClaimVMs = dtMileageClaimVMs;

                BindGSTDropdown();
                return View(pVGClaimDetailVM);
            }
            else
            {
                return Redirect("~/Login/Login");
            }
        }
        public async Task<IActionResult> DetailsDraft(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            long PVGCID = Convert.ToInt64(id);

            if (User != null && User.Identity.IsAuthenticated)
            {
                var mstPVGClaim = await _repository.MstPVGClaimDraft.GetPVGClaimDraftByIdAsync(id);

                if (mstPVGClaim == null)
                {
                    return NotFound();
                }

                var dtPVGSummaries = await _repository.DtPVGClaimSummaryDraft.GetDtPVGClaimSummaryDraftByIdAsync(id);

                var dtPVGClaims = await _repository.DtPVGClaimDraft.GetDtPVGClaimDraftByIdAsync(id);
                PVGClaimDetailVM pVGClaimDetailVM = new PVGClaimDetailVM();
                //List<DtMileageClaimVM> dtMileageClaimVMs = new List<DtMileageClaimVM>();
                pVGClaimDetailVM.DtPVGClaimVMs = new List<DtPVGClaimVM>();
                // List<clsDtSupplierPO> oclsDtSupplierPO = new List<clsDtSupplierPO>();
                foreach (var item in dtPVGClaims)
                {
                    DtPVGClaimVM dtPVGClaimVM = new DtPVGClaimVM();
                    dtPVGClaimVM.Payee = item.Payee;
                    dtPVGClaimVM.PVGCItemID = item.PVGCItemID;
                    dtPVGClaimVM.PVGCID = item.PVGCID;
                    dtPVGClaimVM.Date = item.Date;
                    dtPVGClaimVM.Particulars = item.Particulars;
                    dtPVGClaimVM.InvoiceNo = item.InvoiceNo;

                    dtPVGClaimVM.ChequeNo = item.ChequeNo;
                    dtPVGClaimVM.Amount = item.Amount;
                    dtPVGClaimVM.GST = item.GST;
                    dtPVGClaimVM.AmountWithGST = item.Amount + item.GST;
                    dtPVGClaimVM.ExpenseCategory = item.MstExpenseCategory.Description;
                    dtPVGClaimVM.AccountCode = item.AccountCode;
                    dtPVGClaimVM.ExpenseCategoryID = item.ExpenseCategoryID;
                    dtPVGClaimVM.Bank = item.Bank;
                    dtPVGClaimVM.BankCode = item.BankCode;
                    dtPVGClaimVM.BankSWIFTBIC = item.BankSwiftBIC;
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

                pVGClaimDetailVM.DtPVGClaimSummaries = new List<DtPVGClaimSummary>();
                var GroupByQS = pVGClaimDetailVM.DtPVGClaimVMs.GroupBy(s => s.AccountCode);
                //var GroupByQS = (from std in pVGClaimDetailVM.DtExpenseClaimVMs
                //                                                           group std by std.ExpenseCategoryID);

                //pVGClaimDetailVM.DtPVGClaimVMs = new List<DtPVGClaimVM>();

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

                var ecFileUploads = _repository.DtPVGClaimFileUploadDraft.GetDtPVGClaimDraftAuditByIdAsync(id).Result.ToList();
                foreach (var item in ecFileUploads)
                {
                    MstPVGClaim mstPVGClaim1 = new MstPVGClaim();
                    if (item.MstPVGClaimDraft != null)
                    {
                        mstPVGClaim1 = new MstPVGClaim()
                        {
                            ApprovalBy = item.MstPVGClaimDraft.ApprovalBy,
                            ApprovalDate = item.MstPVGClaimDraft.ApprovalDate,
                            ApprovalStatus = item.MstPVGClaimDraft.ApprovalStatus,
                            ModifiedDate = item.MstPVGClaimDraft.ModifiedDate,
                            ModifiedBy = item.MstPVGClaimDraft.ModifiedBy,
                            Approver = item.MstPVGClaimDraft.Approver,
                            // ClaimType = item.MstExpenseClaimDraft.ClaimType,
                            Company = item.MstPVGClaimDraft.Company,
                            CreatedBy = item.MstPVGClaimDraft.CreatedBy,
                            CreatedDate = item.MstPVGClaimDraft.CreatedDate,
                            DepartmentID = item.MstPVGClaimDraft.DepartmentID,
                            PVGCID = item.MstPVGClaimDraft.PVGCID,
                            PVGCNo = item.MstPVGClaimDraft.PVGCNo,
                            FacilityID = item.MstPVGClaimDraft.FacilityID,
                            FinalApprover = item.MstPVGClaimDraft.FinalApprover,
                            GrandTotal = item.MstPVGClaimDraft.GrandTotal,
                            HODApprover = item.MstPVGClaimDraft.HODApprover,
                            MstDepartment = item.MstPVGClaimDraft.MstDepartment,
                            MstFacility = item.MstPVGClaimDraft.MstFacility,
                            MstUser = item.MstPVGClaimDraft.MstUser,
                            TnC = item.MstPVGClaimDraft.TnC,
                            TotalAmount = item.MstPVGClaimDraft.TotalAmount,
                            UserApprovers = item.MstPVGClaimDraft.UserApprovers,
                            UserID = item.MstPVGClaimDraft.UserID,
                            Verifier = item.MstPVGClaimDraft.Verifier,
                            VoidReason = item.MstPVGClaimDraft.VoidReason
                        };
                    }

                    pVGClaimDetailVM.PVGClaimFileUploads.Add(new DtPVGClaimFileUpload()
                    {
                        CreatedBy = item.CreatedBy,
                        CreatedDate = item.CreatedDate,
                        PVGCID = item.PVGCID,
                        FileID = item.FileID,
                        FileName = item.FileName,
                        FilePath = item.FilePath,
                        IsDeleted = item.IsDeleted,
                        ModifiedBy = item.ModifiedBy,
                        ModifiedDate = item.ModifiedDate,
                        MstPVGClaim = mstPVGClaim1
                    });
                }
                PVGClaimVM PVGClaimVM = new PVGClaimVM();
                PVGClaimVM.VoucherNo = mstPVGClaim.VoucherNo;
                PVGClaimVM.GrandTotal = mstPVGClaim.GrandTotal;
                PVGClaimVM.TotalAmount = mstPVGClaim.TotalAmount;
                PVGClaimVM.GrandGST = mstPVGClaim.TotalAmount - mstPVGClaim.GrandTotal;
                PVGClaimVM.Company = mstPVGClaim.Company;
                PVGClaimVM.Name = mstPVGClaim.MstUser.Name;
                PVGClaimVM.DepartmentName = mstPVGClaim.MstDepartment.Department;
                PVGClaimVM.FacilityName = mstPVGClaim.MstFacility.FacilityName;
                PVGClaimVM.CreatedDate = mstPVGClaim.CreatedDate.ToString("d");
                PVGClaimVM.Verifier = mstPVGClaim.Verifier;
                PVGClaimVM.Approver = mstPVGClaim.Approver;
                PVGClaimVM.PVGCNo = mstPVGClaim.PVGCNo;
                PVGClaimVM.PaymentMode = mstPVGClaim.PaymentMode;
                ViewBag.PVGCID = id;
                TempData["CreatedBy"] = mstPVGClaim.CreatedBy;
                ViewBag.Approvalstatus = mstPVGClaim.ApprovalStatus;


                TempData["ApprovedStatus"] = mstPVGClaim.ApprovalStatus;
                TempData["FinalApproverID"] = mstPVGClaim.FinalApprover;
                ViewBag.VoidReason = mstPVGClaim.VoidReason == null ? "" : mstPVGClaim.VoidReason;

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
                if (mstPVGClaim.Verifier != "")
                {
                    string[] verifierIDs = mstPVGClaim.Verifier.Split(',');
                    TempData["QueryMCVerifierIDs"] = string.Join(",", verifierIDs);
                    foreach (string verifierID in verifierIDs)
                    {
                        if (verifierID != "" && verifierID == (HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value) && User.IsInRole("Finance"))
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
                        if (approverID != "" && approverID == (HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value) && User.IsInRole("Finance"))
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

                if (mstPVGClaim.UserApprovers != "" && mstPVGClaim.Verifier == "")
                {
                    string[] userApproverIDs = mstPVGClaim.UserApprovers.Split(',');
                    TempData["QueryMCUserApproverIDs"] = string.Join(",", userApproverIDs);
                    foreach (string approverID in userApproverIDs)
                    {
                        if (approverID != "" && approverID == (HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value))
                        {
                            TempData["ApprovedStatus"] = mstPVGClaim.ApprovalStatus;
                            //Session["ApprovedStatus"] = oSupplierPO.Approvalstatus;
                            TempData["HODApproverIDs"] = string.Join(",", userApproverIDs.Skip(1));
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

                if (mstPVGClaim.HODApprover != "" && mstPVGClaim.Verifier == "")
                {
                    string[] hodApproverIDs = mstPVGClaim.HODApprover.Split(',');
                    TempData["QueryMCHODApproverIDs"] = string.Join(",", hodApproverIDs);
                    foreach (string approverID in hodApproverIDs)
                    {
                        if (approverID != "" && approverID == (HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value))
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


                pVGClaimDetailVM.PVGClaimVM = PVGClaimVM;
                //mileageClaimDetailVM.DtMileageClaimVMs = dtMileageClaimVMs;


                return View("Details", pVGClaimDetailVM);
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

                    dtPVGClaimVM.Payee = item.Payee;
                    dtPVGClaimVM.PVGCItemID = item.PVGCItemID;
                    dtPVGClaimVM.PVGCID = item.PVGCID;
                    dtPVGClaimVM.Date = item.Date;
                    dtPVGClaimVM.Particulars = item.Particulars;
                    dtPVGClaimVM.InvoiceNo = item.InvoiceNo;

                    dtPVGClaimVM.ChequeNo = item.ChequeNo;
                    dtPVGClaimVM.Amount = item.Amount;
                    dtPVGClaimVM.GST = item.GST;
                    dtPVGClaimVM.AmountWithGST = item.Amount + item.GST;
                    dtPVGClaimVM.ExpenseCategory = item.MstExpenseCategory.Description;
                    dtPVGClaimVM.AccountCode = item.AccountCode;
                    dtPVGClaimVM.ExpenseCategoryID = item.ExpenseCategoryID;
                    dtPVGClaimVM.Bank = item.Bank;
                    dtPVGClaimVM.BankCode = item.BankCode;
                    dtPVGClaimVM.BankSWIFTBIC = item.BankSwiftBIC;
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
                //var GroupByQS = (from std in pVGClaimDetailVM.DtExpenseClaimVMs
                //                                                           group std by std.ExpenseCategoryID);


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

                int loggedInUserId = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
                bool isAlternateApprover = false;
                var delegatedUserId = await _alternateApproverHelper.IsUserHasAnyAlternateApprovalSet(loggedInUserId);
                if (delegatedUserId.HasValue)
                {
                    isAlternateApprover = true;
                }

                if (Convert.ToInt32(approvedStatus) == 3 || Convert.ToInt32(approvedStatus) == 9 || Convert.ToInt32(approvedStatus) == 10)
                {
                    await _repository.MstPVGClaim.UpdateMstPVGClaimStatus(PVGCID, -5, int.Parse(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value), DateTime.Now, reason, string.Empty, string.Empty, string.Empty, string.Empty, isAlternateApprover, 0);
                }
                else
                {
                    await _repository.MstPVGClaim.UpdateMstPVGClaimStatus(PVGCID, 5, int.Parse(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value), DateTime.Now, reason, string.Empty, string.Empty, string.Empty, string.Empty, isAlternateApprover, 0);
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
                int PVGCID = Convert.ToInt32(id);

                var mstPVGClaim = await _repository.MstPVGClaim.GetPVGClaimByIdAsync(PVGCID);

                if (mstPVGClaim == null)
                {
                    // return NotFound();
                }


                int ApprovedStatus = Convert.ToInt32(mstPVGClaim.ApprovalStatus);
                bool excute = _repository.MstPVGClaim.ExistsApproval(PVGCID.ToString(), ApprovedStatus, HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value, "PVG");

                // If execute is false, Check if the current user is alternate user for this claim
                if (excute == false)
                {
                    string usapprover = _repository.MstTBClaim.GetApproverVerifier(PVGCID.ToString(), ApprovedStatus, HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value, "TelephoneBill");
                    int loggedInUserId = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
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
                        await _repository.MstPVGClaim.UpdateMstPVGClaimStatus(PVGCID, 2, int.Parse(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value), DateTime.Now, string.Empty, VerifierIDs.ToString(), ApproverIDs.ToString(), UserApproverIDs.ToString(), HODApproverID.ToString(), isAlternateApprover, 0);

                    }
                    #endregion

                    #region PVG Approver
                    else if (ApprovedStatus == 2)
                    {
                        string VerifierIDs = "";
                        string ApproverIDs = "";
                        string UserApproverIDs = "";
                        string HODApproverID = "";
                        try
                        {
                            string[] PVGapproverIDs = mstPVGClaim.Approver.Split(',');
                            ApproverIDs = string.Join(",", PVGapproverIDs.Skip(1));
                            string[] approverIDs = ApproverIDs.Split(',');
                            int CreatedBy = Convert.ToInt32(mstPVGClaim.CreatedBy);

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
                        await _repository.MstPVGClaim.UpdateMstPVGClaimStatus(PVGCID, 3, int.Parse(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value), DateTime.Now, string.Empty, VerifierIDs, ApproverIDs, UserApproverIDs.ToString(), HODApproverID.ToString(), isAlternateApprover, int.Parse(financeStartDay));
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
                int loggedInUserId = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
                bool isAlternateApprover = false;
                var delegatedUserId = await _alternateApproverHelper.IsUserHasAnyAlternateApprovalSet(loggedInUserId);
                if (delegatedUserId.HasValue)
                {
                    isAlternateApprover = true;
                }

                await _repository.MstPVGClaim.UpdateMstPVGClaimStatus(PVGCID, 4, int.Parse(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value), DateTime.Now, reason, string.Empty, string.Empty, string.Empty, string.Empty, isAlternateApprover, 0);

                return Json(new { res = "Done" });
            }
            else
            {
                return Json(new { res = "Done" });
            }
        }
        public async Task<IActionResult> DeletePVGDraft(string id)
        {
            try
            {
                long idd = Convert.ToInt64(id);
                var pvgClaimsDraft = await _repository.MstPVGClaimDraft.GetPVGClaimDraftByIdAsync(idd);
                _repository.MstPVGClaimDraft.DeletePVGClaimDraft(pvgClaimsDraft);
                await _repository.SaveAsync();
                TempData["Message"] = "Draft deleted successfully";
                Content("<script language='javascript' type='text/javascript'>alert('Draft deleted successfully');</script>");
                return RedirectToAction("Index", "PVGIROClaim");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Something went wrong inside DeletePVGDraft action: {ex.Message}");
            }
            return Json(null);
        }
        public FileResult ExcelDownload()
        {
            /*
            DataTable dt = new DataTable("Grid");
            dt.Columns.AddRange(new DataColumn[15] {new DataColumn("Claimid"),
                                            new DataColumn("Username"),
                                            new DataColumn("Payment Mode"),
                                            new DataColumn("Payee Name"),
                                            new DataColumn("Particulars of payment"),
                                            new DataColumn("Facility"),
                                            new DataColumn("Invoice No"),
                                            new DataColumn("Mobile/UEN No"),
                                            new DataColumn("Bank Name"),
                                            new DataColumn("Bank Code"),
                                            new DataColumn("Branch Code"),
                                            new DataColumn("Bank Account"),
                                            new DataColumn("Amount"),
                                            new DataColumn("GST"),
                                            new DataColumn("Expense Category")});
            using (XLWorkbook wb = new XLWorkbook())
            {
                wb.Worksheets.Add(dt);
                using (MemoryStream stream = new MemoryStream())
                {
                    wb.SaveAs(stream);
                    return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "PVGiroTemplate.xlsx");
                }
            }
            */
            string id = "PVGiroTemplate.xlsm";

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

                        //cmd = new SqlCommand("delete from MstPVGClaimtemp", con);
                        con.Open();
                        //cmd.ExecuteNonQuery();

                        sqlBulkCopy.DestinationTableName = "dbo.MstPVGClaimtemp";

                        sqlBulkCopy.ColumnMappings.Add("UserName", "UserName");
                        sqlBulkCopy.ColumnMappings.Add("Payment Mode", "PaymentMode");
                        sqlBulkCopy.ColumnMappings.Add("Particulars Of payment", "Particulars");
                        sqlBulkCopy.ColumnMappings.Add("Payee Name", "Payee");
                        sqlBulkCopy.ColumnMappings.Add("Facility", "Facility");
                        sqlBulkCopy.ColumnMappings.Add("Invoice No", "InvoiceNo");
                        sqlBulkCopy.ColumnMappings.Add("Amount", "Amount");
                        sqlBulkCopy.ColumnMappings.Add("GST", "GST");
                        sqlBulkCopy.ColumnMappings.Add("GSTPercentage", "GSTPercentage");
                        sqlBulkCopy.ColumnMappings.Add("Mobile/UEN No", "MobileNo");
                        sqlBulkCopy.ColumnMappings.Add("Bank Name", "BankName");
                        sqlBulkCopy.ColumnMappings.Add("Bank Code", "BankCode");
                        sqlBulkCopy.ColumnMappings.Add("Branch Code", "BranchCode");
                        sqlBulkCopy.ColumnMappings.Add("Bank Account", "BankAccount");
                        sqlBulkCopy.ColumnMappings.Add("Claimid", "Claimid");
                        sqlBulkCopy.ColumnMappings.Add("Expense Category", "DescriptionofExpenseCatergory");
                        sqlBulkCopy.ColumnMappings.Add("Userid", "Userid");
                        sqlBulkCopy.ColumnMappings.Add("Facilityid", "FacilityID");
                        sqlBulkCopy.ColumnMappings.Add("Status", "Status");
                        sqlBulkCopy.WriteToServer(dt);
                    }
                }

                DataTable InvaildData = _repository.MstPVGClaim.InsertExcel(Convert.ToInt32((HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value)), Convert.ToInt32(HttpContext.User.FindFirst("userid").Value));

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
                        var mstPVGClaim = await _repository.MstPVGClaim.GetPVGClaimByIdAsync(cid);
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
                    }
                    if (count == 0)
                    {
                        Content("<script language='javascript' type='text/javascript'>alert('File has imported.Please check the downloaded file.');</script>");
                        _toastNotification.AddSuccessToastMessage($"Import process completed. Please check the downloaded file to verify if the data has been successfully imported");
                        return RedirectToAction("Index", "PVGIROClaim", "File has imported.Please check the downloaded file.");

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
                                return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "PVGiroTemplateValidate.xlsx");


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


            return RedirectToAction("Index", "PVGIROClaim");

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
                    if (cell.Address.Contains("M") || cell.Address.Contains("N") || cell.Address.Contains("O"))
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

        public async Task<string> GetBankSwiftBIC(long bankCode)
        {
            var mstBankSwiftBIC = await _repository.MstBankSwiftBIC.GetBankSwiftBICByBankCodeAsync(bankCode);
            if (mstBankSwiftBIC != null)
                return mstBankSwiftBIC.BankSwiftBIC;
            else
                return string.Empty;
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
            mstPVGClaim.CreatedBy = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value); // Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
            mstPVGClaim.ModifiedBy = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value); // Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
            mstPVGClaim.ApprovalDate = DateTime.Now;
            mstPVGClaim.ApprovalBy = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
            mstPVGClaim.DelegatedBy = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? 0 : HttpContext.User.FindFirst("delegateuserid").Value);
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

                if (pVGClaimViewModel.PaymentMode != "PayNow")
                {
                    var mstBankSwiftBIC = await _repository.MstBankSwiftBIC.GetBankSwiftBICByBankCodeAsync(Convert.ToInt64(dtItem.BankCode));
                    dtItem.Bank = mstBankSwiftBIC.BankName;
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
                                    int? approverId = await _alternateApproverHelper.IsAlternateApprovalSetForUser(Convert.ToInt32(hODApproverID));
                                    var mstVerifierDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(hODApproverID));
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

        [HttpPost]
        public async Task<JsonResult> SaveDraftItems(string data)
        {
            //var pVGClaimViewModel = JsonConvert.DeserializeObject<PVGClaimDraftViewModel>(data,
            //    new IsoDateTimeConverter { DateTimeFormat = "dd/MM/yyyy" });

            var pVGClaimViewModel = JsonConvert.DeserializeObject<PVGClaimDraftViewModel>(data);

            var mstFacility = await _repository.MstFacility.GetFacilityWithDepartmentByIdAsync(Convert.ToInt32(HttpContext.User.FindFirst("delegatefacilityid") is null ? HttpContext.User.FindFirst("facilityid").Value : HttpContext.User.FindFirst("delegatefacilityid").Value));



            MstPVGClaimDraft mstPVGClaim = new MstPVGClaimDraft();
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
                    dtItem.AccountCode = mstExpenseCategory.ExpenseCode + "-" + mstFacility1.MstDepartment.Code + "-" + mstFacility1.Code + mstExpenseCategory.Default;
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

                if (PVGCID == 0 || TempData["Updatestatus"].ToString() == "Recreate")
                {
                    dtPVGClaimVM.PVGCID = 0;
                    dtPVGClaimVM.PVGCItemID = 0;
                }
                dtPVGClaimVM.Payee = item.Payee;
                dtPVGClaimVM.Particulars = item.Particulars;
                dtPVGClaimVM.ExpenseCategory = item.MstExpenseCategory.Description;
                dtPVGClaimVM.ExpenseCategoryID = item.MstExpenseCategory.ExpenseCategoryID;
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
                dtPVGClaimVM.InvoiceNo = item.InvoiceNo;
                dtPVGClaimVM.OrderBy = item.OrderBy;
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
                string ExpenseCat = string.Empty;
                string AccountCode = string.Empty;
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
                    AccountCode = dtExpense.AccountCode;
                }
                //gst = gst / group.Count();
                dtPVGClaimVM.Particulars = ExpenseDesc;
                dtPVGClaimVM.ExpenseCategory = ExpenseCat;
                dtPVGClaimVM.AccountCode = AccountCode;
                dtPVGClaimVM.Amount = amount;
                dtPVGClaimVM.GST = gst;
                dtPVGClaimVM.GSTPercentage = gstpercentage;
                dtPVGClaimVM.AmountWithGST = sumamount;
                pVGClaimDetailVM.DtPVGClaimVMSummary.Add(dtPVGClaimVM);
            }
            List<DtPVGClaimSummaryDraft> lstPVGClaimSummary = new List<DtPVGClaimSummaryDraft>();
            foreach (var item in pVGClaimDetailVM.DtPVGClaimVMSummary)
            {
                DtPVGClaimSummaryDraft dtPVGClaimSummary1 = new DtPVGClaimSummaryDraft();
                dtPVGClaimSummary1.AccountCode = item.AccountCode;
                dtPVGClaimSummary1.Amount = item.Amount;
                dtPVGClaimSummary1.ExpenseCategory = item.ExpenseCategory;
                dtPVGClaimSummary1.Description = item.Particulars.ToUpper();
                dtPVGClaimSummary1.GST = item.GST;
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

            DtPVGClaimSummaryDraft dtPVGClaimSummary = new DtPVGClaimSummaryDraft();
            dtPVGClaimSummary.AccountCode = "425000";
            dtPVGClaimSummary.Amount = mstPVGClaim.GrandTotal;
            dtPVGClaimSummary.GST = mstPVGClaim.TotalAmount - mstPVGClaim.GrandTotal;
            dtPVGClaimSummary.AmountWithGST = mstPVGClaim.TotalAmount;
            dtPVGClaimSummary.TaxClass = 0;
            dtPVGClaimSummary.ExpenseCategory = "DBS";
            dtPVGClaimSummary.Description = "";
            lstPVGClaimSummary.Add(dtPVGClaimSummary);

            var res = await _repository.MstPVGClaimDraft.SaveItemsDraft(mstPVGClaim, pVGClaimViewModel.dtClaims, lstPVGClaimSummary);

            if (res != 0)
            {
                if (ClaimStatus == "Add" || ClaimStatus == "Recreate")
                    TempData["Message"] = "PV-GIRO Claim Draft added successfully";
                else
                    TempData["Message"] = "PV-GIRO Claim Draft updated successfully";

                return Json(new { res });
            }
            else
                return Json(new { res });

        }
        public async Task<JsonResult> UploadECFilesDraft(List<IFormFile> files)
        {
            var path = "FileUploads/PVGClaimFiles/";

            int PVGCID = Convert.ToInt32(Request.Form["Id"]);
            if (PVGCID == 0)
            {
                if (TempData.ContainsKey("CID"))
                    PVGCID = Convert.ToInt32(TempData["CID"].ToString());
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
                    string pathToFiles = Regex.Replace(result, @"[^0-9a-zA-Z]+", "_") + "-" + PVGCID.ToString() + "-" + DateTime.Now.ToString("ddMMyyyyss") + ext;

                    DtPVGClaimFileUploadDraft dtPVGClaimFileUpload = new DtPVGClaimFileUploadDraft();
                    dtPVGClaimFileUpload.PVGCID = PVGCID;
                    dtPVGClaimFileUpload.FileName = fileName;
                    dtPVGClaimFileUpload.FilePath = pathToFiles;
                    dtPVGClaimFileUpload.CreatedDate = DateTime.Now;
                    dtPVGClaimFileUpload.ModifiedDate = DateTime.Now;
                    dtPVGClaimFileUpload.CreatedBy = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
                    dtPVGClaimFileUpload.ModifiedBy = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
                    dtPVGClaimFileUpload.IsDeleted = false;
                    _repository.DtPVGClaimFileUploadDraft.CreateDtPVGClaimFileUploadDraft(dtPVGClaimFileUpload);
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

        public async Task<JsonResult> UploadECFiles(List<IFormFile> files)
        {
            var path = "FileUploads/PVGClaimFiles/";
            //var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "FileUploads", "PVGClaimFiles");

            //if (!Directory.Exists(path))
            //{
            //    Directory.CreateDirectory(path);
            //}
            string claimsCondition = Request.Form["claimAddCondition"];
            int ecIDValue = Convert.ToInt32(Request.Form["ecIDValue"]);
            int PVGCID = Convert.ToInt32(Request.Form["Id"]);
            if (PVGCID == 0)
            {
                if (TempData.ContainsKey("CID"))
                    PVGCID = Convert.ToInt32(TempData["CID"].ToString());
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
                    string pathToFiles = Regex.Replace(result, @"[^0-9a-zA-Z]+", "_") + "-" + PVGCID.ToString() + "-" + DateTime.Now.ToString("ddMMyyyyss") + ext;

                    DtPVGClaimFileUpload dtPVGClaimFileUpload = new DtPVGClaimFileUpload();
                    dtPVGClaimFileUpload.PVGCID = PVGCID;
                    dtPVGClaimFileUpload.FileName = fileName;
                    dtPVGClaimFileUpload.FilePath = pathToFiles;
                    dtPVGClaimFileUpload.CreatedDate = DateTime.Now;
                    dtPVGClaimFileUpload.ModifiedDate = DateTime.Now;
                    dtPVGClaimFileUpload.CreatedBy = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
                    dtPVGClaimFileUpload.ModifiedBy = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
                    dtPVGClaimFileUpload.IsDeleted = false;
                    _repository.DtPVGClaimFileUpload.CreateDtPVGClaimFileUpload(dtPVGClaimFileUpload);
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
            var dtFiles = await _repository.DtPVGClaimFileUploadDraft.GetDtPVGClaimDraftAuditByIdAsync(idd);
            if (dtFiles != null)
            {
                foreach (var dtFile in dtFiles)
                {
                    DtPVGClaimFileUpload dtPVGClaimFileUpload = new DtPVGClaimFileUpload()
                    {
                        CreatedBy = dtFile.CreatedBy,
                        CreatedDate = dtFile.CreatedDate,
                        FileID = 0,
                        FileName = dtFile.FileName,
                        FilePath = dtFile.FilePath,
                        IsDeleted = dtFile.IsDeleted,
                        ModifiedBy = dtFile.ModifiedBy,
                        ModifiedDate = dtFile.ModifiedDate,
                        PVGCID = PVGCID

                    };
                    try
                    {
                        _repository.DtPVGClaimFileUpload.Create(dtPVGClaimFileUpload);
                        await _repository.SaveAsync();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Something went wrong inside DeleteExpenseDraft action: {ex.Message}");
                    }
                }
            }
            if (claimsCondition == "claimDraft")
            {
                // Delete the draft claim
                try
                {
                    var expenseClaimsDraft = await _repository.MstPVGClaimDraft.GetPVGClaimDraftByIdAsync(idd);
                    if (expenseClaimsDraft != null)
                    {
                        _repository.MstPVGClaimDraft.DeletePVGClaimDraft(expenseClaimsDraft);
                        await _repository.SaveAsync();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Something went wrong while deleting expense claim draft after submit. error: {ex.Message}");
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
                    //var PGtoEmail = ConfigurationManager.AppSettings["ProcurementEmail"].ToString();
                    long PVGCID = Convert.ToInt64(queryParamViewModel.Cid);
                    int UserID = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
                    // newly Added Code
                    var pVGClaim = await _repository.MstPVGClaim.GetPVGClaimByIdAsync(PVGCID);
                    //var SupplierPO = objERPEntities.MstSupplierPOes.ToList().Where(p => p.SPOID == SPOID && p.InstanceID == int.Parse(Session["InstanceID"].ToString())).ToList().FirstOrDefault();
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
                        var delegatedUserName = string.Empty;
                        if (HttpContext.User.FindFirst("delegateuserid") is not null)
                        {
                            var delUserDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid").Value));
                            delegatedUserName = delUserDetails.Name;
                        }

                        auditUpdate.Description = "" + (string.IsNullOrEmpty(delegatedUserName) ? User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.GivenName).Value : delegatedUserName) + " Sent Query to " + receiver.Name + " on " + formattedDate + " " + time + " ";
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

                        //var mstSenderDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value));
                        var senderName = (string.IsNullOrEmpty(delegatedUserName) ? User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.GivenName).Value : delegatedUserName);
                        //var mstVerifierDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(approverID));
                        var toEmail = receiver.EmailAddress;
                        var receiverName = receiver.Name;
                        var claimNo = pVGClaim.PVGCNo;
                        var screen = "PV-GIRO Claim";
                        var approvalType = "Query";
                        int userID = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
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
                var pVGcid = Convert.ToInt32(id);
                int UserId = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
                ViewBag.userID = UserId;
                //var queries1 = _context.mstQuery.ToList().Where(j => j.ID == smcid && (j.SenderID == UserId || j.ReceiverID == UserId) && j.ModuleType.ToString().Trim() == "Expense Claim").OrderBy(j => j.SentTime);
                var queries = await _repository.MstQuery.GetAllClaimsQueriesAsync(UserId, pVGcid, "PVG Claim");
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
