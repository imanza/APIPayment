using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace APIRestPayment.Models
{
    public class AccountModel
    {
        public long? Id { set; get; }
        public string Url { set; get; }
        public string Accountnumber { set; get; }
        //to make datetime nullable
        public DateTime? Dateofopening { set; get; }
        public string AccountType { set; get; }
        public string Currency { set; get; }
        public string Paymentcode { set; get; }
        //to make decimal nullable
        public Decimal? Balance { set; get; }            
        public bool? IsActive { set; get; }
         public UserModel UserItem { set; get; }
    }
}