using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EClaimsWeb.Models
{

    public class ExpenseClaimsVM
    {
        public ExpenseClaimsVM()
        {
            expenseClaims = new List<ExpenseClaimVM>();
            expenseClaimsDrafts = new List<ExpenseClaimVM>();
        }
        public List<ExpenseClaimVM> expenseClaims { get; set; }
        public List<ExpenseClaimVM> expenseClaimsDrafts { get; set; }
    }
}
