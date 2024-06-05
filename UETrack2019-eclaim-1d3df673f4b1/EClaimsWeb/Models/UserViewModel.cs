using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace EClaimsWeb.Models
{
    public class UserViewModel
    {
        public int UserID { get; set; }
        [StringLength(50)]
        [Display(Name = "Name")]
        [Required(ErrorMessage = "Username is required")]
        public string Name { get; set; }
        [Display(Name = "Facility")]
        public int FacilityID { get; set; }
        [StringLength(10)]
        [Display(Name = "Phone", Prompt = "Phone")]
        [Required(ErrorMessage = "Phone is required")]
        public string Phone { get; set; }
        public string Password { get; set; }
        public DateTime CreationTime { get; set; }
        public long CreatorUserId { get; set; }
        public DateTime CreatedDate { get; set; }

        public DateTime ModifiedDate { get; set; }
        public int CreatedBy { get; set; }

        public int ModifiedBy { get; set; }
    }
}
