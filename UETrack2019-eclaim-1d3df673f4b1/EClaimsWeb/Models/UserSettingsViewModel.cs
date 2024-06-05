using EClaimsEntities.Models;
using EClaimsWeb.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace EClaimsWeb.Models
{
    public class UserSettingsViewModel
    {
        public UserViewModel UserDetails { get; set; }
        #region user details
        public SelectList BankSwiftBICs { get; set; }
        public SelectList Facilities { get; set; }

        public IEnumerable<SelectListItem> Facilitys { get; set; }
        #endregion
        #region Approvers
        public SelectList Approvers { get; set; }
        [Display(Name = "User")]
        public int Approver1ID { get; set; }
        [Display(Name = "User")]
        public int Approver2ID { get; set; }
        [Display(Name = "User")]
        public int Approver3ID { get; set; }
        [Display(Name = "Approver 1")]
        [Required(ErrorMessage = "Atleast one approver is needed")]
        public bool IsApprover1Active { get; set; }
        [Display(Name = "Approver 2")]
        public bool IsApprover2Active { get; set; }
        [Display(Name = "Approver 3")]
        public bool IsApprover3Active { get; set; }
        #endregion

        [BindProperty]
        public BankDetailsViewModel BankDetials { get; set; }

        public AlternateApprover AlternateApprover { get; set; }

        public DelegateUser DelegateUser { get; set; }

        public List<UserApproversViewModel> ApproverDetails { get; set; }
        //[MustBeTrue(ErrorMessage = "Please Confirm entered details are valid")]
        [Required(ErrorMessage = "Please confirm entered details are valid")]
        public bool UserConfirmation { get; set; }
        public UserSettingsViewModel(IEnumerable<MstFacility> result, MstUser userDetails, IEnumerable<MstUser> approversList, MstBankDetails bankDetails)
        {
            Facilities = new SelectList(result.ToList(), "FacilityID", "FacilityName");
            Approvers = new SelectList(approversList.ToList(), "Id", "Name");
            UserDetails = new UserViewModel() { Phone = "9985", Name = "shakirtest" };
            BankDetials = new BankDetailsViewModel()
            {
                AccountNumber = "9985",
                BankCode = "99",
                BankName = "HDFC",
                Branch = "CTE",
                BranchCode = "77",
                ConfirmAccountnumber = "9985",
                NameAsInBank = "shakirtest"
            };
            //UserName = userDetails.UserName;
            //UserPhone = userDetails.PhoneNumber;

            //BankDetials.NameAsInBank = "Sreedhar";
            AlternateApprover = new AlternateApprover();
        }
        public UserSettingsViewModel()
        {
        }
    }

    public class BankDetailsViewModel
    {
        public int ID { get; set; }

        [StringLength(50)]
        [Display(Name = "Name as per bank account", Prompt = "Name as per bank account")]
        [Required(ErrorMessage = "Name is required.")]
        public string NameAsInBank { get; set; }

        [StringLength(20)]
        [Display(Name = "Account number", Prompt = "Account number")]
        [Required(ErrorMessage = "Account number is required.")]
        public string AccountNumber { get; set; }

        [StringLength(20)]
        [Display(Name = "Confirm Account number", Prompt = "Confirm Account number")]
        [Required(ErrorMessage = "Confirm Account number is required.")]
        [Compare("AccountNumber", ErrorMessage = "Account number doesnot match.")]
        public string ConfirmAccountnumber { get; set; }

        [StringLength(50)]
        [Display(Name = "Bank", Prompt = "Bank")]
        [Required(ErrorMessage = "Bank is required.")]
        public string BankName { get; set; }

        [StringLength(10)]
        [Display(Name = "Bank Code", Prompt = "Bank Code")]
        [Required(ErrorMessage = "Bank Code is required.")]
        public string BankCode { get; set; }

        [StringLength(20)]
        [Display(Name = "Bank SWIFT BIC", Prompt = "Bank SWIFT BIC")]
        [Required(ErrorMessage = "Bank SWIFT BIC is required.")]
        public string BankSWIFTBIC { get; set; }

        [StringLength(50)]
        [Display(Name = "Branch", Prompt = "Branch")]
        [Required(ErrorMessage = "Branch is required.")]
        public string Branch { get; set; }

        [StringLength(10)]
        [Display(Name = "Branch Code", Prompt = "Branch Code")]
        [Required(ErrorMessage = "Branch Code is required.")]
        public string BranchCode { get; set; }

        [StringLength(20)]
        [Display(Name = "Pay Now", Prompt = "Pay Now")]
        [Required(ErrorMessage = "Pay Now is required.")]
        public string PayNow { get; set; }

        [Display(Name = "Upload Bank Statement", Prompt = "Upload Bank Statement")]
        public string BankStatementFileName { get; set; }
        [BindProperty]
        public IFormFile BankStatement { get; set; }
        public string BankStatementUrl { get; set; }

        //public UserSettingsViewModel(IEnumerable<MstFacility> result, MstUser userDetails, IEnumerable<MstUser> approversList, MstBankDetails bankDetails)
        //{
        //    Facilities = new SelectList(result.ToList(), "FacilityID", "FacilityName");
        //    Approvers = new SelectList(approversList.ToList(), "Id", "Name");
        //    UserName = userDetails.UserName;
        //    UserPhone = userDetails.Phone;
        //}
        public DateTime CreatedDate { get; set; }

        public DateTime ModifiedDate { get; set; }

        public int CreatedBy { get; set; }

        public int ModifiedBy { get; set; }

    }

    public class UserApproversViewModel
    {
        public int ID { get; set; }
        public int UserId { get; set; }

        //[Required(ErrorMessage = "Approver 1 is required")]
        //[Display(Name ="Approver 1")]
        public int? ApproverId { get; set; }

        public int? FacilityId { get; set; }

        //[MustBeTrue(ErrorMessage = "Atleast on Approver is required")]
        [Display(Name = "Approver 1")]
        public bool IsApproverActive { get; set; }
        public byte SortOrder { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime ModifiedDate { get; set; }
        public int CreatedBy { get; set; }
        public int ModifiedBy { get; set; }
    }

    public class AlternateApprover
    {
        public bool IsEnabled { get; set; }
        public int UserID { get; set; }

        public string FromDate { get; set; }

        public string ToDate { get; set; }

        public string FromTime { get; set; }

        public string ToTime { get; set; }
        public string AlternateUserName { get; set; }
    }

    public class DelegateUser
    {
        public bool IsEnabled { get; set; }
        public int UserID { get; set; }

        public string FromDate { get; set; }

        public string ToDate { get; set; }

        public string FromTime { get; set; }

        public string ToTime { get; set; }
        public string DelegateUserName { get; set; }
    }
}
