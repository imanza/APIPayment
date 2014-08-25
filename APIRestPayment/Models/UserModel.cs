using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace APIRestPayment.Models
{
    public class UserModel
    {
        public string Url { set; get; }
        public string Name { set; get; }
        public bool IsActive { set; get; }
        
        public IList<AccountModel> AccountS { set; get; }

    }
}