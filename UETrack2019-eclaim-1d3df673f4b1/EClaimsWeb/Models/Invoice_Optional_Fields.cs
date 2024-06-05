using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EClaimsWeb.Models
{
    public class Invoice_Optional_Fields
    {
        public string CNTBTCH { get; set; }
        public string CNTITEM { get; set; }
        public string OPTFIELD { get; set; }
        public string VALUE { get; set; }
        public string TYPE { get; set; }
        public string LENGTH { get; set; }
        public string DECIMALS { get; set; }
        public string ALLOWNULL { get; set; }
        public string VALIDATE { get; set; }
        public string VALINDEX { get; set; }
        public string VALIFTEXT { get; set; }
        public string VALIFMONEY { get; set; }
        public string VALIFNUM { get; set; }

        public string VALIFLONG { get; set; }
        public string VALIFBOOL { get; set; }
        public string VALIFDATE { get; set; }
        public string VALIFTIME { get; set; }

        public string FDESC { get; set; }
        public string VDESC { get; set; }
    }
}
