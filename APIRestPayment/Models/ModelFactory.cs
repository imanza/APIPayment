﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Web;
using APIRestPayment.Constants;
using APIRestPayment.Models.POSTModels;
using System.Security.Claims;

namespace APIRestPayment.Models
{
    public class ModelFactory
    {
        private System.Web.Http.Routing.UrlHelper _UrlHelper;

        #region Handlers

        private CASPaymentDAO.DataHandler.CurrencyTypeDataHandler currencyHandler = new CASPaymentDAO.DataHandler.CurrencyTypeDataHandler(WebApiApplication.SessionFactory);
        private CASPaymentDAO.DataHandler.AccountTypeDataHandler accountTypeHandler = new CASPaymentDAO.DataHandler.AccountTypeDataHandler(WebApiApplication.SessionFactory);
        private CASPaymentDAO.DataHandler.UsersDataHandler userHandler = new CASPaymentDAO.DataHandler.UsersDataHandler(WebApiApplication.SessionFactory);
        private CASPaymentDAO.DataHandler.TransactionTypeDataHandler transactionTypeHandler = new CASPaymentDAO.DataHandler.TransactionTypeDataHandler(WebApiApplication.SessionFactory);
        private CASPaymentDAO.DataHandler.AccountDataHandler accountHandler = new CASPaymentDAO.DataHandler.AccountDataHandler(WebApiApplication.SessionFactory);
        private CASPaymentDAO.DataHandler.JaldaContractDataHandler jaldaContractHandler = new CASPaymentDAO.DataHandler.JaldaContractDataHandler(WebApiApplication.SessionFactory);

        //private void ChangeSessionofHandlers(NHibernate.ISessionFactory sessionFactory){
        //    currencyHandler = new CASPaymentDAO.DataHandler.CurrencyTypeDataHandler(sessionFactory);
        //    accountTypeHandler = new CASPaymentDAO.DataHandler.AccountTypeDataHandler(sessionFactory);
        //    accountHandler = new CASPaymentDAO.DataHandler.AccountDataHandler(sessionFactory);
        //    userHandler = new CASPaymentDAO.DataHandler.UsersDataHandler(sessionFactory);
        //    transactionTypeHandler = new CASPaymentDAO.DataHandler.TransactionTypeDataHandler(sessionFactory);
        //    jaldaContractHandler = new CASPaymentDAO.DataHandler.JaldaContractDataHandler(sessionFactory);
        //}

        #endregion

        public ModelFactory(HttpRequestMessage request)
        {
            _UrlHelper = new System.Web.Http.Routing.UrlHelper(request);
        }

        #region Create Models

        #region Public

        public TransactionModel Create(CASPaymentDTO.Domain.Transactions transaction)
        {
            if (transaction == null) throw new NullReferenceException("transaction");

            AccountModel source = null;
            if (transaction.Showsender != null && (bool)transaction.Showsender) source = this.CreateWithoutCircularReference(transaction.SourceAccountItem);

            return new TransactionModel()
            {
                Url = _UrlHelper.Link("GetPayment", new { id = transaction.Id }),
                Trackingnumber = transaction.Trackingnumber,
                ExecutionDate = DateFuncs.ToShamsiCal(transaction.Executiondatetime),
                SourceAccount = source,
                DestinationAccount = this.CreateWithoutCircularReference(transaction.DestinationAccountItem),
                Amount = transaction.Amount,
                Currency = transaction.CurrencyTypeItem.Name,
                Type = transaction.TransactionTypeItem.Name,
                Status = transaction.PaymentStatusItem.Name,
                Description = transaction.Description,
            };
        }

