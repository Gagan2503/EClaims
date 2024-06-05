using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EClaimsEntities.Models
{
    [NotMapped]
    public class CustomIndividualClaimCount
    {
        public string Claim { get; set; }
        public long ClaimCount { get; set; }
    }
}
