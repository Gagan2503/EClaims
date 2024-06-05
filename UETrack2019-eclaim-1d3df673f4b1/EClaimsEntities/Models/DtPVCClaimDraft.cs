using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EClaimsEntities.Models
{
    public class DtPVCClaimDraft
    {

        [Key]
        public long PVCCItemID { get; set; }

        [ForeignKey(nameof(MstPVCClaimDraft))]

        public long PVCCID { get; set; }

        public MstPVCClaimDraft MstPVCClaimDraft { get; set; }

        public DateTime Date { get; set; }

        [ForeignKey(nameof(MstExpenseCategory))]
        public int? ExpenseCategoryID { get; set; }
        public MstExpenseCategory MstExpenseCategory { get; set; }
        [ForeignKey(nameof(MstFacility))]
        public int? FacilityID { get; set; }
        public MstFacility MstFacility { get; set; }

        [StringLength(50)]
        public string ChequeNo { get; set; }

        [StringLength(500)]
        public string Particulars { get; set; }

        [StringLength(50)]
        public string Payee { get; set; }

        [StringLength(50)]
        public string InvoiceNo { get; set; }

        public decimal Amount { get; set; }

        public decimal GST { get; set; }

        public string AccountCode { get; set; }

        public int? OrderBy { get; set; }

        public decimal GSTPercentage { get; set; }

    }
}

