using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EClaimsEntities.Models
{
    [Table("DtHRPVGClaimDraft")]
   public class DtHRPVGClaimDraft
    {
        [Key]
        public long HRPVGCItemID { get; set; }

        [ForeignKey(nameof(MstHRPVGClaim))]

        public long HRPVGCID { get; set; }

        public MstHRPVGClaim MstHRPVGClaim { get; set; }

        public DateTime Date { get; set; }


        [StringLength(50)]
        public string ChequeNo { get; set; }

        [StringLength(50)]
        public string StaffName { get; set; }

        [StringLength(500)]
        public string Reason { get; set; }

        [StringLength(50)]
        public string EmployeeNo { get; set; }

        public int? FacilityID { get; set; }

        //[ForeignKey(nameof(MstFacility))]
        //public int? FacilityID { get; set; }
        //public virtual MstFacility MstFacility { get; set; }
        [StringLength(50)]
        public string Facility { get; set; }

        public decimal Amount { get; set; }

        public decimal GST { get; set; }

        [StringLength(100)]
        public string AccountCode { get; set; }

        [StringLength(50)]
        public string Bank { get; set; }

        [StringLength(50)]
        public string BankCode { get; set; }

        [StringLength(50)]
        public string BankSwiftBIC { get; set; }

        [StringLength(50)]
        public string BranchCode { get; set; }

        [StringLength(50)]
        public string BankAccount { get; set; }

        [StringLength(50)]
        public string Mobile { get; set; }

    }
}
