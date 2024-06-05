using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EClaimsEntities.Models
{
    [Table("DtExpenseClaimDraft")]
    public class DtExpenseClaimDraft
    {
        [Key]
        public long ECItemID { get; set; }

        [ForeignKey(nameof(MstExpenseClaimDraft))]

        public long ECID { get; set; }

        public MstExpenseClaimDraft MstExpenseClaimDraft { get; set; }

        public DateTime Date { get; set; }

        [ForeignKey(nameof(MstFacility))]
        public int? FacilityID { get; set; }
        public MstFacility MstFacility { get; set; }

        [ForeignKey(nameof(MstExpenseCategory))]
        public int? ExpenseCategoryID { get; set; }
        public MstExpenseCategory MstExpenseCategory { get; set; }

        [StringLength(500)]
        public string Description { get; set; }

        public decimal Amount { get; set; }

        public decimal GST { get; set; }
        public decimal GSTPercentage { get; set; }

        [StringLength(100)]
        public string AccountCode { get; set; }
        public int? OrderBy { get; set; }

    }
}
