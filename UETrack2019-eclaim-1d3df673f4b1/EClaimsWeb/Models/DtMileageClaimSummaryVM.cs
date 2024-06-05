using EClaimsEntities.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EClaimsWeb.Models
{
    public class DtMileageClaimSummaryVM
    {
        public int MCID { get; set; }
        public List<DtMileageClaimSummary> dtClaims { get; set; }
    }
}
