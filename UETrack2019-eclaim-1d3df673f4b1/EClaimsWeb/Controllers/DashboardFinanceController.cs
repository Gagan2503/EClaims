using AutoMapper;
using EClaimsEntities;
using EClaimsEntities.Models;
using EClaimsRepository.Contracts;
using EClaimsWeb.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EClaimsWeb.Controllers
{
    public class DashboardFinanceController : Controller
    {
        private ILoggerManager _logger;
        private IRepositoryWrapper _repository;
        private IMapper _mapper;

        private readonly RepositoryContext _context;

        public DashboardFinanceController(ILoggerManager logger, IRepositoryWrapper repository, IMapper mapper, RepositoryContext context)
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

            var customClaimsIndividualApproval = await _repository.MstUser.GetAllUserIndividualApprovalClaimsCountAsync(Int32.Parse(HttpContext.User.FindFirst("userid").Value),"User");
            var customClaimsApproved = await _repository.MstUser.GetAllUserApprovedClaimsCountAsync(Int32.Parse(HttpContext.User.FindFirst("userid").Value),"User");

            ViewBag.ApprovedCounts = JsonConvert.SerializeObject(customClaimsApproved.Select(x => x.ClaimCount));
            //ViewBag.ReceivedCounts = JsonConvert.SerializeObject(customClaimsIndividualApproval.Select(x => x.ClaimCount));

            var result = customClaimsApproved.Concat(customClaimsIndividualApproval).GroupBy(x => x.Claim)
                                    .Select(x => new CustomIndividualClaimCount
                                    {
                                        Claim = x.Key,
                                        ClaimCount = x.Sum(z => z.ClaimCount)
                                    }).ToList();
            ViewBag.ReceivedCounts = JsonConvert.SerializeObject(result.Select(x => x.ClaimCount));

            //HRClaims
            var customHRClaimsIndividualApproval = await _repository.MstUser.GetAllUserIndividualApprovalClaimsCountAsync(Int32.Parse(HttpContext.User.FindFirst("userid").Value), "HR");
            var customHRClaimsApproved = await _repository.MstUser.GetAllUserApprovedClaimsCountAsync(Int32.Parse(HttpContext.User.FindFirst("userid").Value), "HR");

            ViewBag.HRApprovedCounts = JsonConvert.SerializeObject(customHRClaimsApproved.Select(x => x.ClaimCount));
            //ViewBag.ReceivedCounts = JsonConvert.SerializeObject(customClaimsIndividualApproval.Select(x => x.ClaimCount));

            var resultHR = customHRClaimsApproved.Concat(customHRClaimsIndividualApproval).GroupBy(x => x.Claim)
                                    .Select(x => new CustomIndividualClaimCount
                                    {
                                        Claim = x.Key,
                                        ClaimCount = x.Sum(z => z.ClaimCount)
                                    }).ToList();
            ViewBag.HRReceivedCounts = JsonConvert.SerializeObject(resultHR.Select(x => x.ClaimCount));

            var customUserSubmittedClaimsCount = await _repository.MstUser.GetAllUserSubmittedClaimsCountAsync(Int32.Parse(HttpContext.User.FindFirst("userid").Value));
            ViewBag.UserSubmittedClaimsCount = JsonConvert.SerializeObject(customUserSubmittedClaimsCount.Select(x => x.ClaimCount));

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
