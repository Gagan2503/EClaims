using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EClaimsEntities.Models
{
    [Table("DtMileageClaimFileUploadDraft")]
    public class DtMileageClaimFileUploadDraft
    {
        [Key]
        public long FileID { get; set; }

        [ForeignKey(nameof(MstMileageClaim))]
        public long MCID { get; set; }
        public MstMileageClaimDraft MstMileageClaim { get; set; }

        [StringLength(250)]
        public string FileName { get; set; }

        [StringLength(250)]
        public string FilePath { get; set; }

        public DateTime CreatedDate { get; set; }

        public DateTime ModifiedDate { get; set; }

        public int CreatedBy { get; set; }

        public int ModifiedBy { get; set; }

        public bool IsDeleted { get; set; }
    }
}
