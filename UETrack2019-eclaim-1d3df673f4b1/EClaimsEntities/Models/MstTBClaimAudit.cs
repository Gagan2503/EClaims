using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EClaimsEntities.Models
{
    [Table("MstTBClaimAudit")]
   public class MstTBClaimAudit
    {
        [Key]
        public long AuditID { get; set; }

        [ForeignKey(nameof(MstTBClaim))]
        public long TBCID { get; set; }
        public MstTBClaim MstTBClaim { get; set; }

        [StringLength(50)]
        public string Action { get; set; }

        public DateTime AuditDate { get; set; }

        public int AuditBy { get; set; }

        [StringLength(250)]
        public string Reason { get; set; }

        public string Description { get; set; }

        [StringLength(50)]
        public string SentTo { get; set; }


    }
}
