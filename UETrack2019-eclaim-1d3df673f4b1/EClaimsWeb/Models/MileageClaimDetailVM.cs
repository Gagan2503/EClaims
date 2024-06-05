using EClaimsEntities.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EClaimsWeb.Models
{
    public class MileageClaimDetailVM
    {
       public MileageClaimVM MileageClaimVM { get; set; }
       public List<DtMileageClaimVM> DtMileageClaimVMs { get; set; }
        public List<DtMileageClaimSummary> DtMileageClaimSummaries { get; set; }
        public List<DtMileageClaimVM> DtMileageClaimVMSummary { get; set; }
       public List<MileageClaimAuditVM> MileageClaimAudits { get; set; }
       public List<DtMileageClaimFileUpload> MileageClaimFileUploads { get; set; }
    }
}
