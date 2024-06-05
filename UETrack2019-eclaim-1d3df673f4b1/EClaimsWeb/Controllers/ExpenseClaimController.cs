using AutoMapper;
using ClosedXML.Excel;
using EClaimsEntities;
using EClaimsEntities.Models;
using EClaimsRepository.Contracts;
using EClaimsWeb.Helpers;
using EClaimsWeb.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json;
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
using Microsoft.Extensions.Configuration;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using OfficeOpenXml;
using Hangfire;
using Newtonsoft.Json.Converters;
using NToastNotify;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace EClaimsWeb.Controllers
{
    [Authorize(Roles = "Admin,Finance,User,HR")]
    public class ExpenseClaimController : Controller
    {
        private ILoggerManager _logger;
        private IRepositoryWrapper _repository;
        private IMapper _mapper;
        private IConfiguration _configuration;
        private AlternateApproverHelper _alternateApproverHelper;
        private ISendMailServices _sendMailServices;
        private readonly IToastNotification _toastNotification;
        private readonly RepositoryContext _context;

        public ExpenseClaimController(IToastNotification toastNotification, ILoggerManager logger, IRepositoryWrapper repository, IMapper mapper, RepositoryContext context, IConfiguration configuration, ISendMailServices sendMailServices)
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

                ExpenseClaimsVM expenseClaimsVMs = new ExpenseClaimsVM();

                var mstExpenseClaimsWithDetails = await _repository.MstExpenseClaim.GetAllExpenseClaimWithDetailsByFacilityIDAsync(Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value), Convert.ToInt32(HttpContext.User.FindFirst("delegatefacilityid") is null ? HttpContext.User.FindFirst("facilityid").Value : HttpContext.User.FindFirst("delegatefacilityid").Value));
                List<ExpenseClaimVM> expenseClaimVMs = new List<ExpenseClaimVM>();
                foreach (var mc in mstExpenseClaimsWithDetails)
                {
                    ExpenseClaimVM expenseClaimVM = new ExpenseClaimVM();
                    expenseClaimVM.ECID = mc.ECID;
                    expenseClaimVM.ECNo = mc.ECNo;
                    expenseClaimVM.Name = mc.MstUser.Name;
                    expenseClaimVM.CreatedDate = Convert.ToDateTime(mc.CreatedDate).ToString("dd/MM/yyyy", System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
                    expenseClaimVM.FacilityName = mc.MstFacility.FacilityName;
                    expenseClaimVM.Phone = mc.MstUser.Phone;
                    expenseClaimVM.GrandTotal = mc.GrandTotal;
                    expenseClaimVM.ApprovalStatus = mc.ApprovalStatus;
                    expenseClaimVM.ClaimType = mc.ClaimType;
                    expenseClaimVM.TotalAmount = mc.TotalAmount;
                    expenseClaimVM.VoucherNo = mc.VoucherNo;

                    expenseClaimVM.AVerifier = mc.Verifier;
                    expenseClaimVM.AApprover = mc.Approver;
                    expenseClaimVM.AUserApprovers = mc.UserApprovers;
                    expenseClaimVM.AHODApprover = mc.HODApprover;

                    expenseClaimVM.DVerifier = mc.DVerifier;
                    expenseClaimVM.DApprover = mc.DApprover;
                    expenseClaimVM.DUserApprovers = mc.DUserApprovers;
                    expenseClaimVM.DHODApprover = mc.DHODApprover;

                    if (mc.UserApprovers != "")
                    {
                        expenseClaimVM.Approver = mc.UserApprovers.Split(',').First();
                    }
                    else if (mc.Verifier != "")
                    {
                        expenseClaimVM.Approver = mc.Verifier.Split(',').First();
                        //string VerifierIDs = string.Join(",", ExpenseverifierIDs.Skip(1));
                    }
                    else if (mc.HODApprover != "")
                    {
                        expenseClaimVM.Approver = mc.HODApprover.Split(',').First();
                    }
                    else if (mc.Approver != "")
                    {
                        expenseClaimVM.Approver = mc.Approver.Split(',').First();
                    }
                    else
                    {
                        expenseClaimVM.Approver = "";
                    }

                    if (expenseClaimVM.Approver != "")
                    {
                        var alternateUser = await _alternateApproverHelper.IsAlternateApprovalSetForUser(Convert.ToInt32(expenseClaimVM.Approver));
                        if (alternateUser.HasValue)
                        {
                            var mstUserApprover = await _repository.MstUser.GetUserByIdAsync(alternateUser.Value);
                            expenseClaimVM.Approver = mstUserApprover.Name + " (AA)";
                        }
                        else
                        {
                            var mstUserApprover = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(expenseClaimVM.Approver));
                            expenseClaimVM.Approver = mstUserApprover.Name;
                        }
                    }

                    expenseClaimVMs.Add(expenseClaimVM);
                    _logger.LogInfo($"Returned all Expense Claims with details from database.");
                }

                expenseClaimsVMs.expenseClaims = expenseClaimVMs;

                int userID = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
                int facilityID = Convert.ToInt32(HttpContext.User.FindFirst("delegatefacilityid") is null ? HttpContext.User.FindFirst("facilityid").Value : HttpContext.User.FindFirst("delegatefacilityid").Value);

                var mstExpenseClaimsWithDetailsDrafts = await _repository.MstExpenseClaimDraft.GetAllExpenseClaimDraftsWithDetailsByFacilityIDAsync(userID, facilityID);
                if (mstExpenseClaimsWithDetailsDrafts != null && mstExpenseClaimsWithDetailsDrafts.Any())
                {
                    mstExpenseClaimsWithDetailsDrafts = mstExpenseClaimsWithDetailsDrafts.OrderBy(x => x.CreatedDate).ToList();
                }
                foreach (var mc in mstExpenseClaimsWithDetailsDrafts)
                {
                    ExpenseClaimVM expenseClaimVMDraft = new ExpenseClaimVM();
                    expenseClaimVMDraft.ECID = mc.ECID;
                    expenseClaimVMDraft.ECNo = mc.ECNo;
                    expenseClaimVMDraft.Name = mc.MstUser.Name;
                    //expenseClaimVMDraft.CreatedDate = DateTime.ParseExact(mc.CreatedDate, "MM/dd/yyyy HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture)
                    //                                         .ToString("dd/MM/yyyy", System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
                    expenseClaimVMDraft.FacilityName = mc.MstFacility.FacilityName;
                    expenseClaimVMDraft.Phone = mc.MstUser.Phone;
                    expenseClaimVMDraft.GrandTotal = mc.GrandTotal;
                    expenseClaimVMDraft.ApprovalStatus = mc.ApprovalStatus;
                    expenseClaimVMDraft.ClaimType = mc.ClaimType;
                    expenseClaimVMDraft.TotalAmount = mc.TotalAmount;

                    if (mc.UserApprovers != "")
                    {
                        expenseClaimVMDraft.Approver = mc.UserApprovers.Split(',').First();
                    }
                    else if (mc.Verifier != "")
                    {
                        expenseClaimVMDraft.Approver = mc.Verifier.Split(',').First();
                        //string VerifierIDs = string.Join(",", ExpenseverifierIDs.Skip(1));
                    }
                    else if (mc.HODApprover != "")
                    {
                        expenseClaimVMDraft.Approver = mc.HODApprover.Split(',').First();
                    }
                    else if (mc.Approver != "")
                    {
                        expenseClaimVMDraft.Approver = mc.Approver.Split(',').First();
                    }
                    else
                    {
                        expenseClaimVMDraft.Approver = "";
                    }

                    if (expenseClaimVMDraft.Approver != "")
                    {
                        var mstUserApprover = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(expenseClaimVMDraft.Approver));
                        expenseClaimVMDraft.Approver = mstUserApprover.Name;
                    }

                    expenseClaimsVMs.expenseClaimsDrafts.Add(expenseClaimVMDraft);
                }
                _logger.LogInfo($"Returned all Expense Claims draft with details from database.");

                //expenseClaimsVMs.expenseClaimsDrafts = expenseClaimDraftVMs;
                //var mstExpenseCategoriesWithTypesResult = _mapper.Map<IEnumerable<MstExpenseCategory>>(mstExpenseCategoriesWithTypes);
                return View(expenseClaimsVMs);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Something went wrong inside GetAllExpenseClaimWithDetailsByFacilityIDAsync action: {ex.Message}");
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
                    CloudBlob file = container.GetBlobReference("FileUploads/ExpenseClaimFiles/"+id);

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

        public async Task<ActionResult> DeleteExpenseClaimFile(string fileID, string filepath, string ECID)
        {
            DtExpenseClaimFileUpload dtExpenseClaimFileUpload = new DtExpenseClaimFileUpload();

            if (CloudStorageAccount.TryParse(_configuration.GetSection("ConnectionStrings")["BlobConnectionString"], out CloudStorageAccount storageAccount))
            {
                CloudBlobClient BlobClient = storageAccount.CreateCloudBlobClient();
                CloudBlobContainer container = BlobClient.GetContainerReference(_configuration.GetSection("ConnectionStrings")["BlobContainerName"]);

                if (await container.ExistsAsync())
                {
                    CloudBlob file = container.GetBlobReference("FileUploads/ExpenseClaimFiles/" + filepath);

                    if (await file.ExistsAsync())
                    {
                        await file.DeleteIfExistsAsync();
                        dtExpenseClaimFileUpload = await _repository.DtExpenseClaimFileUpload.GetDtExpenseClaimFileUploadByIdAsync(Convert.ToInt64(fileID));
                        _repository.DtExpenseClaimFileUpload.DeleteDtExpenseClaimFileUpload(dtExpenseClaimFileUpload);
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

            return RedirectToAction("Create", "ExpenseClaim", new
            {
                id = ECID,
                Updatestatus = "Edit"
            });
        }

        public async Task<ActionResult> DeleteExpenseClaimDraftFile(string fileID, string filepath, string ECID)
        {
            DtExpenseClaimFileUploadDraft dtExpenseClaimFileUpload = new DtExpenseClaimFileUploadDraft();

            if (CloudStorageAccount.TryParse(_configuration.GetSection("ConnectionStrings")["BlobConnectionString"], out CloudStorageAccount storageAccount))
            {
                CloudBlobClient BlobClient = storageAccount.CreateCloudBlobClient();
                CloudBlobContainer container = BlobClient.GetContainerReference(_configuration.GetSection("ConnectionStrings")["BlobContainerName"]);

                if (await container.ExistsAsync())
                {
                    CloudBlob file = container.GetBlobReference("FileUploads/ExpenseClaimFiles/" + filepath);

                    if (await file.ExistsAsync())
                    {
                        await file.DeleteIfExistsAsync();
                        dtExpenseClaimFileUpload = await _repository.DtExpenseClaimFileUploadDraft.GetDtExpenseClaimDraftFileUploadByIdAsync(Convert.ToInt64(fileID));

                        _repository.DtExpenseClaimFileUploadDraft.DeleteDtExpenseClaimFileUploadDraft(dtExpenseClaimFileUpload);
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

            return RedirectToAction("CreateDraft", "ExpenseClaim", new
            {
                id = ECID,
                Updatestatus = "Edit"
            });
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
            ExpenseClaimDetailVM expenseClaimDetailVM = new ExpenseClaimDetailVM();
            expenseClaimDetailVM.DtExpenseClaimVMs = new List<DtExpenseClaimVM>();
            expenseClaimDetailVM.ExpenseClaimAudits = new List<ExpenseClaimAuditVM>();

            TempData["claimaddcondition"] = "claimnew";
            long idd = 0;
            if (User != null && User.Identity.IsAuthenticated)
            {
                if (!string.IsNullOrEmpty(id))
                {
                    idd = Convert.ToInt64(id);
                    ViewBag.CID = idd;
                    var dtExpenseClaims = await _repository.DtExpenseClaim.GetDtExpenseClaimByIdAsync(idd);

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
                        dtExpenseClaimVM.GSTPercentage = item.GSTPercentage;
                        dtExpenseClaimVM.AmountWithGST = item.Amount + item.GST;
                        dtExpenseClaimVM.ExpenseCategory = item.MstExpenseCategory.Description;
                        dtExpenseClaimVM.AccountCode = item.AccountCode;
                        if (Updatestatus == "Recreate")
                        {
                            ViewBag.UpdateStatus = "Recreate";
                            dtExpenseClaimVM.ECItemID = 0;
                        }
                        expenseClaimDetailVM.DtExpenseClaimVMs.Add(dtExpenseClaimVM);
                    }

                    expenseClaimDetailVM.ExpenseClaimFileUploads = new List<DtExpenseClaimFileUpload>();
                    var fileUploads= await _repository.DtExpenseClaimFileUpload.GetDtExpenseClaimAuditByIdAsync(idd);
                    if (Updatestatus == "Recreate" && fileUploads != null && fileUploads.Count > 0)
                    {
                        foreach (var uploaddata in fileUploads)
                        {
                            uploaddata.ECID = 0;
                            expenseClaimDetailVM.ExpenseClaimFileUploads.Add(uploaddata);
                        }
                    }
                    else
                        expenseClaimDetailVM.ExpenseClaimFileUploads = fileUploads;

                    var mstExpenseClaim = await _repository.MstExpenseClaim.GetExpenseClaimByIdAsync(idd);

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

                    ExpenseClaimVM expenseClaimVM = new ExpenseClaimVM();
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

                    expenseClaimDetailVM.ExpenseClaimVM = expenseClaimVM;

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
                    expenseClaimDetailVM.ExpenseClaimAudits = new List<ExpenseClaimAuditVM>();
                    expenseClaimDetailVM.ExpenseClaimFileUploads = new List<DtExpenseClaimFileUpload>();
                    ExpenseClaimVM expenseClaimVM = new ExpenseClaimVM();
                    expenseClaimVM.ClaimType = "";
                    expenseClaimVM.GrandTotal = 0;
                    expenseClaimVM.GrandGST = 0;
                    expenseClaimVM.TotalAmount = 0;
                    expenseClaimVM.Company = "";
                    expenseClaimVM.Name = "";
                    expenseClaimVM.DepartmentName = "";
                    expenseClaimVM.FacilityName = "";
                    expenseClaimVM.CreatedDate = "";
                    expenseClaimVM.Verifier = "";
                    expenseClaimVM.Approver = "";
                    expenseClaimVM.ECNo = "";

                    DtExpenseClaimVM dtExpenseClaimVM = new DtExpenseClaimVM();

                    dtExpenseClaimVM.ECItemID = 0;
                    dtExpenseClaimVM.ECID = 0;
                    //dtExpenseClaimVM.DateOfJourney = "";

                    dtExpenseClaimVM.Description = "";
                    dtExpenseClaimVM.Amount = 0;
                    dtExpenseClaimVM.Gst = 0;
                    dtExpenseClaimVM.AmountWithGST = 0;
                    dtExpenseClaimVM.ExpenseCategory = "";
                    dtExpenseClaimVM.AccountCode = "";

                    expenseClaimDetailVM.DtExpenseClaimVMs.Add(dtExpenseClaimVM);
                    expenseClaimDetailVM.ExpenseClaimVM = expenseClaimVM;


                    TempData["status"] = "Add";
                }

                ViewData["ExpenseCategoryID"] = new SelectList(await _repository.MstExpenseCategory.GetAllExpenseCategoriesByClaimTypesAsync("expense/pv-cheque/pv-giro", "active"), "ExpenseCategoryID", "Description");
                var mstUsersWithDetails = await _repository.MstUser.GetUserWithDetailsByIdAsync(Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value));

                SelectList facilities = new SelectList(await _repository.MstFacility.GetAllFacilityAsync("active"), "FacilityID", "FacilityName");
                //int userFacilityId = mstUsersWithDetails.MstFacility.FacilityID;
                int userFacilityId = Convert.ToInt32(HttpContext.User.FindFirst("delegatefacilityid") is null ? HttpContext.User.FindFirst("facilityid").Value : HttpContext.User.FindFirst("delegatefacilityid").Value);
                var userFacility = facilities.Where(x => x.Value == userFacilityId.ToString()).FirstOrDefault();
                if (userFacility != null)
                {
                    facilities.Where(x => x.Value == userFacilityId.ToString()).FirstOrDefault().Selected = true;
                }
                ViewData["FacilityID"] = facilities;

                var currFacility = await _repository.MstFacility.GetFacilityWithDepartmentByIdAsync(userFacilityId);

                var delegatedUserName = string.Empty;
                if(HttpContext.User.FindFirst("delegateuserid") is not null)
                {
                    var delUserDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid").Value));
                    delegatedUserName = delUserDetails.Name;
                }

                ViewData["Name"] = string.IsNullOrEmpty(delegatedUserName) ? User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.GivenName).Value : delegatedUserName + "(" + User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.GivenName).Value + ")";
                ViewData["FacilityName"] = currFacility.FacilityName;
                ViewData["Department"] = currFacility.MstDepartment.Department;
                string pettyCashLimit = _configuration.GetValue<string>("PettyCashLimit");
                ViewBag.PettyCashLimit = pettyCashLimit;
                BindGSTDropdown();

                // Below code is to add original tax value in case of amendment under GST dropdown 
                if (!String.IsNullOrEmpty(id) && Updatestatus == "recreate")
                {
                    var dtPVCClaims = await _repository.DtPVCClaim.GetDtPVCClaimByIdAsync(idd);
                }
            }
            return View(expenseClaimDetailVM);

        }

        public async Task<IActionResult> CreateDraft(string id, string Updatestatus)
        {
            //TempData["CBRID"] = 0;
            TempData["Updatestatus"] = "Add";
            TempData["claimaddcondition"] = "claimDraft";
            ExpenseClaimDetailVM expenseClaimDetailVM = new ExpenseClaimDetailVM();
            expenseClaimDetailVM.DtExpenseClaimVMs = new List<DtExpenseClaimVM>();
            expenseClaimDetailVM.ExpenseClaimAudits = new List<ExpenseClaimAuditVM>();

            if (User != null && User.Identity.IsAuthenticated)
            {
                if (!string.IsNullOrEmpty(id))
                {
                    long idd = Convert.ToInt64(id);
                    ViewBag.CID = idd;
                    var dtExpenseClaims = await _repository.DtExpenseClaimDraft.GetDtExpenseClaimDraftByIdAsync(idd);

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
                        dtExpenseClaimVM.GSTPercentage = item.GSTPercentage;
                        dtExpenseClaimVM.AmountWithGST = item.Amount + item.GST;
                        dtExpenseClaimVM.ExpenseCategory = item.MstExpenseCategory.Description;
                        dtExpenseClaimVM.AccountCode = item.AccountCode;
                        expenseClaimDetailVM.DtExpenseClaimVMs.Add(dtExpenseClaimVM);
                    }

                    var ecFuploads = await _repository.DtExpenseClaimFileUploadDraft.GetDtExpenseClaimDraftAuditByIdAsync(idd);

                    expenseClaimDetailVM.ExpenseClaimFileUploads = new List<DtExpenseClaimFileUpload>();

                    foreach (var item in ecFuploads)
                    {
                        MstExpenseClaim mstExpenseClaim1 = new MstExpenseClaim();
                        if (item.MstExpenseClaimDraft != null)
                        {
                            mstExpenseClaim1 = new MstExpenseClaim()
                            {
                                ApprovalBy = item.MstExpenseClaimDraft.ApprovalBy,
                                ApprovalDate = item.MstExpenseClaimDraft.ApprovalDate,
                                ApprovalStatus = item.MstExpenseClaimDraft.ApprovalStatus,
                                ModifiedDate = item.MstExpenseClaimDraft.ModifiedDate,
                                ModifiedBy = item.MstExpenseClaimDraft.ModifiedBy,
                                Approver = item.MstExpenseClaimDraft.Approver,
                                ClaimType = item.MstExpenseClaimDraft.ClaimType,
                                Company = item.MstExpenseClaimDraft.Company,
                                CreatedBy = item.MstExpenseClaimDraft.CreatedBy,
                                CreatedDate = item.MstExpenseClaimDraft.CreatedDate,
                                DepartmentID = item.MstExpenseClaimDraft.DepartmentID,
                                ECID = item.MstExpenseClaimDraft.ECID,
                                ECNo = item.MstExpenseClaimDraft.ECNo,
                                FacilityID = item.MstExpenseClaimDraft.FacilityID,
                                FinalApprover = item.MstExpenseClaimDraft.FinalApprover,
                                GrandTotal = item.MstExpenseClaimDraft.GrandTotal,
                                HODApprover = item.MstExpenseClaimDraft.HODApprover,
                                MstDepartment = item.MstExpenseClaimDraft.MstDepartment,
                                MstFacility = item.MstExpenseClaimDraft.MstFacility,
                                MstUser = item.MstExpenseClaimDraft.MstUser,
                                TnC = item.MstExpenseClaimDraft.TnC,
                                TotalAmount = item.MstExpenseClaimDraft.TotalAmount,
                                UserApprovers = item.MstExpenseClaimDraft.UserApprovers,
                                UserID = item.MstExpenseClaimDraft.UserID,
                                Verifier = item.MstExpenseClaimDraft.Verifier,
                                VoidReason = item.MstExpenseClaimDraft.VoidReason
                            };
                        }

                        expenseClaimDetailVM.ExpenseClaimFileUploads.Add(new DtExpenseClaimFileUpload()
                        {
                            CreatedBy = item.CreatedBy,
                            CreatedDate = item.CreatedDate,
                            ECID = item.ECID,
                            FileID = item.FileID,
                            FileName = item.FileName,
                            FilePath = item.FilePath,
                            IsDeleted = item.IsDeleted,
                            ModifiedBy = item.ModifiedBy,
                            ModifiedDate = item.ModifiedDate,
                            MstExpenseClaim = mstExpenseClaim1
                        });
                    }

                    var mstExpenseClaim = await _repository.MstExpenseClaimDraft.GetExpenseClaimDraftByIdAsync(idd);

                    ExpenseClaimVM expenseClaimVM = new ExpenseClaimVM();
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

                    expenseClaimDetailVM.ExpenseClaimVM = expenseClaimVM;

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
                    expenseClaimDetailVM.ExpenseClaimAudits = new List<ExpenseClaimAuditVM>();
                    expenseClaimDetailVM.ExpenseClaimFileUploads = new List<DtExpenseClaimFileUpload>();
                    ExpenseClaimVM expenseClaimVM = new ExpenseClaimVM();
                    expenseClaimVM.ClaimType = "";
                    expenseClaimVM.GrandTotal = 0;
                    expenseClaimVM.GrandGST = 0;
                    expenseClaimVM.TotalAmount = 0;
                    expenseClaimVM.Company = "";
                    expenseClaimVM.Name = "";
                    expenseClaimVM.DepartmentName = "";
                    expenseClaimVM.FacilityName = "";
                    expenseClaimVM.CreatedDate = "";
                    expenseClaimVM.Verifier = "";
                    expenseClaimVM.Approver = "";
                    expenseClaimVM.ECNo = "";

                    DtExpenseClaimVM dtExpenseClaimVM = new DtExpenseClaimVM();

                    dtExpenseClaimVM.ECItemID = 0;
                    dtExpenseClaimVM.ECID = 0;
                    //dtExpenseClaimVM.DateOfJourney = "";

                    dtExpenseClaimVM.Description = "";
                    dtExpenseClaimVM.Amount = 0;
                    dtExpenseClaimVM.Gst = 0;
                    dtExpenseClaimVM.AmountWithGST = 0;
                    dtExpenseClaimVM.ExpenseCategory = "";
                    dtExpenseClaimVM.AccountCode = "";

                    expenseClaimDetailVM.DtExpenseClaimVMs.Add(dtExpenseClaimVM);
                    expenseClaimDetailVM.ExpenseClaimVM = expenseClaimVM;


                    TempData["status"] = "Add";
                }

                ViewData["ExpenseCategoryID"] = new SelectList(await _repository.MstExpenseCategory.GetAllExpenseCategoriesByClaimTypesAsync("expense/pv-cheque/pv-giro", "active"), "ExpenseCategoryID", "Description");
                var mstUsersWithDetails = await _repository.MstUser.GetUserWithDetailsByIdAsync(Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value));

                SelectList facilities = new SelectList(await _repository.MstFacility.GetAllFacilityAsync("active"), "FacilityID", "FacilityName");
                //int userFacilityId = mstUsersWithDetails.MstFacility.FacilityID;
                int userFacilityId = Convert.ToInt32(User.Claims.FirstOrDefault(c => c.Type == "facilityid").Value);
                var userFacility = facilities.Where(x => x.Value == userFacilityId.ToString()).FirstOrDefault();
                if (userFacility != null)
                {
                    facilities.Where(x => x.Value == userFacilityId.ToString()).FirstOrDefault().Selected = true;
                }
                ViewData["FacilityID"] = facilities;
                var currFacility = await _repository.MstFacility.GetFacilityWithDepartmentByIdAsync(userFacilityId);
                ViewData["Name"] = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.GivenName).Value;
                ViewData["FacilityName"] = currFacility.FacilityName;
                ViewData["Department"] = currFacility.MstDepartment.Department;
                string pettyCashLimit = _configuration.GetValue<string>("PettyCashLimit");
                ViewBag.PettyCashLimit = pettyCashLimit;
                BindGSTDropdown();
            }
            return View("Create", expenseClaimDetailVM);

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
            List<DtExpenseClaimVM> oDtClaimsList = new List<DtExpenseClaimVM>();

            try
            {
                var dtExpenseClaims = await _repository.DtExpenseClaim.GetDtExpenseClaimByIdAsync(Convert.ToInt64(id));

                // List<clsDtSupplierPO> oclsDtSupplierPO = new List<clsDtSupplierPO>();
                foreach (var item in dtExpenseClaims)
                {
                    DtExpenseClaimVM dtExpenseClaimVM = new DtExpenseClaimVM();

                    dtExpenseClaimVM.ECItemID = item.ECItemID;
                    dtExpenseClaimVM.ECID = item.ECID;
                    dtExpenseClaimVM.DateOfJourney = item.Date;
                    dtExpenseClaimVM.FacilityID = item.FacilityID;
                    dtExpenseClaimVM.Description = item.Description;
                    dtExpenseClaimVM.Amount = item.Amount;
                    dtExpenseClaimVM.Gst = item.GST;
                    dtExpenseClaimVM.AmountWithGST = item.Amount + item.GST;
                    dtExpenseClaimVM.ExpenseCategoryID = item.ExpenseCategoryID;
                    dtExpenseClaimVM.AccountCode = item.AccountCode;
                    oDtClaimsList.Add(dtExpenseClaimVM);
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
            List<DtExpenseClaimVM> oDtClaimsList = new List<DtExpenseClaimVM>();

            try
            {
                var dtExpenseClaims = await _repository.DtExpenseClaimDraft.GetDtExpenseClaimDraftByIdAsync(Convert.ToInt64(id));

                // List<clsDtSupplierPO> oclsDtSupplierPO = new List<clsDtSupplierPO>();
                foreach (var item in dtExpenseClaims)
                {
                    DtExpenseClaimVM dtExpenseClaimVM = new DtExpenseClaimVM();

                    dtExpenseClaimVM.ECItemID = item.ECItemID;
                    dtExpenseClaimVM.ECID = item.ECID;
                    dtExpenseClaimVM.DateOfJourney = item.Date;
                    dtExpenseClaimVM.FacilityID = item.FacilityID;
                    dtExpenseClaimVM.Description = item.Description;
                    dtExpenseClaimVM.Amount = item.Amount;
                    dtExpenseClaimVM.Gst = item.GST;
                    dtExpenseClaimVM.AmountWithGST = item.Amount + item.GST;
                    dtExpenseClaimVM.ExpenseCategoryID = item.ExpenseCategoryID;
                    dtExpenseClaimVM.AccountCode = item.AccountCode;
                    oDtClaimsList.Add(dtExpenseClaimVM);
                }
                return Json(new { DtClaimsList = oDtClaimsList });
            }
            catch
            {
                return Json(new { DtClaimsList = oDtClaimsList });
            }

        }


        public async Task<IActionResult> Details(long? id, string userID, string facilityID)
        {
            if (id == null)
            {
                return NotFound();
            }
            long ECID = Convert.ToInt64(id);

            if (User != null && User.Identity.IsAuthenticated)
            {
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
                    dtExpenseClaimVM.GSTPercentage = item.GSTPercentage;
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
                var GroupByQS = expenseClaimDetailVM.DtExpenseClaimVMs.GroupBy(s => s.AccountCode);
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

                if(mstExpenseClaim.Verifier == mstExpenseClaim.DVerifier && mstExpenseClaim.Approver == mstExpenseClaim.DApprover && mstExpenseClaim.UserApprovers == mstExpenseClaim.DUserApprovers && mstExpenseClaim.HODApprover == mstExpenseClaim.DHODApprover)
                {
                    ViewBag.UserEditStatus = 4;
                }
                else
                {
                    ViewBag.UserEditStatus = 0;
                }
                TempData["ApprovedStatus"] = mstExpenseClaim.ApprovalStatus;
                TempData["FinalApproverID"] = mstExpenseClaim.FinalApprover;
                ViewBag.VoidReason = mstExpenseClaim.VoidReason == null ? "" : mstExpenseClaim.VoidReason;

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

                        if (TempData["ApprovedStatus"].ToString() == "-5" && TempData["FinalApproverID"].ToString() != (HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value.ToString() : HttpContext.User.FindFirst("delegateuserid").Value.ToString()))
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
                if (mstExpenseClaim.Verifier != "")
                {
                    string[] verifierIDs = mstExpenseClaim.Verifier.Split(',');
                    TempData["QueryMCVerifierIDs"] = string.Join(",", verifierIDs);
                    foreach (string verifierID in verifierIDs)
                    {
                        if (verifierID != "" && verifierID == (HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value) && User.IsInRole("Finance"))
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
                    TempData["VerifierIDs"] = mstExpenseClaim.Verifier;
                    TempData["ApproverIDs"] = mstExpenseClaim.Approver;
                }

                //Approval Process code
                if (mstExpenseClaim.Approver != "" && mstExpenseClaim.Verifier == "")
                {
                    string[] approverIDs = mstExpenseClaim.Approver.Split(',');
                    TempData["QueryMCApproverIDs"] = string.Join(",", approverIDs);
                    foreach (string approverID in approverIDs)
                    {
                        if (approverID != "" && approverID == (HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value) && User.IsInRole("Finance"))
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


                if (mstExpenseClaim.UserApprovers != "" && mstExpenseClaim.Verifier == "")
                {
                    string[] userApproverIDs = mstExpenseClaim.UserApprovers.Split(',');
                    TempData["QueryMCUserApproverIDs"] = string.Join(",", userApproverIDs);
                    foreach (string approverID in userApproverIDs)
                    {
                        if (approverID != "" && approverID == (HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value))
                        {
                            TempData["ApprovedStatus"] = mstExpenseClaim.ApprovalStatus;
                            //Session["ApprovedStatus"] = oSupplierPO.Approvalstatus;
                            TempData["HODApproverIDs"] = string.Join(",", userApproverIDs.Skip(1));
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

                if (mstExpenseClaim.HODApprover != "" && mstExpenseClaim.Verifier == "")
                {
                    string[] hodApproverIDs = mstExpenseClaim.HODApprover.Split(',');
                    TempData["QueryMCHODApproverIDs"] = string.Join(",", hodApproverIDs);
                    foreach (string approverID in hodApproverIDs)
                    {
                        if (approverID != "" && approverID == (HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value))
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


                BindGSTDropdown();
                return View(expenseClaimDetailVM);
            }
            else
            {
                return Redirect("~/Login/Login");
            }
        }

        public async Task<IActionResult> DraftDetails(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            long ECID = Convert.ToInt64(id);

            if (User != null && User.Identity.IsAuthenticated)
            {
                var mstExpenseClaim = await _repository.MstExpenseClaimDraft.GetExpenseClaimDraftByIdAsync(id);

                if (mstExpenseClaim == null)
                {
                    return NotFound();
                }

                var dtExpenseSummaries = await _repository.DtExpenseClaimSummaryDraft.GetDtExpenseClaimSummaryDraftByIdAsync(id);

                var dtExpenseClaims = await _repository.DtExpenseClaimDraft.GetDtExpenseClaimDraftByIdAsync(id);
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

                expenseClaimDetailVM.DtExpenseClaimSummaries = new List<DtExpenseClaimSummary>();

                foreach (var item in dtExpenseSummaries)
                {
                    expenseClaimDetailVM.DtExpenseClaimSummaries.Add(new DtExpenseClaimSummary()
                    {
                        AccountCode = item.AccountCode,
                        Amount = item.Amount,
                        AmountWithGST = item.AmountWithGST,
                        CItemID = item.CItemID,
                        Date = item.Date,
                        Description = item.Description,
                        ECID = item.ECID,
                        ExpenseCategory = item.ExpenseCategory,
                        GST = item.GST,
                        MstExpenseClaim = item.MstExpenseClaim,
                        TaxClass = item.TaxClass
                    });
                }

                var GroupByQS = expenseClaimDetailVM.DtExpenseClaimVMs.GroupBy(s => s.AccountCode);
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

                //var dtExpenseClaimAudits = await _repository.MstExpenseClaimAudit.GetMstExpenseClaimAuditByIdAsync(id);

                //foreach (var item in dtExpenseClaimAudits)
                //{
                //    ExpenseClaimAuditVM mstExpenseClaimAuditVM = new ExpenseClaimAuditVM();
                //    mstExpenseClaimAuditVM.Action = item.Action;
                //    mstExpenseClaimAuditVM.Description = item.Description;
                //    mstExpenseClaimAuditVM.AuditDateTickle = Helper.RelativeDate(item.AuditDate);
                //    expenseClaimDetailVM.ExpenseClaimAudits.Add(mstExpenseClaimAuditVM);
                //}

                expenseClaimDetailVM.ExpenseClaimFileUploads = new List<DtExpenseClaimFileUpload>();

                var ecFileUploads = _repository.DtExpenseClaimFileUploadDraft.GetDtExpenseClaimDraftAuditByIdAsync(id).Result.ToList();

                foreach (var item in ecFileUploads)
                {
                    MstExpenseClaim mstExpenseClaim1 = new MstExpenseClaim();
                    if (item.MstExpenseClaimDraft != null)
                    {
                        mstExpenseClaim1 = new MstExpenseClaim()
                        {
                            ApprovalBy = item.MstExpenseClaimDraft.ApprovalBy,
                            ApprovalDate = item.MstExpenseClaimDraft.ApprovalDate,
                            ApprovalStatus = item.MstExpenseClaimDraft.ApprovalStatus,
                            ModifiedDate = item.MstExpenseClaimDraft.ModifiedDate,
                            ModifiedBy = item.MstExpenseClaimDraft.ModifiedBy,
                            Approver = item.MstExpenseClaimDraft.Approver,
                            ClaimType = item.MstExpenseClaimDraft.ClaimType,
                            Company = item.MstExpenseClaimDraft.Company,
                            CreatedBy = item.MstExpenseClaimDraft.CreatedBy,
                            CreatedDate = item.MstExpenseClaimDraft.CreatedDate,
                            DepartmentID = item.MstExpenseClaimDraft.DepartmentID,
                            ECID = item.MstExpenseClaimDraft.ECID,
                            ECNo = item.MstExpenseClaimDraft.ECNo,
                            FacilityID = item.MstExpenseClaimDraft.FacilityID,
                            FinalApprover = item.MstExpenseClaimDraft.FinalApprover,
                            GrandTotal = item.MstExpenseClaimDraft.GrandTotal,
                            HODApprover = item.MstExpenseClaimDraft.HODApprover,
                            MstDepartment = item.MstExpenseClaimDraft.MstDepartment,
                            MstFacility = item.MstExpenseClaimDraft.MstFacility,
                            MstUser = item.MstExpenseClaimDraft.MstUser,
                            TnC = item.MstExpenseClaimDraft.TnC,
                            TotalAmount = item.MstExpenseClaimDraft.TotalAmount,
                            UserApprovers = item.MstExpenseClaimDraft.UserApprovers,
                            UserID = item.MstExpenseClaimDraft.UserID,
                            Verifier = item.MstExpenseClaimDraft.Verifier,
                            VoidReason = item.MstExpenseClaimDraft.VoidReason
                        };
                    }

                    expenseClaimDetailVM.ExpenseClaimFileUploads.Add(new DtExpenseClaimFileUpload()
                    {
                        CreatedBy = item.CreatedBy,
                        CreatedDate = item.CreatedDate,
                        ECID = item.ECID,
                        FileID = item.FileID,
                        FileName = item.FileName,
                        FilePath = item.FilePath,
                        IsDeleted = item.IsDeleted,
                        ModifiedBy = item.ModifiedBy,
                        ModifiedDate = item.ModifiedDate,
                        MstExpenseClaim = mstExpenseClaim1
                    });
                }

                ExpenseClaimVM expenseClaimVM = new ExpenseClaimVM();
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


                TempData["ApprovedStatus"] = mstExpenseClaim.ApprovalStatus;
                TempData["FinalApproverID"] = mstExpenseClaim.FinalApprover;
                ViewBag.VoidReason = mstExpenseClaim.VoidReason == null ? "" : mstExpenseClaim.VoidReason;

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
                if (mstExpenseClaim.Verifier != "")
                {
                    string[] verifierIDs = mstExpenseClaim.Verifier.Split(',');
                    TempData["QueryMCVerifierIDs"] = string.Join(",", verifierIDs);
                    foreach (string verifierID in verifierIDs)
                    {
                        if (verifierID != "" && verifierID == (HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value) && User.IsInRole("Finance"))
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
                    TempData["VerifierIDs"] = mstExpenseClaim.Verifier;
                    TempData["ApproverIDs"] = mstExpenseClaim.Approver;
                }

                //Approval Process code
                if (mstExpenseClaim.Approver != "" && mstExpenseClaim.Verifier == "")
                {
                    string[] approverIDs = mstExpenseClaim.Approver.Split(',');
                    TempData["QueryMCApproverIDs"] = string.Join(",", approverIDs);
                    foreach (string approverID in approverIDs)
                    {
                        if (approverID != "" && approverID == (HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value) && User.IsInRole("Finance"))
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


                if (mstExpenseClaim.UserApprovers != "" && mstExpenseClaim.Verifier == "")
                {
                    string[] userApproverIDs = mstExpenseClaim.UserApprovers.Split(',');
                    TempData["QueryMCUserApproverIDs"] = string.Join(",", userApproverIDs);
                    foreach (string approverID in userApproverIDs)
                    {
                        if (approverID != "" && approverID == (HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value))
                        {
                            TempData["ApprovedStatus"] = mstExpenseClaim.ApprovalStatus;
                            //Session["ApprovedStatus"] = oSupplierPO.Approvalstatus;
                            TempData["HODApproverIDs"] = string.Join(",", userApproverIDs.Skip(1));
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

                if (mstExpenseClaim.HODApprover != "" && mstExpenseClaim.Verifier == "")
                {
                    string[] hodApproverIDs = mstExpenseClaim.HODApprover.Split(',');
                    TempData["QueryMCHODApproverIDs"] = string.Join(",", hodApproverIDs);
                    foreach (string approverID in hodApproverIDs)
                    {
                        if (approverID != "" && approverID == (HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value))
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
                //var mstExpenseClaimAudits = await _repository.MstExpenseClaimAudit.GetMstExpenseClaimAuditByIdAsync(ECID);
                //var AuditIDs = mstExpenseClaimAudits.Select(m => m.AuditBy.ToString()).Distinct();
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


                expenseClaimDetailVM.ExpenseClaimVM = expenseClaimVM;
                //mileageClaimDetailVM.DtMileageClaimVMs = dtMileageClaimVMs;



                return View("Details", expenseClaimDetailVM);
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
            long ECID = Convert.ToInt64(id);
            ExpenseClaimDetailVM expenseClaimDetailVM = new ExpenseClaimDetailVM();
            if (User != null && User.Identity.IsAuthenticated)
            {
                var mstExpenseClaim = await _repository.MstExpenseClaim.GetExpenseClaimByIdAsync(id);

                if (mstExpenseClaim == null)
                {
                    return NotFound();
                }

                var dtExpenseSummaries = await _repository.DtExpenseClaimSummary.GetDtExpenseClaimSummaryByIdAsync(id);
                var dtExpenseClaims = await _repository.DtExpenseClaim.GetDtExpenseClaimByIdAsync(id);

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
                    string AccountCode = string.Empty;
                    foreach (var dtExpense in group)
                    {
                        amount = amount + dtExpense.Amount;
                        gst = gst + dtExpense.Gst;
                        sumamount = sumamount + dtExpense.AmountWithGST;
                        ExpenseDesc = dtExpense.ExpenseCategory;
                        AccountCode = dtExpense.AccountCode;
                    }
                    gst = gst / group.Count();
                    dtExpenseClaimVM.ExpenseCategory = ExpenseDesc;
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
                expenseClaimVM.ClaimType = mstExpenseClaim.ClaimType;
                expenseClaimVM.GrandTotal = mstExpenseClaim.GrandTotal;
                expenseClaimVM.TotalAmount = mstExpenseClaim.TotalAmount;
                expenseClaimVM.GrandGST = mstExpenseClaim.TotalAmount - mstExpenseClaim.GrandTotal;
                expenseClaimVM.Company = mstExpenseClaim.Company;
                expenseClaimVM.Name = mstExpenseClaim.MstUser.Name;
                expenseClaimVM.DepartmentName = mstExpenseClaim.MstDepartment.Department;
                expenseClaimVM.FacilityName = mstExpenseClaim.MstFacility.FacilityName;
                expenseClaimVM.CreatedDate = Convert.ToDateTime(mstExpenseClaim.CreatedDate).ToString("dd/MM/yyyy", System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
                expenseClaimVM.Verifier = mstExpenseClaim.Verifier;
                expenseClaimVM.Approver = mstExpenseClaim.Approver;
                expenseClaimVM.ECNo = mstExpenseClaim.ECNo;
                expenseClaimVM.VoucherNo = mstExpenseClaim.VoucherNo;
                ViewBag.ECID = id;
                expenseClaimDetailVM.ExpenseClaimVM = expenseClaimVM;
                //mileageClaimDetailVM.DtMileageClaimVMs = dtMileageClaimVMs;
            }
            return PartialView("GetExpenseDetailsPrint", expenseClaimDetailVM);
        }

        public async Task<JsonResult> UpdateStatusforVoid(string id, string reason, string approvedStatus)
        {
            if (User != null && User.Identity.IsAuthenticated)
            {
                int ECID = Convert.ToInt32(id);

                var mstExpenseClaim = await _repository.MstExpenseClaim.GetExpenseClaimByIdAsync(ECID);

                if (mstExpenseClaim == null)
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
                    await _repository.MstExpenseClaim.UpdateMstExpenseClaimStatus(ECID, -5, int.Parse(HttpContext.User.FindFirst("userid").Value), DateTime.Now, reason, string.Empty, string.Empty, string.Empty, string.Empty, isAlternateApprover, 0);
                }
                else
                {
                    await _repository.MstExpenseClaim.UpdateMstExpenseClaimStatus(ECID, 5, int.Parse(HttpContext.User.FindFirst("userid").Value), DateTime.Now, reason, string.Empty, string.Empty, string.Empty, string.Empty, isAlternateApprover, 0);
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
                int ECID = Convert.ToInt32(id);

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

                await _repository.MstExpenseClaim.UpdateMstExpenseClaimStatus(ECID, 4, int.Parse(HttpContext.User.FindFirst("userid").Value), DateTime.Now, reason, string.Empty, string.Empty, string.Empty, string.Empty, isAlternateApprover, 0);

                return Json(new { res = "Done" });
            }
            else
            {
                return Json(new { res = "Done" });
            }
        }

        public async Task<IActionResult> DeleteExpenseDraft(string id)
        {
            try
            {
                long idd = Convert.ToInt64(id);
                var expenseClaimsDraft = await _repository.MstExpenseClaimDraft.GetExpenseClaimDraftByIdAsync(idd);
                _repository.MstExpenseClaimDraft.DeleteExpenseClaimDraft(expenseClaimsDraft);
                await _repository.SaveAsync();
                TempData["Message"] = "Draft deleted successfully";
                Content("<script language='javascript' type='text/javascript'>alert('Draft deleted successfully');</script>");
                return RedirectToAction("Index", "ExpenseClaim");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Something went wrong inside DeleteExpenseDraft action: {ex.Message}");
            }
            return Json(null);
        }


        public FileResult ExcelDownload()
        {
            /*
            DataTable dt = new DataTable("Grid");
            dt.Columns.AddRange(new DataColumn[9] {new DataColumn("Claimid"),
                                            new DataColumn("Username"),
                                            new DataColumn("Claim Type"),
                                            new DataColumn("Date"),
                                            new DataColumn("Facility"),
                                            new DataColumn("Description of Expense"),
                                            new DataColumn("Amount"),
                                            new DataColumn("GST"),
                                             new DataColumn("Expense Category")});
            using (XLWorkbook wb = new XLWorkbook())
            {
                wb.Worksheets.Add(dt);
                using (MemoryStream stream = new MemoryStream())
                {
                    wb.SaveAs(stream);
                    return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "ExpenseTemplate.xlsx");
                }
            }
            */
            string id = "ExpenseTemplate.xlsm";

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
                DataTable dt = new DataTable();
                dt = ExcelPackageToDataTable(package);
                //conString = string.Format(conString, filePath);
                /*
                //Open the Excel file in Read Mode using OpenXml.
                using (SpreadsheetDocument doc = SpreadsheetDocument.Open(filePath, false))
                {
                    //Read the first Sheets from Excel file.
                    Sheet sheet = doc.WorkbookPart.Workbook.Sheets.GetFirstChild<Sheet>();

                    //Get the Worksheet instance.
                    Worksheet worksheet = (doc.WorkbookPart.GetPartById(sheet.Id.Value) as WorksheetPart).Worksheet;

                    //Fetch all the rows present in the Worksheet.
                    IEnumerable<Row> rows = worksheet.GetFirstChild<SheetData>().Descendants<Row>();

                    //Create a new DataTable.
                    //DataTable dt = new DataTable();

                    //Loop through the Worksheet rows.
                    foreach (Row row in rows)
                    {
                        //Use the first row to add columns to DataTable
                        if (row.RowIndex.Value == 1)
                        {
                            foreach (Cell cell in row.Descendants<Cell>())
                            {
                                dt.Columns.Add(GetValue(doc, cell));
                            }
                        }
                        else
                        {
                            //Add rows to DataTable.
                            dt.Rows.Add();
                            int i = 0;
                            foreach (Cell cell in row.Descendants<Cell>())
                            {
                                dt.Rows[dt.Rows.Count - 1][i] = GetValue(doc, cell);
                                i++;
                            }
                        }
                    }
                }

                DataRow[] drows = dt.Select();

                for (int i = 0; i < drows.Length; i++)
                {
                    dt.Rows[i]["UserName"] = User.FindFirstValue("username");
                    dt.Rows[i].EndEdit();
                    dt.AcceptChanges();
                }
                */

                //using (OleDbConnection connExcel = new OleDbConnection(conString))
                //{
                //    using (OleDbCommand cmdExcel = new OleDbCommand())
                //    {
                //        using (OleDbDataAdapter odaExcel = new OleDbDataAdapter())
                //        {
                //            cmdExcel.Connection = connExcel;

                //            //Get the name of First Sheet.
                //            connExcel.Open();
                //            DataTable dtExcelSchema;
                //            dtExcelSchema = connExcel.GetOleDbSchemaTable(OleDbSchemaGuid.Tables, null);
                //            string sheetName = dtExcelSchema.Rows[0]["TABLE_NAME"].ToString();
                //            connExcel.Close();

                //            //Read Data from First Sheet.
                //            connExcel.Open();
                //            cmdExcel.CommandText = "SELECT * From [" + sheetName + "]";
                //            odaExcel.SelectCommand = cmdExcel;
                //            odaExcel.Fill(dt);
                //            connExcel.Close();

                //            //int userid = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value);

                //            //var mstUser = _repository.MstUser.GetUserByID(userid);


                //            DataRow[] drows = dt.Select();

                //            for (int i = 0; i < drows.Length; i++)
                //            {
                //                dt.Rows[i]["UserName"] = User.FindFirstValue("username");
                //                dt.Rows[i].EndEdit();
                //                dt.AcceptChanges();
                //            }
                //        }
                //    }
                //}

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

                        //cmd = new SqlCommand("delete from MstExpenseClaimtemp", con);
                        con.Open();
                        //cmd.ExecuteNonQuery();

                        sqlBulkCopy.DestinationTableName = "dbo.MstExpenseClaimtemp";

                        sqlBulkCopy.ColumnMappings.Add("UserName", "UserName");
                        //sqlBulkCopy.ColumnMappings.Add("EmailAddress", "EmailAddress");
                        //sqlBulkCopy.ColumnMappings.Add("Company", "Company");
                        //sqlBulkCopy.ColumnMappings.Add("Department", "Department");
                        //sqlBulkCopy.ColumnMappings.Add("Facility", "Facility");
                        //sqlBulkCopy.ColumnMappings.Add("DateofCreated", "DateofCreated");
                        sqlBulkCopy.ColumnMappings.Add("Claim Type", "ClaimType");
                        sqlBulkCopy.ColumnMappings.Add("Date", "DateofJourney");
                        sqlBulkCopy.ColumnMappings.Add("Facility", "Facility");
                        sqlBulkCopy.ColumnMappings.Add("Description of Expense", "DescriptionofExpense");
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

                DataTable InvaildData = _repository.MstExpenseClaim.InsertExcel(Convert.ToInt32((HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value)), Convert.ToInt32(HttpContext.User.FindFirst("userid").Value));

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
                        var mstExpenseClaim = await _repository.MstExpenseClaim.GetExpenseClaimByIdAsync(cid);
                        if (mstExpenseClaim.ApprovalStatus == 1)
                        {
                            string VerifierIDs = "";
                            string ApproverIDs = "";
                            string UserApproverIDs = "";
                            string HODApproverID = "";
                            try
                            {
                                //VerifierIDs = mstExpenseClaim.Verifier.Split(',');
                                //VerifierIDs = string.Join(",", ExpenseverifierIDs.Skip(1));
                                string[] verifierIDs = mstExpenseClaim.Verifier.Split(',');
                                ApproverIDs = mstExpenseClaim.Approver;
                                HODApproverID = mstExpenseClaim.HODApprover;



                                //BackgroundJob.Enqueue(() => _sendMailServices.SendEmail());
                                //Mail Code Implementation for Verifiers

                                foreach (string verifierID in verifierIDs)
                                {
                                    if (verifierID != "")
                                    {
                                        string domainUrl = HttpContext.Request.Scheme + "://" + HttpContext.Request.Host;
                                        string clickUrl = domainUrl + "/" + "FinanceExpenseClaim/Details/" + mstExpenseClaim.ECID;

                                        var mstSenderDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value));
                                        var senderName = mstSenderDetails.Name;
                                        var mstVerifierDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(verifierID));
                                        var toEmail = mstVerifierDetails.EmailAddress;
                                        var receiverName = mstVerifierDetails.Name;
                                        var claimNo = mstExpenseClaim.ECNo;
                                        var screen = "Expense Claim";
                                        var approvalType = "Verification Request";
                                        int userID = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
                                        var subject = "Expense Claim for Verification " + claimNo;

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
                            string[] userApproverIDs = mstExpenseClaim.UserApprovers.ToString().Split(',');
                            foreach (string userApproverID in userApproverIDs)
                            {
                                if (userApproverID != "")
                                {
                                    string domainUrl = HttpContext.Request.Scheme + "://" + HttpContext.Request.Host;
                                    string clickUrl = domainUrl + "/" + "HodSummary/ECDetails/" + mstExpenseClaim.ECID;

                                    var mstSenderDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value));
                                    var senderName = mstSenderDetails.Name;
                                    var mstVerifierDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(userApproverID));
                                    var toEmail = mstVerifierDetails.EmailAddress;
                                    var receiverName = mstVerifierDetails.Name;
                                    var claimNo = mstExpenseClaim.ECNo;
                                    var screen = "Expense Claim";
                                    var approvalType = "Approval Request";
                                    int userID = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
                                    var subject = "Expense Claim for Approval " + claimNo;

                                    BackgroundJob.Enqueue(() => _sendMailServices.SendEmail("EmailTemplate.html", screen, subject, senderName, receiverName, claimNo, approvalType, userID, toEmail, clickUrl));
                                }
                                break;
                            }
                        }
                    }
                    if (count == 0)
                    {
                        Content("<script language='javascript' type='text/javascript'>alert('File has imported.Please check the downloaded file.');</script>");
                        _toastNotification.AddSuccessToastMessage("File has imported.Please check the downloaded file");
                        return RedirectToAction("Index", "ExpenseClaim", "File has imported.Please check the downloaded file.");

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
                                return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "ExpenseTemplateValidate.xlsx");
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

            return RedirectToAction("Index", "ExpenseClaim");

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
                if (columnName == "Date")
                {
                    DataColumn colDateTime = new DataColumn("Date");
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

                //loop all cells in the row
                foreach (var cell in row)
                {
                    if (cell.Address.Contains("C"))
                    {
                        if (cell.Text != string.Empty)
                            newRow[cell.Start.Column - 1] = DateTime.Parse(cell.Text, new System.Globalization.CultureInfo("pt-BR"));
                    }
                    else if (cell.Address.Contains("G") || cell.Address.Contains("H") || cell.Address.Contains("I"))
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

        private string GetValue(SpreadsheetDocument doc, Cell cell)
        {
            string value = cell.CellValue.InnerText;
            if (cell.DataType != null && cell.DataType.Value == CellValues.SharedString)
            {
                return doc.WorkbookPart.SharedStringTablePart.SharedStringTable.ChildElements.GetItem(int.Parse(value)).InnerText;
            }
            else if (cell.CellReference.Value.StartsWith("D"))
            {
                return DateTime.FromOADate(double.Parse(value)).ToShortDateString();
            }
            return value;
        }


        [HttpPost]
        public async Task<JsonResult> SaveItems(string data)
        {
            //var expenseClaimViewModel = JsonConvert.DeserializeObject<ExpenseClaimViewModel>(data,
            //    new IsoDateTimeConverter { DateTimeFormat = "dd/MM/yyyy" });   

            var expenseClaimViewModel = JsonConvert.DeserializeObject<ExpenseClaimViewModel>(data);

            var mstFacility = await _repository.MstFacility.GetFacilityWithDepartmentByIdAsync(Convert.ToInt32(HttpContext.User.FindFirst("delegatefacilityid") is null ? HttpContext.User.FindFirst("facilityid").Value : HttpContext.User.FindFirst("delegatefacilityid").Value));



            MstExpenseClaim mstExpenseClaim = new MstExpenseClaim();
            mstExpenseClaim.ECNo = expenseClaimViewModel.ECNo;
            mstExpenseClaim.UserID = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
            mstExpenseClaim.ClaimType = expenseClaimViewModel.ClaimType;
            mstExpenseClaim.Verifier = "";
            mstExpenseClaim.Approver = "";
            mstExpenseClaim.FinalApprover = "";
            mstExpenseClaim.ApprovalStatus = 1;
            mstExpenseClaim.GrandTotal = expenseClaimViewModel.GrandTotal;
            mstExpenseClaim.TotalAmount = expenseClaimViewModel.TotalAmount;
            mstExpenseClaim.Company = expenseClaimViewModel.Company;
            mstExpenseClaim.FacilityID = Convert.ToInt32(HttpContext.User.FindFirst("delegatefacilityid") is null ? HttpContext.User.FindFirst("facilityid").Value : HttpContext.User.FindFirst("delegatefacilityid").Value);
            mstExpenseClaim.DepartmentID = mstFacility.MstDepartment.DepartmentID;
            mstExpenseClaim.CreatedDate = DateTime.Now;
            mstExpenseClaim.ModifiedDate = DateTime.Now;
            mstExpenseClaim.CreatedBy = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value); // Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
            mstExpenseClaim.ModifiedBy = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value); //Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
            mstExpenseClaim.ApprovalDate = DateTime.Now;
            mstExpenseClaim.ApprovalBy = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
            mstExpenseClaim.DelegatedBy = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? 0 : HttpContext.User.FindFirst("delegateuserid").Value);
            mstExpenseClaim.TnC = true;

            foreach (var dtItem in expenseClaimViewModel.dtClaims)
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
            long ECID = 0;
            try
            {
                //CBRID = Convert.ToInt32(Session["CBRID"].ToString());
                ECID = Convert.ToInt64(expenseClaimViewModel.ECID);
                if (ECID == 0 || TempData["Updatestatus"].ToString() == "Recreate")
                {
                    ClaimStatus = "Recreate";
                    ECID = 0;
                }
                else if (ECID == 0)
                    ClaimStatus = "Add";
                else
                {                   
                    ClaimStatus = "Update";
                }                    

                if (expenseClaimViewModel.ClaimAddCondition == "claimDraft")
                {
                    mstExpenseClaim.ECID = 0;
                }
                else
                {
                    mstExpenseClaim.ECID = ECID;
                }
                //mstExpenseClaim.ECNo = expenseClaimViewModel.;             
            }
            catch { }

            ExpenseClaimDetailVM expenseClaimDetailVM = new ExpenseClaimDetailVM();
            //List<DtMileageClaimVM> dtMileageClaimVMs = new List<DtMileageClaimVM>();
            expenseClaimDetailVM.DtExpenseClaimVMs = new List<DtExpenseClaimVM>();
            // List<clsDtSupplierPO> oclsDtSupplierPO = new List<clsDtSupplierPO>();
            foreach (var item in expenseClaimViewModel.dtClaims)
            {
                DtExpenseClaimVM dtExpenseClaimVM = new DtExpenseClaimVM();
                if (ECID == 0 || TempData["Updatestatus"].ToString() == "Recreate")
                {
                    dtExpenseClaimVM.ECItemID = 0;
                    dtExpenseClaimVM.ECID = 0;
                }
                dtExpenseClaimVM.FacilityID = item.FacilityID;
                //dtExpenseClaimVM.Payee = item.Payee;
                //dtExpenseClaimVM.Particulars = item.Particulars;
                dtExpenseClaimVM.Description = item.Description;
                dtExpenseClaimVM.ExpenseCategory = item.MstExpenseCategory.Description;
                dtExpenseClaimVM.ExpenseCategoryID = item.MstExpenseCategory.ExpenseCategoryID;
                //dtExpenseClaimVM.Reason = item.Reason;
                //dtExpenseClaimVM.EmployeeNo = item.EmployeeNo;
                //dtExpenseClaimVM.ChequeNo = item.ChequeNo;
                dtExpenseClaimVM.Amount = item.Amount;
                dtExpenseClaimVM.Gst = item.GST;
                dtExpenseClaimVM.GSTPercentage = item.GSTPercentage;
                dtExpenseClaimVM.AmountWithGST = item.Amount + item.GST;
                //dtExpenseClaimVM.Facility = item.Facility;
                dtExpenseClaimVM.AccountCode = item.AccountCode;
                dtExpenseClaimVM.DateOfJourney = item.Date;
                expenseClaimDetailVM.DtExpenseClaimVMs.Add(dtExpenseClaimVM);
            }

            var GroupByQS = expenseClaimDetailVM.DtExpenseClaimVMs.GroupBy(s => new { s.AccountCode, s.ExpenseCategory, s.FacilityID, s.Gst });

            expenseClaimDetailVM.DtExpenseClaimVMSummary = new List<DtExpenseClaimVM>();

            foreach (var group in GroupByQS)
            {
                DtExpenseClaimVM dtExpenseClaimVM = new DtExpenseClaimVM();
                decimal amount = 0;
                decimal gst = 0;
                decimal gstpercentage = 0;
                decimal sumamount = 0;
                string ExpenseDesc = string.Empty;
                string ExpenseCat = string.Empty;
                string Facility = string.Empty;
                string AccountCode = string.Empty;
                int? ExpenseCatID = 0;
                int? facilityID = 0;
                int i = 0;
                foreach (var dtExpense in group)
                {
                    if (i == 0)
                        ExpenseDesc = dtExpense.Description;
                    i++;
                    amount = amount + dtExpense.Amount;
                    gst = gst + dtExpense.Gst;
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
                dtExpenseClaimVM.Description = ExpenseDesc;
                dtExpenseClaimVM.ExpenseCategory = ExpenseCat;
                dtExpenseClaimVM.ExpenseCategoryID = ExpenseCatID;
                dtExpenseClaimVM.FacilityID = facilityID;
                dtExpenseClaimVM.Facility = Facility;
                dtExpenseClaimVM.AccountCode = AccountCode;
                dtExpenseClaimVM.Amount = amount;
                dtExpenseClaimVM.Gst = gst;
                dtExpenseClaimVM.GSTPercentage = gstpercentage;
                dtExpenseClaimVM.AmountWithGST = sumamount;
                expenseClaimDetailVM.DtExpenseClaimVMSummary.Add(dtExpenseClaimVM);
            }
            List<DtExpenseClaimSummary> lstExpenseClaimSummary = new List<DtExpenseClaimSummary>();
            foreach (var item in expenseClaimDetailVM.DtExpenseClaimVMSummary)
            {
                DtExpenseClaimSummary dtExpenseClaimSummary1 = new DtExpenseClaimSummary();
                dtExpenseClaimSummary1.AccountCode = item.AccountCode;
                dtExpenseClaimSummary1.Amount = item.Amount;
                dtExpenseClaimSummary1.ExpenseCategory = item.ExpenseCategory;
                dtExpenseClaimSummary1.ExpenseCategoryID = item.ExpenseCategoryID;
                dtExpenseClaimSummary1.FacilityID = item.FacilityID;
                dtExpenseClaimSummary1.Facility = item.Facility;
                dtExpenseClaimSummary1.Description = item.Description.ToUpper();
                dtExpenseClaimSummary1.GST = item.Gst;
                dtExpenseClaimSummary1.GSTPercentage = item.GSTPercentage;
                if (item.Gst != 0)
                {
                    dtExpenseClaimSummary1.TaxClass = Math.Round((decimal)item.GSTPercentage, (int)1);
                }
                else
                {
                    dtExpenseClaimSummary1.TaxClass = 4;
                }
                dtExpenseClaimSummary1.AmountWithGST = item.AmountWithGST;
                lstExpenseClaimSummary.Add(dtExpenseClaimSummary1);
            }

            DtExpenseClaimSummary dtExpenseClaimSummary = new DtExpenseClaimSummary();
            dtExpenseClaimSummary.AccountCode = "425000";
            dtExpenseClaimSummary.Amount = mstExpenseClaim.GrandTotal;
            dtExpenseClaimSummary.GST = mstExpenseClaim.TotalAmount - mstExpenseClaim.GrandTotal;
            dtExpenseClaimSummary.AmountWithGST = mstExpenseClaim.TotalAmount;
            dtExpenseClaimSummary.TaxClass = 0;
            dtExpenseClaimSummary.ExpenseCategory = "DBS";
            dtExpenseClaimSummary.Description = "";
            lstExpenseClaimSummary.Add(dtExpenseClaimSummary);

            var res = await _repository.MstExpenseClaim.SaveItems(mstExpenseClaim, expenseClaimViewModel.dtClaims, lstExpenseClaimSummary);

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
                    mstExpenseClaim = await _repository.MstExpenseClaim.GetExpenseClaimByIdAsync(res);
                    if (mstExpenseClaim.ApprovalStatus == 1)
                    {
                        string VerifierIDs = "";
                        string ApproverIDs = "";
                        string UserApproverIDs = "";
                        string HODApproverID = "";
                        try
                        {
                            //VerifierIDs = mstExpenseClaim.Verifier.Split(',');
                            //VerifierIDs = string.Join(",", ExpenseverifierIDs.Skip(1));
                            string[] verifierIDs = mstExpenseClaim.Verifier.Split(',');
                            ApproverIDs = mstExpenseClaim.Approver;
                            HODApproverID = mstExpenseClaim.HODApprover;



                            //BackgroundJob.Enqueue(() => _sendMailServices.SendEmail());
                            //Mail Code Implementation for Verifiers

                            foreach (string verifierID in verifierIDs)
                            {
                                if (verifierID != "")
                                {
                                    string domainUrl = HttpContext.Request.Scheme + "://" + HttpContext.Request.Host;
                                    string clickUrl = domainUrl + "/" + "FinanceExpenseClaim/Details/" + mstExpenseClaim.ECID;

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

                                    //var mstVerifierDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(verifierID));
                                    var toEmail = mstVerifierDetails.EmailAddress;
                                    var receiverName = mstVerifierDetails.Name;
                                    var claimNo = mstExpenseClaim.ECNo;
                                    var screen = "Expense Claim";
                                    var approvalType = "Verification Request";
                                    int userID = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
                                    var subject = "Expense Claim for Verification " + claimNo;

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
                        string[] userApproverIDs = mstExpenseClaim.UserApprovers.ToString().Split(',');
                        foreach (string userApproverID in userApproverIDs)
                        {
                            if (userApproverID != "")
                            {
                                string domainUrl = HttpContext.Request.Scheme + "://" + HttpContext.Request.Host;
                                string clickUrl = domainUrl + "/" + "HodSummary/ECDetails/" + mstExpenseClaim.ECID;

                                var mstSenderDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value));
                                var senderName = mstSenderDetails.Name;
                                var mstVerifierDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(userApproverID));
                                var toEmail = mstVerifierDetails.EmailAddress;
                                var receiverName = mstVerifierDetails.Name;
                                var claimNo = mstExpenseClaim.ECNo;
                                var screen = "Expense Claim";
                                var approvalType = "Approval Request";
                                int userID = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
                                var subject = "Expense Claim for Approval " + claimNo;

                                BackgroundJob.Enqueue(() => _sendMailServices.SendEmail("EmailTemplate.html", screen, subject, senderName, receiverName, claimNo, approvalType, userID, toEmail, clickUrl));
                            }
                            break;
                        }
                    }
                    TempData["Message"] = "Expense Claim added successfully";
                }
                else
                {
                    mstExpenseClaim = await _repository.MstExpenseClaim.GetExpenseClaimByIdAsync(res);
                    if (mstExpenseClaim.ApprovalStatus == 1)
                    {
                        string VerifierIDs = "";
                        string ApproverIDs = "";
                        string UserApproverIDs = "";
                        string HODApproverID = "";
                        try
                        {
                            //VerifierIDs = mstExpenseClaim.Verifier.Split(',');
                            //VerifierIDs = string.Join(",", ExpenseverifierIDs.Skip(1));
                            string[] verifierIDs = mstExpenseClaim.Verifier.Split(',');
                            ApproverIDs = mstExpenseClaim.Approver;
                            HODApproverID = mstExpenseClaim.HODApprover;



                            //BackgroundJob.Enqueue(() => _sendMailServices.SendEmail());
                            //Mail Code Implementation for Verifiers

                            foreach (string verifierID in verifierIDs)
                            {
                                if (verifierID != "")
                                {
                                    string domainUrl = HttpContext.Request.Scheme + "://" + HttpContext.Request.Host;
                                    string clickUrl = domainUrl + "/" + "FinanceExpenseClaim/Details/" + mstExpenseClaim.ECID;

                                    var mstSenderDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value));
                                    var senderName = mstSenderDetails.Name;
                                    var mstVerifierDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(verifierID));
                                    var toEmail = mstVerifierDetails.EmailAddress;
                                    var receiverName = mstVerifierDetails.Name;
                                    var claimNo = mstExpenseClaim.ECNo;
                                    var screen = "Expense Claim";
                                    var approvalType = "Verification Request";
                                    int userID = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
                                    var subject = "Expense Claim for Verification " + claimNo;

                                    BackgroundJob.Enqueue(() => _sendMailServices.SendEmail("EmailTemplate.html", screen, subject, senderName, receiverName, claimNo, approvalType, userID, toEmail, clickUrl));
                                }
                                break;
                            }
                        }
                        catch
                        {
                        }
                    }
                    else if (mstExpenseClaim.ApprovalStatus == 6)
                    {
                        string[] userApproverIDs = mstExpenseClaim.UserApprovers.ToString().Split(',');
                        foreach (string userApproverID in userApproverIDs)
                        {
                            if (userApproverID != "")
                            {
                                string domainUrl = HttpContext.Request.Scheme + "://" + HttpContext.Request.Host;
                                string clickUrl = domainUrl + "/" + "HodSummary/ECDetails/" + mstExpenseClaim.ECID;

                                var mstSenderDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value));
                                var senderName = mstSenderDetails.Name;
                                var mstVerifierDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(userApproverID));
                                var toEmail = mstVerifierDetails.EmailAddress;
                                var receiverName = mstVerifierDetails.Name;
                                var claimNo = mstExpenseClaim.ECNo;
                                var screen = "Expense Claim";
                                var approvalType = "Approval Request";
                                int userID = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
                                var subject = "Expense Claim for Approval " + claimNo;

                                BackgroundJob.Enqueue(() => _sendMailServices.SendEmail("EmailTemplate.html", screen, subject, senderName, receiverName, claimNo, approvalType, userID, toEmail, clickUrl));
                            }
                            break;
                        }
                    }
                    else if (mstExpenseClaim.ApprovalStatus == 7)
                    {
                        string[] hODApproverIDs = mstExpenseClaim.HODApprover.ToString().Split(',');
                        foreach (string hODApproverID in hODApproverIDs)
                        {
                            if (hODApproverID != "")
                            {
                                string domainUrl = HttpContext.Request.Scheme + "://" + HttpContext.Request.Host;
                                string clickUrl = domainUrl + "/" + "HodSummary/ECDetails/" + mstExpenseClaim.ECID;

                                var mstSenderDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value));
                                var senderName = mstSenderDetails.Name;
                                var mstVerifierDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(hODApproverID));
                                var toEmail = mstVerifierDetails.EmailAddress;
                                var receiverName = mstVerifierDetails.Name;
                                var claimNo = mstExpenseClaim.ECNo;
                                var screen = "Expense Claim";
                                var approvalType = "Approval Request";
                                int userID = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
                                var subject = "Expense Claim for Approval " + claimNo;

                                BackgroundJob.Enqueue(() => _sendMailServices.SendEmail("EmailTemplate.html", screen, subject, senderName, receiverName, claimNo, approvalType, userID, toEmail, clickUrl));
                            }
                            break;
                        }
                    }
                    else
                    {
                        string[] ExpenseapproverIDs = mstExpenseClaim.Approver.ToString().Split(',');
                        foreach (string approverID in ExpenseapproverIDs)
                        {
                            if (approverID != "")
                            {
                                string domainUrl = HttpContext.Request.Scheme + "://" + HttpContext.Request.Host;
                                string clickUrl = domainUrl + "/" + "FinanceExpenseClaim/Details/" + mstExpenseClaim.ECID;

                                var mstSenderDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value));
                                var senderName = mstSenderDetails.Name;
                                var mstVerifierDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(approverID));
                                var toEmail = mstVerifierDetails.EmailAddress;
                                var receiverName = mstVerifierDetails.Name;
                                var claimNo = mstExpenseClaim.ECNo;
                                var screen = "Expense Claim";
                                var approvalType = "Approval Request";
                                int userID = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
                                var subject = "Expense Claim for Approval " + claimNo;

                                BackgroundJob.Enqueue(() => _sendMailServices.SendEmail("EmailTemplate.html", screen, subject, senderName, receiverName, claimNo, approvalType, userID, toEmail, clickUrl));
                            }
                            break;
                        }
                    }
                    TempData["Message"] = "Expense Claim updated successfully";
                }

                return Json(new { res });
            }
            else
                return Json(new { res });
        }

        [HttpPost]
        public async Task<JsonResult> SaveDraftItems(string data)
        {
            //var expenseClaimViewModel = JsonConvert.DeserializeObject<ExpenseClaimDraftViewModel>(data,
            //    new IsoDateTimeConverter { DateTimeFormat = "dd/MM/yyyy" });                


            var expenseClaimViewModel = JsonConvert.DeserializeObject<ExpenseClaimDraftViewModel>(data);

            var mstFacility = await _repository.MstFacility.GetFacilityWithDepartmentByIdAsync(Convert.ToInt32(HttpContext.User.FindFirst("delegatefacilityid") is null ? HttpContext.User.FindFirst("facilityid").Value : HttpContext.User.FindFirst("delegatefacilityid").Value));

            MstExpenseClaimDraft mstExpenseClaim = new MstExpenseClaimDraft();
            mstExpenseClaim.ECNo = "D" + expenseClaimViewModel.ECNo;
            mstExpenseClaim.UserID = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
            mstExpenseClaim.ClaimType = expenseClaimViewModel.ClaimType;
            mstExpenseClaim.Verifier = "";
            mstExpenseClaim.Approver = "";
            mstExpenseClaim.FinalApprover = "";
            mstExpenseClaim.ApprovalStatus = 1;
            mstExpenseClaim.GrandTotal = expenseClaimViewModel.GrandTotal;
            mstExpenseClaim.TotalAmount = expenseClaimViewModel.TotalAmount;
            mstExpenseClaim.Company = expenseClaimViewModel.Company;
            mstExpenseClaim.FacilityID = Convert.ToInt32(HttpContext.User.FindFirst("delegatefacilityid") is null ? HttpContext.User.FindFirst("facilityid").Value : HttpContext.User.FindFirst("delegatefacilityid").Value);
            mstExpenseClaim.DepartmentID = mstFacility.MstDepartment.DepartmentID;
            mstExpenseClaim.CreatedDate = DateTime.Now;
            mstExpenseClaim.ModifiedDate = DateTime.Now;
            mstExpenseClaim.CreatedBy = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
            mstExpenseClaim.ModifiedBy = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
            mstExpenseClaim.ApprovalDate = DateTime.Now;
            mstExpenseClaim.ApprovalBy = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
            mstExpenseClaim.TnC = true;

            foreach (var dtItem in expenseClaimViewModel.dtClaims)
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
            long ECID = 0;
            try
            {
                //CBRID = Convert.ToInt32(Session["CBRID"].ToString());
                ECID = Convert.ToInt64(expenseClaimViewModel.ECID);
                if (ECID == 0 || TempData["Updatestatus"].ToString() == "Recreate")
                {
                    ClaimStatus = "Recreate";
                    ECID = 0;
                }
                else if (ECID == 0)
                    ClaimStatus = "Add";
                else
                    ClaimStatus = "Update";
                mstExpenseClaim.ECID = ECID;
                //mstExpenseClaim.ECNo = expenseClaimViewModel.;
            }
            catch { }

            ExpenseClaimDetailVM expenseClaimDetailVM = new ExpenseClaimDetailVM();
            //List<DtMileageClaimVM> dtMileageClaimVMs = new List<DtMileageClaimVM>();
            expenseClaimDetailVM.DtExpenseClaimVMs = new List<DtExpenseClaimVM>();
            // List<clsDtSupplierPO> oclsDtSupplierPO = new List<clsDtSupplierPO>();
            foreach (var item in expenseClaimViewModel.dtClaims)
            {
                DtExpenseClaimVM dtExpenseClaimVM = new DtExpenseClaimVM();
                if (ECID == 0 || TempData["Updatestatus"].ToString() == "Recreate")
                {
                    dtExpenseClaimVM.ECItemID = 0;
                    dtExpenseClaimVM.ECID = 0;
                }
                //dtExpenseClaimVM.Payee = item.Payee;
                //dtExpenseClaimVM.Particulars = item.Particulars;
                dtExpenseClaimVM.Description = item.Description;
                dtExpenseClaimVM.ExpenseCategory = item.MstExpenseCategory.Description;
                dtExpenseClaimVM.ExpenseCategoryID = item.MstExpenseCategory.ExpenseCategoryID;
                //dtExpenseClaimVM.Reason = item.Reason;
                //dtExpenseClaimVM.EmployeeNo = item.EmployeeNo;
                //dtExpenseClaimVM.ChequeNo = item.ChequeNo;
                dtExpenseClaimVM.Amount = item.Amount;
                dtExpenseClaimVM.Gst = item.GST;
                dtExpenseClaimVM.GSTPercentage = item.GSTPercentage;
                dtExpenseClaimVM.AmountWithGST = item.Amount + item.GST;
                //dtExpenseClaimVM.Facility = item.Facility;
                dtExpenseClaimVM.AccountCode = item.AccountCode;
                dtExpenseClaimVM.DateOfJourney = item.Date;
                dtExpenseClaimVM.OrderBy = item.OrderBy;
                expenseClaimDetailVM.DtExpenseClaimVMs.Add(dtExpenseClaimVM);
            }

            var GroupByQS = expenseClaimDetailVM.DtExpenseClaimVMs.GroupBy(s => new { s.AccountCode, s.ExpenseCategory, s.FacilityID, s.Gst });

            expenseClaimDetailVM.DtExpenseClaimVMSummary = new List<DtExpenseClaimVM>();

            foreach (var group in GroupByQS)
            {
                DtExpenseClaimVM dtExpenseClaimVM = new DtExpenseClaimVM();
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
                        ExpenseDesc = dtExpense.Description;
                    i++;
                    amount = amount + dtExpense.Amount;
                    gst = gst + dtExpense.Gst;
                    gstpercentage = dtExpense.GSTPercentage;
                    sumamount = sumamount + dtExpense.AmountWithGST;
                    ExpenseCat = dtExpense.ExpenseCategory;
                    AccountCode = dtExpense.AccountCode;
                }
                //gst = gst / group.Count();
                dtExpenseClaimVM.Description = ExpenseDesc;
                dtExpenseClaimVM.ExpenseCategory = ExpenseCat;
                dtExpenseClaimVM.AccountCode = AccountCode;
                dtExpenseClaimVM.Amount = amount;
                dtExpenseClaimVM.Gst = gst;
                dtExpenseClaimVM.GSTPercentage = gstpercentage;
                dtExpenseClaimVM.AmountWithGST = sumamount;
                expenseClaimDetailVM.DtExpenseClaimVMSummary.Add(dtExpenseClaimVM);
            }
            List<DtExpenseClaimSummaryDraft> lstExpenseClaimSummary = new List<DtExpenseClaimSummaryDraft>();
            foreach (var item in expenseClaimDetailVM.DtExpenseClaimVMSummary)
            {
                DtExpenseClaimSummaryDraft dtExpenseClaimSummary1 = new DtExpenseClaimSummaryDraft();
                dtExpenseClaimSummary1.AccountCode = item.AccountCode;
                dtExpenseClaimSummary1.Amount = item.Amount;
                dtExpenseClaimSummary1.ExpenseCategory = item.ExpenseCategory;
                dtExpenseClaimSummary1.Description = item.Description.ToUpper();
                dtExpenseClaimSummary1.GST = item.Gst;
                if (item.Gst != 0)
                {
                    dtExpenseClaimSummary1.TaxClass = Math.Round((decimal)item.GSTPercentage, (int)1);
                }
                else
                {
                    dtExpenseClaimSummary1.TaxClass = 4;
                }
                dtExpenseClaimSummary1.AmountWithGST = item.AmountWithGST;
                lstExpenseClaimSummary.Add(dtExpenseClaimSummary1);
            }

            DtExpenseClaimSummaryDraft dtExpenseClaimSummary = new DtExpenseClaimSummaryDraft();
            dtExpenseClaimSummary.AccountCode = "425000";
            dtExpenseClaimSummary.Amount = mstExpenseClaim.GrandTotal;
            dtExpenseClaimSummary.GST = mstExpenseClaim.TotalAmount - mstExpenseClaim.GrandTotal;
            dtExpenseClaimSummary.AmountWithGST = mstExpenseClaim.TotalAmount;
            dtExpenseClaimSummary.TaxClass = 0;
            dtExpenseClaimSummary.ExpenseCategory = "DBS";
            dtExpenseClaimSummary.Description = "";
            lstExpenseClaimSummary.Add(dtExpenseClaimSummary);

            var res = await _repository.MstExpenseClaimDraft.SaveDraftItems(mstExpenseClaim, expenseClaimViewModel.dtClaims, lstExpenseClaimSummary);

            if (res != 0)
            {
                if (ClaimStatus == "Add" || ClaimStatus == "Recreate")
                    TempData["Message"] = "Expense Claim Draft added successfully";
                else
                    TempData["Message"] = "Expense Claim Draft updated successfully";

                return Json(new { res });
            }
            else
                return Json(new { res });
        }

        public async Task<JsonResult> UploadECFilesDraft(List<IFormFile> files)
        {
            var path = "FileUploads/ExpenseClaimFiles/";

            foreach (IFormFile formFile in files)
            {
                int ECID = Convert.ToInt32(Request.Form["Id"]);
                if (formFile.Length > 0)
                {
                    int fileSize = formFile.ContentDisposition.Length;
                    string fileName = ContentDispositionHeaderValue.Parse(formFile.ContentDisposition).FileName.Trim('"');
                    string mimeType = formFile.ContentType;
                    var filePath = Path.Combine(path, formFile.FileName);
                    string ext = Path.GetExtension(filePath);
                    string result = Path.GetFileNameWithoutExtension(filePath);
                    string pathToFiles = Regex.Replace(result, @"[^0-9a-zA-Z]+", "_") + "-" + ECID.ToString() + "-" + DateTime.Now.ToString("ddMMyyyyss") + ext;

                    DtExpenseClaimFileUploadDraft dtExpenseClaimFileUpload = new DtExpenseClaimFileUploadDraft();
                    dtExpenseClaimFileUpload.ECID = ECID;
                    dtExpenseClaimFileUpload.FileName = fileName;
                    dtExpenseClaimFileUpload.FilePath = pathToFiles;
                    dtExpenseClaimFileUpload.CreatedDate = DateTime.Now;
                    dtExpenseClaimFileUpload.ModifiedDate = DateTime.Now;
                    dtExpenseClaimFileUpload.CreatedBy = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
                    dtExpenseClaimFileUpload.ModifiedBy = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
                    dtExpenseClaimFileUpload.IsDeleted = false;
                    _repository.DtExpenseClaimFileUploadDraft.CreateDtExpenseClaimFileUploadDraft(dtExpenseClaimFileUpload);
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

        public async Task<JsonResult> UploadECFiles(List<IFormFile> files)
        {
            //var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "FileUploads", "ExpenseClaimFiles");
            var path = "FileUploads/ExpenseClaimFiles/";

            //if (!Directory.Exists(path))
            //{
            //    Directory.CreateDirectory(path);
            //}

            // var id1 = Request.Form["Id"];
            //var id = Request.Form["Id"].ToString();

            string claimsCondition = Request.Form["claimAddCondition"];
            int ECID = Convert.ToInt32(Request.Form["Id"]);
            int ecIDValue = Convert.ToInt32(Request.Form["ecIDValue"]);

            if (ECID == 0)
            {
                if (TempData.ContainsKey("CID"))
                    ECID = Convert.ToInt32(TempData["CID"].ToString());
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
                    string pathToFiles = Regex.Replace(result, @"[^0-9a-zA-Z]+", "_") + "-" + ECID.ToString() + "-" + DateTime.Now.ToString("ddMMyyyyss") + ext;

                    DtExpenseClaimFileUpload dtExpenseClaimFileUpload = new DtExpenseClaimFileUpload();
                    dtExpenseClaimFileUpload.ECID = ECID;
                    dtExpenseClaimFileUpload.FileName = fileName;
                    dtExpenseClaimFileUpload.FilePath = pathToFiles;
                    dtExpenseClaimFileUpload.CreatedDate = DateTime.Now;
                    dtExpenseClaimFileUpload.ModifiedDate = DateTime.Now;
                    dtExpenseClaimFileUpload.CreatedBy = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
                    dtExpenseClaimFileUpload.ModifiedBy = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
                    dtExpenseClaimFileUpload.IsDeleted = false;
                    _repository.DtExpenseClaimFileUpload.CreateDtExpenseClaimFileUpload(dtExpenseClaimFileUpload);
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
                        //await blockBlob.SetPropertiesAsync();

                        await blockBlob.UploadFromStreamAsync(formFile.OpenReadStream());

                    }
                }
            }

            // Check if any previous files and move them from draft and save
            long idd = Convert.ToInt64(ecIDValue);

            var dtFiles = await _repository.DtExpenseClaimFileUploadDraft.GetDtExpenseClaimDraftAuditByIdAsync(idd);
            if (dtFiles != null)
            {
                foreach (var dtFile in dtFiles)
                {
                    DtExpenseClaimFileUpload dtExpenseClaimFileUpload = new DtExpenseClaimFileUpload()
                    {
                        CreatedBy = dtFile.CreatedBy,
                        CreatedDate = dtFile.CreatedDate,
                        FileID = 0,
                        FileName = dtFile.FileName,
                        FilePath = dtFile.FilePath,
                        IsDeleted = dtFile.IsDeleted,
                        ModifiedBy = dtFile.ModifiedBy,
                        ModifiedDate = dtFile.ModifiedDate,
                        ECID = ECID
                    };
                    try
                    {
                        _repository.DtExpenseClaimFileUpload.Create(dtExpenseClaimFileUpload);
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
                    var expenseClaimsDraft = await _repository.MstExpenseClaimDraft.GetExpenseClaimDraftByIdAsync(idd);
                    if (expenseClaimsDraft != null)
                    {
                        _repository.MstExpenseClaimDraft.DeleteExpenseClaimDraft(expenseClaimsDraft);
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
                    long ECID = Convert.ToInt64(queryParamViewModel.Cid);
                    int UserID = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
                    // newly Added Code
                    var expenseClaim = await _repository.MstExpenseClaim.GetExpenseClaimByIdAsync(ECID);
                    for (int i = 0; i < UserIds.Length; i++)
                    {
                        MstQuery clsdtExpenseQuery = new MstQuery();
                        // if (data["MessageDescription"] != null)               
                        clsdtExpenseQuery.ModuleType = "Expense Claim";
                        //  clsdtSupplierQuery.ID = Convert.ToInt64(data["SPOID"]);
                        clsdtExpenseQuery.ID = ECID;
                        clsdtExpenseQuery.SenderID = UserID;
                        //var recieverId = data["queryusers"];       
                        clsdtExpenseQuery.ReceiverID = Convert.ToInt32(UserIds[i]);
                        clsdtExpenseQuery.MessageDescription = queryParamViewModel.Message;
                        clsdtExpenseQuery.SentTime = DateTime.Now;
                        //clsdtExpenseQuery.NotificationStatus = false;
                        await _repository.MstQuery.CreateQuery(clsdtExpenseQuery);
                        //await _repository.SaveAsync();
                        //objERPEntities.AddToMstQueries(clsdtSupplierQuery);
                        //objERPEntities.SaveChanges();
                        result = "Success";

                        var receiver = await _repository.MstUser.GetUserByIdAsync(UserIds[i]);
                        //var reciever = objERPEntities.MstUsers.ToList().Where(p => p.UserID == Convert.ToInt32(UserIds[i]) && p.InstanceID == int.Parse(Session["InstanceID"].ToString())).ToList().FirstOrDefault();
                        MstExpenseClaimAudit auditUpdate = new MstExpenseClaimAudit();
                        auditUpdate.ECID = ECID;
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
                        await _repository.MstExpenseClaimAudit.CreateExpenseClaimAudit(auditUpdate);
                        await _repository.SaveAsync();

                        string domainUrl = HttpContext.Request.Scheme + "://" + HttpContext.Request.Host;
                        string clickUrl = string.Empty;

                        if (expenseClaim.CreatedBy.ToString().Contains(UserIds[i].ToString()))
                            clickUrl = domainUrl + "/" + "ExpenseClaim/Details/" + ECID;
                        else if (expenseClaim.DApprover.Contains(UserIds[i].ToString()) || expenseClaim.DVerifier.Contains(UserIds[i].ToString()))
                            clickUrl = domainUrl + "/" + "FinanceExpenseClaim/Details/" + ECID;
                        else
                            clickUrl = domainUrl + "/" + "HodSummary/ECDetails/" + ECID;
                        //if (expenseClaim.DUserApprovers.Contains(UserIds[i].ToString()) || expenseClaim.DHODApprover.Contains(UserIds[i].ToString()))

                        //var mstSenderDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value));
                        var senderName = (string.IsNullOrEmpty(delegatedUserName) ? User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.GivenName).Value : delegatedUserName);
                        //var mstVerifierDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(approverID));
                        var toEmail = receiver.EmailAddress;
                        var receiverName = receiver.Name;
                        var claimNo = expenseClaim.ECNo;
                        var screen = "Expense Claim";
                        var approvalType = "Query";
                        int userID = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
                        var subject = "Expense Claim Query " + claimNo;
                        BackgroundJob.Enqueue(() => _sendMailServices.SendEmail("QueryTemplate.html", screen, subject, senderName, receiverName, claimNo, approvalType, userID, toEmail, clickUrl, string.Empty, string.Empty, queryParamViewModel.Message));
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Something went wrong inside CreateExpenseClaimAudit action: {ex.Message}");
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
                int UserId = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
                ViewBag.userID = UserId;
                //var queries1 = _context.mstQuery.ToList().Where(j => j.ID == smcid && (j.SenderID == UserId || j.ReceiverID == UserId) && j.ModuleType.ToString().Trim() == "Expense Claim").OrderBy(j => j.SentTime);
                var queries = await _repository.MstQuery.GetAllClaimsQueriesAsync(UserId, ecid, "Expense Claim");
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
