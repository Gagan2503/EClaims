using EClaimsEntities.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EClaimsWeb.Models
{
    public class PVGClaimDetailVM
    {
        public PVGClaimVM PVGClaimVM { get; set; }
        public List<DtPVGClaimVM> DtPVGClaimVMs { get; set; }
        public List<DtPVGClaimSummary> DtPVGClaimSummaries { get; set; }
        public List<DtPVGClaimVM> DtPVGClaimVMSummary { get; set; }
        //public IEnumerable<IGrouping<int?, DtExpenseClaimVM>> DtExpenseClaimVMsSummary { get; set; }
        public List<PVGClaimAuditVM> PVGClaimAudits { get; set; }
        public List<DtPVGClaimFileUpload> PVGClaimFileUploads { get; set; }
    }
}
