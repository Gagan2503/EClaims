using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EClaimsEntities.Models
{
    [Table("MstApprovalMatrix")]
    public class MstApprovalMatrix
    {
        [Key]
        public int AMID { get; set; }
        [Display(Name = "Approval Required", Prompt = "Approval Required")]
        public bool ApprovalRequired { get; set; }
        [Display(Name = "Verification Levels", Prompt = "Verification Levels")]
        public int VerificationLevels { get; set; }
        [Display(Name = "Approval Levels", Prompt = "Approval Levels")]
        public int ApprovalLevels { get; set; }
        public DateTime CreatedDate { get; set; }

        public DateTime ModifiedDate { get; set; }

        public int CreatedBy { get; set; }

        public int ModifiedBy { get; set; }

        [ForeignKey(nameof(MstScreens))]
        public int? ScreenID { get; set; }
        public MstScreens MstScreens { get; set; }

        [ForeignKey(nameof(MstDepartment))]
        public int? DepartmentID { get; set; }
        public MstDepartment MstDepartment { get; set; }
    }
}
