using EClaimsEntities.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EClaimsWeb.Models
{
    public class HRPVCClaimViewModel
    {
        public string Company { get; set; }
        public decimal GrandTotal { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal HRPVCCID { get; set; }
        public string HRPVCCNo { get; set; }
        public string VoucherNo { get; set; }
        public string ChequeNo { get; set; }
        public string ParticularsOfPayment { get; set; }
        public decimal Amount { get; set; }
        public string ClaimAddCondition { get; set; }
        public List<DtHRPVCClaim> dtClaims { get; set; }
    }
}
