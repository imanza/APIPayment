using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace APIRestPayment.Models
{
    public class JaldaThickModel
    {
        public long? Id { set; get; }
        public long? JaldaContractID { set; get; }
        public string JaldaThickType { set; get; }
        public uint? SerialNumber { set; get; }
        public DateTime? SubmitDateTime { set; get; }
        public string OrderNumber { set; get; }
    }
}