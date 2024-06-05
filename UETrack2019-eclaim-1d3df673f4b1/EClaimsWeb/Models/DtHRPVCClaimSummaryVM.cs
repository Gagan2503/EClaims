using EClaimsEntities.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EClaimsWeb.Models
{
    public class DtHRPVCClaimSummaryVM
    {
        public int HRPVCCID { get; set; }
        public List<DtHRPVCClaimSummary> dtClaims { get; set; }
    }
}
