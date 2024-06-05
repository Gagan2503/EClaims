using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EClaimsEntities.Models
{
    public class MstUserApprovers
    {
        [Key]
        public int ID { get; set; }
        public long UserId { get; set; }
        public int? ApproverId { get; set; }
        public int? FacilityId { get; set; }
        public bool IsApproverActive { get; set; }
        public byte SortOrder { get; set; }

        public DateTime CreatedDate { get; set; }
        public DateTime ModifiedDate { get; set; }
        public int CreatedBy { get; set; }
        public int ModifiedBy { get; set; }

    }
}
