using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EClaimsWeb.Models
{
    public class TBClaimsVM
    {
        public TBClaimsVM()
        {
            tbClaims = new List<TBClaimVM>();
            tbClaimsDrafts = new List<TBClaimVM>();
        }
        public List<TBClaimVM> tbClaims { get; set; }
        public List<TBClaimVM> tbClaimsDrafts { get; set; }
    }
}
