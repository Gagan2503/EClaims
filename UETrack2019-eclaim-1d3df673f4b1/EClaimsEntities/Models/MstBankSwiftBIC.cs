using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EClaimsEntities.Models
{
    [Table("MstBankSwiftBIC")]
    public class MstBankSwiftBIC
    {
        [Key]
        public int ID { get; set; }
        public string BankName { get; set; }
        public long BankCode { get; set; }
        public string BankSwiftBIC { get; set; }

        public DateTime CreatedDate { get; set; }

        public DateTime ModifiedDate { get; set; }

        public int CreatedBy { get; set; }

        public int ModifiedBy { get; set; }
    }
}
