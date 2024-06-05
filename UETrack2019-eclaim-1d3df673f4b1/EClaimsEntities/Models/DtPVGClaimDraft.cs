using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EClaimsEntities.Models
{
    [Table("DtPVGClaimDraft")]
    public class DtPVGClaimDraft
    {
        [Key]
        public long PVGCItemID { get; set; }

        [ForeignKey(nameof(MstPVGClaimDraft))]

        public long PVGCID { get; set; }

        public MstPVGClaimDraft MstPVGClaimDraft { get; set; }

        public DateTime Date { get; set; }

        [ForeignKey(nameof(MstExpenseCategory))]
        public int? ExpenseCategoryID { get; set; }
        public MstExpenseCategory MstExpenseCategory { get; set; }

        [ForeignKey(nameof(MstFacility))]
        public int? FacilityID { get; set; }
        public MstFacility MstFacility { get; set; }

        [StringLength(50)]
        public string ChequeNo { get; set; }

        [StringLength(500)]
        public string Particulars { get; set; }

        [StringLength(50)]
        public string Payee { get; set; }

        [StringLength(50)]
        public string InvoiceNo { get; set; }

        public decimal Amount { get; set; }

        public decimal GST { get; set; }
        public decimal GSTPercentage { get; set; }
        public string AccountCode { get; set; }

        [StringLength(50)]
        public string Bank { get; set; }

        [StringLength(50)]
        public string BankCode { get; set; }

        [StringLength(50)]
        public string BankSwiftBIC { get; set; }

        [StringLength(50)]
        public string BranchCode { get; set; }

        [StringLength(50)]
        public string BankAccount { get; set; }

        [StringLength(50)]
        public string Mobile { get; set; }
        public int? OrderBy { get; set; }
    }
}
