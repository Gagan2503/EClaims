using EClaimsEntities.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EClaimsWeb.Models
{
    public class HRPVCClaimsVM
    {
        public HRPVCClaimsVM()
        {
            hRPvcClaims = new List<CustomHRPVCClaim>();
            hRPvcClaimsDrafts = new List<CustomHRPVCClaim>();
        }
        public List<CustomHRPVCClaim> hRPvcClaims { get; set; }
        public List<CustomHRPVCClaim> hRPvcClaimsDrafts { get; set; }
    }
}
