using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EClaimsWeb.Models
{
    public class QueryParam
    {
        public string Message { get; set; }
        public string Cid { get; set; }
        public string[] RecieverId { get; set; }
        public string Claim { get; set; }
    }
}
