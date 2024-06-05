using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EClaimsWeb.Models
{
    public class DtPVCClaimDraftVM
    {
        public long PVCCItemID { get; set; }
        public long PVCCID { get; set; }
        public DateTime Date { get; set; }
        public int? ExpenseCategoryID { get; set; }
        public string ExpenseCategory { get; set; }
        public string AccountCode { get; set; }
        public string ChequeNo { get; set; }
        public string Particulars { get; set; }
        public string Payee { get; set; }
        public string InvoiceNo { get; set; }
        public decimal Amount { get; set; }
        public decimal GST { get; set; }
        public decimal AmountWithGST { get; set; }
        public int? FacilityID { get; set; }
        public string Facility { get; set; }
        public int? OrderBy { get; set; }

        public decimal GSTPercentage { get; set; }
    }
}

