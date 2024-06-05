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
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace EClaimsWeb.Controllers
{
    [Authorize(Roles = "Admin")]
    public class FinanceReportsController : Controller
    {
        private ILoggerManager _logger;
        private IRepositoryWrapper _repository;
        private readonly IToastNotification _toastNotification;
        private IMapper _mapper;
        private readonly RepositoryContext _context;
        private IConfiguration _configuration;
        public FinanceReportsController(ILoggerManager logger, IRepositoryWrapper repository, IMapper mapper, RepositoryContext context,IConfiguration configuration)
        {
            _logger = logger;
            _repository = repository;
            _mapper = mapper;
            _context = context;
            _configuration = configuration;
        }
        public async Task<IActionResult> Index(string moduleName, int facilityID, int statusID, string fromDate, string toDate)
        {
            try
            {
                //if(ddlReportType == "1")
                //{

                //}
                //var mstFacilities = new SelectListItem( (await _repository.MstFacility.GetAllFacilityAsync("active"), "FacilityID", "FacilityName");
                if (string.IsNullOrEmpty(moduleName))
                {
                    moduleName = "ExpenseClaim";
                }

                if (statusID == 0)
                {
                    statusID = 3;
                }

                if (string.IsNullOrEmpty(fromDate) || string.IsNullOrEmpty(toDate))
                {
                    fromDate = DateTime.Now.AddDays(-60).ToString("dd/MM/yyyy");
                    toDate = DateTime.Now.ToString("dd/MM/yyyy");
                }

                List<clsModule> oclsStatus = new List<clsModule>();
                //oclsStatus.Add(new clsModule() { ModuleName = "Admin Settings", ModuleId = "Admin Settings" });
                oclsStatus.Add(new clsModule() { ModuleName = "Approved", ModuleId = "3" });
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
                oclsModule.Add(new clsModule() { ModuleName = "PV-Cheque Claim", ModuleId = "PV-ChequeClaim" });
                oclsModule.Add(new clsModule() { ModuleName = "PV-Giro Claim", ModuleId = "PV-GiroClaim" });
                oclsModule.Add(new clsModule() { ModuleName = "HRPV-Cheque Claim", ModuleId = "HRPV-ChequeClaim" });
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

                var mstMileageClaimsWithDetails = await _repository.MstMileageClaim.GetAllMileageClaimForAPExportAsync("", moduleName, facilityID,statusID, fromDate, toDate);
                foreach (var mc in mstMileageClaimsWithDetails)
                {
                    CustomClaim mileageClaimVM = new CustomClaim();
                    mileageClaimVM.CID = mc.CID;
                    mileageClaimVM.CNO = mc.CNO;
                    mileageClaimVM.Name = mc.Name;
                    mileageClaimVM.CreatedDate = Convert.ToDateTime(mc.CreatedDate).ToString("dd/MM/yyyy", System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
                    mileageClaimVM.ApprovalDate = Convert.ToDateTime(mc.ApprovalDate).ToString("dd/MM/yyyy", System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
                    mileageClaimVM.ExportAccPacDate = Convert.ToDateTime(mc.ExportAccPacDate).ToString("dd/MM/yyyy", System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
                    mileageClaimVM.ExportBankDate = Convert.ToDateTime(mc.ExportBankDate).ToString("dd/MM/yyyy", System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
                    mileageClaimVM.FacilityName = mc.FacilityName;
                    mileageClaimVM.Phone = mc.Phone;
                    mileageClaimVM.GrandTotal = mc.TotalAmount;
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
                /*
                //var mstMileageClaimsWithDetails = await _repository.MstMileageClaim.GetAllMileageClaimForAPExportAsync("", facilityID, fromDate, toDate);
                if (moduleName == "MileageClaim")
                {
                    var mstMileageClaimsWithDetails = await _repository.MstMileageClaim.GetAllMileageClaimForAPExportAsync("",moduleName, facilityID, fromDate, toDate);
                    foreach (var mc in mstMileageClaimsWithDetails)
                    {
                        CustomClaim mileageClaimVM = new CustomClaim();
                        mileageClaimVM.CID = mc.CID;
                        mileageClaimVM.CNO = mc.CNO;
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
                else if (moduleName == "ExpenseClaim")
                {
                    var mstExpenseClaimsWithDetails = await _repository.MstExpenseClaim.GetAllExpenseClaimForAPExportAsync("", facilityID, fromDate, toDate);
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
                    var mstTelephoneBillClaimsWithDetails = await _repository.MstTBClaim.GetAllTBClaimForAPExportAsync("", facilityID, fromDate, toDate);
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
                else if (moduleName == "PV-ChequeClaim")
                {
                    var mstPVCClaimsWithDetails = await _repository.MstPVCClaim.GetAllPVCClaimForAPExportAsync("", facilityID, fromDate, toDate);
                    foreach (var mc in mstPVCClaimsWithDetails)
                    {
                        CustomClaim mileageClaimVM = new CustomClaim();
                        mileageClaimVM.CID = mc.PVCCID;
                        mileageClaimVM.CNO = mc.PVCCNo;
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
                    var mstPVGClaimsWithDetails = await _repository.MstPVGClaim.GetAllPVGClaimForAPExportAsync("", facilityID, fromDate, toDate);
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
                else if (moduleName == "HRPV-ChequeClaim")
                {
                    var mstHRPVCClaimsWithDetails = await _repository.MstHRPVCClaim.GetAllHRPVCClaimWithDetailsByFacilityIDForAPExportAsync("",facilityID, fromDate, toDate);
                    foreach (var mc in mstHRPVCClaimsWithDetails)
                    {
                        CustomClaim mileageClaimVM = new CustomClaim();
                        mileageClaimVM.CID = mc.HRPVCCID;
                        mileageClaimVM.CNO = mc.HRPVCCNo;
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
                else if (moduleName == "HRPV-GiroClaim")
                {
                    var mstHRPVGClaimsWithDetails = await _repository.MstHRPVGClaim.GetAllHRPVGClaimWithDetailsByFacilityIDForAPExportAsync("", facilityID, fromDate, toDate);
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
                    ReportTypes = new SelectList( reports, "Value", "Text"),
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
            string filename = "APExport-POC-" + DateTime.Now.ToString("ddMMyyyyss")+".xlsx";
            string financeStartDay = _configuration.GetValue<string>("FinanceStartDay");

            var path = "FileUploads/temp/";
            string pathToFilesold = Path.Combine(path, filename);

            ListtoDataTableConverter converter = new ListtoDataTableConverter();
            List<APExport_Invoice> AllInvoices = new List<APExport_Invoice>();
            List<APExport_Invoice_Details> AllInvoiceDetails = new List<APExport_Invoice_Details>();
            List<APExport_Invoice_Payment_Schedules> AllInvoicesPayments = new List<APExport_Invoice_Payment_Schedules>();
            List<Invoice_Optional_Fields> AllInvoiceOptionalFields = new List<Invoice_Optional_Fields>();
            List<Invoice_Details_Optional_Fields> AllInvoiceDetailsOptionalFields = new List<Invoice_Details_Optional_Fields>();

            string Description = string.Empty;
            if (aPExportSearch.ModuleName == "MileageClaim")
            {
                var Po_Invoice = await _repository.MstMileageClaim.GetAllMileageClaimForAPExportAsync(aPExportSearch.ClaimIds, aPExportSearch.ModuleName, Int32.Parse(aPExportSearch.FacilityID), Int32.Parse(aPExportSearch.StatusID), aPExportSearch.FromDate, aPExportSearch.ToDate);
                int num1 = 0;

                #region Noraml AP export without Consolidated Invoices
                foreach (var item in Po_Invoice)
                {
                    num1 = num1 + 1;
                    //var id = await _repository.DtMileageClaim.GetDtMileageClaimByIdAsync(item.CID);

                    var summary = await _repository.DtMileageClaimSummary.GetDtMileageClaimSummaryByIdAsync(item.CID);
                    var TaxClass = summary.Where(s => s.TaxClass == 4).FirstOrDefault();
                    //var id = objERPEntities.DtSupplierPOCs.ToList().Where(p => p.POCID == Convert.ToInt32(item.POCID)).ToList();
                    //if (id.Count != 0)
                    //    Description = id.FirstOrDefault().desc item.SupplierShortName + "/" + objERPEntities.DtSupplierPOCs.ToList().Where(p => p.POCID == Convert.ToInt32(item.POCID)).ToList().FirstOrDefault().Description;
                    //else
                    //    Description = "";
                    int n = summary.Count;
                    if (summary.Count != 0)
                    {
                        if (summary.Count == n && summary.Count == 1)
                        {
                            Description = summary.FirstOrDefault().Description;
                            //Description = "Mileage Claim " + "from " + summary.FirstOrDefault().DateOfJourney.Day + " " + Helper.Month(summary.FirstOrDefault().DateOfJourney.Month) + " to " + id.FirstOrDefault().DateOfJourney.Day + " " + Helper.Month(id.FirstOrDefault().DateOfJourney.Month) + " " + id.FirstOrDefault().DateOfJourney.Year.ToString();
                        }
                        else
                        {
                            Description = summary.FirstOrDefault().Description;
                            //Description = "Mileage Claim " + "from " + id.FirstOrDefault().DateOfJourney.Day + " " + Helper.Month(id.FirstOrDefault().DateOfJourney.Month) + " to " + id.LastOrDefault().DateOfJourney.Day + " " + Helper.Month(id.LastOrDefault().DateOfJourney.Month) + " " + id.LastOrDefault().DateOfJourney.Year.ToString();
                        }
                    }

                    APExport_Invoice oAPExport_Invoice = new APExport_Invoice();
                    oAPExport_Invoice.CNTBTCH = "1";
                    oAPExport_Invoice.CNTITEM = num1.ToString();
                    oAPExport_Invoice.IDVEND = "DBS PV";
                    oAPExport_Invoice.IDINVC = item.VoucherNo;
                    oAPExport_Invoice.TEXTTRX = "1";
                    oAPExport_Invoice.ORDRNBR = "";
                    oAPExport_Invoice.PONBR = "";
                    oAPExport_Invoice.INVCDESC = Description;
                    oAPExport_Invoice.INVCAPPLTO = "";
                    oAPExport_Invoice.IDACCTSET = "1";
                    oAPExport_Invoice.DATEINVC = Convert.ToDateTime(DateTime.Now).ToString("dd/MM/yyyy HH:mm:ss", System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
                    oAPExport_Invoice.FISCYR = DateTime.Now.Year.ToString();
                    oAPExport_Invoice.FISCPER = DateTime.Now.Day >= Convert.ToInt32(financeStartDay) && DateTime.Now.Month != 12 ? DateTime.Now.AddMonths(1).Month.ToString() : DateTime.Now.Month.ToString();
                    oAPExport_Invoice.CODECURN = "SGD";
                    oAPExport_Invoice.TERMCODE = "1";
                    oAPExport_Invoice.DATEDUE = Convert.ToDateTime(DateTime.Now).ToString("dd/MM/yyyy HH:mm:ss", System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
                    oAPExport_Invoice.CODETAXGRP = "TXGSGD";

                    oAPExport_Invoice.TAXCLASS1 = (TaxClass != null ? (TaxClass.TaxClass == 4 ? "4" : TaxClass.TaxClass.ToString()) : "4");
                    oAPExport_Invoice.BASETAX1 = "0.00";
                    //if (item. == 0)
                    //{
                    //    oAPExport_Invoice.TAXCLASS1 = "4";
                    //    oAPExport_Invoice.BASETAX1 = "0.00";
                    //}
                    //else
                    //{

                    //    oAPExport_Invoice.TAXCLASS1 = Math.Round((decimal)item.TaxValue, (int)2).ToString("#,##0");
                    //    oAPExport_Invoice.BASETAX1 = Math.Round((decimal)item.AmountBeforeGST, (int)2).ToString("#0.00");
                    //}
                    //oAPExport_Invoice.AMTTAX1 = Math.Round((decimal)item.GSTAmount, (int)2).ToString("#0.00");
                    //oAPExport_Invoice.AMTTAXDIST = Math.Round((decimal)item.GSTAmount, (int)2).ToString("#0.00");
                    //oAPExport_Invoice.AMTINVCTOT = Math.Round((decimal)item.AmountBeforeGST, (int)2).ToString("#0.00");
                    //oAPExport_Invoice.AMTTOTDIST = Math.Round((decimal)item.AmountBeforeGST, (int)2).ToString("#0.00");
                    //oAPExport_Invoice.AMTGROSDST = Math.Round((decimal)item.TotalAmount, (int)2).ToString("#0.00");
                    //oAPExport_Invoice.AMTDUE = Math.Round((decimal)item.TotalAmount, (int)2).ToString("#0.00");
                    //oAPExport_Invoice.AMTTAXTOT = Math.Round((decimal)item.GSTAmount, (int)2).ToString("#0.00");
                    //oAPExport_Invoice.AMTGROSTOT = Math.Round((decimal)item.TotalAmount, (int)2).ToString("#0.00");

                    oAPExport_Invoice.AMTTAX1 = "0.00";
                    oAPExport_Invoice.AMTTAXDIST = "0.00";
                    oAPExport_Invoice.AMTINVCTOT = Math.Round((decimal)item.GrandTotal, (int)2).ToString("#0.00");
                    oAPExport_Invoice.AMTTOTDIST = Math.Round((decimal)item.GrandTotal, (int)2).ToString("#0.00");
                    oAPExport_Invoice.AMTGROSDST = Math.Round((decimal)item.GrandTotal, (int)2).ToString("#0.00");
                    oAPExport_Invoice.AMTDUE = Math.Round((decimal)item.GrandTotal, (int)2).ToString("#0.00");
                    oAPExport_Invoice.AMTTAXTOT = "0.00";
                    oAPExport_Invoice.AMTGROSTOT = Math.Round((decimal)item.GrandTotal, (int)2).ToString("#0.00");

                    AllInvoices.Add(oAPExport_Invoice);


                    APExport_Invoice_Payment_Schedules oAPExport_Invoice_Payment_Schedules = new APExport_Invoice_Payment_Schedules();
                    oAPExport_Invoice_Payment_Schedules.CNTBTCH = "1";
                    oAPExport_Invoice_Payment_Schedules.CNTITEM = num1.ToString();
                    oAPExport_Invoice_Payment_Schedules.CNTPAYM = "";
                    oAPExport_Invoice_Payment_Schedules.DATEDUE = Convert.ToDateTime(DateTime.Now).ToString("dd/MM/yyyy HH:mm:ss", System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
                    oAPExport_Invoice_Payment_Schedules.AMTDUE = Math.Round((decimal)item.GrandTotal, (int)2).ToString("#0.00");
                    AllInvoicesPayments.Add(oAPExport_Invoice_Payment_Schedules);

                    //below code is to change the status to Exported
                    if (aPExportSearch.StatusID == "3")
                        await _repository.MstMileageClaim.UpdateMstMileageClaimStatus(item.CID, 9, int.Parse(HttpContext.User.FindFirst("userid").Value), DateTime.Now, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, false, 0);
                    //var oSupplierPOC = objERPEntities.MstSupplierPOCs.ToList().Where(p => p.POCID == item.POCID).FirstOrDefault();
                    //if (oSupplierPOC != null)
                    //{
                    //    oSupplierPOC.ApprovalStatus = 5;
                    //    objERPEntities.SaveChanges();
                    //}
                    ////


                    //var Supplier_Invoice_Details = await _repository.DtMileageClaim.GetDtMileageClaimByIdAsync(item.CID);
                    int num = 0;
                    int num2 = 0;
                    try
                    {
                        foreach (var item1 in summary)
                        {
                            if (item1.ExpenseCategory != "DBS")
                            {
                                num = num + 20;
                                num2 = num2 + 1;
                                //if (item.POCNO.Contains("RH") || item.POCNO.Contains("RL"))
                                //{
                                //    num2 = num2 + 21;
                                //}
                                APExport_Invoice_Details oAPExport_Invoice_Details = new APExport_Invoice_Details();
                                oAPExport_Invoice_Details.CNTBTCH = "1";
                                oAPExport_Invoice_Details.CNTITEM = num1.ToString();
                                oAPExport_Invoice_Details.CNTLINE = num.ToString();
                                oAPExport_Invoice_Details.IDDIST = "";
                                oAPExport_Invoice_Details.TEXTDESC = item1.Description;
                                //oAPExport_Invoice_Details.AMTTOTTAX = Math.Round((((decimal)item.AmountBeforeGST) * ((decimal)item.TaxValue)) / 100, (int)2).ToString("#0.00");
                                //oAPExport_Invoice_Details.AMTTOTTAX = Math.Round((decimal)item.AmountBeforeGST, (int)2).ToString("#0.00");

                                oAPExport_Invoice_Details.TAXCLASS1 = item1.TaxClass.ToString();
                                //oAPExport_Invoice_Details.RATETAX1 = "4";
                                oAPExport_Invoice_Details.RATETAX1 = "0";
                                oAPExport_Invoice_Details.BASETAX1 = "0.00";
                                oAPExport_Invoice_Details.AMTTOTTAX = "0";

                                //if (item.TaxValue == 0)
                                //{
                                //    oAPExport_Invoice_Details.TAXCLASS1 = "4";
                                //    //oAPExport_Invoice_Details.RATETAX1 = "4";
                                //    oAPExport_Invoice_Details.RATETAX1 = "0";
                                //    oAPExport_Invoice_Details.BASETAX1 = "0.00";
                                //    oAPExport_Invoice_Details.AMTTOTTAX = Math.Round((decimal)item.AmountBeforeGST, (int)2).ToString("#0.00");
                                //}
                                //else
                                //{

                                //    oAPExport_Invoice_Details.BASETAX1 = Math.Round((decimal)item.AmountBeforeGST, (int)2).ToString("#0.00");
                                //    oAPExport_Invoice_Details.TAXCLASS1 = Math.Round((decimal)item.TaxValue, (int)2).ToString("#,##0");
                                //    oAPExport_Invoice_Details.RATETAX1 = Math.Round((decimal)item.TaxValue, (int)2).ToString("#,##0");
                                //    oAPExport_Invoice_Details.AMTTOTTAX = Math.Round((((decimal)item.AmountBeforeGST) * ((decimal)item.TaxValue)) / 100, (int)2).ToString("#0.00");
                                //}
                                oAPExport_Invoice_Details.AMTTAX1 = "0";
                                oAPExport_Invoice_Details.IDGLACCT = item1.AccountCode;
                                //oAPExport_Invoice_Details.AMTDIST = item1.Amount.ToString();
                                oAPExport_Invoice_Details.AMTDIST = Math.Round((decimal)item1.Amount, (int)2).ToString("#0.00");
                                oAPExport_Invoice_Details.COMMENT = "";
                                oAPExport_Invoice_Details.SWIBT = "0";
                                oAPExport_Invoice_Details.IDINVC = item.VoucherNo;
                                AllInvoiceDetails.Add(oAPExport_Invoice_Details);
                            }
                        }
                        TempData["Status"] = "Selected Mileage Claim Exported Successfully";
                    }
                    catch (Exception ex)
                    {

                    }
                }
                #endregion
            }
            else if (aPExportSearch.ModuleName == "ExpenseClaim")
            {
                var Po_Invoice = await _repository.MstMileageClaim.GetAllMileageClaimForAPExportAsync(aPExportSearch.ClaimIds,aPExportSearch.ModuleName, Int32.Parse(aPExportSearch.FacilityID),Int32.Parse(aPExportSearch.StatusID), aPExportSearch.FromDate, aPExportSearch.ToDate);
                int num1 = 0;

                #region Noraml AP export without Consolidated Invoices
                foreach (var item in Po_Invoice)
                {
                    num1 = num1 + 1;
                    //var id = await _repository.DtExpenseClaim.GetDtExpenseClaimByIdAsync(item.CID);

                    var summary = await _repository.DtExpenseClaimSummary.GetDtExpenseClaimSummaryByIdAsync(item.CID);
                    var TaxClass = summary.Where(s => s.TaxClass != 4 && s.TaxClass != 0).FirstOrDefault();
                    var TaxClass7Amount = summary.Where(s => s.TaxClass == (TaxClass != null ? TaxClass.TaxClass : 1)).GroupBy(s => s.TaxClass).Select(cl => new DtExpenseClaimSummary
                                                                                                        {
                                                                                                            Amount = cl.Sum(c => c.Amount),
                                                                                                            GST = cl.Sum(c => c.GST),
                                                                                                        }).FirstOrDefault();

                    //var id = objERPEntities.DtSupplierPOCs.ToList().Where(p => p.POCID == Convert.ToInt32(item.POCID)).ToList();
                    //if (id.Count != 0)
                    //    Description = id.FirstOrDefault().desc item.SupplierShortName + "/" + objERPEntities.DtSupplierPOCs.ToList().Where(p => p.POCID == Convert.ToInt32(item.POCID)).ToList().FirstOrDefault().Description;
                    //else
                    //    Description = "";
                    int n = summary.Count;
                    if (summary.Count != 0)
                    {
                        if (summary.Count == n && summary.Count == 1)
                        {
                            Description = summary.FirstOrDefault().Description;
                            //Description = "Expense Claim " + "from " + id.FirstOrDefault().DateOfJourney.Day + " " + Helper.Month(id.FirstOrDefault().DateOfJourney.Month) + " to " + id.FirstOrDefault().DateOfJourney.Day + " " + Helper.Month(id.FirstOrDefault().DateOfJourney.Month) + " " + id.FirstOrDefault().DateOfJourney.Year.ToString();
                        }
                        else
                        {
                            Description = summary.FirstOrDefault().Description;
                            //Description = "Expense Claim " + "from " + id.FirstOrDefault().DateOfJourney.Day + " " + Helper.Month(id.FirstOrDefault().DateOfJourney.Month) + " to " + id.LastOrDefault().DateOfJourney.Day + " " + Helper.Month(id.LastOrDefault().DateOfJourney.Month) + " " + id.LastOrDefault().DateOfJourney.Year.ToString();
                        }
                    }

                    APExport_Invoice oAPExport_Invoice = new APExport_Invoice();
                    oAPExport_Invoice.CNTBTCH = "1";
                    oAPExport_Invoice.CNTITEM = num1.ToString();
                    oAPExport_Invoice.IDVEND = "DBS PV";
                    oAPExport_Invoice.IDINVC = item.VoucherNo;
                    oAPExport_Invoice.TEXTTRX = "1";
                    oAPExport_Invoice.ORDRNBR = "";
                    oAPExport_Invoice.PONBR = "";
                    oAPExport_Invoice.INVCDESC = Description;
                    oAPExport_Invoice.INVCAPPLTO = "";
                    oAPExport_Invoice.IDACCTSET = "1";
                    oAPExport_Invoice.DATEINVC = Convert.ToDateTime(DateTime.Now).ToString("dd/MM/yyyy HH:mm:ss", System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
                    oAPExport_Invoice.FISCYR = DateTime.Now.Year.ToString();
                    oAPExport_Invoice.FISCPER = DateTime.Now.Day >= Convert.ToInt32(financeStartDay) && DateTime.Now.Month != 12 ? DateTime.Now.AddMonths(1).Month.ToString() : DateTime.Now.Month.ToString();
                    oAPExport_Invoice.CODECURN = "SGD";
                    oAPExport_Invoice.TERMCODE = "1";
                    oAPExport_Invoice.DATEDUE = Convert.ToDateTime(DateTime.Now).ToString("dd/MM/yyyy HH:mm:ss", System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
                    oAPExport_Invoice.CODETAXGRP = "TXGSGD";

                    oAPExport_Invoice.TAXCLASS1 = (TaxClass != null ?( TaxClass.TaxClass == 4?"4":TaxClass.TaxClass.ToString()):"4");
                    oAPExport_Invoice.BASETAX1 = TaxClass7Amount != null ? Math.Round((decimal)TaxClass7Amount.Amount, (int)2).ToString("#0.00") : "0.00";
                    //if (item. == 0)
                    //{
                    //    oAPExport_Invoice.TAXCLASS1 = "4";
                    //    oAPExport_Invoice.BASETAX1 = "0.00";
                    //}
                    //else
                    //{

                    //    oAPExport_Invoice.TAXCLASS1 = Math.Round((decimal)item.TaxValue, (int)2).ToString("#,##0");
                    //    oAPExport_Invoice.BASETAX1 = Math.Round((decimal)item.AmountBeforeGST, (int)2).ToString("#0.00");
                    //}
                    //oAPExport_Invoice.AMTTAX1 = Math.Round((decimal)item.GSTAmount, (int)2).ToString("#0.00");
                    //oAPExport_Invoice.AMTTAXDIST = Math.Round((decimal)item.GSTAmount, (int)2).ToString("#0.00");
                    //oAPExport_Invoice.AMTINVCTOT = Math.Round((decimal)item.AmountBeforeGST, (int)2).ToString("#0.00");
                    //oAPExport_Invoice.AMTTOTDIST = Math.Round((decimal)item.AmountBeforeGST, (int)2).ToString("#0.00");
                    //oAPExport_Invoice.AMTGROSDST = Math.Round((decimal)item.TotalAmount, (int)2).ToString("#0.00");
                    //oAPExport_Invoice.AMTDUE = Math.Round((decimal)item.TotalAmount, (int)2).ToString("#0.00");
                    //oAPExport_Invoice.AMTTAXTOT = Math.Round((decimal)item.GSTAmount, (int)2).ToString("#0.00");
                    //oAPExport_Invoice.AMTGROSTOT = Math.Round((decimal)item.TotalAmount, (int)2).ToString("#0.00");

                    oAPExport_Invoice.AMTTAX1 = TaxClass7Amount != null ? Math.Round((decimal)TaxClass7Amount.GST, (int)2).ToString("#0.00") : "0.00";
                    oAPExport_Invoice.AMTTAXDIST = TaxClass7Amount != null ? Math.Round((decimal)TaxClass7Amount.GST, (int)2).ToString("#0.00") : "0.00";
                    //oAPExport_Invoice.AMTINVCTOT = TaxClass7Amount != null ? Math.Round((decimal)TaxClass7Amount.Amount, (int)2).ToString("#0.00") : Math.Round((decimal)item.GrandTotal, (int)2).ToString("#0.00");
                    //oAPExport_Invoice.AMTTOTDIST = TaxClass7Amount != null ? Math.Round((decimal)TaxClass7Amount.Amount, (int)2).ToString("#0.00") : Math.Round((decimal)item.GrandTotal, (int)2).ToString("#0.00");
                    oAPExport_Invoice.AMTINVCTOT = TaxClass7Amount != null ? Math.Round((decimal)TaxClass7Amount.Amount, (int)2).ToString("#0.00") : Math.Round((decimal)item.GrandTotal, (int)2).ToString("#0.00");
                    oAPExport_Invoice.AMTTOTDIST = TaxClass7Amount != null ? Math.Round((decimal)TaxClass7Amount.Amount, (int)2).ToString("#0.00") : Math.Round((decimal)item.GrandTotal, (int)2).ToString("#0.00");
                    oAPExport_Invoice.AMTGROSDST = Math.Round((decimal)item.TotalAmount, (int)2).ToString("#0.00");
                    oAPExport_Invoice.AMTDUE = Math.Round((decimal)item.TotalAmount, (int)2).ToString("#0.00");
                    oAPExport_Invoice.AMTTAXTOT = TaxClass7Amount != null ? Math.Round((decimal)TaxClass7Amount.GST, (int)2).ToString("#0.00") : "0.00";
                    oAPExport_Invoice.AMTGROSTOT = Math.Round((decimal)item.TotalAmount, (int)2).ToString("#0.00");

                    AllInvoices.Add(oAPExport_Invoice);


                    APExport_Invoice_Payment_Schedules oAPExport_Invoice_Payment_Schedules = new APExport_Invoice_Payment_Schedules();
                    oAPExport_Invoice_Payment_Schedules.CNTBTCH = "1";
                    oAPExport_Invoice_Payment_Schedules.CNTITEM = num1.ToString();
                    oAPExport_Invoice_Payment_Schedules.CNTPAYM = "";
                    oAPExport_Invoice_Payment_Schedules.DATEDUE = Convert.ToDateTime(DateTime.Now).ToString("dd/MM/yyyy HH:mm:ss", System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
                    oAPExport_Invoice_Payment_Schedules.AMTDUE = Math.Round((decimal)item.TotalAmount, (int)2).ToString("#0.00");
                    AllInvoicesPayments.Add(oAPExport_Invoice_Payment_Schedules);

                    //below code is to change the status to Exported
                    if(aPExportSearch.StatusID == "3")
                        await _repository.MstExpenseClaim.UpdateMstExpenseClaimStatus(item.CID, 9, int.Parse(HttpContext.User.FindFirst("userid").Value), DateTime.Now, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, false, 0);
                    //var oSupplierPOC = objERPEntities.MstSupplierPOCs.ToList().Where(p => p.POCID == item.POCID).FirstOrDefault();
                    //if (oSupplierPOC != null)
                    //{
                    //    oSupplierPOC.ApprovalStatus = 5;
                    //    objERPEntities.SaveChanges();
                    //}
                    ////


                    //var Supplier_Invoice_Details = await _repository.DtExpenseClaim.GetDtExpenseClaimByIdAsync(item.CID);
                    //var Supplier_Invoice_Details = await _repository.DtExpenseClaimSummary.GetDtExpenseClaimSummaryByIdAsync(item.CID);
                    int num = 0;
                    int num2 = 0;
                    try
                    {
                        foreach (var item1 in summary)
                        {
                            if(item1.ExpenseCategory != "DBS")
                            {
                                //var expenseCategory = await _repository.MstExpenseCategory.GetExpenseCategoryByIdAsync(item1.ExpenseCategoryID);
                                //var TaxClassSummary = summaryDetails.FirstOrDefault(s => s.ExpenseCategory == expenseCategory.Description);
                                num = num + 20;
                                num2 = num2 + 1;
                                //if (item.POCNO.Contains("RH") || item.POCNO.Contains("RL"))
                                //{
                                //    num2 = num2 + 21;
                                //}
                                APExport_Invoice_Details oAPExport_Invoice_Details = new APExport_Invoice_Details();
                                oAPExport_Invoice_Details.CNTBTCH = "1";
                                oAPExport_Invoice_Details.CNTITEM = num1.ToString();
                                oAPExport_Invoice_Details.CNTLINE = num.ToString();
                                oAPExport_Invoice_Details.IDDIST = "";
                                oAPExport_Invoice_Details.TEXTDESC = item1.Description;
                                //oAPExport_Invoice_Details.AMTTOTTAX = Math.Round((((decimal)item.AmountBeforeGST) * ((decimal)item.TaxValue)) / 100, (int)2).ToString("#0.00");
                                //oAPExport_Invoice_Details.AMTTOTTAX = Math.Round((decimal)item.AmountBeforeGST, (int)2).ToString("#0.00");

                                oAPExport_Invoice_Details.TAXCLASS1 = item1.TaxClass.ToString();
                                //oAPExport_Invoice_Details.RATETAX1 = "4";
                                oAPExport_Invoice_Details.RATETAX1 = "0";
                                oAPExport_Invoice_Details.BASETAX1 = item1.TaxClass.ToString() == "4.00" ? "0.00" : Math.Round((decimal)item1.Amount, (int)2).ToString("#0.00");
                                oAPExport_Invoice_Details.AMTTOTTAX = item1.TaxClass.ToString() == "4.00" ? "0.00" : Math.Round((decimal)item1.GST, (int)2).ToString("#0.00");

                                //if (item.TaxValue == 0)
                                //{
                                //    oAPExport_Invoice_Details.TAXCLASS1 = "4";
                                //    //oAPExport_Invoice_Details.RATETAX1 = "4";
                                //    oAPExport_Invoice_Details.RATETAX1 = "0";
                                //    oAPExport_Invoice_Details.BASETAX1 = "0.00";
                                //    oAPExport_Invoice_Details.AMTTOTTAX = Math.Round((decimal)item.AmountBeforeGST, (int)2).ToString("#0.00");
                                //}
                                //else
                                //{

                                //    oAPExport_Invoice_Details.BASETAX1 = Math.Round((decimal)item.AmountBeforeGST, (int)2).ToString("#0.00");
                                //    oAPExport_Invoice_Details.TAXCLASS1 = Math.Round((decimal)item.TaxValue, (int)2).ToString("#,##0");
                                //    oAPExport_Invoice_Details.RATETAX1 = Math.Round((decimal)item.TaxValue, (int)2).ToString("#,##0");
                                //    oAPExport_Invoice_Details.AMTTOTTAX = Math.Round((((decimal)item.AmountBeforeGST) * ((decimal)item.TaxValue)) / 100, (int)2).ToString("#0.00");
                                //}
                                oAPExport_Invoice_Details.AMTTAX1 = item1.TaxClass.ToString() == "4.00" ? "0.00" : Math.Round((decimal)item1.GST, (int)2).ToString("#0.00");
                                oAPExport_Invoice_Details.IDGLACCT = item1.AccountCode;
                                //oAPExport_Invoice_Details.AMTDIST = item1.Amount.ToString();
                                oAPExport_Invoice_Details.AMTDIST = Math.Round((decimal)item1.Amount, (int)2).ToString("#0.00");
                                oAPExport_Invoice_Details.COMMENT = "";
                                oAPExport_Invoice_Details.SWIBT = "0";
                                oAPExport_Invoice_Details.IDINVC = item.VoucherNo;
                                AllInvoiceDetails.Add(oAPExport_Invoice_Details);
                            }
                        }
                        TempData["Status"] = "Selected Expense Claim Exported Successfully";
                    }
                    catch (Exception ex)
                    {

                    }
                }
                #endregion
            }
            else if (aPExportSearch.ModuleName == "TelephoneBillClaim")
            {
                var Po_Invoice = await _repository.MstMileageClaim.GetAllMileageClaimForAPExportAsync(aPExportSearch.ClaimIds,aPExportSearch.ModuleName, Int32.Parse(aPExportSearch.FacilityID),Int32.Parse(aPExportSearch.StatusID), aPExportSearch.FromDate, aPExportSearch.ToDate);
                int num1 = 0;

                #region Noraml AP export without Consolidated Invoices
                foreach (var item in Po_Invoice)
                {
                    num1 = num1 + 1;
                    //var id = await _repository.DtTBClaim.GetDtTBClaimByIdAsync(item.CID);
                    var summary = await _repository.DtTBClaimSummary.GetDtTBClaimSummaryByIdAsync(item.CID);
                    var TaxClass = summary.Where(s => s.TaxClass == 4).FirstOrDefault();
                    //var id = objERPEntities.DtSupplierPOCs.ToList().Where(p => p.POCID == Convert.ToInt32(item.POCID)).ToList();
                    //if (id.Count != 0)
                    //    Description = id.FirstOrDefault().desc item.SupplierShortName + "/" + objERPEntities.DtSupplierPOCs.ToList().Where(p => p.POCID == Convert.ToInt32(item.POCID)).ToList().FirstOrDefault().Description;
                    //else
                    //    Description = "";
                    int n = summary.Count;
                    if (summary.Count != 0)
                    {
                        if (summary.Count == n && summary.Count == 1)
                        {
                            Description = summary.FirstOrDefault().Description;
                            //Description = "Expense Claim " + "from " + id.FirstOrDefault().DateOfJourney.Day + " " + Helper.Month(id.FirstOrDefault().DateOfJourney.Month) + " to " + id.FirstOrDefault().DateOfJourney.Day + " " + Helper.Month(id.FirstOrDefault().DateOfJourney.Month) + " " + id.FirstOrDefault().DateOfJourney.Year.ToString();
                        }
                        else
                        {
                            Description = summary.FirstOrDefault().Description;
                            //Description = "Expense Claim " + "from " + id.FirstOrDefault().DateOfJourney.Day + " " + Helper.Month(id.FirstOrDefault().DateOfJourney.Month) + " to " + id.LastOrDefault().DateOfJourney.Day + " " + Helper.Month(id.LastOrDefault().DateOfJourney.Month) + " " + id.LastOrDefault().DateOfJourney.Year.ToString();
                        }
                    }

                    APExport_Invoice oAPExport_Invoice = new APExport_Invoice();
                    oAPExport_Invoice.CNTBTCH = "1";
                    oAPExport_Invoice.CNTITEM = num1.ToString();
                    oAPExport_Invoice.IDVEND = "DBS PV";
                    oAPExport_Invoice.IDINVC = item.VoucherNo;
                    oAPExport_Invoice.TEXTTRX = "1";
                    oAPExport_Invoice.ORDRNBR = "";
                    oAPExport_Invoice.PONBR = "";
                    oAPExport_Invoice.INVCDESC = Description;
                    oAPExport_Invoice.INVCAPPLTO = "";
                    oAPExport_Invoice.IDACCTSET = "1";
                    oAPExport_Invoice.DATEINVC = Convert.ToDateTime(DateTime.Now).ToString("dd/MM/yyyy HH:mm:ss", System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
                    oAPExport_Invoice.FISCYR = DateTime.Now.Year.ToString();
                    oAPExport_Invoice.FISCPER = DateTime.Now.Day >= Convert.ToInt32(financeStartDay) && DateTime.Now.Month != 12 ? DateTime.Now.AddMonths(1).Month.ToString() : DateTime.Now.Month.ToString();
                    oAPExport_Invoice.CODECURN = "SGD";
                    oAPExport_Invoice.TERMCODE = "1";
                    oAPExport_Invoice.DATEDUE = Convert.ToDateTime(DateTime.Now).ToString("dd/MM/yyyy HH:mm:ss", System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
                    oAPExport_Invoice.CODETAXGRP = "TXGSGD";

                    oAPExport_Invoice.TAXCLASS1 = (TaxClass != null ? (TaxClass.TaxClass == 4 ? "4" : TaxClass.TaxClass.ToString()) : "4");
                    oAPExport_Invoice.BASETAX1 = "0.00";
                    //if (item. == 0)
                    //{
                    //    oAPExport_Invoice.TAXCLASS1 = "4";
                    //    oAPExport_Invoice.BASETAX1 = "0.00";
                    //}
                    //else
                    //{

                    //    oAPExport_Invoice.TAXCLASS1 = Math.Round((decimal)item.TaxValue, (int)2).ToString("#,##0");
                    //    oAPExport_Invoice.BASETAX1 = Math.Round((decimal)item.AmountBeforeGST, (int)2).ToString("#0.00");
                    //}
                    //oAPExport_Invoice.AMTTAX1 = Math.Round((decimal)item.GSTAmount, (int)2).ToString("#0.00");
                    //oAPExport_Invoice.AMTTAXDIST = Math.Round((decimal)item.GSTAmount, (int)2).ToString("#0.00");
                    //oAPExport_Invoice.AMTINVCTOT = Math.Round((decimal)item.AmountBeforeGST, (int)2).ToString("#0.00");
                    //oAPExport_Invoice.AMTTOTDIST = Math.Round((decimal)item.AmountBeforeGST, (int)2).ToString("#0.00");
                    //oAPExport_Invoice.AMTGROSDST = Math.Round((decimal)item.TotalAmount, (int)2).ToString("#0.00");
                    //oAPExport_Invoice.AMTDUE = Math.Round((decimal)item.TotalAmount, (int)2).ToString("#0.00");
                    //oAPExport_Invoice.AMTTAXTOT = Math.Round((decimal)item.GSTAmount, (int)2).ToString("#0.00");
                    //oAPExport_Invoice.AMTGROSTOT = Math.Round((decimal)item.TotalAmount, (int)2).ToString("#0.00");

                    oAPExport_Invoice.AMTTAX1 = "0.00";
                    oAPExport_Invoice.AMTTAXDIST = "0.00";
                    oAPExport_Invoice.AMTINVCTOT = Math.Round((decimal)item.GrandTotal, (int)2).ToString("#0.00");
                    oAPExport_Invoice.AMTTOTDIST = Math.Round((decimal)item.GrandTotal, (int)2).ToString("#0.00");
                    oAPExport_Invoice.AMTGROSDST = Math.Round((decimal)item.GrandTotal, (int)2).ToString("#0.00");
                    oAPExport_Invoice.AMTDUE = Math.Round((decimal)item.GrandTotal, (int)2).ToString("#0.00");
                    oAPExport_Invoice.AMTTAXTOT = "0.00";
                    oAPExport_Invoice.AMTGROSTOT = Math.Round((decimal)item.GrandTotal, (int)2).ToString("#0.00");

                    AllInvoices.Add(oAPExport_Invoice);


                    APExport_Invoice_Payment_Schedules oAPExport_Invoice_Payment_Schedules = new APExport_Invoice_Payment_Schedules();
                    oAPExport_Invoice_Payment_Schedules.CNTBTCH = "1";
                    oAPExport_Invoice_Payment_Schedules.CNTITEM = num1.ToString();
                    oAPExport_Invoice_Payment_Schedules.CNTPAYM = "";
                    oAPExport_Invoice_Payment_Schedules.DATEDUE = Convert.ToDateTime(DateTime.Now).ToString("dd/MM/yyyy HH:mm:ss", System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
                    oAPExport_Invoice_Payment_Schedules.AMTDUE = Math.Round((decimal)item.GrandTotal, (int)2).ToString("#0.00");
                    AllInvoicesPayments.Add(oAPExport_Invoice_Payment_Schedules);

                    //below code is to change the status to Exported
                    if (aPExportSearch.StatusID == "3")
                        await _repository.MstTBClaim.UpdateMstTBClaimStatus(item.CID, 9, int.Parse(HttpContext.User.FindFirst("userid").Value), DateTime.Now, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, false, 0);
                    //var oSupplierPOC = objERPEntities.MstSupplierPOCs.ToList().Where(p => p.POCID == item.POCID).FirstOrDefault();
                    //if (oSupplierPOC != null)
                    //{
                    //    oSupplierPOC.ApprovalStatus = 5;
                    //    objERPEntities.SaveChanges();
                    //}
                    ////


                    //var Supplier_Invoice_Details = await _repository.DtTBClaim.GetDtTBClaimByIdAsync(item.CID);
                    //var summaryDetails = await _repository.DtTBClaimSummary.GetDtTBClaimSummaryByIdAsync(item.CID);
                    int num = 0;
                    int num2 = 0;
                    try
                    {
                        foreach (var item1 in summary)
                        {
                            if (item1.ExpenseCategory != "DBS")
                            {
                                //var expenseCategory = await _repository.MstExpenseCategory.GetExpenseCategoryByIdAsync(item1.ExpenseCategoryID);
                                //var TaxClassSummary = summaryDetails.FirstOrDefault(s => s.ExpenseCategory == expenseCategory.Description);
                                num = num + 20;
                                num2 = num2 + 1;
                                //if (item.POCNO.Contains("RH") || item.POCNO.Contains("RL"))
                                //{
                                //    num2 = num2 + 21;
                                //}
                                APExport_Invoice_Details oAPExport_Invoice_Details = new APExport_Invoice_Details();
                                oAPExport_Invoice_Details.CNTBTCH = "1";
                                oAPExport_Invoice_Details.CNTITEM = num1.ToString();
                                oAPExport_Invoice_Details.CNTLINE = num.ToString();
                                oAPExport_Invoice_Details.IDDIST = "";
                                oAPExport_Invoice_Details.TEXTDESC = item1.Description;
                                //oAPExport_Invoice_Details.AMTTOTTAX = Math.Round((((decimal)item.AmountBeforeGST) * ((decimal)item.TaxValue)) / 100, (int)2).ToString("#0.00");
                                //oAPExport_Invoice_Details.AMTTOTTAX = Math.Round((decimal)item.AmountBeforeGST, (int)2).ToString("#0.00");

                                oAPExport_Invoice_Details.TAXCLASS1 = item1.TaxClass.ToString();
                                //oAPExport_Invoice_Details.RATETAX1 = "4";
                                oAPExport_Invoice_Details.RATETAX1 = "0";
                                oAPExport_Invoice_Details.BASETAX1 = "0.00";
                                oAPExport_Invoice_Details.AMTTOTTAX = "0";

                                //if (item.TaxValue == 0)
                                //{
                                //    oAPExport_Invoice_Details.TAXCLASS1 = "4";
                                //    //oAPExport_Invoice_Details.RATETAX1 = "4";
                                //    oAPExport_Invoice_Details.RATETAX1 = "0";
                                //    oAPExport_Invoice_Details.BASETAX1 = "0.00";
                                //    oAPExport_Invoice_Details.AMTTOTTAX = Math.Round((decimal)item.AmountBeforeGST, (int)2).ToString("#0.00");
                                //}
                                //else
                                //{

                                //    oAPExport_Invoice_Details.BASETAX1 = Math.Round((decimal)item.AmountBeforeGST, (int)2).ToString("#0.00");
                                //    oAPExport_Invoice_Details.TAXCLASS1 = Math.Round((decimal)item.TaxValue, (int)2).ToString("#,##0");
                                //    oAPExport_Invoice_Details.RATETAX1 = Math.Round((decimal)item.TaxValue, (int)2).ToString("#,##0");
                                //    oAPExport_Invoice_Details.AMTTOTTAX = Math.Round((((decimal)item.AmountBeforeGST) * ((decimal)item.TaxValue)) / 100, (int)2).ToString("#0.00");
                                //}
                                oAPExport_Invoice_Details.AMTTAX1 = "0";
                                oAPExport_Invoice_Details.IDGLACCT = item1.AccountCode;
                                //oAPExport_Invoice_Details.AMTDIST = item1.Amount.ToString();
                                oAPExport_Invoice_Details.AMTDIST = Math.Round((decimal)item1.Amount, (int)2).ToString("#0.00");
                                oAPExport_Invoice_Details.COMMENT = "";
                                oAPExport_Invoice_Details.SWIBT = "0";
                                oAPExport_Invoice_Details.IDINVC = item.VoucherNo;
                                AllInvoiceDetails.Add(oAPExport_Invoice_Details);
                            }
                        }
                        TempData["Status"] = "Selected Telephone Bill Claim Exported Successfully";
                    }
                    catch (Exception ex)
                    {

                    }
                }
                #endregion
            }
            else if (aPExportSearch.ModuleName == "PV-ChequeClaim")
            {
                var Po_Invoice = await _repository.MstMileageClaim.GetAllMileageClaimForAPExportAsync(aPExportSearch.ClaimIds,aPExportSearch.ModuleName, Int32.Parse(aPExportSearch.FacilityID),Int32.Parse(aPExportSearch.StatusID), aPExportSearch.FromDate, aPExportSearch.ToDate);
                int num1 = 0;
                long tCID = 0;

                #region Noraml AP export without Consolidated Invoices
                foreach (var item in Po_Invoice)
                {
                    if (tCID != item.CID)
                    {
                        num1 = num1 + 1;
                        //var id = await _repository.DtPVCClaim.GetDtPVCClaimByIdAsync(item.CID);
                        var summary = await _repository.DtPVCClaimSummary.GetDtPVCClaimSummaryByIdAsync(item.CID);
                        var TaxClass = summary.Where(s => s.TaxClass != 4 && s.TaxClass != 0).FirstOrDefault();
                        var TaxClass7Amount = summary.Where(s => s.TaxClass == (TaxClass != null ? TaxClass.TaxClass : 1)).GroupBy(s => s.TaxClass).Select(cl => new DtExpenseClaimSummary
                        {
                            Amount = cl.Sum(c => c.Amount),
                            GST = cl.Sum(c => c.GST),
                        }).FirstOrDefault();
                        //var id = objERPEntities.DtSupplierPOCs.ToList().Where(p => p.POCID == Convert.ToInt32(item.POCID)).ToList();
                        //if (id.Count != 0)
                        //    Description = id.FirstOrDefault().desc item.SupplierShortName + "/" + objERPEntities.DtSupplierPOCs.ToList().Where(p => p.POCID == Convert.ToInt32(item.POCID)).ToList().FirstOrDefault().Description;
                        //else
                        //    Description = "";
                        int n = summary.Count;
                        if (summary.Count != 0)
                        {
                            if (summary.Count == n && summary.Count == 1)
                            {
                                Description = summary.FirstOrDefault().Description;
                                //Description = "Expense Claim " + "from " + id.FirstOrDefault().DateOfJourney.Day + " " + Helper.Month(id.FirstOrDefault().DateOfJourney.Month) + " to " + id.FirstOrDefault().DateOfJourney.Day + " " + Helper.Month(id.FirstOrDefault().DateOfJourney.Month) + " " + id.FirstOrDefault().DateOfJourney.Year.ToString();
                            }
                            else
                            {
                                Description = summary.FirstOrDefault().Description;
                                //Description = "Expense Claim " + "from " + id.FirstOrDefault().DateOfJourney.Day + " " + Helper.Month(id.FirstOrDefault().DateOfJourney.Month) + " to " + id.LastOrDefault().DateOfJourney.Day + " " + Helper.Month(id.LastOrDefault().DateOfJourney.Month) + " " + id.LastOrDefault().DateOfJourney.Year.ToString();
                            }
                        }

                        APExport_Invoice oAPExport_Invoice = new APExport_Invoice();
                        oAPExport_Invoice.CNTBTCH = "1";
                        oAPExport_Invoice.CNTITEM = num1.ToString();
                        oAPExport_Invoice.IDVEND = "DBS PV";
                        oAPExport_Invoice.IDINVC = item.VoucherNo;
                        oAPExport_Invoice.TEXTTRX = "1";
                        oAPExport_Invoice.ORDRNBR = "";
                        oAPExport_Invoice.PONBR = "";
                        oAPExport_Invoice.INVCDESC = Description;
                        oAPExport_Invoice.INVCAPPLTO = "";
                        oAPExport_Invoice.IDACCTSET = "1";
                        oAPExport_Invoice.DATEINVC = Convert.ToDateTime(DateTime.Now).ToString("dd/MM/yyyy HH:mm:ss", System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
                        oAPExport_Invoice.FISCYR = DateTime.Now.Year.ToString();
                        oAPExport_Invoice.FISCPER = DateTime.Now.Day >= Convert.ToInt32(financeStartDay) && DateTime.Now.Month != 12 ? DateTime.Now.AddMonths(1).Month.ToString() : DateTime.Now.Month.ToString();
                        oAPExport_Invoice.CODECURN = "SGD";
                        oAPExport_Invoice.TERMCODE = "1";
                        oAPExport_Invoice.DATEDUE = Convert.ToDateTime(DateTime.Now).ToString("dd/MM/yyyy HH:mm:ss", System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
                        oAPExport_Invoice.CODETAXGRP = "TXGSGD";

                        oAPExport_Invoice.TAXCLASS1 = (TaxClass != null ? (TaxClass.TaxClass == 4 ? "4" : TaxClass.TaxClass.ToString()) : "4");
                        oAPExport_Invoice.BASETAX1 = TaxClass7Amount != null ? Math.Round((decimal)TaxClass7Amount.Amount, (int)2).ToString("#0.00") : "0.00";
                        //if (item. == 0)
                        //{
                        //    oAPExport_Invoice.TAXCLASS1 = "4";
                        //    oAPExport_Invoice.BASETAX1 = "0.00";
                        //}
                        //else
                        //{

                        //    oAPExport_Invoice.TAXCLASS1 = Math.Round((decimal)item.TaxValue, (int)2).ToString("#,##0");
                        //    oAPExport_Invoice.BASETAX1 = Math.Round((decimal)item.AmountBeforeGST, (int)2).ToString("#0.00");
                        //}
                        //oAPExport_Invoice.AMTTAX1 = Math.Round((decimal)item.GSTAmount, (int)2).ToString("#0.00");
                        //oAPExport_Invoice.AMTTAXDIST = Math.Round((decimal)item.GSTAmount, (int)2).ToString("#0.00");
                        //oAPExport_Invoice.AMTINVCTOT = Math.Round((decimal)item.AmountBeforeGST, (int)2).ToString("#0.00");
                        //oAPExport_Invoice.AMTTOTDIST = Math.Round((decimal)item.AmountBeforeGST, (int)2).ToString("#0.00");
                        //oAPExport_Invoice.AMTGROSDST = Math.Round((decimal)item.TotalAmount, (int)2).ToString("#0.00");
                        //oAPExport_Invoice.AMTDUE = Math.Round((decimal)item.TotalAmount, (int)2).ToString("#0.00");
                        //oAPExport_Invoice.AMTTAXTOT = Math.Round((decimal)item.GSTAmount, (int)2).ToString("#0.00");
                        //oAPExport_Invoice.AMTGROSTOT = Math.Round((decimal)item.TotalAmount, (int)2).ToString("#0.00");

                        oAPExport_Invoice.AMTTAX1 = TaxClass7Amount != null ? Math.Round((decimal)TaxClass7Amount.GST, (int)2).ToString("#0.00") : "0.00";
                        oAPExport_Invoice.AMTTAXDIST = TaxClass7Amount != null ? Math.Round((decimal)TaxClass7Amount.GST, (int)2).ToString("#0.00") : "0.00";
                        oAPExport_Invoice.AMTINVCTOT = TaxClass7Amount != null ? Math.Round((decimal)TaxClass7Amount.Amount, (int)2).ToString("#0.00") : Math.Round((decimal)item.GrandTotal, (int)2).ToString("#0.00");
                        oAPExport_Invoice.AMTTOTDIST = TaxClass7Amount != null ? Math.Round((decimal)TaxClass7Amount.Amount, (int)2).ToString("#0.00") : Math.Round((decimal)item.GrandTotal, (int)2).ToString("#0.00");
                        oAPExport_Invoice.AMTGROSDST = Math.Round((decimal)item.TotalAmount, (int)2).ToString("#0.00");
                        oAPExport_Invoice.AMTDUE = Math.Round((decimal)item.TotalAmount, (int)2).ToString("#0.00");
                        oAPExport_Invoice.AMTTAXTOT = TaxClass7Amount != null ? Math.Round((decimal)TaxClass7Amount.GST, (int)2).ToString("#0.00") : "0.00";
                        oAPExport_Invoice.AMTGROSTOT = Math.Round((decimal)item.TotalAmount, (int)2).ToString("#0.00");

                        AllInvoices.Add(oAPExport_Invoice);


                        APExport_Invoice_Payment_Schedules oAPExport_Invoice_Payment_Schedules = new APExport_Invoice_Payment_Schedules();
                        oAPExport_Invoice_Payment_Schedules.CNTBTCH = "1";
                        oAPExport_Invoice_Payment_Schedules.CNTITEM = num1.ToString();
                        oAPExport_Invoice_Payment_Schedules.CNTPAYM = "";
                        oAPExport_Invoice_Payment_Schedules.DATEDUE = Convert.ToDateTime(DateTime.Now).ToString("dd/MM/yyyy HH:mm:ss", System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
                        oAPExport_Invoice_Payment_Schedules.AMTDUE = Math.Round((decimal)item.TotalAmount, (int)2).ToString("#0.00");
                        AllInvoicesPayments.Add(oAPExport_Invoice_Payment_Schedules);

                        //below code is to change the status to Exported
                        if (aPExportSearch.StatusID == "3")
                            await _repository.MstPVCClaim.UpdateMstPVCClaimStatus(item.CID, 9, int.Parse(HttpContext.User.FindFirst("userid").Value), DateTime.Now, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, false, 0);
                        //var oSupplierPOC = objERPEntities.MstSupplierPOCs.ToList().Where(p => p.POCID == item.POCID).FirstOrDefault();
                        //if (oSupplierPOC != null)
                        //{
                        //    oSupplierPOC.ApprovalStatus = 5;
                        //    objERPEntities.SaveChanges();
                        //}
                        ////


                        //var Supplier_Invoice_Details = await _repository.DtPVCClaim.GetDtPVCClaimByIdAsync(item.CID);
                        //var summaryDetails = await _repository.DtPVCClaimSummary.GetDtPVCClaimSummaryByIdAsync(item.CID);
                        int num = 0;
                        int num2 = 0;
                        try
                        {
                            foreach (var item1 in summary)
                            {
                                if (item1.ExpenseCategory != "DBS")
                                {
                                    //var expenseCategory = await _repository.MstExpenseCategory.GetExpenseCategoryByIdAsync(item1.ExpenseCategoryID);
                                    //var TaxClassSummary = summaryDetails.FirstOrDefault(s => s.ExpenseCategory == expenseCategory.Description);
                                    num = num + 20;
                                    num2 = num2 + 1;
                                    //if (item.POCNO.Contains("RH") || item.POCNO.Contains("RL"))
                                    //{
                                    //    num2 = num2 + 21;
                                    //}
                                    APExport_Invoice_Details oAPExport_Invoice_Details = new APExport_Invoice_Details();
                                    oAPExport_Invoice_Details.CNTBTCH = "1";
                                    oAPExport_Invoice_Details.CNTITEM = num1.ToString();
                                    oAPExport_Invoice_Details.CNTLINE = num.ToString();
                                    oAPExport_Invoice_Details.IDDIST = "";
                                    oAPExport_Invoice_Details.TEXTDESC = item1.Description;
                                    //oAPExport_Invoice_Details.AMTTOTTAX = Math.Round((((decimal)item.AmountBeforeGST) * ((decimal)item.TaxValue)) / 100, (int)2).ToString("#0.00");
                                    //oAPExport_Invoice_Details.AMTTOTTAX = Math.Round((decimal)item.AmountBeforeGST, (int)2).ToString("#0.00");

                                    oAPExport_Invoice_Details.TAXCLASS1 = item1.TaxClass.ToString();
                                    //oAPExport_Invoice_Details.RATETAX1 = "4";
                                    oAPExport_Invoice_Details.RATETAX1 = "0";
                                    oAPExport_Invoice_Details.BASETAX1 = item1.TaxClass.ToString() == "4.00" ? "0.00" : Math.Round((decimal)item1.Amount, (int)2).ToString("#0.00");
                                    oAPExport_Invoice_Details.AMTTOTTAX = item1.TaxClass.ToString() == "4.00" ? "0.00" : Math.Round((decimal)item1.GST, (int)2).ToString("#0.00");

                                    //if (item.TaxValue == 0)
                                    //{
                                    //    oAPExport_Invoice_Details.TAXCLASS1 = "4";
                                    //    //oAPExport_Invoice_Details.RATETAX1 = "4";
                                    //    oAPExport_Invoice_Details.RATETAX1 = "0";
                                    //    oAPExport_Invoice_Details.BASETAX1 = "0.00";
                                    //    oAPExport_Invoice_Details.AMTTOTTAX = Math.Round((decimal)item.AmountBeforeGST, (int)2).ToString("#0.00");
                                    //}
                                    //else
                                    //{

                                    //    oAPExport_Invoice_Details.BASETAX1 = Math.Round((decimal)item.AmountBeforeGST, (int)2).ToString("#0.00");
                                    //    oAPExport_Invoice_Details.TAXCLASS1 = Math.Round((decimal)item.TaxValue, (int)2).ToString("#,##0");
                                    //    oAPExport_Invoice_Details.RATETAX1 = Math.Round((decimal)item.TaxValue, (int)2).ToString("#,##0");
                                    //    oAPExport_Invoice_Details.AMTTOTTAX = Math.Round((((decimal)item.AmountBeforeGST) * ((decimal)item.TaxValue)) / 100, (int)2).ToString("#0.00");
                                    //}
                                    oAPExport_Invoice_Details.AMTTAX1 = item1.TaxClass.ToString() == "4.00" ? "0.00" : Math.Round((decimal)item1.GST, (int)2).ToString("#0.00");
                                    oAPExport_Invoice_Details.IDGLACCT = item1.AccountCode;
                                    //oAPExport_Invoice_Details.AMTDIST = item1.Amount.ToString();
                                    oAPExport_Invoice_Details.AMTDIST = Math.Round((decimal)item1.Amount, (int)2).ToString("#0.00");
                                    oAPExport_Invoice_Details.COMMENT = "";
                                    oAPExport_Invoice_Details.SWIBT = "0";
                                    oAPExport_Invoice_Details.IDINVC = item.VoucherNo;
                                    AllInvoiceDetails.Add(oAPExport_Invoice_Details);
                                }
                            }
                            tCID = item.CID;
                            TempData["Status"] = "Selected PV-Cheque Claim Exported Successfully";
                        }
                        catch (Exception ex)
                        {

                        }
                    }
                }
                #endregion
            }
            else if (aPExportSearch.ModuleName == "PV-GiroClaim")
            {
                var Po_Invoice = await _repository.MstMileageClaim.GetAllMileageClaimForAPExportAsync(aPExportSearch.ClaimIds,aPExportSearch.ModuleName, Int32.Parse(aPExportSearch.FacilityID),Int32.Parse(aPExportSearch.StatusID), aPExportSearch.FromDate, aPExportSearch.ToDate);
                int num1 = 0;
                long tCID = 0;

                #region Noraml AP export without Consolidated Invoices
                foreach (var item in Po_Invoice)
                {
                    if (tCID != item.CID)
                    {
                        num1 = num1 + 1;
                        //var id = await _repository.DtPVGClaim.GetDtPVGClaimByIdAsync(item.CID);
                        var summary = await _repository.DtPVGClaimSummary.GetDtPVGClaimSummaryByIdAsync(item.CID);
                        var TaxClass = summary.Where(s => s.TaxClass != 4 && s.TaxClass != 0).FirstOrDefault();
                        var TaxClass7Amount = summary.Where(s => s.TaxClass == (TaxClass != null ? TaxClass.TaxClass : 1)).GroupBy(s => s.TaxClass).Select(cl => new DtExpenseClaimSummary
                        {
                            Amount = cl.Sum(c => c.Amount),
                            GST = cl.Sum(c => c.GST),
                        }).FirstOrDefault();
                        //var id = objERPEntities.DtSupplierPOCs.ToList().Where(p => p.POCID == Convert.ToInt32(item.POCID)).ToList();
                        //if (id.Count != 0)
                        //    Description = id.FirstOrDefault().desc item.SupplierShortName + "/" + objERPEntities.DtSupplierPOCs.ToList().Where(p => p.POCID == Convert.ToInt32(item.POCID)).ToList().FirstOrDefault().Description;
                        //else
                        //    Description = "";
                        int n = summary.Count;
                        if (summary.Count != 0)
                        {
                            if (summary.Count == n && summary.Count == 1)
                            {
                                Description = summary.FirstOrDefault().Description;
                                //Description = "Expense Claim " + "from " + id.FirstOrDefault().DateOfJourney.Day + " " + Helper.Month(id.FirstOrDefault().DateOfJourney.Month) + " to " + id.FirstOrDefault().DateOfJourney.Day + " " + Helper.Month(id.FirstOrDefault().DateOfJourney.Month) + " " + id.FirstOrDefault().DateOfJourney.Year.ToString();
                            }
                            else
                            {
                                Description = summary.FirstOrDefault().Description;
                                //Description = "Expense Claim " + "from " + id.FirstOrDefault().DateOfJourney.Day + " " + Helper.Month(id.FirstOrDefault().DateOfJourney.Month) + " to " + id.LastOrDefault().DateOfJourney.Day + " " + Helper.Month(id.LastOrDefault().DateOfJourney.Month) + " " + id.LastOrDefault().DateOfJourney.Year.ToString();
                            }
                        }

                        APExport_Invoice oAPExport_Invoice = new APExport_Invoice();
                        oAPExport_Invoice.CNTBTCH = "1";
                        oAPExport_Invoice.CNTITEM = num1.ToString();
                        oAPExport_Invoice.IDVEND = "DBS PV";
                        oAPExport_Invoice.IDINVC = item.VoucherNo;
                        oAPExport_Invoice.TEXTTRX = "1";
                        oAPExport_Invoice.ORDRNBR = "";
                        oAPExport_Invoice.PONBR = "";
                        oAPExport_Invoice.INVCDESC = Description;
                        oAPExport_Invoice.INVCAPPLTO = "";
                        oAPExport_Invoice.IDACCTSET = "1";
                        oAPExport_Invoice.DATEINVC = Convert.ToDateTime(DateTime.Now).ToString("dd/MM/yyyy HH:mm:ss", System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
                        oAPExport_Invoice.FISCYR = DateTime.Now.Year.ToString();
                        oAPExport_Invoice.FISCPER = DateTime.Now.Day >= Convert.ToInt32(financeStartDay) && DateTime.Now.Month != 12 ? DateTime.Now.AddMonths(1).Month.ToString() : DateTime.Now.Month.ToString();
                        oAPExport_Invoice.CODECURN = "SGD";
                        oAPExport_Invoice.TERMCODE = "1";
                        oAPExport_Invoice.DATEDUE = Convert.ToDateTime(DateTime.Now).ToString("dd/MM/yyyy HH:mm:ss", System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
                        oAPExport_Invoice.CODETAXGRP = "TXGSGD";

                        oAPExport_Invoice.TAXCLASS1 = (TaxClass != null ? (TaxClass.TaxClass == 4 ? "4" : TaxClass.TaxClass.ToString()) : "4");
                        oAPExport_Invoice.BASETAX1 = TaxClass7Amount != null ? Math.Round((decimal)TaxClass7Amount.Amount, (int)2).ToString("#0.00") : "0.00";
                        //if (item. == 0)
                        //{
                        //    oAPExport_Invoice.TAXCLASS1 = "4";
                        //    oAPExport_Invoice.BASETAX1 = "0.00";
                        //}
                        //else
                        //{

                        //    oAPExport_Invoice.TAXCLASS1 = Math.Round((decimal)item.TaxValue, (int)2).ToString("#,##0");
                        //    oAPExport_Invoice.BASETAX1 = Math.Round((decimal)item.AmountBeforeGST, (int)2).ToString("#0.00");
                        //}
                        //oAPExport_Invoice.AMTTAX1 = Math.Round((decimal)item.GSTAmount, (int)2).ToString("#0.00");
                        //oAPExport_Invoice.AMTTAXDIST = Math.Round((decimal)item.GSTAmount, (int)2).ToString("#0.00");
                        //oAPExport_Invoice.AMTINVCTOT = Math.Round((decimal)item.AmountBeforeGST, (int)2).ToString("#0.00");
                        //oAPExport_Invoice.AMTTOTDIST = Math.Round((decimal)item.AmountBeforeGST, (int)2).ToString("#0.00");
                        //oAPExport_Invoice.AMTGROSDST = Math.Round((decimal)item.TotalAmount, (int)2).ToString("#0.00");
                        //oAPExport_Invoice.AMTDUE = Math.Round((decimal)item.TotalAmount, (int)2).ToString("#0.00");
                        //oAPExport_Invoice.AMTTAXTOT = Math.Round((decimal)item.GSTAmount, (int)2).ToString("#0.00");
                        //oAPExport_Invoice.AMTGROSTOT = Math.Round((decimal)item.TotalAmount, (int)2).ToString("#0.00");

                        oAPExport_Invoice.AMTTAX1 = TaxClass7Amount != null ? Math.Round((decimal)TaxClass7Amount.GST, (int)2).ToString("#0.00") : "0.00";
                        oAPExport_Invoice.AMTTAXDIST = TaxClass7Amount != null ? Math.Round((decimal)TaxClass7Amount.GST, (int)2).ToString("#0.00") : "0.00";
                        //oAPExport_Invoice.AMTINVCTOT = TaxClass7Amount != null ? Math.Round((decimal)TaxClass7Amount.Amount, (int)2).ToString("#0.00") : Math.Round((decimal)item.GrandTotal, (int)2).ToString("#0.00");
                        //oAPExport_Invoice.AMTTOTDIST = TaxClass7Amount != null ? Math.Round((decimal)TaxClass7Amount.Amount, (int)2).ToString("#0.00") : Math.Round((decimal)item.GrandTotal, (int)2).ToString("#0.00");
                        oAPExport_Invoice.AMTINVCTOT = TaxClass7Amount != null ? Math.Round((decimal)TaxClass7Amount.Amount, (int)2).ToString("#0.00") : Math.Round((decimal)item.GrandTotal, (int)2).ToString("#0.00");
                        oAPExport_Invoice.AMTTOTDIST = TaxClass7Amount != null ? Math.Round((decimal)TaxClass7Amount.Amount, (int)2).ToString("#0.00") : Math.Round((decimal)item.GrandTotal, (int)2).ToString("#0.00");
                        oAPExport_Invoice.AMTGROSDST = Math.Round((decimal)item.TotalAmount, (int)2).ToString("#0.00");
                        oAPExport_Invoice.AMTDUE = Math.Round((decimal)item.TotalAmount, (int)2).ToString("#0.00");
                        oAPExport_Invoice.AMTTAXTOT = TaxClass7Amount != null ? Math.Round((decimal)TaxClass7Amount.GST, (int)2).ToString("#0.00") : "0.00";
                        oAPExport_Invoice.AMTGROSTOT = Math.Round((decimal)item.TotalAmount, (int)2).ToString("#0.00");

                        AllInvoices.Add(oAPExport_Invoice);


                        APExport_Invoice_Payment_Schedules oAPExport_Invoice_Payment_Schedules = new APExport_Invoice_Payment_Schedules();
                        oAPExport_Invoice_Payment_Schedules.CNTBTCH = "1";
                        oAPExport_Invoice_Payment_Schedules.CNTITEM = num1.ToString();
                        oAPExport_Invoice_Payment_Schedules.CNTPAYM = "";
                        oAPExport_Invoice_Payment_Schedules.DATEDUE = Convert.ToDateTime(DateTime.Now).ToString("dd/MM/yyyy HH:mm:ss", System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
                        oAPExport_Invoice_Payment_Schedules.AMTDUE = Math.Round((decimal)item.TotalAmount, (int)2).ToString("#0.00");
                        AllInvoicesPayments.Add(oAPExport_Invoice_Payment_Schedules);

                        //below code is to change the status to Exported
                        if (aPExportSearch.StatusID == "3")
                            await _repository.MstPVGClaim.UpdateMstPVGClaimStatus(item.CID, 9, int.Parse(HttpContext.User.FindFirst("userid").Value), DateTime.Now, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, false, 0);
                        //var oSupplierPOC = objERPEntities.MstSupplierPOCs.ToList().Where(p => p.POCID == item.POCID).FirstOrDefault();
                        //if (oSupplierPOC != null)
                        //{
                        //    oSupplierPOC.ApprovalStatus = 5;
                        //    objERPEntities.SaveChanges();
                        //}
                        ////


                        //var Supplier_Invoice_Details = await _repository.DtPVGClaim.GetDtPVGClaimByIdAsync(item.CID);
                        //var summaryDetails = await _repository.DtPVGClaimSummary.GetDtPVGClaimSummaryByIdAsync(item.CID);
                        int num = 0;
                        int num2 = 0;
                        try
                        {
                            foreach (var item1 in summary)
                            {
                                if (item1.ExpenseCategory != "DBS")
                                {
                                    //var expenseCategory = await _repository.MstExpenseCategory.GetExpenseCategoryByIdAsync(item1.ExpenseCategoryID);
                                    //var TaxClassSummary = summaryDetails.FirstOrDefault(s => s.ExpenseCategory == expenseCategory.Description);
                                    num = num + 20;
                                    num2 = num2 + 1;
                                    //if (item.POCNO.Contains("RH") || item.POCNO.Contains("RL"))
                                    //{
                                    //    num2 = num2 + 21;
                                    //}
                                    APExport_Invoice_Details oAPExport_Invoice_Details = new APExport_Invoice_Details();
                                    oAPExport_Invoice_Details.CNTBTCH = "1";
                                    oAPExport_Invoice_Details.CNTITEM = num1.ToString();
                                    oAPExport_Invoice_Details.CNTLINE = num.ToString();
                                    oAPExport_Invoice_Details.IDDIST = "";
                                    oAPExport_Invoice_Details.TEXTDESC = item1.Description;
                                    //oAPExport_Invoice_Details.AMTTOTTAX = Math.Round((((decimal)item.AmountBeforeGST) * ((decimal)item.TaxValue)) / 100, (int)2).ToString("#0.00");
                                    //oAPExport_Invoice_Details.AMTTOTTAX = Math.Round((decimal)item.AmountBeforeGST, (int)2).ToString("#0.00");

                                    oAPExport_Invoice_Details.TAXCLASS1 = item1.TaxClass.ToString();
                                    //oAPExport_Invoice_Details.RATETAX1 = "4";
                                    oAPExport_Invoice_Details.RATETAX1 = "0";
                                    oAPExport_Invoice_Details.BASETAX1 = item1.TaxClass.ToString() == "4.00" ? "0.00" : Math.Round((decimal)item1.Amount, (int)2).ToString("#0.00");
                                    oAPExport_Invoice_Details.AMTTOTTAX = item1.TaxClass.ToString() == "4.00" ? "0.00" : Math.Round((decimal)item1.GST, (int)2).ToString("#0.00");

                                    //if (item.TaxValue == 0)
                                    //{
                                    //    oAPExport_Invoice_Details.TAXCLASS1 = "4";
                                    //    //oAPExport_Invoice_Details.RATETAX1 = "4";
                                    //    oAPExport_Invoice_Details.RATETAX1 = "0";
                                    //    oAPExport_Invoice_Details.BASETAX1 = "0.00";
                                    //    oAPExport_Invoice_Details.AMTTOTTAX = Math.Round((decimal)item.AmountBeforeGST, (int)2).ToString("#0.00");
                                    //}
                                    //else
                                    //{

                                    //    oAPExport_Invoice_Details.BASETAX1 = Math.Round((decimal)item.AmountBeforeGST, (int)2).ToString("#0.00");
                                    //    oAPExport_Invoice_Details.TAXCLASS1 = Math.Round((decimal)item.TaxValue, (int)2).ToString("#,##0");
                                    //    oAPExport_Invoice_Details.RATETAX1 = Math.Round((decimal)item.TaxValue, (int)2).ToString("#,##0");
                                    //    oAPExport_Invoice_Details.AMTTOTTAX = Math.Round((((decimal)item.AmountBeforeGST) * ((decimal)item.TaxValue)) / 100, (int)2).ToString("#0.00");
                                    //}
                                    oAPExport_Invoice_Details.AMTTAX1 = item1.TaxClass.ToString() == "4.00" ? "0.00" : Math.Round((decimal)item1.GST, (int)2).ToString("#0.00");
                                    oAPExport_Invoice_Details.IDGLACCT = item1.AccountCode;
                                    //oAPExport_Invoice_Details.AMTDIST = item1.Amount.ToString();
                                    oAPExport_Invoice_Details.AMTDIST = Math.Round((decimal)item1.Amount, (int)2).ToString("#0.00");
                                    oAPExport_Invoice_Details.COMMENT = "";
                                    oAPExport_Invoice_Details.SWIBT = "0";
                                    oAPExport_Invoice_Details.IDINVC = item.VoucherNo;
                                    AllInvoiceDetails.Add(oAPExport_Invoice_Details);
                                }
                            }
                            tCID = item.CID;
                            TempData["Status"] = "Selected PV-Giro Claim Exported Successfully";
                        }
                        catch (Exception ex)
                        {

                        }
                    }
                }
                #endregion
            }
            else if (aPExportSearch.ModuleName == "HRPV-ChequeClaim")
            {
                var Po_Invoice = await _repository.MstMileageClaim.GetAllMileageClaimForAPExportAsync(aPExportSearch.ClaimIds,aPExportSearch.ModuleName, Int32.Parse(aPExportSearch.FacilityID),Int32.Parse(aPExportSearch.StatusID), aPExportSearch.FromDate, aPExportSearch.ToDate);
                int num1 = 0;
                long tCID = 0;

                #region Noraml AP export without Consolidated Invoices
                foreach (var item in Po_Invoice)
                {
                    if (tCID != item.CID)
                    {
                        var summary = await _repository.DtHRPVCClaimSummary.GetDtHRPVCClaimSummaryByIdAsync(item.CID);
                        var TaxClass = summary.Where(s => s.TaxClass == 4).FirstOrDefault();
                        num1 = num1 + 1;
                        //var id = await _repository.DtHRPVCClaim.GetDtHRPVCClaimByIdAsync(item.HRPVCCID);
                        //var id = objERPEntities.DtSupplierPOCs.ToList().Where(p => p.POCID == Convert.ToInt32(item.POCID)).ToList();
                        //if (id.Count != 0)
                        //    Description = id.FirstOrDefault().desc item.SupplierShortName + "/" + objERPEntities.DtSupplierPOCs.ToList().Where(p => p.POCID == Convert.ToInt32(item.POCID)).ToList().FirstOrDefault().Description;
                        //else
                        //    Description = "";
                        //Description = item.ParticularsOfPayment;
                        //int n = id.Count;
                        //if (id.Count != 0)
                        //{
                        //    if (id.Count == n && id.Count == 1)
                        //    {
                        //        Description = id.FirstOrDefault().Reason;
                        //        //Description = "Expense Claim " + "from " + id.FirstOrDefault().DateOfJourney.Day + " " + Helper.Month(id.FirstOrDefault().DateOfJourney.Month) + " to " + id.FirstOrDefault().DateOfJourney.Day + " " + Helper.Month(id.FirstOrDefault().DateOfJourney.Month) + " " + id.FirstOrDefault().DateOfJourney.Year.ToString();
                        //    }
                        //    else
                        //    {
                        //        Description = id.FirstOrDefault().Reason;
                        //        //Description = "Expense Claim " + "from " + id.FirstOrDefault().DateOfJourney.Day + " " + Helper.Month(id.FirstOrDefault().DateOfJourney.Month) + " to " + id.LastOrDefault().DateOfJourney.Day + " " + Helper.Month(id.LastOrDefault().DateOfJourney.Month) + " " + id.LastOrDefault().DateOfJourney.Year.ToString();
                        //    }
                        //}
                        int n = summary.Count;
                        if (summary.Count != 0)
                        {
                            if (summary.Count == n && summary.Count == 1)
                            {
                                Description = summary.FirstOrDefault().Description;
                                //Description = "Expense Claim " + "from " + id.FirstOrDefault().DateOfJourney.Day + " " + Helper.Month(id.FirstOrDefault().DateOfJourney.Month) + " to " + id.FirstOrDefault().DateOfJourney.Day + " " + Helper.Month(id.FirstOrDefault().DateOfJourney.Month) + " " + id.FirstOrDefault().DateOfJourney.Year.ToString();
                            }
                            else
                            {
                                Description = summary.FirstOrDefault().Description;
                                //Description = "Expense Claim " + "from " + id.FirstOrDefault().DateOfJourney.Day + " " + Helper.Month(id.FirstOrDefault().DateOfJourney.Month) + " to " + id.LastOrDefault().DateOfJourney.Day + " " + Helper.Month(id.LastOrDefault().DateOfJourney.Month) + " " + id.LastOrDefault().DateOfJourney.Year.ToString();
                            }
                        }

                        APExport_Invoice oAPExport_Invoice = new APExport_Invoice();
                        oAPExport_Invoice.CNTBTCH = "1";
                        oAPExport_Invoice.CNTITEM = num1.ToString();
                        oAPExport_Invoice.IDVEND = "DBS PV";
                        oAPExport_Invoice.IDINVC = item.VoucherNo;
                        oAPExport_Invoice.TEXTTRX = "1";
                        oAPExport_Invoice.ORDRNBR = "";
                        oAPExport_Invoice.PONBR = "";
                        oAPExport_Invoice.INVCDESC = Description;
                        oAPExport_Invoice.INVCAPPLTO = "";
                        oAPExport_Invoice.IDACCTSET = "1";
                        oAPExport_Invoice.DATEINVC = Convert.ToDateTime(DateTime.Now).ToString("dd/MM/yyyy HH:mm:ss", System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
                        oAPExport_Invoice.FISCYR = DateTime.Now.Year.ToString();
                        oAPExport_Invoice.FISCPER = DateTime.Now.Day >= Convert.ToInt32(financeStartDay) && DateTime.Now.Month != 12 ? DateTime.Now.AddMonths(1).Month.ToString() : DateTime.Now.Month.ToString();
                        oAPExport_Invoice.CODECURN = "SGD";
                        oAPExport_Invoice.TERMCODE = "1";
                        oAPExport_Invoice.DATEDUE = Convert.ToDateTime(DateTime.Now).ToString("dd/MM/yyyy HH:mm:ss", System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
                        oAPExport_Invoice.CODETAXGRP = "TXGSGD";

                        oAPExport_Invoice.TAXCLASS1 = (TaxClass != null ? (TaxClass.TaxClass == 4 ? "4" : TaxClass.TaxClass.ToString()) : "4");
                        oAPExport_Invoice.BASETAX1 = "0.00";
                        //if (item. == 0)
                        //{
                        //    oAPExport_Invoice.TAXCLASS1 = "4";
                        //    oAPExport_Invoice.BASETAX1 = "0.00";
                        //}
                        //else
                        //{

                        //    oAPExport_Invoice.TAXCLASS1 = Math.Round((decimal)item.TaxValue, (int)2).ToString("#,##0");
                        //    oAPExport_Invoice.BASETAX1 = Math.Round((decimal)item.AmountBeforeGST, (int)2).ToString("#0.00");
                        //}
                        //oAPExport_Invoice.AMTTAX1 = Math.Round((decimal)item.GSTAmount, (int)2).ToString("#0.00");
                        //oAPExport_Invoice.AMTTAXDIST = Math.Round((decimal)item.GSTAmount, (int)2).ToString("#0.00");
                        //oAPExport_Invoice.AMTINVCTOT = Math.Round((decimal)item.AmountBeforeGST, (int)2).ToString("#0.00");
                        //oAPExport_Invoice.AMTTOTDIST = Math.Round((decimal)item.AmountBeforeGST, (int)2).ToString("#0.00");
                        //oAPExport_Invoice.AMTGROSDST = Math.Round((decimal)item.TotalAmount, (int)2).ToString("#0.00");
                        //oAPExport_Invoice.AMTDUE = Math.Round((decimal)item.TotalAmount, (int)2).ToString("#0.00");
                        //oAPExport_Invoice.AMTTAXTOT = Math.Round((decimal)item.GSTAmount, (int)2).ToString("#0.00");
                        //oAPExport_Invoice.AMTGROSTOT = Math.Round((decimal)item.TotalAmount, (int)2).ToString("#0.00");

                        oAPExport_Invoice.AMTTAX1 = "0.00";
                        oAPExport_Invoice.AMTTAXDIST = "0.00";
                        oAPExport_Invoice.AMTINVCTOT = Math.Round((decimal)item.GrandTotal, (int)2).ToString("#0.00");
                        oAPExport_Invoice.AMTTOTDIST = Math.Round((decimal)item.GrandTotal, (int)2).ToString("#0.00");
                        oAPExport_Invoice.AMTGROSDST = Math.Round((decimal)item.GrandTotal, (int)2).ToString("#0.00");
                        oAPExport_Invoice.AMTDUE = Math.Round((decimal)item.GrandTotal, (int)2).ToString("#0.00");
                        oAPExport_Invoice.AMTTAXTOT = "0.00";
                        oAPExport_Invoice.AMTGROSTOT = Math.Round((decimal)item.GrandTotal, (int)2).ToString("#0.00");

                        AllInvoices.Add(oAPExport_Invoice);


                        APExport_Invoice_Payment_Schedules oAPExport_Invoice_Payment_Schedules = new APExport_Invoice_Payment_Schedules();
                        oAPExport_Invoice_Payment_Schedules.CNTBTCH = "1";
                        oAPExport_Invoice_Payment_Schedules.CNTITEM = num1.ToString();
                        oAPExport_Invoice_Payment_Schedules.CNTPAYM = "";
                        oAPExport_Invoice_Payment_Schedules.DATEDUE = Convert.ToDateTime(DateTime.Now).ToString("dd/MM/yyyy HH:mm:ss", System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
                        oAPExport_Invoice_Payment_Schedules.AMTDUE = Math.Round((decimal)item.GrandTotal, (int)2).ToString("#0.00");
                        AllInvoicesPayments.Add(oAPExport_Invoice_Payment_Schedules);

                        //below code is to change the status to Exported
                        if (aPExportSearch.StatusID == "3")
                            await _repository.MstHRPVCClaim.UpdateMstHRPVCClaimStatus(item.CID, 9, int.Parse(HttpContext.User.FindFirst("userid").Value), DateTime.Now, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, false, 0);
                        //var oSupplierPOC = objERPEntities.MstSupplierPOCs.ToList().Where(p => p.POCID == item.POCID).FirstOrDefault();
                        //if (oSupplierPOC != null)
                        //{
                        //    oSupplierPOC.ApprovalStatus = 5;
                        //    objERPEntities.SaveChanges();
                        //}
                        ////


                        //var Supplier_Invoice_Details = await _repository.DtHRPVCClaim.GetDtHRPVCClaimByIdAsync(item.CID);
                        int num = 0;
                        int num2 = 0;
                        try
                        {
                            foreach (var item1 in summary)
                            {
                                if (item1.ExpenseCategory != "DBS")
                                {
                                    num = num + 20;
                                    num2 = num2 + 1;
                                    //if (item.POCNO.Contains("RH") || item.POCNO.Contains("RL"))
                                    //{
                                    //    num2 = num2 + 21;
                                    //}
                                    APExport_Invoice_Details oAPExport_Invoice_Details = new APExport_Invoice_Details();
                                    oAPExport_Invoice_Details.CNTBTCH = "1";
                                    oAPExport_Invoice_Details.CNTITEM = num1.ToString();
                                    oAPExport_Invoice_Details.CNTLINE = num.ToString();
                                    oAPExport_Invoice_Details.IDDIST = "";
                                    oAPExport_Invoice_Details.TEXTDESC = item1.Description;
                                    //oAPExport_Invoice_Details.AMTTOTTAX = Math.Round((((decimal)item.AmountBeforeGST) * ((decimal)item.TaxValue)) / 100, (int)2).ToString("#0.00");
                                    //oAPExport_Invoice_Details.AMTTOTTAX = Math.Round((decimal)item.AmountBeforeGST, (int)2).ToString("#0.00");

                                    oAPExport_Invoice_Details.TAXCLASS1 = item1.TaxClass.ToString();
                                    //oAPExport_Invoice_Details.RATETAX1 = "4";
                                    oAPExport_Invoice_Details.RATETAX1 = "0";
                                    oAPExport_Invoice_Details.BASETAX1 = "0.00";
                                    oAPExport_Invoice_Details.AMTTOTTAX = "0";

                                    //if (item.TaxValue == 0)
                                    //{
                                    //    oAPExport_Invoice_Details.TAXCLASS1 = "4";
                                    //    //oAPExport_Invoice_Details.RATETAX1 = "4";
                                    //    oAPExport_Invoice_Details.RATETAX1 = "0";
                                    //    oAPExport_Invoice_Details.BASETAX1 = "0.00";
                                    //    oAPExport_Invoice_Details.AMTTOTTAX = Math.Round((decimal)item.AmountBeforeGST, (int)2).ToString("#0.00");
                                    //}
                                    //else
                                    //{

                                    //    oAPExport_Invoice_Details.BASETAX1 = Math.Round((decimal)item.AmountBeforeGST, (int)2).ToString("#0.00");
                                    //    oAPExport_Invoice_Details.TAXCLASS1 = Math.Round((decimal)item.TaxValue, (int)2).ToString("#,##0");
                                    //    oAPExport_Invoice_Details.RATETAX1 = Math.Round((decimal)item.TaxValue, (int)2).ToString("#,##0");
                                    //    oAPExport_Invoice_Details.AMTTOTTAX = Math.Round((((decimal)item.AmountBeforeGST) * ((decimal)item.TaxValue)) / 100, (int)2).ToString("#0.00");
                                    //}
                                    oAPExport_Invoice_Details.AMTTAX1 = "0";
                                    oAPExport_Invoice_Details.IDGLACCT = item1.AccountCode;
                                    //oAPExport_Invoice_Details.AMTDIST = item1.Amount.ToString();
                                    oAPExport_Invoice_Details.AMTDIST = Math.Round((decimal)item1.Amount, (int)2).ToString("#0.00");
                                    oAPExport_Invoice_Details.COMMENT = "";
                                    oAPExport_Invoice_Details.SWIBT = "0";
                                    oAPExport_Invoice_Details.IDINVC = item.VoucherNo;
                                    AllInvoiceDetails.Add(oAPExport_Invoice_Details);
                                }
                            }
                            tCID = item.CID;
                            TempData["Status"] = "Selected HR PV-Cheque Claim Exported Successfully";
                        }
                        catch (Exception ex)
                        {

                        }
                    }                 
                }
                #endregion
            }
            else if (aPExportSearch.ModuleName == "HRPV-GiroClaim")
            {
                var Po_Invoice = await _repository.MstMileageClaim.GetAllMileageClaimForAPExportAsync(aPExportSearch.ClaimIds,aPExportSearch.ModuleName, Int32.Parse(aPExportSearch.FacilityID),Int32.Parse(aPExportSearch.StatusID), aPExportSearch.FromDate, aPExportSearch.ToDate);
                int num1 = 0;
                long tCID = 0;
                #region Noraml AP export without Consolidated Invoices
                foreach (var item in Po_Invoice)
                {
                    if(tCID != item.CID)
                    {
                        num1 = num1 + 1;
                        var id = await _repository.DtHRPVGClaim.GetDtHRPVGClaimByIdAsync(item.CID);
                        var summary = await _repository.DtHRPVGClaimSummary.GetDtHRPVGClaimSummaryByIdAsync(item.CID);
                        var TaxClass = summary.Where(s => s.TaxClass == 4).FirstOrDefault();
                        //var id = objERPEntities.DtSupplierPOCs.ToList().Where(p => p.POCID == Convert.ToInt32(item.POCID)).ToList();
                        //if (id.Count != 0)
                        //    Description = id.FirstOrDefault().desc item.SupplierShortName + "/" + objERPEntities.DtSupplierPOCs.ToList().Where(p => p.POCID == Convert.ToInt32(item.POCID)).ToList().FirstOrDefault().Description;
                        //else
                        //    Description = "";
                        //Description = item.ParticularsOfPayment;
                        //int n = id.Count;
                        //if (id.Count != 0)
                        //{
                        //    if (id.Count == n && id.Count == 1)
                        //    {
                        //        Description = id.FirstOrDefault().Reason;
                        //        //Description = "Expense Claim " + "from " + id.FirstOrDefault().DateOfJourney.Day + " " + Helper.Month(id.FirstOrDefault().DateOfJourney.Month) + " to " + id.FirstOrDefault().DateOfJourney.Day + " " + Helper.Month(id.FirstOrDefault().DateOfJourney.Month) + " " + id.FirstOrDefault().DateOfJourney.Year.ToString();
                        //    }
                        //    else
                        //    {
                        //        Description = id.FirstOrDefault().Reason;
                        //        //Description = "Expense Claim " + "from " + id.FirstOrDefault().DateOfJourney.Day + " " + Helper.Month(id.FirstOrDefault().DateOfJourney.Month) + " to " + id.LastOrDefault().DateOfJourney.Day + " " + Helper.Month(id.LastOrDefault().DateOfJourney.Month) + " " + id.LastOrDefault().DateOfJourney.Year.ToString();
                        //    }
                        //}

                        int n = summary.Count;
                        if (summary.Count != 0)
                        {
                            if (summary.Count == n && summary.Count == 1)
                            {
                                Description = summary.FirstOrDefault().Description;
                                //Description = "Expense Claim " + "from " + id.FirstOrDefault().DateOfJourney.Day + " " + Helper.Month(id.FirstOrDefault().DateOfJourney.Month) + " to " + id.FirstOrDefault().DateOfJourney.Day + " " + Helper.Month(id.FirstOrDefault().DateOfJourney.Month) + " " + id.FirstOrDefault().DateOfJourney.Year.ToString();
                            }
                            else
                            {
                                Description = summary.FirstOrDefault().Description;
                                //Description = "Expense Claim " + "from " + id.FirstOrDefault().DateOfJourney.Day + " " + Helper.Month(id.FirstOrDefault().DateOfJourney.Month) + " to " + id.LastOrDefault().DateOfJourney.Day + " " + Helper.Month(id.LastOrDefault().DateOfJourney.Month) + " " + id.LastOrDefault().DateOfJourney.Year.ToString();
                            }
                        }


                        APExport_Invoice oAPExport_Invoice = new APExport_Invoice();
                        oAPExport_Invoice.CNTBTCH = "1";
                        oAPExport_Invoice.CNTITEM = num1.ToString();
                        oAPExport_Invoice.IDVEND = "DBS PV";
                        oAPExport_Invoice.IDINVC = item.VoucherNo;
                        oAPExport_Invoice.TEXTTRX = "1";
                        oAPExport_Invoice.ORDRNBR = "";
                        oAPExport_Invoice.PONBR = "";
                        oAPExport_Invoice.INVCDESC = Description;
                        oAPExport_Invoice.INVCAPPLTO = "";
                        oAPExport_Invoice.IDACCTSET = "1";
                        oAPExport_Invoice.DATEINVC = Convert.ToDateTime(DateTime.Now).ToString("dd/MM/yyyy HH:mm:ss", System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
                        oAPExport_Invoice.FISCYR = DateTime.Now.Year.ToString();
                        oAPExport_Invoice.FISCPER = DateTime.Now.Day >= Convert.ToInt32(financeStartDay) && DateTime.Now.Month != 12 ? DateTime.Now.AddMonths(1).Month.ToString() : DateTime.Now.Month.ToString();
                        oAPExport_Invoice.CODECURN = "SGD";
                        oAPExport_Invoice.TERMCODE = "1";
                        oAPExport_Invoice.DATEDUE = Convert.ToDateTime(DateTime.Now).ToString("dd/MM/yyyy HH:mm:ss", System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
                        oAPExport_Invoice.CODETAXGRP = "TXGSGD";

                        oAPExport_Invoice.TAXCLASS1 = (TaxClass != null ? (TaxClass.TaxClass == 4 ? "4" : TaxClass.TaxClass.ToString()) : "4");
                        oAPExport_Invoice.BASETAX1 = "0.00";
                        //if (item. == 0)
                        //{
                        //    oAPExport_Invoice.TAXCLASS1 = "4";
                        //    oAPExport_Invoice.BASETAX1 = "0.00";
                        //}
                        //else
                        //{

                        //    oAPExport_Invoice.TAXCLASS1 = Math.Round((decimal)item.TaxValue, (int)2).ToString("#,##0");
                        //    oAPExport_Invoice.BASETAX1 = Math.Round((decimal)item.AmountBeforeGST, (int)2).ToString("#0.00");
                        //}
                        //oAPExport_Invoice.AMTTAX1 = Math.Round((decimal)item.GSTAmount, (int)2).ToString("#0.00");
                        //oAPExport_Invoice.AMTTAXDIST = Math.Round((decimal)item.GSTAmount, (int)2).ToString("#0.00");
                        //oAPExport_Invoice.AMTINVCTOT = Math.Round((decimal)item.AmountBeforeGST, (int)2).ToString("#0.00");
                        //oAPExport_Invoice.AMTTOTDIST = Math.Round((decimal)item.AmountBeforeGST, (int)2).ToString("#0.00");
                        //oAPExport_Invoice.AMTGROSDST = Math.Round((decimal)item.TotalAmount, (int)2).ToString("#0.00");
                        //oAPExport_Invoice.AMTDUE = Math.Round((decimal)item.TotalAmount, (int)2).ToString("#0.00");
                        //oAPExport_Invoice.AMTTAXTOT = Math.Round((decimal)item.GSTAmount, (int)2).ToString("#0.00");
                        //oAPExport_Invoice.AMTGROSTOT = Math.Round((decimal)item.TotalAmount, (int)2).ToString("#0.00");

                        oAPExport_Invoice.AMTTAX1 = "0.00";
                        oAPExport_Invoice.AMTTAXDIST = "0.00";
                        oAPExport_Invoice.AMTINVCTOT = Math.Round((decimal)item.GrandTotal, (int)2).ToString("#0.00");
                        oAPExport_Invoice.AMTTOTDIST = Math.Round((decimal)item.GrandTotal, (int)2).ToString("#0.00");
                        oAPExport_Invoice.AMTGROSDST = Math.Round((decimal)item.GrandTotal, (int)2).ToString("#0.00");
                        oAPExport_Invoice.AMTDUE = Math.Round((decimal)item.GrandTotal, (int)2).ToString("#0.00");
                        oAPExport_Invoice.AMTTAXTOT = "0.00";
                        oAPExport_Invoice.AMTGROSTOT = Math.Round((decimal)item.GrandTotal, (int)2).ToString("#0.00");

                        AllInvoices.Add(oAPExport_Invoice);


                        APExport_Invoice_Payment_Schedules oAPExport_Invoice_Payment_Schedules = new APExport_Invoice_Payment_Schedules();
                        oAPExport_Invoice_Payment_Schedules.CNTBTCH = "1";
                        oAPExport_Invoice_Payment_Schedules.CNTITEM = num1.ToString();
                        oAPExport_Invoice_Payment_Schedules.CNTPAYM = "";
                        oAPExport_Invoice_Payment_Schedules.DATEDUE = Convert.ToDateTime(DateTime.Now).ToString("dd/MM/yyyy HH:mm:ss", System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
                        oAPExport_Invoice_Payment_Schedules.AMTDUE = Math.Round((decimal)item.GrandTotal, (int)2).ToString("#0.00");
                        AllInvoicesPayments.Add(oAPExport_Invoice_Payment_Schedules);

                        //below code is to change the status to Exported
                        if (aPExportSearch.StatusID == "3")
                            await _repository.MstHRPVGClaim.UpdateMstHRPVGClaimStatus(item.CID, 9, int.Parse(HttpContext.User.FindFirst("userid").Value), DateTime.Now, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, false, 0);
                        //var oSupplierPOC = objERPEntities.MstSupplierPOCs.ToList().Where(p => p.POCID == item.POCID).FirstOrDefault();
                        //if (oSupplierPOC != null)
                        //{
                        //    oSupplierPOC.ApprovalStatus = 5;
                        //    objERPEntities.SaveChanges();
                        //}
                        ////


                        //var Supplier_Invoice_Details = await _repository.DtHRPVGClaim.GetDtHRPVGClaimByIdAsync(item.CID);
                        int num = 0;
                        int num2 = 0;
                        try
                        {
                            foreach (var item1 in summary)
                            {
                                if (item1.ExpenseCategory != "DBS")
                                {
                                    num = num + 20;
                                    num2 = num2 + 1;
                                    //if (item.POCNO.Contains("RH") || item.POCNO.Contains("RL"))
                                    //{
                                    //    num2 = num2 + 21;
                                    //}
                                    APExport_Invoice_Details oAPExport_Invoice_Details = new APExport_Invoice_Details();
                                    oAPExport_Invoice_Details.CNTBTCH = "1";
                                    oAPExport_Invoice_Details.CNTITEM = num1.ToString();
                                    oAPExport_Invoice_Details.CNTLINE = num.ToString();
                                    oAPExport_Invoice_Details.IDDIST = "";
                                    oAPExport_Invoice_Details.TEXTDESC = item1.Description;
                                    //oAPExport_Invoice_Details.AMTTOTTAX = Math.Round((((decimal)item.AmountBeforeGST) * ((decimal)item.TaxValue)) / 100, (int)2).ToString("#0.00");
                                    //oAPExport_Invoice_Details.AMTTOTTAX = Math.Round((decimal)item.AmountBeforeGST, (int)2).ToString("#0.00");

                                    oAPExport_Invoice_Details.TAXCLASS1 = item1.TaxClass.ToString();
                                    //oAPExport_Invoice_Details.RATETAX1 = "4";
                                    oAPExport_Invoice_Details.RATETAX1 = "0";
                                    oAPExport_Invoice_Details.BASETAX1 = "0.00";
                                    oAPExport_Invoice_Details.AMTTOTTAX = "0";

                                    //if (item.TaxValue == 0)
                                    //{
                                    //    oAPExport_Invoice_Details.TAXCLASS1 = "4";
                                    //    //oAPExport_Invoice_Details.RATETAX1 = "4";
                                    //    oAPExport_Invoice_Details.RATETAX1 = "0";
                                    //    oAPExport_Invoice_Details.BASETAX1 = "0.00";
                                    //    oAPExport_Invoice_Details.AMTTOTTAX = Math.Round((decimal)item.AmountBeforeGST, (int)2).ToString("#0.00");
                                    //}
                                    //else
                                    //{

                                    //    oAPExport_Invoice_Details.BASETAX1 = Math.Round((decimal)item.AmountBeforeGST, (int)2).ToString("#0.00");
                                    //    oAPExport_Invoice_Details.TAXCLASS1 = Math.Round((decimal)item.TaxValue, (int)2).ToString("#,##0");
                                    //    oAPExport_Invoice_Details.RATETAX1 = Math.Round((decimal)item.TaxValue, (int)2).ToString("#,##0");
                                    //    oAPExport_Invoice_Details.AMTTOTTAX = Math.Round((((decimal)item.AmountBeforeGST) * ((decimal)item.TaxValue)) / 100, (int)2).ToString("#0.00");
                                    //}
                                    oAPExport_Invoice_Details.AMTTAX1 = "0";
                                    oAPExport_Invoice_Details.IDGLACCT = item1.AccountCode;
                                    //oAPExport_Invoice_Details.AMTDIST = item1.Amount.ToString();
                                    oAPExport_Invoice_Details.AMTDIST = Math.Round((decimal)item1.Amount, (int)2).ToString("#0.00");
                                    oAPExport_Invoice_Details.COMMENT = "";
                                    oAPExport_Invoice_Details.SWIBT = "0";
                                    oAPExport_Invoice_Details.IDINVC = item.VoucherNo;
                                    AllInvoiceDetails.Add(oAPExport_Invoice_Details);
                                }
                            }
                            tCID = item.CID;
                            TempData["Status"] = "Selected HR PV-Cheque Claim Exported Successfully";
                        }
                        catch (Exception ex)
                        {

                        }
                    }                  
                }
                #endregion
            }
            try
            {
                DataTable dt_Invoices = converter.ToDataTable(AllInvoices);
                DataTable dt_Invoices_Details = converter.ToDataTable(AllInvoiceDetails);
                DataTable dt_Invoices_Payments = converter.ToDataTable(AllInvoicesPayments);
                DataTable dt_Invoices_OptionalFields = converter.ToDataTable(AllInvoiceOptionalFields);
                DataTable dt_Invoices_Details_OptionalFields = converter.ToDataTable(AllInvoiceDetailsOptionalFields);
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
                        Sheet sheet = new Sheet() { Id = workbookPart.GetIdOfPart(worksheetPart), SheetId = 1, Name = "Invoices" };
                       
                        sheets.Append(sheet);
                        
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
                                if (col == "TAXCLASS1" || col == "BASETAX1" || col == "AMTTAX1" || col == "AMTTAXDIST" || col == "AMTINVCTOT" || col == "AMTTOTDIST" || col == "AMTGROSDST" || col == "AMTDUE" || col == "AMTTAXTOT" || col == "AMTGROSTOT")
                                {
                                    Cell cell = new Cell();
                                    cell.DataType = CellValues.Number;
                                    cell.CellValue = new CellValue(dsrow[col].ToString());
                                    //cell.StyleIndex = 3;
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
                        

                        WorksheetPart worksheetPart2 = workbookPart.AddNewPart<WorksheetPart>();
                        var sheetData2 = new SheetData();
                        worksheetPart2.Worksheet = new Worksheet(sheetData2);

                        Sheet sheet2 = new Sheet() { Id = workbookPart.GetIdOfPart(worksheetPart2), SheetId = 2, Name = "Invoice_Details" };
                        sheets.Append(sheet2);

                        Row headerRow2 = new Row();

                        List<String> columns2 = new List<string>();
                        foreach (System.Data.DataColumn column in dt_Invoices_Details.Columns)
                        {
                            columns2.Add(column.ColumnName);

                            Cell cell = new Cell();
                            cell.DataType = CellValues.String;
                            cell.CellValue = new CellValue(column.ColumnName);
                            headerRow2.AppendChild(cell);
                        }

                        sheetData2.AppendChild(headerRow2);

                        foreach (DataRow dsrow in dt_Invoices_Details.Rows)
                        {
                            Row newRow = new Row();
                            foreach (String col in columns2)
                            {
                                if (col == "AMTTOTTAX" || col == "BASETAX1" || col == "TAXCLASS1" || col == "RATETAX1" || col == "AMTTAX1")
                                {
                                    Cell cell = new Cell();
                                    cell.DataType = CellValues.Number;
                                    //cell.StyleIndex = 3;
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

                            sheetData2.AppendChild(newRow);
                        }

                        WorksheetPart worksheetPart3 = workbookPart.AddNewPart<WorksheetPart>();
                        var sheetData3 = new SheetData();
                        worksheetPart3.Worksheet = new Worksheet(sheetData3);

                        Sheet sheet3 = new Sheet() { Id = workbookPart.GetIdOfPart(worksheetPart3), SheetId = 3, Name = "Invoice_Payment_Schedules" };
                        sheets.Append(sheet3);

                        Row headerRow3 = new Row();

                        List<String> columns3 = new List<string>();
                        foreach (System.Data.DataColumn column in dt_Invoices_Payments.Columns)
                        {
                            columns3.Add(column.ColumnName);

                            Cell cell = new Cell();
                            cell.DataType = CellValues.String;
                            cell.CellValue = new CellValue(column.ColumnName);
                            headerRow3.AppendChild(cell);
                        }

                        sheetData3.AppendChild(headerRow3);

                        foreach (DataRow dsrow in dt_Invoices_Payments.Rows)
                        {
                            Row newRow = new Row();
                            foreach (String col in columns3)
                            {
                                if (col == "AMTDUE")
                                {
                                    Cell cell = new Cell();
                                    cell.DataType = CellValues.Number;
                                    //cell.StyleIndex = 3;
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

                            sheetData3.AppendChild(newRow);
                        }

                        WorksheetPart worksheetPart4 = workbookPart.AddNewPart<WorksheetPart>();
                        var sheetData4 = new SheetData();
                        worksheetPart4.Worksheet = new Worksheet(sheetData4);

                        Sheet sheet4 = new Sheet() { Id = workbookPart.GetIdOfPart(worksheetPart4), SheetId = 4, Name = "Invoice_Optional_Fields" };
                        sheets.Append(sheet4);

                        Row headerRow4 = new Row();

                        List<String> columns4 = new List<string>();
                        foreach (System.Data.DataColumn column in dt_Invoices_OptionalFields.Columns)
                        {
                            columns4.Add(column.ColumnName);

                            Cell cell = new Cell();
                            cell.DataType = CellValues.String;
                            cell.CellValue = new CellValue(column.ColumnName);
                            headerRow4.AppendChild(cell);
                        }

                        sheetData4.AppendChild(headerRow4);

                        WorksheetPart worksheetPart5 = workbookPart.AddNewPart<WorksheetPart>();
                        var sheetData5 = new SheetData();
                        worksheetPart5.Worksheet = new Worksheet(sheetData5);
                        
                        Sheet sheet5 = new Sheet() { Id = workbookPart.GetIdOfPart(worksheetPart5), SheetId = 5, Name = "Invoice_Detail_Optional_Fields" };
                        sheets.Append(sheet5);

                        Row headerRow5 = new Row();

                        List<String> columns5 = new List<string>();
                        foreach (System.Data.DataColumn column in dt_Invoices_Details_OptionalFields.Columns)
                        {
                            columns5.Add(column.ColumnName);

                            Cell cell = new Cell();
                            cell.DataType = CellValues.String;
                            cell.CellValue = new CellValue(column.ColumnName);
                            headerRow5.AppendChild(cell);
                        }

                        string strRange = "Invoices!$A$1:$AA$" + (dt_Invoices.Rows.Count + 1);
                        string strRangeDetails = "Invoice_Details!$A$1:$O$" + (dt_Invoices_Details.Rows.Count + 1);
                        string strRangeSchedules = "Invoice_Payment_Schedules!$A$1:$E$" + (dt_Invoices_Payments.Rows.Count + 1);
                        string strRangeOptionalFields = "Invoice_Optional_Fields!$A$1:$S$" + (dt_Invoices_OptionalFields.Rows.Count + 1);
                        string strRangeOptionalFieldsDetails = "Invoice_Detail_Optional_Fields!$A$1:$T$" + (dt_Invoices_Details_OptionalFields.Rows.Count + 1);
                        
                        DefinedName definedName1 = new DefinedName { Name = "Invoices", Text = strRange };
                        DefinedName definedName1Details = new DefinedName { Name = "Invoice_Details", Text = strRangeDetails };
                        DefinedName definedName1Schedules = new DefinedName { Name = "Invoice_Payment_Schedules", Text = strRangeSchedules };
                        DefinedName definedName1OptionalFields = new DefinedName { Name = "Invoice_Optional_Fields", Text = strRangeOptionalFields };
                        DefinedName definedName1OptionalFieldsDetails = new DefinedName { Name = "Invoice_Detail_Optional_Fields", Text = strRangeOptionalFieldsDetails };
                        //DefinedName definedName2 = new DefinedName { Name = "RowRange", Text = "Sheet1!$A$1:$A$15" };
                        if (workbookPart.Workbook.DefinedNames == null)
                        {
                            DefinedNames definedNames1 = new DefinedNames();
                            workbookPart.Workbook.Append(definedNames1);
                        }

                        workbookPart.Workbook.DefinedNames.Append(definedName1);
                        workbookPart.Workbook.DefinedNames.Append(definedName1Details);
                        workbookPart.Workbook.DefinedNames.Append(definedName1Schedules);
                        workbookPart.Workbook.DefinedNames.Append(definedName1OptionalFields);
                        workbookPart.Workbook.DefinedNames.Append(definedName1OptionalFieldsDetails);

                        sheetData5.AppendChild(headerRow5);
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
                return null;// RedirectToAction("Index", "FinanceReports");
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
                        return File(blobStream, file.Properties.ContentType, "ExportClaimsAccPac.xlsx");
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
            //return File(fileByteArray, "application/vnd.ms-excel", "FinanceHRPVCClaimsReport.xlsx");
        }
     }
}
