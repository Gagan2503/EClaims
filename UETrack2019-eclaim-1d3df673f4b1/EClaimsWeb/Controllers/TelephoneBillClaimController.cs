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
using NToastNotify;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace EClaimsWeb.Controllers
{
    [Authorize(Roles = "Admin,Finance,User,HR")]
    public class TelephoneBillClaimController : Controller
    {
        private ILoggerManager _logger;
        private IRepositoryWrapper _repository;
        private IMapper _mapper;
        private AlternateApproverHelper _alternateApproverHelper;
        private IConfiguration _configuration;
        private readonly RepositoryContext _context;
        private ISendMailServices _sendMailServices;

        private readonly IToastNotification _toastNotification;
        public TelephoneBillClaimController(IToastNotification toastNotification, ILoggerManager logger, IRepositoryWrapper repository, IMapper mapper, RepositoryContext context, IConfiguration configuration, ISendMailServices sendMailServices)
        {
            _logger = logger;
            _repository = repository;
            _mapper = mapper;
            _context = context;
            _sendMailServices = sendMailServices;
            _alternateApproverHelper = new AlternateApproverHelper(logger, repository, context);
            _toastNotification = toastNotification;
            _configuration = configuration;
        }

        public FileResult ExcelDownload()
        {
            /*
            DataTable dt = new DataTable("Grid");
            dt.Columns.AddRange(new DataColumn[7] { new DataColumn("Claimid"),
                                            new DataColumn("Username"),
                                           // new DataColumn("Claim Type"),
                                            new DataColumn("Telephone Bill Date"),
                                            new DataColumn("Facility"),
                                            new DataColumn("Description of Expense"),
                                            new DataColumn("Amount"),
                                            new DataColumn("Expense Category")});
            using (XLWorkbook wb = new XLWorkbook())
            {
                wb.Worksheets.Add(dt);
                using (MemoryStream stream = new MemoryStream())
                {
                    wb.SaveAs(stream);
                    return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "TelephoneBillTemplate.xlsx");
                }
            }
            */
            string id = "TelephoneBillTemplate.xlsm";

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

                        //cmd = new SqlCommand("delete from MstTBClaimtemp", con);
                        con.Open();
                        //cmd.ExecuteNonQuery();

                        sqlBulkCopy.DestinationTableName = "dbo.MstTBClaimtemp";

                        sqlBulkCopy.ColumnMappings.Add("UserName", "UserName");
                        //sqlBulkCopy.ColumnMappings.Add("EmailAddress", "EmailAddress");
                        //sqlBulkCopy.ColumnMappings.Add("Company", "Company");
                        //sqlBulkCopy.ColumnMappings.Add("Department", "Department");
                        //sqlBulkCopy.ColumnMappings.Add("Facility", "Facility");
                        //sqlBulkCopy.ColumnMappings.Add("DateofCreated", "DateofCreated");
                        //sqlBulkCopy.ColumnMappings.Add("Claim Type", "ClaimType");
                        sqlBulkCopy.ColumnMappings.Add("Telephone Bill Date", "DateofJourney");
                        sqlBulkCopy.ColumnMappings.Add("Facility", "Facility");
                        sqlBulkCopy.ColumnMappings.Add("Description of Expense", "DescriptionofExpense");
                        sqlBulkCopy.ColumnMappings.Add("Amount", "Amount");
                        sqlBulkCopy.ColumnMappings.Add("Claimid", "Claimid");
                        sqlBulkCopy.ColumnMappings.Add("Expense Category", "DescriptionofExpenseCatergory");
                        sqlBulkCopy.ColumnMappings.Add("Userid", "Userid");
                        sqlBulkCopy.ColumnMappings.Add("Facilityid", "FacilityID");
                        sqlBulkCopy.ColumnMappings.Add("Status", "Status");
                        sqlBulkCopy.WriteToServer(dt);
                    }
                }

                DataTable InvaildData = _repository.MstTBClaim.InsertExcel(Convert.ToInt32((HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value)), Convert.ToInt32(HttpContext.User.FindFirst("userid").Value));

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
                            var fileResult = await UploadTBFiles(FileInput);
                        }
                        var mstTBClaim = await _repository.MstTBClaim.GetTBClaimByIdAsync(cid);
                        if (mstTBClaim.ApprovalStatus == 1)
                        {
                            string VerifierIDs = "";
                            string ApproverIDs = "";
                            string UserApproverIDs = "";
                            string HODApproverID = "";
                            try
                            {
                                //VerifierIDs = mstTBClaim.Verifier.Split(',');
                                //VerifierIDs = string.Join(",", ExpenseverifierIDs.Skip(1));
                                string[] verifierIDs = mstTBClaim.Verifier.Split(',');
                                ApproverIDs = mstTBClaim.Approver;
                                HODApproverID = mstTBClaim.HODApprover;



                                //BackgroundJob.Enqueue(() => _sendMailServices.SendEmail());
                                //Mail Code Implementation for Verifiers

                                foreach (string verifierID in verifierIDs)
                                {
                                    if (verifierID != "")
                                    {
                                        string domainUrl = HttpContext.Request.Scheme + "://" + HttpContext.Request.Host;
                                        string clickUrl = domainUrl + "/" + "FinanceTBClaim/Details/" + mstTBClaim.TBCID;

                                        var mstSenderDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value));
                                        var senderName = mstSenderDetails.Name;
                                        var mstVerifierDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(verifierID));
                                        var toEmail = mstVerifierDetails.EmailAddress;
                                        var receiverName = mstVerifierDetails.Name;
                                        var claimNo = mstTBClaim.TBCNo;
                                        var screen = "Telephone Bill Claim";
                                        var approvalType = "Verification Request";
                                        int userID = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
                                        var subject = "Telephone Bill Claim for Verification " + claimNo;

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
                            string[] userApproverIDs = mstTBClaim.UserApprovers.ToString().Split(',');
                            foreach (string userApproverID in userApproverIDs)
                            {
                                if (userApproverID != "")
                                {
                                    string domainUrl = HttpContext.Request.Scheme + "://" + HttpContext.Request.Host;
                                    string clickUrl = domainUrl + "/" + "HodSummary/TBCDetails/" + mstTBClaim.TBCID;

                                    var mstSenderDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value));
                                    var senderName = mstSenderDetails.Name;
                                    var mstVerifierDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(userApproverID));
                                    var toEmail = mstVerifierDetails.EmailAddress;
                                    var receiverName = mstVerifierDetails.Name;
                                    var claimNo = mstTBClaim.TBCNo;
                                    var screen = "Telephone Bill Claim";
                                    var approvalType = "Approval Request";
                                    int userID = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
                                    var subject = "Telephone Bill Claim for Approval " + claimNo;

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
                        return RedirectToAction("Index", "TelephoneBillClaim");

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
                                return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "TelephoneBillTemplateValidate.xlsx");


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


            return RedirectToAction("Index", "TelephoneBillClaim");

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

                if (columnName == "Telephone Bill Date")
                {
                    DataColumn colDateTime = new DataColumn("Telephone Bill Date");
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
                    if (cell.Address.Contains("B"))
                    {
                        if (cell.Text != string.Empty)
                            newRow[cell.Start.Column - 1] = DateTime.Parse(cell.Text, new System.Globalization.CultureInfo("pt-BR"));
                    }
                    else if (cell.Address.Contains("E"))
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

                TBClaimsVM tBClaimsVM = new TBClaimsVM();

                var mstTBClaimsWithDetails = await _repository.MstTBClaim.GetAllTBClaimWithDetailsByFacilityIDAsync(Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value), Convert.ToInt32(HttpContext.User.FindFirst("delegatefacilityid") is null ? HttpContext.User.FindFirst("facilityid").Value : HttpContext.User.FindFirst("delegatefacilityid").Value));
                _logger.LogInfo($"Returned all Telephone Bill Claims with details from database.");
                List<TBClaimVM> tBClaimVMs = new List<TBClaimVM>();
                foreach (var mc in mstTBClaimsWithDetails)
                {
                    TBClaimVM tBClaimVM = new TBClaimVM();
                    tBClaimVM.TBCID = mc.TBCID;
                    tBClaimVM.TBCNo = mc.TBCNo;
                    tBClaimVM.Name = mc.MstUser.Name;
                    tBClaimVM.CreatedDate = Convert.ToDateTime(mc.CreatedDate).ToString("dd/MM/yyyy", System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
                    tBClaimVM.FacilityName = mc.MstFacility.FacilityName;
                    tBClaimVM.Phone = mc.MstUser.Phone;
                    tBClaimVM.GrandTotal = mc.GrandTotal;
                    tBClaimVM.ApprovalStatus = mc.ApprovalStatus;
                    tBClaimVM.VoucherNo = mc.VoucherNo;

                    DateTime date = new DateTime(mc.Year, mc.Month, 1);

                    tBClaimVM.MonthYear = date.ToString("MMM") + " " + mc.Year;

                    //tBClaimVM.ClaimType = mc.ClaimType;
                    //tBClaimVM.TotalAmount = mc.TotalAmount;
                    tBClaimVM.AVerifier = mc.Verifier;
                    tBClaimVM.AApprover = mc.Approver;
                    tBClaimVM.AUserApprovers = mc.UserApprovers;
                    tBClaimVM.AHODApprover = mc.HODApprover;

                    tBClaimVM.DVerifier = mc.DVerifier;
                    tBClaimVM.DApprover = mc.DApprover;
                    tBClaimVM.DUserApprovers = mc.DUserApprovers;
                    tBClaimVM.DHODApprover = mc.DHODApprover;


                    if (mc.UserApprovers != "")
                    {
                        tBClaimVM.Approver = mc.UserApprovers.Split(',').First();
                    }
                    else if (mc.Verifier != "")
                    {
                        tBClaimVM.Approver = mc.Verifier.Split(',').First();
                        //string VerifierIDs = string.Join(",", MileageverifierIDs.Skip(1));
                    }
                    else if (mc.HODApprover != "")
                    {
                        tBClaimVM.Approver = mc.HODApprover.Split(',').First();
                    }
                    else if (mc.Approver != "")
                    {
                        tBClaimVM.Approver = mc.Approver.Split(',').First();
                    }
                    else
                    {
                        tBClaimVM.Approver = "";
                    }

                    if (tBClaimVM.Approver != "")
                    {
                        var alternateUser = await _alternateApproverHelper.IsAlternateApprovalSetForUser(Convert.ToInt32(tBClaimVM.Approver));
                        if (alternateUser.HasValue)
                        {
                            var mstUserApprover = await _repository.MstUser.GetUserByIdAsync(alternateUser.Value);
                            tBClaimVM.Approver = mstUserApprover.Name + " (AA)";
                        }
                        else
                        {
                            var mstUserApprover = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(tBClaimVM.Approver));
                            tBClaimVM.Approver = mstUserApprover.Name;
                        }
                    }

                    tBClaimVMs.Add(tBClaimVM);
                }
                tBClaimsVM.tbClaims = tBClaimVMs;

                var mstTBClaimsWithDetailsDraft = await _repository.MstTBClaimDraft.GetAllTBClaimDraftWithDetailsByFacilityIDAsync(Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value), Convert.ToInt32(HttpContext.User.FindFirst("delegatefacilityid") is null ? HttpContext.User.FindFirst("facilityid").Value : HttpContext.User.FindFirst("delegatefacilityid").Value));
                List<TBClaimVM> tBClaimDraftVM = new List<TBClaimVM>();
                foreach (var mc in mstTBClaimsWithDetailsDraft)
                {
                    TBClaimVM tBClaimVM = new TBClaimVM();
                    tBClaimVM.TBCID = mc.TBCID;
                    tBClaimVM.TBCNo = mc.TBCNo;
                    tBClaimVM.Name = mc.MstUser.Name;
                    tBClaimVM.CreatedDate = Convert.ToDateTime(mc.CreatedDate).ToString("dd/MM/yyyy", System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
                    tBClaimVM.FacilityName = mc.MstFacility.FacilityName;
                    tBClaimVM.Phone = mc.MstUser.Phone;
                    tBClaimVM.GrandTotal = mc.GrandTotal;
                    tBClaimVM.ApprovalStatus = mc.ApprovalStatus;

                    DateTime date = new DateTime(mc.Year, mc.Month, 1);

                    tBClaimVM.MonthYear = date.ToString("MMM") + " " + mc.Year;

                    //tBClaimVM.ClaimType = mc.ClaimType;
                    //tBClaimVM.TotalAmount = mc.TotalAmount;
                    if (mc.UserApprovers != "")
                    {
                        tBClaimVM.Approver = mc.UserApprovers.Split(',').First();
                    }
                    else if (mc.Verifier != "")
                    {
                        tBClaimVM.Approver = mc.Verifier.Split(',').First();
                        //string VerifierIDs = string.Join(",", MileageverifierIDs.Skip(1));
                    }
                    else if (mc.HODApprover != "")
                    {
                        tBClaimVM.Approver = mc.HODApprover.Split(',').First();
                    }
                    else if (mc.Approver != "")
                    {
                        tBClaimVM.Approver = mc.Approver.Split(',').First();
                    }
                    else
                    {
                        tBClaimVM.Approver = "";
                    }

                    if (tBClaimVM.Approver != "")
                    {
                        var mstUserApprover = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(tBClaimVM.Approver));
                        tBClaimVM.Approver = mstUserApprover.Name;
                    }

                    tBClaimDraftVM.Add(tBClaimVM);
                }
                tBClaimsVM.tbClaimsDrafts = tBClaimDraftVM;
                //var mstExpenseCategoriesWithTypesResult = _mapper.Map<IEnumerable<MstExpenseCategory>>(mstExpenseCategoriesWithTypes);
                return View(tBClaimsVM);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Something went wrong inside GetAllTBClaimWithDetailsByFacilityIDAsync action: {ex.Message}");
                return View();
            }
        }

        public async Task<IActionResult> Create(string id, string Updatestatus)
        {
            TempData["Updatestatus"] = "Add";
            TempData["claimaddcondition"] = "claimnew";
            TBClaimDetailVM tBClaimDetailVM = new TBClaimDetailVM();
            tBClaimDetailVM.DtTBClaimVMs = new List<DtTBClaimVM>();
            tBClaimDetailVM.TBClaimAudits = new List<TBClaimAuditVM>();

            if (User != null && User.Identity.IsAuthenticated)
            {
                if (!string.IsNullOrEmpty(id))
                {
                    long idd = Convert.ToInt64(id);
                    ViewBag.CID = idd;
                    var dtTBClaims = await _repository.DtTBClaim.GetDtTBClaimByIdAsync(idd);

                    tBClaimDetailVM.Month = DateTime.Now;

                    // List<clsDtSupplierPO> oclsDtSupplierPO = new List<clsDtSupplierPO>();
                    foreach (var item in dtTBClaims)
                    {
                        DtTBClaimVM dtTBClaimVM = new DtTBClaimVM();

                        dtTBClaimVM.TBCItemID = item.TBCItemID;
                        dtTBClaimVM.TBCID = item.TBCID;
                        dtTBClaimVM.DateOfJourney = item.Date;

                        dtTBClaimVM.Description = item.Description;
                        dtTBClaimVM.Amount = item.Amount;
                        //dtTBClaimVM.Gst = item.GST;
                        //dtTBClaimVM.AmountWithGST = item.Amount + item.GST;
                        dtTBClaimVM.ExpenseCategory = item.MstExpenseCategory.Description;
                        dtTBClaimVM.AccountCode = item.AccountCode;
                        if (Updatestatus == "Recreate")
                        {
                            ViewBag.UpdateStatus = "Recreate";
                            dtTBClaimVM.TBCItemID = 0;
                        }
                        tBClaimDetailVM.DtTBClaimVMs.Add(dtTBClaimVM);
                    }

                    tBClaimDetailVM.TBClaimFileUploads = new List<DtTBClaimFileUpload>();
                    var fileUploads = await _repository.DtTBClaimFileUpload.GetDtTBClaimAuditByIdAsync(idd);
                    if (Updatestatus == "Recreate" && fileUploads != null && fileUploads.Count > 0)
                    {
                        foreach (var uploaddata in fileUploads)
                        {
                            uploaddata.TBCID = 0;
                            tBClaimDetailVM.TBClaimFileUploads.Add(uploaddata);
                        }
                    }
                    else
                        tBClaimDetailVM.TBClaimFileUploads = fileUploads;

                    var mstTBClaim = await _repository.MstTBClaim.GetTBClaimByIdAsync(idd);

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

                    //    tBClaimDetailVM.DtExpenseClaimVMs.Add(dtExpenseClaimVM);
                    //}
                    //tBClaimDetailVM.ExpenseClaimAudits = new List<MstExpenseClaimAudit>();

                    //tBClaimDetailVM.ExpenseClaimAudits = _repository.MstExpenseClaimAudit.GetMstExpenseClaimAuditByIdAsync(id).Result.ToList();

                    //tBClaimDetailVM.ExpenseClaimFileUploads = new List<DtExpenseClaimFileUpload>();

                    //tBClaimDetailVM.ExpenseClaimFileUploads = _repository.DtExpenseClaimFileUpload.GetDtExpenseClaimAuditByIdAsync(id).Result.ToList();

                    TBClaimVM tBClaimVM = new TBClaimVM();
                    //tBClaimVM.ClaimType = mstExpenseClaim.ClaimType;
                    tBClaimVM.GrandTotal = mstTBClaim.GrandTotal;
                    //tBClaimVM.TotalAmount = mstExpenseClaim.TotalAmount;
                    tBClaimVM.Company = mstTBClaim.Company;
                    tBClaimVM.Name = mstTBClaim.MstUser.Name;
                    tBClaimVM.DepartmentName = mstTBClaim.MstDepartment.Department;
                    tBClaimVM.FacilityName = mstTBClaim.MstFacility.FacilityName;
                    tBClaimVM.CreatedDate = mstTBClaim.CreatedDate.ToString("d");
                    tBClaimVM.Verifier = mstTBClaim.Verifier;
                    tBClaimVM.Approver = mstTBClaim.Approver;
                    tBClaimVM.TBCNo = mstTBClaim.TBCNo;
                    DateTime date = new DateTime(mstTBClaim.Year, mstTBClaim.Month, 1);
                    tBClaimVM.MonthYearDate = date;
                    tBClaimVM.MonthYear = date.ToString("MMM") + " " + mstTBClaim.Year;


                    tBClaimDetailVM.TBClaimVM = tBClaimVM;

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
                    tBClaimDetailVM.TBClaimAudits = new List<TBClaimAuditVM>();
                    tBClaimDetailVM.TBClaimFileUploads = new List<DtTBClaimFileUpload>();
                    TBClaimVM tBClaimVM = new TBClaimVM();
                    //tBClaimVM.ClaimType = "";
                    tBClaimVM.GrandTotal = 0;
                    //tBClaimVM.TotalAmount = 0;
                    tBClaimVM.Company = "";
                    tBClaimVM.Name = "";
                    tBClaimVM.DepartmentName = "";
                    tBClaimVM.FacilityName = "";
                    tBClaimVM.CreatedDate = "";
                    tBClaimVM.Verifier = "";
                    tBClaimVM.Approver = "";
                    tBClaimVM.TBCNo = "";

                    DtTBClaimVM dtTBClaimVM = new DtTBClaimVM();

                    dtTBClaimVM.TBCItemID = 0;
                    dtTBClaimVM.TBCID = 0;
                    //dtTBClaimVM.DateOfJourney = "";

                    dtTBClaimVM.Description = "";
                    dtTBClaimVM.Amount = 0;
                    dtTBClaimVM.Gst = 0;
                    //dtTBClaimVM.AmountWithGST = 0;
                    dtTBClaimVM.ExpenseCategory = "";
                    dtTBClaimVM.AccountCode = "";

                    tBClaimDetailVM.DtTBClaimVMs.Add(dtTBClaimVM);
                    tBClaimDetailVM.TBClaimVM = tBClaimVM;


                    TempData["status"] = "Add";
                }

                ViewData["ExpenseCategoryID"] = new SelectList(await _repository.MstExpenseCategory.GetAllExpenseCategoriesByClaimTypesAsync("telephone bill", "active"), "ExpenseCategoryID", "Description");
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
                ViewBag.TelephoneClaimLimit = mstUsersWithDetails.TelephoneLimit;

            }
            return View(tBClaimDetailVM);
        }

        public async Task<IActionResult> CreateDraft(string id, string Updatestatus)
        {
            TempData["Updatestatus"] = "Add";
            TempData["claimaddcondition"] = "claimDraft";
            TBClaimDetailVM tBClaimDetailVM = new TBClaimDetailVM();
            tBClaimDetailVM.DtTBClaimVMs = new List<DtTBClaimVM>();
            tBClaimDetailVM.TBClaimAudits = new List<TBClaimAuditVM>();

            if (User != null && User.Identity.IsAuthenticated)
            {
                if (!string.IsNullOrEmpty(id))
                {
                    long idd = Convert.ToInt64(id);
                    ViewBag.CID = idd;
                    var dtTBClaims = await _repository.DtTBClaimDraft.GetDtTBClaimDraftByIdAsync(idd);

                    tBClaimDetailVM.Month = DateTime.Now;

                    // List<clsDtSupplierPO> oclsDtSupplierPO = new List<clsDtSupplierPO>();
                    foreach (var item in dtTBClaims)
                    {
                        DtTBClaimVM dtTBClaimVM = new DtTBClaimVM();

                        dtTBClaimVM.TBCItemID = item.TBCItemID;
                        dtTBClaimVM.TBCID = item.TBCID;
                        dtTBClaimVM.DateOfJourney = item.Date;

                        dtTBClaimVM.Description = item.Description;
                        dtTBClaimVM.Amount = item.Amount;
                        //dtTBClaimVM.Gst = item.GST;
                        //dtTBClaimVM.AmountWithGST = item.Amount + item.GST;
                        dtTBClaimVM.ExpenseCategory = item.MstExpenseCategory.Description;
                        dtTBClaimVM.AccountCode = item.AccountCode;
                        tBClaimDetailVM.DtTBClaimVMs.Add(dtTBClaimVM);
                    }

                    tBClaimDetailVM.TBClaimFileUploads = new List<DtTBClaimFileUpload>();

                    var tbFileUploads = await _repository.DtTBClaimFileUploadDraft.GetDtTBClaimDraftAuditByIdAsync(idd);

                    foreach (var item in tbFileUploads)
                    {
                        MstTBClaim mstTBClaim1 = new MstTBClaim();
                        if (item.MstTBClaimDraft != null)
                        {
                            mstTBClaim1 = new MstTBClaim()
                            {
                                ApprovalBy = item.MstTBClaimDraft.ApprovalBy,
                                ApprovalDate = item.MstTBClaimDraft.ApprovalDate,
                                ApprovalStatus = item.MstTBClaimDraft.ApprovalStatus,
                                ModifiedDate = item.MstTBClaimDraft.ModifiedDate,
                                ModifiedBy = item.MstTBClaimDraft.ModifiedBy,
                                Approver = item.MstTBClaimDraft.Approver,
                                Company = item.MstTBClaimDraft.Company,
                                CreatedBy = item.MstTBClaimDraft.CreatedBy,
                                CreatedDate = item.MstTBClaimDraft.CreatedDate,
                                DepartmentID = item.MstTBClaimDraft.DepartmentID,
                                TBCID = item.MstTBClaimDraft.TBCID,
                                TBCNo = item.MstTBClaimDraft.TBCNo,
                                FacilityID = item.MstTBClaimDraft.FacilityID,
                                FinalApprover = item.MstTBClaimDraft.FinalApprover,
                                GrandTotal = item.MstTBClaimDraft.GrandTotal,
                                HODApprover = item.MstTBClaimDraft.HODApprover,
                                MstDepartment = item.MstTBClaimDraft.MstDepartment,
                                MstFacility = item.MstTBClaimDraft.MstFacility,
                                MstUser = item.MstTBClaimDraft.MstUser,
                                TnC = item.MstTBClaimDraft.TnC,
                                Month = item.MstTBClaimDraft.Month,
                                Year = item.MstTBClaimDraft.Year,
                                UserApprovers = item.MstTBClaimDraft.UserApprovers,
                                UserID = item.MstTBClaimDraft.UserID,
                                Verifier = item.MstTBClaimDraft.Verifier,
                                VoidReason = item.MstTBClaimDraft.VoidReason
                            };
                        }

                        tBClaimDetailVM.TBClaimFileUploads.Add(new DtTBClaimFileUpload()
                        {
                            CreatedBy = item.CreatedBy,
                            CreatedDate = item.CreatedDate,
                            TBCID = item.TBCID,
                            FileID = item.FileID,
                            FileName = item.FileName,
                            FilePath = item.FilePath,
                            IsDeleted = item.IsDeleted,
                            ModifiedBy = item.ModifiedBy,
                            ModifiedDate = item.ModifiedDate,
                            MstTBClaim = mstTBClaim1
                        });
                    }

                    var mstTBClaim = await _repository.MstTBClaimDraft.GetTBClaimDraftByIdAsync(idd);

                    TBClaimVM tBClaimVM = new TBClaimVM();
                    //tBClaimVM.ClaimType = mstExpenseClaim.ClaimType;
                    tBClaimVM.GrandTotal = mstTBClaim.GrandTotal;
                    //tBClaimVM.TotalAmount = mstExpenseClaim.TotalAmount;
                    tBClaimVM.Company = mstTBClaim.Company;
                    tBClaimVM.Name = mstTBClaim.MstUser.Name;
                    tBClaimVM.DepartmentName = mstTBClaim.MstDepartment.Department;
                    tBClaimVM.FacilityName = mstTBClaim.MstFacility.FacilityName;
                    tBClaimVM.CreatedDate = mstTBClaim.CreatedDate.ToString("d");
                    tBClaimVM.Verifier = mstTBClaim.Verifier;
                    tBClaimVM.Approver = mstTBClaim.Approver;
                    tBClaimVM.TBCNo = mstTBClaim.TBCNo;
                    DateTime date = new DateTime(mstTBClaim.Year, mstTBClaim.Month, 1);
                    tBClaimVM.MonthYearDate = date;
                    tBClaimVM.MonthYear = date.ToString("MMM") + " " + mstTBClaim.Year;

                    tBClaimDetailVM.TBClaimVM = tBClaimVM;

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
                    tBClaimDetailVM.TBClaimAudits = new List<TBClaimAuditVM>();
                    tBClaimDetailVM.TBClaimFileUploads = new List<DtTBClaimFileUpload>();
                    TBClaimVM tBClaimVM = new TBClaimVM();
                    //tBClaimVM.ClaimType = "";
                    tBClaimVM.GrandTotal = 0;
                    //tBClaimVM.TotalAmount = 0;
                    tBClaimVM.Company = "";
                    tBClaimVM.Name = "";
                    tBClaimVM.DepartmentName = "";
                    tBClaimVM.FacilityName = "";
                    tBClaimVM.CreatedDate = "";
                    tBClaimVM.Verifier = "";
                    tBClaimVM.Approver = "";
                    tBClaimVM.TBCNo = "";

                    DtTBClaimVM dtTBClaimVM = new DtTBClaimVM();

                    dtTBClaimVM.TBCItemID = 0;
                    dtTBClaimVM.TBCID = 0;
                    //dtTBClaimVM.DateOfJourney = "";

                    dtTBClaimVM.Description = "";
                    dtTBClaimVM.Amount = 0;
                    dtTBClaimVM.Gst = 0;
                    //dtTBClaimVM.AmountWithGST = 0;
                    dtTBClaimVM.ExpenseCategory = "";
                    dtTBClaimVM.AccountCode = "";

                    tBClaimDetailVM.DtTBClaimVMs.Add(dtTBClaimVM);
                    tBClaimDetailVM.TBClaimVM = tBClaimVM;

                    TempData["status"] = "Add";
                }

                ViewData["ExpenseCategoryID"] = new SelectList(await _repository.MstExpenseCategory.GetAllExpenseCategoriesByClaimTypesAsync("telephone bill", "active"), "ExpenseCategoryID", "Description");
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
                ViewBag.TelephoneClaimLimit = mstUsersWithDetails.TelephoneLimit;

            }
            return View("Create", tBClaimDetailVM);
        }

        public async Task<IActionResult> Details(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            long TBCID = Convert.ToInt64(id);

            if (User != null && User.Identity.IsAuthenticated)
            {
                var mstTBClaim = await _repository.MstTBClaim.GetTBClaimByIdAsync(id);

                if (mstTBClaim == null)
                {
                    return NotFound();
                }

                var dtTBSummaries = await _repository.DtTBClaimSummary.GetDtTBClaimSummaryByIdAsync(id);
                var dtTBClaims = await _repository.DtTBClaim.GetDtTBClaimByIdAsync(id);
                TBClaimDetailVM tBClaimDetailVM = new TBClaimDetailVM();
                //List<DtMileageClaimVM> dtMileageClaimVMs = new List<DtMileageClaimVM>();
                tBClaimDetailVM.DtTBClaimVMs = new List<DtTBClaimVM>();
                // List<clsDtSupplierPO> oclsDtSupplierPO = new List<clsDtSupplierPO>();
                foreach (var item in dtTBClaims)
                {
                    DtTBClaimVM dtTBClaimVM = new DtTBClaimVM();

                    dtTBClaimVM.TBCItemID = item.TBCItemID;
                    dtTBClaimVM.TBCID = item.TBCID;
                    dtTBClaimVM.DateOfJourney = item.Date;

                    dtTBClaimVM.Description = item.Description;
                    dtTBClaimVM.Amount = item.Amount;
                    dtTBClaimVM.ExpenseCategory = item.MstExpenseCategory.Description;
                    dtTBClaimVM.AccountCode = item.AccountCode;
                    if (item.FacilityID != null)
                    {
                        var mstFacility = await _repository.MstFacility.GetFacilityByIdAsync(item.FacilityID);
                        dtTBClaimVM.Facility = mstFacility.FacilityName;
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

                    tBClaimDetailVM.DtTBClaimVMs.Add(dtTBClaimVM);
                }

                tBClaimDetailVM.DtTBClaimSummaries = dtTBSummaries;
                var GroupByQS = tBClaimDetailVM.DtTBClaimVMs.GroupBy(s => s.AccountCode);
                //var GroupByQS = (from std in tBClaimDetailVM.DtExpenseClaimVMs
                //                                                           group std by std.ExpenseCategoryID);

                tBClaimDetailVM.DtTBClaimVMSummary = new List<DtTBClaimVM>();

                foreach (var group in GroupByQS)
                {
                    DtTBClaimVM dtTBClaimVM = new DtTBClaimVM();
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
                        //gst = gst + dtExpense.Gst;
                        //sumamount = sumamount + dtExpense.AmountWithGST;
                        ExpenseCat = dtExpense.ExpenseCategory;
                        AccountCode = dtExpense.AccountCode;
                    }
                    gst = gst / group.Count();
                    dtTBClaimVM.Description = ExpenseDesc;
                    dtTBClaimVM.ExpenseCategory = ExpenseCat;
                    dtTBClaimVM.AccountCode = AccountCode;
                    dtTBClaimVM.Amount = amount;
                    dtTBClaimVM.Gst = gst;
                    //dtTBClaimVM.AmountWithGST = sumamount;
                    tBClaimDetailVM.DtTBClaimVMSummary.Add(dtTBClaimVM);
                }

                tBClaimDetailVM.TBClaimAudits = new List<TBClaimAuditVM>();

                var dtTBClaimAudits = await _repository.MstTBClaimAudit.GetMstTBClaimAuditByIdAsync(id);

                foreach (var item in dtTBClaimAudits)
                {
                    TBClaimAuditVM mstTBClaimAuditVM = new TBClaimAuditVM();
                    mstTBClaimAuditVM.Action = item.Action;
                    mstTBClaimAuditVM.Description = item.Description;
                    mstTBClaimAuditVM.AuditDateTickle = Helper.RelativeDate(item.AuditDate);
                    tBClaimDetailVM.TBClaimAudits.Add(mstTBClaimAuditVM);
                }

                tBClaimDetailVM.TBClaimFileUploads = new List<DtTBClaimFileUpload>();

                tBClaimDetailVM.TBClaimFileUploads = _repository.DtTBClaimFileUpload.GetDtTBClaimAuditByIdAsync(id).Result.ToList();

                TBClaimVM tBClaimVM = new TBClaimVM();
                tBClaimVM.VoucherNo = mstTBClaim.VoucherNo;
                tBClaimVM.Month = mstTBClaim.Month;
                tBClaimVM.Year = mstTBClaim.Year;
                tBClaimVM.GrandTotal = mstTBClaim.GrandTotal;
                tBClaimVM.Company = mstTBClaim.Company;
                tBClaimVM.Name = mstTBClaim.MstUser.Name;
                tBClaimVM.DepartmentName = mstTBClaim.MstDepartment.Department;
                tBClaimVM.FacilityName = mstTBClaim.MstFacility.FacilityName;
                tBClaimVM.CreatedDate = mstTBClaim.CreatedDate.ToString("d");
                tBClaimVM.Verifier = mstTBClaim.Verifier;
                tBClaimVM.Approver = mstTBClaim.Approver;
                ViewBag.TBCID = id;
                tBClaimVM.TBCNo = mstTBClaim.TBCNo;
                TempData["CreatedBy"] = mstTBClaim.CreatedBy;
                ViewBag.Approvalstatus = mstTBClaim.ApprovalStatus;

                if (mstTBClaim.Verifier == mstTBClaim.DVerifier && mstTBClaim.Approver == mstTBClaim.DApprover && mstTBClaim.UserApprovers == mstTBClaim.DUserApprovers && mstTBClaim.HODApprover == mstTBClaim.DHODApprover)
                {
                    ViewBag.UserEditStatus = 4;
                }
                else
                {
                    ViewBag.UserEditStatus = 0;
                }

                TempData["ApprovedStatus"] = mstTBClaim.ApprovalStatus;
                TempData["FinalApproverID"] = mstTBClaim.FinalApprover;
                ViewBag.VoidReason = mstTBClaim.VoidReason == null ? "" : mstTBClaim.VoidReason;
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
                if (mstTBClaim.Verifier != "")
                {
                    string[] verifierIDs = mstTBClaim.Verifier.Split(',');
                    TempData["QueryMCVerifierIDs"] = string.Join(",", verifierIDs);
                    foreach (string verifierID in verifierIDs)
                    {
                        if (verifierID != "" && verifierID == (HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value) && User.IsInRole("Finance"))
                        {
                            TempData["ApprovedStatus"] = mstTBClaim.ApprovalStatus;
                            //Session["ApprovedStatus"] = oSupplierPO.Approvalstatus;
                            TempData["VerifierIDs"] = string.Join(",", verifierIDs.Skip(1));
                        }
                        else
                        {
                            TempData["ApprovedStatus"] = "";
                            //Session["ApprovedStatus"] = oSupplierPO.Approvalstatus;
                            TempData["VerifierIDs"] = mstTBClaim.Verifier;
                        }
                        TempData["ApproverIDs"] = mstTBClaim.Approver;
                        break;
                    }
                }
                else
                {
                    TempData["VerifierIDs"] = mstTBClaim.Verifier;
                    TempData["ApproverIDs"] = mstTBClaim.Approver;
                }

                //Approval Process code
                if (mstTBClaim.Approver != "" && mstTBClaim.Verifier == "")
                {
                    string[] approverIDs = mstTBClaim.Approver.Split(',');
                    TempData["QueryMCApproverIDs"] = string.Join(",", approverIDs);
                    foreach (string approverID in approverIDs)
                    {
                        if (approverID != "" && approverID == (HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value) && User.IsInRole("Finance"))
                        {
                            TempData["ApprovedStatus"] = mstTBClaim.ApprovalStatus;
                            //Session["ApprovedStatus"] = oSupplierPO.Approvalstatus;
                            TempData["ApproverIDs"] = string.Join(",", approverIDs.Skip(1));
                        }
                        else
                        {
                            TempData["ApprovedStatus"] = "";
                            //Session["ApprovedStatus"] = oSupplierPO.Approvalstatus;
                            TempData["ApproverIDs"] = mstTBClaim.Approver;
                        }
                        break;
                    }
                }
                else
                {
                    string[] approverIDs = mstTBClaim.Approver.Split(',');
                    TempData["QueryMCApproverIDs"] = string.Join(",", approverIDs);
                }

                if (mstTBClaim.UserApprovers != "" && mstTBClaim.Verifier == "")
                {
                    string[] userApproverIDs = mstTBClaim.UserApprovers.Split(',');
                    TempData["QueryMCUserApproverIDs"] = string.Join(",", userApproverIDs);
                    foreach (string approverID in userApproverIDs)
                    {
                        if (approverID != "" && approverID == (HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value))
                        {
                            TempData["ApprovedStatus"] = mstTBClaim.ApprovalStatus;
                            //Session["ApprovedStatus"] = oSupplierPO.Approvalstatus;
                            TempData["HODApproverIDs"] = string.Join(",", userApproverIDs.Skip(1));
                        }
                        else
                        {
                            TempData["ApprovedStatus"] = "";
                            //Session["ApprovedStatus"] = oSupplierPO.Approvalstatus;
                            TempData["UserApproverIDs"] = mstTBClaim.UserApprovers;
                        }
                        break;
                    }
                }
                else
                {
                    string[] userApproverIDs = mstTBClaim.UserApprovers.Split(',');
                    TempData["QueryMCUserApproverIDs"] = string.Join(",", userApproverIDs);
                }

                if (mstTBClaim.HODApprover != "" && mstTBClaim.Verifier == "")
                {
                    string[] hodApproverIDs = mstTBClaim.HODApprover.Split(',');
                    TempData["QueryMCHODApproverIDs"] = string.Join(",", hodApproverIDs);
                    foreach (string approverID in hodApproverIDs)
                    {
                        if (approverID != "" && approverID == (HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value))
                        {
                            TempData["ApprovedStatus"] = mstTBClaim.ApprovalStatus;
                            //Session["ApprovedStatus"] = oSupplierPO.Approvalstatus;
                            TempData["HODApproverIDs"] = string.Join(",", hodApproverIDs.Skip(1));
                        }
                        else
                        {
                            TempData["ApprovedStatus"] = "";
                            //Session["ApprovedStatus"] = oSupplierPO.Approvalstatus;
                            TempData["HODApproverIDs"] = mstTBClaim.HODApprover;
                        }
                        break;
                    }
                }
                else
                {
                    string[] hodApproverIDs = mstTBClaim.HODApprover.Split(',');
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
                var mstMileageClaimAudits = await _repository.MstMileageClaimAudit.GetMstMileageClaimAuditByIdAsync(TBCID);
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




                tBClaimDetailVM.TBClaimVM = tBClaimVM;
                //mileageClaimDetailVM.DtMileageClaimVMs = dtMileageClaimVMs;



                return View(tBClaimDetailVM);
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
            long TBCID = Convert.ToInt64(id);

            if (User != null && User.Identity.IsAuthenticated)
            {
                var mstTBClaim = await _repository.MstTBClaimDraft.GetTBClaimDraftByIdAsync(id);

                if (mstTBClaim == null)
                {
                    return NotFound();
                }

                var dtTBSummaries = await _repository.DtTBClaimSummaryDraft.GetDtTBClaimSummaryDraftByIdAsync(id);
                var dtTBClaims = await _repository.DtTBClaimDraft.GetDtTBClaimDraftByIdAsync(id);
                TBClaimDetailVM tBClaimDetailVM = new TBClaimDetailVM();
                //List<DtMileageClaimVM> dtMileageClaimVMs = new List<DtMileageClaimVM>();
                tBClaimDetailVM.DtTBClaimVMs = new List<DtTBClaimVM>();
                // List<clsDtSupplierPO> oclsDtSupplierPO = new List<clsDtSupplierPO>();
                foreach (var item in dtTBClaims)
                {
                    DtTBClaimVM dtTBClaimVM = new DtTBClaimVM();

                    dtTBClaimVM.TBCItemID = item.TBCItemID;
                    dtTBClaimVM.TBCID = item.TBCID;
                    dtTBClaimVM.DateOfJourney = item.Date;

                    dtTBClaimVM.Description = item.Description;
                    dtTBClaimVM.Amount = item.Amount;
                    dtTBClaimVM.ExpenseCategory = item.MstExpenseCategory.Description;
                    dtTBClaimVM.AccountCode = item.AccountCode;
                    if (item.FacilityID != null)
                    {
                        var mstFacility = await _repository.MstFacility.GetFacilityByIdAsync(item.FacilityID);
                        dtTBClaimVM.Facility = mstFacility.FacilityName;
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

                    tBClaimDetailVM.DtTBClaimVMs.Add(dtTBClaimVM);
                }

                tBClaimDetailVM.DtTBClaimSummaries = new List<DtTBClaimSummary>();

                foreach (var item in dtTBSummaries)
                {
                    MstTBClaim mstTBClaim1 = new MstTBClaim();
                    if (item.MstTBClaimDraft != null)
                    {
                        mstTBClaim1 = new MstTBClaim()
                        {
                            ApprovalBy = item.MstTBClaimDraft.ApprovalBy,
                            ApprovalDate = item.MstTBClaimDraft.ApprovalDate,
                            ApprovalStatus = item.MstTBClaimDraft.ApprovalStatus,
                            ModifiedDate = item.MstTBClaimDraft.ModifiedDate,
                            ModifiedBy = item.MstTBClaimDraft.ModifiedBy,
                            Approver = item.MstTBClaimDraft.Approver,
                            Company = item.MstTBClaimDraft.Company,
                            CreatedBy = item.MstTBClaimDraft.CreatedBy,
                            CreatedDate = item.MstTBClaimDraft.CreatedDate,
                            DepartmentID = item.MstTBClaimDraft.DepartmentID,
                            TBCID = item.MstTBClaimDraft.TBCID,
                            TBCNo = item.MstTBClaimDraft.TBCNo,
                            FacilityID = item.MstTBClaimDraft.FacilityID,
                            FinalApprover = item.MstTBClaimDraft.FinalApprover,
                            GrandTotal = item.MstTBClaimDraft.GrandTotal,
                            HODApprover = item.MstTBClaimDraft.HODApprover,
                            MstDepartment = item.MstTBClaimDraft.MstDepartment,
                            MstFacility = item.MstTBClaimDraft.MstFacility,
                            MstUser = item.MstTBClaimDraft.MstUser,
                            TnC = item.MstTBClaimDraft.TnC,
                            Month = item.MstTBClaimDraft.Month,
                            Year = item.MstTBClaimDraft.Year,
                            UserApprovers = item.MstTBClaimDraft.UserApprovers,
                            UserID = item.MstTBClaimDraft.UserID,
                            Verifier = item.MstTBClaimDraft.Verifier,
                            VoidReason = item.MstTBClaimDraft.VoidReason
                        };
                    }

                    tBClaimDetailVM.DtTBClaimSummaries.Add(new DtTBClaimSummary()
                    {
                        AccountCode = item.AccountCode,
                        Amount = item.Amount,
                        AmountWithGST = item.AmountWithGST,
                        CItemID = item.CItemID,
                        Date = item.Date,
                        Description = item.Description,
                        TBCID = item.TBCID,
                        ExpenseCategory = item.ExpenseCategory,
                        GST = item.GST,
                        MstTBClaim = mstTBClaim1,
                        TaxClass = item.TaxClass
                    });
                }

                var GroupByQS = tBClaimDetailVM.DtTBClaimVMs.GroupBy(s => s.AccountCode);
                //var GroupByQS = (from std in tBClaimDetailVM.DtExpenseClaimVMs
                //                                                           group std by std.ExpenseCategoryID);

                tBClaimDetailVM.DtTBClaimVMSummary = new List<DtTBClaimVM>();

                foreach (var group in GroupByQS)
                {
                    DtTBClaimVM dtTBClaimVM = new DtTBClaimVM();
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
                        //gst = gst + dtExpense.Gst;
                        //sumamount = sumamount + dtExpense.AmountWithGST;
                        ExpenseCat = dtExpense.ExpenseCategory;
                        AccountCode = dtExpense.AccountCode;
                    }
                    gst = gst / group.Count();
                    dtTBClaimVM.Description = ExpenseDesc;
                    dtTBClaimVM.ExpenseCategory = ExpenseCat;
                    dtTBClaimVM.AccountCode = AccountCode;
                    dtTBClaimVM.Amount = amount;
                    dtTBClaimVM.Gst = gst;
                    //dtTBClaimVM.AmountWithGST = sumamount;
                    tBClaimDetailVM.DtTBClaimVMSummary.Add(dtTBClaimVM);
                }

                tBClaimDetailVM.TBClaimAudits = new List<TBClaimAuditVM>();

                //var dtTBClaimAudits = await _repository.MstTBClaimAudit.GetMstTBClaimAuditByIdAsync(id);

                //foreach (var item in dtTBClaimAudits)
                //{
                //    TBClaimAuditVM mstTBClaimAuditVM = new TBClaimAuditVM();
                //    mstTBClaimAuditVM.Action = item.Action;
                //    mstTBClaimAuditVM.Description = item.Description;
                //    mstTBClaimAuditVM.AuditDateTickle = Helper.RelativeDate(item.AuditDate);
                //    tBClaimDetailVM.TBClaimAudits.Add(mstTBClaimAuditVM);
                //}

                tBClaimDetailVM.TBClaimFileUploads = new List<DtTBClaimFileUpload>();

                var tbFileUploads = _repository.DtTBClaimFileUploadDraft.GetDtTBClaimDraftAuditByIdAsync(id).Result.ToList();

                foreach (var item in tbFileUploads)
                {
                    MstTBClaim mstTBClaim1 = new MstTBClaim();
                    if (item.MstTBClaimDraft != null)
                    {
                        mstTBClaim1 = new MstTBClaim()
                        {
                            ApprovalBy = item.MstTBClaimDraft.ApprovalBy,
                            ApprovalDate = item.MstTBClaimDraft.ApprovalDate,
                            ApprovalStatus = item.MstTBClaimDraft.ApprovalStatus,
                            ModifiedDate = item.MstTBClaimDraft.ModifiedDate,
                            ModifiedBy = item.MstTBClaimDraft.ModifiedBy,
                            Approver = item.MstTBClaimDraft.Approver,
                            Company = item.MstTBClaimDraft.Company,
                            CreatedBy = item.MstTBClaimDraft.CreatedBy,
                            CreatedDate = item.MstTBClaimDraft.CreatedDate,
                            DepartmentID = item.MstTBClaimDraft.DepartmentID,
                            TBCID = item.MstTBClaimDraft.TBCID,
                            TBCNo = item.MstTBClaimDraft.TBCNo,
                            FacilityID = item.MstTBClaimDraft.FacilityID,
                            FinalApprover = item.MstTBClaimDraft.FinalApprover,
                            GrandTotal = item.MstTBClaimDraft.GrandTotal,
                            HODApprover = item.MstTBClaimDraft.HODApprover,
                            MstDepartment = item.MstTBClaimDraft.MstDepartment,
                            MstFacility = item.MstTBClaimDraft.MstFacility,
                            MstUser = item.MstTBClaimDraft.MstUser,
                            TnC = item.MstTBClaimDraft.TnC,
                            Month = item.MstTBClaimDraft.Month,
                            Year = item.MstTBClaimDraft.Year,
                            UserApprovers = item.MstTBClaimDraft.UserApprovers,
                            UserID = item.MstTBClaimDraft.UserID,
                            Verifier = item.MstTBClaimDraft.Verifier,
                            VoidReason = item.MstTBClaimDraft.VoidReason
                        };
                    }

                    tBClaimDetailVM.TBClaimFileUploads.Add(new DtTBClaimFileUpload()
                    {
                        CreatedBy = item.CreatedBy,
                        CreatedDate = item.CreatedDate,
                        TBCID = item.TBCID,
                        FileID = item.FileID,
                        FileName = item.FileName,
                        FilePath = item.FilePath,
                        IsDeleted = item.IsDeleted,
                        ModifiedBy = item.ModifiedBy,
                        ModifiedDate = item.ModifiedDate,
                        MstTBClaim = mstTBClaim1
                    });
                }

                TBClaimVM tBClaimVM = new TBClaimVM();
                tBClaimVM.Month = mstTBClaim.Month;
                tBClaimVM.Year = mstTBClaim.Year;
                tBClaimVM.GrandTotal = mstTBClaim.GrandTotal;
                tBClaimVM.Company = mstTBClaim.Company;
                tBClaimVM.Name = mstTBClaim.MstUser.Name;
                tBClaimVM.DepartmentName = mstTBClaim.MstDepartment.Department;
                tBClaimVM.FacilityName = mstTBClaim.MstFacility.FacilityName;
                tBClaimVM.CreatedDate = mstTBClaim.CreatedDate.ToString("d");
                tBClaimVM.Verifier = mstTBClaim.Verifier;
                tBClaimVM.Approver = mstTBClaim.Approver;
                ViewBag.TBCID = id;
                tBClaimVM.TBCNo = mstTBClaim.TBCNo;
                TempData["CreatedBy"] = mstTBClaim.CreatedBy;
                ViewBag.Approvalstatus = mstTBClaim.ApprovalStatus;

                TempData["ApprovedStatus"] = mstTBClaim.ApprovalStatus;
                TempData["FinalApproverID"] = mstTBClaim.FinalApprover;
                ViewBag.VoidReason = mstTBClaim.VoidReason == null ? "" : mstTBClaim.VoidReason;
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
                if (mstTBClaim.Verifier != "")
                {
                    string[] verifierIDs = mstTBClaim.Verifier.Split(',');
                    TempData["QueryMCVerifierIDs"] = string.Join(",", verifierIDs);
                    foreach (string verifierID in verifierIDs)
                    {
                        if (verifierID != "" && verifierID == (HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value) && User.IsInRole("Finance"))
                        {
                            TempData["ApprovedStatus"] = mstTBClaim.ApprovalStatus;
                            //Session["ApprovedStatus"] = oSupplierPO.Approvalstatus;
                            TempData["VerifierIDs"] = string.Join(",", verifierIDs.Skip(1));
                        }
                        else
                        {
                            TempData["ApprovedStatus"] = "";
                            //Session["ApprovedStatus"] = oSupplierPO.Approvalstatus;
                            TempData["VerifierIDs"] = mstTBClaim.Verifier;
                        }
                        TempData["ApproverIDs"] = mstTBClaim.Approver;
                        break;
                    }
                }
                else
                {
                    TempData["VerifierIDs"] = mstTBClaim.Verifier;
                    TempData["ApproverIDs"] = mstTBClaim.Approver;
                }

                //Approval Process code
                if (mstTBClaim.Approver != "" && mstTBClaim.Verifier == "")
                {
                    string[] approverIDs = mstTBClaim.Approver.Split(',');
                    TempData["QueryMCApproverIDs"] = string.Join(",", approverIDs);
                    foreach (string approverID in approverIDs)
                    {
                        if (approverID != "" && approverID == (HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value) && User.IsInRole("Finance"))
                        {
                            TempData["ApprovedStatus"] = mstTBClaim.ApprovalStatus;
                            //Session["ApprovedStatus"] = oSupplierPO.Approvalstatus;
                            TempData["ApproverIDs"] = string.Join(",", approverIDs.Skip(1));
                        }
                        else
                        {
                            TempData["ApprovedStatus"] = "";
                            //Session["ApprovedStatus"] = oSupplierPO.Approvalstatus;
                            TempData["ApproverIDs"] = mstTBClaim.Approver;
                        }
                        break;
                    }
                }
                else
                {
                    string[] approverIDs = mstTBClaim.Approver.Split(',');
                    TempData["QueryMCApproverIDs"] = string.Join(",", approverIDs);
                }

                if (mstTBClaim.UserApprovers != "" && mstTBClaim.Verifier == "")
                {
                    string[] userApproverIDs = mstTBClaim.UserApprovers.Split(',');
                    TempData["QueryMCUserApproverIDs"] = string.Join(",", userApproverIDs);
                    foreach (string approverID in userApproverIDs)
                    {
                        if (approverID != "" && approverID == (HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value))
                        {
                            TempData["ApprovedStatus"] = mstTBClaim.ApprovalStatus;
                            //Session["ApprovedStatus"] = oSupplierPO.Approvalstatus;
                            TempData["HODApproverIDs"] = string.Join(",", userApproverIDs.Skip(1));
                        }
                        else
                        {
                            TempData["ApprovedStatus"] = "";
                            //Session["ApprovedStatus"] = oSupplierPO.Approvalstatus;
                            TempData["UserApproverIDs"] = mstTBClaim.UserApprovers;
                        }
                        break;
                    }
                }
                else
                {
                    string[] userApproverIDs = mstTBClaim.UserApprovers.Split(',');
                    TempData["QueryMCUserApproverIDs"] = string.Join(",", userApproverIDs);
                }

                if (mstTBClaim.HODApprover != "" && mstTBClaim.Verifier == "")
                {
                    string[] hodApproverIDs = mstTBClaim.HODApprover.Split(',');
                    TempData["QueryMCHODApproverIDs"] = string.Join(",", hodApproverIDs);
                    foreach (string approverID in hodApproverIDs)
                    {
                        if (approverID != "" && approverID == (HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value))
                        {
                            TempData["ApprovedStatus"] = mstTBClaim.ApprovalStatus;
                            //Session["ApprovedStatus"] = oSupplierPO.Approvalstatus;
                            TempData["HODApproverIDs"] = string.Join(",", hodApproverIDs.Skip(1));
                        }
                        else
                        {
                            TempData["ApprovedStatus"] = "";
                            //Session["ApprovedStatus"] = oSupplierPO.Approvalstatus;
                            TempData["HODApproverIDs"] = mstTBClaim.HODApprover;
                        }
                        break;
                    }
                }
                else
                {
                    string[] hodApproverIDs = mstTBClaim.HODApprover.Split(',');
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
                //var mstMileageClaimAudits = await _repository.MstMileageClaimAudit.GetMstMileageClaimAuditByIdAsync(TBCID);
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

                tBClaimDetailVM.TBClaimVM = tBClaimVM;
                //mileageClaimDetailVM.DtMileageClaimVMs = dtMileageClaimVMs;
                return View("Details", tBClaimDetailVM);
            }
            else
            {
                return Redirect("~/Login/Login");
            }
        }

        public async Task<IActionResult> DeleteTbDraft(string id)
        {
            try
            {
                long idd = Convert.ToInt64(id);
                var tbCliamDrafts = await _repository.MstTBClaimDraft.GetTBClaimDraftByIdAsync(idd);
                _repository.MstTBClaimDraft.DeleteTBClaimDraft(tbCliamDrafts);
                await _repository.SaveAsync();
                TempData["Message"] = "Draft deleted successfully";
                Content("<script language='javascript' type='text/javascript'>alert('Draft deleted successfully');</script>");
                return RedirectToAction("Index", "TelephoneBillClaim");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Something went wrong inside DeleteTbDraft action: {ex.Message}");
            }
            return Json(null);
        }

        public async Task<IActionResult> GetPrintClaimDetails(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            long TBCID = Convert.ToInt64(id);
            TBClaimDetailVM tBClaimDetailVM = new TBClaimDetailVM();
            if (User != null && User.Identity.IsAuthenticated)
            {
                var mstTBClaim = await _repository.MstTBClaim.GetTBClaimByIdAsync(id);

                if (mstTBClaim == null)
                {
                    return NotFound();
                }

                var dtTBSummaries = await _repository.DtTBClaimSummary.GetDtTBClaimSummaryByIdAsync(id);
                var dtTBClaims = await _repository.DtTBClaim.GetDtTBClaimByIdAsync(id);

                //List<DtMileageClaimVM> dtMileageClaimVMs = new List<DtMileageClaimVM>();
                tBClaimDetailVM.DtTBClaimVMs = new List<DtTBClaimVM>();
                // List<clsDtSupplierPO> oclsDtSupplierPO = new List<clsDtSupplierPO>();
                foreach (var item in dtTBClaims)
                {
                    DtTBClaimVM dtTBClaimVM = new DtTBClaimVM();

                    dtTBClaimVM.TBCItemID = item.TBCItemID;
                    dtTBClaimVM.TBCID = item.TBCID;
                    dtTBClaimVM.DateOfJourney = item.Date;

                    dtTBClaimVM.Description = item.Description;
                    dtTBClaimVM.Amount = item.Amount;
                    dtTBClaimVM.ExpenseCategory = item.MstExpenseCategory.Description;
                    dtTBClaimVM.AccountCode = item.AccountCode;

                    if (item.FacilityID != null)
                    {
                        var mstFacility = await _repository.MstFacility.GetFacilityByIdAsync(item.FacilityID);
                        dtTBClaimVM.Facility = mstFacility.FacilityName;
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

                    tBClaimDetailVM.DtTBClaimVMs.Add(dtTBClaimVM);
                }
                tBClaimDetailVM.DtTBClaimSummaries = dtTBSummaries;
                tBClaimDetailVM.TBClaimAudits = new List<TBClaimAuditVM>();

                var dtTBClaimAudits = await _repository.MstTBClaimAudit.GetMstTBClaimAuditByIdAsync(id);

                foreach (var item in dtTBClaimAudits)
                {
                    TBClaimAuditVM mstTBClaimAuditVM = new TBClaimAuditVM();
                    mstTBClaimAuditVM.Action = item.Action;
                    mstTBClaimAuditVM.Description = item.Description;
                    mstTBClaimAuditVM.AuditDateTickle = Helper.RelativeDate(item.AuditDate);
                    tBClaimDetailVM.TBClaimAudits.Add(mstTBClaimAuditVM);
                }

                tBClaimDetailVM.TBClaimFileUploads = new List<DtTBClaimFileUpload>();

                tBClaimDetailVM.TBClaimFileUploads = _repository.DtTBClaimFileUpload.GetDtTBClaimAuditByIdAsync(id).Result.ToList();

                TBClaimVM tBClaimVM = new TBClaimVM();
                tBClaimVM.Month = mstTBClaim.Month;
                tBClaimVM.Year = mstTBClaim.Year;
                tBClaimVM.GrandTotal = mstTBClaim.GrandTotal;
                tBClaimVM.Company = mstTBClaim.Company;
                tBClaimVM.Name = mstTBClaim.MstUser.Name;
                tBClaimVM.DepartmentName = mstTBClaim.MstDepartment.Department;
                tBClaimVM.FacilityName = mstTBClaim.MstFacility.FacilityName;
                tBClaimVM.CreatedDate = mstTBClaim.CreatedDate.ToString("d");
                tBClaimVM.Verifier = mstTBClaim.Verifier;
                tBClaimVM.Approver = mstTBClaim.Approver;
                tBClaimVM.VoucherNo = mstTBClaim.VoucherNo;
                ViewBag.TBCID = id;
                tBClaimVM.TBCNo = mstTBClaim.TBCNo;
                tBClaimDetailVM.TBClaimVM = tBClaimVM;
            }
            return PartialView("GetTBDetailsPrint", tBClaimDetailVM);
        }
        public async Task<JsonResult> UpdateStatusforVoid(string id, string reason, string approvedStatus)
        {
            if (User != null && User.Identity.IsAuthenticated)
            {
                int TBCID = Convert.ToInt32(id);

                var mstTBClaim = await _repository.MstTBClaim.GetTBClaimByIdAsync(TBCID);

                if (mstTBClaim == null)
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
                    await _repository.MstTBClaim.UpdateMstTBClaimStatus(TBCID, -5, int.Parse(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value), DateTime.Now, reason, string.Empty, string.Empty, string.Empty, string.Empty, isAlternateApprover, 0);
                }
                else
                {
                    await _repository.MstTBClaim.UpdateMstTBClaimStatus(TBCID, 5, int.Parse(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value), DateTime.Now, reason, string.Empty, string.Empty, string.Empty, string.Empty, isAlternateApprover, 0);
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
                int TBCID = Convert.ToInt32(id);

                var mstTBClaim = await _repository.MstTBClaim.GetTBClaimByIdAsync(TBCID);

                if (mstTBClaim == null)
                {
                    // return NotFound();
                }


                int ApprovedStatus = Convert.ToInt32(mstTBClaim.ApprovalStatus);
                bool excute = _repository.MstTBClaim.ExistsApproval(TBCID.ToString(), ApprovedStatus, HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value, "TelephoneBill");
                // If execute is false, Check if the current user is alternate user for this claim
                if (excute == false)
                {
                    string usapprover = _repository.MstTBClaim.GetApproverVerifier(TBCID.ToString(), ApprovedStatus, HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value, "TelephoneBill");
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
                    #region TB Verifier
                    if (ApprovedStatus == 1)
                    {
                        string VerifierIDs = "";
                        string ApproverIDs = "";
                        string UserApproverIDs = "";
                        string HODApproverID = "";
                        try
                        {
                            string[] TBverifierIDs = mstTBClaim.Verifier.Split(',');
                            VerifierIDs = string.Join(",", TBverifierIDs.Skip(1));
                            string[] verifierIDs = VerifierIDs.ToString().Split(',');
                            ApproverIDs = mstTBClaim.Approver;
                            HODApproverID = mstTBClaim.HODApprover;
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
                        await _repository.MstTBClaim.UpdateMstTBClaimStatus(TBCID, 7, int.Parse(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value), DateTime.Now, string.Empty, VerifierIDs.ToString(), ApproverIDs.ToString(), UserApproverIDs.ToString(), HODApproverID.ToString(), isAlternateApprover, 0);

                    }
                    #endregion

                    #region TB Approver
                    else if (ApprovedStatus == 2)
                    {
                        string VerifierIDs = "";
                        string ApproverIDs = "";
                        string UserApproverIDs = "";
                        string HODApproverID = "";
                        try
                        {
                            string[] TBapproverIDs = mstTBClaim.Approver.Split(',');
                            ApproverIDs = string.Join(",", TBapproverIDs.Skip(1));
                            string[] approverIDs = ApproverIDs.Split(',');
                            int CreatedBy = Convert.ToInt32(mstTBClaim.CreatedBy);
                            HODApproverID = mstTBClaim.HODApprover;


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
                        await _repository.MstTBClaim.UpdateMstTBClaimStatus(TBCID, 3, int.Parse(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value), DateTime.Now, string.Empty, VerifierIDs, ApproverIDs, UserApproverIDs.ToString(), HODApproverID.ToString(), isAlternateApprover, int.Parse(financeStartDay));
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
                int TBCID = Convert.ToInt32(id);

                var mstTBClaim = await _repository.MstTBClaim.GetTBClaimByIdAsync(TBCID);

                if (mstTBClaim == null)
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

                await _repository.MstTBClaim.UpdateMstTBClaimStatus(TBCID, 4, int.Parse(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value), DateTime.Now, reason, string.Empty, string.Empty, string.Empty, string.Empty, isAlternateApprover, 0);

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
                    CloudBlob file = container.GetBlobReference("FileUploads/TBClaimFiles/" + id);

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

        public async Task<ActionResult> DeleteTBClaimFile(string fileID, string filepath, string TBCID)
        {
            DtTBClaimFileUpload dtTBClaimFileUpload = new DtTBClaimFileUpload();
            if (CloudStorageAccount.TryParse(_configuration.GetSection("ConnectionStrings")["BlobConnectionString"], out CloudStorageAccount storageAccount))
            {
                CloudBlobClient BlobClient = storageAccount.CreateCloudBlobClient();
                CloudBlobContainer container = BlobClient.GetContainerReference(_configuration.GetSection("ConnectionStrings")["BlobContainerName"]);

                if (await container.ExistsAsync())
                {
                    CloudBlob file = container.GetBlobReference("FileUploads/TBClaimFiles/" + filepath);

                    if (await file.ExistsAsync())
                    {
                        await file.DeleteIfExistsAsync();
                        dtTBClaimFileUpload = await _repository.DtTBClaimFileUpload.GetDtTBClaimFileUploadByIdAsync(Convert.ToInt64(fileID));
                        _repository.DtTBClaimFileUpload.DeleteDtTBClaimFileUpload(dtTBClaimFileUpload);
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
            
            return RedirectToAction("Create", "TelephoneBillClaim", new
            {
                id = TBCID,
                Updatestatus = "Edit"
            });
        }

        public async Task<ActionResult> DeleteTBClaimDraftFile(string fileID, string filepath, string TBCID)
        {
            DtTBClaimFileUploadDraft dtTBClaimFileUpload = new DtTBClaimFileUploadDraft();
            if (CloudStorageAccount.TryParse(_configuration.GetSection("ConnectionStrings")["BlobConnectionString"], out CloudStorageAccount storageAccount))
            {
                CloudBlobClient BlobClient = storageAccount.CreateCloudBlobClient();
                CloudBlobContainer container = BlobClient.GetContainerReference(_configuration.GetSection("ConnectionStrings")["BlobContainerName"]);

                if (await container.ExistsAsync())
                {
                    CloudBlob file = container.GetBlobReference("FileUploads/TBClaimFiles/" + filepath);

                    if (await file.ExistsAsync())
                    {
                        await file.DeleteIfExistsAsync();
                        dtTBClaimFileUpload = await _repository.DtTBClaimFileUploadDraft.GetDtTBClaimFileUploadDraftByIdAsync(Convert.ToInt64(fileID));
                        _repository.DtTBClaimFileUploadDraft.DeleteDtTBClaimFileUploadDraft(dtTBClaimFileUpload);
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
 
            return RedirectToAction("CreateDraft", "TelephoneBillClaim", new
            {
                id = TBCID,
                Updatestatus = "Edit"
            });
        }

        public async Task<JsonResult> GetTextValuesSG(string id)
        {
            List<DtTBClaimVM> oDtClaimsList = new List<DtTBClaimVM>();

            try
            {
                var dtTBClaims = await _repository.DtTBClaim.GetDtTBClaimByIdAsync(Convert.ToInt64(id));

                // List<clsDtSupplierPO> oclsDtSupplierPO = new List<clsDtSupplierPO>();
                foreach (var item in dtTBClaims)
                {
                    DtTBClaimVM dtTBClaimVM = new DtTBClaimVM();

                    dtTBClaimVM.TBCItemID = item.TBCItemID;
                    dtTBClaimVM.TBCID = item.TBCID;
                    dtTBClaimVM.DateOfJourney = item.Date;
                    dtTBClaimVM.FacilityID = item.FacilityID;
                    dtTBClaimVM.Description = item.Description;
                    dtTBClaimVM.Amount = item.Amount;
                    //dtTBClaimVM.ExpenseCategoryID
                    //dtTBClaimVM.Gst = item.GST;
                    //dtTBClaimVM.AmountWithGST = item.Amount + item.GST;
                    dtTBClaimVM.ExpenseCategoryID = item.ExpenseCategoryID;
                    dtTBClaimVM.AccountCode = item.AccountCode;
                    oDtClaimsList.Add(dtTBClaimVM);
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
            List<DtTBClaimVM> oDtClaimsList = new List<DtTBClaimVM>();

            try
            {
                var dtTBClaims = await _repository.DtTBClaimDraft.GetDtTBClaimDraftByIdAsync(Convert.ToInt64(id));

                foreach (var item in dtTBClaims)
                {
                    DtTBClaimVM dtTBClaimVM = new DtTBClaimVM();
                    dtTBClaimVM.TBCItemID = item.TBCItemID;
                    dtTBClaimVM.TBCID = item.TBCID;
                    dtTBClaimVM.DateOfJourney = item.Date;
                    dtTBClaimVM.FacilityID = item.FacilityID;
                    dtTBClaimVM.Description = item.Description;
                    dtTBClaimVM.Amount = item.Amount;
                    dtTBClaimVM.ExpenseCategoryID = item.ExpenseCategoryID;
                    dtTBClaimVM.AccountCode = item.AccountCode;
                    oDtClaimsList.Add(dtTBClaimVM);
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
            //var tBClaimViewModel = JsonConvert.DeserializeObject<TBClaimViewModel>(data,
            //    new IsoDateTimeConverter { DateTimeFormat = "dd/MM/yyyy" });

            var tBClaimViewModel = JsonConvert.DeserializeObject<TBClaimViewModel>(data);
            var mstFacility = await _repository.MstFacility.GetFacilityWithDepartmentByIdAsync(Convert.ToInt32(HttpContext.User.FindFirst("delegatefacilityid") is null ? HttpContext.User.FindFirst("facilityid").Value : HttpContext.User.FindFirst("delegatefacilityid").Value));

            MstTBClaim mstTBClaim = new MstTBClaim();
            mstTBClaim.TBCNo = tBClaimViewModel.TBCNo;
            mstTBClaim.UserID = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
            mstTBClaim.Month = tBClaimViewModel.Month.Month;
            mstTBClaim.Year = tBClaimViewModel.Month.Year;
            mstTBClaim.Verifier = "";
            mstTBClaim.Approver = "";
            mstTBClaim.FinalApprover = "";
            mstTBClaim.ApprovalStatus = 1;
            mstTBClaim.GrandTotal = tBClaimViewModel.GrandTotal;
            mstTBClaim.Company = tBClaimViewModel.Company;
            mstTBClaim.FacilityID = Convert.ToInt32(HttpContext.User.FindFirst("delegatefacilityid") is null ? HttpContext.User.FindFirst("facilityid").Value : HttpContext.User.FindFirst("delegatefacilityid").Value);
            mstTBClaim.DepartmentID = mstFacility.MstDepartment.DepartmentID;
            mstTBClaim.CreatedDate = DateTime.Now;
            mstTBClaim.ModifiedDate = DateTime.Now;
            mstTBClaim.CreatedBy = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value); // Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
            mstTBClaim.ModifiedBy = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value); // Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
            mstTBClaim.ApprovalDate = DateTime.Now;
            mstTBClaim.ApprovalBy = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
            mstTBClaim.DelegatedBy = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? 0 : HttpContext.User.FindFirst("delegateuserid").Value);
            mstTBClaim.TnC = true;
            foreach (var dtItem in tBClaimViewModel.dtClaims)
            {
                var mstFacility1 = await _repository.MstFacility.GetFacilityWithDepartmentByIdAsync(Convert.ToInt32(dtItem.FacilityID));

                var mstExpenseCategory = await _repository.MstExpenseCategory.ExpenseCategoriesByClaimType("Telephone Bill");
                dtItem.MstExpenseCategory = mstExpenseCategory;
                //var mstExpenseCategory = await _repository.MstExpenseCategory.GetExpenseCategoryWithTypesByIdAsync(dtItem.ExpenseCategoryID);

                dtItem.AccountCode = mstExpenseCategory.ExpenseCode + "-" + mstFacility1.MstDepartment.Code + "-" + mstFacility1.Code + mstExpenseCategory.Default;
            }

            string ClaimStatus = "";
            long TBCID = 0;
            try
            {
                //CBRID = Convert.ToInt32(Session["CBRID"].ToString());
                TBCID = Convert.ToInt64(tBClaimViewModel.TBCID);
                if (TBCID == 0 || TempData["Updatestatus"].ToString() == "Recreate")
                {
                    ClaimStatus = "Recreate";
                    TBCID = 0;
                }
                else if (TBCID == 0)
                    ClaimStatus = "Add";
                else
                    ClaimStatus = "Update";

                if (tBClaimViewModel.ClaimAddCondition == "claimDraft")
                {
                    mstTBClaim.TBCID = 0;
                }
                else
                {
                    mstTBClaim.TBCID = TBCID;
                }
                //mstExpenseClaim.ECNo = tBClaimViewModel.;
            }
            catch { }

            TBClaimDetailVM tBClaimDetailVM = new TBClaimDetailVM();
            //List<DtMileageClaimVM> dtMileageClaimVMs = new List<DtMileageClaimVM>();
            tBClaimDetailVM.DtTBClaimVMs = new List<DtTBClaimVM>();
            // List<clsDtSupplierPO> oclsDtSupplierPO = new List<clsDtSupplierPO>();
            foreach (var item in tBClaimViewModel.dtClaims)
            {
                DtTBClaimVM dtTBClaimVM = new DtTBClaimVM();
                if (TBCID == 0 || TempData["Updatestatus"].ToString() == "Recreate")
                {
                    dtTBClaimVM.TBCItemID = 0;
                    dtTBClaimVM.TBCID = 0;
                }

                //dtTBClaimVM.Payee = item.Payee;
                //dtTBClaimVM.Particulars = item.Particulars;
                dtTBClaimVM.ExpenseCategory = item.MstExpenseCategory.Description;
                dtTBClaimVM.ExpenseCategoryID = item.MstExpenseCategory.ExpenseCategoryID;
                dtTBClaimVM.FacilityID = item.FacilityID;
                //dtTBClaimVM.Reason = item.Reason;
                //dtTBClaimVM.EmployeeNo = item.EmployeeNo;
                //dtTBClaimVM.ChequeNo = item.ChequeNo;
                dtTBClaimVM.Amount = item.Amount;
                dtTBClaimVM.Description = item.Description;
                //dtTBClaimVM.Gst = item.GST;
                //dtTBClaimVM.AmountWithGST = item.Amount + item.GST;
                //dtTBClaimVM.Facility = item.Facility;
                dtTBClaimVM.AccountCode = item.AccountCode;
                dtTBClaimVM.DateOfJourney = item.Date;
                tBClaimDetailVM.DtTBClaimVMs.Add(dtTBClaimVM);
            }

            var GroupByQS = tBClaimDetailVM.DtTBClaimVMs.GroupBy(s => s.AccountCode);

            tBClaimDetailVM.DtTBClaimVMSummary = new List<DtTBClaimVM>();

            foreach (var group in GroupByQS)
            {
                DtTBClaimVM dtTBClaimVM = new DtTBClaimVM();
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
                        ExpenseDesc = dtExpense.Description;
                    i++;
                    amount = amount + dtExpense.Amount;
                    //gst = gst + dtExpense.Gst;
                    //sumamount = sumamount + dtExpense.AmountWithGST;
                    ExpenseCat = dtExpense.ExpenseCategory;
                    facilityID = dtExpense.FacilityID;
                    if (dtExpense.FacilityID != null)
                    {
                        var mstFacility1 = await _repository.MstFacility.GetFacilityByIdAsync(dtExpense.FacilityID);
                        Facility = mstFacility1.FacilityName;
                    }
                    AccountCode = dtExpense.AccountCode;
                }
                //gst = gst / group.Count();
                dtTBClaimVM.Description = ExpenseDesc;
                dtTBClaimVM.ExpenseCategory = ExpenseCat;
                dtTBClaimVM.FacilityID = facilityID;
                dtTBClaimVM.Facility = Facility;
                dtTBClaimVM.AccountCode = AccountCode;
                dtTBClaimVM.Amount = amount;
                //dtTBClaimVM.Gst = gst;
                //dtTBClaimVM.AmountWithGST = sumamount;
                tBClaimDetailVM.DtTBClaimVMSummary.Add(dtTBClaimVM);
            }
            List<DtTBClaimSummary> lstTBClaimSummary = new List<DtTBClaimSummary>();
            foreach (var item in tBClaimDetailVM.DtTBClaimVMSummary)
            {
                DtTBClaimSummary dtTBClaimSummary1 = new DtTBClaimSummary();
                dtTBClaimSummary1.AccountCode = item.AccountCode;
                dtTBClaimSummary1.Amount = item.Amount;
                dtTBClaimSummary1.ExpenseCategory = item.ExpenseCategory;
                dtTBClaimSummary1.FacilityID = item.FacilityID;
                dtTBClaimSummary1.Facility = item.Facility;
                dtTBClaimSummary1.Description = item.Description.ToUpper();
                dtTBClaimSummary1.TaxClass = 4;
                //dtTBClaimSummary1.GST = item.Gst;
                //dtTBClaimSummary1.AmountWithGST = item.AmountWithGST;
                lstTBClaimSummary.Add(dtTBClaimSummary1);
            }

            DtTBClaimSummary dtTBClaimSummary = new DtTBClaimSummary();
            dtTBClaimSummary.AccountCode = "425000";
            dtTBClaimSummary.Amount = mstTBClaim.GrandTotal;
            dtTBClaimSummary.TaxClass = 0;
            //dtTBClaimSummary.GST = mstTBClaim.TotalAmount - mstTBClaim.GrandTotal;
            //dtTBClaimSummary.AmountWithGST = mstTBClaim.TotalAmount;
            dtTBClaimSummary.ExpenseCategory = "DBS";
            dtTBClaimSummary.Description = "";
            lstTBClaimSummary.Add(dtTBClaimSummary);

            var res = await _repository.MstTBClaim.SaveItems(mstTBClaim, tBClaimViewModel.dtClaims, lstTBClaimSummary);

            if (res != 0)
            {
                if (ClaimStatus == "Add" || ClaimStatus == "Recreate")
                {
                    mstTBClaim = await _repository.MstTBClaim.GetTBClaimByIdAsync(res);
                    if (mstTBClaim.ApprovalStatus == 1)
                    {
                        string VerifierIDs = "";
                        string ApproverIDs = "";
                        string UserApproverIDs = "";
                        string HODApproverID = "";
                        try
                        {
                            //VerifierIDs = mstTBClaim.Verifier.Split(',');
                            //VerifierIDs = string.Join(",", ExpenseverifierIDs.Skip(1));
                            string[] verifierIDs = mstTBClaim.Verifier.Split(',');
                            ApproverIDs = mstTBClaim.Approver;
                            HODApproverID = mstTBClaim.HODApprover;



                            //BackgroundJob.Enqueue(() => _sendMailServices.SendEmail());
                            //Mail Code Implementation for Verifiers

                            foreach (string verifierID in verifierIDs)
                            {
                                if (verifierID != "")
                                {
                                    string domainUrl = HttpContext.Request.Scheme + "://" + HttpContext.Request.Host;
                                    string clickUrl = domainUrl + "/" + "FinanceTBClaim/Details/" + mstTBClaim.TBCID;

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
                                    var claimNo = mstTBClaim.TBCNo;
                                    var screen = "Telephone Bill Claim";
                                    var approvalType = "Verification Request";
                                    int userID = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
                                    var subject = "Telephone Bill Claim for Verification " + claimNo;

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
                        string[] userApproverIDs = mstTBClaim.UserApprovers.ToString().Split(',');
                        foreach (string userApproverID in userApproverIDs)
                        {
                            if (userApproverID != "")
                            {
                                string domainUrl = HttpContext.Request.Scheme + "://" + HttpContext.Request.Host;
                                string clickUrl = domainUrl + "/" + "HodSummary/TBCDetails/" + mstTBClaim.TBCID;

                                var mstSenderDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value));
                                var senderName = mstSenderDetails.Name;
                                var mstVerifierDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(userApproverID));
                                var toEmail = mstVerifierDetails.EmailAddress;
                                var receiverName = mstVerifierDetails.Name;
                                var claimNo = mstTBClaim.TBCNo;
                                var screen = "Telephone Bill Claim";
                                var approvalType = "Approval Request";
                                int userID = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
                                var subject = "Telephone Bill Claim for Approval " + claimNo;

                                BackgroundJob.Enqueue(() => _sendMailServices.SendEmail("EmailTemplate.html", screen, subject, senderName, receiverName, claimNo, approvalType, userID, toEmail, clickUrl));
                            }
                            break;
                        }
                    }
                    TempData["Message"] = "Telephone Bill Claim added successfully";
                }
                else
                {
                    mstTBClaim = await _repository.MstTBClaim.GetTBClaimByIdAsync(res);
                    if (mstTBClaim.ApprovalStatus == 1)
                    {
                        string VerifierIDs = "";
                        string ApproverIDs = "";
                        string UserApproverIDs = "";
                        string HODApproverID = "";
                        try
                        {
                            //VerifierIDs = mstTBClaim.Verifier.Split(',');
                            //VerifierIDs = string.Join(",", ExpenseverifierIDs.Skip(1));
                            string[] verifierIDs = mstTBClaim.Verifier.Split(',');
                            ApproverIDs = mstTBClaim.Approver;
                            HODApproverID = mstTBClaim.HODApprover;



                            //BackgroundJob.Enqueue(() => _sendMailServices.SendEmail());
                            //Mail Code Implementation for Verifiers

                            foreach (string verifierID in verifierIDs)
                            {
                                if (verifierID != "")
                                {
                                    string domainUrl = HttpContext.Request.Scheme + "://" + HttpContext.Request.Host;
                                    string clickUrl = domainUrl + "/" + "FinanceTBClaim/Details/" + mstTBClaim.TBCID;

                                    var mstSenderDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value));
                                    var senderName = mstSenderDetails.Name;
                                    var mstVerifierDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(verifierID));
                                    var toEmail = mstVerifierDetails.EmailAddress;
                                    var receiverName = mstVerifierDetails.Name;
                                    var claimNo = mstTBClaim.TBCNo;
                                    var screen = "Telephone Bill Claim";
                                    var approvalType = "Verification Request";
                                    int userID = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
                                    var subject = "Telephone Bill Claim for Verification " + claimNo;

                                    BackgroundJob.Enqueue(() => _sendMailServices.SendEmail("EmailTemplate.html", screen, subject, senderName, receiverName, claimNo, approvalType, userID, toEmail, clickUrl));
                                }
                                break;
                            }
                        }
                        catch
                        {
                        }
                    }
                    else if (mstTBClaim.ApprovalStatus == 6)
                    {
                        string[] userApproverIDs = mstTBClaim.UserApprovers.ToString().Split(',');
                        foreach (string userApproverID in userApproverIDs)
                        {
                            if (userApproverID != "")
                            {
                                string domainUrl = HttpContext.Request.Scheme + "://" + HttpContext.Request.Host;
                                string clickUrl = domainUrl + "/" + "HodSummary/TBCDetails/" + mstTBClaim.TBCID;

                                var mstSenderDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value));
                                var senderName = mstSenderDetails.Name;
                                var mstVerifierDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(userApproverID));
                                var toEmail = mstVerifierDetails.EmailAddress;
                                var receiverName = mstVerifierDetails.Name;
                                var claimNo = mstTBClaim.TBCNo;
                                var screen = "Telephone Bill Claim";
                                var approvalType = "Approval Request";
                                int userID = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
                                var subject = "Telephone Bill Claim for Approval " + claimNo;

                                BackgroundJob.Enqueue(() => _sendMailServices.SendEmail("EmailTemplate.html", screen, subject, senderName, receiverName, claimNo, approvalType, userID, toEmail, clickUrl));
                            }
                            break;
                        }
                    }
                    else if (mstTBClaim.ApprovalStatus == 7)
                    {
                        string[] hODApproverIDs = mstTBClaim.HODApprover.ToString().Split(',');
                        foreach (string hODApproverID in hODApproverIDs)
                        {
                            if (hODApproverID != "")
                            {
                                string domainUrl = HttpContext.Request.Scheme + "://" + HttpContext.Request.Host;
                                string clickUrl = domainUrl + "/" + "HodSummary/TBCDetails/" + mstTBClaim.TBCID;

                                var mstSenderDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value));
                                var senderName = mstSenderDetails.Name;
                                var mstVerifierDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(hODApproverID));
                                var toEmail = mstVerifierDetails.EmailAddress;
                                var receiverName = mstVerifierDetails.Name;
                                var claimNo = mstTBClaim.TBCNo;
                                var screen = "Telephone Bill Claim";
                                var approvalType = "Approval Request";
                                int userID = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
                                var subject = "Telephone Bill Claim for Approval " + claimNo;

                                BackgroundJob.Enqueue(() => _sendMailServices.SendEmail("EmailTemplate.html", screen, subject, senderName, receiverName, claimNo, approvalType, userID, toEmail, clickUrl));
                            }
                            break;
                        }
                    }
                    else
                    {
                        string[] ExpenseapproverIDs = mstTBClaim.Approver.ToString().Split(',');
                        foreach (string approverID in ExpenseapproverIDs)
                        {
                            if (approverID != "")
                            {
                                string domainUrl = HttpContext.Request.Scheme + "://" + HttpContext.Request.Host;
                                string clickUrl = domainUrl + "/" + "FinanceTBClaim/Details/" + mstTBClaim.TBCID;

                                var mstSenderDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value));
                                var senderName = mstSenderDetails.Name;
                                var mstVerifierDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(approverID));
                                var toEmail = mstVerifierDetails.EmailAddress;
                                var receiverName = mstVerifierDetails.Name;
                                var claimNo = mstTBClaim.TBCNo;
                                var screen = "Telephone Bill Claim";
                                var approvalType = "Approval Request";
                                int userID = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
                                var subject = "Telephone Bill Claim for Approval " + claimNo;

                                BackgroundJob.Enqueue(() => _sendMailServices.SendEmail("EmailTemplate.html", screen, subject, senderName, receiverName, claimNo, approvalType, userID, toEmail, clickUrl));
                            }
                            break;
                        }
                    }
                    TempData["Message"] = "Telephone Bill Claim updated successfully";
                }

                return Json(new { res });
            }
            else
                return Json(new { res });
        }

        [HttpPost]
        public async Task<JsonResult> SaveDraftItems(string data)
        {
            //var tBClaimViewModel = JsonConvert.DeserializeObject<TBClaimDraftViewModel>(data,
            //    new IsoDateTimeConverter { DateTimeFormat = "dd/MM/yyyy" });

            var tBClaimViewModel = JsonConvert.DeserializeObject<TBClaimDraftViewModel>(data);
            var mstFacility = await _repository.MstFacility.GetFacilityWithDepartmentByIdAsync(Convert.ToInt32(HttpContext.User.FindFirst("delegatefacilityid") is null ? HttpContext.User.FindFirst("facilityid").Value : HttpContext.User.FindFirst("delegatefacilityid").Value));

            MstTBClaimDraft mstTBClaim = new MstTBClaimDraft();
            mstTBClaim.TBCNo = tBClaimViewModel.TBCNo;
            mstTBClaim.UserID = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
            mstTBClaim.Month = tBClaimViewModel.Month.Month;
            mstTBClaim.Year = tBClaimViewModel.Month.Year;
            mstTBClaim.Verifier = "";
            mstTBClaim.Approver = "";
            mstTBClaim.FinalApprover = "";
            mstTBClaim.ApprovalStatus = 1;
            mstTBClaim.GrandTotal = tBClaimViewModel.GrandTotal;
            mstTBClaim.Company = tBClaimViewModel.Company;
            mstTBClaim.FacilityID = Convert.ToInt32(HttpContext.User.FindFirst("delegatefacilityid") is null ? HttpContext.User.FindFirst("facilityid").Value : HttpContext.User.FindFirst("delegatefacilityid").Value);
            mstTBClaim.DepartmentID = mstFacility.MstDepartment.DepartmentID;
            mstTBClaim.CreatedDate = DateTime.Now;
            mstTBClaim.ModifiedDate = DateTime.Now;
            mstTBClaim.CreatedBy = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
            mstTBClaim.ModifiedBy = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
            mstTBClaim.ApprovalDate = DateTime.Now;
            mstTBClaim.ApprovalBy = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
            mstTBClaim.TnC = true;
            foreach (var dtItem in tBClaimViewModel.dtClaims)
            {
                var mstFacility1 = await _repository.MstFacility.GetFacilityWithDepartmentByIdAsync(Convert.ToInt32(dtItem.FacilityID));

                var mstExpenseCategory = await _repository.MstExpenseCategory.ExpenseCategoriesByClaimType("Telephone Bill");
                dtItem.MstExpenseCategory = mstExpenseCategory;
                //var mstExpenseCategory = await _repository.MstExpenseCategory.GetExpenseCategoryWithTypesByIdAsync(dtItem.ExpenseCategoryID);

                dtItem.AccountCode = mstExpenseCategory.ExpenseCode + "-" + mstFacility1.MstDepartment.Code + "-" + mstFacility1.Code + mstExpenseCategory.Default;
            }

            string ClaimStatus = "";
            long TBCID = 0;
            try
            {
                //CBRID = Convert.ToInt32(Session["CBRID"].ToString());
                TBCID = Convert.ToInt64(tBClaimViewModel.TBCID);
                if (TBCID == 0 || TempData["Updatestatus"].ToString() == "Recreate")
                {
                    ClaimStatus = "Recreate";
                    TBCID = 0;
                }
                else if (TBCID == 0)
                    ClaimStatus = "Add";
                else
                    ClaimStatus = "Update";
                mstTBClaim.TBCID = TBCID;
                //mstExpenseClaim.ECNo = tBClaimViewModel.;
            }
            catch { }

            TBClaimDetailVM tBClaimDetailVM = new TBClaimDetailVM();
            //List<DtMileageClaimVM> dtMileageClaimVMs = new List<DtMileageClaimVM>();
            tBClaimDetailVM.DtTBClaimVMs = new List<DtTBClaimVM>();
            // List<clsDtSupplierPO> oclsDtSupplierPO = new List<clsDtSupplierPO>();
            foreach (var item in tBClaimViewModel.dtClaims)
            {
                DtTBClaimVM dtTBClaimVM = new DtTBClaimVM();

                if (TBCID == 0 || TempData["Updatestatus"].ToString() == "Recreate")
                {
                    dtTBClaimVM.TBCItemID = 0;
                    dtTBClaimVM.TBCID = 0;
                }
                //dtTBClaimVM.Payee = item.Payee;
                //dtTBClaimVM.Particulars = item.Particulars;
                dtTBClaimVM.ExpenseCategory = item.MstExpenseCategory.Description;
                dtTBClaimVM.ExpenseCategoryID = item.MstExpenseCategory.ExpenseCategoryID;
                //dtTBClaimVM.Reason = item.Reason;
                //dtTBClaimVM.EmployeeNo = item.EmployeeNo;
                //dtTBClaimVM.ChequeNo = item.ChequeNo;
                dtTBClaimVM.Amount = item.Amount;
                dtTBClaimVM.Description = item.Description;
                //dtTBClaimVM.Gst = item.GST;
                //dtTBClaimVM.AmountWithGST = item.Amount + item.GST;
                //dtTBClaimVM.Facility = item.Facility;
                dtTBClaimVM.AccountCode = item.AccountCode;
                dtTBClaimVM.DateOfJourney = item.Date;
                dtTBClaimVM.OrderBy = item.OrderBy;
                tBClaimDetailVM.DtTBClaimVMs.Add(dtTBClaimVM);
            }

            var GroupByQS = tBClaimDetailVM.DtTBClaimVMs.GroupBy(s => s.AccountCode);

            tBClaimDetailVM.DtTBClaimVMSummary = new List<DtTBClaimVM>();

            foreach (var group in GroupByQS)
            {
                DtTBClaimVM dtTBClaimVM = new DtTBClaimVM();
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
                    //gst = gst + dtExpense.Gst;
                    //sumamount = sumamount + dtExpense.AmountWithGST;
                    ExpenseCat = dtExpense.ExpenseCategory;
                    AccountCode = dtExpense.AccountCode;
                }
                //gst = gst / group.Count();
                dtTBClaimVM.Description = ExpenseDesc;
                dtTBClaimVM.ExpenseCategory = ExpenseCat;
                dtTBClaimVM.AccountCode = AccountCode;
                dtTBClaimVM.Amount = amount;
                //dtTBClaimVM.Gst = gst;
                //dtTBClaimVM.AmountWithGST = sumamount;
                tBClaimDetailVM.DtTBClaimVMSummary.Add(dtTBClaimVM);
            }
            List<DtTBClaimSummaryDraft> lstTBClaimSummary = new List<DtTBClaimSummaryDraft>();
            foreach (var item in tBClaimDetailVM.DtTBClaimVMSummary)
            {
                DtTBClaimSummaryDraft dtTBClaimSummary1 = new DtTBClaimSummaryDraft();
                dtTBClaimSummary1.AccountCode = item.AccountCode;
                dtTBClaimSummary1.Amount = item.Amount;
                dtTBClaimSummary1.ExpenseCategory = item.ExpenseCategory;
                dtTBClaimSummary1.Description = item.Description.ToUpper();
                dtTBClaimSummary1.TaxClass = 4;
                //dtTBClaimSummary1.GST = item.Gst;
                //dtTBClaimSummary1.AmountWithGST = item.AmountWithGST;
                lstTBClaimSummary.Add(dtTBClaimSummary1);
            }

            DtTBClaimSummaryDraft dtTBClaimSummary = new DtTBClaimSummaryDraft();
            dtTBClaimSummary.AccountCode = "425000";
            dtTBClaimSummary.Amount = mstTBClaim.GrandTotal;
            dtTBClaimSummary.TaxClass = 0;
            //dtTBClaimSummary.GST = mstTBClaim.TotalAmount - mstTBClaim.GrandTotal;
            //dtTBClaimSummary.AmountWithGST = mstTBClaim.TotalAmount;
            dtTBClaimSummary.ExpenseCategory = "DBS";
            dtTBClaimSummary.Description = "";
            lstTBClaimSummary.Add(dtTBClaimSummary);

            var res = await _repository.MstTBClaimDraft.SaveDraftItems(mstTBClaim, tBClaimViewModel.dtClaims, lstTBClaimSummary);


            if (res != 0)
            {
                if (ClaimStatus == "Add" || ClaimStatus == "Recreate")
                    TempData["Message"] = "Telephone Bill Draft added successfully";
                else
                    TempData["Message"] = "Telephone Bill Draft updated successfully";

                return Json(new { res });
            }
            else
                return Json(new { res });
        }

        public async Task<JsonResult> UploadTBFiles(List<IFormFile> files)
        {
            var path = "FileUploads/TBClaimFiles/";
            //var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "FileUploads", "TBClaimFiles");

            //if (!Directory.Exists(path))
            //{
            //    Directory.CreateDirectory(path);
            //}

            // var id1 = Request.Form["Id"];
            //var id = Request.Form["Id"].ToString();

            string claimsCondition = Request.Form["claimAddCondition"];
            int TBCID = Convert.ToInt32(Request.Form["Id"]);
            int tbIDValue = Convert.ToInt32(Request.Form["tbIDValue"]);

            if (TBCID == 0)
            {
                if (TempData.ContainsKey("CID"))
                    TBCID = Convert.ToInt32(TempData["CID"].ToString());
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
                    string pathToFiles = Regex.Replace(result, @"[^0-9a-zA-Z]+", "_") + "-" + TBCID.ToString() + "-" + DateTime.Now.ToString("ddMMyyyyss") + ext;

                    DtTBClaimFileUpload dtTBClaimFileUpload = new DtTBClaimFileUpload();
                    dtTBClaimFileUpload.TBCID = TBCID;
                    dtTBClaimFileUpload.FileName = fileName;
                    dtTBClaimFileUpload.FilePath = pathToFiles;
                    dtTBClaimFileUpload.CreatedDate = DateTime.Now;
                    dtTBClaimFileUpload.ModifiedDate = DateTime.Now;
                    dtTBClaimFileUpload.CreatedBy = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
                    dtTBClaimFileUpload.ModifiedBy = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
                    dtTBClaimFileUpload.IsDeleted = false;
                    _repository.DtTBClaimFileUpload.Create(dtTBClaimFileUpload);
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
            long idd = Convert.ToInt64(tbIDValue);

            var dtFiles = await _repository.DtTBClaimFileUploadDraft.GetDtTBClaimDraftAuditByIdAsync(idd);
            if (dtFiles != null)
            {
                foreach (var dtFile in dtFiles)
                {
                    DtTBClaimFileUpload dtTBClaimFileUpload = new DtTBClaimFileUpload()
                    {
                        CreatedBy = dtFile.CreatedBy,
                        CreatedDate = dtFile.CreatedDate,
                        FileID = 0,
                        FileName = dtFile.FileName,
                        FilePath = dtFile.FilePath,
                        IsDeleted = dtFile.IsDeleted,
                        ModifiedBy = dtFile.ModifiedBy,
                        ModifiedDate = dtFile.ModifiedDate,
                        TBCID = TBCID
                    };
                    try
                    {
                        _repository.DtTBClaimFileUpload.Create(dtTBClaimFileUpload);
                        await _repository.SaveAsync();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Something went wrong inside DeleteTbDraft action: {ex.Message}");
                    }
                }
            }

            if (claimsCondition == "claimDraft")
            {
                try
                {
                    var tbCliamDrafts = await _repository.MstTBClaimDraft.GetTBClaimDraftByIdAsync(idd);
                    if (tbCliamDrafts != null)
                    {
                        _repository.MstTBClaimDraft.DeleteTBClaimDraft(tbCliamDrafts);
                        await _repository.SaveAsync();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Something went wrong inside DeleteTbDraft action: {ex.Message}");
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

        public async Task<JsonResult> UploadTBFilesDraft(List<IFormFile> files)
        {
            var path = "FileUploads/TBClaimFiles/";
            
            foreach (IFormFile formFile in files)
            {
                int TBCID = Convert.ToInt32(Request.Form["Id"]);
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
                    string pathToFiles = Regex.Replace(result, @"[^0-9a-zA-Z]+", "_") + "-" + TBCID.ToString() + "-" + DateTime.Now.ToString("ddMMyyyyss") + ext;

                    DtTBClaimFileUploadDraft dtTBClaimFileUpload = new DtTBClaimFileUploadDraft();
                    dtTBClaimFileUpload.TBCID = TBCID;
                    dtTBClaimFileUpload.FileName = fileName;
                    dtTBClaimFileUpload.FilePath = pathToFiles;
                    dtTBClaimFileUpload.CreatedDate = DateTime.Now;
                    dtTBClaimFileUpload.ModifiedDate = DateTime.Now;
                    dtTBClaimFileUpload.CreatedBy = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
                    dtTBClaimFileUpload.ModifiedBy = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
                    dtTBClaimFileUpload.IsDeleted = false;
                    _repository.DtTBClaimFileUploadDraft.Create(dtTBClaimFileUpload);
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
                    long TBCID = Convert.ToInt64(queryParamViewModel.Cid);
                    int UserID = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
                    // newly Added Code
                    var tbClaim = await _repository.MstTBClaim.GetTBClaimByIdAsync(TBCID);
                    for (int i = 0; i < UserIds.Length; i++)
                    {
                        MstQuery clsdtTBQuery = new MstQuery();
                        // if (data["MessageDescription"] != null)               
                        clsdtTBQuery.ModuleType = "TelephoneBill Claim";
                        //  clsdtSupplierQuery.ID = Convert.ToInt64(data["SPOID"]);
                        clsdtTBQuery.ID = TBCID;
                        clsdtTBQuery.SenderID = UserID;
                        //var recieverId = data["queryusers"];       
                        clsdtTBQuery.ReceiverID = Convert.ToInt32(UserIds[i]);
                        clsdtTBQuery.MessageDescription = queryParamViewModel.Message;
                        clsdtTBQuery.SentTime = DateTime.Now;
                        //clsdtExpenseQuery.NotificationStatus = false;
                        await _repository.MstQuery.CreateQuery(clsdtTBQuery);
                        //await _repository.SaveAsync();
                        //objERPEntities.AddToMstQueries(clsdtSupplierQuery);
                        //objERPEntities.SaveChanges();
                        result = "Success";

                        var receiver = await _repository.MstUser.GetUserByIdAsync(UserIds[i]);
                        //var reciever = objERPEntities.MstUsers.ToList().Where(p => p.UserID == Convert.ToInt32(UserIds[i]) && p.InstanceID == int.Parse(Session["InstanceID"].ToString())).ToList().FirstOrDefault();
                        MstTBClaimAudit auditUpdate = new MstTBClaimAudit();
                        auditUpdate.TBCID = TBCID;
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
                        await _repository.MstTBClaimAudit.CreateTBClaimAudit(auditUpdate);
                        await _repository.SaveAsync();

                        string domainUrl = HttpContext.Request.Scheme + "://" + HttpContext.Request.Host;
                        string clickUrl = string.Empty;

                        if (tbClaim.CreatedBy.ToString().Contains(UserIds[i].ToString()))
                            clickUrl = domainUrl + "/" + "TelephoneBillClaim/Details/" + TBCID;
                        else if (tbClaim.DApprover.Contains(UserIds[i].ToString()) || tbClaim.DVerifier.Contains(UserIds[i].ToString()))
                            clickUrl = domainUrl + "/" + "FinanceTBClaim/Details/" + TBCID;
                        else
                            clickUrl = domainUrl + "/" + "HodSummary/TBCDetails/" + TBCID;
                        //if (tbClaim.DUserApprovers.Contains(UserIds[i].ToString()) || tbClaim.DHODApprover.Contains(UserIds[i].ToString()))

                        //var mstSenderDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value));
                        var senderName = (string.IsNullOrEmpty(delegatedUserName) ? User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.GivenName).Value : delegatedUserName);
                        //var mstVerifierDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(approverID));
                        var toEmail = receiver.EmailAddress;
                        var receiverName = receiver.Name;
                        var claimNo = tbClaim.TBCNo;
                        var screen = "Telephone Bill Claim";
                        var approvalType = "Query";
                        int userID = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
                        var subject = "Telephone Bill Claim Query " + claimNo;
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
                var tbcid = Convert.ToInt32(id);
                int UserId = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid") is null ? HttpContext.User.FindFirst("userid").Value : HttpContext.User.FindFirst("delegateuserid").Value);
                ViewBag.userID = UserId;
                //var queries1 = _context.mstQuery.ToList().Where(j => j.ID == smcid && (j.SenderID == UserId || j.ReceiverID == UserId) && j.ModuleType.ToString().Trim() == "Expense Claim").OrderBy(j => j.SentTime);
                var queries = await _repository.MstQuery.GetAllClaimsQueriesAsync(UserId, tbcid, "TelephoneBill Claim");
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
