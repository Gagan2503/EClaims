using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EClaimsWeb.Models
{
    public class APExport_Invoice
    {
        public string CNTBTCH { get; set; }
        public string CNTITEM { get; set; }
        public string IDVEND { get; set; }
        public string IDINVC { get; set; }
        public string TEXTTRX { get; set; }

        public string ORDRNBR { get; set; }
        public string PONBR { get; set; }
        public string INVCDESC { get; set; }
        public string INVCAPPLTO { get; set; }
        public string IDACCTSET { get; set; }

        public string DATEINVC { get; set; }
        public string FISCYR { get; set; }
        public string FISCPER { get; set; }
        public string CODECURN { get; set; }
        public string TERMCODE { get; set; }

        public string DATEDUE { get; set; }
        public string CODETAXGRP { get; set; }
        public string TAXCLASS1 { get; set; }
        public string BASETAX1 { get; set; }
        public string AMTTAX1 { get; set; }

        public string AMTTAXDIST { get; set; }
        public string AMTINVCTOT { get; set; }
        public string AMTTOTDIST { get; set; }
        public string AMTGROSDST { get; set; }
        public string AMTDUE { get; set; }

        public string AMTTAXTOT { get; set; }
        public string AMTGROSTOT { get; set; }
    }
}
