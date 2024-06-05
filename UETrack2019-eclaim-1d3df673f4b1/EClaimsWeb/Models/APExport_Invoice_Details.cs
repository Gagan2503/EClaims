using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EClaimsWeb.Models
{
    public class APExport_Invoice_Details
    {
        public string CNTBTCH { get; set; }
        public string CNTITEM { get; set; }
        public string CNTLINE { get; set; }
        public string IDDIST { get; set; }
        public string TEXTDESC { get; set; }

        public string AMTTOTTAX { get; set; }
        public string BASETAX1 { get; set; }
        public string TAXCLASS1 { get; set; }
        public string RATETAX1 { get; set; }
        public string AMTTAX1 { get; set; }

        public string IDGLACCT { get; set; }
        public string AMTDIST { get; set; }
        public string COMMENT { get; set; }
        public string SWIBT { get; set; }
        public string IDINVC { get; set; }
    }
}
