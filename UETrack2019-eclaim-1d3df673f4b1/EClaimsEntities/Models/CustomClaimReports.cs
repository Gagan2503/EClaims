using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EClaimsEntities.Models
{
    [NotMapped]
    public class CustomClaimReports
    {
        public long CID { get; set; }
        public long PVGCItemID { get; set; }

        public string CNO { get; set; }

        public string Name { get; set; }

        public string Phone { get; set; }

        public string ClaimType { get; set; }

        public string Verifier { get; set; }

        public string Approver { get; set; }

        public string UserApprovers { get; set; }

        public string HODApprover { get; set; }

        public string FinalApprover { get; set; }

        public int ApprovalStatus { get; set; }

        public string ParticularsOfPayment { get; set; }

        public decimal GrandTotal { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal ClaimsGrandTotal { get; set; }
        public decimal ClaimsTotalAmount { get; set; }
        public string Company { get; set; }

        public string DepartmentName { get; set; }
        public string FacilityName { get; set; }

        public string CreatedDate { get; set; }
        public bool IsActionAllowed { get; set; }
        public string ClaimStatusName { get; set; }
        public string PayeeName { get; set; }
        public string ChequeNo { get; set; }
        public string InvoiceNo { get; set; }
        public decimal Amount { get; set; }
        public decimal GST { get; set; }
        public int UserID { get; set; }
        public string EmailAddress { get; set; }
        public string PaymentMode { get; set; }
        public string BankAccount { get; set; }
        public string BankSwiftBIC { get; set; }
        public string ExportAccPacDate { get; set; }
        public string ExportBankDate { get; set; }
        public string ApprovalDate { get; set; }
        public bool IsDelegated { get; set; }
        public string VoucherNo { get; set; }
        public string Mobile { get; set; }

        public string DVerifier { get; set; }
        public string DApprover { get; set; }
        public string DUserApprovers { get; set; }
        public string DHODApprover { get; set; }
        public string AccountCode { get; set; }
        public string Description { get; set; }
        public string ExpenseCategory { get; set; }

    }
}
