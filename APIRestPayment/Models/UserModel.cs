using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace APIRestPayment.Models
{
    public class UserModel
    {
        public long? Id { set; get; }
        public string Url { set; get; }
        public string UserType { set; get; }
        public string CorrespondingNationalNumber { set; get; }
        public RealPersonModel PersonDetails { set; get; }
        public LegalPersonModel CompanyDetails { set; get; }
        public string MobileNumber { set; get; }
        public string TelephoneNumber { set; get; }
        public string Email { set; get; }
        public string PostalCode { set; get; }
        public string Address { set; get; }
        public bool? IsActive { set; get; }

        public string Country { set; get; }
        public string City { set; get; }
        public string Province { set; get; }
        public IList<AccountModel> AccountS { set; get; }
        public IList<ApplicationModel> ApplicationS { set; get; }
        public IList<BankAccountModel> BankAccountS { set; get; }

    }
}