using EClaimsEntities.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EClaimsWeb.Models
{
    public class HRPVGClaimDetailVM
    {
        public HRPVGClaimVM HRPVGClaimVM { get; set; }
        public List<DtHRPVGClaimVM> DtHRPVGClaimVMs { get; set; }
        public List<DtHRPVGClaimDraftVM> DtHRPVGClaimDraftVMs { get; set; }
        public List<DtHRPVGClaimSummary> DtHRPVGClaimSummaries { get; set; }
        public List<DtHRPVGClaimVM> DtHRPVGClaimVMSummary { get; set; }
        public List<DtHRPVGClaimDraftVM> DtHRPVGClaimDraftVMSummary { get; set; }
        //public IEnumerable<IGrouping<int?, DtExpenseClaimVM>> DtExpenseClaimVMsSummary { get; set; }
        public List<HRPVGClaimAuditVM> HRPVGClaimAudits { get; set; }
        public List<DtHRPVGClaimFileUpload> HRPVGClaimFileUploads { get; set; }
    }
}
