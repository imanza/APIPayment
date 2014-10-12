using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace APIRestPayment.Models.POSTModels
{
    public class PaymentPOSTModel
    {
        public string TransactionType { set; get; }
        public string Currency { set; get; }
        public Decimal Amount {set; get;}
        public string PayeeAccountNumber { set; get; }
        public string RedirectUrl { set; get; }
        public string RequestNonce { set; get; }
        public DateTime RequestDate  { set; get; }
        //public SalesModel SaleDetails { set; get; }
        //public TransferModel TransferDetails { set; get; }

    }
}