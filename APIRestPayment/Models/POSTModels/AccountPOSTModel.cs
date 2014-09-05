using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace APIRestPayment.Models.POSTModels
{
    public class AccountPOSTModel
    {
        public string AccountType { set; get; }
        public string Currency { set; get; }
        public long UserId { set; get; }
    }
}