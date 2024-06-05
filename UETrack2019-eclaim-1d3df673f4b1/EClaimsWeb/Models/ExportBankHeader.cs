using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EClaimsWeb.Models
{
    public class ExportBankHeader
    {
        public string Header { get; set; }
        public  string CreationDate { get; set; }
        public string OrganizationID { get; set; }
        public string CompanyName { get; set; }
    }
}
