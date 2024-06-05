using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EClaimsWeb.Models
{
    public class HRPVCClaimVM
    {
        public long HRPVCCID { get; set; }

        public string HRPVCCNo { get; set; }

        public string Name { get; set; }
        public string Phone { get; set; }

        public string VoucherNo { get; set; }
        public string ChequeNo { get; set; }

         public string ParticularsOfPayment { get; set; }

        public decimal Amount { get; set; }

        public string Verifier { get; set; }

        public string Approver { get; set; }

        //        public string FinalApprover { get; set; }

        public int ApprovalStatus { get; set; }

        public decimal GrandTotal { get; set; }
        public decimal TotalAmount { get; set; }

        public string Company { get; set; }

        public string DepartmentName { get; set; }
        public string FacilityName { get; set; }

        public string CreatedDate { get; set; }
        public bool IsActionAllowed { get; set; }
        public string ExpenseStatusName { get; set; }

        public string PayeeName { get; set; }
        public string AVerifier { get; set; }
        public string AApprover { get; set; }
        public string AUserApprovers { get; set; }
        public string AHODApprover { get; set; }
        public string DVerifier { get; set; }
        public string DApprover { get; set; }
        public string DUserApprovers { get; set; }
        public string DHODApprover { get; set; }

    }
}
