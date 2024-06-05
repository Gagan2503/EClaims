using AutoMapper;
using EClaimsEntities;
using EClaimsEntities.Models;
using EClaimsRepository.Contracts;
using EClaimsWeb.Helpers;
using EClaimsWeb.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using NToastNotify;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using static System.Net.WebRequestMethods;

namespace EClaimsWeb.Controllers
{
    [Authorize(Roles = "Admin,Finance,User,HR")]
    public class UserSettingsController : Controller
    {
        private byte _userApproversCount = 3;
        private ILoggerManager _logger;
        private IRepositoryWrapper _repository;
        private IMapper _mapper;
        private IWebHostEnvironment _webHostEnvironment;
        private RepositoryContext _repositoryContext;
        private readonly IToastNotification _toastNotification;

        public UserSettingsController(ILoggerManager logger, IRepositoryWrapper repository,
            IMapper mapper, IWebHostEnvironment webHostEnvironment, RepositoryContext repositoryContext,
            IToastNotification toastNotification)
        {
            _logger = logger;
            _repository = repository;
            _mapper = mapper;
            _webHostEnvironment = webHostEnvironment;
            _repositoryContext = repositoryContext;
            _toastNotification = toastNotification;
        }

        public async Task<IActionResult> Index(string userId)
        {
            try
            {
                int userid = 0;
                if (!string.IsNullOrEmpty(userId))
                {
                    userid = int.Parse(userId);
                    ViewBag.IsEditForLoggedInUser = "0";
                }
                else
                {
                    userid = 2;
                    ViewBag.IsEditForLoggedInUser = "1";
                }

                var currentUser = User.Claims;
                UserSettingsViewModel userSettingsViewModel = new UserSettingsViewModel();
                string sUserId = User.FindFirstValue("userid"); // will give the user's userId

                bool isValidUserid = !string.IsNullOrEmpty(sUserId) ? int.TryParse(sUserId, out userid) : false;

                //var userId = User.FindFirstValue(ClaimTypes.NameIdentifier); // will give the user's userId
                //var userName = User.FindFirstValue(ClaimTypes.Name); // will give the user's userName
                //var userDetails = _repository.MstUser.GetByUserName(userName);
                //IEnumerable<MstUser> approversList;
                //if (User.IsInRole("HR"))
                //{
                //    var mstRole = await _repository.MstRole.GetRoleByNameAsync("hr");
                //    approversList = _repository.DtUserRoles.GetAllHRHODUsersByRoleIdAsync(mstRole.RoleID);
                //}
                //else
                //{
                //    var mstRole = await _repository.MstRole.GetRoleByNameAsync("user");
                //    approversList = _repository.DtUserRoles.GetAllHRHODUsersByRoleIdAsync(mstRole.RoleID);
                //}
                var bankList = await _repository.MstBankSwiftBIC.GetAllBankSwiftBICAsync();
                var approversList = await _repository.MstUser.GetAllUsersAsync();
                //approversList = approversList.OrderBy(u => u.Name).ToList();
                //var facilitiesList = await _repository.MstFacility.GetAllFacilityAsync("active");
                var facilitiesList = await _repository.DtUserFacilities.GetAllFacilitiesByUserIdAsync(Convert.ToInt32(sUserId));
                if (userid == 0)
                {
                    userSettingsViewModel.BankDetials = new BankDetailsViewModel();
                    userSettingsViewModel.UserDetails = new UserViewModel();
                    userSettingsViewModel.ApproverDetails = ReconcileUserApprovers(new List<UserApproversViewModel>());
                }
                else
                {
                    var userDetails = _repository.MstUser.GetUserByID(userid);
                    userDetails.FacilityID = Convert.ToInt32(User.Claims.FirstOrDefault(c => c.Type == "facilityid").Value);

                    var bankDetails = await _repository.MstBankDetails.GetBankDetailsByUserIdAsync(userid);
                    //var approverDetails = await _repository.MstUserApprovers.GetUserApproversByUserIdAsync(userid);
                    var approverDetails = await _repository.MstUserApprovers.GetUserApproversByUserIdFacilityIdAsync(userid, userDetails.FacilityID);
                    var alternateApprover = await _repository.MstAlternateApprover.GetAlternateApproverByUserIdAsync(userid);
                    var delegateUsers = await _repository.MstDelegateUsers.GetDelegateUserByUserIdAsync(userid);

                    BankDetailsViewModel bankDetailsViewModel = new BankDetailsViewModel();

                    if (bankDetails != null)
                    {
                        bankDetailsViewModel.AccountNumber = Aes256CbcEncrypter.Decrypt(bankDetails.AccountNumber);
                        bankDetailsViewModel.NameAsInBank = Aes256CbcEncrypter.Decrypt(bankDetails.NameAsInBank);
                        bankDetailsViewModel.NameAsInBank = Aes256CbcEncrypter.Decrypt(bankDetails.NameAsInBank);
                        bankDetailsViewModel.BankName = Aes256CbcEncrypter.Decrypt(bankDetails.BankName);
                        bankDetailsViewModel.BankCode = Aes256CbcEncrypter.Decrypt(bankDetails.BankCode);
                        bankDetailsViewModel.BankSWIFTBIC = Aes256CbcEncrypter.Decrypt(bankDetails.BankSwiftBIC);
                        bankDetailsViewModel.Branch = Aes256CbcEncrypter.Decrypt(bankDetails.Branch);
                        bankDetailsViewModel.BranchCode = Aes256CbcEncrypter.Decrypt(bankDetails.BranchCode);
                        bankDetailsViewModel.PayNow = Aes256CbcEncrypter.Decrypt(bankDetails.PayNow);
                        bankDetailsViewModel.BankStatementFileName = Aes256CbcEncrypter.Decrypt(bankDetails.BankStatementFileName);
                        bankDetailsViewModel.BankStatementUrl = bankDetails.BankStatementUrl;
                        userSettingsViewModel.UserConfirmation = true;
                    }

                    if (bankDetailsViewModel != null)
                        bankDetailsViewModel.ConfirmAccountnumber = bankDetailsViewModel.AccountNumber;
                    else
                        bankDetailsViewModel = new BankDetailsViewModel();
                    userSettingsViewModel.BankDetials = _mapper.Map<BankDetailsViewModel>(bankDetailsViewModel);
                    userSettingsViewModel.UserDetails = _mapper.Map<UserViewModel>(userDetails);
                    userSettingsViewModel.ApproverDetails = ReconcileUserApprovers(_mapper.Map<List<UserApproversViewModel>>(approverDetails));

                    if (alternateApprover != null)
                    {
                        bool isAppEabled = false;
                        DateTime fromDate = alternateApprover.FromDate;
                        DateTime toDate = alternateApprover.ToDate;
                        DateTime fromTime = alternateApprover.FromTime;
                        DateTime toTime = alternateApprover.ToTime;

                        DateTime startDate = fromDate.Date.Add(fromTime.TimeOfDay);
                        DateTime endDate = toDate.Date.Add(toTime.TimeOfDay);
                        long alternateUserId = alternateApprover.AlternateUser;
                        string alternateUserName = "";

                        DateTime currentDate = DateTime.Now;

                        if (alternateApprover.IsActive && currentDate.Ticks > startDate.Ticks && currentDate.Ticks < endDate.Ticks)
                        {
                            var altUserDetails = _repository.MstUser.GetUserByID(alternateUserId);
                            if (altUserDetails != null)
                            {
                                alternateUserName = altUserDetails.Name;
                            }
                            isAppEabled = true;
                        }

                        userSettingsViewModel.AlternateApprover = new AlternateApprover()
                        {
                            UserID = Convert.ToInt32(alternateApprover.AlternateUser),
                            FromDate = Convert.ToDateTime(alternateApprover.FromDate).ToString("dd/MM/yyyy", System.Globalization.CultureInfo.CreateSpecificCulture("en-US")),
                            ToDate = Convert.ToDateTime(alternateApprover.ToDate).ToString("dd/MM/yyyy", System.Globalization.CultureInfo.CreateSpecificCulture("en-US")),
                            FromTime = alternateApprover.FromTime.ToString("hh:mm tt"),
                            ToTime = alternateApprover.ToTime.ToString("hh:mm tt"),
                            IsEnabled = isAppEabled,
                            AlternateUserName = alternateUserName
                        };

                        userSettingsViewModel.UserConfirmation = true;
                    }
                    else
                    {
                        userSettingsViewModel.AlternateApprover = new AlternateApprover();
                    }

                    if (delegateUsers != null)
                    {
                        bool isAppEabled = false;
                        DateTime fromDate = delegateUsers.FromDate;
                        DateTime toDate = delegateUsers.ToDate;
                        DateTime fromTime = delegateUsers.FromTime;
                        DateTime toTime = delegateUsers.ToTime;

                        DateTime startDate = fromDate.Date.Add(fromTime.TimeOfDay);
                        DateTime endDate = toDate.Date.Add(toTime.TimeOfDay);
                        long delegateUserId = delegateUsers.DelegateUser;
                        string delegateUserName = "";

                        DateTime currentDate = DateTime.Now;

                        if (delegateUsers.IsActive && currentDate.Ticks > startDate.Ticks && currentDate.Ticks < endDate.Ticks)
                        {
                            var delUserDetails = _repository.MstUser.GetUserByID(delegateUserId);
                            if (delUserDetails != null)
                            {
                                delegateUserName = delUserDetails.Name;
                            }
                            isAppEabled = true;
                        }

                        userSettingsViewModel.DelegateUser = new DelegateUser()
                        {
                            UserID = Convert.ToInt32(delegateUsers.DelegateUser),
                            FromDate = Convert.ToDateTime(delegateUsers.FromDate).ToString("dd/MM/yyyy", System.Globalization.CultureInfo.CreateSpecificCulture("en-US")),
                            ToDate = Convert.ToDateTime(delegateUsers.ToDate).ToString("dd/MM/yyyy", System.Globalization.CultureInfo.CreateSpecificCulture("en-US")),
                            FromTime = delegateUsers.FromTime.ToString("hh:mm tt"),
                            ToTime = delegateUsers.ToTime.ToString("hh:mm tt"),
                            IsEnabled = isAppEabled,
                            DelegateUserName = delegateUserName
                        };

                        userSettingsViewModel.UserConfirmation = true;
                    }
                    else
                    {
                        userSettingsViewModel.DelegateUser = new DelegateUser();
                    }
                }

                userSettingsViewModel.Facilities = new SelectList(facilitiesList.ToList(), "FacilityID", "FacilityName");
                userSettingsViewModel.Approvers = new SelectList(approversList, "UserID", "Name");
                userSettingsViewModel.BankSwiftBICs = new SelectList(bankList, "BankCode", "BankName");

                //await Task.WhenAll(facilitiesList, bankDetails);
                _logger.LogInfo($"Returned all facilities from database.");
                return View(userSettingsViewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Something went wrong inside get First time user settings index action: {ex.Message}");
                _toastNotification.AddErrorToastMessage(ex.Message);
                return View();
            }
        }

        [HttpPost]
        public async Task<IActionResult> Index(UserSettingsViewModel userSettings)
        {
            try
            {
                if (userSettings == null)
                {
                    _logger.LogError("First Time UserSettings object sent from client is null.");
                    _toastNotification.AddErrorToastMessage("User object is null");
                    return BadRequest("User object is null");
                }
                var bankList = await _repository.MstBankSwiftBIC.GetAllBankSwiftBICAsync();
                var approversList = _repository.MstUser.GetUserApprovers();
                var facilitiesList = await _repository.MstFacility.GetAllFacilityAsync("active");
                userSettings.Facilities = new SelectList(facilitiesList.ToList(), "FacilityID", "FacilityName");
                userSettings.Approvers = new SelectList(approversList.ToList(), "UserID", "Name");

                if (!ModelState.IsValid)
                {
                    string modelErrors = Helper.GetModelValidationErrors(ModelState);
                    _toastNotification.AddErrorToastMessage("Invalid data. Error = " + modelErrors);
                    _logger.LogError("Invalid user settings while add. Error = " + modelErrors);
                    return View(userSettings);
                }

                // Validate Alternate approver or verifier section
                if (userSettings.AlternateApprover.UserID > 0)
                {
                    // Check From date is present
                    if (string.IsNullOrEmpty(userSettings.AlternateApprover.FromDate))
                    {
                        _toastNotification.AddErrorToastMessage("Alternate approver or verifier user is slected. Hence From date is required. Else deselect the user.");
                        return View(userSettings);
                    }

                    if (string.IsNullOrEmpty(userSettings.AlternateApprover.ToDate))
                    {
                        _toastNotification.AddErrorToastMessage("Alternate approver or verifier user is slected. Hence To date is required. Else deselect the user.");
                        return View(userSettings);
                    }

                    if (string.IsNullOrEmpty(userSettings.AlternateApprover.FromTime))
                    {
                        _toastNotification.AddErrorToastMessage("Alternate approver or verifier user is slected. Hence From Time is required. Else deselect the user.");
                        return View(userSettings);
                    }

                    if (string.IsNullOrEmpty(userSettings.AlternateApprover.FromTime))
                    {
                        _toastNotification.AddErrorToastMessage("Alternate approver or verifier user is slected. Hence From Time is required. Else deselect the user.");
                        return View(userSettings);
                    }

                    if (string.IsNullOrEmpty(userSettings.AlternateApprover.ToTime))
                    {
                        _toastNotification.AddErrorToastMessage("Alternate approver or verifier user is slected. Hence To Time is required. Else deselect the user.");
                        return View(userSettings);
                    }
                }

                // Validate Delegate user section
                if (userSettings.DelegateUser != null)
                {
                    if (userSettings.DelegateUser.UserID > 0)
                    {
                        // Check From date is present
                        if (string.IsNullOrEmpty(userSettings.DelegateUser.FromDate))
                        {
                            _toastNotification.AddErrorToastMessage("Delegate user is slected. Hence From date is required. Else deselect the user.");
                            return View(userSettings);
                        }

                        if (string.IsNullOrEmpty(userSettings.DelegateUser.ToDate))
                        {
                            _toastNotification.AddErrorToastMessage("Delegate user is slected. Hence To date is required. Else deselect the user.");
                            return View(userSettings);
                        }

                        if (string.IsNullOrEmpty(userSettings.DelegateUser.FromTime))
                        {
                            _toastNotification.AddErrorToastMessage("Delegate user is slected. Hence From Time is required. Else deselect the user.");
                            return View(userSettings);
                        }

                        if (string.IsNullOrEmpty(userSettings.DelegateUser.ToTime))
                        {
                            _toastNotification.AddErrorToastMessage("Delegate user is slected. Hence To Time is required. Else deselect the user.");
                            return View(userSettings);
                        }
                    }
                }


                if (userSettings.BankDetials != null)
                {
                    if (userSettings.BankDetials.AccountNumber != userSettings.BankDetials.ConfirmAccountnumber)
                    {
                        _toastNotification.AddErrorToastMessage("Account number doesnot match");
                        return View(userSettings);
                    }

                    if (userSettings.BankDetials.BankStatement != null)
                    {
                        string folder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads");
                        string fileName = Guid.NewGuid().ToString() + "_" + userSettings.BankDetials.BankStatement.FileName;
                        string filePath = Path.Combine(folder, fileName);

                        if (userSettings.BankDetials.BankStatement.Length > 0)
                        {
                            using (var ms = new MemoryStream())
                            {
                                userSettings.BankDetials.BankStatement.CopyTo(ms);
                                var fileBytes = ms.ToArray();
                                Aes256CbcEncrypter.EncryptFile(fileBytes, filePath);
                            }
                        }
                        userSettings.BankDetials.BankStatementUrl = filePath;
                    }
                }

                var mstExistingUserDetails = _repository.MstUser.GetUserByID(userSettings.UserDetails.UserID);
                var mstExistingBankDetails = await _repository.MstBankDetails.GetBankDetailsByUserIdAsync(userSettings.UserDetails.UserID);
                var mstAlternateApproverDetails = await _repository.MstAlternateApprover.GetAlternateApproverByUserIdAsync(userSettings.UserDetails.UserID);
                var mstDelegateUsersDetails = await _repository.MstDelegateUsers.GetDelegateUserByUserIdAsync(userSettings.UserDetails.UserID);

                var mstUserApproverDetails = _mapper.Map<List<MstUserApprovers>>(userSettings.ApproverDetails);

                using (var transaction = _repositoryContext.Database.BeginTransaction())
                {
                    try
                    {
                        mstExistingUserDetails.FacilityID = userSettings.UserDetails.FacilityID;
                        mstExistingUserDetails.Name = userSettings.UserDetails.Name;
                        mstExistingUserDetails.Phone = userSettings.UserDetails.Phone;
                        //mstExistingUserDetails.Password = userSettings.UserDetails.Password;
                        mstExistingUserDetails.CreatorUserId = 1;
                        mstExistingUserDetails.LastModifierUserId = 1;

                        _repository.MstUser.Update(mstExistingUserDetails);
                        await _repository.SaveAsync();


                        if (mstExistingBankDetails != null)
                        {
                            var mstBankDetails = await _repository.MstBankDetails.GetBankDetailsByUserIdAsync(userSettings.UserDetails.UserID);
                            mstBankDetails.AccountNumber = Aes256CbcEncrypter.Encrypt(userSettings.BankDetials.AccountNumber);
                            mstBankDetails.NameAsInBank = Aes256CbcEncrypter.Encrypt(userSettings.BankDetials.NameAsInBank);
                            mstBankDetails.NameAsInBank = Aes256CbcEncrypter.Encrypt(userSettings.BankDetials.NameAsInBank);
                            mstBankDetails.BankName = Aes256CbcEncrypter.Encrypt(userSettings.BankDetials.BankName);
                            mstBankDetails.BankCode = Aes256CbcEncrypter.Encrypt(userSettings.BankDetials.BankCode);
                            mstBankDetails.BankSwiftBIC = Aes256CbcEncrypter.Encrypt(userSettings.BankDetials.BankSWIFTBIC);
                            mstBankDetails.Branch = Aes256CbcEncrypter.Encrypt(userSettings.BankDetials.Branch);
                            mstBankDetails.BranchCode = Aes256CbcEncrypter.Encrypt(userSettings.BankDetials.BranchCode);
                            mstBankDetails.PayNow = Aes256CbcEncrypter.Encrypt(userSettings.BankDetials.PayNow);
                            mstBankDetails.BankStatementFileName = Aes256CbcEncrypter.Encrypt(userSettings.BankDetials.BankStatementFileName != null ? userSettings.BankDetials.BankStatementFileName : "");
                            mstBankDetails.BankStatementUrl = userSettings.BankDetials.BankStatementUrl != null ? userSettings.BankDetials.BankStatementUrl : mstExistingBankDetails.BankStatementUrl;
                            mstBankDetails.UserId = mstExistingUserDetails.UserID;
                            mstBankDetails.ModifiedBy = 1;
                            mstBankDetails.ModifiedDate = DateTime.Now;
                            mstBankDetails.CreatedBy = mstExistingBankDetails.CreatedBy;
                            mstBankDetails.CreatedDate = mstExistingBankDetails.CreatedDate;
                            _repository.MstBankDetails.Update(mstBankDetails);
                        }
                        else
                        {
                            var mstBankDetails = _mapper.Map<MstBankDetails>(userSettings.BankDetials);
                            mstBankDetails.AccountNumber = Aes256CbcEncrypter.Encrypt(userSettings.BankDetials.AccountNumber);
                            mstBankDetails.NameAsInBank = Aes256CbcEncrypter.Encrypt(userSettings.BankDetials.NameAsInBank);
                            mstBankDetails.NameAsInBank = Aes256CbcEncrypter.Encrypt(userSettings.BankDetials.NameAsInBank);
                            mstBankDetails.BankName = Aes256CbcEncrypter.Encrypt(userSettings.BankDetials.BankName);
                            mstBankDetails.BankCode = Aes256CbcEncrypter.Encrypt(userSettings.BankDetials.BankCode);
                            mstBankDetails.BankSwiftBIC = Aes256CbcEncrypter.Encrypt(userSettings.BankDetials.BankSWIFTBIC);
                            mstBankDetails.Branch = Aes256CbcEncrypter.Encrypt(userSettings.BankDetials.Branch);
                            mstBankDetails.BranchCode = Aes256CbcEncrypter.Encrypt(userSettings.BankDetials.BranchCode);
                            mstBankDetails.PayNow = Aes256CbcEncrypter.Encrypt(userSettings.BankDetials.PayNow);
                            mstBankDetails.BankStatementFileName = Aes256CbcEncrypter.Encrypt(userSettings.BankDetials.BankStatementFileName != null ? userSettings.BankDetials.BankStatementFileName : "");
                            mstBankDetails.BankStatementUrl = userSettings.BankDetials.BankStatementUrl != null ? userSettings.BankDetials.BankStatementUrl : "";
                            mstBankDetails.UserId = mstExistingUserDetails.UserID;
                            mstBankDetails.ModifiedBy = 1;
                            mstBankDetails.ModifiedDate = DateTime.Now;
                            mstBankDetails.CreatedBy = 1;
                            mstBankDetails.CreatedDate = DateTime.Now;
                            _repository.MstBankDetails.Create(mstBankDetails);
                        }
                        await _repository.SaveAsync();


                        // Alternate approver or verifier
                        if (mstAlternateApproverDetails != null)
                        {
                            // string fromDate = userSettings.AlternateApprover.FromDate.Value.Date.ToString("dd/MM/yyyy");
                            // string toDate = userSettings.AlternateApprover.ToDate.Value.Date.ToString("dd/MM/yyyy");

                            string fromTime = userSettings.AlternateApprover.FromTime;
                            string toTime = userSettings.AlternateApprover.ToTime;

                            mstAlternateApproverDetails.AlternateUser = userSettings.AlternateApprover.UserID;
                            mstAlternateApproverDetails.FromDate = DateTime.ParseExact(userSettings.AlternateApprover.FromDate + " " + fromTime, "dd/MM/yyyy hh:mm tt", CultureInfo.InvariantCulture);
                            mstAlternateApproverDetails.ToDate = DateTime.ParseExact(userSettings.AlternateApprover.ToDate + " " + toTime, "dd/MM/yyyy hh:mm tt", CultureInfo.InvariantCulture);
                            mstAlternateApproverDetails.FromTime = DateTime.Parse(userSettings.AlternateApprover.FromTime);
                            mstAlternateApproverDetails.ToTime = DateTime.Parse(userSettings.AlternateApprover.ToTime);
                            mstAlternateApproverDetails.ModifiedBy = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value);
                            mstAlternateApproverDetails.ModifiedDate = DateTime.Now;
                            mstAlternateApproverDetails.IsActive = userSettings.AlternateApprover.IsEnabled;
                            _repository.MstAlternateApprover.Update(mstAlternateApproverDetails);
                        }
                        else
                        {
                            if (userSettings.AlternateApprover.IsEnabled && userSettings.AlternateApprover.FromDate != null && userSettings.AlternateApprover.ToDate != null && userSettings.AlternateApprover.FromTime != null && userSettings.AlternateApprover.ToTime != null)
                            {
                                string fromTime = userSettings.AlternateApprover.FromTime;
                                string toTime = userSettings.AlternateApprover.ToTime;

                                MstAlternateApprovers mstAlternateApprover = new MstAlternateApprovers()
                                {
                                    UserId = mstExistingUserDetails.UserID,
                                    FromDate = DateTime.ParseExact(userSettings.AlternateApprover.FromDate + " " + fromTime, "dd/MM/yyyy hh:mm tt", CultureInfo.InvariantCulture),
                                    ToDate = DateTime.ParseExact(userSettings.AlternateApprover.ToDate + " " + toTime, "dd/MM/yyyy hh:mm tt", CultureInfo.InvariantCulture),
                                    FromTime = DateTime.Parse(userSettings.AlternateApprover.FromTime),
                                    ToTime = DateTime.Parse(userSettings.AlternateApprover.ToTime),
                                    AlternateUser = userSettings.AlternateApprover.UserID,
                                    CreatedBy = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value),
                                    CreatedDate = DateTime.Now,
                                    IsActive = userSettings.AlternateApprover.IsEnabled
                                };
                                _repository.MstAlternateApprover.Create(mstAlternateApprover);
                            }
                        }
                        await _repository.SaveAsync();

                        // Delegate user
                        if (mstDelegateUsersDetails != null)
                        {
                            // string fromDate = userSettings.AlternateApprover.FromDate.Value.Date.ToString("dd/MM/yyyy");
                            // string toDate = userSettings.AlternateApprover.ToDate.Value.Date.ToString("dd/MM/yyyy");

                            string fromTime = userSettings.DelegateUser.FromTime;
                            string toTime = userSettings.DelegateUser.ToTime;

                            mstDelegateUsersDetails.DelegateUser = userSettings.DelegateUser.UserID;
                            mstDelegateUsersDetails.FromDate = DateTime.ParseExact(userSettings.DelegateUser.FromDate + " " + fromTime, "dd/MM/yyyy hh:mm tt", CultureInfo.InvariantCulture);
                            mstDelegateUsersDetails.ToDate = DateTime.ParseExact(userSettings.DelegateUser.ToDate + " " + toTime, "dd/MM/yyyy hh:mm tt", CultureInfo.InvariantCulture);
                            mstDelegateUsersDetails.FromTime = DateTime.Parse(userSettings.DelegateUser.FromTime);
                            mstDelegateUsersDetails.ToTime = DateTime.Parse(userSettings.DelegateUser.ToTime);
                            mstDelegateUsersDetails.ModifiedBy = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value);
                            mstDelegateUsersDetails.ModifiedDate = DateTime.Now;
                            mstDelegateUsersDetails.IsActive = userSettings.DelegateUser.IsEnabled;
                            _repository.MstDelegateUsers.Update(mstDelegateUsersDetails);
                        }
                        else
                        {
                            if (userSettings.DelegateUser != null)
                            {
                                if (userSettings.DelegateUser.IsEnabled && userSettings.DelegateUser.FromDate != null && userSettings.DelegateUser.ToDate != null && userSettings.DelegateUser.FromTime != null && userSettings.DelegateUser.ToTime != null)
                                {
                                    string fromTime = userSettings.DelegateUser.FromTime;
                                    string toTime = userSettings.DelegateUser.ToTime;

                                    MstDelegateUsers mstDelegateUsers = new MstDelegateUsers()
                                    {
                                        UserId = mstExistingUserDetails.UserID,
                                        FromDate = DateTime.ParseExact(userSettings.DelegateUser.FromDate + " " + fromTime, "dd/MM/yyyy hh:mm tt", CultureInfo.InvariantCulture),
                                        ToDate = DateTime.ParseExact(userSettings.DelegateUser.ToDate + " " + toTime, "dd/MM/yyyy hh:mm tt", CultureInfo.InvariantCulture),
                                        FromTime = DateTime.Parse(userSettings.DelegateUser.FromTime),
                                        ToTime = DateTime.Parse(userSettings.DelegateUser.ToTime),
                                        DelegateUser = userSettings.DelegateUser.UserID,
                                        CreatedBy = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value),
                                        CreatedDate = DateTime.Now,
                                        IsActive = userSettings.DelegateUser.IsEnabled
                                    };
                                    _repository.MstDelegateUsers.Create(mstDelegateUsers);
                                }
                            }
                        }
                        await _repository.SaveAsync();


                        // TODO 3: Approvers list should be deleted first. Delete all the rows of approvers for this user id and re create 
                        //List<MstUserApprovers> existingApprovers = await _repository.MstUserApprovers.GetUserApproversByUserIdAsync(mstExistingUserDetails.UserID) as List<MstUserApprovers>;
                        List<MstUserApprovers> existingApprovers = await _repository.MstUserApprovers.GetUserApproversByUserIdFacilityIdAsync(mstExistingUserDetails.UserID, mstExistingUserDetails.FacilityID) as List<MstUserApprovers>;
                        foreach (var approver in existingApprovers)
                        {
                            _repository.MstUserApprovers.Delete(approver);
                        }
                        foreach (MstUserApprovers approver in mstUserApproverDetails)
                        {
                            if (approver.IsApproverActive)
                            {
                                approver.UserId = mstExistingUserDetails.UserID;
                                approver.FacilityId = mstExistingUserDetails.FacilityID;
                                _repository.MstUserApprovers.Create(approver);
                            }
                        }
                        await _repository.SaveAsync();
                        transaction.Commit();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Something went wrong inside CreateDepartment action: {ex.Message}");
                        transaction.Rollback();
                    }
                }
                _toastNotification.AddSuccessToastMessage("User Settings Updated Successfully", new NotyOptions() { Timeout = 5000 });
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Something went wrong inside CreateDepartment action: {ex.Message}");
                _toastNotification.AddErrorToastMessage($"Failed to Update Settings. Error: {ex.Message}");
                return StatusCode(500, "Internal server error");
            }
        }

        private List<UserApproversViewModel> ReconcileUserApprovers(List<UserApproversViewModel> approvers)
        {
            List<UserApproversViewModel> Approvers = new List<UserApproversViewModel>();
            if (approvers == null)
                return Approvers;
            for (byte cnt = 1; cnt <= _userApproversCount; cnt++)
            {
                if (approvers.Where(x => x.SortOrder == cnt).Any())
                {
                    Approvers.Add(approvers.Where(x => x.SortOrder == cnt).FirstOrDefault());
                }
                else
                {
                    Approvers.Add(new UserApproversViewModel
                    {
                        SortOrder = cnt
                    });
                }
            }
            return Approvers;
        }

        public async Task<string> GetBankSwiftBIC(long bankCode)
        {
            var mstBankSwiftBIC = await _repository.MstBankSwiftBIC.GetBankSwiftBICByBankCodeAsync(bankCode);
            if (mstBankSwiftBIC != null)
                return mstBankSwiftBIC.BankSwiftBIC;
            else
                return string.Empty;
        }


    }
}
