using EClaimsEntities.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EClaimsWeb.Models
{
    public class PVCClaimDetailVM
    {
        public PVCClaimVM PVCClaimVM { get; set; }
        public List<DtPVCClaimVM> DtPVCClaimVMs { get; set; }
        public List<DtPVCClaimSummary> DtPVCClaimSummaries { get; set; }
        public List<DtPVCClaimDraftVM> DtPVCClaimDraftVMs { get; set; }
        public List<DtPVCClaimDraftVM> DtPVCClaimDraftVMSummary { get; set; }
        public List<DtPVCClaimVM> DtPVCClaimVMSummary { get; set; }
        //public IEnumerable<IGrouping<int?, DtPVCClaimVM>> DtExpenseClaimVMsSummary { get; set; }
        public List<PVCClaimAuditVM> PVCClaimAudits { get; set; }
        public List<DtPVCClaimFileUpload> PVCClaimFileUploads { get; set; }
    }
}
