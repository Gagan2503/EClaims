using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EClaimsWeb.Models
{
    public class PVCClaimsVM
    {
        public PVCClaimsVM()
        {
            pvcClaims = new List<PVCClaimVM>();
            pvcClaimsDrafts = new List<PVCClaimVM>();
        }
        public List<PVCClaimVM> pvcClaims { get; set; }
        public List<PVCClaimVM> pvcClaimsDrafts { get; set; }
    }
}
