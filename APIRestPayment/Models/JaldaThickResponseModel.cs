using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace APIRestPayment.Models
{
    public class JaldaThickResponseModel
    {
        public string JaldaThickStatus { set; get; }
        public Models.PaymentResultModel DepositPaymentResult { set; get; }
        public Models.PaymentResultModel TerminatePaymentResult { set; get; }
        public JaldaThickModel JaldaThick { set; get; }
        public string Trailer { set; get; }

    }
}