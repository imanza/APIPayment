using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Web;

namespace APIRestPayment.Models
{
    public class ModelFactory
    {
        private System.Web.Http.Routing.UrlHelper _UrlHelper;

        public ModelFactory(HttpRequestMessage request)
        {
            _UrlHelper = new System.Web.Http.Routing.UrlHelper(request);
        }


        #region Create Models

        public PaymentModel Create(CASPaymentDTO.Domain.Transactions transaction)
        {
            if (transaction == null) throw new NullReferenceException();

            AccountModel source = null;
            if (transaction.Showsender) source = this.CreateWithoutCircularReference(transaction.SourceAccountItem);

            return new PaymentModel()
            {
                Url = _UrlHelper.Link("Payments", new { id = transaction.Id }),
                Trackingnumber = transaction.Id.ToString(),
                ExecutionDate = transaction.Executiondatetime,
                SourceAccount = source,
                DestinationAccount = this.CreateWithoutCircularReference(transaction.DestinationAccountItem),
                Amount = transaction.Amount,
                Currency = transaction.CurrencyTypeItem.Name,
                Type = transaction.TransactionTypeItem.Name,
                Status = transaction.PaymentStatusItem.Name,
            };
        }

        public AccountModel Create(CASPaymentDTO.Domain.Account account)
        {
            if (account == null) throw new NullReferenceException();
            return new AccountModel()
            {
                Url = _UrlHelper.Link("Accounts", new { id = account.Id }),
                Accountnumber = account.Accountnumber,
                Dateofopening = account.Dateofopening,
                Paymentcode = account.Paymentcode,
                UserItem = CreateWithoutCircularReference(account.UsersItem)
            };
        }

        public UserModel Create(CASPaymentDTO.Domain.Users user)
        {
            if (user == null) throw new NullReferenceException();
            string name = "";
            if (user.UserType == "LP") name = user.LegalPersonItem.Companyname;
            else name = user.RealPersonItem.Firstname + " " + user.RealPersonItem.Lastname;
            return new UserModel()
            {
                Url = _UrlHelper.Link("Users", new { id = user.Id }),
                Name = user.Username,
                IsActive = user.IsActive
            };
        }

        #endregion

        #region Create Without Circular Reference

        private AccountModel CreateWithoutCircularReference(CASPaymentDTO.Domain.Account account)
        {
            if (account == null) throw new NullReferenceException();
            return new AccountModel()
            {
                Accountnumber = account.Accountnumber,
                Dateofopening = account.Dateofopening,
                Paymentcode = account.Paymentcode
            };
        }

        private UserModel CreateWithoutCircularReference(CASPaymentDTO.Domain.Users user)
        {
            if (user == null) throw new NullReferenceException();
            string name = "";
            if (user.UserType == "LP") name = user.LegalPersonItem.Companyname;
            else name = user.RealPersonItem.Firstname + " " + user.RealPersonItem.Lastname;
            
            return new UserModel()
            {
                Name = name,
                IsActive = user.IsActive
            };
        }
        #endregion

        private IList<Tmodel> ExtractModel<Tmodel,Tsource>(IList<Tsource> sourceList )
        {
            IList<Tmodel> result = new List<Tmodel>(); 

            foreach (Tsource ts in sourceList)
            {
                Tmodel elementModel = (Tmodel)this.Create(ts);
                if(elementModel != null)result.Add(elementModel);
            }
            return result;
        }

        private object Create(object ts)
        {
            
            return null;
        }

    }
}