using AutoMapper;
using EClaimsEntities;
using EClaimsEntities.Models;
using EClaimsRepository.Contracts;
using EClaimsWeb.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EClaimsWeb.Controllers
{
    public class DashboardController : Controller
    {
        private ILoggerManager _logger;
        private IRepositoryWrapper _repository;
        private IMapper _mapper;

        private readonly RepositoryContext _context;

        public DashboardController(ILoggerManager logger, IRepositoryWrapper repository, IMapper mapper, RepositoryContext context)
        {
            _logger = logger;
            _repository = repository;
            _mapper = mapper;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            DateTime? currentDate = DateTime.Now;
            ViewBag.CurrentMonth = currentDate.Value.ToString("MMM");
            var customClaimsApprovals = await _repository.MstUser.GetAllUserApprovalClaimsCountAsync(Int32.Parse(HttpContext.User.FindFirst("userid").Value));
            var customClaimsVerification = await _repository.MstUser.GetAllUserVerificationClaimsCountAsync(Int32.Parse(HttpContext.User.FindFirst("userid").Value));
            var customClaimsTotalClaimsThisYear = await _repository.MstUser.GetAllUserTOTALCLAIMSCOUNTTHISYEARAsync(Int32.Parse(HttpContext.User.FindFirst("userid").Value));
            var customClaimsTotalClaimsTillNow = await _repository.MstUser.GetAllUserTOTALCLAIMSCOUNTTILLNOWAsync(Int32.Parse(HttpContext.User.FindFirst("userid").Value));

            List<CustomClaim> customClaimVMs = new List<CustomClaim>();

            var mstPendingApprovalClaimsWithDetails = await _repository.MstUser.GetAllPendingUserApprovalClaimsAsync(Int32.Parse(HttpContext.User.FindFirst("userid").Value));
            if (mstPendingApprovalClaimsWithDetails != null && mstPendingApprovalClaimsWithDetails.Any())
            {
                mstPendingApprovalClaimsWithDetails.ToList().ForEach(c => c.IsDelegated = false);
            }

            foreach (var mc in mstPendingApprovalClaimsWithDetails)
            {
                CustomClaim mileageClaimVM = new CustomClaim();
                mileageClaimVM.PVGCItemID = mc.PVGCItemID;
                mileageClaimVM.CID = mc.CID;
                mileageClaimVM.CNO = mc.CNO;
                mileageClaimVM.Name = mc.Name;
                mileageClaimVM.CreatedDate = Convert.ToDateTime(mc.CreatedDate).ToString("dd/MM/yyyy", System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
                mileageClaimVM.TotalAmount = mc.TotalAmount;
                customClaimVMs.Add(mileageClaimVM);
            }

            List<CustomClaim> customUserClaimVMs = new List<CustomClaim>();

            var mstUserSubmittedClaimsWithDetails = await _repository.MstUser.GetAllUserSubmittedClaimsAsync(Int32.Parse(HttpContext.User.FindFirst("userid").Value));
            if (mstUserSubmittedClaimsWithDetails != null && mstUserSubmittedClaimsWithDetails.Any())
            {
                mstUserSubmittedClaimsWithDetails.ToList().ForEach(c => c.IsDelegated = false);
            }

            foreach (var mc in mstUserSubmittedClaimsWithDetails)
            {
                CustomClaim mileageClaimVM = new CustomClaim();
                mileageClaimVM.PVGCItemID = mc.PVGCItemID;
                mileageClaimVM.CID = mc.CID;
                mileageClaimVM.CNO = mc.CNO;
                mileageClaimVM.Name = mc.Name;
                mileageClaimVM.CreatedDate = Convert.ToDateTime(mc.CreatedDate).ToString("dd/MM/yyyy", System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
                mileageClaimVM.TotalAmount = mc.TotalAmount;
                mileageClaimVM.ApprovalStatus = mc.ApprovalStatus;
                customUserClaimVMs.Add(mileageClaimVM);
            }

            var mstDashboardUserVM = new DashboardViewModel
            {
                customClaimVMs = customClaimVMs,
                customUserClaimVMs = customUserClaimVMs,
                VerificationCount = customClaimsVerification.TOTALVERIFICATIONCOUNT,
                ApprovalCount = customClaimsApprovals.TOTALAPPROVALCOUNT,
                CurrentYearCount = customClaimsTotalClaimsThisYear.TOTALCLAIMSCOUNTTHISYEAR,
                OverallCount = customClaimsTotalClaimsTillNow.TOTALCLAIMSCOUNTTILLNOW
            };

            return View(mstDashboardUserVM);
        }
    }
}
