using EClaimsEntities.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EClaimsWeb.Models
{
    public class ApprovalViewModel
    {
        public List<MstApprovalMatrix> ApprovalMatrices { get; set; }
        public SelectList Screens { get; set; }
        public string MovieGenre { get; set; }
        public string SearchString { get; set; }
    }
}
