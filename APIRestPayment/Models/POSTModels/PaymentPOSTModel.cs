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
        public Decimal? Amount {set; get;}
        public string PayeeAccountNumber { set; get; }
        public string PayerAccountNumber { set; get; }
        public string RedirectUrl { set; get; }
        public string OrderNumber { get; set; }
        public string PaymentPIN { get; set; }
        public string HashPaymentCode { set; get; }
        public string Description { set; get; }
        //public SalesModel SaleDetails { set; get; }
        //public TransferModel TransferDetails { set; get; }

    }
}