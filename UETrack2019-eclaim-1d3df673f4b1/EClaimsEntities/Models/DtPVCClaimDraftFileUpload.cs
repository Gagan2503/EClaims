using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EClaimsEntities.Models
{
    [Table("DtPVCClaimDraftFileUpload")]
    public class DtPVCClaimDraftFileUpload
    {

        [Key]
        public long FileID { get; set; }

        [ForeignKey(nameof(MstPVCClaimDraft))]
        public long PVCCID { get; set; }
        public MstPVCClaimDraft MstPVCClaimDraft { get; set; }

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
