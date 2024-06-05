using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EClaimsEntities.Models
{
    [NotMapped]
    public class CustomClaimCount
    {
        public long TOTALAPPROVALCOUNT { get; set; }
        public long TOTALVERIFICATIONCOUNT { get; set; }
        public long TOTALCLAIMSCOUNTTHISYEAR { get; set; }
        public long TOTALCLAIMSCOUNTTILLNOW { get; set; }

    }
}
