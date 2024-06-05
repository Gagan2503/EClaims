using EClaimsEntities.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EClaimsWeb.Models
{
    public class DashboardViewModel
    {
        public List<CustomClaim> customClaimVMs { get; set; }
        public List<CustomClaim> customUserClaimVMs { get; set; }
        public long VerificationCount { get; set; }
        public long ApprovalCount { get; set; }

        public long CurrentYearCount { get; set; }

        public long OverallCount { get; set; }
        //public List<CustomClaim> customClaimVMs { get; set; }
    }
}
