using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace APIRestPayment.Models
{
    public class PaymentResultModel
    {
        public string TransactionType { set; get; }
        public string ResultOfPayment { set; get; }
        public string Error { set; get; }
        public string TrackingNumber { set; get; }
        public string TimeSpan { set; get; }
        public string OrderNumber { set; get; }

    }
}