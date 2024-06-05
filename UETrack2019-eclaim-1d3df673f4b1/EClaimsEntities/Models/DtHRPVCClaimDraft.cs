using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EClaimsEntities.Models
{
    [Table("DtHRPVCClaimDraft")]
   public class DtHRPVCClaimDraft
    {
        [Key]
        public long HRPVCCItemID { get; set; }

        [ForeignKey(nameof(MstHRPVCClaim))]

        public long HRPVCCID { get; set; }

        public MstHRPVCClaim MstHRPVCClaim { get; set; }

        public DateTime Date { get; set; }
        public int? FacilityID { get; set; }

        [StringLength(50)]
        public string ChequeNo { get; set; }

        [StringLength(50)]
        public string StaffName { get; set; }

        [StringLength(500)]
        public string Reason { get; set; }

        [StringLength(50)]
        public string EmployeeNo { get; set; }

        //[ForeignKey(nameof(MstFacility))]
        //public int? FacilityID { get; set; }
        //public virtual MstFacility MstFacility { get; set; }

        [StringLength(50)]
        public string Facility { get; set; }

        public decimal Amount { get; set; }

        public decimal GST { get; set; }

        [StringLength(100)]
        public string AccountCode { get; set; }

    }
}
