using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EClaimsWeb.Models
{
    public class HodClaimSearch
    {
        public string ModuleName { get; set; }
        public int StatusID { get; set; }
        public string FromDate { get; set; }
        public string ToDate { get; set; }
        public string ClaimIds { get; set; }
    }
}
