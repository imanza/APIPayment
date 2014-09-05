using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace APIRestPayment.Models
{
    public class RealPersonModel
    { 
        public  string FirstName { set; get; }
        public  string LastName { set; get; }
        public  string FatherName { set; get; }
        public  DateTime? Dateofbirth { set; get; }
        
    }
}