using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EClaimsEntities.Models
{
    [Table("MstExpenseClaimDraft")]
    public class MstExpenseClaimDraft
    {

        [Key]
        public long ECID { get; set; }

        [Display(Name = "Claim #")]
        [StringLength(10)]
        public string ECNo { get; set; }

        [ForeignKey(nameof(MstUser))]
        public int? UserID { get; set; }
        public MstUser MstUser { get; set; }

        [Display(Name = "Claim Type")]
        [StringLength(25)]
        public string ClaimType { get; set; }

        [StringLength(50)]
        public string Verifier { get; set; }

        [StringLength(50)]
        public string Approver { get; set; }

        [StringLength(50)]
        public string UserApprovers { get; set; }

        [StringLength(50)]
        public string HODApprover { get; set; }

        [StringLength(250)]
        public string VoidReason { get; set; }

        [StringLength(10)]
        public string FinalApprover { get; set; }

        public int ApprovalStatus { get; set; }

        [Display(Name = "Grand Total")]
        public decimal GrandTotal { get; set; }

        [Display(Name = "Total Claim")]
        public decimal TotalAmount { get; set; }

        [StringLength(10)]
        public string Company { get; set; }

        [ForeignKey(nameof(MstDepartment))]
        public int? DepartmentID { get; set; }
        public MstDepartment MstDepartment { get; set; }

        [ForeignKey(nameof(MstFacility))]
        public int? FacilityID { get; set; }
        public virtual MstFacility MstFacility { get; set; }

        [Display(Name = "Date Created")]
        public DateTime CreatedDate { get; set; }

        public DateTime ModifiedDate { get; set; }

        public int CreatedBy { get; set; }

        public int ModifiedBy { get; set; }

        public DateTime ApprovalDate { get; set; }

        public int ApprovalBy { get; set; }

        public bool TnC { get; set; }
    }
}
