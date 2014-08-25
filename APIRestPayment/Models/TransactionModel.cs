using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace APIRestPayment.Models
{
    public class TransactionModel
    {
        public long Amount { set; get; }
        public string Trackingnumber { set; get; }
        public DateTime Executiondatetime { set; get; }
        public bool Showsender { set; get; }
    }
}