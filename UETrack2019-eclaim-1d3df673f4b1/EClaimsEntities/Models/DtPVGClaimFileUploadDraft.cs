using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EClaimsEntities.Models
{
    [Table("DtPVGClaimFileUploadDraft")]
  public  class DtPVGClaimFileUploadDraft
    {
        [Key]
        public long FileID { get; set; }

        [ForeignKey(nameof(MstPVGClaimDraft))]
        public long PVGCID { get; set; }
        public MstPVGClaimDraft MstPVGClaimDraft { get; set; }

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
