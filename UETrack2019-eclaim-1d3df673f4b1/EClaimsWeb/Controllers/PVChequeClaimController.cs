using AutoMapper;
using ClosedXML.Excel;
using DocumentFormat.OpenXml.Office2010.Excel;
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
    public class PVChequeClaimController : Controller
    {
        private ILoggerManager _logger;
        private IRepositoryWrapper _repository;
        private IMapper _mapper;
        private IConfiguration _configuration;
        private AlternateApproverHelper _alternateApproverHelper;
        private ISendMailServices _sendMailServices;
        private readonly RepositoryContext _context;
        private readonly IToastNotification _toastNotification;
        public PVChequeClaimController(IToastNotification toastNotification, ILoggerManager logger, IRepositoryWrapper repository, IMapper mapper, RepositoryContext context, IConfiguration configuration, ISendMailServices sendMailServices)
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

                var mstPVCClaimsWithDetails = await _repository.MstPVCClaim.GetAllPVCClaimWithDetailsAsync(Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value), 0, 0, "", "");
                //var mstPVCClaimsWithDetails = await _repository.MstPVCClaim.GetAllPVCClaimWithDetailsByFacilityIDAsync(Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value), Convert.ToInt32(HttpContext.User.FindFirst("delegatefacilityid") is null ? HttpContext.User.FindFirst("facilityid").Value : HttpContext.User.FindFirst("delegatefacilityid").Value));
                PVCClaimsVM pVCClaimVMs = new PVCClaimsVM();
                //List<PVCClaimVM> pVCClaimVMs = new List<PVCClaimVM>();
                foreach (var mc in mstPVCClaimsWithDetails)
                {
                    PVCClaimVM pVCClaimVM = new PVCClaimVM();
                    pVCClaimVM.PVCCID = mc.CID;
                    pVCClaimVM.PVCCNo = mc.CNO;
                    pVCClaimVM.Name = mc.Name;
                    pVCClaimVM.CreatedDate = Convert.ToDateTime(mc.CreatedDate).ToString("dd/MM/yyyy", System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
                    pVCClaimVM.FacilityName = mc.FacilityName;
                    pVCClaimVM.Phone = mc.Phone;
                    pVCClaimVM.GrandTotal = mc.GrandTotal;
                    pVCClaimVM.ApprovalStatus = mc.ApprovalStatus;
                    pVCClaimVM.TotalAmount = mc.TotalAmount;
                    pVCClaimVM.VoucherNo = mc.VoucherNo;
                    pVCClaimVM.PayeeName = mc.PayeeName;

                    pVCClaimVM.AVerifier = mc.Verifier;
                    pVCClaimVM.AApprover = mc.Approver;
                    pVCClaimVM.AUserApprovers = mc.UserApprovers;
                    pVCClaimVM.AHODApprover = mc.HODApprover;

                    pVCClaimVM.DVerifier = mc.DVerifier;
                    pVCClaimVM.DApprover = mc.DApprover;
                    pVCClaimVM.DUserApprovers = mc.DUserApprovers;
                    pVCClaimVM.DHODApprover = mc.DHODApprover;

                    if (mc.UserApprovers != "")
                    {
                        pVCClaimVM.Approver = mc.UserApprovers.Split(',').First();
                    }
                    else if (mc.HODApprover != "")
                    {
                        pVCClaimVM.Approver = mc.HODApprover.Split(',').First();
                    }
                    else if (mc.Verifier != "")
                    {
                        pVCClaimVM.Approver = mc.Verifier.Split(',').First();
                        //string VerifierIDs = string.Join(",", PVCverifierIDs.Skip(1));
                    }
                    else if (mc.Approver != "")
                    {
                        pVCClaimVM.Approver = mc.Approver.Split(',').First();
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

                    // pVCClaimVMs.Add(pVCClaimVM);
                    pVCClaimVMs.pvcClaims.Add(pVCClaimVM);
                    _logger.LogInfo($"Returned all PV Cheque Claims with details from database.");
                }
                //var mstExpenseCategoriesWithTypesResult = _mapper.Map<IEnumerable<MstExpenseCategory>>(mstExpenseCategoriesWithTypes);
                var mstPVCClaimsDraftWithDetails = await _repository.MstPVCClaimDraft.GetAllPVCClaimDraftWithDetailsByFacilityIDAsync(Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value), Convert.ToInt32(HttpContext.User.FindFirst("delegatefacilityid") is null ? HttpContext.User.FindFirst("facilityid").Value : HttpContext.User.FindFirst("delegatefacilityid").Value));

                // List<PVCClaimVM> pVCClaimVMs = new List<PVCClaimVM>();
                foreach (var mc in mstPVCClaimsDraftWithDetails)
                {
                    PVCClaimVM pVCClaimDraftVM = new PVCClaimVM();
                    pVCClaimDraftVM.PVCCID = mc.PVCCID;
                    pVCClaimDraftVM.PVCCNo = mc.PVCCNo;
                    pVCClaimDraftVM.Name = mc.MstUser.Name;
                    pVCClaimDraftVM.CreatedDate = Convert.ToDateTime(mc.CreatedDate).ToString("dd/MM/yyyy", System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
                    pVCClaimDraftVM.FacilityName = mc.MstFacility.FacilityName;
                    pVCClaimDraftVM.Phone = mc.MstUser.Phone;
                    pVCClaimDraftVM.GrandTotal = mc.GrandTotal;
                    pVCClaimDraftVM.ApprovalStatus = mc.ApprovalStatus;
                    pVCClaimDraftVM.TotalAmount = mc.TotalAmount;


                    if (mc.UserApprovers != "")
                    {
                        pVCClaimDraftVM.Approver = mc.UserApprovers.Split(',').First();
                    }
                    else if (mc.HODApprover != "")
                    {
                        pVCClaimDraftVM.Approver = mc.HODApprover.Split(',').First();
                    }
                    else if (mc.Verifier != "")
                    {
                        pVCClaimDraftVM.Approver = mc.Verifier.Split(',').First();
                        //string VerifierIDs = string.Join(",", PVCverifierIDs.Skip(1));
                    }
                    else if (mc.Approver != "")
                    {
                        pVCClaimDraftVM.Approver = mc.Approver.Split(',').First();
                    }
                    else
                    {
                        pVCClaimDraftVM.Approver = "";
                    }

                    if (pVCClaimDraftVM.Approver != "")
                    {

                        var alternateUser = await _alternateApproverHelper.IsAlternateApprovalSetForUser(Convert.ToInt32(pVCClaimDraftVM.Approver));
                        if (alternateUser.HasValue)
                        {
                            var mstUserApprover = await _repository.MstUser.GetUserByIdAsync(alternateUser.Value);
                            pVCClaimDraftVM.Approver = mstUserApprover.Name + " (AA)";
                        }
                        else
                        {
                            var mstUserApprover = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(pVCClaimDraftVM.Approver));
                            pVCClaimDraftVM.Approver = mstUserApprover.Name;
                        }
                    }
                    pVCClaimVMs.pvcClaimsDrafts.Add(pVCClaimDraftVM);
                    //  pVCClaimVMs.Add(pVCClaimVM);
                    _logger.LogInfo($"Returned all PV Cheque Claims with details from database.");
                }
                return View(pVCClaimVMs);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Something went wrong inside GetAllPVCClaimWithDetailsAsync action: {ex.Message}");
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

            return RedirectToAction("Create", "PVChequeClaim", new
            {
                id = PVCCID,
                Updatestatus = "Edit"
            });
        }
        public async Task<ActionResult> DeletePVCClaimDraftFile(string fileID, string filepath, string PVCCID)
        {
            DtPVCClaimDraftFileUpload dtPVCClaimFileUpload = new DtPVCClaimDraftFileUpload();

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
                        dtPVCClaimFileUpload = await _repository.DtPVCClaimFileUploadDraft.GetDtPVCClaimDraftFileUploadByIdAsync(Convert.ToInt64(fileID));
                        _repository.DtPVCClaimFileUploadDraft.DeleteDtPVCClaimFileUploadDraft(dtPVCClaimFileUpload);
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

            return RedirectToAction("CreateDraft", "PVChequeClaim", new
            {
                id = PVCCID,
                Updatestatus = "Edit"
            });
        }
        public async Task<IActionResult> Create(string id, string Updatestatus)
        {
            //TempData["CBRID"] = 0;
            TempData["Updatestatus"] = "Add";
            PVCClaimDetailVM pvcClaimDetailVM = new PVCClaimDetailVM();
            pvcClaimDetailVM.DtPVCClaimVMs = new List<DtPVCClaimVM>();
            pvcClaimDetailVM.PVCClaimAudits = new List<PVCClaimAuditVM>();
            long idd = 0;
            TempData["claimaddcondition"] = "claimnew";
            if (User != null && User.Identity.IsAuthenticated)
            {
                if (!string.IsNullOrEmpty(id))
                {
                    idd = Convert.ToInt64(id);
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
                        if (Updatestatus == "Recreate")
                        {
                            ViewBag.UpdateStatus = "Recreate";
                            dtPVCClaimVM.PVCCItemID = 0;
                        }
                        pvcClaimDetailVM.DtPVCClaimVMs.Add(dtPVCClaimVM);
                    }

                    pvcClaimDetailVM.PVCClaimFileUploads = new List<DtPVCClaimFileUpload>();
                    var pvcuploadedFiles = await _repository.DtPVCClaimFileUpload.GetDtPVCClaimAuditByIdAsync(idd);
                    if (Updatestatus == "Recreate" && pvcuploadedFiles != null && pvcuploadedFiles.Count > 0)
                    {
                        foreach (var uploadedData in pvcuploadedFiles)
                        {
                            uploadedData.PVCCID = 0;
                            pvcClaimDetailVM.PVCClaimFileUploads.Add(uploadedData);
                        }
                    }
                    else
                        pvcClaimDetailVM.PVCClaimFileUploads = pvcuploadedFiles;

                    var mstPVCClaim = await _repository.MstPVCClaim.GetPVCClaimByIdAsync(idd);


                    PVCClaimVM pvcClaimVM = new PVCClaimVM();
                    pvcClaimVM.GrandTotal = mstPVCClaim.GrandTotal;
                    pvcClaimVM.GrandGST = mstPVCClaim.TotalAmount - mstPVCClaim.GrandTotal;
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
                    pvcClaimDetailVM.PVCClaimAudits = new List<PVCClaimAuditVM>();
                    pvcClaimDetailVM.PVCClaimFileUploads = new List<DtPVCClaimFileUpload>();
                    PVCClaimVM pvcClaimVM = new PVCClaimVM();
                    pvcClaimVM.GrandTotal = 0;
                    pvcClaimVM.TotalAmount = 0;
                    pvcClaimVM.GrandGST = 0;
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

                BindGSTDropdown();

                // Below code is to add original tax value in case of amendment under GST dropdown 
                if (!String.IsNullOrEmpty(id) && Updatestatus == "Recreate")
                {
                    var dtPVCClaims = await _repository.DtPVCClaim.GetDtPVCClaimByIdAsync(idd);
                    //bool exists = items.Any(x => x.Value == dtPVCClaims.GSTPercentage.ToString());
                    //if (exists)
                    //{
                    //    foreach (var item in items)
                    //    {
                    //        item.Selected = false;
                    //    }
                    //    var selected = items.Where(x => x.Value == dtPVCClaims.GSTPercentage.ToString()).First();
                    //    selected.Selected = true;
                    //    ViewBag.dllGST = items;
                    //}
                }

            }
            return View(pvcClaimDetailVM);

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

        public async Task<IActionResult> CreateDraft(string id, string Updatestatus)
        {
            //TempData["CBRID"] = 0;
            TempData["Updatestatus"] = "Add";
            TempData["claimaddcondition"] = "claimDraft";
            PVCClaimDetailVM pvcClaimDetailVM = new PVCClaimDetailVM();
            pvcClaimDetailVM.DtPVCClaimVMs = new List<DtPVCClaimVM>();
            pvcClaimDetailVM.PVCClaimAudits = new List<PVCClaimAuditVM>();

            if (User != null && User.Identity.IsAuthenticated)
            {
                if (!string.IsNullOrEmpty(id))
                {
                    long idd = Convert.ToInt64(id);
                    ViewBag.CID = idd;
                    var dtPVCClaims = await _repository.DtPVCClaimDraft.GetDtPVCClaimDraftByIdAsync(idd);

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
                    var ecFuploads = await _repository.DtPVCClaimFileUploadDraft.GetDtPVCClaimDraftAuditByIdAsync(idd);
                    pvcClaimDetailVM.PVCClaimFileUploads = new List<DtPVCClaimFileUpload>();

                    foreach (var item in ecFuploads)
                    {
                        MstPVCClaim mstPVCClaim1 = new MstPVCClaim();
                        if (item.MstPVCClaimDraft != null)
                        {
                            mstPVCClaim1 = new MstPVCClaim()
                            {
                                ApprovalBy = item.MstPVCClaimDraft.ApprovalBy,
                                ApprovalDate = item.MstPVCClaimDraft.ApprovalDate,
                                ApprovalStatus = item.MstPVCClaimDraft.ApprovalStatus,
                                ModifiedDate = item.MstPVCClaimDraft.ModifiedDate,
                                ModifiedBy = item.MstPVCClaimDraft.ModifiedBy,
                                Approver = item.MstPVCClaimDraft.Approver,
                                //ClaimType = item.MstExpenseClaimDraft.ClaimType,
                                Company = item.MstPVCClaimDraft.Company,
                                CreatedBy = item.MstPVCClaimDraft.CreatedBy,
                                CreatedDate = item.MstPVCClaimDraft.CreatedDate,
                                DepartmentID = item.MstPVCClaimDraft.DepartmentID,
                                PVCCID = item.MstPVCClaimDraft.PVCCID,
                                PVCCNo = item.MstPVCClaimDraft.PVCCNo,
                                FacilityID = item.MstPVCClaimDraft.FacilityID,
                                FinalApprover = item.MstPVCClaimDraft.FinalApprover,
                                GrandTotal = item.MstPVCClaimDraft.GrandTotal,
                                HODApprover = item.MstPVCClaimDraft.HODApprover,
                                MstDepartment = item.MstPVCClaimDraft.MstDepartment,
                                MstFacility = item.MstPVCClaimDraft.MstFacility,
                                MstUser = item.MstPVCClaimDraft.MstUser,
                                TnC = item.MstPVCClaimDraft.TnC,
                                TotalAmount = item.MstPVCClaimDraft.TotalAmount,
                                UserApprovers = item.MstPVCClaimDraft.UserApprovers,
                                UserID = item.MstPVCClaimDraft.UserID,
                                Verifier = item.MstPVCClaimDraft.Verifier,
                                VoidReason = item.MstPVCClaimDraft.VoidReason
                            };
                        }

                        pvcClaimDetailVM.PVCClaimFileUploads.Add(new DtPVCClaimFileUpload()
                        {
                            CreatedBy = item.CreatedBy,
                            CreatedDate = item.CreatedDate,
                            PVCCID = item.PVCCID,
                            FileID = item.FileID,
                            FileName = item.FileName,
                            FilePath = item.FilePath,
                            IsDeleted = item.IsDeleted,
                            ModifiedBy = item.ModifiedBy,
                            ModifiedDate = item.ModifiedDate,
                            MstPVCClaim = mstPVCClaim1
                        });
                    }

                    // pvcClaimDetailVM.PVCClaimFileUploads = await _repository.DtPVCClaimFileUpload.GetDtPVCClaimAuditByIdAsync(idd);

                    var mstPVCClaim = await _repository.MstPVCClaimDraft.GetPVCClaimDraftByIdAsync(idd);


                    PVCClaimVM pvcClaimVM = new PVCClaimVM();
                    pvcClaimVM.GrandTotal = mstPVCClaim.GrandTotal;
                    pvcClaimVM.GrandGST = mstPVCClaim.TotalAmount - mstPVCClaim.GrandTotal;
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
                    pvcClaimVM.GrandGST = 0;
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
                BindGSTDropdown();
            }
            return View("Create", pvcClaimDetailVM);

        }

        public async Task<IActionResult> DeletePVCClaimDraft(string id)
        {
            try
            {
                long idd = Convert.ToInt64(id);
                var pvcClaimsDraft = await _repository.MstPVCClaimDraft.GetPVCClaimDraftByIdAsync(idd);
                _repository.MstPVCClaimDraft.DeletePVCClaimDraft(pvcClaimsDraft);
                await _repository.SaveAsync();
                TempData["Message"] = "Draft deleted successfully";
                Content("<script language='javascript' type='text/javascript'>alert('Draft deleted successfully');</script>");
                return RedirectToAction("Index", "PVChequeClaim");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Something went wrong inside DeletePVCClaimDraft action: {ex.Message}");
            }
            return Json(null);
        }
        public async Task<JsonResult> GetTextValuesSGDraft(string id)
        {
            List<DtPVCClaimVM> oDtClaimsList = new List<DtPVCClaimVM>();

            try
            {
                var dtPVCClaims = await _repository.DtPVCClaimDraft.GetDtPVCClaimDraftByIdAsync(Convert.ToInt64(id));

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

        public async Task<IActionResult> Details(long? id)
        {
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
                    dtPVCClaimVM.Particulars = item.Particulars;
                    dtPVCClaimVM.InvoiceNo = item.InvoiceNo;
                    dtPVCClaimVM.Payee = item.Payee;
                    dtPVCClaimVM.ChequeNo = item.ChequeNo;
                    dtPVCClaimVM.Amount = item.Amount;
                    dtPVCClaimVM.GSTPercentage = item.GSTPercentage;
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
                var GroupByQS = pVCClaimDetailVM.DtPVCClaimVMs.GroupBy(s => s.AccountCode);
                //var GroupByQS = (from std in pVCClaimDetailVM.DtExpenseClaimVMs
                //                                                           group std by std.ExpenseCategoryID);

                //pVCClaimDetailVM.DtPVCClaimVMs = new List<DtPVCClaimVM>();

                //foreach (var group in GroupByQS)
                //{
                //    DtPVCClaimVM dtPVCClaimVM = new DtPVCClaimVM();
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
                //    dtPVCClaimVM.ExpenseCategory = PVCDesc;
                //    dtPVCClaimVM.AccountCode = AccountCode;
                //    dtPVCClaimVM.Amount = amount;
                //    dtPVCClaimVM.GST = gst;
                //    dtPVCClaimVM.AmountWithGST = sumamount;
                //    pVCClaimDetailVM.DtPVCClaimVMSummary.Add(dtPVCClaimVM);
                //}

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

                PVCClaimVM PVCClaimVM = new PVCClaimVM();
                PVCClaimVM.VoucherNo = mstPVCClaim.VoucherNo;
                PVCClaimVM.GrandTotal = mstPVCClaim.GrandTotal;
                PVCClaimVM.TotalAmount = mstPVCClaim.TotalAmount;
                PVCClaimVM.GrandGST = mstPVCClaim.TotalAmount - mstPVCClaim.GrandTotal;
                PVCClaimVM.Company = mstPVCClaim.Company;
                PVCClaimVM.Name = mstPVCClaim.MstUser.Name;
                PVCClaimVM.DepartmentName = mstPVCClaim.MstDepartment.Department;
                PVCClaimVM.FacilityName = mstPVCClaim.MstFacility.FacilityName;
                PVCClaimVM.CreatedDate = mstPVCClaim.CreatedDate.ToString("d");
                PVCClaimVM.Verifier = mstPVCClaim.Verifier;
                PVCClaimVM.Approver = mstPVCClaim.Approver;
                PVCClaimVM.PVCCNo = mstPVCClaim.PVCCNo;
                ViewBag.PVCCID = id;
                TempData["CreatedBy"] = mstPVCClaim.CreatedBy;
                ViewBag.Approvalstatus = mstPVCClaim.ApprovalStatus;

                if (mstPVCClaim.Verifier == mstPVCClaim.DVerifier && mstPVCClaim.Approver == mstPVCClaim.DApprover && mstPVCClaim.UserApprovers == mstPVCClaim.DUserApprovers && mstPVCClaim.HODApprover == mstPVCClaim.DHODApprover)
                {
                    ViewBag.UserEditStatus = 4;
                }
                else
                {
                    ViewBag.UserEditStatus = 0;
                }

                TempData["ApprovedStatus"] = mstPVCClaim.ApprovalStatus;
                TempData["FinalApproverID"] = mstPVCClaim.FinalApprover;
                ViewBag.VoidReason = mstPVCClaim.VoidReason == null ? "" : mstPVCClaim.VoidReason;

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
                if (mstPVCClaim.Verifier != "")
                {
                    string[] verifierIDs = mstPVCClaim.Verifier.Split(',');
                    TempData["QueryMCVerifierIDs"] = string.Join(",", verifierIDs);
                    foreach (string verifierID in verifierIDs)
                    {
                        if (verifierID != "" && verifierID == (HttpContext.User.FindFirst("userid").Value) && User.IsInRole("Finance"))
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
                        if (approverID != "" && approverID == (HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value) && User.IsInRole("Finance"))
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



                if (mstPVCClaim.UserApprovers != "" && mstPVCClaim.Verifier == "")
                {
                    string[] userApproverIDs = mstPVCClaim.UserApprovers.Split(',');
                    TempData["QueryMCUserApproverIDs"] = string.Join(",", userApproverIDs);
                    foreach (string approverID in userApproverIDs)
                    {
                        if (approverID != "" && approverID == (HttpContext.User.FindFirst("userid").Value))
                        {
                            TempData["ApprovedStatus"] = mstPVCClaim.ApprovalStatus;
                            //Session["ApprovedStatus"] = oSupplierPO.Approvalstatus;
                            TempData["HODApproverIDs"] = string.Join(",", userApproverIDs.Skip(1));
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

                if (mstPVCClaim.HODApprover != "" && mstPVCClaim.Verifier == "")
                {
                    string[] hodApproverIDs = mstPVCClaim.HODApprover.Split(',');
                    TempData["QueryMCHODApproverIDs"] = string.Join(",", hodApproverIDs);
                    foreach (string approverID in hodApproverIDs)
                    {
                        if (approverID != "" && approverID == (HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value))
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


                pVCClaimDetailVM.PVCClaimVM = PVCClaimVM;
                //mileageClaimDetailVM.DtMileageClaimVMs = dtMileageClaimVMs;


                BindGSTDropdown();
                return View(pVCClaimDetailVM);
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
                    dtPVCClaimVM.Particulars = item.Particulars;
                    dtPVCClaimVM.InvoiceNo = item.InvoiceNo;
                    dtPVCClaimVM.Payee = item.Payee;
                    dtPVCClaimVM.ChequeNo = item.ChequeNo;
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
                //var GroupByQS = (from std in pVCClaimDetailVM.DtExpenseClaimVMs
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

                int loggedInUserId = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
                bool isAlternateApprover = false;
                var delegatedUserId = await _alternateApproverHelper.IsUserHasAnyAlternateApprovalSet(loggedInUserId);
                if (delegatedUserId.HasValue)
                {
                    isAlternateApprover = true;
                }

                if (Convert.ToInt32(approvedStatus) == 3 || Convert.ToInt32(approvedStatus) == 9 || Convert.ToInt32(approvedStatus) == 10)
                {
                    await _repository.MstPVCClaim.UpdateMstPVCClaimStatus(PVCCID, -5, int.Parse(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value), DateTime.Now, reason, string.Empty, string.Empty, string.Empty, string.Empty, isAlternateApprover, 0);
                }
                else
                {
                    await _repository.MstPVCClaim.UpdateMstPVCClaimStatus(PVCCID, 5, int.Parse(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value), DateTime.Now, reason, string.Empty, string.Empty, string.Empty, string.Empty, isAlternateApprover, 0);
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
                int PVCCID = Convert.ToInt32(id);

                var mstPVCClaim = await _repository.MstPVCClaim.GetPVCClaimByIdAsync(PVCCID);

                if (mstPVCClaim == null)
                {
                    // return NotFound();
                }

                int ApprovedStatus = Convert.ToInt32(mstPVCClaim.ApprovalStatus);
                bool excute = _repository.MstPVCClaim.ExistsApproval(PVCCID.ToString(), ApprovedStatus, HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value, "PVC");

                // If execute is false, Check if the current user is alternate user for this claim
                if (excute == false)
                {
                    string usapprover = _repository.MstTBClaim.GetApproverVerifier(PVCCID.ToString(), ApprovedStatus, HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value, "TelephoneBill");
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
                        await _repository.MstPVCClaim.UpdateMstPVCClaimStatus(PVCCID, 2, int.Parse(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value), DateTime.Now, string.Empty, VerifierIDs.ToString(), ApproverIDs.ToString(), UserApproverIDs.ToString(), HODApproverID.ToString(), isAlternateApprover, 0);

                    }
                    #endregion

                    #region PVC Approver
                    else if (ApprovedStatus == 2)
                    {
                        string VerifierIDs = "";
                        string ApproverIDs = "";
                        string UserApproverIDs = "";
                        string HODApproverID = "";
                        try
                        {
                            string[] PVCapproverIDs = mstPVCClaim.Approver.Split(',');
                            ApproverIDs = string.Join(",", PVCapproverIDs.Skip(1));
                            string[] approverIDs = ApproverIDs.Split(',');
                            int CreatedBy = Convert.ToInt32(mstPVCClaim.CreatedBy);

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
                        await _repository.MstPVCClaim.UpdateMstPVCClaimStatus(PVCCID, 3, int.Parse(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value), DateTime.Now, string.Empty, VerifierIDs, ApproverIDs, UserApproverIDs.ToString(), HODApproverID.ToString(), isAlternateApprover, int.Parse(financeStartDay));
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
                int loggedInUserId = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
                bool isAlternateApprover = false;
                var delegatedUserId = await _alternateApproverHelper.IsUserHasAnyAlternateApprovalSet(loggedInUserId);
                if (delegatedUserId.HasValue)
                {
                    isAlternateApprover = true;
                }

                await _repository.MstPVCClaim.UpdateMstPVCClaimStatus(PVCCID, 4, int.Parse(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value), DateTime.Now, reason, string.Empty, string.Empty, string.Empty, string.Empty, isAlternateApprover, 0);

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
                                            new DataColumn("Particulars of payment"),
                                            new DataColumn("Payee Name"),
                                            new DataColumn("Amount"),
                                            new DataColumn("GST"),
                                             new DataColumn("Expense Category")});
            using (XLWorkbook wb = new XLWorkbook())
            {
                wb.Worksheets.Add(dt);
                using (MemoryStream stream = new MemoryStream())
                {
                    wb.SaveAs(stream);
                    return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "PVChequeTemplate.xlsx");
                }
            }
            */
            string id = "PVChequeTemplate.xlsm";

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

                        //cmd = new SqlCommand("delete from MstPVCClaimtemp", con);
                        con.Open();
                        //cmd.ExecuteNonQuery();

                        sqlBulkCopy.DestinationTableName = "dbo.MstPVCClaimtemp";

                        sqlBulkCopy.ColumnMappings.Add("UserName", "UserName");
                        //sqlBulkCopy.ColumnMappings.Add("EmailAddress", "EmailAddress");
                        //sqlBulkCopy.ColumnMappings.Add("Company", "Company");
                        //sqlBulkCopy.ColumnMappings.Add("Department", "Department");
                        //sqlBulkCopy.ColumnMappings.Add("Facility", "Facility");
                        //sqlBulkCopy.ColumnMappings.Add("DateofCreated", "DateofCreated");       
                        //sqlBulkCopy.ColumnMappings.Add("Date", "Date");
                        sqlBulkCopy.ColumnMappings.Add("Facility", "Facility");
                        sqlBulkCopy.ColumnMappings.Add("Particulars Of payment", "Particulars");
                        sqlBulkCopy.ColumnMappings.Add("Payee Name", "Payee");
                        sqlBulkCopy.ColumnMappings.Add("Amount", "Amount");
                        sqlBulkCopy.ColumnMappings.Add("GST", "GST");
                        sqlBulkCopy.ColumnMappings.Add("GSTPercentage", "GSTPercentage");
                        sqlBulkCopy.ColumnMappings.Add("Claimid", "Claimid");
                        sqlBulkCopy.ColumnMappings.Add("Expense Category", "DescriptionofExpenseCatergory");
                        sqlBulkCopy.ColumnMappings.Add("Userid", "Userid");
                        sqlBulkCopy.ColumnMappings.Add("Facilityid", "FacilityID");
                        sqlBulkCopy.ColumnMappings.Add("Status", "Status");
                        sqlBulkCopy.WriteToServer(dt);
                    }
                }

                DataTable InvaildData = _repository.MstPVCClaim.InsertExcel(Convert.ToInt32((HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value)), Convert.ToInt32(HttpContext.User.FindFirst("userid").Value));

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
                        var mstPVCClaim = await _repository.MstPVCClaim.GetPVCClaimByIdAsync(cid);
                        if (mstPVCClaim.ApprovalStatus == 7)
                        {
                            string VerifierIDs = "";
                            string ApproverIDs = "";
                            string UserApproverIDs = "";
                            string HODApproverID = "";
                            try
                            {
                                //VerifierIDs = mstPVCClaim.Verifier.Split(',');
                                //VerifierIDs = string.Join(",", ExpenseverifierIDs.Skip(1));
                                string[] hODApproverIDs = mstPVCClaim.HODApprover.Split(',');
                                ApproverIDs = mstPVCClaim.Approver;
                                //HODApproverID = mstPVCClaim.HODApprover;



                                //BackgroundJob.Enqueue(() => _sendMailServices.SendEmail());
                                //Mail Code Implementation for Verifiers

                                foreach (string hODApproverID in hODApproverIDs)
                                {
                                    if (hODApproverID != "")
                                    {
                                        string domainUrl = HttpContext.Request.Scheme + "://" + HttpContext.Request.Host;
                                        string clickUrl = domainUrl + "/" + "HodSummary/PVCCDetails/" + mstPVCClaim.PVCCID;

                                        var mstSenderDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value));
                                        var senderName = mstSenderDetails.Name;
                                        var mstVerifierDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(hODApproverID));
                                        var toEmail = mstVerifierDetails.EmailAddress;
                                        var receiverName = mstVerifierDetails.Name;
                                        var claimNo = mstPVCClaim.PVCCNo;
                                        var screen = "PV-Cheque Claim";
                                        var approvalType = "Approval Request";
                                        int userID = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
                                        var subject = "PV-Cheque Claim for Approval " + claimNo;

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
                            string[] userApproverIDs = mstPVCClaim.UserApprovers.ToString().Split(',');
                            foreach (string userApproverID in userApproverIDs)
                            {
                                if (userApproverID != "")
                                {
                                    string domainUrl = HttpContext.Request.Scheme + "://" + HttpContext.Request.Host;
                                    string clickUrl = domainUrl + "/" + "HodSummary/PVCCDetails/" + mstPVCClaim.PVCCID;

                                    var mstSenderDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value));
                                    var senderName = mstSenderDetails.Name;
                                    var mstVerifierDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(userApproverID));
                                    var toEmail = mstVerifierDetails.EmailAddress;
                                    var receiverName = mstVerifierDetails.Name;
                                    var claimNo = mstPVCClaim.PVCCNo;
                                    var screen = "PV-Cheque Claim";
                                    var approvalType = "Approval Request";
                                    int userID = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
                                    var subject = "PV-Cheque Claim for Approval " + claimNo;

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
                        return RedirectToAction("Index", "PVChequeClaim", "File has imported.Please check the downloaded file.");

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
                                return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "PVChequeTemplateValidate.xlsx");


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


            return RedirectToAction("Index", "PVCClaim");

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
                    if (cell.Address.Contains("F") || cell.Address.Contains("G") || cell.Address.Contains("H"))
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
            //var pVCClaimViewModel = JsonConvert.DeserializeObject<PVCClaimViewModel>(data,
            //    new IsoDateTimeConverter { DateTimeFormat = "dd/MM/yyyy" });

            var pVCClaimViewModel = JsonConvert.DeserializeObject<PVCClaimViewModel>(data);
            string claimsCondition = Request.Form["claimAddCondition"];
            int pvcIDValue = Convert.ToInt32(Request.Form["ecIDValue"]);

            var mstFacility = await _repository.MstFacility.GetFacilityWithDepartmentByIdAsync(Convert.ToInt32(HttpContext.User.FindFirst("delegatefacilityid") is null ? HttpContext.User.FindFirst("facilityid").Value : HttpContext.User.FindFirst("delegatefacilityid").Value));



            MstPVCClaim mstPVCClaim = new MstPVCClaim();
            mstPVCClaim.PVCCNo = pVCClaimViewModel.PVCCNo;
            mstPVCClaim.UserID = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
            mstPVCClaim.Verifier = "";
            mstPVCClaim.Approver = "";
            mstPVCClaim.FinalApprover = "";
            mstPVCClaim.ApprovalStatus = 1;
            mstPVCClaim.GrandTotal = pVCClaimViewModel.GrandTotal;
            mstPVCClaim.TotalAmount = pVCClaimViewModel.TotalAmount;
            mstPVCClaim.Company = pVCClaimViewModel.Company;
            mstPVCClaim.FacilityID = Convert.ToInt32(HttpContext.User.FindFirst("delegatefacilityid") is null ? HttpContext.User.FindFirst("facilityid").Value : HttpContext.User.FindFirst("delegatefacilityid").Value);
            mstPVCClaim.DepartmentID = mstFacility.MstDepartment.DepartmentID;
            mstPVCClaim.CreatedDate = DateTime.Now;
            mstPVCClaim.ModifiedDate = DateTime.Now;
            mstPVCClaim.CreatedBy = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value); // Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
            mstPVCClaim.ModifiedBy = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value); // Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
            mstPVCClaim.ApprovalDate = DateTime.Now;
            mstPVCClaim.ApprovalBy = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
            mstPVCClaim.DelegatedBy = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? 0 : HttpContext.User.FindFirst("delegateuserid").Value);
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
                if (PVCCID == 0 || TempData["Updatestatus"].ToString() == "Recreate")
                {
                    ClaimStatus = "Recreate";
                    PVCCID = 0;
                }
                else if(PVCCID == 0)
                    ClaimStatus = "Add";
                else
                    ClaimStatus = "Update";

                if (pVCClaimViewModel.ClaimAddCondition == "claimDraft")
                {
                    mstPVCClaim.PVCCID = 0;
                }
                else
                {
                    mstPVCClaim.PVCCID = PVCCID;
                }
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

                if (PVCCID == 0 || TempData["Updatestatus"].ToString() == "Recreate")
                {
                    dtPVCClaimVM.PVCCID = 0;
                    dtPVCClaimVM.PVCCItemID = 0;
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

            var GroupByQS = pVCClaimDetailVM.DtPVCClaimVMs.GroupBy(s => new { s.AccountCode, s.ExpenseCategory, s.FacilityID, s.GST });

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
           // dtPVCClaimSummary.GSTPercentage = mstPVCClaim.GSTP;
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
                if (ClaimStatus == "Add" || ClaimStatus == "Recreate")
                {
                    mstPVCClaim = await _repository.MstPVCClaim.GetPVCClaimByIdAsync(res);
                    if (mstPVCClaim.ApprovalStatus == 7)
                    {
                        string VerifierIDs = "";
                        string ApproverIDs = "";
                        string UserApproverIDs = "";
                        string HODApproverID = "";
                        try
                        {
                            //VerifierIDs = mstPVCClaim.Verifier.Split(',');
                            //VerifierIDs = string.Join(",", ExpenseverifierIDs.Skip(1));
                            string[] hODApproverIDs = mstPVCClaim.HODApprover.Split(',');
                            ApproverIDs = mstPVCClaim.Approver;
                            //HODApproverID = mstPVCClaim.HODApprover;



                            //BackgroundJob.Enqueue(() => _sendMailServices.SendEmail());
                            //Mail Code Implementation for Verifiers

                            foreach (string hODApproverID in hODApproverIDs)
                            {
                                if (hODApproverID != "")
                                {
                                    string domainUrl = HttpContext.Request.Scheme + "://" + HttpContext.Request.Host;
                                    string clickUrl = domainUrl + "/" + "HodSummary/PVCCDetails/" + mstPVCClaim.PVCCID;

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
                                    var claimNo = mstPVCClaim.PVCCNo;
                                    var screen = "PV-Cheque Claim";
                                    var approvalType = "Approval Request";
                                    int userID = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
                                    var subject = "PV-Cheque Claim for Approval " + claimNo;

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
                        string[] userApproverIDs = mstPVCClaim.UserApprovers.ToString().Split(',');
                        foreach (string userApproverID in userApproverIDs)
                        {
                            if (userApproverID != "")
                            {
                                string domainUrl = HttpContext.Request.Scheme + "://" + HttpContext.Request.Host;
                                string clickUrl = domainUrl + "/" + "HodSummary/PVCCDetails/" + mstPVCClaim.PVCCID;

                                var mstSenderDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value));
                                var senderName = mstSenderDetails.Name;
                                var mstVerifierDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(userApproverID));
                                var toEmail = mstVerifierDetails.EmailAddress;
                                var receiverName = mstVerifierDetails.Name;
                                var claimNo = mstPVCClaim.PVCCNo;
                                var screen = "PV-Cheque Claim";
                                var approvalType = "Approval Request";
                                int userID = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
                                var subject = "PV-Cheque Claim for Approval " + claimNo;

                                BackgroundJob.Enqueue(() => _sendMailServices.SendEmail("EmailTemplate.html", screen, subject, senderName, receiverName, claimNo, approvalType, userID, toEmail, clickUrl));
                            }
                            break;
                        }
                    }

                    TempData["Message"] = "PV-Cheque Claim added successfully";
                }
                else
                {
                    mstPVCClaim = await _repository.MstPVCClaim.GetPVCClaimByIdAsync(res);
                    if (mstPVCClaim.ApprovalStatus == 1)
                    {
                        string VerifierIDs = "";
                        string ApproverIDs = "";
                        string UserApproverIDs = "";
                        string HODApproverID = "";
                        try
                        {
                            //VerifierIDs = mstPVCClaim.Verifier.Split(',');
                            //VerifierIDs = string.Join(",", ExpenseverifierIDs.Skip(1));
                            string[] verifierIDs = mstPVCClaim.Verifier.Split(',');
                            ApproverIDs = mstPVCClaim.Approver;
                            HODApproverID = mstPVCClaim.HODApprover;



                            //BackgroundJob.Enqueue(() => _sendMailServices.SendEmail());
                            //Mail Code Implementation for Verifiers

                            foreach (string verifierID in verifierIDs)
                            {
                                if (verifierID != "")
                                {
                                    string domainUrl = HttpContext.Request.Scheme + "://" + HttpContext.Request.Host;
                                    string clickUrl = domainUrl + "/" + "FinancePVCClaim/Details/" + mstPVCClaim.PVCCID;

                                    var mstSenderDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value));
                                    var senderName = mstSenderDetails.Name;
                                    var mstVerifierDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(verifierID));
                                    var toEmail = mstVerifierDetails.EmailAddress;
                                    var receiverName = mstVerifierDetails.Name;
                                    var claimNo = mstPVCClaim.PVCCNo;
                                    var screen = "PV-Cheque Claim";
                                    var approvalType = "Verification Request";
                                    int userID = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
                                    var subject = "PV-Cheque Claim for Verification " + claimNo;

                                    BackgroundJob.Enqueue(() => _sendMailServices.SendEmail("EmailTemplate.html", screen, subject, senderName, receiverName, claimNo, approvalType, userID, toEmail, clickUrl));
                                }
                                break;
                            }
                        }
                        catch
                        {
                        }
                    }
                    else if (mstPVCClaim.ApprovalStatus == 6)
                    {
                        string[] userApproverIDs = mstPVCClaim.UserApprovers.ToString().Split(',');
                        foreach (string userApproverID in userApproverIDs)
                        {
                            if (userApproverID != "")
                            {
                                string domainUrl = HttpContext.Request.Scheme + "://" + HttpContext.Request.Host;
                                string clickUrl = domainUrl + "/" + "HodSummary/PVCCDetails/" + mstPVCClaim.PVCCID;

                                var mstSenderDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value));
                                var senderName = mstSenderDetails.Name;
                                var mstVerifierDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(userApproverID));
                                var toEmail = mstVerifierDetails.EmailAddress;
                                var receiverName = mstVerifierDetails.Name;
                                var claimNo = mstPVCClaim.PVCCNo;
                                var screen = "PV-Cheque Claim";
                                var approvalType = "Approval Request";
                                int userID = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
                                var subject = "PV-Cheque Claim for Approval " + claimNo;

                                BackgroundJob.Enqueue(() => _sendMailServices.SendEmail("EmailTemplate.html", screen, subject, senderName, receiverName, claimNo, approvalType, userID, toEmail, clickUrl));
                            }
                            break;
                        }
                    }
                    else if (mstPVCClaim.ApprovalStatus == 7)
                    {
                        string[] hODApproverIDs = mstPVCClaim.HODApprover.ToString().Split(',');
                        foreach (string hODApproverID in hODApproverIDs)
                        {
                            if (hODApproverID != "")
                            {
                                string domainUrl = HttpContext.Request.Scheme + "://" + HttpContext.Request.Host;
                                string clickUrl = domainUrl + "/" + "HodSummary/PVCCDetails/" + mstPVCClaim.PVCCID;

                                var mstSenderDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value));
                                var senderName = mstSenderDetails.Name;
                                var mstVerifierDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(hODApproverID));
                                var toEmail = mstVerifierDetails.EmailAddress;
                                var receiverName = mstVerifierDetails.Name;
                                var claimNo = mstPVCClaim.PVCCNo;
                                var screen = "PV-Cheque Claim";
                                var approvalType = "Approval Request";
                                int userID = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
                                var subject = "PV-Cheque Claim for Approval " + claimNo;

                                BackgroundJob.Enqueue(() => _sendMailServices.SendEmail("EmailTemplate.html", screen, subject, senderName, receiverName, claimNo, approvalType, userID, toEmail, clickUrl));
                            }
                            break;
                        }
                    }
                    else
                    {
                        string[] ExpenseapproverIDs = mstPVCClaim.Approver.ToString().Split(',');
                        foreach (string approverID in ExpenseapproverIDs)
                        {
                            if (approverID != "")
                            {
                                string domainUrl = HttpContext.Request.Scheme + "://" + HttpContext.Request.Host;
                                string clickUrl = domainUrl + "/" + "FinancePVCClaim/Details/" + mstPVCClaim.PVCCID;

                                var mstSenderDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value));
                                var senderName = mstSenderDetails.Name;
                                var mstVerifierDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(approverID));
                                var toEmail = mstVerifierDetails.EmailAddress;
                                var receiverName = mstVerifierDetails.Name;
                                var claimNo = mstPVCClaim.PVCCNo;
                                var screen = "PV-Cheque Claim";
                                var approvalType = "Approval Request";
                                int userID = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
                                var subject = "PV-Cheque Claim for Approval " + claimNo;

                                BackgroundJob.Enqueue(() => _sendMailServices.SendEmail("EmailTemplate.html", screen, subject, senderName, receiverName, claimNo, approvalType, userID, toEmail, clickUrl));
                            }
                            break;
                        }
                    }
                    TempData["Message"] = "PV-Cheque Claim updated successfully";
                }

                return Json(new { res });
            }
            else
                return Json(new { res });

        }

        [HttpPost]
        public async Task<JsonResult> SaveItemsDraft(string data)
        {
            //var pVCClaimViewModel = JsonConvert.DeserializeObject<PVCClaimViewModel>(data,
            //    new IsoDateTimeConverter { DateTimeFormat = "dd/MM/yyyy" });

            var pVCClaimViewModel = JsonConvert.DeserializeObject<PVCClaimViewModel>(data);

            var mstFacility = await _repository.MstFacility.GetFacilityWithDepartmentByIdAsync(Convert.ToInt32(HttpContext.User.FindFirst("delegatefacilityid") is null ? HttpContext.User.FindFirst("facilityid").Value : HttpContext.User.FindFirst("delegatefacilityid").Value));


            MstPVCClaimDraft mstPVCClaim = new MstPVCClaimDraft();
            mstPVCClaim.PVCCNo = pVCClaimViewModel.PVCCNo;
            mstPVCClaim.UserID = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
            mstPVCClaim.Verifier = "";
            mstPVCClaim.Approver = "";
            mstPVCClaim.FinalApprover = "";
            mstPVCClaim.ApprovalStatus = 1;
            mstPVCClaim.GrandTotal = pVCClaimViewModel.GrandTotal;
            mstPVCClaim.TotalAmount = pVCClaimViewModel.TotalAmount;
            mstPVCClaim.Company = pVCClaimViewModel.Company;
            mstPVCClaim.FacilityID = Convert.ToInt32(HttpContext.User.FindFirst("delegatefacilityid") is null ? HttpContext.User.FindFirst("facilityid").Value : HttpContext.User.FindFirst("delegatefacilityid").Value);
            mstPVCClaim.DepartmentID = mstFacility.MstDepartment.DepartmentID;
            mstPVCClaim.CreatedDate = DateTime.Now;
            mstPVCClaim.ModifiedDate = DateTime.Now;
            mstPVCClaim.CreatedBy = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
            mstPVCClaim.ModifiedBy = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
            mstPVCClaim.ApprovalDate = DateTime.Now;
            mstPVCClaim.ApprovalBy = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
            mstPVCClaim.TnC = true;

            List<DtPVCClaimDraft> dtPVCClaims = new List<DtPVCClaimDraft>();
            foreach (var dtItem in pVCClaimViewModel.dtClaims)
            {
                DtPVCClaimDraft dtPVCClaim = new DtPVCClaimDraft();

                dtPVCClaim.PVCCItemID = dtItem.PVCCItemID;
                dtPVCClaim.PVCCID = dtItem.PVCCID;
                dtPVCClaim.Payee = dtItem.Payee;
                dtPVCClaim.Particulars = dtItem.Particulars;
                // dtPVCClaim.ExpenseCategory = dtItem.MstExpenseCategory.Description;
                dtPVCClaim.ExpenseCategoryID = dtItem.ExpenseCategoryID;
                //dtPVCClaimVM.EmployeeNo = item.EmployeeNo;
                dtPVCClaim.ChequeNo = dtItem.ChequeNo;
                dtPVCClaim.Amount = dtItem.Amount;
                dtPVCClaim.GST = dtItem.GST;
                dtPVCClaim.GSTPercentage = dtItem.GSTPercentage;
                //dtPVCClaim.AmountWithGST = dtItem.Amount + item.GST;
                //dtPVCClaimVM.Facility = item.Facility;
                dtPVCClaim.AccountCode = dtItem.AccountCode;
                dtPVCClaim.Date = dtItem.Date;
                dtPVCClaim.InvoiceNo = dtItem.InvoiceNo;
                dtPVCClaim.OrderBy = dtItem.OrderBy;

                var mstFacility1 = await _repository.MstFacility.GetFacilityWithDepartmentByIdAsync(Convert.ToInt32(dtItem.FacilityID));
                dtPVCClaim.FacilityID = Convert.ToInt32(mstFacility1.FacilityID);
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
                dtPVCClaims.Add(dtPVCClaim);
            }

            string ClaimStatus = "";
            long PVCCID = 0;
            try
            {
                //CBRID = Convert.ToInt32(Session["CBRID"].ToString());
                PVCCID = Convert.ToInt64(pVCClaimViewModel.PVCCID);
                if (PVCCID == 0 || TempData["Updatestatus"].ToString() == "Recreate")
                {
                    ClaimStatus = "Recreate";
                    PVCCID = 0;
                }
                else if (PVCCID == 0)
                    ClaimStatus = "Add";
                else
                    ClaimStatus = "Update";
                mstPVCClaim.PVCCID = PVCCID;
                //mstPVCClaim.PVCCNo = pVCClaimViewModel.;
            }
            catch { }

            PVCClaimDetailVM pVCClaimDetailVM = new PVCClaimDetailVM();
            //List<DtMileageClaimVM> dtMileageClaimVMs = new List<DtMileageClaimVM>();
            pVCClaimDetailVM.DtPVCClaimDraftVMs = new List<DtPVCClaimDraftVM>();
            // List<clsDtSupplierPO> oclsDtSupplierPO = new List<clsDtSupplierPO>();
            foreach (var item in pVCClaimViewModel.dtClaims)
            {
                DtPVCClaimDraftVM dtPVCClaimVM = new DtPVCClaimDraftVM();

                if (PVCCID == 0 || TempData["Updatestatus"].ToString() == "Recreate")
                {
                    dtPVCClaimVM.PVCCID = 0;
                    dtPVCClaimVM.PVCCItemID = 0;
                }
                dtPVCClaimVM.Payee = item.Payee;
                dtPVCClaimVM.Particulars = item.Particulars;
                dtPVCClaimVM.ExpenseCategory = item.MstExpenseCategory.Description;
                dtPVCClaimVM.ExpenseCategoryID = item.MstExpenseCategory.ExpenseCategoryID;
                //dtPVCClaimVM.EmployeeNo = item.EmployeeNo;
                dtPVCClaimVM.ChequeNo = item.ChequeNo;
                dtPVCClaimVM.Amount = item.Amount;
                dtPVCClaimVM.GST = item.GST;
                dtPVCClaimVM.GSTPercentage = item.GSTPercentage;
                dtPVCClaimVM.AmountWithGST = item.Amount + item.GST;
                //dtPVCClaimVM.Facility = item.Facility;
                dtPVCClaimVM.AccountCode = item.AccountCode;
                dtPVCClaimVM.Date = item.Date;
                dtPVCClaimVM.OrderBy = item.OrderBy;
                pVCClaimDetailVM.DtPVCClaimDraftVMs.Add(dtPVCClaimVM);
            }

            var GroupByQS = pVCClaimDetailVM.DtPVCClaimDraftVMs.GroupBy(s => new { s.AccountCode, s.ExpenseCategory, s.FacilityID, s.GST });

            pVCClaimDetailVM.DtPVCClaimDraftVMSummary = new List<DtPVCClaimDraftVM>();

            foreach (var group in GroupByQS)
            {
                DtPVCClaimDraftVM dtPVCClaimVM = new DtPVCClaimDraftVM();
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
                dtPVCClaimVM.Particulars = ExpenseDesc;
                dtPVCClaimVM.ExpenseCategory = ExpenseCat;
                dtPVCClaimVM.AccountCode = AccountCode;
                dtPVCClaimVM.Amount = amount;
                dtPVCClaimVM.GST = gst;
                dtPVCClaimVM.GSTPercentage = gstpercentage;
                dtPVCClaimVM.AmountWithGST = sumamount;
                pVCClaimDetailVM.DtPVCClaimDraftVMSummary.Add(dtPVCClaimVM);
            }
            List<DtPVCClaimSummaryDraft> lstPVCClaimSummary = new List<DtPVCClaimSummaryDraft>();
            foreach (var item in pVCClaimDetailVM.DtPVCClaimDraftVMSummary)
            {
                DtPVCClaimSummaryDraft dtPVCClaimSummary1 = new DtPVCClaimSummaryDraft();
                dtPVCClaimSummary1.AccountCode = item.AccountCode;
                dtPVCClaimSummary1.Amount = item.Amount;
                dtPVCClaimSummary1.ExpenseCategory = item.ExpenseCategory;
                dtPVCClaimSummary1.Description = item.Particulars.ToUpper();
                dtPVCClaimSummary1.GST = item.GST;
                dtPVCClaimSummary1.GSTPercentage = item.GSTPercentage;
                if (item.GST != 0)
                {
                    dtPVCClaimSummary1.TaxClass  = Math.Round((decimal)item.GSTPercentage, (int)1);
                }
                else
                {
                    dtPVCClaimSummary1.TaxClass = 4;
                }
                dtPVCClaimSummary1.AmountWithGST = item.AmountWithGST;
                lstPVCClaimSummary.Add(dtPVCClaimSummary1);
            }

            DtPVCClaimSummaryDraft dtPVCClaimSummary = new DtPVCClaimSummaryDraft();
            dtPVCClaimSummary.AccountCode = "425000";
            dtPVCClaimSummary.Amount = mstPVCClaim.GrandTotal;
            dtPVCClaimSummary.TaxClass = 0;
            dtPVCClaimSummary.GST = mstPVCClaim.TotalAmount - mstPVCClaim.GrandTotal;
            dtPVCClaimSummary.AmountWithGST = mstPVCClaim.TotalAmount;
            dtPVCClaimSummary.ExpenseCategory = "DBS";
            dtPVCClaimSummary.Description = "";
            lstPVCClaimSummary.Add(dtPVCClaimSummary);


            var res = await _repository.MstPVCClaimDraft.SaveItemsDraft(mstPVCClaim, dtPVCClaims, lstPVCClaimSummary);

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
            var path = "FileUploads/PVCClaimFiles/";
            //var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "FileUploads", "PVCClaimFiles");

            //if (!Directory.Exists(path))
            //{
            //    Directory.CreateDirectory(path);
            //}
            string claimsCondition = Request.Form["claimAddCondition"];
            int ecIDValue = Convert.ToInt32(Request.Form["ecIDValue"]);
            int PVCCID = Convert.ToInt32(Request.Form["Id"]);
            if (PVCCID == 0)
            {
                if (TempData.ContainsKey("CID"))
                    PVCCID = Convert.ToInt32(TempData["CID"].ToString());
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
                    string pathToFiles = Regex.Replace(result, @"[^0-9a-zA-Z]+", "_") + "-" + PVCCID.ToString() + "-" + DateTime.Now.ToString("ddMMyyyyss") + ext;

                    DtPVCClaimFileUpload dtPVCClaimFileUpload = new DtPVCClaimFileUpload();
                    dtPVCClaimFileUpload.PVCCID = PVCCID;
                    dtPVCClaimFileUpload.FileName = fileName;
                    dtPVCClaimFileUpload.FilePath = pathToFiles;
                    dtPVCClaimFileUpload.CreatedDate = DateTime.Now;
                    dtPVCClaimFileUpload.ModifiedDate = DateTime.Now;
                    dtPVCClaimFileUpload.CreatedBy = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
                    dtPVCClaimFileUpload.ModifiedBy = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
                    dtPVCClaimFileUpload.IsDeleted = false;
                    _repository.DtPVCClaimFileUpload.CreateDtPVCClaimFileUpload(dtPVCClaimFileUpload);
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
            long idd = Convert.ToInt64(ecIDValue);

            var dtFiles = await _repository.DtPVCClaimFileUploadDraft.GetDtPVCClaimDraftAuditByIdAsync(idd);
            if (dtFiles != null)
            {
                foreach (var dtFile in dtFiles)
                {
                    DtPVCClaimFileUpload dtPVCClaimFileUpload = new DtPVCClaimFileUpload()
                    {
                        CreatedBy = dtFile.CreatedBy,
                        CreatedDate = dtFile.CreatedDate,
                        FileID = 0,
                        FileName = dtFile.FileName,
                        FilePath = dtFile.FilePath,
                        IsDeleted = dtFile.IsDeleted,
                        ModifiedBy = dtFile.ModifiedBy,
                        ModifiedDate = dtFile.ModifiedDate,
                        PVCCID = PVCCID

                    };
                    try
                    {
                        _repository.DtPVCClaimFileUpload.Create(dtPVCClaimFileUpload);
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
                    var expenseClaimsDraft = await _repository.MstPVCClaimDraft.GetPVCClaimDraftByIdAsync(idd);
                    if (expenseClaimsDraft != null)
                    {
                        _repository.MstPVCClaimDraft.DeletePVCClaimDraft(expenseClaimsDraft);
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

        public async Task<JsonResult> UploadECFilesDraft(List<IFormFile> files)
        {
            var path = "FileUploads/PVCClaimFiles/";
            
            foreach (IFormFile formFile in files)
            {
                int PVCCID = Convert.ToInt32(Request.Form["Id"]);
                if (formFile.Length > 0)
                {
                    int fileSize = formFile.ContentDisposition.Length;
                    string fileName = ContentDispositionHeaderValue.Parse(formFile.ContentDisposition).FileName.Trim('"');
                    string mimeType = formFile.ContentType;
                    var filePath = Path.Combine(path, formFile.FileName);
                    string ext = Path.GetExtension(filePath);
                    string result = Path.GetFileNameWithoutExtension(filePath);
                    string pathToFiles = Regex.Replace(result, @"[^0-9a-zA-Z]+", "_") + "-" + PVCCID.ToString() + "-" + DateTime.Now.ToString("ddMMyyyyss") + ext;

                    DtPVCClaimDraftFileUpload dtPVCClaimDraftFileUpload = new DtPVCClaimDraftFileUpload();
                    dtPVCClaimDraftFileUpload = new DtPVCClaimDraftFileUpload();
                    dtPVCClaimDraftFileUpload.PVCCID = PVCCID;
                    dtPVCClaimDraftFileUpload.FileName = fileName;
                    dtPVCClaimDraftFileUpload.FilePath = pathToFiles;
                    dtPVCClaimDraftFileUpload.CreatedDate = DateTime.Now;
                    dtPVCClaimDraftFileUpload.ModifiedDate = DateTime.Now;
                    dtPVCClaimDraftFileUpload.CreatedBy = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
                    dtPVCClaimDraftFileUpload.ModifiedBy = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
                    dtPVCClaimDraftFileUpload.IsDeleted = false;
                    _repository.DtPVCClaimFileUploadDraft.CreateDtPVCClaimFileUploadDraft(dtPVCClaimDraftFileUpload);
                    await _repository.SaveAsync();

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
                    long PVCCID = Convert.ToInt64(queryParamViewModel.Cid);
                    int UserID = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
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
                        var delegatedUserName = string.Empty;
                        if (HttpContext.User.FindFirst("delegateuserid") is not null)
                        {
                            var delUserDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid").Value));
                            delegatedUserName = delUserDetails.Name;
                        }

                        auditUpdate.Description = "" + (string.IsNullOrEmpty(delegatedUserName) ? User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.GivenName).Value : delegatedUserName) + " Sent Query to " + receiver.Name + " on " + formattedDate + " " + time + " ";
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

                        //var mstSenderDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value));
                        var senderName = (string.IsNullOrEmpty(delegatedUserName) ? User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.GivenName).Value : delegatedUserName);
                        //var mstVerifierDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(approverID));
                        var toEmail = receiver.EmailAddress;
                        var receiverName = receiver.Name;
                        var claimNo = pVCClaim.PVCCNo;
                        var screen = "PV-Cheque Claim";
                        var approvalType = "Query";
                        int userID = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
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
                var pVCcid = Convert.ToInt32(id);
                int UserId = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
                ViewBag.userID = UserId;
                //var queries1 = _context.mstQuery.ToList().Where(j => j.ID == smcid && (j.SenderID == UserId || j.ReceiverID == UserId) && j.ModuleType.ToString().Trim() == "Expense Claim").OrderBy(j => j.SentTime);
                var queries = await _repository.MstQuery.GetAllClaimsQueriesAsync(UserId, pVCcid, "PVC Claim");
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
