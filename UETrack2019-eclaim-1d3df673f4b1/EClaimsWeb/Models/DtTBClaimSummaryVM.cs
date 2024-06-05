using EClaimsEntities.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EClaimsWeb.Models
{
    public class DtTBClaimSummaryVM
    {
        public int TBCID { get; set; }
        public List<DtTBClaimSummary> dtClaims { get; set; }
    }
}
