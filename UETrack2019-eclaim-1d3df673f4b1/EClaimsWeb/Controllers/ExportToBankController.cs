using AutoMapper;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
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
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using NToastNotify;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace EClaimsWeb.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ExportToBankController : Controller
    {
        private ILoggerManager _logger;
        private IRepositoryWrapper _repository;
        private readonly IToastNotification _toastNotification;
        private IMapper _mapper;
        private readonly RepositoryContext _context;
        private ISendMailServices _sendMailServices;
        private IConfiguration _configuration;
        public ExportToBankController(ILoggerManager logger, IRepositoryWrapper repository, IMapper mapper, RepositoryContext context, ISendMailServices sendMailServices, IConfiguration configuration)
        {
            _logger = logger;
            _repository = repository;
            _mapper = mapper;
            _context = context;
            _sendMailServices = sendMailServices;
            _configuration = configuration;
        }
        public async Task<IActionResult> Index(string moduleName, int facilityID, int statusID, string fromDate, string toDate)
        {
            try
            {
                if (string.IsNullOrEmpty(moduleName))
                {
                    moduleName = "ExpenseClaim";
                }

                if (statusID == 0)
                {
                    statusID = 9;
                }

                if (string.IsNullOrEmpty(fromDate) || string.IsNullOrEmpty(toDate))
                {
                    fromDate = DateTime.Now.AddDays(-60).ToString("dd/MM/yyyy");
                    toDate = DateTime.Now.ToString("dd/MM/yyyy");
                }

                List<clsModule> oclsStatus = new List<clsModule>();
                oclsStatus.Add(new clsModule() { ModuleName = "Exported to AccPac", ModuleId = "9" });
                oclsStatus.Add(new clsModule() { ModuleName = "Exported to Bank", ModuleId = "10" });

                List<SelectListItem> status = (from t in oclsStatus
                                               select new SelectListItem
                                               {
                                                   Text = t.ModuleName.ToString(),
                                                   Value = t.ModuleId.ToString(),
                                               }).OrderBy(p => p.Text).ToList();

                List<clsModule> oclsModule = new List<clsModule>();
                //oclsModule.Add(new clsModule() { ModuleName = "Admin Settings", ModuleId = "Admin Settings" });
                oclsModule.Add(new clsModule() { ModuleName = "Mileage Claim", ModuleId = "MileageClaim" });
                oclsModule.Add(new clsModule() { ModuleName = "Expense Claim", ModuleId = "ExpenseClaim" });
                oclsModule.Add(new clsModule() { ModuleName = "TelephoneBill Claim", ModuleId = "TelephoneBillClaim" });
                oclsModule.Add(new clsModule() { ModuleName = "PV-Giro Claim", ModuleId = "PV-GiroClaim" });
                oclsModule.Add(new clsModule() { ModuleName = "HRPV-Giro Claim", ModuleId = "HRPV-GiroClaim" });

                List<SelectListItem> reports = (from t in oclsModule
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

                //ViewData["FacilityID"] = new SelectList(await _repository.MstFacility.GetAllFacilityAsync("active"), "FacilityID", "FacilityName");
                ViewData["UserID"] = new SelectList(await _repository.MstUser.GetAllUsersAsync("active"), "UserID", "Name");

                List<CustomClaim> customClaimVMs = new List<CustomClaim>();

                var mstUsers = await _repository.MstUser.GetAllUsersAsync("active");

                var mstMileageClaimsWithDetails = await _repository.MstMileageClaim.GetAllMileageClaimForExportToBankAsync("",moduleName, facilityID,statusID, fromDate, toDate);
                foreach (var mc in mstMileageClaimsWithDetails)
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

                    if (mc.Verifier != "")
                    {
                        mileageClaimVM.Approver = mc.Verifier.Split(',').First();
                        if (mileageClaimVM.Approver == HttpContext.User.FindFirst("userid").Value && (mileageClaimVM.ApprovalStatus == 1 || mileageClaimVM.ApprovalStatus == 2))
                        {
                            mileageClaimVM.IsActionAllowed = true;
                        }

                        //string VerifierIDs = string.Join(",", MileageverifierIDs.Skip(1));
                    }
                    else if (mc.Approver != "")
                    {
                        mileageClaimVM.Approver = mc.Approver.Split(',').First();
                        if (mileageClaimVM.Approver == HttpContext.User.FindFirst("userid").Value && (mileageClaimVM.ApprovalStatus == 1 || mileageClaimVM.ApprovalStatus == 2))
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
                        var mstUserApprover = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(mileageClaimVM.Approver));
                        mileageClaimVM.Approver = mstUserApprover.Name;
                    }

                    customClaimVMs.Add(mileageClaimVM);
                }
                /*
                if (moduleName == "MileageClaim")
                {
                    var mstMileageClaimsWithDetails = await _repository.MstMileageClaim.GetAllMileageClaimForExportToBankAsync("", facilityID, fromDate, toDate);
                    foreach (var mc in mstMileageClaimsWithDetails)
                    {
                        CustomClaim mileageClaimVM = new CustomClaim();
                        mileageClaimVM.CID = mc.MCID;
                        mileageClaimVM.CNO = mc.MCNo;
                        mileageClaimVM.Name = mc.MstUser.Name;
                        mileageClaimVM.CreatedDate = Convert.ToDateTime(mc.CreatedDate).ToString("dd/MM/yyyy", System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
                        mileageClaimVM.FacilityName = mc.MstFacility.FacilityName;
                        mileageClaimVM.Phone = mc.MstUser.Phone;
                        mileageClaimVM.GrandTotal = mc.GrandTotal;
                        mileageClaimVM.ApprovalStatus = mc.ApprovalStatus;

                        if (mc.Verifier != "")
                        {
                            mileageClaimVM.Approver = mc.Verifier.Split(',').First();
                            if (mileageClaimVM.Approver == HttpContext.User.FindFirst("userid").Value && (mileageClaimVM.ApprovalStatus == 1 || mileageClaimVM.ApprovalStatus == 2))
                            {
                                mileageClaimVM.IsActionAllowed = true;
                            }

                            //string VerifierIDs = string.Join(",", MileageverifierIDs.Skip(1));
                        }
                        else if (mc.Approver != "")
                        {
                            mileageClaimVM.Approver = mc.Approver.Split(',').First();
                            if (mileageClaimVM.Approver == HttpContext.User.FindFirst("userid").Value && (mileageClaimVM.ApprovalStatus == 1 || mileageClaimVM.ApprovalStatus == 2))
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
                            var mstUserApprover = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(mileageClaimVM.Approver));
                            mileageClaimVM.Approver = mstUserApprover.Name;
                        }

                        customClaimVMs.Add(mileageClaimVM);
                    }
                }
                else if (moduleName == "ExpenseClaim")
                {
                    var mstExpenseClaimsWithDetails = await _repository.MstExpenseClaim.GetAllExpenseClaimForExportToBankAsync("", facilityID, fromDate, toDate);
                    foreach (var mc in mstExpenseClaimsWithDetails)
                    {
                        CustomClaim mileageClaimVM = new CustomClaim();
                        mileageClaimVM.CID = mc.ECID;
                        mileageClaimVM.CNO = mc.ECNo;
                        mileageClaimVM.Name = mc.MstUser.Name;
                        mileageClaimVM.CreatedDate = Convert.ToDateTime(mc.CreatedDate).ToString("dd/MM/yyyy", System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
                        mileageClaimVM.FacilityName = mc.MstFacility.FacilityName;
                        mileageClaimVM.Phone = mc.MstUser.Phone;
                        mileageClaimVM.GrandTotal = mc.GrandTotal;
                        mileageClaimVM.ApprovalStatus = mc.ApprovalStatus;

                        if (mc.Verifier != "")
                        {
                            mileageClaimVM.Approver = mc.Verifier.Split(',').First();
                            if (mileageClaimVM.Approver == HttpContext.User.FindFirst("userid").Value && (mileageClaimVM.ApprovalStatus == 1 || mileageClaimVM.ApprovalStatus == 2))
                            {
                                mileageClaimVM.IsActionAllowed = true;
                            }

                            //string VerifierIDs = string.Join(",", MileageverifierIDs.Skip(1));
                        }
                        else if (mc.Approver != "")
                        {
                            mileageClaimVM.Approver = mc.Approver.Split(',').First();
                            if (mileageClaimVM.Approver == HttpContext.User.FindFirst("userid").Value && (mileageClaimVM.ApprovalStatus == 1 || mileageClaimVM.ApprovalStatus == 2))
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
                            var mstUserApprover = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(mileageClaimVM.Approver));
                            mileageClaimVM.Approver = mstUserApprover.Name;
                        }

                        customClaimVMs.Add(mileageClaimVM);
                    }
                }
                else if (moduleName == "TelephoneBillClaim")
                {
                    var mstTelephoneBillClaimsWithDetails = await _repository.MstTBClaim.GetAllTBClaimForExportToBankAsync("", facilityID, fromDate, toDate);
                    foreach (var mc in mstTelephoneBillClaimsWithDetails)
                    {
                        CustomClaim mileageClaimVM = new CustomClaim();
                        mileageClaimVM.CID = mc.TBCID;
                        mileageClaimVM.CNO = mc.TBCNo;
                        mileageClaimVM.Name = mc.MstUser.Name;
                        mileageClaimVM.CreatedDate = Convert.ToDateTime(mc.CreatedDate).ToString("dd/MM/yyyy", System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
                        mileageClaimVM.FacilityName = mc.MstFacility.FacilityName;
                        mileageClaimVM.Phone = mc.MstUser.Phone;
                        mileageClaimVM.GrandTotal = mc.GrandTotal;
                        mileageClaimVM.ApprovalStatus = mc.ApprovalStatus;

                        if (mc.Verifier != "")
                        {
                            mileageClaimVM.Approver = mc.Verifier.Split(',').First();
                            if (mileageClaimVM.Approver == HttpContext.User.FindFirst("userid").Value && (mileageClaimVM.ApprovalStatus == 1 || mileageClaimVM.ApprovalStatus == 2))
                            {
                                mileageClaimVM.IsActionAllowed = true;
                            }

                            //string VerifierIDs = string.Join(",", MileageverifierIDs.Skip(1));
                        }
                        else if (mc.Approver != "")
                        {
                            mileageClaimVM.Approver = mc.Approver.Split(',').First();
                            if (mileageClaimVM.Approver == HttpContext.User.FindFirst("userid").Value && (mileageClaimVM.ApprovalStatus == 1 || mileageClaimVM.ApprovalStatus == 2))
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
                            var mstUserApprover = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(mileageClaimVM.Approver));
                            mileageClaimVM.Approver = mstUserApprover.Name;
                        }

                        customClaimVMs.Add(mileageClaimVM);
                    }
                }
                else if (moduleName == "PV-GiroClaim")
                {
                    var mstPVGClaimsWithDetails = await _repository.MstPVGClaim.GetAllPVGClaimForExportToBankAsync("", facilityID, fromDate, toDate);
                    foreach (var mc in mstPVGClaimsWithDetails)
                    {
                        CustomClaim mileageClaimVM = new CustomClaim();
                        mileageClaimVM.CID = mc.PVGCID;
                        mileageClaimVM.CNO = mc.PVGCNo;
                        mileageClaimVM.Name = mc.MstUser.Name;
                        mileageClaimVM.CreatedDate = Convert.ToDateTime(mc.CreatedDate).ToString("dd/MM/yyyy", System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
                        mileageClaimVM.FacilityName = mc.MstFacility.FacilityName;
                        mileageClaimVM.Phone = mc.MstUser.Phone;
                        mileageClaimVM.GrandTotal = mc.GrandTotal;
                        mileageClaimVM.ApprovalStatus = mc.ApprovalStatus;

                        if (mc.Verifier != "")
                        {
                            mileageClaimVM.Approver = mc.Verifier.Split(',').First();
                            if (mileageClaimVM.Approver == HttpContext.User.FindFirst("userid").Value && (mileageClaimVM.ApprovalStatus == 1 || mileageClaimVM.ApprovalStatus == 2))
                            {
                                mileageClaimVM.IsActionAllowed = true;
                            }

                            //string VerifierIDs = string.Join(",", MileageverifierIDs.Skip(1));
                        }
                        else if (mc.Approver != "")
                        {
                            mileageClaimVM.Approver = mc.Approver.Split(',').First();
                            if (mileageClaimVM.Approver == HttpContext.User.FindFirst("userid").Value && (mileageClaimVM.ApprovalStatus == 1 || mileageClaimVM.ApprovalStatus == 2))
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
                            var mstUserApprover = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(mileageClaimVM.Approver));
                            mileageClaimVM.Approver = mstUserApprover.Name;
                        }

                        customClaimVMs.Add(mileageClaimVM);
                    }
                }
                else if (moduleName == "HRPV-GiroClaim")
                {
                    var mstHRPVGClaimsWithDetails = await _repository.MstHRPVGClaim.GetAllHRPVGClaimWithDetailsByFacilityIDForExportToBankAsync("", facilityID, fromDate, toDate);
                    foreach (var mc in mstHRPVGClaimsWithDetails)
                    {
                        CustomClaim mileageClaimVM = new CustomClaim();
                        mileageClaimVM.CID = mc.HRPVGCID;
                        mileageClaimVM.CNO = mc.HRPVGCNo;
                        mileageClaimVM.Name = mc.Name;
                        mileageClaimVM.CreatedDate = Convert.ToDateTime(mc.CreatedDate).ToString("dd/MM/yyyy", System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
                        mileageClaimVM.FacilityName = mc.FacilityName;
                        mileageClaimVM.Phone = mc.Phone;
                        mileageClaimVM.GrandTotal = mc.GrandTotal;
                        mileageClaimVM.ApprovalStatus = mc.ApprovalStatus;

                        if (mc.Verifier != "")
                        {
                            mileageClaimVM.Approver = mc.Verifier.Split(',').First();
                            if (mileageClaimVM.Approver == HttpContext.User.FindFirst("userid").Value && (mileageClaimVM.ApprovalStatus == 1 || mileageClaimVM.ApprovalStatus == 2))
                            {
                                mileageClaimVM.IsActionAllowed = true;
                            }

                            //string VerifierIDs = string.Join(",", MileageverifierIDs.Skip(1));
                        }
                        else if (mc.Approver != "")
                        {
                            mileageClaimVM.Approver = mc.Approver.Split(',').First();
                            if (mileageClaimVM.Approver == HttpContext.User.FindFirst("userid").Value && (mileageClaimVM.ApprovalStatus == 1 || mileageClaimVM.ApprovalStatus == 2))
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
                            var mstUserApprover = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(mileageClaimVM.Approver));
                            mileageClaimVM.Approver = mstUserApprover.Name;
                        }

                        customClaimVMs.Add(mileageClaimVM);
                    }
                }
                */
                _logger.LogInfo($"Returned all Mileage Claims with details from database.");

                var mstMileageClaimVM = new APReportViewModel
                {
                    //Screens = new SelectList(await screenQuery.Distinct().ToListAsync()),
                    customClaimVMs = customClaimVMs,
                    ReportTypes = new SelectList(reports, "Value", "Text"),
                    Facilities = new SelectList(facilities, "Value", "Text"),
                    Statuses = new SelectList(status, "Value", "Text"),
                    FromDate = fromDate,
                    ToDate = toDate
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

        public async Task<JsonResult> ExporttoExcel(string data)
        {
            var aPExportSearch = JsonConvert.DeserializeObject<APExportSearch>(data);
            string filename = "Export-ToBank-" + DateTime.Now.ToString("ddMMyyyyss") + ".xlsx";

            var path = "FileUploads/temp/";
            string pathToFilesold = Path.Combine(path, filename);

            ListtoDataTableConverter converter = new ListtoDataTableConverter();
            List<ExportBank> AllClaims = new List<ExportBank>();
            List<ExportBankHeader> AllClaimsHeaders = new List<ExportBankHeader>();
            List<ExportBankTrailer> AllClaimsTrailers = new List<ExportBankTrailer>();

            ExportBankHeader exportBankHeader = new ExportBankHeader();
            exportBankHeader.Header = "HEADER";
            exportBankHeader.CreationDate = DateTime.Now.Day.ToString("00") + DateTime.Now.Month.ToString("00") + DateTime.Now.Year.ToString();
            exportBankHeader.OrganizationID = "UEMSOLU3";
            exportBankHeader.CompanyName = "UEMS SOLUTIONS PTE LTD";

            AllClaimsHeaders.Add(exportBankHeader);

            string Description = string.Empty;
            if (aPExportSearch.ModuleName == "MileageClaim")
            {
                var Po_Invoice = await _repository.MstMileageClaim.GetAllMileageClaimForExportToBankAsync(aPExportSearch.ClaimIds,aPExportSearch.ModuleName, Int32.Parse(aPExportSearch.FacilityID),Int32.Parse(aPExportSearch.StatusID), aPExportSearch.FromDate, aPExportSearch.ToDate);
                int num1 = 0;
                decimal TotalAmount = 0;

                #region Noraml AP export without Consolidated Invoices

                foreach (var item in Po_Invoice)
                {
                    num1 = num1 + 1;
                    var id = await _repository.DtMileageClaim.GetDtMileageClaimByIdAsync(item.CID);
                    //var id = objERPEntities.DtSupplierPOCs.ToList().Where(p => p.POCID == Convert.ToInt32(item.POCID)).ToList();
                    //if (id.Count != 0)
                    //    Description = id.FirstOrDefault().desc item.SupplierShortName + "/" + objERPEntities.DtSupplierPOCs.ToList().Where(p => p.POCID == Convert.ToInt32(item.POCID)).ToList().FirstOrDefault().Description;
                    //else
                    //    Description = "";
                    int n = id.Count;
                    if (id.Count != 0)
                    {
                        if (id.Count == n && id.Count == 1)
                        {
                            Description = "Mileage Claim " + "from " + id.FirstOrDefault().DateOfJourney.Day + " " + Helper.Month(id.FirstOrDefault().DateOfJourney.Month) + " to " + id.FirstOrDefault().DateOfJourney.Day + " " + Helper.Month(id.FirstOrDefault().DateOfJourney.Month) + " " + id.FirstOrDefault().DateOfJourney.Year.ToString();
                        }
                        else
                        {
                            Description = "Mileage Claim " + "from " + id.FirstOrDefault().DateOfJourney.Day + " " + Helper.Month(id.FirstOrDefault().DateOfJourney.Month) + " to " + id.LastOrDefault().DateOfJourney.Day + " " + Helper.Month(id.LastOrDefault().DateOfJourney.Month) + " " + id.LastOrDefault().DateOfJourney.Year.ToString();
                        }
                    }

                    var bankDetails = await _repository.MstBankDetails.GetBankDetailsByUserIdAsync(item.UserID);

                    ExportBank exportBank = new ExportBank();
                    exportBank.RECORD = "PAYMENT";
                    exportBank.PAYMENTTYPE = "GPP";
                    exportBank.ORIGINATINGACCOUNTNUMBER = "0020218313";
                    exportBank.ORIGINATINGACCOUNTCURRENCY = "SGD";
                    exportBank.PAYMENTCURRENCY = "SGD";
                    exportBank.PAYMENTDATE = DateTime.Now.ToString("ddMMyyyy");
                    exportBank.RECEIVINGPARTYNAME = item.Name;
                    exportBank.CUSTOMERREFERENCE = item.VoucherNo;
                    if (bankDetails != null)
                    {
                        exportBank.RECEIVINGACCOUNTNUMBER = !string.IsNullOrEmpty(bankDetails.AccountNumber) ? Aes256CbcEncrypter.Decrypt(bankDetails.AccountNumber) : "";
                        exportBank.BENEFICIARYBANKSWIFTBIC = !string.IsNullOrEmpty(bankDetails.BankSwiftBIC) ? Aes256CbcEncrypter.Decrypt(bankDetails.BankSwiftBIC) : "";
                    }
                    else
                    {
                        exportBank.RECEIVINGACCOUNTNUMBER = "";
                        exportBank.BENEFICIARYBANKSWIFTBIC = "";
                    }
                    exportBank.AMOUNT = item.GrandTotal.ToString();
                    exportBank.PURPOSEOFPAYMENT = "BEXP";
                    exportBank.DELIVERYMODE = "E";
                    exportBank.EMAIL1 = item.EmailAddress;
                    exportBank.INVOICEDETAIL = item.VoucherNo;
                    exportBank.TRANSACTIONCODE = "20";

                    TotalAmount = TotalAmount + item.GrandTotal;

                    AllClaims.Add(exportBank);

                    //below code is to change the status to Exported
                    if (aPExportSearch.StatusID == "9")
                    {
                        await _repository.MstMileageClaim.UpdateMstMileageClaimStatus(item.CID, 10, int.Parse(HttpContext.User.FindFirst("userid").Value), DateTime.Now, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, false, 0);
                        string domainUrl = HttpContext.Request.Scheme + "://" + HttpContext.Request.Host;
                        string clickUrl = domainUrl + "/" + "MileageClaim/Details/" + item.CID;

                        var mstSenderDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(HttpContext.User.FindFirst("userid").Value));
                        var senderName = mstSenderDetails.Name;
                        //var mstVerifierDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(ApproverIDs));
                        //var toEmail = mstVerifierDetails.EmailAddress;
                        //var receiverName = mstVerifierDetails.Name;
                        //var mstVerifierDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(ApproverIDs));
                        var toEmail = item.EmailAddress;
                        var receiverName = item.Name;
                        var claimNo = item.CNO;
                        var screen = "Mileage Claim";
                        var approvalType = "Exported to Bank";
                        int userID = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value);
                        var subject = "Mileage Claim " + claimNo + " has been successfully Exported to Bank";

                        BackgroundJob.Enqueue(() => _sendMailServices.SendEmail("ApprovedTemplate.html", screen, subject, senderName, receiverName, claimNo, approvalType, userID, toEmail, clickUrl));
                    }

                }

                ExportBankTrailer exportBankTrailer = new ExportBankTrailer();
                exportBankTrailer.Header = "TRAILER";
                exportBankTrailer.Count = num1;
                exportBankTrailer.TotalAmount = Math.Round((decimal)TotalAmount, (int)2).ToString("#0.00");

                AllClaimsTrailers.Add(exportBankTrailer);


                #endregion
            }
            else if (aPExportSearch.ModuleName == "ExpenseClaim")
            {
                var Po_Invoice = await _repository.MstMileageClaim.GetAllMileageClaimForExportToBankAsync(aPExportSearch.ClaimIds, aPExportSearch.ModuleName, Int32.Parse(aPExportSearch.FacilityID),Int32.Parse(aPExportSearch.StatusID), aPExportSearch.FromDate, aPExportSearch.ToDate);
                int num1 = 0;
                decimal TotalAmount = 0;

                #region Noraml AP export without Consolidated Invoices
                foreach (var item in Po_Invoice)
                {
                    num1 = num1 + 1;
                    var id = await _repository.DtExpenseClaim.GetDtExpenseClaimByIdAsync(item.CID);
                    //var id = objERPEntities.DtSupplierPOCs.ToList().Where(p => p.POCID == Convert.ToInt32(item.POCID)).ToList();
                    //if (id.Count != 0)
                    //    Description = id.FirstOrDefault().desc item.SupplierShortName + "/" + objERPEntities.DtSupplierPOCs.ToList().Where(p => p.POCID == Convert.ToInt32(item.POCID)).ToList().FirstOrDefault().Description;
                    //else
                    //    Description = "";
                    int n = id.Count;
                    if (id.Count != 0)
                    {
                        if (id.Count == n && id.Count == 1)
                        {
                            Description = id.FirstOrDefault().Description;
                            //Description = "Expense Claim " + "from " + id.FirstOrDefault().DateOfJourney.Day + " " + Helper.Month(id.FirstOrDefault().DateOfJourney.Month) + " to " + id.FirstOrDefault().DateOfJourney.Day + " " + Helper.Month(id.FirstOrDefault().DateOfJourney.Month) + " " + id.FirstOrDefault().DateOfJourney.Year.ToString();
                        }
                        else
                        {
                            Description = id.FirstOrDefault().Description;
                            //Description = "Expense Claim " + "from " + id.FirstOrDefault().DateOfJourney.Day + " " + Helper.Month(id.FirstOrDefault().DateOfJourney.Month) + " to " + id.LastOrDefault().DateOfJourney.Day + " " + Helper.Month(id.LastOrDefault().DateOfJourney.Month) + " " + id.LastOrDefault().DateOfJourney.Year.ToString();
                        }
                    }

                    var bankDetails = await _repository.MstBankDetails.GetBankDetailsByUserIdAsync(item.UserID);

                    ExportBank exportBank = new ExportBank();
                    exportBank.RECORD = "PAYMENT";
                    exportBank.PAYMENTTYPE = "GPP";
                    exportBank.ORIGINATINGACCOUNTNUMBER = "0020218313";
                    exportBank.ORIGINATINGACCOUNTCURRENCY = "SGD";
                    exportBank.PAYMENTCURRENCY = "SGD";
                    exportBank.PAYMENTDATE = DateTime.Now.ToString("ddMMyyyy");
                    exportBank.RECEIVINGPARTYNAME = item.Name;
                    exportBank.CUSTOMERREFERENCE = item.VoucherNo;
                    if (bankDetails != null)
                    {
                        exportBank.RECEIVINGACCOUNTNUMBER = !string.IsNullOrEmpty(bankDetails.AccountNumber) ? Aes256CbcEncrypter.Decrypt(bankDetails.AccountNumber) : "";
                        exportBank.BENEFICIARYBANKSWIFTBIC = !string.IsNullOrEmpty(bankDetails.BankSwiftBIC) ? Aes256CbcEncrypter.Decrypt(bankDetails.BankSwiftBIC) : "";
                    }
                    else
                    {
                        exportBank.RECEIVINGACCOUNTNUMBER = "";
                        exportBank.BENEFICIARYBANKSWIFTBIC = "";
                    }
                    exportBank.AMOUNT = item.TotalAmount.ToString();
                    exportBank.PURPOSEOFPAYMENT = "BEXP";
                    exportBank.DELIVERYMODE = "E";
                    exportBank.EMAIL1 = item.EmailAddress;
                    exportBank.INVOICEDETAIL = item.VoucherNo;
                    exportBank.TRANSACTIONCODE = "20";

                    TotalAmount = TotalAmount + item.TotalAmount;
                    AllClaims.Add(exportBank);

                    //below code is to change the status to Exported
                    if(aPExportSearch.StatusID == "9")
                    {
                        await _repository.MstExpenseClaim.UpdateMstExpenseClaimStatus(item.CID, 10, int.Parse(HttpContext.User.FindFirst("userid").Value), DateTime.Now, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, false, 0);
                        string domainUrl = HttpContext.Request.Scheme + "://" + HttpContext.Request.Host;
                        string clickUrl = domainUrl + "/" + "ExpenseClaim/Details/"+item.CID;

                        var mstSenderDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(HttpContext.User.FindFirst("userid").Value));
                        var senderName = mstSenderDetails.Name;
                        //var mstVerifierDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(ApproverIDs));
                        //var toEmail = mstVerifierDetails.EmailAddress;
                        //var receiverName = mstVerifierDetails.Name;
                        //var mstVerifierDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(ApproverIDs));
                        var toEmail = item.EmailAddress;
                        var receiverName = item.Name;
                        var claimNo = item.CNO;
                        var screen = "Expense Claim";
                        var approvalType = "Exported to Bank";
                        int userID = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value);
                        var subject = "Expense Claim " + claimNo + " has been successfully Exported to Bank";

                        BackgroundJob.Enqueue(() => _sendMailServices.SendEmail("ApprovedTemplate.html", screen, subject, senderName, receiverName, claimNo, approvalType, userID, toEmail, clickUrl));
                    }
                }

                ExportBankTrailer exportBankTrailer = new ExportBankTrailer();
                exportBankTrailer.Header = "TRAILER";
                exportBankTrailer.Count = num1;
                exportBankTrailer.TotalAmount = Math.Round((decimal)TotalAmount, (int)2).ToString("#0.00");

                AllClaimsTrailers.Add(exportBankTrailer);

                #endregion
            }
            else if (aPExportSearch.ModuleName == "TelephoneBillClaim")
            {
                var Po_Invoice = await _repository.MstMileageClaim.GetAllMileageClaimForExportToBankAsync(aPExportSearch.ClaimIds, aPExportSearch.ModuleName, Int32.Parse(aPExportSearch.FacilityID),Int32.Parse(aPExportSearch.StatusID), aPExportSearch.FromDate, aPExportSearch.ToDate);
                int num1 = 0;
                decimal TotalAmount = 0;

                #region Noraml AP export without Consolidated Invoices
                foreach (var item in Po_Invoice)
                {
                    num1 = num1 + 1;
                    var id = await _repository.DtTBClaim.GetDtTBClaimByIdAsync(item.CID);
                    //var id = objERPEntities.DtSupplierPOCs.ToList().Where(p => p.POCID == Convert.ToInt32(item.POCID)).ToList();
                    //if (id.Count != 0)
                    //    Description = id.FirstOrDefault().desc item.SupplierShortName + "/" + objERPEntities.DtSupplierPOCs.ToList().Where(p => p.POCID == Convert.ToInt32(item.POCID)).ToList().FirstOrDefault().Description;
                    //else
                    //    Description = "";
                    int n = id.Count;
                    if (id.Count != 0)
                    {
                        if (id.Count == n && id.Count == 1)
                        {
                            Description = id.FirstOrDefault().Description;
                            //Description = "Expense Claim " + "from " + id.FirstOrDefault().DateOfJourney.Day + " " + Helper.Month(id.FirstOrDefault().DateOfJourney.Month) + " to " + id.FirstOrDefault().DateOfJourney.Day + " " + Helper.Month(id.FirstOrDefault().DateOfJourney.Month) + " " + id.FirstOrDefault().DateOfJourney.Year.ToString();
                        }
                        else
                        {
                            Description = id.FirstOrDefault().Description;
                            //Description = "Expense Claim " + "from " + id.FirstOrDefault().DateOfJourney.Day + " " + Helper.Month(id.FirstOrDefault().DateOfJourney.Month) + " to " + id.LastOrDefault().DateOfJourney.Day + " " + Helper.Month(id.LastOrDefault().DateOfJourney.Month) + " " + id.LastOrDefault().DateOfJourney.Year.ToString();
                        }
                    }

                    var bankDetails = await _repository.MstBankDetails.GetBankDetailsByUserIdAsync(item.UserID);

                    ExportBank exportBank = new ExportBank();
                    exportBank.RECORD = "PAYMENT";
                    exportBank.PAYMENTTYPE = "GPP";
                    exportBank.ORIGINATINGACCOUNTNUMBER = "0020218313";
                    exportBank.ORIGINATINGACCOUNTCURRENCY = "SGD";
                    exportBank.PAYMENTCURRENCY = "SGD";
                    exportBank.PAYMENTDATE = DateTime.Now.ToString("ddMMyyyy");
                    exportBank.RECEIVINGPARTYNAME = item.Name;
                    exportBank.CUSTOMERREFERENCE = item.VoucherNo;
                    if (bankDetails != null)
                    {
                        exportBank.RECEIVINGACCOUNTNUMBER = !string.IsNullOrEmpty(bankDetails.AccountNumber) ? Aes256CbcEncrypter.Decrypt(bankDetails.AccountNumber) : "";
                        exportBank.BENEFICIARYBANKSWIFTBIC = !string.IsNullOrEmpty(bankDetails.BankSwiftBIC) ? Aes256CbcEncrypter.Decrypt(bankDetails.BankSwiftBIC) : "";
                    }
                    else
                    {
                        exportBank.RECEIVINGACCOUNTNUMBER = "";
                        exportBank.BENEFICIARYBANKSWIFTBIC = "";
                    }
                    exportBank.AMOUNT = item.GrandTotal.ToString();
                    exportBank.PURPOSEOFPAYMENT = "BEXP";
                    exportBank.DELIVERYMODE = "E";
                    exportBank.EMAIL1 = item.EmailAddress;
                    exportBank.INVOICEDETAIL = item.VoucherNo;
                    exportBank.TRANSACTIONCODE = "20";

                    TotalAmount = TotalAmount + item.GrandTotal;
                    AllClaims.Add(exportBank);


                    //below code is to change the status to Exported
                    if (aPExportSearch.StatusID == "9")
                    {
                        await _repository.MstTBClaim.UpdateMstTBClaimStatus(item.CID, 10, int.Parse(HttpContext.User.FindFirst("userid").Value), DateTime.Now, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, false, 0);
                        string domainUrl = HttpContext.Request.Scheme + "://" + HttpContext.Request.Host;
                        string clickUrl = domainUrl + "/" + "TelephoneBillClaim/Details/" + item.CID;

                        var mstSenderDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(HttpContext.User.FindFirst("userid").Value));
                        var senderName = mstSenderDetails.Name;
                        //var mstVerifierDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(ApproverIDs));
                        //var toEmail = mstVerifierDetails.EmailAddress;
                        //var receiverName = mstVerifierDetails.Name;
                        //var mstVerifierDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(ApproverIDs));
                        var toEmail = item.EmailAddress;
                        var receiverName = item.Name;
                        var claimNo = item.CNO;
                        var screen = "Telephone Bill Claim";
                        var approvalType = "Exported to Bank";
                        int userID = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value);
                        var subject = "Telephone Bill Claim " + claimNo + " has been successfully Exported to Bank";

                        BackgroundJob.Enqueue(() => _sendMailServices.SendEmail("ApprovedTemplate.html", screen, subject, senderName, receiverName, claimNo, approvalType, userID, toEmail, clickUrl));
                    }
                    
                }

                ExportBankTrailer exportBankTrailer = new ExportBankTrailer();
                exportBankTrailer.Header = "TRAILER";
                exportBankTrailer.Count = num1;
                exportBankTrailer.TotalAmount = Math.Round((decimal)TotalAmount, (int)2).ToString("#0.00");

                AllClaimsTrailers.Add(exportBankTrailer);

                #endregion
            }
            else if (aPExportSearch.ModuleName == "PV-GiroClaim")
            {
                var Po_Invoice = await _repository.MstMileageClaim.GetAllMileageClaimForExportToBankAsync(aPExportSearch.ClaimIds, aPExportSearch.ModuleName, Int32.Parse(aPExportSearch.FacilityID),Int32.Parse(aPExportSearch.StatusID), aPExportSearch.FromDate, aPExportSearch.ToDate);
                int num1 = 0;
                decimal TotalAmount = 0;

                #region Noraml AP export without Consolidated Invoices
                foreach (var item in Po_Invoice)
                {
                    num1 = num1 + 1;
                    string ExpenseDesc = string.Empty;
                    var id = await _repository.DtPVGClaim.GetDtPVGClaimByIdAsync(item.CID);
                    var Po_Invoice1 = id.GroupBy(s => new { s.Payee, s.BankAccount, s.BankSwiftBIC });
                    foreach (var group in Po_Invoice1)
                    {
                        int i = 0;
                        bool verified = false;
                        foreach (var dtExpense in group)
                        {
                            int n = group.Count();
                            if (group.Count() != 0)
                            {
                                if (group.Count() == n && group.Count() == 1)
                                {
                                    ExpenseDesc =  dtExpense.InvoiceNo + "  -  " + dtExpense.Particulars;
                                    //Description = id.FirstOrDefault().Particulars;
                                    //Description = "Expense Claim " + "from " + id.FirstOrDefault().DateOfJourney.Day + " " + Helper.Month(id.FirstOrDefault().DateOfJourney.Month) + " to " + id.FirstOrDefault().DateOfJourney.Day + " " + Helper.Month(id.FirstOrDefault().DateOfJourney.Month) + " " + id.FirstOrDefault().DateOfJourney.Year.ToString();
                                }
                                else
                                {
                                    if(i==0)
                                    {
                                        ExpenseDesc = dtExpense.InvoiceNo + "  -  " + dtExpense.Particulars;
                                        i++;
                                    }
                                    else
                                    {
                                        ExpenseDesc = ExpenseDesc + "  -  " + dtExpense.InvoiceNo + "  -  " + dtExpense.Particulars;
                                        verified = true;
                                        i++;
                                    }
                                 
                                    //Description = "Expense Claim " + "from " + id.FirstOrDefault().DateOfJourney.Day + " " + Helper.Month(id.FirstOrDefault().DateOfJourney.Month) + " to " + id.LastOrDefault().DateOfJourney.Day + " " + Helper.Month(id.LastOrDefault().DateOfJourney.Month) + " " + id.LastOrDefault().DateOfJourney.Year.ToString();
                                }
                            }
                            if (item.PVGCItemID == dtExpense.PVGCItemID || verified)
                            {
                                item.ParticularsOfPayment = ExpenseDesc;
                            }
                        }

                    }
                    //    int n = id.Count;
                    //if (id.Count != 0)
                    //{
                    //    if (id.Count == n && id.Count == 1)
                    //    {
                    //        Description = id.FirstOrDefault().Particulars;
                    //        //Description = "Expense Claim " + "from " + id.FirstOrDefault().DateOfJourney.Day + " " + Helper.Month(id.FirstOrDefault().DateOfJourney.Month) + " to " + id.FirstOrDefault().DateOfJourney.Day + " " + Helper.Month(id.FirstOrDefault().DateOfJourney.Month) + " " + id.FirstOrDefault().DateOfJourney.Year.ToString();
                    //    }
                    //    else
                    //    {
                    //        Description = id.FirstOrDefault().Particulars;
                    //        //Description = "Expense Claim " + "from " + id.FirstOrDefault().DateOfJourney.Day + " " + Helper.Month(id.FirstOrDefault().DateOfJourney.Month) + " to " + id.LastOrDefault().DateOfJourney.Day + " " + Helper.Month(id.LastOrDefault().DateOfJourney.Month) + " " + id.LastOrDefault().DateOfJourney.Year.ToString();
                    //    }
                    //}

                    var bankDetails = await _repository.MstBankDetails.GetBankDetailsByUserIdAsync(item.UserID);

                    ExportBank exportBank = new ExportBank();
                    exportBank.RECORD = "PAYMENT";

                    if(item.PaymentMode == "Fast Payment")
                        exportBank.PAYMENTTYPE = "GPP";
                    else if (item.PaymentMode == "PayNow")
                        exportBank.PAYMENTTYPE = "PPP";
                    else if (item.PaymentMode == "GIRO")
                        exportBank.PAYMENTTYPE = "BPY";
                    else if (item.PaymentMode == "TT")
                        exportBank.PAYMENTTYPE = "TT";
                    else if (item.PaymentMode == "RTGS")
                        exportBank.PAYMENTTYPE = "MEP";

                    exportBank.ORIGINATINGACCOUNTNUMBER = "0020218313";
                    exportBank.ORIGINATINGACCOUNTCURRENCY = "SGD";
                    exportBank.PAYMENTCURRENCY = "SGD";
                    exportBank.PAYMENTDATE = DateTime.Now.ToString("ddMMyyyy");
                    exportBank.RECEIVINGPARTYNAME = item.PayeeName;
                    //exportBank.RECEIVINGACCOUNTNUMBER = !string.IsNullOrEmpty(item.BankAccount) ? item.BankAccount : "";
                    if (item.PaymentMode == "PayNow")
                    {
                        if (item.Mobile.All(Char.IsDigit))
                        {
                            exportBank.BeneficiaryCategory = "M";
                            exportBank.RECEIVINGACCOUNTNUMBER = "+65" + item.Mobile;
                        }
                        else
                        {
                            exportBank.BeneficiaryCategory = "U";
                            exportBank.RECEIVINGACCOUNTNUMBER = item.Mobile;
                        }
                    }
                    else
                    {
                        exportBank.RECEIVINGACCOUNTNUMBER = !string.IsNullOrEmpty(item.BankAccount) ? item.BankAccount : "";
                    }
                    exportBank.BENEFICIARYBANKSWIFTBIC = !string.IsNullOrEmpty(item.BankSwiftBIC) ? item.BankSwiftBIC : "";
                    decimal amount = item.Amount + item.GST;
                    exportBank.AMOUNT = amount.ToString();
                    exportBank.PURPOSEOFPAYMENT = "IVPT";
                    exportBank.DELIVERYMODE = "E";
                    exportBank.EMAIL1 = item.EmailAddress;
                    exportBank.INVOICEDETAIL = item.InvoiceNo;
                    exportBank.CUSTOMERREFERENCE = item.VoucherNo;
                    exportBank.BENEFICIARYREFERENCE = item.InvoiceNo;
                    exportBank.TRANSACTIONCODE = "20";
                    TotalAmount = TotalAmount + item.Amount + item.GST;
                    AllClaims.Add(exportBank);

                    //below code is to change the status to Exported
                    if (aPExportSearch.StatusID == "9")
                    {
                        await _repository.MstPVGClaim.UpdateMstPVGClaimStatus(item.CID, 10, int.Parse(HttpContext.User.FindFirst("userid").Value), DateTime.Now, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, false, 0);
                        string domainUrl = HttpContext.Request.Scheme + "://" + HttpContext.Request.Host;
                        string clickUrl = domainUrl + "/" + "PVGIROClaim/Details/" + item.CID;

                        var mstSenderDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(HttpContext.User.FindFirst("userid").Value));
                        var senderName = mstSenderDetails.Name;
                        //var mstVerifierDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(ApproverIDs));
                        //var toEmail = mstVerifierDetails.EmailAddress;
                        //var receiverName = mstVerifierDetails.Name;
                        //var mstVerifierDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(ApproverIDs));
                        var toEmail = item.EmailAddress;
                        var receiverName = item.Name;
                        var claimNo = item.CNO;
                        var screen = "PV-GIRO Claim";
                        var approvalType = "Exported to Bank";
                        int userID = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value);
                        var subject = "PV-GIRO Claim " + claimNo + " has been successfully Exported to Bank";

                        BackgroundJob.Enqueue(() => _sendMailServices.SendEmail("ApprovedTemplate.html", screen, subject, senderName, receiverName, claimNo, approvalType, userID, toEmail, clickUrl));
                    }

                }

                ExportBankTrailer exportBankTrailer = new ExportBankTrailer();
                exportBankTrailer.Header = "TRAILER";
                exportBankTrailer.Count = num1;
                exportBankTrailer.TotalAmount = Math.Round((decimal)TotalAmount, (int)2).ToString("#0.00");

                AllClaimsTrailers.Add(exportBankTrailer);

                #endregion
            }
            else if (aPExportSearch.ModuleName == "HRPV-GiroClaim")
            {
                var Po_Invoice = await _repository.MstMileageClaim.GetAllMileageClaimForExportToBankAsync(aPExportSearch.ClaimIds, aPExportSearch.ModuleName, Int32.Parse(aPExportSearch.FacilityID),Int32.Parse(aPExportSearch.StatusID), aPExportSearch.FromDate, aPExportSearch.ToDate);
                int num1 = 0;
                decimal TotalAmount = 0;

                #region Noraml AP export without Consolidated Invoices
                foreach (var item in Po_Invoice)
                {
                    num1 = num1 + 1;
                    var id = await _repository.DtHRPVGClaim.GetDtHRPVGClaimByIdAsync(item.CID);

                    Description = item.ParticularsOfPayment;

                    //var bankDetails = await _repository.MstBankDetails.GetBankDetailsByUserIdAsync(item.UserID.Value);

                    ExportBank exportBank = new ExportBank();
                    exportBank.RECORD = "PAYMENT";

                    if (item.PaymentMode == "Fast Payment")
                        exportBank.PAYMENTTYPE = "GPP";
                    else if (item.PaymentMode == "PayNow")
                        exportBank.PAYMENTTYPE = "PPP";
                    else if (item.PaymentMode == "GIRO")
                        exportBank.PAYMENTTYPE = "BPY";
                    else if (item.PaymentMode == "TT")
                        exportBank.PAYMENTTYPE = "TT";
                    else if (item.PaymentMode == "RTGS")
                        exportBank.PAYMENTTYPE = "MEP";

                    exportBank.ORIGINATINGACCOUNTNUMBER = "0020218313";
                    exportBank.ORIGINATINGACCOUNTCURRENCY = "SGD";
                    exportBank.PAYMENTCURRENCY = "SGD";
                    exportBank.PAYMENTDATE = DateTime.Now.ToString("ddMMyyyy");
                    exportBank.RECEIVINGPARTYNAME = item.PayeeName;

                    if (item.PaymentMode == "PayNow")
                    {
                        if (item.Mobile.All(Char.IsDigit))
                        {
                            exportBank.BeneficiaryCategory = "M";
                            exportBank.RECEIVINGACCOUNTNUMBER = "+65" + item.Mobile;
                        }
                        else
                        {
                            exportBank.BeneficiaryCategory = "U";
                            exportBank.RECEIVINGACCOUNTNUMBER = item.Mobile;
                        }
                    }
                    else
                    {
                        exportBank.RECEIVINGACCOUNTNUMBER = !string.IsNullOrEmpty(item.BankAccount) ? item.BankAccount : "";
                    }
                    //exportBank.RECEIVINGACCOUNTNUMBER = !string.IsNullOrEmpty(item.BankAccount) ? item.BankAccount : "";
                    exportBank.BENEFICIARYBANKSWIFTBIC = !string.IsNullOrEmpty(item.BankSwiftBIC) ? item.BankSwiftBIC : "";
                    exportBank.AMOUNT = item.Amount.ToString();
                    exportBank.PURPOSEOFPAYMENT = "SALA";
                    exportBank.DELIVERYMODE = "";
                    exportBank.EMAIL1 = "";
                    exportBank.INVOICEDETAIL = item.VoucherNo;
                    exportBank.CUSTOMERREFERENCE = item.VoucherNo;
                    TotalAmount = TotalAmount +  item.Amount;
                    AllClaims.Add(exportBank);

                    //below code is to change the status to Exported
                    if (aPExportSearch.StatusID == "9")
                    {
                        await _repository.MstHRPVGClaim.UpdateMstHRPVGClaimStatus(item.CID, 10, int.Parse(HttpContext.User.FindFirst("userid").Value), DateTime.Now, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, false, 0);
                        string domainUrl = HttpContext.Request.Scheme + "://" + HttpContext.Request.Host;
                        string clickUrl = domainUrl + "/" + "HRPVGiroClaim/Details/" + item.CID;

                        var mstSenderDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(HttpContext.User.FindFirst("userid").Value));
                        var senderName = mstSenderDetails.Name;
                        //var mstVerifierDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(ApproverIDs));
                        //var toEmail = mstVerifierDetails.EmailAddress;
                        //var receiverName = mstVerifierDetails.Name;
                        //var mstVerifierDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(ApproverIDs));
                        var toEmail = item.EmailAddress;
                        var receiverName = item.Name;
                        var claimNo = item.CNO;
                        var screen = "HR PV-GIRO Claim";
                        var approvalType = "Exported to Bank";
                        int userID = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value);
                        var subject = "HR PV-GIRO Claim " + claimNo + " has been successfully Exported to Bank";

                        BackgroundJob.Enqueue(() => _sendMailServices.SendEmail("ApprovedTemplate.html", screen, subject, senderName, receiverName, claimNo, approvalType, userID, toEmail, clickUrl));
                    }
                }

                ExportBankTrailer exportBankTrailer = new ExportBankTrailer();
                exportBankTrailer.Header = "TRAILER";
                exportBankTrailer.Count = num1;
                exportBankTrailer.TotalAmount = Math.Round((decimal)TotalAmount, (int)2).ToString("#0.00");

                AllClaimsTrailers.Add(exportBankTrailer);

                #endregion
            }
            try
            {
                DataTable dt_InvoicesHeader = converter.ToDataTable(AllClaimsHeaders);
                DataTable dt_Invoices = converter.ToDataTable(AllClaims);
                DataTable dt_InvoicesTriler = converter.ToDataTable(AllClaimsTrailers);
                //DataTable dt_Invoices_Details = converter.ToDataTable(AllInvoiceDetails);
                //DataTable dt_Invoices_Payments = converter.ToDataTable(AllInvoicesPayments);
                //DataTable dt_Invoices_OptionalFields = converter.ToDataTable(AllInvoiceOptionalFields);
                //DataTable dt_Invoices_Details_OptionalFields = converter.ToDataTable(AllInvoiceDetailsOptionalFields);
                IActionResult rslt = new BadRequestResult();
                using (MemoryStream ms = new MemoryStream())
                {
                    //using (SpreadsheetDocument document = SpreadsheetDocument.Create(ms, SpreadsheetDocumentType.Workbook))
                    using (SpreadsheetDocument document = SpreadsheetDocument.Create(ms, SpreadsheetDocumentType.Workbook))
                    {
                        WorkbookPart workbookPart = document.AddWorkbookPart();
                        workbookPart.Workbook = new Workbook();

                        WorksheetPart worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
                        var sheetData = new SheetData();
                        worksheetPart.Worksheet = new Worksheet(sheetData);

                        Sheets sheets = workbookPart.Workbook.AppendChild(new Sheets());
                        Sheet sheet = new Sheet() { Id = workbookPart.GetIdOfPart(worksheetPart), SheetId = 1, Name = "Master" };

                        sheets.Append(sheet);

                        //For Header
                        Row headerRow2 = new Row();

                        List<String> columns2 = new List<string>();
                        foreach (System.Data.DataColumn column in dt_InvoicesHeader.Columns)
                        {
                            columns2.Add(column.ColumnName);

                            Cell cell = new Cell();
                            cell.DataType = CellValues.String;
                            cell.CellValue = new CellValue(column.ColumnName);
                            headerRow2.AppendChild(cell);
                        }

                        foreach (DataRow dsrow in dt_InvoicesHeader.Rows)
                        {
                            Row newRow = new Row();
                            foreach (String col in columns2)
                            {
                                Cell cell = new Cell();
                                cell.DataType = CellValues.String;
                                cell.CellValue = new CellValue(dsrow[col].ToString());
                                newRow.AppendChild(cell);
                            }

                            sheetData.AppendChild(newRow);
                        }

                        //For Data
                        Row headerRow = new Row();

                        List<String> columns = new List<string>();
                        foreach (System.Data.DataColumn column in dt_Invoices.Columns)
                        {
                            columns.Add(column.ColumnName);

                            Cell cell = new Cell();
                            cell.DataType = CellValues.String;
                            cell.CellValue = new CellValue(column.ColumnName);
                            headerRow.AppendChild(cell);
                        }

                        sheetData.AppendChild(headerRow);

                        foreach (DataRow dsrow in dt_Invoices.Rows)
                        {
                            Row newRow = new Row();
                            foreach (String col in columns)
                            {
                                if(col == "AMOUNT")
                                {
                                    Cell cell = new Cell();
                                    cell.DataType = CellValues.Number;
                                    cell.CellValue = new CellValue(dsrow[col].ToString());
                                    newRow.AppendChild(cell);
                                }
                                else
                                {
                                    Cell cell = new Cell();
                                    cell.DataType = CellValues.String;
                                    cell.CellValue = new CellValue(dsrow[col].ToString());
                                    newRow.AppendChild(cell);
                                }
                            }

                            sheetData.AppendChild(newRow);
                        }

                        //For Trailer
                        Row headerRow1 = new Row();

                        List<String> columns1 = new List<string>();
                        foreach (System.Data.DataColumn column in dt_InvoicesTriler.Columns)
                        {
                            columns1.Add(column.ColumnName);

                            Cell cell = new Cell();
                            cell.DataType = CellValues.String;
                            cell.CellValue = new CellValue(column.ColumnName);
                            headerRow1.AppendChild(cell);
                        }

                        foreach (DataRow dsrow in dt_InvoicesTriler.Rows)
                        {
                            Row newRow = new Row();
                            foreach (String col in columns1)
                            {
                                if(col == "TotalAmount")
                                {
                                    Cell cell = new Cell();
                                    cell.DataType = CellValues.Number;
                                    cell.CellValue = new CellValue(dsrow[col].ToString());
                                    newRow.AppendChild(cell);
                                }
                                else
                                {
                                    Cell cell = new Cell();
                                    cell.DataType = CellValues.String;
                                    cell.CellValue = new CellValue(dsrow[col].ToString());
                                    newRow.AppendChild(cell);
                                }
                            }

                            sheetData.AppendChild(newRow);
                        }

                        workbookPart.Workbook.Save();
                        document.Close();
                        if (CloudStorageAccount.TryParse(_configuration.GetSection("ConnectionStrings")["BlobConnectionString"], out CloudStorageAccount storageAccount))
                        {
                            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

                            CloudBlobContainer container = blobClient.GetContainerReference(_configuration.GetSection("ConnectionStrings")["BlobContainerName"]);

                            CloudBlockBlob blockBlob = container.GetBlockBlobReference(pathToFilesold);
                            ms.Position = 0;
                            await blockBlob.UploadFromStreamAsync(ms);
                        }
                    }
                    return Json(new { fileName = pathToFilesold });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Something went wrong inside ExporttoExcel action: {ex.Message}");
                _toastNotification.AddErrorToastMessage($"Failed to download the Export to Accpac file. Error: {ex.Message}");
                return null;// RedirectToAction("Index", "ExportToBank");
            }
        }
        public async Task<IActionResult> Download(string fileName)
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
                        return File(blobStream, file.Properties.ContentType, "ExportClaimsToBank.xlsx");
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
