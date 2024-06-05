using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EClaimsWeb.Models
{
    public class DtExpenseClaimVM
    {
        public long ECItemID { get; set; }
        public long ECID { get; set; }
        public DateTime DateOfJourney { get; set; }

        public string Description { get; set; }
        public int? ExpenseCategoryID { get; set; }
        public string ExpenseCategory { get; set; }

        public string AccountCode { get; set; }

        public decimal Amount { get; set; }
        public decimal Gst { get; set; }

        public decimal GSTPercentage { get; set; }
        public decimal AmountWithGST { get; set; }
        public int? FacilityID { get; set; }
        public string Facility { get; set; }
        public int? OrderBy { get; set; }
    }
}
