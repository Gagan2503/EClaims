using EClaimsEntities.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EClaimsWeb.Models
{
    public class ExpenseClaimViewModel
    {
        public string Company { get; set; }
        public string ClaimType { get; set; }
        public decimal GrandTotal { get; set; }
        public decimal TotalAmount { get; set; }
        public  decimal ECID { get; set; }
        public string ECNo { get; set; }
        public string VoucherNo { get; set; }
        public List<DtExpenseClaim> dtClaims { get; set; }
        public string ClaimAddCondition { get; set; }
        public string UpdateStatus { get; set; }
    }
}
