using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EClaimsWeb.Models
{
    public class Querydata
    {
        public long MsgID { get; set; }
        public string ModuleType { get; set; }
        public long ID { get; set; }
        public long SenderID { get; set; }
        public long RecieverID { get; set; }
        public DateTime SentTime { get; set; }
        public string MessageDescription { get; set; }
        public string FullName { get; set; }
    }
}
