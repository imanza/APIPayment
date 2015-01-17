using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace APIRestPayment.Models
{
    public class TransactionModel
    {
        public string Url { set; get; }
        public string Trackingnumber { set; get; }
        public DateTime? ExecutionDate{ set; get; }
        public AccountModel SourceAccount { set; get; }
        public AccountModel DestinationAccount { set; get; }
        public Decimal? Amount { set; get; }
        public string Currency { set; get; }
        public string Type { set; get; }
        public string Status { set; get; }
        public string Description { set; get; }

    }
}