﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EClaimsEntities.Models
{
    [Table("MstExpenseClaimAudit")]
   public class MstExpenseClaimAudit
    {
        [Key]
        public long AuditID { get; set; }

        [ForeignKey(nameof(MstExpenseClaim))]
        public long ECID { get; set; }
        public MstExpenseClaim MstExpenseClaim { get; set; }

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
