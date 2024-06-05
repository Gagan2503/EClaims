using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EClaimsEntities.Models
{
    [Table("MstTaxClass")]
    public class MstTaxClass
    {
        [Key]
        public int TaxClassID { get; set; }
        public int Code { get; set; }
        public decimal TaxClass { get; set; }

        public string Description { get; set; }
        public bool IsActive { get; set; }

        public bool IsDefault { get; set; }
        public bool IsOptional { get; set; }
        public int? OptionalTaxClassID { get; set; }
        // public decimal OptionalTaxClass { get; set; }

        public DateTime? CreatedDate { get; set; }

        public DateTime? ModifiedDate { get; set; }

        public int? CreatedBy { get; set; }

        public int? ModifiedBy { get; set; }

        public DateTime ApprovalDate { get; set; }

        public int ApprovalStatus { get; set; }

        public int ApprovalBy { get; set; }

        //public string Reason { get; set; }

    }
}
