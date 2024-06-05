using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EClaimsWeb.Models
{
    public class ExportBankTrailer
    {
        public string Header { get; set; }
        public int Count { get; set; }
        public string TotalAmount { get; set; }
    }
}
