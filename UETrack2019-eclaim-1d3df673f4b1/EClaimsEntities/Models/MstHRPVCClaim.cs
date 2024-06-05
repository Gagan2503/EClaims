using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EClaimsEntities.Models
{
    [Table("MstHRPVCClaim")]
    public class MstHRPVCClaim
    {
        [Key]
        public long HRPVCCID { get; set; }

        [StringLength(10)]
        public string HRPVCCNo { get; set; }

        [ForeignKey(nameof(MstUser))]
        public int? UserID { get; set; }
        public MstUser MstUser { get; set; }

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

        [StringLength(50)]
        public string ChequeNo { get; set; }

        [StringLength(500)]
        public string ParticularsOfPayment { get; set; }

        public decimal Amount { get; set; } 

        [Display(Name = "Grand Total")]
        public decimal GrandTotal { get; set; }

        [Display(Name = "Total Amount with GST")]
        public decimal TotalAmount { get; set; }

        [ForeignKey(nameof(MstFacility))]
        public int? FacilityID { get; set; }
        public virtual MstFacility MstFacility { get; set; }

        [ForeignKey(nameof(MstDepartment))]
        public int? DepartmentID { get; set; }
        public MstDepartment MstDepartment { get; set; }

        [StringLength(10)]
       
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
