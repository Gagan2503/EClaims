using EClaimsEntities.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EClaimsWeb.Models
{
    public class DtPVGClaimSummaryVM
    {
        public int PVGCID { get; set; }
        public List<DtPVGClaimSummary> dtClaims { get; set; }
    }
}
