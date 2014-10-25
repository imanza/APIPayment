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
        CASPaymentDAO.DataHandler.CurrencyTypeDataHandler currencyHandler = new CASPaymentDAO.DataHandler.CurrencyTypeDataHandler(WebApiApplication.SessionFactory);
        CASPaymentDAO.DataHandler.TransactionTypeDataHandler transactionTypeHanlder = new CASPaymentDAO.DataHandler.TransactionTypeDataHandler(WebApiApplication.SessionFactory);
        #endregion 


        //
        // GET: /Sales/
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult SalesLaunch(string paymentModelJSON , string returnUrl)
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
            if (Int64.TryParse(payerId, out payerIdLong))
            {
                Models.POSTModels.PaymentPOSTModel paymodel = (Models.POSTModels.PaymentPOSTModel)Newtonsoft.Json.JsonConvert.DeserializeObject(paymentModelJSON, typeof(Models.POSTModels.PaymentPOSTModel), new Newtonsoft.Json.JsonSerializerSettings { NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore });
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
            //CASPaymentDTO.Domain. Request["TransactionsPayeeJSON"]
            //CASPaymentDTO.Domain.Transactions transactionPayee = (CASPaymentDTO.Domain.Transactions)Newtonsoft.Json.JsonConvert.DeserializeObject(TransactionsPayeeJSON, typeof(CASPaymentDTO.Domain.Transactions), new Newtonsoft.Json.JsonSerializerSettings { NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore });
            //ViewBag.payerAccountNumbers = new SelectList()
            return View();
        }

        [HttpPost]
        public async Task<ActionResult> SalesLaunch(Models.WebPageModels.SalesPageModel model, string returnUrl , string PaymentPOSTModelJSON)
        {
            Models.POSTModels.PaymentPOSTModel PaymentPOSTModel = (Models.POSTModels.PaymentPOSTModel)Newtonsoft.Json.JsonConvert.DeserializeObject(PaymentPOSTModelJSON, typeof(Models.POSTModels.PaymentPOSTModel));
            if (ModelState.IsValid)
            {
                PaymentsController paymentController = new PaymentsController();
                CASPaymentDTO.Domain.Account PayerAccount = accountHandler.GetEntity(model.SelectedUserAccountId);
                string paymentResult = "";
                if (!await paymentController.CheckAccountAndPINCode(PayerAccount, model.PINCode))
                {
                    //PIN is incorrect
                    paymentResult = "Incorrect Pin";
                }
                else if (!await paymentController.CheckBalance(PayerAccount, (Decimal)PaymentPOSTModel.Amount))
                {
                    //Not Enough Money in Bank Account
                    paymentResult = "Error: Not Enough Money";
                }
                else if (!await paymentController.CheckUpperLimit(PayerAccount, (Decimal)PaymentPOSTModel.Amount, Constants.TransactionTypes.Purchase))
                {
                    //Limit for purchase today is reached
                    paymentResult = "Error: Upper Limit";
                }
                else
                {
                    //Do payment job
                    CASPaymentDTO.Domain.Transactions payerTransaction = CreatePayerTransaction(PaymentPOSTModel, PayerAccount);
                    paymentResult = paymentController.PerformPayment(payerTransaction);
                }
                string completeQueryStringForRedirect = Constants.QueryStringFuncs.GenerateRequestStringAsync(returnUrl, new string[] { "ResultOfPayment" }, new string[] { paymentResult });
                return Redirect(completeQueryStringForRedirect);                
            }

            // If we got this far, something failed, redisplay form
            return Redirect(returnUrl);
        }

        private CASPaymentDTO.Domain.Transactions CreatePayerTransaction(Models.POSTModels.PaymentPOSTModel paymodel, CASPaymentDTO.Domain.Account PayerAccount)
        {
            CASPaymentDTO.Domain.Transactions payerTransaction = new CASPaymentDTO.Domain.Transactions();
            CASPaymentDTO.Domain.Account PayeeAccount = accountHandler.Search(new CASPaymentDTO.Domain.Account { Accountnumber = paymodel.PayeeAccountNumber }).Cast<CASPaymentDTO.Domain.Account>().FirstOrDefault();
            CASPaymentDTO.Domain.CurrencyType currencyItem = currencyHandler.Search(new CASPaymentDTO.Domain.CurrencyType { Charcode = paymodel.Currency }).Cast<CASPaymentDTO.Domain.CurrencyType>().FirstOrDefault();
            CASPaymentDTO.Domain.TransactionType transactionTypeItem = transactionTypeHanlder.Search(new CASPaymentDTO.Domain.TransactionType { NameEn = Constants.TransactionTypes.Purchase }).Cast<CASPaymentDTO.Domain.TransactionType>().FirstOrDefault();
            payerTransaction.Amount = (-1) * Math.Abs((Decimal) paymodel.Amount);
            payerTransaction.CurrencyTypeItem = currencyItem;
            payerTransaction.TransactionTypeItem = transactionTypeItem;
            payerTransaction.DestinationAccountItem = PayerAccount;
            payerTransaction.SourceAccountItem = PayeeAccount;
            return payerTransaction;
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
    }
}