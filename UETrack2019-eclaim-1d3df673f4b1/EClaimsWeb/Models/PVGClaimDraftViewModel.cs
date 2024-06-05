using EClaimsEntities.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EClaimsWeb.Models
{
    public class PVGClaimDraftViewModel
    {
        public string Company { get; set; }
        public decimal GrandTotal { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal PVGCID { get; set; }
        public string PVGCNo { get; set; }
        public string PaymentMode { get; set; }
        public string VoucherNo { get; set; }
        public List<DtPVGClaimDraft> dtClaims { get; set; }
    }
}
