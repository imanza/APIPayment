using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using APIRestPayment;
using System.Web.Http.Routing;
using APIRestPayment.Constants;
using System.Collections;
using System.Threading.Tasks;



namespace APIRestPayment.Controllers
{

    public class PaymentsController : BaseApiController
    {
        #region Handlers

        CASPaymentDAO.DataHandler.TransactionsDataHandler transactionHandler = new CASPaymentDAO.DataHandler.TransactionsDataHandler(WebApiApplication.SessionFactory);
        CASPaymentDAO.DataHandler.AccountDataHandler accountHandler = new CASPaymentDAO.DataHandler.AccountDataHandler(WebApiApplication.SessionFactory);
        CASPaymentDAO.DataHandler.SettingDataDataHandler settingDataHandler = new CASPaymentDAO.DataHandler.SettingDataDataHandler(WebApiApplication.SessionFactory);
        CASPaymentDAO.DataHandler.AccountDailyActivityDataHandler accountDailyActivityHandler = new CASPaymentDAO.DataHandler.AccountDailyActivityDataHandler(WebApiApplication.SessionFactory);
        CASPaymentDAO.DataHandler.PaymentStatusDataHandler paymentStatusHandler = new CASPaymentDAO.DataHandler.PaymentStatusDataHandler(WebApiApplication.SessionFactory);

        #endregion

        #region GET

        [Filters.GeneralAuthorization]
        public HttpResponseMessage GetPayment(long id)
        {
            try
            {
                CASPaymentDTO.Domain.Transactions searchedTransaction = this.transactionHandler.GetEntity(id);
                return Request.CreateResponse(HttpStatusCode.OK, new Models.QueryResponseModel
                {
                    meta = new Models.MetaModel
                    {
                        code = (int)HttpStatusCode.OK
                    },
                    data = TheModelFactory.Create(searchedTransaction)
                });
            }
            catch (NHibernate.ObjectNotFoundException)
            {
                return Request.CreateResponse(HttpStatusCode.NotFound, new Models.QueryResponseModel
                {
                    meta = new Models.MetaModel
                    {
                        code = (int)HttpStatusCode.NotFound,
                        errorMessage = "Item was not found"
                    }
                });
            }
        }

        [Filters.GeneralAuthorization]
        public HttpResponseMessage Get(int page = 0, int pageSize = 10)
        {

            IList<CASPaymentDTO.Domain.Transactions> result = this.transactionHandler.SelectAll().Cast<CASPaymentDTO.Domain.Transactions>().ToList();
            //////////////////////////////////////////////////
            var totalCount = result.Count();
            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
            var urlHelper = new UrlHelper(Request);
            var prevLink = page > 0 ? urlHelper.Link("Payments", new { page = page - 1 }) : null;
            var nextLink = page < totalPages - 1 ? urlHelper.Link("Payments", new { page = page + 1 }) : null;
            ///////////////////////////////////////////////////
            var resultInModel = result
            .Skip(pageSize * page)
            .Take(pageSize)
            .ToList()
            .Select(s => TheModelFactory.Create(s));
            ////////////////////////////////////////////////////
            return Request.CreateResponse(HttpStatusCode.OK, new Models.QueryResponseModel
            {
                meta = new Models.MetaModel
                {
                    code = (int)HttpStatusCode.OK,
                },
                data = resultInModel.ToList(),
                pagination = new Models.PaginationModel
                {
                    TotalCount = totalCount,
                    TotalPages = totalPages,
                    PrevPageLink = prevLink,
                    NextPageLink = nextLink,
                }

            });
        }

