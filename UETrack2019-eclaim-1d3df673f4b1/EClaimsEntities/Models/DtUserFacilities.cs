using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EClaimsEntities.Models
{
    [Table("DtUserFacilities")]
    public class DtUserFacilities
    {
        [Key, Column(Order = 0)]
        public int UserFacilityID { get; set; }
        public int UserID { get; set; }
        public MstUser User { get; set; }
        public int FacilityID { get; set; }
        public MstFacility Facility { get; set; }
    }
}
