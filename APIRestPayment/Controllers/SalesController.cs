using APIRestPayment.App_Start.MessageHandlers.Crypto;
using Nito.AsyncEx;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace APIRestPayment.Controllers
{
    public class SalesController : Controller
    {
        #region Handlers

        CASPaymentDAO.DataHandler.UsersDataHandler userHandler = new CASPaymentDAO.DataHandler.UsersDataHandler(WebApiApplication.SessionFactory);
        CASPaymentDAO.DataHandler.AccountDataHandler accountHandler = new CASPaymentDAO.DataHandler.AccountDataHandler(WebApiApplication.SessionFactory);
        CASPaymentDAO.DataHandler.ApplicationDataHandler applicationHandler = new CASPaymentDAO.DataHandler.ApplicationDataHandler(WebApiApplication.SessionFactory);
        CASPaymentDAO.DataHandler.CurrencyTypeDataHandler currencyHandler = new CASPaymentDAO.DataHandler.CurrencyTypeDataHandler(WebApiApplication.SessionFactory);
        CASPaymentDAO.DataHandler.TransactionTypeDataHandler transactionTypeHandler = new CASPaymentDAO.DataHandler.TransactionTypeDataHandler(WebApiApplication.SessionFactory);
        #endregion

        #region Main Actions
        //
        // GET: /Sales/
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult SalesLaunch(string encContent, string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            //TODO unify the authentication methods with the other server
            var authentication = HttpContext.GetOwinContext().Authentication;
            var ticket = authentication.AuthenticateAsync("Application").Result;
            var identity = ticket != null ? ticket.Identity : null;
            if (identity == null)
            {
                //return Redirect("http://"+Request.Url.Authority + "/Account/Login");
                authentication.Challenge("Application");
                return new HttpUnauthorizedResult();
            }
            string payerId = identity.Claims.Where(c => c.Type == ClaimTypes.NameIdentifier).Select(c => c.Value).FirstOrDefault();
            long payerIdLong;
            if (!string.IsNullOrEmpty(encContent) && Int64.TryParse(payerId, out payerIdLong))
            {
                string paymentModelJSON =this.DecryptRequest(encContent);
                Models.POSTModels.PaymentPOSTModel paymodel = (Models.POSTModels.PaymentPOSTModel)Newtonsoft.Json.JsonConvert.DeserializeObject(paymentModelJSON, typeof(Models.POSTModels.PaymentPOSTModel), new Newtonsoft.Json.JsonSerializerSettings { NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore });
                string error;
                if (CheckValidityOFPOSTModel(paymodel, out error))
                {
                    CASPaymentDTO.Domain.Users payeeUserItem = accountHandler.Search(new CASPaymentDTO.Domain.Account { Accountnumber = paymodel.PayeeAccountNumber }).Cast<CASPaymentDTO.Domain.Account>().FirstOrDefault().UsersItem;
                    string payeeName = (payeeUserItem.UserType == "RP") ? payeeUserItem.RealPersonItem.Firstname + " " + payeeUserItem.RealPersonItem.Lastname : payeeUserItem.LegalPersonItem.Companyname;
                    //ViewData.Add("PaymentPOSTModel", paymodel);
                    ViewData.Add("accountHandler", accountHandler);
                    var model = new Models.WebPageModels.SalesPageModel
                    {
                        UserAccount = GetAccounts(payerIdLong),
                    };
                    ViewBag.PaymentPOSTModel = paymodel;
                    return View(model);
                }
            }
            //CASPaymentDTO.Domain. Request["TransactionsPayeeJSON"]
            //CASPaymentDTO.Domain.Transactions transactionPayee = (CASPaymentDTO.Domain.Transactions)Newtonsoft.Json.JsonConvert.DeserializeObject(TransactionsPayeeJSON, typeof(CASPaymentDTO.Domain.Transactions), new Newtonsoft.Json.JsonSerializerSettings { NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore });
            //ViewBag.payerAccountNumbers = new SelectList()
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> SalesLaunch(Models.WebPageModels.SalesPageModel model, string returnUrl, string PaymentPOSTModelJSON)
        {
            Models.POSTModels.PaymentPOSTModel PaymentPOSTModel = (Models.POSTModels.PaymentPOSTModel)Newtonsoft.Json.JsonConvert.DeserializeObject(PaymentPOSTModelJSON, typeof(Models.POSTModels.PaymentPOSTModel));
            if (ModelState.IsValid)
            {
                PaymentsController paymentController = new PaymentsController();
                CASPaymentDTO.Domain.Account PayerAccount = accountHandler.GetEntity(model.SelectedUserAccountId);
                Models.PaymentResultModel resultofSalesPayment;
                if (!await paymentController.CheckAccountAndPINCode(PayerAccount, model.PINCode))
                {
                    //PIN is incorrect
                    resultofSalesPayment = new Models.PaymentResultModel
                    {
                        Error = "Error: Incorrect Pin",
                        ResultOfPayment = Constants.PaymentStatusTypes.Canceled
                    };
                }
                else if (!await paymentController.CheckBalance(PayerAccount, (Decimal)PaymentPOSTModel.Amount))
                {
                    //Not Enough Money in Bank Account
                    resultofSalesPayment = new Models.PaymentResultModel
                    {
                        Error = "Error: Not Enough Money",
                        ResultOfPayment = Constants.PaymentStatusTypes.Canceled
                    };
                }
                else if (!await paymentController.CheckUpperLimit(PayerAccount, (Decimal)PaymentPOSTModel.Amount, Constants.TransactionTypes.Purchase))
                {
                    //Limit for purchase today is reached
                    resultofSalesPayment = new Models.PaymentResultModel
                    {
                        Error = "Error: Upper Limit",
                        ResultOfPayment = Constants.PaymentStatusTypes.Canceled
                    };
                }
                else
                {
                    //Do payment job
                    CASPaymentDTO.Domain.Transactions payerTransaction = AsyncContext.Run(() => this.CreatePayerTransaction(PaymentPOSTModel, PayerAccount));
                    string paymentStatus = null, paymentErrors = null, paymentTrackingNumber = null;
                    AsyncContext.Run(() => paymentController.PerformPayment(payerTransaction, out paymentStatus, out paymentErrors, out paymentTrackingNumber));
                    resultofSalesPayment = new Models.PaymentResultModel
                    {
                        ResultOfPayment = paymentStatus,
                        TrackingNumber = paymentTrackingNumber,
                        Error = paymentErrors,
                        TransactionType = Constants.TransactionTypes.Purchase
                    };
                }
                string ResultOfPaymentJson = Newtonsoft.Json.JsonConvert.SerializeObject(resultofSalesPayment, new Newtonsoft.Json.JsonSerializerSettings { NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore });
                string encContet = this.EncryptResponse(ResultOfPaymentJson);
                string completeQueryStringForRedirect = Constants.QueryStringFuncs.GenerateRequestStringAsync(returnUrl, new string[] { "ResultOfPayment" }, new string[] { encContet });
                return Redirect(completeQueryStringForRedirect);
            }

            // If we got this far, something failed, redisplay form
            return Redirect(returnUrl);
        }

        #endregion

        #region AUX Methods

        private bool CheckValidityOFPOSTModel(Models.POSTModels.PaymentPOSTModel paymentModel, out string ErrorMessage)
        {
            if (paymentModel.PayeeAccountNumber == null || paymentModel.TransactionType == null || paymentModel.Amount == null)
            {
                ErrorMessage = "Incomplete Payment Data.";
                return false;
            }
            if (paymentModel.TransactionType == Constants.TransactionTypes.Transfer && string.IsNullOrEmpty(paymentModel.PayerAccountNumber))
            {
                ErrorMessage = "PayerAccountNumber must be assigned in transfer requests";
                return false;
            }
            if (paymentModel.TransactionType == Constants.TransactionTypes.Purchase && (string.IsNullOrEmpty(paymentModel.OrderNumber) || string.IsNullOrEmpty(paymentModel.RedirectUrl)))
            {
                ErrorMessage = string.IsNullOrEmpty(paymentModel.OrderNumber) ? "OrderNumber must be assigned in purchase requests" : "Redirect Url must be assigned in purchase requests";
                return false;
            }

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
        
        private async Task<CASPaymentDTO.Domain.Transactions> CreatePayerTransaction(Models.POSTModels.PaymentPOSTModel paymodel, CASPaymentDTO.Domain.Account PayerAccount)
        {
            return await Task.Run(() =>
            {
                CASPaymentDTO.Domain.Transactions payerTransaction = new CASPaymentDTO.Domain.Transactions();
                CASPaymentDTO.Domain.Account PayeeAccount = accountHandler.Search(new CASPaymentDTO.Domain.Account { Accountnumber = paymodel.PayeeAccountNumber }).Cast<CASPaymentDTO.Domain.Account>().FirstOrDefault();
                CASPaymentDTO.Domain.CurrencyType currencyItem = currencyHandler.Search(new CASPaymentDTO.Domain.CurrencyType { Charcode = paymodel.Currency }).Cast<CASPaymentDTO.Domain.CurrencyType>().FirstOrDefault();
                CASPaymentDTO.Domain.TransactionType transactionTypeItem = transactionTypeHandler.Search(new CASPaymentDTO.Domain.TransactionType { NameEn = paymodel.TransactionType }).Cast<CASPaymentDTO.Domain.TransactionType>().FirstOrDefault();
                payerTransaction.Amount = (-1) * Math.Abs((Decimal)paymodel.Amount);
                payerTransaction.CurrencyTypeItem = currencyItem;
                payerTransaction.TransactionTypeItem = transactionTypeItem;
                payerTransaction.DestinationAccountItem = PayerAccount;
                payerTransaction.SourceAccountItem = PayeeAccount;
                payerTransaction.Description = paymodel.Description;
                return payerTransaction;
            }).ConfigureAwait(false);
        }

        private IEnumerable<SelectListItem> GetAccounts(long userID)
        {
            var accounts = userHandler.GetEntity(userID).AccountS.ToList().Select(x =>
                new SelectListItem
                {
                    Value = x.Id.ToString(),
                    Text = x.Accountnumber
                });
            return new SelectList(accounts, "Value", "Text");
        }

        private ActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            else
            {
                return RedirectToAction("Index", "Home");
            }
        }

        #endregion

        #region Encrypt-Decrypt-QueryString
        private string DecryptRequest(string EncrptedReq)
        {
            Models.SalesModels.EncryptedJSONModel cipherMessageBody = (Models.SalesModels.EncryptedJSONModel)Newtonsoft.Json.JsonConvert.DeserializeObject(EncrptedReq, typeof(Models.SalesModels.EncryptedJSONModel), new Newtonsoft.Json.JsonSerializerSettings { NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore });
            CASPaymentDTO.Domain.Application applicationClient = applicationHandler.Search(new CASPaymentDTO.Domain.Application { ClientID = cipherMessageBody.ClientID }).Cast<CASPaymentDTO.Domain.Application>().FirstOrDefault();
            if (object.Equals(applicationClient, default(CASPaymentDTO.Domain.Application))) return null;
            ViewBag.ClientID = applicationClient.ClientID;
            string decryptedJSONstring = StringCipher.Decrypt(cipherMessageBody.CipherText, applicationClient.Secrethash);
            return decryptedJSONstring;
        }
        private string EncryptResponse(string plainText)
        {
            //Newtonsoft.Json.Linq.JObject jobject = (Newtonsoft.Json.Linq.JObject)await res.Content.ReadAsAsync<Newtonsoft.Json.Linq.JObject>().ConfigureAwait(false);
            //string responseJSONPlainText =await res.Content.ReadAsStringAsync().ConfigureAwait(false);
            CASPaymentDTO.Domain.Application applicationClient = applicationHandler.Search(new CASPaymentDTO.Domain.Application { ClientID = ViewBag.ClientID }).Cast<CASPaymentDTO.Domain.Application>().FirstOrDefault();
            if (object.Equals(applicationClient, default(CASPaymentDTO.Domain.Application))) return null;
            Models.SalesModels.EncryptedJSONModel cipherMessageBody = new Models.SalesModels.EncryptedJSONModel
                {
                    CipherText = StringCipher.Encrypt(plainText, applicationClient.Secrethash),
                    ClientID = applicationClient.ClientID
                };
            return Newtonsoft.Json.JsonConvert.SerializeObject(cipherMessageBody, new Newtonsoft.Json.JsonSerializerSettings { NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore });
        }

        #endregion
    }
}