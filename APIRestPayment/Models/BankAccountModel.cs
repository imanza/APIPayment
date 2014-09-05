using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace APIRestPayment.Models
{
    public class BankAccountModel
    {
        public string Accountnumber { set; get; }
        public string Bank { set; get; }
        public string Branch { set; get; }
        public string Iban { set; get; }
 
    }
}