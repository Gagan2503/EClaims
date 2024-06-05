using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EClaimsWeb.Models
{
    public class ExpenseClaimSearch
    {
        public int UserID { get; set; }
        public int FacilityID { get; set; }
        public int StatusID { get; set; }
        public string ExpenseID { get; set; }
        public string FromDate { get; set; }
        public string ToDate { get; set; }
    }
}
