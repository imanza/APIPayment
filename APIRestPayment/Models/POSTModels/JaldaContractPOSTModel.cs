using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace APIRestPayment.Models.POSTModels
{
    public class JaldaContractPOSTModel
    {
        public string PayeeAccountNumber { set; get; }
        public Decimal MaxAmount { set; get; }
        public string Currency { set; get; }
        public Decimal PricePerThick { set; get; }
        public int StartSerialNumber  { set; get; }
        public string OrderNumber { set; get; }
    }
}