﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EClaimsEntities.DataTransferObjects
{
    public class DepartmentForCreationDto
    {
        [Required(ErrorMessage = "Code is required")]
        [StringLength(10, ErrorMessage = "Code can't be longer than 10 characters")]
        public string Code { get; set; }

        [Required(ErrorMessage = "Department is required")]
        [StringLength(50, ErrorMessage = "Department can't be longer than 50 characters")]
        public string Department { get; set; }

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
    }
}
