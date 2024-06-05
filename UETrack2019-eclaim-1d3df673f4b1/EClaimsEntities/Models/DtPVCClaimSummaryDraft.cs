using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EClaimsEntities.Models
{
    [Table("DtPVCClaimSummaryDraft")]
    public class DtPVCClaimSummaryDraft
    {
        [Key]
        public long CItemID { get; set; }

        [ForeignKey(nameof(MstPVCClaimDraft))]

        public long PVCCID { get; set; }

        public MstPVCClaimDraft MstPVCClaimDraft { get; set; }

        public DateTime Date { get; set; }

        [StringLength(500)]
        public string Description { get; set; }

        [StringLength(100)]
        public string ExpenseCategory { get; set; }

        [StringLength(100)]
        public string AccountCode { get; set; }
        public decimal TaxClass { get; set; }
        public decimal Amount { get; set; }

        public decimal GST { get; set; }

        public decimal AmountWithGST { get; set; }

        public decimal GSTPercentage { get; set; }
    }
}
