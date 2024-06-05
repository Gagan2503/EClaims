using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EClaimsEntities.Models
{
    [Table("MstExpenseCategory")]
    public class MstExpenseCategory
    {
        [Key]
        public int ExpenseCategoryID { get; set; }

        [Display(Name = "Category Code")]
        [Required(ErrorMessage = "Category Code is required")]
        [StringLength(10, ErrorMessage = "Category Code can't be longer than 10 characters")]
        public string CategoryCode { get; set; }

        [Display(Name = "Category Description")]
        [Required(ErrorMessage = "Category Description is required")]
        [StringLength(50, ErrorMessage = "Category Description can't be longer than 50 characters")]
        public string Description { get; set; }

        [Required(ErrorMessage = "Default is required")]
        [StringLength(10, ErrorMessage = "Default can't be longer than 10 characters")]
        public string Default { get; set; }

        [Display(Name = "Expense Code")]
        [Required(ErrorMessage = "Expense Code is required")]
        [StringLength(10, ErrorMessage = "Expense Code can't be longer than 10 characters")]
        public string ExpenseCode { get; set; }

        [Display(Name = "Is GST Required?")]
        [Required(ErrorMessage = "GST is required")]
        public bool IsGSTRequired { get; set; }

        [Display(Name = "Is Active")]
        [Required(ErrorMessage = "Is Active is required")]
        public bool IsActive { get; set; }


        public DateTime CreatedDate { get; set; }

        public DateTime ModifiedDate { get; set; }

        public int CreatedBy { get; set; }

        public int ModifiedBy { get; set; }

        public DateTime ApprovalDate { get; set; }

        [Display(Name = "Approval Status")]
        public int ApprovalStatus { get; set; }


        public int ApprovalBy { get; set; }

        public string Reason { get; set; }

        [Display(Name = "Claim Type")]
        [ForeignKey(nameof(MstClaimType))]
        public int ClaimTypeID { get; set; }
        public MstClaimType MstClaimType { get; set; }


        [Display(Name = "Cost Type")]
        [ForeignKey(nameof(MstCostType))]
        public int CostTypeID { get; set; }
        public MstCostType MstCostType { get; set; }

        [Display(Name = "Cost Structure")]
        [ForeignKey(nameof(MstCostStructure))]
        public int CostStructureID { get; set; }
        public MstCostStructure MstCostStructure { get; set; }
    }
}
