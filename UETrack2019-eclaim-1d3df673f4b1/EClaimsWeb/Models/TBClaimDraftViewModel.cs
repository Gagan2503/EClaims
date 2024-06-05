using EClaimsEntities.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EClaimsWeb.Models
{
    public class TBClaimDraftViewModel
    {
        public string Company { get; set; }
        public DateTime Month { get; set; }
        public decimal GrandTotal { get; set; }
        public decimal TotalAmount { get; set; }

        public decimal TBCID { get; set; }
        public string TBCNo { get; set; }

        public List<DtTBClaimDraft> dtClaims { get; set; }
    }
}