        public AccountModel Create(CASPaymentDTO.Domain.Account account, DataAccessTypes level_of_detail)
        {
            if (account == null) throw new NullReferenceException();
            AccountModel result = new AccountModel();
            result.Url = _UrlHelper.Link("GetAccount", new { id = account.Id });
            result.Accountnumber = account.Accountnumber;
            if (level_of_detail != DataAccessTypes.Anonymous)
            {
                result.UserItem = CreateWithoutCircularReference(account.UsersItem);
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

        public TransferPaymentResultModel Create(PaymentPOSTModel paymodel)
        {
            if (paymodel == null) throw new NullReferenceException();
            //Get Payee Account ===> We are sure that this account was in the DB ==> Get User Item from Account
            CASPaymentDTO.Domain.Users PayeeUser = (accountHandler.Search(new CASPaymentDTO.Domain.Account { Accountnumber = paymodel.PayeeAccountNumber }).Cast<CASPaymentDTO.Domain.Account>().FirstOrDefault()).UsersItem;
            TransferPaymentResultModel result = new TransferPaymentResultModel();
            result.OrderNumber = paymodel.OrderNumber;
            result.TransactionType = TransactionTypes.Transfer;
            result.HashPaymentCode = paymodel.HashPaymentCode;
            if (PayeeUser.UserType == "LP") result.CompanyDetails = PayeeUser.LegalPersonItem.Companyname + " ";
            else result.PersonDetails = PayeeUser.RealPersonItem.Firstname + " " + PayeeUser.RealPersonItem.Lastname;
            return result;
        }

        public PaymentResultModel Create(string resultOfPayment, string errorMessage, string TrackingNumber ,string OrderNumber )
        {
            PaymentResultModel result = new PaymentResultModel
            {
                Error = errorMessage,
                OrderNumber = OrderNumber,
                TimeSpan = Constants.DateFuncs.Get_String_Date(Constants.DateFuncs.ToShamsiCal(DateTime.Now)),
                TrackingNumber = TrackingNumber,
                ResultOfPayment = resultOfPayment
            };
            return result;
        }

        public UserModel Create(CASPaymentDTO.Domain.Users user, DataAccessTypes level_of_detail)
        {
            UserModel result = new UserModel();
            result.UserType = user.UserType;
            result.Url = _UrlHelper.Link("GetUser", new { id = user.Id });
            if (level_of_detail != DataAccessTypes.Anonymous)
            {
                if (user.UserType == "LP") result.CompanyDetails = this.Create(user.LegalPersonItem);
                else result.PersonDetails = this.Create(user.RealPersonItem, level_of_detail);
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
                foreach (CASPaymentDTO.Domain.Application ap in user.ApplicationS) if (ap.State == 0) apps.Add(this.Create(ap));
                result.ApplicationS = apps;
                //////////
                IList<BankAccountModel> bankaccount = new List<BankAccountModel>();
                foreach (CASPaymentDTO.Domain.BankAccount ba in user.BankAccountS) if (ba.State == 0) bankaccount.Add(this.Create(ba));
                result.BankAccountS = bankaccount;
                //////////
                IList<AccountModel> accounts = new List<AccountModel>();
                foreach (CASPaymentDTO.Domain.Account ac in user.AccountS) if (ac.State == 0) accounts.Add(this.CreateWithoutCircularReference(ac));
                result.AccountS = accounts;
                //////////
            }
            return result;
        }

        public Models.JaldaThickModel Create(CASPaymentDTO.Domain.JaldaTicks jaldaThick)
        {
            if (jaldaThick == null) throw new NullReferenceException();
            return new JaldaThickModel
            {
                Id = jaldaThick.Id,
                JaldaContractID = jaldaThick.JaldaContractItem.Id,
                JaldaThickType = jaldaThick.Type,
                OrderNumber = jaldaThick.Ordernumber,
                SerialNumber = (uint)jaldaThick.Serialnumber,
                SubmitDateTime = jaldaThick.SubmissionDateTime,
            };
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
                result.Dateofbirth = DateFuncs.ToShamsiCal(realPerson.Dateofbirth).Date;
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
            if (account == null) throw new NullReferenceException("account");
            AccountModel result = new AccountModel();
            result.Url = _UrlHelper.Link("GetAccount", new { id = account.Id });
            result.Accountnumber = account.Accountnumber;
            return result;
        }

        private UserModel CreateWithoutCircularReference(CASPaymentDTO.Domain.Users user)
        {
            if (user == null) throw new NullReferenceException("user");
            UserModel result = new UserModel();
            result.UserType = user.UserType;
            //if (user.UserType == "LP") result.CompanyDetails = this.Create(user.LegalPersonItem);
            //else result.PersonDetails = this.Create(user.RealPersonItem, DataAccessTypes.Anonymous);
            //
            result.Url = _UrlHelper.Link("GetUser", new { id = user.Id });
            return result;
        }
        #endregion


        #region Parse For POST Methods

        public CASPaymentDTO.Domain.Account Parse(AccountPOSTModel accountPOSTModel, out string ErrorMessage)
        {
            if (accountPOSTModel.UserId == null || accountPOSTModel.AccountType == null || accountPOSTModel.Currency == null)
            {
                ErrorMessage = "Incomplete Account Data.";
                return null;
            }
            CASPaymentDTO.Domain.Account account = new CASPaymentDTO.Domain.Account();
            try
            {
                CASPaymentDTO.Domain.AccountType query = new CASPaymentDTO.Domain.AccountType() { NameEn = accountPOSTModel.AccountType };
                CASPaymentDTO.Domain.CurrencyType ct = currencyHandler.Search(new CASPaymentDTO.Domain.CurrencyType() { Charcode = accountPOSTModel.Currency }).Cast<CASPaymentDTO.Domain.CurrencyType>().FirstOrDefault();
                CASPaymentDTO.Domain.AccountType at = accountTypeHandler.Search(query).Cast<CASPaymentDTO.Domain.AccountType>().FirstOrDefault();
                //CASPaymentDTO.Domain.AccountType at = accountTypeHandler.Search(new CASPaymentDTO.Domain.AccountType() { Name = accountPOSTModel.AccountType,  }).Cast<CASPaymentDTO.Domain.AccountType>().First();
                if (!object.Equals(ct, default(CASPaymentDTO.Domain.CurrencyType))) account.CurrencyTypeItem = ct;
                if (!object.Equals(at, default(CASPaymentDTO.Domain.AccountType))) account.AccountTypeItem = at;
                account.UsersItem = userHandler.GetEntity(accountPOSTModel.UserId);
                account.IsPublic = (accountPOSTModel.IsPublic != null) ? accountPOSTModel.IsPublic : true;
                ErrorMessage = "";
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
            if (paymentPOSTModel.PayeeAccountNumber == null || paymentPOSTModel.TransactionType == null || paymentPOSTModel.Amount == null  || paymentPOSTModel.RedirectUrl == null)
            {
                ErrorMessage = "Incomplete Payment Data.";
                return null;
            }
            CASPaymentDTO.Domain.Transactions payment = new CASPaymentDTO.Domain.Transactions();
            try
            {
                CASPaymentDTO.Domain.CurrencyType ct = currencyHandler.Search(new CASPaymentDTO.Domain.CurrencyType() { Charcode = paymentPOSTModel.Currency }).Cast<CASPaymentDTO.Domain.CurrencyType>().FirstOrDefault();
                CASPaymentDTO.Domain.TransactionType tt = transactionTypeHandler.Search(new CASPaymentDTO.Domain.TransactionType() { Name = paymentPOSTModel.TransactionType }).Cast<CASPaymentDTO.Domain.TransactionType>().FirstOrDefault();
                CASPaymentDTO.Domain.Account destAcc = accountHandler.Search(new CASPaymentDTO.Domain.Account() { Accountnumber = paymentPOSTModel.PayeeAccountNumber }).Cast<CASPaymentDTO.Domain.Account>().FirstOrDefault();
                if (object.Equals(ct, default(CASPaymentDTO.Domain.CurrencyType)) || object.Equals(tt, default(CASPaymentDTO.Domain.TransactionType)) || object.Equals(destAcc, default(CASPaymentDTO.Domain.Account))) throw new System.Exception("Invalid data for transaction.");
                payment.CurrencyTypeItem = ct;
                payment.Amount = paymentPOSTModel.Amount;
                payment.TransactionTypeItem = tt;
                payment.DestinationAccountItem = destAcc;
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

        private IList<Tmodel> ExtractModel<Tmodel, Tsource>(IList<Tsource> sourceList)
        {
            IList<Tmodel> result = new List<Tmodel>();

            foreach (Tsource ts in sourceList)
            {
                Tmodel elementModel = (Tmodel)this.Create(ts);
                if (elementModel != null) result.Add(elementModel);
            }
            return result;
        }


        private object Create(object ts)
        {

            return null;
        }


        internal CASPaymentDTO.Domain.JaldaContract Parse(JaldaContractPOSTModel jaldaPOSTModel, out string ParseErrorMessage)
        {

            throw new NotImplementedException();
        }

        internal CASPaymentDTO.Domain.JaldaTicks Parse(JaldaThickPOSTModel jaldaThickPOSTModel, CASPaymentDTO.Domain.JaldaContract jaldaContract, out string ErrorMessage)
        {
            CASPaymentDTO.Domain.JaldaTicks jaldaThick = new CASPaymentDTO.Domain.JaldaTicks();
            jaldaThick.JaldaContractItem = jaldaContract;
            jaldaThick.Ordernumber = jaldaThickPOSTModel.OrderNumber;
            jaldaThick.Serialnumber = (int)jaldaThickPOSTModel.SerialNumber;
            jaldaThick.Type = jaldaThickPOSTModel.JaldaThickType;
            jaldaThick.SubmissionDateTime = DateTime.UtcNow;
            ErrorMessage = "";
            return jaldaThick;
        }
    }
}