using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EClaimsEntities.Models
{
    [Table("MstTBClaim")]
    public class MstTBClaim
    {
        [Key]
        public long TBCID { get; set; }

        [Display(Name = "Claim #")]
        [StringLength(10)]
        public string TBCNo { get; set; }

        [ForeignKey(nameof(MstUser))]
        public int? UserID { get; set; }
        public MstUser MstUser { get; set; }

        [Display(Name = "Month")]
        [Required(ErrorMessage = "Month is required")]
        public int Month { get; set; }

        public int Year { get; set; }

        [StringLength(50)]
        public string Verifier { get; set; }

        [StringLength(50)]
        public string Approver { get; set; }

        [StringLength(50)]
        public string UserApprovers { get; set; }

        [StringLength(50)]
        public string HODApprover { get; set; }

        [StringLength(50)]
        public string DVerifier { get; set; }

        [StringLength(50)]
        public string DApprover { get; set; }

        [StringLength(50)]
        public string DUserApprovers { get; set; }

        [StringLength(50)]
        public string DHODApprover { get; set; }

        [StringLength(250)]
        public string VoidReason { get; set; }

        [StringLength(10)]
        public string FinalApprover { get; set; }

        public int ApprovalStatus { get; set; }

        [StringLength(50)]
        public string VoucherNo { get; set; }

        [Display(Name = "Total Claim")]
        public decimal GrandTotal { get; set; }

        [StringLength(10)]
        public string Company { get; set; }

        [ForeignKey(nameof(MstDepartment))]
        public int? DepartmentID { get; set; }
        public MstDepartment MstDepartment { get; set; }

        [Display(Name = "Facility")]
        [ForeignKey(nameof(MstFacility))]
        public int? FacilityID { get; set; }
        public virtual MstFacility MstFacility { get; set; }

        [Display(Name = "Date Created")]
        public DateTime CreatedDate { get; set; }

        public DateTime ModifiedDate { get; set; }

        public int CreatedBy { get; set; }

        public int ModifiedBy { get; set; }
        public int DelegatedBy { get; set; }

        public DateTime ApprovalDate { get; set; }

        public int ApprovalBy { get; set; }

        public bool TnC { get; set; }
    }
}
