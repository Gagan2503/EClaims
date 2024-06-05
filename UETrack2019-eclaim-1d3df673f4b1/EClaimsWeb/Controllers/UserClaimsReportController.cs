using AutoMapper;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using EClaimsEntities;
using EClaimsEntities.Models;
using EClaimsRepository.Contracts;
using EClaimsWeb.Helpers;
using EClaimsWeb.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json;
using NToastNotify;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using ClosedXML.Excel;
using System.Text;
using System.IO;
using System.Text;
using System.Reflection;
using DinkToPdf.Contracts;
using DinkToPdf;
using IPdfConverter = DinkToPdf.Contracts.IConverter;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage;

namespace EClaimsWeb.Controllers
{
    public class UserClaimsReportController : Controller
    {
        private ILoggerManager _logger;
        private IRepositoryWrapper _repository;
        private readonly IToastNotification _toastNotification;
        private IMapper _mapper;
        private readonly RepositoryContext _context;
        private IConfiguration _configuration;
        private IConverter _converter;
        public UserClaimsReportController(ILoggerManager logger, IRepositoryWrapper repository, IMapper mapper, RepositoryContext context, IConfiguration configuration,IConverter converter)
        {
            _logger = logger;
            _repository = repository;
            _mapper = mapper;
            _context = context;
            _configuration = configuration;
            _converter = converter;
        }
        public async Task<IActionResult> Index(string moduleName, int facilityID, int statusID, string fromDate, string toDate)
        {
            try
            {
                //if(ddlReportType == "1")
                //{

                //}
                //var mstFacilities = new SelectListItem( (await _repository.MstFacility.GetAllFacilityAsync("active"), "FacilityID", "FacilityName");
                //if (string.IsNullOrEmpty(moduleName))
                //{
                //    moduleName = "ExpenseClaim";
                //}

                //if (statusID == 0)
                //{
                //    statusID = 3;
                //}

                if (string.IsNullOrEmpty(fromDate) || string.IsNullOrEmpty(toDate))
                {
                    fromDate = DateTime.Now.AddDays(-60).ToString("dd/MM/yyyy");
                    toDate = DateTime.Now.ToString("dd/MM/yyyy");
                }

                List<clsModule> oclsStatus = new List<clsModule>();
                oclsStatus.Add(new clsModule() { ModuleName = "Approved", ModuleId = "3" });
                oclsStatus.Add(new clsModule() { ModuleName = "Awaiting Approval", ModuleId = "6" });
                oclsStatus.Add(new clsModule() { ModuleName = "Awaiting HOD Approval", ModuleId = "7" });
                oclsStatus.Add(new clsModule() { ModuleName = "Awaiting Signatory approval", ModuleId = "2" });
                oclsStatus.Add(new clsModule() { ModuleName = "Awaiting Verification", ModuleId = "1" });
                oclsStatus.Add(new clsModule() { ModuleName = "Exported to AccPac", ModuleId = "9" });
                oclsStatus.Add(new clsModule() { ModuleName = "Exported to Bank", ModuleId = "10" });
                oclsStatus.Add(new clsModule() { ModuleName = "Requested for Void", ModuleId = "-5" });
                oclsStatus.Add(new clsModule() { ModuleName = "Request to Amend", ModuleId = "4" });
                oclsStatus.Add(new clsModule() { ModuleName = "Voided", ModuleId = "5" });


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

                List<CustomClaimReports> customClaimVMs = new List<CustomClaimReports>();

                var mstUsers = await _repository.MstUser.GetAllUsersAsync("active");
                decimal ClaimsGrandTotal = 0;
                decimal ClaimsTotalAmount = 0;
                var mstMileageClaimsWithDetails = await _repository.MstMileageClaim.GetAllUserClaimsReportAsync(Convert.ToInt32(HttpContext.User.FindFirst("userid").Value), "", moduleName, facilityID, statusID, fromDate, toDate);
                foreach (var mc in mstMileageClaimsWithDetails)
                {
                    CustomClaimReports mileageClaimVM = new CustomClaimReports();
                    mileageClaimVM.CID = mc.CID;
                    mileageClaimVM.CNO = mc.CNO;
                    mileageClaimVM.Name = mc.Name;
                    mileageClaimVM.CreatedDate = Convert.ToDateTime(mc.CreatedDate).ToString("dd/MM/yyyy", System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
                    mileageClaimVM.ApprovalDate = Convert.ToDateTime(mc.ApprovalDate).ToString("dd/MM/yyyy", System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
                    mileageClaimVM.ExportAccPacDate = Convert.ToDateTime(mc.ExportAccPacDate).ToString("dd/MM/yyyy", System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
                    mileageClaimVM.ExportBankDate = Convert.ToDateTime(mc.ExportBankDate).ToString("dd/MM/yyyy", System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
                    mileageClaimVM.FacilityName = mc.FacilityName;
                    mileageClaimVM.Phone = mc.Phone;
                    mileageClaimVM.GrandTotal = mc.GrandTotal;
                    mileageClaimVM.TotalAmount = mc.TotalAmount;
                    mileageClaimVM.GST = mc.TotalAmount - mc.GrandTotal;
                    ClaimsGrandTotal = ClaimsGrandTotal + mc.GrandTotal;
                    ClaimsTotalAmount = ClaimsTotalAmount + mc.TotalAmount;
                    mileageClaimVM.AccountCode = mc.AccountCode;
                    mileageClaimVM.Description = mc.Description;
                    mileageClaimVM.ExpenseCategory = mc.ExpenseCategory;
                    mileageClaimVM.ApprovalStatus = mc.ApprovalStatus;
                    mileageClaimVM.VoucherNo = mc.VoucherNo;
                    mileageClaimVM.PayeeName = mc.PayeeName;

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
                _logger.LogInfo($"Returned all Mileage Claims with details from database.");

                var mstMileageClaimVM = new APClaimsReportViewModel
                {
                    //Screens = new SelectList(await screenQuery.Distinct().ToListAsync()),
                    customClaimVMs = customClaimVMs,
                    ReportTypes = new SelectList(reports, "Value", "Text"),
                    Facilities = new SelectList(facilities, "Value", "Text"),
                    Statuses = new SelectList(status, "Value", "Text"),
                    ClaimsGrandTotal = ClaimsGrandTotal,
                    ClaimsTotalAmount = ClaimsTotalAmount,
                    FromDate = fromDate,
                    ToDate = toDate
                };

                return View(mstMileageClaimVM);
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
            string filename = "UserClaims-Report-" + DateTime.Now.ToString("ddMMyyyyss") + ".xlsx";
            var path = "FileUploads/temp/";
            string pathToFilesold = System.IO.Path.Combine(path, filename);
            //FileInfo newFile = new FileInfo(pathToFilesold);
            //if (newFile.Exists)
            //{
            //    newFile.Delete();
            //}

            string Description = string.Empty;
            //if (aPExportSearch.ModuleName == "MileageClaim")
            //{
                //var mstMileageClaimsWithDetails = await _repository.MstMileageClaim.GetAllUserClaimsReportAsync(0, "", aPExportSearch.ModuleName, aPExportSearch.FacilityID, aPExportSearch.StatusID, aPExportSearch.FromDate, aPExportSearch.ToDate);
                var customClaimReports = await _repository.MstMileageClaim.GetAllUserClaimsReportAsync(Convert.ToInt32(HttpContext.User.FindFirst("userid").Value), "", aPExportSearch.ModuleName, Int32.Parse(aPExportSearch.FacilityID), Int32.Parse(aPExportSearch.StatusID), aPExportSearch.FromDate, aPExportSearch.ToDate);

                List<CustomClaimReports> expenseClaimVMs = new List<CustomClaimReports>();

                DataTable dt = new DataTable("Grid");
                dt.Columns.AddRange(new DataColumn[13] {
                                        new DataColumn("Date Created"),
                                        new DataColumn("Voucher No"),
                                        new DataColumn("Claim #"),
                                        new DataColumn("Requester"),
                                        new DataColumn("Payee"),
                                        new DataColumn("Account Code"),
                                        new DataColumn("Facility"),
                                        new DataColumn("Expense Type"),
                                        new DataColumn("Description"),
                                        new DataColumn("Amount(Excluding GST)"),
                                        new DataColumn("GST"),
                                        new DataColumn("Amount(Including GST)"),
                                        new DataColumn("Status")});

                decimal ClaimsGrandTotal = 0;
                decimal ClaimsTotalAmount = 0;

                foreach (var mc in customClaimReports)
                {
                    CustomClaimReports mileageClaimVM = new CustomClaimReports();
                    mileageClaimVM.ApprovalStatus = mc.ApprovalStatus;

                    if (mc.ApprovalStatus == 1)
                    {
                        mileageClaimVM.ClaimStatusName = "Awaiting Verification";

                    }
                    else if (mc.ApprovalStatus == 2)
                    {
                        mileageClaimVM.ClaimStatusName = "Awaiting Signatory approval";

                    }
                    else if (mc.ApprovalStatus == 3)
                    {
                        mileageClaimVM.ClaimStatusName = "Approved";

                    }
                    else if (mc.ApprovalStatus == 4)
                    {
                        mileageClaimVM.ClaimStatusName = "Request to Amend";
                    }
                    else if (mc.ApprovalStatus == 5)
                    {
                        mileageClaimVM.ClaimStatusName = "Voided";

                    }
                    else if (mc.ApprovalStatus == -5)
                    {
                        mileageClaimVM.ClaimStatusName = "Requested to Void";

                    }
                    else if (mc.ApprovalStatus == 6)
                    {
                        mileageClaimVM.ClaimStatusName = "Awaiting approval";

                    }
                    else if (mc.ApprovalStatus == 7)
                    {
                        mileageClaimVM.ClaimStatusName = "Awaiting HOD approval";

                    }
                    else if (mc.ApprovalStatus == 9)
                    {
                        mileageClaimVM.ClaimStatusName = "Exported to AccPac";

                    }
                    else if (mc.ApprovalStatus == 10)
                    {
                        mileageClaimVM.ClaimStatusName = "Exported to Bank";

                    }
                    else
                    {
                        mileageClaimVM.ClaimStatusName = "New";
                    }

                    ClaimsGrandTotal = ClaimsGrandTotal + mc.GrandTotal;
                    ClaimsTotalAmount = ClaimsTotalAmount + mc.TotalAmount;

                    dt.Rows.Add(mileageClaimVM.CreatedDate = Convert.ToDateTime(mc.CreatedDate).ToString("dd/MM/yyyy", System.Globalization.CultureInfo.CreateSpecificCulture("en-US")), 
                        mileageClaimVM.VoucherNo = mc.VoucherNo,
                        mileageClaimVM.CNO = mc.CNO,
                                mileageClaimVM.Name = mc.Name,
                                mileageClaimVM.PayeeName = mc.PayeeName,
                                mileageClaimVM.AccountCode = mc.AccountCode,
                                mileageClaimVM.FacilityName = mc.FacilityName,
                                mileageClaimVM.ExpenseCategory = mc.ExpenseCategory,
                                mileageClaimVM.Description = mc.Description,
                                mileageClaimVM.GrandTotal = mc.GrandTotal,
                                mileageClaimVM.GST = mc.TotalAmount - mc.GrandTotal,
                                mileageClaimVM.TotalAmount = mc.TotalAmount,
                                mileageClaimVM.ClaimStatusName = mileageClaimVM.ClaimStatusName);
                }

                dt.Rows.Add("", "", "", "", "", "", "", "", "Total",ClaimsGrandTotal,ClaimsTotalAmount-ClaimsGrandTotal,ClaimsTotalAmount);

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
                        return File(blobStream, file.Properties.ContentType, "UserClaimsReport.xlsx");
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

            //byte[] fileByteArray = System.IO.File.ReadAllBytes(fileName);
            //System.IO.File.Delete(fileName);
            //return File(fileByteArray, "application/vnd.ms-excel", "UserClaimsReport.xlsx");
        }

        public async Task<IActionResult> DownloadPdf(string fileName)
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
                        return File(blobStream, file.Properties.ContentType, "UserClaimsReport.pdf");
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
            //byte[] fileByteArray = System.IO.File.ReadAllBytes(fileName);
            //System.IO.File.Delete(fileName);
            //return File(fileByteArray, "application/pdf", "FinanceUserClaimsReport.pdf");
        }

        IPdfConverter pdfConverter = new SynchronizedConverter(new PdfTools());

        private byte[] BuildPdf(string HtmlContent, string Width, string Height, MarginSettings Margins, int? DPI = 180)
        {
            // Call the Convert method of SynchronizedConverter "pdfConverter"
            return _converter.Convert(new HtmlToPdfDocument()
            {
                // Set the html content
                Objects =
                {
                    new ObjectSettings
                    {
                         PagesCount = true,
                        //HtmlContent = TemplateGenerator.GetHTMLString(),
                        HtmlContent = HtmlContent,
                        //Page = "https://code-maze.com/", //USE THIS PROPERTY TO GENERATE PDF CONTENT FROM AN HTML PAGE
                        WebSettings = { DefaultEncoding = "utf-8", UserStyleSheet = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "css", "styles.css") },
                        //WebSettings = {DefaultEncoding="utf-8",UserStyleSheet=Path.com},
                        HeaderSettings = { FontName = "Arial", FontSize = 9, Right = "Page [page] of [toPage]", Line = true },
                        FooterSettings = { FontName = "Arial", FontSize = 9, Line = true, Center = "UEMS EClaims" }
                    }
                },
                // Set the configurations
                GlobalSettings = new GlobalSettings
                {
                    ColorMode = ColorMode.Color,
                    Orientation = Orientation.Landscape,
                    PaperSize = PaperKind.A4,
                    DPI = DPI,
                    Margins = new MarginSettings { Top = 10 },
                    DocumentTitle = "User Claims Report"
                }
            });
        }

        public async Task<JsonResult> ExporttoPdf(string data)
        {
            var aPExportSearch = JsonConvert.DeserializeObject<APExportSearch>(data);
            string filename = "UserClaims-Report-" + DateTime.Now.ToString("ddMMyyyyss") + ".pdf";
            var path = "FileUploads/temp/";
            string pathToFilesold = System.IO.Path.Combine(path, filename);
            //FileInfo newFile = new FileInfo(pathToFilesold);
            //if (newFile.Exists)
            //{
            //    newFile.Delete();
            //}

            string Description = string.Empty;
            var customClaimReports = await _repository.MstMileageClaim.GetAllUserClaimsReportAsync(Convert.ToInt32(HttpContext.User.FindFirst("userid").Value), "", aPExportSearch.ModuleName, Int32.Parse(aPExportSearch.FacilityID), Int32.Parse(aPExportSearch.StatusID), aPExportSearch.FromDate, aPExportSearch.ToDate);

            var sb = new StringBuilder();
            sb.Append(@"
                        <html>
                            <head>
                            </head>
                            <body>
                                <div class='header'><h1>User Claims Report</h1></div>
                                <table align='center'>
                                    <tr>
                                        <th>Date Created</th>
                                        <th>Voucher No</th>
                                        <th>Claim #</th>
                                        <th>Requester</th>
                                        <th>Payee</th>
                                        <th>Account Code</th>
                                        <th>Facility</th>
                                        <th>Expense Type</th>
                                        <th>Description</th>
                                        <th>Amount(Excl GST)</th>
                                        <th>GST</th>
                                        <th>Amount(Incl GST)</th>
                                        <th>Status</th>
                                    </tr>");
            decimal ClaimsGrandTotal = 0;
            decimal ClaimsTotalAmount = 0;
            foreach (var mc in customClaimReports)
            {

                    if (mc.ApprovalStatus == 1)
                    {
                        mc.ClaimStatusName = "Awaiting Verification";

                    }
                    else if (mc.ApprovalStatus == 2)
                    {
                        mc.ClaimStatusName = "Awaiting Signatory approval";

                    }
                    else if (mc.ApprovalStatus == 3)
                    {
                        mc.ClaimStatusName = "Approved";

                    }
                    else if (mc.ApprovalStatus == 4)
                    {
                        mc.ClaimStatusName = "Request to Amend";
                    }
                    else if (mc.ApprovalStatus == 5)
                    {
                        mc.ClaimStatusName = "Voided";

                    }
                    else if (mc.ApprovalStatus == -5)
                    {
                        mc.ClaimStatusName = "Requested to Void";

                    }
                    else if (mc.ApprovalStatus == 6)
                    {
                        mc.ClaimStatusName = "Awaiting approval";

                    }
                    else if (mc.ApprovalStatus == 7)
                    {
                        mc.ClaimStatusName = "Awaiting HOD approval";

                    }
                    else if (mc.ApprovalStatus == 9)
                    {
                        mc.ClaimStatusName = "Exported to AccPac";

                    }
                    else if (mc.ApprovalStatus == 10)
                    {
                        mc.ClaimStatusName = "Exported to Bank";

                    }
                    else
                    {
                        mc.ClaimStatusName = "New";
                    }

                    ClaimsGrandTotal = ClaimsGrandTotal + mc.GrandTotal;
                    ClaimsTotalAmount = ClaimsTotalAmount + mc.TotalAmount;

                    sb.AppendFormat(@"<tr>
                                    <td>{0}</td>
                                    <td>{1}</td>
                                    <td>{2}</td>
                                    <td>{3}</td>
                                    <td>{4}</td>
                                    <td>{5}</td>
                                    <td>{6}</td>
                                    <td>{7}</td>
                                    <td>{8}</td>
                                    <td>{9}</td>
                                    <td>{10}</td>
                                    <td>{11}</td>
                                    <td>{12}</td>
                                  </tr>", Convert.ToDateTime(mc.CreatedDate).ToString("dd/MM/yyyy", System.Globalization.CultureInfo.CreateSpecificCulture("en-US")), mc.VoucherNo, mc.CNO, mc.Name,mc.PayeeName,mc.AccountCode,
                                  mc.FacilityName,mc.ExpenseCategory,mc.Description,mc.GrandTotal, mc.TotalAmount - mc.GrandTotal, mc.TotalAmount,mc.ClaimStatusName);
            }
            sb.AppendFormat(@"<tr>
                                    <td>{0}</td>
                                    <td>{1}</td>
                                    <td>{2}</td>
                                    <td>{3}</td>
                                    <td>{4}</td>
                                    <td>{5}</td>
                                    <td>{6}</td>
                                    <td>{7}</td>
                                    <td>{8}</td>
                                    <td>{9}</td>
                                    <td>{10}</td>
                                    <td>{11}</td>
                                    <td>{12}</td>
                                  </tr>", string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty,"Total ", ClaimsGrandTotal, ClaimsTotalAmount - ClaimsGrandTotal, ClaimsTotalAmount,string.Empty);
            sb.Append(@"
                                </table>
                            </body>
                        </html>");


            // PDFByteArray is a byte array of pdf generated from the HtmlContent 
            var PDFByteArray = BuildPdf(sb.ToString(), "8.5in", "11in", new MarginSettings(0, 0, 0, 0));

            if (CloudStorageAccount.TryParse(_configuration.GetSection("ConnectionStrings")["BlobConnectionString"], out CloudStorageAccount storageAccount))
            {
                CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

                CloudBlobContainer container = blobClient.GetContainerReference(_configuration.GetSection("ConnectionStrings")["BlobContainerName"]);

                CloudBlockBlob blockBlob = container.GetBlockBlobReference(pathToFilesold);

                // Upload the pdf blob
                await blockBlob.UploadFromByteArrayAsync(PDFByteArray, 0, PDFByteArray.Length);
            }

            return Json(new { fileName = pathToFilesold });

        }
    }
}