        public HttpResponseMessage Get([FromUri]string PaymentPOSTModelJson/* string TransactionType,string Currency, string Amount,string PayeeAccountNumber,string RedirectUrl,string RequestNonce,string RequestDate*/)
        {
            string ParseErrorMessage;
            Newtonsoft.Json.JsonSerializerSettings jj = new Newtonsoft.Json.JsonSerializerSettings { NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore };
            APIRestPayment.Models.POSTModels.PaymentPOSTModel paymodel = (APIRestPayment.Models.POSTModels.PaymentPOSTModel)Newtonsoft.Json.JsonConvert.DeserializeObject(PaymentPOSTModelJson, typeof(APIRestPayment.Models.POSTModels.PaymentPOSTModel), jj);

            if (this.CheckValidityOFPOSTModel(paymodel, out ParseErrorMessage))
            {

                if (paymodel.TransactionType == Constants.TransactionTypes.Purchase)
                {
                    var response = Request.CreateResponse(HttpStatusCode.Moved);

                    string fullyQualifiedUrl = Request.RequestUri.GetLeftPart(UriPartial.Authority); // (http://localhost)
                    string Salesurl = fullyQualifiedUrl + "/" + Request.RequestUri.Segments[1] + "/Sales/SalesLaunch";
                    string[] Requestparams = { "paymentModelJSON" , "returnUrl" };
                    string[] Requestvalues = { PaymentPOSTModelJson , paymodel.RedirectUrl};
                    response.Headers.Location = new Uri(QueryStringFuncs.GenerateRequestStringAsync(Salesurl, Requestparams, Requestvalues));
                    //response.Headers.Location = new Uri( , UriKind.Relative);
                    return response;
                }
                else if (paymodel.TransactionType == TransactionTypes.Transfer)
                {
                    //TODO implement transfer

                }
                return Request.CreateResponse(HttpStatusCode.BadRequest, new Models.QueryResponseModel
                    {
                        meta = new Models.MetaModel
                        {
                            code = (int)HttpStatusCode.BadRequest,
                            errorMessage = " not implemented Yet!!!!",
                        }
                    });
            }
            else
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, new Models.QueryResponseModel
                {
                    meta = new Models.MetaModel
                    {
                        code = (int)HttpStatusCode.BadRequest,
                        errorMessage = ParseErrorMessage,
                    }
                });
            }
            //if (Decimal.TryParse(Amount, out paymentAmount))
            //{
            //    Models.POSTModels.PaymentPOSTModel paymentPOSTModel = new Models.POSTModels.PaymentPOSTModel
            //    {
            //        Amount = paymentAmount,
            //        Currency = Currency,
            //        PayeeAccountNumber = PayeeAccountNumber,
            //        RedirectUrl = RedirectUrl,
            //         //RequestDate = DateFuncs.ConvertStringToDate(RequestDate),
            //         RequestNonce = RequestNonce,
            //         TransactionType = TransactionType
            //    };
            //    var response = Request.CreateResponse(HttpStatusCode.Moved /*, new Object[]{paymentPOSTModel , transactionPAYEE} */);
            //    string fullyQualifiedUrl = Request.RequestUri.GetLeftPart(UriPartial.Authority);
            //    response.Headers.Location = new Uri(fullyQualifiedUrl + "/APIRestPayment" +"/Home/sales");
            //    //response.Headers.Location = new Uri( , UriKind.Relative);
            //    return response;
            //}
            //else
            //{
            //    return Request.CreateResponse(HttpStatusCode.BadRequest, new Models.QueryResponseModel
            //    {
            //        meta = new Models.MetaModel
            //        {
            //            code =(int) HttpStatusCode.BadRequest,
            //            errorType = "incompatible data type",
            //            errorMessage = "Amount is not convertible "
            //        }
            //    });
            //}
        }


        #endregion

        #region POST


        public HttpResponseMessage Post([FromBody] APIRestPayment.Models.POSTModels.PaymentPOSTModel paymentPOSTModel)
        {

            string ParseErrorMessage;
            CASPaymentDTO.Domain.Transactions transactionPAYEE = TheModelFactory.Parse(paymentPOSTModel, out ParseErrorMessage);

            if (transactionPAYEE == null)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, new Models.QueryResponseModel
                {
                    meta = new Models.MetaModel
                    {
                        code = (int)HttpStatusCode.BadRequest,
                        errorMessage = ParseErrorMessage,
                    }
                });
            }

            //if (CheckValidityOFPOST(transactionPAYEE))
            //{

            //}


            var response = Request.CreateResponse(HttpStatusCode.Moved /*, new Object[]{paymentPOSTModel , transactionPAYEE} */);
            string fullyQualifiedUrl = Request.RequestUri.GetLeftPart(UriPartial.Authority);
            response.Headers.Location = new Uri(fullyQualifiedUrl + "/Home/sales");
            //response.Headers.Location = new Uri( , UriKind.Relative);
            return response;


        }

        #endregion

        #region Checks

        private bool CheckValidityOFPOSTModel(Models.POSTModels.PaymentPOSTModel paymentModel, out string ErrorMessage)
        {
            if (paymentModel.PayeeAccountNumber == null || paymentModel.TransactionType == null || paymentModel.Amount == null || paymentModel.RequestNonce == null || paymentModel.RedirectUrl == null)
            {
                ErrorMessage = "Incomplete Payment Data.";
                return false;
            }
            if (paymentModel.TransactionType == Constants.TransactionTypes.Transfer && string.IsNullOrEmpty(paymentModel.PayerAccountNumber))
            {
                ErrorMessage = "PayerAccountNumber must be assigned in transfer requests";
                return false;
            }
            if (paymentModel.TransactionType == Constants.TransactionTypes.Purchase && string.IsNullOrEmpty(paymentModel.OrderNumber))
            {
                ErrorMessage = "OrderNumber must be assigned in sale requests";
                return false;
            }
            //TODO check for nonces and datetimes to prevent system exposed with REPLAY ATTACK

            CASPaymentDTO.Domain.Account PayeeAccNo = accountHandler.Search(new CASPaymentDTO.Domain.Account { Accountnumber = paymentModel.PayeeAccountNumber }).Cast<CASPaymentDTO.Domain.Account>().FirstOrDefault();
            if (object.Equals(PayeeAccNo, default(CASPaymentDTO.Domain.Account)))
            {
                ErrorMessage = "Payee Account Number is not valid";
                return false;
            }
            if (paymentModel.TransactionType == Constants.TransactionTypes.Transfer)
            {
                CASPaymentDTO.Domain.Account PayerAccNo = accountHandler.Search(new CASPaymentDTO.Domain.Account { Accountnumber = paymentModel.PayerAccountNumber }).Cast<CASPaymentDTO.Domain.Account>().FirstOrDefault();
                if (object.Equals(PayerAccNo, default(CASPaymentDTO.Domain.Account)))
                {
                    ErrorMessage = "Payer Account Number is not valid in this transfer request";
                    return false;
                }
            }
            ErrorMessage = "";
            return true;
        }

        public async Task<bool> CheckAccountAndPINCode(CASPaymentDTO.Domain.Account PayerAccount, string insertedPIN)
        {
            Microsoft.AspNet.Identity.PasswordVerificationResult res = await Task.Run(() =>
            {
                CASPaymentDTO.Domain.Users user = PayerAccount.UsersItem;
                Microsoft.AspNet.Identity.PasswordVerificationResult result = Microsoft.AspNet.Identity.PasswordVerificationResult.Failed;
                if (user != null)
                {
                    Microsoft.AspNet.Identity.PasswordHasher hasher = new Microsoft.AspNet.Identity.PasswordHasher();
                    result = hasher.VerifyHashedPassword(user.SecretS[0].Encryptionbysecret, insertedPIN);

                }
                return result;
            }).ConfigureAwait(false);
            return res == Microsoft.AspNet.Identity.PasswordVerificationResult.Success;
        }

        public async Task<bool> CheckBalance(CASPaymentDTO.Domain.Account PayerAccount, Decimal Amount)
        {
            return await Task.Run(() =>
             {
                 CASPaymentDTO.Domain.Account account = PayerAccount;
                 bool result = false;
                 if (!object.Equals(account, default(CASPaymentDTO.Domain.Account)))
                 {
                     if (Amount < account.Balance) result = true;
                 }
                 return result;
             }).ConfigureAwait(false);
        }

        public async Task<bool> CheckUpperLimit(CASPaymentDTO.Domain.Account PayerAccount, Decimal Amount, string TransactionType)
        {
            return await Task.Run(() =>
            {
                if (PayerAccount.AccountDailyActivityS.Count == 0) return true;
                string upperLimitString = settingDataHandler.Search(new CASPaymentDTO.Domain.SettingData { Name = "Limit" + TransactionType }).Cast<CASPaymentDTO.Domain.SettingData>().FirstOrDefault().Value;
                Decimal spentMoneyOnTodayTransactions = GetSpentMonetOnTodaysTransactions(PayerAccount.AccountDailyActivityS[0], TransactionType);
                Decimal upperLimitDecimal;
                bool result = false;
                if (Decimal.TryParse(upperLimitString, out upperLimitDecimal))
                {
                    if (spentMoneyOnTodayTransactions + Amount < upperLimitDecimal) result = true;
                }
                return result;
            }).ConfigureAwait(false);
        }

        private decimal GetSpentMonetOnTodaysTransactions(CASPaymentDTO.Domain.AccountDailyActivity accountDailyActivity, string TransactionType)
        {
            switch (TransactionType)
            {
                case Constants.TransactionTypes.Transfer: return (Decimal)accountDailyActivity.TransferAmount;
                case Constants.TransactionTypes.Purchase: return (Decimal)accountDailyActivity.PurchaseAmount;
                case Constants.TransactionTypes.Jalda: return (Decimal)accountDailyActivity.JaldaAmount;
            }
            return 0;
        }

        #endregion


        #region Perform Payment

        public string PerformPayment(CASPaymentDTO.Domain.Transactions payerTransaction)
        {
            string trackingNumber = GenerateUniqueTrackingNumber(payerTransaction.DestinationAccountItem.Accountnumber, payerTransaction.SourceAccountItem.Accountnumber);
            CASPaymentDTO.Domain.Transactions payeeTransaction = ReverseTransaction(payerTransaction);
            
            ///////Assign Tracking Number
            payerTransaction.Trackingnumber = trackingNumber;
            payeeTransaction.Trackingnumber = trackingNumber;
            
            //////Account Read
            CASPaymentDTO.Domain.Account payerAccount = payerTransaction.DestinationAccountItem;
            CASPaymentDTO.Domain.Account payeeAccount = payeeTransaction.DestinationAccountItem;
            
            /////Assign Execution Date 
            DateTime ExecutionDateTime = DateTime.UtcNow;
            payerTransaction.Executiondatetime = ExecutionDateTime;
            payeeTransaction.Executiondatetime = ExecutionDateTime;
            
            /////Assign Current Balance
            payerTransaction.CurrentBalance = payerAccount.Balance + payerTransaction.Amount;
            payeeTransaction.CurrentBalance = payeeAccount.Balance + payeeTransaction.Amount;

            /////Assign Account Balance
            payerAccount.Balance = payerTransaction.CurrentBalance;
            payeeAccount.Balance = payeeTransaction.CurrentBalance;

            /////Assign Daily Activity For Current Purchase To Payer
            bool hasDailyActivity = false;
            CASPaymentDTO.Domain.AccountDailyActivity accountDailyActivityForPayer = GetPayerTodayActivity(payerAccount,payerTransaction.TransactionTypeItem.NameEn ,(Decimal) payerTransaction.Amount, out hasDailyActivity );



            CASPaymentDTO.Domain.PaymentStatus completePaymentStatus = paymentStatusHandler.Search(new CASPaymentDTO.Domain.PaymentStatus { NameEn = PaymentStatusTypes.Completed }).Cast<CASPaymentDTO.Domain.PaymentStatus>().FirstOrDefault();
            //bool Completed = false;
            using (var session = WebApiApplication.SessionFactory.OpenSession())
            using (var tx = session.BeginTransaction())
            {
                try
                {
                    accountHandler.Update(payerAccount);
                    accountHandler.Update(payerAccount);
                    payerTransaction.PaymentStatusItem = completePaymentStatus;
                    payeeTransaction.PaymentStatusItem = completePaymentStatus;
                    transactionHandler.Save(payerTransaction);
                    transactionHandler.Save(payeeTransaction);

                    if (hasDailyActivity) accountDailyActivityHandler.Update(accountDailyActivityForPayer);
                    else accountDailyActivityHandler.Save(accountDailyActivityForPayer);
                    // execute code that uses the session 
                    tx.Commit();
                }
                catch (NHibernate.StaleStateException ex)
                {
                    tx.Rollback();

                }
                if (tx.WasCommitted)
                {
                    //TODO create the new transaction model and pass it to client web site
                    //TODO send the above model to Payer Email
                    return "Success";
                }
                else
                {
                    //TODO redirect user with Error parameters set
                }
            }
            return "Success";
        }

        private CASPaymentDTO.Domain.AccountDailyActivity GetPayerTodayActivity(CASPaymentDTO.Domain.Account payerAccount, string TransactionType , Decimal AmountToPay, out bool hasDailyActivity)
        {   
            Decimal ABSAmountToPay = Math.Abs(AmountToPay);
            if (payerAccount.AccountDailyActivityS.Count == 0)
            {
                hasDailyActivity = false;                
                CASPaymentDTO.Domain.AccountDailyActivity newAccountActivity = new CASPaymentDTO.Domain.AccountDailyActivity();
                newAccountActivity.AccountItem = payerAccount;
                newAccountActivity.JaldaAmount = (TransactionType == TransactionTypes.Jalda) ? ABSAmountToPay : 0;
                newAccountActivity.PurchaseAmount = (TransactionType == TransactionTypes.Purchase) ? ABSAmountToPay : 0;
                newAccountActivity.TransferAmount = (TransactionType == TransactionTypes.Transfer) ? ABSAmountToPay : 0;
                return newAccountActivity;
            }
            else
            {
                hasDailyActivity = true;
                payerAccount.AccountDailyActivityS[0].JaldaAmount += (TransactionType == TransactionTypes.Jalda) ? ABSAmountToPay : 0;
                payerAccount.AccountDailyActivityS[0].PurchaseAmount += (TransactionType == TransactionTypes.Purchase) ? ABSAmountToPay : 0;
                payerAccount.AccountDailyActivityS[0].TransferAmount += (TransactionType == TransactionTypes.Transfer) ? ABSAmountToPay : 0;
                return payerAccount.AccountDailyActivityS[0];
            }
        }

        private CASPaymentDTO.Domain.Transactions ReverseTransaction(CASPaymentDTO.Domain.Transactions mainTransaction)
        {
            CASPaymentDTO.Domain.Transactions reverseTransaction = new CASPaymentDTO.Domain.Transactions();
            reverseTransaction.Amount = (-1) * mainTransaction.Amount;
            reverseTransaction.CurrencyTypeItem = mainTransaction.CurrencyTypeItem;
            reverseTransaction.DestinationAccountItem = mainTransaction.SourceAccountItem;
            reverseTransaction.SourceAccountItem = mainTransaction.DestinationAccountItem;
            reverseTransaction.TransactionTypeItem = mainTransaction.TransactionTypeItem;
            return reverseTransaction;
        }

        public string GenerateUniqueTrackingNumber(string PayeeAccountNumber , string PayerAccountNumber)
        {
            string trackingNumber ="";
            bool isTrackingNumberUnique = false;
            do
            {
                string newGuid = Guid.NewGuid().ToString("n");
                trackingNumber = newGuid.GetHashCode().ToString() + (PayeeAccountNumber + PayerAccountNumber).GetHashCode().ToString();
                isTrackingNumberUnique = transactionHandler.Search(new CASPaymentDTO.Domain.Transactions { Trackingnumber = trackingNumber }).Count == 0;
            } while (!isTrackingNumberUnique);
            return trackingNumber;
        }


        #endregion

    }
}
