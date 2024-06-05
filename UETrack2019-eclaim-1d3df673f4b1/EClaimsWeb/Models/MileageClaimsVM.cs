using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EClaimsWeb.Models
{
    public class MileageClaimsVM
    {
        public MileageClaimsVM()
        {
            mileageClaims = new List<MileageClaimVM>();
            mileageClaimsDrafts = new List<MileageClaimVM>();
        }
        public List<MileageClaimVM> mileageClaims { get; set; }
        public List<MileageClaimVM> mileageClaimsDrafts { get; set; }
    }
}
