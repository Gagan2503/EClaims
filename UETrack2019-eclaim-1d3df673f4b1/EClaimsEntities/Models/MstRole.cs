using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EClaimsEntities.Models
{
    [Table("MstRole")]
    public class MstRole
    {
        [Key]
        public int RoleID { get; set; }

        [Required(ErrorMessage = "RoleName is required")]
        [StringLength(10, ErrorMessage = "Code can't be longer than 10 characters")]
        public string RoleName { get; set; }

        
        [Required(ErrorMessage = "Is Active is required")]
        public bool IsActive { get; set; }


        public DateTime CreatedDate { get; set; }

        public DateTime ModifiedDate { get; set; }

        public int CreatedBy { get; set; }

        public int ModifiedBy { get; set; }

        public DateTime ApprovalDate { get; set; }


        public int ApprovalStatus { get; set; }


        public int ApprovalBy { get; set; }

        public string Reason { get; set; }

        public ICollection<DtUserRoles> DtUserRoles { get; set; }
        //public virtual ICollection<MstUser> Users { get; set; }
    }
}
