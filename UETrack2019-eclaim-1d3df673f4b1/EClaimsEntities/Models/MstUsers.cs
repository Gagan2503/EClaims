using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EClaimsEntities.Models
{
    [Table("MstUser")]
    public class MstUser
    {
        [Key]
        public int UserID { get; set; }
        public string NameIdentifier { get; set; }
        public int AccessFailedCount { get; set; }
        public string AuthenticationSource { get; set; }
        public string ConcurrencyStamp { get; set; }
        public DateTime CreationTime { get; set; }
        public long CreatorUserId { get; set; }
        public long DeleterUserId { get; set; }
        public DateTime DeletionTime { get; set; }

        [Display(Name = "Email")]
        [RegularExpression(@"^[a-zA-Z0-9_.+-]+@[a-zA-Z0-9-]+\.[a-zA-Z0-9-.]+$",ErrorMessage = "Invalid email format")]
        [Required(ErrorMessage = "Email is required")]
        [StringLength(50, ErrorMessage = "Email can't be longer than 50 characters")]
        public string EmailAddress { get; set; }
        public string EmailConfirmationCode { get; set; }

        [Display(Name = "Phone")]
        [Required(ErrorMessage = "Phone is required")]
        [StringLength(20, ErrorMessage = "Phone can't be longer than 20 characters")]
        public string Phone { get; set; }

        [Display(Name = "Employee No")]
        [Required(ErrorMessage = "Employee No is required")]
        [StringLength(20, ErrorMessage = "Employee No can't be longer than 20 characters")]
        public string EmployeeNo { get; set; }

        public  bool IsHOD { get; set; }

        public bool IsActive { get; set; }
        public bool IsDeleted { get; set; }
        public bool IsEmailConfirmed { get; set; }
        public bool IsLockoutEnabled { get; set; }
        public bool IsPhoneNumberConfirmed { get; set; }
        public bool IsTwoFactorEnabled { get; set; }
        public DateTime LastModificationTime { get; set; }
        public long LastModifierUserId { get; set; }

        public DateTime LockoutEndDateUtc { get; set; }

        [Display(Name = "Name")]
        [Required(ErrorMessage = "Name is required")]
        [StringLength(50, ErrorMessage = "Name can't be longer than 50 characters")]
        public string Name { get; set; }
        public string NormalizedEmailAddress { get; set; }
        public string NormalizedUserName { get; set; }
        public string Password { get; set; }
        public string PasswordResetCode { get; set; }
        public string SecurityStamp { get; set; }
        public string Surname { get; set; }
        public int TenantId { get; set; }
        public string UserName { get; set; }
        public decimal ExpenseLimit { get; set; }
        public decimal MileageLimit { get; set; }
        public decimal TelephoneLimit { get; set; }

        [ForeignKey(nameof(MstFacility))]
        public int? FacilityID { get; set; }
        public virtual MstFacility MstFacility { get; set; }


        public ICollection<DtUserRoles> DtUserRoles { get; set; }
        public ICollection<DtUserFacilities> DtUserFacilities { get; set; }
    }
}
