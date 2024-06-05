using AutoMapper;
using EClaimsEntities.Models;
using EClaimsRepository.Contracts;
using EClaimsWeb.Helpers;
using EClaimsWeb.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace EClaimsWeb.Controllers
{
    public class AccountController : Controller
    {
        private ILoggerManager _logger;
        private IRepositoryWrapper _repository;
        private IMapper _mapper;
        private DelegateUserHelper _delegateUserHelper;

        public AccountController(ILoggerManager logger, IRepositoryWrapper repository, IMapper mapper)
        {
            _logger = logger;
            _repository = repository;
            _mapper = mapper;
            _delegateUserHelper = new DelegateUserHelper(logger, repository);
        }

        public IActionResult Login(string returnUrl)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpGet("login/{provider}")]
        public IActionResult LoginExternal([FromRoute] string provider, [FromQuery] string returnUrl)
        {
            if (User != null && User.Identities.Any(identity => identity.IsAuthenticated))
            {
                return RedirectToAction("", "Home");
            }

            // By default the client will be redirect back to the URL that issued the challenge (/login?authtype=foo),
            // send them to the home page instead (/).
            returnUrl = string.IsNullOrEmpty(returnUrl) ? "/" : returnUrl;
            var authenticationProperties = new AuthenticationProperties { RedirectUri = returnUrl };
            // authenticationProperties.SetParameter("prompt", "select_account");
            return new ChallengeResult(provider, authenticationProperties);
        }

        [HttpGet("newuser")]
        public IActionResult NewUser()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken()]
        [Route("validate")]
        public async Task<IActionResult> Validate(string username, string password, string returnUrl)
        {
            returnUrl = string.IsNullOrEmpty(returnUrl) ? "/" : returnUrl;
            ViewData["ReturnUrl"] = returnUrl;
            string decryptpassword = Aes256CbcEncrypter.Encrypt(password);
            if ( _repository.MstUser.TryValidateUser(username, decryptpassword, out List<Claim> claims))
            {
                var mstUser = _repository.MstUser.GetByUserName(username);
                var userRoles = _repository.DtUserRoles.GetAllRolesByUserIdAsync(mstUser.UserID);
                var userFacilities = await _repository.DtUserFacilities.GetAllFacilitiesByUserIdAsync(mstUser.UserID);
                foreach (var r in userRoles)
                {
                    claims.Add(new Claim(ClaimTypes.Role, r.RoleName));
                }

                //if (userFacilities.Count() > 1)
                //    return Redirect("/LoginFacility");

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);
                var items = new Dictionary<string, string>();
                items.Add(".AuthScheme", CookieAuthenticationDefaults.AuthenticationScheme);
                items.Add("IsPersistent", "true");
                items.Add("ExpiresUtc", DateTime.UtcNow.AddMinutes(10).ToString());
                var properties = new AuthenticationProperties(items);
                await HttpContext.SignInAsync(claimsPrincipal, properties);
                //var delegatedUserId = await _delegateUserHelper.IsUserHasAnyDelegateUserSet(mstUser.UserID);

                //if(delegatedUserId.HasValue)
                //    return Redirect("/DelegateUser");
                
                if (mstUser.IsTwoFactorEnabled)
                    return Redirect(returnUrl);
                else
                    return Redirect("/FirstTimeUserSettings");
            }
            else
            {
                TempData["Error"] = "Error. Username or Password is invalid";
                return View("login");
            }
        }

        [Authorize]
        public async Task<IActionResult> Logout()
        {
            var scheme = User.Claims.FirstOrDefault(c => c.Type == ".AuthScheme").Value;
            string domainUrl = HttpContext.Request.Scheme + "://" + HttpContext.Request.Host;
            switch (scheme)
            {
                case "google":
                    await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    var redirect = $"https://www.google.com/accounts/Logout?continue=https://appengine.google.com/_ah/logout?continue={domainUrl}";
                    return Redirect(redirect);
                case "facebook":
                case "Cookies":
                    await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    return Redirect("/");
                case "microsoft":
                    await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    return Redirect("/");
                default:
                    return new SignOutResult(new[] { CookieAuthenticationDefaults.AuthenticationScheme, scheme });
            }
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Login(LoginViewModel model, string returnUrl)
        {
            try
            {
                if (model == null)
                {
                    _logger.LogError("Login object sent from client is null.");
                    return BadRequest("Login object is null");
                }

                if (!ModelState.IsValid)
                {
                    _logger.LogError("Invalid Login object sent from client.");
                    return BadRequest("Invalid Login object");
                }

                var mstUserEntity = _mapper.Map<MstUser>(model);

                var userEntity = _repository.MstUser.Authenticate(mstUserEntity);

                if (userEntity == null)
                {
                    ViewBag.Error = "Authentication failed";
                    return View();
                }

                var claims = new List<Claim>() {
                    new Claim(ClaimTypes.NameIdentifier, Convert.ToString(userEntity.UserID)),
                        new Claim(ClaimTypes.Name, userEntity.UserName),
                        new Claim(ClaimTypes.Role, Convert.ToString(userEntity.TenantId)),
                };
                //Initialize a new instance of the ClaimsIdentity with the claims and authentication scheme    
                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                //Initialize a new instance of the ClaimsPrincipal with ClaimsIdentity    
                var principal = new ClaimsPrincipal(identity);
                //SignInAsync is a Extension method for Sign in a principal for the specified scheme.    
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, new AuthenticationProperties()
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTime.UtcNow.AddMinutes(10)
                });
                

                
                return RedirectToAction("Index", "Home", new { area = "" });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Something went wrong inside Authentication action: {ex.Message}");
                return RedirectToAction("Index");
            }
        }

        public IActionResult Denied()
        {
            if (User.Identity.IsAuthenticated)
            {
                //var role = User.Claims.FirstOrDefault(m => m.Type == ClaimTypes.Role)?.Value;
                //if (role == "NewUser") // we have a new user, let's take them to a new user welcome page    
                //{
                //    return Redirect("/newuser");
                //}
            }
            return View();
        }

        [HttpGet]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public ActionResult<MstUser> GetCurrentUser()
        {
            return Ok(_repository.MstUser.GetByUserName(User.Identity.Name));
        }
    }
}
