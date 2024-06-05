using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EClaimsEntities.Models
{
    [Table("MstCostStructure")]
    public class MstCostStructure
    {
        [Key]
        public int CostStructureID { get; set; }

        [Required(ErrorMessage = "Code is required")]
        [StringLength(10, ErrorMessage = "Code can't be longer than 10 characters")]
        public string Code { get; set; }

        [Display(Name = "Cost Structure")]
        [Required(ErrorMessage = "CostStructure is required")]
        [StringLength(50, ErrorMessage = "CostStructure can't be longer than 50 characters")]
        public string CostStructure { get; set; }

        [Required(ErrorMessage = "Is Active is required")]
        public bool IsActive { get; set; }


        public DateTime CreatedDate { get; set; }

        public DateTime ModifiedDate { get; set; }

        public int CreatedBy { get; set; }

        public int ModifiedBy { get; set; }

        public DateTime ApprovalDate { get; set; }


        public int ApprovalStatus { get; set; }


        public int ApprovalBy { get; set; }

        public string Reason { get; set; }
    }
}
