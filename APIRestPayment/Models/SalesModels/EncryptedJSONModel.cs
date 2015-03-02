using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace APIRestPayment.Models.SalesModels
{
    public class EncryptedJSONModel
    {
        public string CipherText { get; set; }
        public string ClientID { get; set; }
        //public Type innerJSONModel { get; set; }
    }
}