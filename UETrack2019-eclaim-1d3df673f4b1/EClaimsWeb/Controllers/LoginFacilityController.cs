using AutoMapper;
using EClaimsEntities;
using EClaimsEntities.Models;
using EClaimsRepository.Contracts;
using EClaimsWeb.Helpers;
using EClaimsWeb.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
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
    public class LoginFacilityController : Controller
    {
        private ILoggerManager _logger;
        private IRepositoryWrapper _repository;
        private IMapper _mapper;
        private readonly IToastNotification _toastNotification;
        private readonly RepositoryContext _context;
        private IWebHostEnvironment _webHostEnvironment;

        public LoginFacilityController(ILoggerManager logger, IRepositoryWrapper repository, IMapper mapper, RepositoryContext context, IToastNotification toastNotification, IWebHostEnvironment webHostEnvironment)
        {
            _logger = logger;
            _repository = repository;
            _mapper = mapper;
            _context = context;
            _toastNotification = toastNotification;
            _webHostEnvironment = webHostEnvironment;
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

                int loggedInUserId = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value);
                int delegatedUserId = 0;
                //int loggedInFacilityId = Convert.ToInt32(HttpContext.User.FindFirst("facilityid").Value);
                //int delegatedFacilityId = 0;
                if (User.Claims.FirstOrDefault(c => c.Type == "delegateuserid") is not null)
                {
                    delegatedUserId = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid").Value);
                    //delegatedFacilityId = Convert.ToInt32(HttpContext.User.FindFirst("facilityid").Value);
                }


                var facilitiesList = await _repository.DtUserFacilities.GetAllFacilitiesByUserIdAsync(delegatedUserId == 0 ? loggedInUserId : delegatedUserId);

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

                userSettingsViewModel.Facilitys = new SelectList(facilities, "Value", "Text");

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

                int loggedInUserId = Convert.ToInt32(HttpContext.User.FindFirst("userid").Value);
                int delegatedUserId = 0;
                //int loggedInFacilityId = Convert.ToInt32(HttpContext.User.FindFirst("facilityid").Value);
                int delegatedFacilityId = 0;

                var user = User as ClaimsPrincipal;
                var identity = user.Identity as ClaimsIdentity;

                if (User.Claims.FirstOrDefault(c => c.Type == "delegateuserid") is not null)
                {
                    delegatedUserId = Convert.ToInt32(HttpContext.User.FindFirst("delegateuserid").Value);
                    if (User.Claims.FirstOrDefault(c => c.Type == "delegatefacilityid") is null)
                    {
                        identity.AddClaim(new Claim("delegatefacilityid", id));
                        delegatedFacilityId = Convert.ToInt32(id);
                    }
                    else
                    {
                        var claim = (from c in user.Claims
                                     where c.Type == "delegatefacilityid"
                                     select c).Single();
                        identity.RemoveClaim(claim);
                        identity.AddClaim(new Claim("delegatefacilityid", id));
                    }
                }

                //var name = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.GivenName).Value;

                if (User.Claims.FirstOrDefault(c => c.Type == "delegateuserid") is null)
                {
                    var claim = (from c in user.Claims
                                 where c.Type == "facilityid"
                                 select c).Single();
                    identity.RemoveClaim(claim);
                    identity.AddClaim(new Claim("facilityid", id));
                }

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

                _logger.LogInfo($"Returned all facilities from database.");

                if(User.IsInRole("Finance") && User.FindFirst("delegateuserid") is null)
                    return Json(new { res = "fin" });
                else if(User.IsInRole("HR") && User.FindFirst("delegateuserid") is null)
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
