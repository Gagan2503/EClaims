using EClaimsEntities.Models;
using System.Collections.Generic;

namespace EClaimsWeb.Models
{
    public class ExpenseClaimDraftViewModel
    {
        public string Company { get; set; }
        public string ClaimType { get; set; }
        public decimal GrandTotal { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal ECID { get; set; }
        public string ECNo { get; set; }
        public List<DtExpenseClaimDraft> dtClaims { get; set; }
    }
}
