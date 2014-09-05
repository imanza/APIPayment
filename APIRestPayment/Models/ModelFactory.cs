using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Web;
using APIRestPayment.Constants;

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

        #region Public

        public PaymentModel Create(CASPaymentDTO.Domain.Transactions transaction)
        {
            if (transaction == null) throw new NullReferenceException();

            AccountModel source = null;
            if (transaction.Showsender) source = this.CreateWithoutCircularReference(transaction.SourceAccountItem);

            return new PaymentModel()
            {
                Url = _UrlHelper.Link("Payments", new { id = transaction.Id }),
                Trackingnumber = transaction.Id.ToString(),
                ExecutionDate = DateFuncs.ToShamsiCal( transaction.Executiondatetime),
                SourceAccount = source,
                DestinationAccount = this.CreateWithoutCircularReference(transaction.DestinationAccountItem),
                Amount = transaction.Amount,
                Currency = transaction.CurrencyTypeItem.Name,
                Type = transaction.TransactionTypeItem.Name,
                Status = transaction.PaymentStatusItem.Name,
            };
        }

        public AccountModel Create(CASPaymentDTO.Domain.Account account , DataAccessTypes level_of_detail)
        {
            if (account == null) throw new NullReferenceException();
            AccountModel result = new AccountModel();
            result.Url = _UrlHelper.Link("Accounts", new { id = account.Id });
            result.Accountnumber = account.Accountnumber;
            result.UserItem = CreateWithoutCircularReference(account.UsersItem);
            if(level_of_detail != DataAccessTypes.Anonymous){    
                result.Dateofopening = DateFuncs.ToShamsiCal(account.Dateofopening);
                result.Paymentcode = account.Paymentcode;
                result.IsActive = account.IsActive;
                result.Balance = account.Balance;
                result.AccountType = account.AccountTypeItem.Name;
                result.Currency = account.CurrencyTypeItem.Name;
            }
            
            return result;
        }

        public UserModel Create(CASPaymentDTO.Domain.Users user , DataAccessTypes level_of_detail)
        {
            UserModel result = new UserModel();
            result.UserType = user.UserType;
            if (user.UserType == "LP") result.CompanyDetails = this.Create(user.LegalPersonItem);
            else result.PersonDetails = this.Create(user.RealPersonItem , level_of_detail);
            result.Url = _UrlHelper.Link("Users", new { id = user.Id });
            if(level_of_detail != DataAccessTypes.Anonymous){
                result.CorrespondingNationalNumber = user.Nationalnumber;
                result.MobileNumber = user.Mobilenumber;
                result.TelephoneNumber = user.Mobilenumber;
                result.Email = user.Email;
                result.PostalCode = user.Postalcode;
                result.Address = user.Address;
                result.Country = (user.CountryItem != null) ? user.CountryItem.Name : null;
                result.Province = (user.ProvinceItem != null) ? user.ProvinceItem.Name : null;
                result.City = (user.CityItem != null) ? user.CityItem.Name : null;
                result.IsActive = user.IsActive;
                //////////
                IList<ApplicationModel> apps = new List<ApplicationModel>();
                foreach (CASPaymentDTO.Domain.Application ap in user.ApplicationS) apps.Add(this.Create(ap));
                result.ApplicationS = apps;
                //////////
                IList<BankAccountModel> bankaccount = new List<BankAccountModel>();
                foreach (CASPaymentDTO.Domain.BankAccount ba in user.BankAccountS) bankaccount.Add(this.Create(ba));
                result.BankAccountS = bankaccount;
                //////////
                IList<AccountModel> accounts = new List<AccountModel>();
                foreach (CASPaymentDTO.Domain.Account ac in user.AccountS) accounts.Add(this.CreateWithoutCircularReference(ac));
                result.AccountS = accounts;
                //////////
            }
            return result;
        }

        #endregion

        #region Private Model
        private RealPersonModel Create(CASPaymentDTO.Domain.RealPerson realPerson, DataAccessTypes level_of_detail)
        {
            RealPersonModel result = new RealPersonModel();
            result.FirstName = realPerson.Firstname;
            result.LastName = realPerson.Lastname;
            if (level_of_detail != DataAccessTypes.Anonymous)
            {
                result.FatherName = realPerson.Fathername;
                result.Dateofbirth =  DateFuncs.ToShamsiCal(realPerson.Dateofbirth).Date;
            }
            return result;
        }

        private LegalPersonModel Create(CASPaymentDTO.Domain.LegalPerson legalPerson)
        {
            return new LegalPersonModel
            {
                CompanyName = legalPerson.Companyname,
                RegistrationNumber = legalPerson.Registrationnumber
            };
        }

        private BankAccountModel Create(CASPaymentDTO.Domain.BankAccount bankAccount)
        {
            return new BankAccountModel
            {
               Accountnumber = bankAccount.Accountnumber,
               Bank = bankAccount.Bank,
               Branch = bankAccount.Branch,
               Iban = bankAccount.Iban
            };
        }

        private ApplicationModel Create(CASPaymentDTO.Domain.Application application)
        {
            return new ApplicationModel
            {
                Name = application.Name,
                Applicationcode = application.Applicationcode
            };
        }
        #endregion

        #endregion

        #region Create Without Circular Reference

        private AccountModel CreateWithoutCircularReference(CASPaymentDTO.Domain.Account account)
        {
            if (account == null) throw new NullReferenceException();
            AccountModel result = new AccountModel();
            result.Url = _UrlHelper.Link("Accounts", new { id = account.Id });
            result.Accountnumber = account.Accountnumber;
            result.UserItem = CreateWithoutCircularReference(account.UsersItem);
            return result;
        }

        private UserModel CreateWithoutCircularReference(CASPaymentDTO.Domain.Users user)
        {
            if (user == null) throw new NullReferenceException();
            UserModel result = new UserModel();
            result.UserType = user.UserType;
            if (user.UserType == "LP") result.CompanyDetails = this.Create(user.LegalPersonItem);
            else result.PersonDetails = this.Create(user.RealPersonItem, DataAccessTypes.Anonymous);
            result.Url = _UrlHelper.Link("Users", new { id = user.Id });

            return result;
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