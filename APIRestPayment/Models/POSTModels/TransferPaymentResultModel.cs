using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace APIRestPayment.Models.POSTModels
{
    public class TransferPaymentResultModel
    {
        public string TransactionType { set; get; }
        public string OrderNumber { get; set; }
        public string HashPaymentCode { set; get; }
        public string PersonDetails { set; get; }
        public string CompanyDetails { set; get; }

    }
}