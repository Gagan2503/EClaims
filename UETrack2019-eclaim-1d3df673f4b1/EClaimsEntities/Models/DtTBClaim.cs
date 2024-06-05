using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EClaimsEntities.Models
{
    [Table("DtTBClaim")]
   public class DtTBClaim
    {
        [Key]
        public long TBCItemID { get; set; }

        [ForeignKey(nameof(MstTBClaim))]

        public long TBCID { get; set; }

        public MstTBClaim MstTBClaim { get; set; }

        public DateTime Date { get; set; }

        public int? FacilityID { get; set; }

        [ForeignKey(nameof(MstExpenseCategory))]
        public int? ExpenseCategoryID { get; set; }
        public MstExpenseCategory MstExpenseCategory { get; set; }

        [StringLength(500)]
        public string Description { get; set; }

        public decimal Amount { get; set; }

        [StringLength(100)]
        public string AccountCode { get; set; }


    }
}
