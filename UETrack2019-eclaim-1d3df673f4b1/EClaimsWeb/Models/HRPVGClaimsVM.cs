using EClaimsEntities.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EClaimsWeb.Models
{
    public class HRPVGClaimsVM
    {
        public HRPVGClaimsVM()
        {
            hRPvcClaims = new List<CustomHRPVGClaim>();
            hRPvcClaimsDrafts = new List<CustomHRPVGClaim>();
        }
        public List<CustomHRPVGClaim> hRPvcClaims { get; set; }
        public List<CustomHRPVGClaim> hRPvcClaimsDrafts { get; set; }
    }
}
