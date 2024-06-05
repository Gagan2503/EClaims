using EClaimsEntities.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EClaimsWeb.Models
{
    public class PVCClaimViewModel
    {
        public string Company { get; set; }
        public decimal GrandTotal { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal PVCCID { get; set; }
        public string PVCCNo { get; set; }
        public string VoucherNo { get; set; }
        public List<DtPVCClaim> dtClaims { get; set; }
        public List<DtPVCClaimDraft> dtClaimsDraft { get; set; }
        public string ClaimAddCondition { get; set; }

        public string UpdateStatus { get; set; }

    }
}
