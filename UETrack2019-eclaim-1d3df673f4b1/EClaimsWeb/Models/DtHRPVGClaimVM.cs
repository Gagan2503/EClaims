using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EClaimsWeb.Models
{
    public class DtHRPVGClaimVM
    {
        public long HRPVGCItemID { get; set; }
        public long HRPVGCID { get; set; }
        public DateTime Date { get; set; }
        public int? ExpenseCategoryID { get; set; }
        public string ExpenseCategory { get; set; }
        public string AccountCode { get; set; }
        public string ChequeNo { get; set; }
        public string StaffName { get; set; }

        public string Reason { get; set; }

        public string EmployeeNo { get; set; }

        public int? FacilityID { get; set; }
        public string Facility { get; set; }
        public decimal Amount { get; set; }
        public decimal GST { get; set; }
        public decimal AmountWithGST { get; set; }
        public string Particulars { get; set; }
        public string Bank { get; set; }
        public string BankCode { get; set; }
        public string BankSWIFTBIC { get; set; }
        public string BranchCode { get; set; }
        public string BankAccount { get; set; }
        public string Mobile { get; set; }
    }
}
