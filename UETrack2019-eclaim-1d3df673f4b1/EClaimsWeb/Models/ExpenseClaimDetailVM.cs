using EClaimsEntities.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EClaimsWeb.Models
{
    public class ExpenseClaimDetailVM
    {
        public ExpenseClaimVM ExpenseClaimVM { get; set; }
        public List<DtExpenseClaimVM> DtExpenseClaimVMs { get; set; }
        public List<DtExpenseClaimSummary> DtExpenseClaimSummaries { get; set; }
        public List<DtExpenseClaimVM> DtExpenseClaimVMSummary { get; set; }
        //public IEnumerable<IGrouping<int?, DtExpenseClaimVM>> DtExpenseClaimVMsSummary { get; set; }
        public List<ExpenseClaimAuditVM> ExpenseClaimAudits { get; set; }
        public List<DtExpenseClaimFileUpload> ExpenseClaimFileUploads { get; set; }

    }
}
