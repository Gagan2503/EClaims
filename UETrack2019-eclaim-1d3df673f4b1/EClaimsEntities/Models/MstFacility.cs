using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EClaimsEntities.Models
{
    [Table("MstFacility")]
    public class MstFacility
    {
        [Key]
        public int FacilityID { get; set; }

        [Required(ErrorMessage = "Code is required")]
        [StringLength(10, ErrorMessage = "Code can't be longer than 10 characters")]
        public string Code { get; set; }

        [Display(Name = "Facility Name")]
        [Required(ErrorMessage = "Facility Name is required")]
        [StringLength(50, ErrorMessage = "FacilityName can't be longer than 50 characters")]
        public string FacilityName { get; set; }

        [Display(Name = "Is Active")]
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

        [Display(Name = "Department")]
        [ForeignKey(nameof(MstDepartment))]
        public int? DepartmentID { get; set; }
        public virtual MstDepartment MstDepartment { get; set; }
        [Display(Name = "HOD Approver")]
        public int? UserID { get; set; }
    }
}
