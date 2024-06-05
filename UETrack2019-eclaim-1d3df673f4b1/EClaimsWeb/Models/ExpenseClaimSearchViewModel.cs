using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EClaimsWeb.Models
{
    public class ExpenseClaimSearchViewModel
    {
        public List<ExpenseClaimVM> expenseClaimVMs { get; set; }
        //public SelectList Screens { get; set; }
        //public string ScreenId { get; set; }
        public IEnumerable<SelectListItem> Users { get; set; }
        public int? UserID { get; set; }

        public IEnumerable<SelectListItem> Facilities { get; set; }

        public int? FacilityID { get; set; }

        public IEnumerable<SelectListItem> ExpenseTypes { get; set; }

        public int? ExpenseID { get; set; }

        public IEnumerable<SelectListItem> Statuses { get; set; }
        public int? StatusID { get; set; }

        public string FromDate { get; set; }
        public string ToDate { get; set; }
    }
}
