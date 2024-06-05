using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EClaimsWeb.Models
{
    public class APExportSearch
    {
        public string UserID { get; set; }
        public string ModuleName { get; set; }
        public string FacilityID { get; set; }
        public string StatusID { get; set; }
        public string FromDate { get; set; }
        public string ToDate { get; set; }
        public string ClaimIds { get; set; }
        public string Reason { get; set; }
        public string ApprovedStatus { get; set; }
        public string FromPage { get; set; }
    }
}
