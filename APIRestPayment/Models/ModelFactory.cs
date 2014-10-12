using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Web;
using APIRestPayment.Constants;
using APIRestPayment.Models.POSTModels;

namespace APIRestPayment.Models
{
    public class ModelFactory
    {
        private System.Web.Http.Routing.UrlHelper _UrlHelper;

        private CASPaymentDAO.DataHandler.CurrencyTypeDataHandler currencyHandler = new CASPaymentDAO.DataHandler.CurrencyTypeDataHandler(WebApiApplication.SessionFactory);
        private CASPaymentDAO.DataHandler.AccountTypeDataHandler accountTypeHandler = new CASPaymentDAO.DataHandler.AccountTypeDataHandler(WebApiApplication.SessionFactory);
        private CASPaymentDAO.DataHandler.UsersDataHandler userHandler = new CASPaymentDAO.DataHandler.UsersDataHandler(WebApiApplication.SessionFactory);
        private CASPaymentDAO.DataHandler.TransactionTypeDataHandler transactionTypeHandler = new CASPaymentDAO.DataHandler.TransactionTypeDataHandler(WebApiApplication.SessionFactory);
        private CASPaymentDAO.DataHandler.AccountDataHandler accountHandler = new CASPaymentDAO.DataHandler.AccountDataHandler(WebApiApplication.SessionFactory);
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
            if ((bool)transaction.Showsender) source = this.CreateWithoutCircularReference(transaction.SourceAccountItem);

            return new PaymentModel()
            {
                Url = _UrlHelper.Link("Payments", new { id = transaction.Id }),
                Trackingnumber = transaction.Id.ToString(),
                ExecutionDate =DateFuncs.ToShamsiCal( transaction.Executiondatetime),
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
                result.Id = account.Id;
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
                result.Id = user.Id;
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
                Applicationcode = application.ClientID
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


        #region Parse For POST Methods

        public CASPaymentDTO.Domain.Account Parse(AccountPOSTModel accountPOSTModel , out string ErrorMessage)
        {
            if (accountPOSTModel.UserId == null || accountPOSTModel.AccountType == null)
            {
                ErrorMessage = "Incomplete Account Data.";
                return null;
            }
            CASPaymentDTO.Domain.Account account = new CASPaymentDTO.Domain.Account();
            try
            {
                CASPaymentDTO.Domain.CurrencyType ct = currencyHandler.Search(new CASPaymentDTO.Domain.CurrencyType() { Name = accountPOSTModel.Currency }).Cast<CASPaymentDTO.Domain.CurrencyType>().First();
                CASPaymentDTO.Domain.AccountType at = accountTypeHandler.Search(new CASPaymentDTO.Domain.AccountType() { Name = accountPOSTModel.AccountType }).Cast<CASPaymentDTO.Domain.AccountType>().First();
                account.CurrencyTypeItem = ct;
                account.AccountTypeItem = at;
                account.UsersItem = userHandler.GetEntity(accountPOSTModel.UserId);
                account.IsPublic = (accountPOSTModel.IsPublic != null) ? accountPOSTModel.IsPublic : true;
                ErrorMessage="";
                return account;
            }
            catch (Exception)
            {
                ErrorMessage = "Account data were not valid.";
                return null;
            }
        }


        public CASPaymentDTO.Domain.Transactions Parse(PaymentPOSTModel paymentPOSTModel, out string ErrorMessage)
        {
            if (paymentPOSTModel.PayeeAccountNumber == null || paymentPOSTModel.TransactionType == null || paymentPOSTModel.Amount == null || paymentPOSTModel.RequestNonce == null || paymentPOSTModel.RedirectUrl == null)
            {
                ErrorMessage = "Incomplete Payment Data.";
                return null;
            }
            //TODO check for nonces and datetimes to prevent system exposed with REPLY ATTACK
            CASPaymentDTO.Domain.Transactions payment = new CASPaymentDTO.Domain.Transactions();
            try
            {
                CASPaymentDTO.Domain.CurrencyType ct = currencyHandler.Search(new CASPaymentDTO.Domain.CurrencyType() { Name = paymentPOSTModel.Currency }).Cast<CASPaymentDTO.Domain.CurrencyType>().First();
                CASPaymentDTO.Domain.TransactionType tt = transactionTypeHandler.Search(new CASPaymentDTO.Domain.TransactionType() { Name = paymentPOSTModel.TransactionType }).Cast<CASPaymentDTO.Domain.TransactionType>().First();
                payment.CurrencyTypeItem = ct;
                payment.Amount = paymentPOSTModel.Amount;
                payment.TransactionTypeItem = tt;
                payment.DestinationAccountItem = accountHandler.Search(new CASPaymentDTO.Domain.Account() { Accountnumber = paymentPOSTModel.PayeeAccountNumber }).Cast<CASPaymentDTO.Domain.Account>().First();
                ErrorMessage = "";
                return payment;
            }
            catch (Exception)
            {
                ErrorMessage = "Payment data were not valid.";
                return null;
            }
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