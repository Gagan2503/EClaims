using EClaimsEntities.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EClaimsEntities.DataTransferObjects
{
    public class DepartmentWithFacilitiesDto
    {
        public int DepartmentID { get; set; }

        public string Code { get; set; }

        public string Department { get; set; }

        public bool IsActive { get; set; }


        public DateTime CreatedDate { get; set; }

        public DateTime ModifiedDate { get; set; }

        public int CreatedBy { get; set; }

        public int ModifiedBy { get; set; }

        public DateTime ApprovalDate { get; set; }


        public int ApprovalStatus { get; set; }


        public int ApprovalBy { get; set; }

        public string Reason { get; set; }

        public IEnumerable<MstFacility> Facilities { get; set; }
    }
}
