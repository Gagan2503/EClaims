using EClaimsRepository.Contracts;
using EClaimsWeb.Helpers;
using EClaimsWeb.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace EClaimsWeb.Controllers
{
    [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private IRepositoryWrapper _repository;
        private DelegateUserHelper _delegateUserHelper;

        public HomeController(ILogger<HomeController> logger, IRepositoryWrapper repository)
        {
            _logger = logger;
            _repository = repository;
            _delegateUserHelper = new DelegateUserHelper(null, repository);
        }

        public async Task<IActionResult> Index()
        {
            string sUserId = User.FindFirstValue("userid");
            var userFacilities = await _repository.DtUserFacilities.GetAllFacilitiesByUserIdAsync(Convert.ToInt32(sUserId));

            var delegatedUserId = await _delegateUserHelper.IsUserHasAnyDelegateUserSet(Convert.ToInt32(sUserId));

            if (delegatedUserId.HasValue)
                return Redirect("/DelegateUser");

            if (userFacilities.Count() > 1)
                return Redirect("/LoginFacility");

            return View();
        }

        [HttpGet("denied")]
        public IActionResult Denied()
        {
            if (User.Identity.IsAuthenticated)
            {
                var role = User.Claims.FirstOrDefault(m => m.Type == ClaimTypes.Role)?.Value;
                if (role == "NewUser") // we have a new user, let's take them to a new user welcome page    
                {
                    return Redirect("/newuser");
                }
            }
            return View();
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Secured()
        {
            await Task.CompletedTask;
            //var idToken = await HttpContext.GetTokenAsync("id_token");
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
