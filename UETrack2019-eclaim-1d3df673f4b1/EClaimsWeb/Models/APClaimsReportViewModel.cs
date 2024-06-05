using EClaimsEntities.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace EClaimsWeb.Models
{
    public class APClaimsReportViewModel
    {
        public List<CustomClaimReports> customClaimVMs { get; set; }
        //public SelectList Screens { get; set; }
        //public string ScreenId { get; set; }

        public IEnumerable<SelectListItem> Users { get; set; }
        public int? UserID { get; set; }

        public IEnumerable<SelectListItem> ReportTypes { get; set; }
        [Display(Name = "Report", Prompt = "Report")]
        [Required(ErrorMessage = "Report is required")]
        public string ModuleName { get; set; }

        public IEnumerable<SelectListItem> Facilities { get; set; }

        [Display(Name = "Facility", Prompt = "Facility")]
        [Required(ErrorMessage = "Facility is required")]
        public int? FacilityID { get; set; }

        public IEnumerable<SelectListItem> Statuses { get; set; }
        public int? StatusID { get; set; }

        public string FromDate { get; set; }
        public string ToDate { get; set; }
        public decimal ClaimsGrandTotal { get; set; }
        public decimal ClaimsTotalAmount { get; set; }
    }
}
