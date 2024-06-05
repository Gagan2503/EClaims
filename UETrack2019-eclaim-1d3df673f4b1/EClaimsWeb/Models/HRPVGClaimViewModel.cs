using EClaimsEntities.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EClaimsWeb.Models
{
    public class HRPVGClaimViewModel
    {
        public string Company { get; set; }
        public decimal GrandTotal { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal HRPVGCID { get; set; }
        public string HRPVGCNo { get; set; }
        public string VoucherNo { get; set; }
        public string ChequeNo { get; set; }
        public string ParticularsOfPayment { get; set; }
        public decimal Amount { get; set; }
        public string PaymentMode { get; set; }
        public string ClaimAddCondition { get; set; }
        public List<DtHRPVGClaim> dtClaims { get; set; }
    }
}
