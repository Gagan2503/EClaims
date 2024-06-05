using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EClaimsEntities.Models
{
    [Table("DtMileageClaimDraft")]
    public class DtMileageClaimDraft
    {
        [Key]
        public long MCItemID { get; set; }

        [ForeignKey(nameof(MstMileageClaim))]
        public long MCID { get; set; }
        public MstMileageClaimDraft MstMileageClaim { get; set; }

        public DateTime DateOfJourney { get; set; }

        [ForeignKey(nameof(MstFacility))]
        public int? FacilityID { get; set; }
        public MstFacility MstFacility { get; set; }

        [StringLength(50)]
        public string FromFacilityID { get; set; }

        [StringLength(50)]
        public string ToFacilityID { get; set; }

        public DateTime InTime { get; set; }

        public DateTime OutTime { get; set; }

        [StringLength(10)]
        public decimal StartReading { get; set; }

        [StringLength(10)]
        public decimal EndReading { get; set; }

        public decimal Kms { get; set; }

        [StringLength(500)]
        public string Remark { get; set; }

        [StringLength(10)]
        public decimal Amount { get; set; }

        [StringLength(100)]
        public string AccountCode { get; set; }
        public int? OrderBy { get; set; }
    }
}
