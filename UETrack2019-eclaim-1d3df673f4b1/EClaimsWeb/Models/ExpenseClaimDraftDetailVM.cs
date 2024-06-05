using EClaimsEntities.Models;
using System.Collections.Generic;

namespace EClaimsWeb.Models
{
    public class ExpenseClaimDraftDetailVM
    {
        public ExpenseClaimVM ExpenseClaimVM { get; set; }
        public List<DtExpenseClaimVM> DtExpenseClaimVMs { get; set; }
        public List<DtExpenseClaimSummaryDraft> DtExpenseClaimSummaries { get; set; }
        public List<DtExpenseClaimVM> DtExpenseClaimVMSummary { get; set; }
        //public IEnumerable<IGrouping<int?, DtExpenseClaimVM>> DtExpenseClaimVMsSummary { get; set; }
        public List<ExpenseClaimAuditVM> ExpenseClaimAudits { get; set; }
        public List<DtExpenseClaimFileUpload> ExpenseClaimFileUploads { get; set; }

    }
}
