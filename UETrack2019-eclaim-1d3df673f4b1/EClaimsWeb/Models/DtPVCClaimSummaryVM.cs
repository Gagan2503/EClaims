using EClaimsEntities.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EClaimsWeb.Models
{
    public class DtPVCClaimSummaryVM
    {
        public int PVCCID { get; set; }
        public List<DtPVCClaimSummary> dtClaims { get; set; }
    }
}
