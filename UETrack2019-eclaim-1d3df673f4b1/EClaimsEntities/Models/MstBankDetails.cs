using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EClaimsEntities.Models
{
    public class MstBankDetails
    {
        [Key]
        public int ID { get; set; }
        public long UserId { get; set; }
        public string NameAsInBank { get; set; }
        public string AccountNumber { get; set; }
        public string BankName { get; set; }
        public string BankCode { get; set; }
        public string BankSwiftBIC { get; set; }
        public string Branch { get; set; }
        public string BranchCode { get; set; }
        public string PayNow { get; set; }
        public string BankStatementUrl { get; set; }
        public string BankStatementFileName { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime ModifiedDate { get; set; }
        public int CreatedBy { get; set; }
        public int ModifiedBy { get; set; }
    }
}
