﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EClaimsEntities.Models
{
    [Table("DtTBClaimSummaryDraft")]
    public class DtTBClaimSummaryDraft
    {
        [Key]
        public long CItemID { get; set; }

        [ForeignKey(nameof(MstTBClaimDraft))]

        public long TBCID { get; set; }

        public MstTBClaimDraft MstTBClaimDraft { get; set; }

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
    }
}