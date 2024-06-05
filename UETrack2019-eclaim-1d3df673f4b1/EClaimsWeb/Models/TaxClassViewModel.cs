using EClaimsEntities.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace EClaimsWeb.Models
{
    public class TaxClassViewModel
    {
        [Key]
        public int TaxClassID { get; set; }

        [Required(ErrorMessage = "Code is required")]
        [StringLength(4, ErrorMessage = "Code can't be longer than 4 characters")]
        public string Code { get; set; }

        [Display(Name = "Tax Class")]
        [StringLength(50, ErrorMessage = "TaxClass can't be longer than 50 characters")]
        public string TaxClass { get; set; }

        [Required(ErrorMessage = "Description is required")]
        [StringLength(50, ErrorMessage = "Description can't be longer than 50 characters")]
        public string Description { get; set; }

        [Display(Name = "Is Active")]
        [Required(ErrorMessage = "Is Active is required")]
        public bool IsActive { get; set; }

        [Display(Name = "Is Default")]
        [Required(ErrorMessage = "Is Default is required")]
        public bool IsDefault { get; set; }

        [Display(Name = "Is Optional")]
        [Required(ErrorMessage = "Is Optional is required")]
        public bool IsOptional { get; set; }

        [Display(Name = "Optional Tax Class")]
        [Required(ErrorMessage = "Optional TaxClass is required")]
        public decimal OptionalTaxClass { get; set; }
        public int OptionalTaxClassID { get; set; }


        public DateTime? CreatedDate { get; set; }

        //public DateTime ModifiedDate { get; set; }

        public int? CreatedBy { get; set; }

        //public int ModifiedBy { get; set; }

        //public DateTime ApprovalDate { get; set; }


        //public int ApprovalStatus { get; set; }


        //public int ApprovalBy { get; set; }

        //public string Reason { get; set; }
    }
}
