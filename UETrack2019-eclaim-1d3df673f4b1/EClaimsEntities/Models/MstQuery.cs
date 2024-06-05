using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EClaimsEntities.Models
{
    [Table("MstQuery")]
    public class MstQuery
    {
        [Key]
        public int MsgID { get; set; }

        [StringLength(30, ErrorMessage = "ModuleType can't be longer than 30 characters")]
        public string ModuleType { get; set; }
        public long? ID { get; set; }
        public long? SenderID { get; set; }
        public long? ReceiverID { get; set; }
        public DateTime SentTime { get; set; }

        public string MessageDescription { get; set; }
    }
}
