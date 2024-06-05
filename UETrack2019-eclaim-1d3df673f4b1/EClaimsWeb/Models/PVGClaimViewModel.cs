using EClaimsEntities.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EClaimsWeb.Models
{
    public class PVGClaimViewModel
    {
        public string Company { get; set; }
        public decimal GrandTotal { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal PVGCID { get; set; }
        public string PVGCNo { get; set; }
        public string PaymentMode { get; set; }
        public string VoucherNo { get; set; }
        public List<DtPVGClaim> dtClaims { get; set; }
        public string ClaimAddCondition { get; set; }
        public string UpdateStatus { get; set; }
    }
}
