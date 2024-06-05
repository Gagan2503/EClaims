using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EClaimsEntities.Models
{
    [Table("DtExpenseClaimFileUpload")]
   public class DtExpenseClaimFileUpload
    {

        [Key]
        public long FileID { get; set; }

        [ForeignKey(nameof(MstExpenseClaim))]
        public long ECID { get; set; }
        public MstExpenseClaim MstExpenseClaim { get; set; }

        [StringLength(250)]
        public string FileName { get; set; }

        [StringLength(250)]
        public string FilePath { get; set; }

        public DateTime CreatedDate { get; set; }

        public DateTime ModifiedDate { get; set; }

        public int CreatedBy { get; set; }

        public int ModifiedBy { get; set; }

        public bool IsDeleted { get; set; }

    }
}
