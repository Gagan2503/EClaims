using EClaimsEntities.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace EClaimsWeb.Models
{
    public class ApprovalMatrixViewModel
    {
        public List<MstApprovalMatrix> ApprovalMatrices { get; set; }
        public SelectList Screens { get; set; }
        public string ScreenId { get; set; }
        public IEnumerable<SelectListItem> Modules { get; set; }
        [Display(Name = "Module", Prompt = "Module")]
        [Required(ErrorMessage = "Module is required")]
        public string ModuleName { get; set; }
        public IEnumerable<SelectListItem> LScreens { get; set; }

        [Display(Name = "Sub Module", Prompt = "Screens")]
        [Required(ErrorMessage = "Sub module is required")]
        public int? LScreenId { get; set; }

        [Display(Name = "Department", Prompt = "Department")]
        [Required(ErrorMessage = "Department is required")]
        public int DepartmentID { get; set; }
        public IEnumerable<SelectListItem> Departments { get; set; }

        public string SearchModule { get; set; }
    }
}
