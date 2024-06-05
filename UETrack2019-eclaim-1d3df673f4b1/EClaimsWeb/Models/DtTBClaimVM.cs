using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EClaimsWeb.Models
{
    public class DtTBClaimVM
    {
        public long TBCItemID { get; set; }
        public long TBCID { get; set; }
        public DateTime DateOfJourney { get; set; }
        public string AccountCode { get; set; }
        public string Description { get; set; }

        public int? ExpenseCategoryID { get; set; }
        public string ExpenseCategory { get; set; }

        public decimal Amount { get; set; }
        public decimal Gst { get; set; }
        public int? FacilityID { get; set; }
        public string Facility { get; set; }
        public int? OrderBy { get; set; }
    }
}
