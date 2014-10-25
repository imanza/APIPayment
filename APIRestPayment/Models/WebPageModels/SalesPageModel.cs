using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace APIRestPayment.Models.WebPageModels
{
    public class SalesPageModel
    {
        //[Display(Name = "Payee Name")]
        //public string PayeeName { get; set; }

        //[Display(Name = "Payee Account Number")]
        //public string PayeeAccountNumber { get; set; }

        //[Display(Name = "Amount")]
        //public Decimal PaymentAmount { get; set; }
        //public string PaymentCurrency { get; set; }



        [Display(Name = "User Account")]
        public long SelectedUserAccountId { get; set; }
        public IEnumerable<SelectListItem> UserAccount { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "PIN Code")]
        public string PINCode { get; set; }
    }
}