using EClaimsEntities.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EClaimsWeb.Models
{
    public class MileageClaimViewModel
    {
        public string Company { get; set; }
        public string TravelMode { get; set; }
        public decimal GrandTotal { get; set; }
        public decimal TotalKm { get; set; }
        public string VoucherNo { get; set; }
        public decimal MCID { get; set; }
        public string MCNo { get; set; }
        public List<DtMileageClaimVM> dtClaims { get; set; }
        public string ClaimAddCondition { get; set; }
        //DtMileageClaim[] dtClaims;
    }
}
