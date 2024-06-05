using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EClaimsEntities.Models
{
    [Table("MstPVCClaimDraft")]
    public class MstPVCClaimDraft
    {
        [Key]
        public long PVCCID { get; set; }

        [StringLength(10)]
        public string PVCCNo { get; set; }

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

        [StringLength(250)]
        public string VoidReason { get; set; }

        [StringLength(10)]
        public string FinalApprover { get; set; }

        public int ApprovalStatus { get; set; }

        //[StringLength(50)]
        //public string VoucherNo { get; set; }

        [Display(Name = "Grand Total")]
        public decimal GrandTotal { get; set; }

        [Display(Name = "Total Amount with GST")]
        public decimal TotalAmount { get; set; }

        [StringLength(10)]
        public string Company { get; set; }

        [ForeignKey(nameof(MstDepartment))]
        public int? DepartmentID { get; set; }
        public MstDepartment MstDepartment { get; set; }

        [ForeignKey(nameof(MstFacility))]
        public int? FacilityID { get; set; }
        public virtual MstFacility MstFacility { get; set; }

        public DateTime CreatedDate { get; set; }

        public DateTime ModifiedDate { get; set; }

        public int CreatedBy { get; set; }

        public int ModifiedBy { get; set; }

        public DateTime ApprovalDate { get; set; }

        public int ApprovalBy { get; set; }

        public bool TnC { get; set; }
    }
}
