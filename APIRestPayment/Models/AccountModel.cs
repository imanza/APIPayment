using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace APIRestPayment.Models
{
    public class AccountModel
    {
        public string Url { set; get; }
        public string Accountnumber { set; get; }
        public DateTime Dateofopening { set; get; }
        public string Paymentcode { set; get; }
        public UserModel UserItem { set; get; }
    }
}