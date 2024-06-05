using EClaimsEntities.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EClaimsWeb.Models
{
    public class HODClaimSearchViewModel
    {
        public List<CustomClaim> customClaimVMs { get; set; }
        //public SelectList Screens { get; set; }
        //public string ScreenId { get; set; }
        public IEnumerable<SelectListItem> ReportTypes { get; set; }
        public string ModuleName { get; set; }

        public IEnumerable<SelectListItem> Statuses { get; set; }
        public int? StatusID { get; set; }

        public string FromDate { get; set; }
        public string ToDate { get; set; }
    }
}
