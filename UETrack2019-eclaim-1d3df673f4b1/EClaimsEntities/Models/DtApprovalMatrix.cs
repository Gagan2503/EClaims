using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EClaimsEntities.Models
{
    [Table("DtApprovalMatrix")]
    public class DtApprovalMatrix
    {
        [Key]
        public int DTAMID { get; set; }
        public int Verifier { get; set; }
        public int Approver { get; set; }
        [StringLength(20)]
        public string AmountFrom { get; set; }
        [StringLength(20)]
        public string AmountTo { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime ModifiedDate { get; set; }
        public int CreatedBy { get; set; }
        public int ModifiedBy { get; set; }

        [ForeignKey(nameof(MstApprovalMatrix))]
        public int AMID { get; set; }
        public MstApprovalMatrix MstApprovalMatrix { get; set; }
        public bool IsDeleted { get; set; }
    }
}
