using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EClaimsEntities.Models
{
    [Table("MstEmailAuditLog")]
    public class MstEmailAuditLog
    {
        [Key]
        public int AuditID { get; set; }
        public int? SentBY { get; set; }
        public string SentDate { get; set; }
        public string Screen { get; set; }
        public string Subject { get; set; }
        public string Body { get; set; }
        public string SentTo { get; set; }
        public int? InstanceID { get; set; }
        public string Status { get; set; }
    }
}
