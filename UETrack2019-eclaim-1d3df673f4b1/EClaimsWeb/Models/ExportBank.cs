using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace EClaimsWeb.Models
{
    public class ExportBank
    {
        [Display(Name = "RECORD")]
        public string RECORD { get; set; }

        [Display(Name = "PAYMENT TYPE")]
        public string PAYMENTTYPE { get; set; }

        [Display(Name = "ORIGINATING ACCOUNT NUMBER")]
        public string ORIGINATINGACCOUNTNUMBER { get; set; }

        [Display(Name = "ORIGINATING ACCOUNT CURRENCY")]
        public string ORIGINATINGACCOUNTCURRENCY { get; set; }

        [Display(Name = "CUSTOMER REFERENCE / BATCH REFERENCE")]
        public string CUSTOMERREFERENCE { get; set; }

        [Display(Name = "PAYMENT CURRENCY")]
        public string PAYMENTCURRENCY { get; set; }

        [Display(Name = "BATCH ID")]
        public string BATCHID { get; set; }

        [Display(Name = "PAYMENT DATE (DDMMYYYY)")]
        public string PAYMENTDATE { get; set; }

        [Display(Name = "BANK CHARGES")]
        public string BANKCHARGES { get; set; }

        [Display(Name = "DEBIT ACCOUNT BANK CHARGES")]
        public string DEBITACCOUNTBANKCHARGES { get; set; }

        [Display(Name = "RECEIVING PARTY NAME")]
        public string RECEIVINGPARTYNAME { get; set; }

        [Display(Name = "PAYABLE TO (ONLY FOR CHEQUES)")]
        public string PAYABLETO { get; set; }

        [Display(Name = "RECEIVING PARTY ADDRESS 1")]
        public string RECEIVINGPARTYADDRESS1 { get; set; }

        [Display(Name = "RECEIVING PARTY ADDRESS 2")]
        public string RECEIVINGPARTYADDRESS2 { get; set; }

        [Display(Name = "RECEIVING PARTY ADDRESS 3")]
        public string RECEIVINGPARTYADDRESS3 { get; set; }

        [Display(Name = "RECEIVING ACCOUNT NUMBER")]
        public string RECEIVINGACCOUNTNUMBER { get; set; }

        [Display(Name = "Country Specific field")]
        public string CountrySpecificfield { get; set; }

        [Display(Name = "BENEFICIARY BANK CODE")]
        public string BENEFICIARYBANKCODE { get; set; }

        [Display(Name = "BENEFICIARY BANK BRANCH CODE")]
        public string BENEFICIARYBANKBRANCHCODE { get; set; }

        [Display(Name = "Clearing Code")]
        public string ClearingCode { get; set; }

        [Display(Name = "BENEFICIARY BANK SWIFT BIC")]
        public string BENEFICIARYBANKSWIFTBIC { get; set; }

        [Display(Name = "BENEFICIARY BANK NAME")]
        public string BENEFICIARYBANKNAME { get; set; }

        [Display(Name = "BENEFICIARY BANK ADDRESS")]
        public string BENEFICIARYBANKADDRESS { get; set; }

        [Display(Name = "BENEFICIARY BANK COUNTRY")]
        public string BENEFICIARYBANKCOUNTRY { get; set; }

        [Display(Name = "BENE BANK ROUTING CODE")]
        public string BENEBANKROUTINGCODE { get; set; }

        [Display(Name = "INTERMEDIARY BANK SWIFT BIC")]
        public string INTERMEDIARYBANKSWIFTBIC { get; set; }

        [Display(Name = "Amount Currency")]
        public string AmountCurrency { get; set; }

        [Display(Name = "AMOUNT")]
        public string AMOUNT { get; set; }

        [Display(Name = "FX Contract Reference 1")]
        public string FXContractReference1 { get; set; }

        [Display(Name = "Amount to be Utilized 1")]
        public string AmounttobeUtilized1 { get; set; }

        [Display(Name = "FX Contract Reference 2")]
        public string FXContractReference2 { get; set; }

        [Display(Name = "Amount to be Utilized 2")]
        public string AmounttobeUtilized2 { get; set; }

        [Display(Name = "TRANSACTION CODE")]
        public string TRANSACTIONCODE { get; set; }

        [Display(Name = "BENEFICIARY REFERENCE")]
        public string BENEFICIARYREFERENCE { get; set; }

        [Display(Name = "DDA REFERENCE/ CHEQUE REFERENCE")]
        public string DDAREFERENCE { get; set; }

        [Display(Name = "PAYMENT DETAILS")]
        public string PAYMENTDETAILS { get; set; }

        [Display(Name = "Instruction to ordering bank")]
        public string Instructiontoorderingbank { get; set; }

        [Display(Name = "Beneficiary Nationality Status")]
        public string BeneficiaryNationalityStatus { get; set; }

        [Display(Name = "Beneficiary Category")]
        public string BeneficiaryCategory { get; set; }

        [Display(Name = "Transaction Relationship")]
        public string TransactionRelationship { get; set; }

        [Display(Name = "Payee Role")]
        public string PayeeRole { get; set; }

        [Display(Name = "Remitter Identity")]
        public string RemitterIdentity { get; set; }

        [Display(Name = "PURPOSE OF PAYMENT")]
        public string PURPOSEOFPAYMENT { get; set; }

        [Display(Name = "Supplementary Info")]
        public string SupplementaryInfo { get; set; }

        [Display(Name = "DELIVERY MODE (E = EMAIL)")]
        public string DELIVERYMODE{ get; set; }

        [Display(Name = "Print at location")]
        public string Printatlocation { get; set; }

        [Display(Name = "Payable at location")]
        public string Payableatlocation { get; set; }

        [Display(Name = "Mail to Party Name")]
        public string MailtoPartyName { get; set; }

        [Display(Name = "Address Line 1")]
        public string AddressLine1 { get; set; }

        [Display(Name = "Address Line 2")]
        public string AddressLine2 { get; set; }

        [Display(Name = "Address Line 3")]
        public string AddressLine3 { get; set; }

        [Display(Name = "Reserved Field - leave ")]
        public string ReservedField  { get; set; }

        [Display(Name = "Postal code ")]
        public string Postalcode { get; set; }

        [Display(Name = "EMAIL 1")]
        public string EMAIL1 { get; set; }

        [Display(Name = "EMAIL 2")]
        public string EMAIL2 { get; set; }

        [Display(Name = "EMAIL 3")]
        public string EMAIL3 { get; set; }

        [Display(Name = "EMAIL 4")]
        public string EMAIL4 { get; set; }

        [Display(Name = "EMAIL 5")]
        public string EMAIL5 { get; set; }

        [Display(Name = "Phone number 1")]
        public string Phonenumber1 { get; set; }

        [Display(Name = "Phone number 2")]
        public string Phonenumber2 { get; set; }

        [Display(Name = "Phone number 3")]
        public string Phonenumber3 { get; set; }

        [Display(Name = "Phone number 4")]
        public string Phonenumber4 { get; set; }

        [Display(Name = "Phone number 5")]
        public string Phonenumber5 { get; set; }

        [Display(Name = "INVOICE DETAIL")]
        public string INVOICEDETAIL { get; set; }

        [Display(Name = "Client reference 1")]
        public string Clientreference1 { get; set; }

        [Display(Name = "Client reference 2")]
        public string Clientreference2 { get; set; }

        [Display(Name = "Client reference 3")]
        public string Clientreference3 { get; set; }

        [Display(Name = "Client reference 4")]
        public string Clientreference4 { get; set; }

    }
}
