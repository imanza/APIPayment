using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace APIRestPayment.Models
{
    public class QueryResponseModel
    {
        public int TotalCount;
        public int TotalPages;
        public string PrevPageLink;
        public string NextPageLink;
        public object Data;

    }
}