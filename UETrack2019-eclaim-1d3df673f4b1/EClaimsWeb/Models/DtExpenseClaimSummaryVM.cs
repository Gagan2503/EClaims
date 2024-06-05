using EClaimsEntities.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EClaimsWeb.Models
{
    public class DtExpenseClaimSummaryVM
    {
        public int ECID { get; set; }
        public List<DtExpenseClaimSummary> dtClaims { get; set; }
    }
}
