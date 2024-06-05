using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EClaimsEntities.Models
{
    [Table("DtExpenseClaim")]
   public class DtExpenseClaim
    {
        [Key]
        public long ECItemID { get; set; }

        [ForeignKey(nameof(MstExpenseClaim))]

        public long ECID { get; set; }

        public MstExpenseClaim MstExpenseClaim { get; set; }

        public DateTime Date { get; set; }

        public int? FacilityID { get; set; }

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

    }
}
