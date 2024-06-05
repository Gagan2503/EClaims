using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EClaimsWeb.Models
{
    public class PVGClaimsVM
    {
        public PVGClaimsVM()
        {
            pvgClaims = new List<PVGClaimVM>();
            pvgClaimsDrafts = new List<PVGClaimVM>();
        }
        public List<PVGClaimVM> pvgClaims { get; set; }
        public List<PVGClaimVM> pvgClaimsDrafts { get; set; }
    }
}
