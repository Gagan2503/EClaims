using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EClaimsEntities.Models
{
    [Table("DtUserRoles")]
    public class DtUserRoles
    {
        [Key, Column(Order = 0)]
        public  int UserRoleID { get; set; }
        public int UserID { get; set; }
        public MstUser User { get; set; }
        public int RoleID { get; set; }
        public MstRole Role { get; set; }
    }
}
