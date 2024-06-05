using Newtonsoft.Json.Converters;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace EClaimsWeb.Models
{
    public class DtMileageClaimVM
    {
        public long MCItemID { get; set; }
        public long MCID { get; set; }
        public DateTime DateOfJourney { get; set; }
        public string DateOfJourneyString { get; set; }
        public string AccountCode { get; set; }
        public string FacilityName { get; set; }
        public int? FacilityID { get; set; }
        public string FromFacilityName { get; set; }
        public string FromFacilityID { get; set; }
        public string ToFacilityName { get; set; }
        public string ToFacilityID { get; set; }
        public DateTime InTime { get; set; }

        public string InTimeTime { get; set; }

        public string OutTimeTime { get; set; }

        public DateTime OutTime { get; set; }

        public decimal StartReading { get; set; }

        public decimal EndReading { get; set; }

        public decimal Kms { get; set; }

        public string Remark { get; set; }

        public decimal Amount { get; set; }
        public string Description { get; set; }

        public int? ExpenseCategoryID { get; set; }
        public string ExpenseCategory { get; set; }
        public int? OrderBy { get; set; }
    }
}
