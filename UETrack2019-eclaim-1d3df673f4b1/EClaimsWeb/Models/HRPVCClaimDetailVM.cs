using EClaimsEntities.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EClaimsWeb.Models
{
    public class HRPVCClaimDetailVM
    {
        public HRPVCClaimVM HRPVCClaimVM { get; set; }
        public List<DtHRPVCClaimVM> DtHRPVCClaimVMs { get; set; }
        public List<DtHRPVCClaimVM> DtHRPVCClaimDraftVMs { get; set; }
        public List<DtHRPVCClaimSummary> DtHRPVCClaimSummaries { get; set; }
        public List<DtHRPVCClaimVM> DtHRPVCClaimVMSummary { get; set; }

        //public IEnumerable<IGrouping<int?, DtExpenseClaimVM>> DtExpenseClaimVMsSummary { get; set; }
        public List<HRPVCClaimAuditVM> HRPVCClaimAudits { get; set; }
        public List<DtHRPVCClaimFileUpload> HRPVCClaimFileUploads { get; set; }
        public List<DtHRPVCClaimFileUploadDraft> HRPVCClaimDraftFileUploads { get; set; }
    }
}
