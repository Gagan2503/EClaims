using EClaimsEntities.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EClaimsWeb.Models
{
    public class DtHRPVGClaimSummaryVM
    {
        public int HRPVGCID { get; set; }
        public List<DtHRPVGClaimSummary> dtClaims { get; set; }
    }
}
