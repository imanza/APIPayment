using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace APIRestPayment.Models.POSTModels
{
    public class OAuthServerCheckUserAccountPOSTModel
    {
        public string PayerAccountNumber { set; get; }
        public string PINCode { set; get; }
        public string DateTimeofRequest { set; get; }
    }
}