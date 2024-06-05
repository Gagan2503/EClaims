using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EClaimsWeb.Models
{
    public class PVCClaimAuditVM
    {
        public long AuditID { get; set; }

        public long PVCCID { get; set; }
        public string Action { get; set; }

        public DateTime AuditDate { get; set; }

        public int AuditBy { get; set; }

        public string Reason { get; set; }

        public string Description { get; set; }

        public string SentTo { get; set; }
        public string AuditDateTickle { get; set; }
    }
}
