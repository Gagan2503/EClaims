using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace EClaimsWeb.Models
{
    public class UserVM
    {
        public int UserID { get; set; }

        [Display(Name = "Name", Prompt = "Full Name")]
        [Required(ErrorMessage = "Name is required")]
        public string Name { get; set; }


        [Display(Name = "Phone", Prompt = "Phone")]
        [StringLength(10, ErrorMessage = "Phone Number can't be longer than 10 characters")]
        [Required(ErrorMessage = "Phone is required")]
        public string Phone { get; set; }

        [Display(Name = "Employee No", Prompt = "Employee No")]
        [Required(ErrorMessage = "Employee No is required")]
        public string EmployeeNo { get; set; }

        [Display(Name = "Petty Cash Float", Prompt = "Petty Cash Float")]
        [Required(ErrorMessage = "Expense Limit is required")]
        public decimal ExpenseLimit { get; set; }

        [Display(Name = "Mileage Limit", Prompt = "Mileage Limit")]
        [Required(ErrorMessage = "Mileage Limit is required")]
        public decimal MileageLimit { get; set; }

        [Display(Name = "Telephone Limit", Prompt = "Telephone Limit")]
        [Required(ErrorMessage = "Telephone Limit is required")]
        public decimal TelephoneLimit { get; set; }

        [Display(Name = "Email", Prompt = "Email")]
        [RegularExpression(@"^[a-zA-Z0-9_.+-]+@[a-zA-Z0-9-]+\.[a-zA-Z0-9-.]+$", ErrorMessage = "Invalid email format")]
        [Required(ErrorMessage = "Email is required")]
        public string EmailAddress { get; set; }
        [Display(Name = "Is HOD", Prompt = "Is HOD")]
        public bool IsHOD { get; set; }
        [Display(Name = "Is Active", Prompt = "Is Active")]
        public bool IsActive { get; set; }

        public List<SelectListItem> drpRoles { get; set; }

        [Required(ErrorMessage = "Role is required")]
        [Display(Name = "Roles", Prompt = "Select Roles")]
        public int[] RoleIds { get; set; }

        public List<SelectListItem> drpFacilities { get; set; }

        [Required(ErrorMessage = "Facility is required")]
        [Display(Name = "Facility", Prompt = "Facility")]
        public int[] FacilityIDs { get; set; }

    }
}
