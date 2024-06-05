using EClaimsEntities.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EClaimsWeb.Models
{
    public class TBClaimDetailVM
    {
        public DateTime Month { get; set; }
        public TBClaimVM TBClaimVM { get; set; }
        public List<DtTBClaimVM> DtTBClaimVMs { get; set; }
        public List<DtTBClaimSummary> DtTBClaimSummaries { get; set; }
        public List<DtTBClaimVM> DtTBClaimVMSummary { get; set; }
        public List<TBClaimAuditVM> TBClaimAudits { get; set; }
        public List<DtTBClaimFileUpload> TBClaimFileUploads { get; set; }
    }
}
