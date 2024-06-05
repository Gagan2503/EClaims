using AutoMapper;
using EClaimsEntities;
using EClaimsRepository.Contracts;
using EClaimsWeb.Helpers;
using EClaimsWeb.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using NToastNotify;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace EClaimsWeb.Controllers
{
    [Authorize(Roles = "Admin,Finance,User,HR")]
    public class DelegateUserController : Controller
    {
        private ILoggerManager _logger;
        private IRepositoryWrapper _repository;
        private IMapper _mapper;
        private readonly IToastNotification _toastNotification;
        private readonly RepositoryContext _context;
        private IWebHostEnvironment _webHostEnvironment;
        private DelegateUserHelper _delegateUserHelper;

        public DelegateUserController(ILoggerManager logger, IRepositoryWrapper repository, IMapper mapper, RepositoryContext context, IToastNotification toastNotification, IWebHostEnvironment webHostEnvironment)
        {
            _logger = logger;
            _repository = repository;
            _mapper = mapper;
            _context = context;
            _toastNotification = toastNotification;
            _webHostEnvironment = webHostEnvironment;
            _delegateUserHelper = new DelegateUserHelper(logger, repository, context);
        }
        public async Task<IActionResult> Index()
        {
            try
            {
                string userId = string.Empty;
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

                List<UserVM> oclsUsers = new List<UserVM>();
                //oclsModule.Add(new clsModule() { ModuleName = "Admin Settings", ModuleId = "Admin Settings" });
                oclsUsers.Add(new UserVM { UserID = Convert.ToInt32(sUserId), Name = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.GivenName).Value.ToString() });

                


                var facilitiesList = await _repository.DtUserFacilities.GetAllFacilitiesByUserIdAsync(Convert.ToInt32(sUserId));

                List<SelectListItem> facilities = (from t in facilitiesList
                                                   select new SelectListItem
                                                   {
                                                       Text = t.FacilityName.ToString(),
                                                       Value = t.FacilityID.ToString(),
                                                   }).OrderBy(p => p.Text).ToList();

                var departmenttip = new SelectListItem()
                {
                    Value = null,
                    Text = "--- Select Facility ---"
                };

                facilities.Insert(0, departmenttip);

                #region Delegate User Check code
                int? delegatedUserId = null;
                int loggedInUserId = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value);

                int? approverId = await _delegateUserHelper.IsDelegateUserSetForUser(loggedInUserId);
                bool isDelegateUserSet = false;
                if (approverId.HasValue)
                {
                    // Delegate User is configured for the current user. So, do not show actions
                    isDelegateUserSet = true;
                }
                else
                {
                    // Current user has not delegated his delegation. Check if the current user has any delegation 
                    delegatedUserId = await _delegateUserHelper.IsUserHasAnyDelegateUserSet(loggedInUserId);
                }

                var delegatedUserDetails = await _repository.MstUser.GetUserWithDetailsByIdAsync(delegatedUserId);
                if(delegatedUserDetails != null)
                    oclsUsers.Add(new UserVM { UserID = delegatedUserDetails.UserID, Name = delegatedUserDetails.Name });

                List<SelectListItem> delUsers = (from t in oclsUsers
                                                select new SelectListItem
                                                {
                                                    Text = t.Name.ToString(),
                                                    Value = t.UserID.ToString(),
                                                }).OrderBy(p => p.Text).ToList();

                var usertip = new SelectListItem()
                {
                    Value = null,
                    Text = "--- Select User ---"
                };

                delUsers.Insert(0, usertip);
                //return new SelectList(delUsers, "Value", "Text");

                userSettingsViewModel.Facilitys = new SelectList(delUsers, "Value", "Text");


                TempData["isDelegateUserSet"] = isDelegateUserSet;
                #endregion

                //userSettingsViewModel.Facilitys = new SelectList(facilities, "Value", "Text");

                _logger.LogInfo($"Returned all facilities from database.");
                return View(userSettingsViewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Something went wrong inside Login Facility index action: {ex.Message}");
                _toastNotification.AddErrorToastMessage(ex.Message);
                return View();
            }
        }

        [HttpPost]
        public async Task<IActionResult> Index(string id)
        {
            try
            {
                string userId = string.Empty;
                int userid = 0;
                int delegatedUserId = 0;
                if (!string.IsNullOrEmpty(id))
                {
                    delegatedUserId = int.Parse(id);
                    ViewBag.IsEditForLoggedInUser = "0";
                }
                else
                {
                    delegatedUserId = int.Parse(id);
                    ViewBag.IsEditForLoggedInUser = "1";
                }
                int loggedInUserId = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value);
                var userFacilities = await _repository.DtUserFacilities.GetAllFacilitiesByUserIdAsync(Convert.ToInt32(delegatedUserId));
                if (loggedInUserId != delegatedUserId)
                {
                    var currentUser = User.Claims;
                    UserSettingsViewModel userSettingsViewModel = new UserSettingsViewModel();
                    string sUserId = User.FindFirstValue("userid"); // will give the user's userId

                    bool isValidUserid = !string.IsNullOrEmpty(sUserId) ? int.TryParse(sUserId, out userid) : false;

                    var name = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.GivenName).Value;



                    var user = User as ClaimsPrincipal;
                    var identity = user.Identity as ClaimsIdentity;

                    if (identity.Claims.FirstOrDefault(c => c.Type == "delegateuserid") is null)
                    {
                        identity.AddClaim(new Claim("delegateuserid", delegatedUserId.ToString()));

                        if (userFacilities.Count() == 1)
                            identity.AddClaim(new Claim("delegatefacilityid", userFacilities.FirstOrDefault().FacilityID.ToString()));

                        var scheme = User.Claims.FirstOrDefault(c => c.Type == ".AuthScheme").Value;
                        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

                        var claimsIdentity = new ClaimsIdentity(user.Claims, CookieAuthenticationDefaults.AuthenticationScheme);
                        var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);
                        var items = new Dictionary<string, string>();
                        items.Add(".AuthScheme", CookieAuthenticationDefaults.AuthenticationScheme);
                        items.Add("IsPersistent", "true");
                        items.Add("ExpiresUtc", DateTime.UtcNow.AddMinutes(10).ToString());
                        var properties = new AuthenticationProperties(items);
                        await HttpContext.SignInAsync(claimsPrincipal, properties);
                    }
                        
                }

                

                //var scheme = User.Claims.FirstOrDefault(c => c.Type == ".AuthScheme").Value;
                //await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

                //var claimsIdentity = new ClaimsIdentity(user.Claims, CookieAuthenticationDefaults.AuthenticationScheme);
                //var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);
                //var items = new Dictionary<string, string>();
                //items.Add(".AuthScheme", CookieAuthenticationDefaults.AuthenticationScheme);
                //items.Add("IsPersistent", "true");
                //items.Add("ExpiresUtc", DateTime.UtcNow.AddMinutes(10).ToString());
                //var properties = new AuthenticationProperties(items);
                //await HttpContext.SignInAsync(claimsPrincipal, properties);

                
                _logger.LogInfo($"Returned all facilities from database.");
                if (userFacilities.Count() > 1)
                    return Json(new { res = "fac" });
                else if (User.IsInRole("Finance") && User.FindFirst("delegateuserid") is null)
                    return Json(new { res = "fin" });
                else if (User.IsInRole("HR") && User.FindFirst("delegateuserid") is null)
                    return Json(new { res = "hr" });
                else if (User.FindFirst("delegateuserid") is not null)
                    return Json(new { res = "del" });
                else
                    return Json(new { res = "user" });

                //return RedirectToAction("Index", "DashboardFinance", new { area = "" });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Something went wrong inside Login Facility index post action: {ex.Message}");
                _toastNotification.AddErrorToastMessage(ex.Message);
                return View();
            }
        }
    }
}
