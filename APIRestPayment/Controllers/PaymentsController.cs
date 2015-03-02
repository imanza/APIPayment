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
using System.Security.Claims;
using System.Web;
using Nito.AsyncEx;
//using System.Web.Http.Cors;



namespace APIRestPayment.Controllers
{
    [Authorize]
    [RoutePrefix("api/payments")]
    public class PaymentsController : BaseApiController
    {
        HttpContext mainContext;

        public PaymentsController(HttpContext context)
        {
            mainContext = context;
        }
        public PaymentsController()
        {

        }

        #region Handlers

        CASPaymentDAO.DataHandler.UsersDataHandler usersHandler = new CASPaymentDAO.DataHandler.UsersDataHandler(WebApiApplication.SessionFactory);
        CASPaymentDAO.DataHandler.TransactionsDataHandler transactionHandler = new CASPaymentDAO.DataHandler.TransactionsDataHandler(WebApiApplication.SessionFactory);
        CASPaymentDAO.DataHandler.AccountDataHandler accountHandler = new CASPaymentDAO.DataHandler.AccountDataHandler(WebApiApplication.SessionFactory);
        CASPaymentDAO.DataHandler.SettingDataDataHandler settingDataHandler = new CASPaymentDAO.DataHandler.SettingDataDataHandler(WebApiApplication.SessionFactory);
        CASPaymentDAO.DataHandler.AccountDailyActivityDataHandler accountDailyActivityHandler = new CASPaymentDAO.DataHandler.AccountDailyActivityDataHandler(WebApiApplication.SessionFactory);
        CASPaymentDAO.DataHandler.PaymentStatusDataHandler paymentStatusHandler = new CASPaymentDAO.DataHandler.PaymentStatusDataHandler(WebApiApplication.SessionFactory);
        CASPaymentDAO.DataHandler.CurrencyTypeDataHandler currencyHandler = new CASPaymentDAO.DataHandler.CurrencyTypeDataHandler(WebApiApplication.SessionFactory);
        CASPaymentDAO.DataHandler.TransactionTypeDataHandler transactionTypeHandler = new CASPaymentDAO.DataHandler.TransactionTypeDataHandler(WebApiApplication.SessionFactory);

        #endregion

        #region Access

        public override DataAccessTypes CurrentUserAccessType
        {
            get
            {
                //My own logic to check whether user has rights to access the transaction
                if (base.CurrentUserAccessType == DataAccessTypes.Administrator) return DataAccessTypes.Administrator;
                else
                {
                    var routeData = Request.GetRouteData();
                    var resourceID = routeData.Values["id"] as string;
                    //

                    var identity = User.Identity as ClaimsIdentity;
                    if (identity != null)
                    {
                        var ss = identity.Claims.Where(x => x.Type == ClaimTypes.NameIdentifier).FirstOrDefault().Value;
                        long currentUserId;
                        if (long.TryParse(ss, out currentUserId))
                        {
                            CASPaymentDTO.Domain.Users currentUser = usersHandler.GetEntity(currentUserId);
                            foreach (var item in currentUser.AccountS)
                            {
                                if (item.DestinationTransactionsS.Where(x => x.Id.ToString() == resourceID).Count() > 0)
                                {
                                    return DataAccessTypes.Owner;
                                }
                            }
                        }
                    }
                }
                /////////////////////////////
                return DataAccessTypes.Anonymous;
            }
        }

        #endregion

        #region GET



        [Route("{id:long}",Name="GetPayment")]
        public HttpResponseMessage GetPayment(long id)
        {
            var identity = User.Identity as ClaimsIdentity;
            if (identity == null)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, new Models.QueryResponseModel
                {
                    meta = new Models.MetaModel
                    {
                        code = (int)HttpStatusCode.BadRequest,
                        errorMessage = "The Identity of the user is not known"
                    }
                });
            }
            else
            {
                var scopesGranted = identity.Claims.Where(c => c.Type == ClaimNames.OAuthScope).Select(c => c.Value);
                if (scopesGranted.Contains(ScopeTypes.Report) || scopesGranted.Contains(ScopeTypes.AllAccess))
                {
                    try
                    {
                        if (CurrentUserAccessType != DataAccessTypes.Anonymous)
                        {
                            CASPaymentDTO.Domain.Transactions searchedTransaction = this.transactionHandler.GetEntity(id);
                            if (searchedTransaction != null)
                            {
                                return Request.CreateResponse(HttpStatusCode.OK, new Models.QueryResponseModel
                                {
                                    meta = new Models.MetaModel
                                    {
                                        code = (int)HttpStatusCode.OK
                                    },
                                    data = TheModelFactory.Create(searchedTransaction)
                                });
                            }
                            else
                            {
                                return Request.CreateResponse(HttpStatusCode.NotFound, new Models.QueryResponseModel
                                {
                                    meta = new Models.MetaModel
                                    {
                                        code = (int)HttpStatusCode.NotFound,
                                        errorMessage = "Payment was not found"
                                    }
                                });
                            }
                        }
                        else
                        {
                            return Request.CreateResponse(HttpStatusCode.Unauthorized, new Models.QueryResponseModel
                            {
                                meta = new Models.MetaModel
                                {
                                    code = (int)HttpStatusCode.Unauthorized,
                                    errorMessage = "Cannot access requested resource"
                                }
                            });
                        }
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
                else
                {
                    //Application Does Not Have permission to view payments
                    return Request.CreateResponse(HttpStatusCode.Unauthorized, new Models.QueryResponseModel
                    {
                        meta = new Models.MetaModel
                        {
                            code = (int)HttpStatusCode.Unauthorized,
                            errorMessage = "Not enough permisions granted to view payments!"
                        },
                    });
                }
            }
        }
        
        private long GetIdofCurrentUser(ClaimsIdentity identity)
        {
            if (identity != null)
            {
                var currentuserIdstring = identity.Claims.Where(c => c.Type == ClaimTypes.NameIdentifier).Select(c => c.Value).FirstOrDefault();
                long currentUserId;
                if (long.TryParse(currentuserIdstring, out currentUserId))
                {
                    return currentUserId;
                }
            }
            return -1;
        }
        
        [Route(Name="GetAllPayments")]
        public HttpResponseMessage Get(int page = 0, int pageSize = 10, string trackingNumber = "", string startDate = "", string endDate = "")
        {
            IList<CASPaymentDTO.Domain.Transactions> result = new List<CASPaymentDTO.Domain.Transactions>();
            var identity = User.Identity as ClaimsIdentity;
            if (identity == null)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, new Models.QueryResponseModel
                {
                    meta = new Models.MetaModel
                    {
                        code = (int)HttpStatusCode.BadRequest,
                        errorMessage = "The Identity of the user is not known"
                    }
                });
            }
            else
            {

                var scopesGranted = identity.Claims.Where(c => c.Type == ClaimNames.OAuthScope).Select(c => c.Value);
                if (scopesGranted.Contains(ScopeTypes.Report) || scopesGranted.Contains(ScopeTypes.AllAccess))
                {

                    if (base.CurrentUserAccessType != DataAccessTypes.Administrator)
                    {
                        bool isTrackingNumberParamNull = string.IsNullOrEmpty(trackingNumber);
                        bool isStartDateParamNull = string.IsNullOrEmpty(startDate);
                        bool isEndDateParamNull = string.IsNullOrEmpty(endDate);

                        long currentUserId = GetIdofCurrentUser(identity);
                        if (currentUserId != -1)
                        {
                            foreach (var item in usersHandler.GetEntity(currentUserId).AccountS)
                            {

                                result = result.Concat(item.DestinationTransactionsS.Where(x => x.TransactionTypeItem.NameEn != TransactionTypes.Deposit)).ToList();

                                //&&
                                //    ( ( string.IsNullOrEmpty(trackingNumber)  (!string.IsNullOrEmpty(trackingNumber) && x.Trackingnumber == trackingNumber))
                                //     (!string.IsNullOrEmpty(startDate) && x.Executiondatetime >= DateFuncs.ToGregorianDate(DateFuncs.ConvertStringToDate(startDate))) 

                                //    )

                            }
                            if (!(isTrackingNumberParamNull && isStartDateParamNull && isEndDateParamNull))
                            {
                                result = result.Where(x =>
                                    (isTrackingNumberParamNull || (!isTrackingNumberParamNull && x.Trackingnumber == trackingNumber)) &&
                                    (isStartDateParamNull || (!isStartDateParamNull && x.Executiondatetime >= DateFuncs.ToGregorianDate(DateFuncs.ConvertStringToDate(startDate)))) &&
                                    (isEndDateParamNull || (!isEndDateParamNull && x.Executiondatetime <= DateFuncs.ToGregorianDate(DateFuncs.ConvertStringToDate(endDate))))
                                    ).ToList();
                            }
                            result = result.OrderBy(x => x.Executiondatetime).OrderBy(x => x.Id).ToList();
                        }
                        else
                        {
                            return Request.CreateResponse(HttpStatusCode.BadRequest, new Models.QueryResponseModel
                            {
                                meta = new Models.MetaModel
                                {
                                    code = (int)HttpStatusCode.BadRequest,
                                    errorMessage = "Bad Identity set for user"
                                }
                            });
                        }
                    }

                    else
                    {
                        result = this.transactionHandler.SelectAll().Cast<CASPaymentDTO.Domain.Transactions>().ToList();
                    }
                }
                else
                {
                    //Application is not granted to access resource
                    return Request.CreateResponse(HttpStatusCode.Unauthorized, new Models.QueryResponseModel
                        {
                            meta = new Models.MetaModel
                            {
                                code = (int)HttpStatusCode.Unauthorized,
                                errorMessage = "Not enough permissions for the application"
                            }
                        });
                }
            }

            //////////////////////////////////////////////////
            var totalCount = result.Count();
            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
            var urlHelper = new UrlHelper(Request);
            var prevLink = page > 0 ? urlHelper.Link("GetAllPayments", new { page = page - 1 }) : null;
            var nextLink = page < totalPages - 1 ? urlHelper.Link("GetAllPayments", new { page = page + 1 }) : null;
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

        [AllowAnonymous]
        public HttpResponseMessage Get([FromUri]string PaymentPOSTModelJson)
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
                    string[] Requestparams = { "paymentModelJSON", "returnUrl" };
                    string[] Requestvalues = { PaymentPOSTModelJson, paymodel.RedirectUrl };
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
        }


        #endregion

        #region POST


        [Route("create" , Name="CreatePayment")]
        [HttpPost]
        public async Task<HttpResponseMessage> Post([FromBody] Models.POSTModels.PaymentPOSTModel PaymentPOSTModelJson)
        {
            mainContext = HttpContext.Current;
            return await Task.Run(async () =>
                {
                    var identity = User.Identity as ClaimsIdentity;
                    var scopesGranted = identity.Claims.Where(c => c.Type == ClaimNames.OAuthScope).Select(c => c.Value);
                    if (scopesGranted.Contains(ScopeTypes.Report) || scopesGranted.Contains(ScopeTypes.AllAccess))
                    {

                        string ParseErrorMessage;
                        //Newtonsoft.Json.JsonSerializerSettings jj = new Newtonsoft.Json.JsonSerializerSettings { NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore };
                        //APIRestPayment.Models.POSTModels.PaymentPOSTModel paymodel = (APIRestPayment.Models.POSTModels.PaymentPOSTModel)Newtonsoft.Json.JsonConvert.DeserializeObject(PaymentPOSTModelJson, typeof(APIRestPayment.Models.POSTModels.PaymentPOSTModel), jj);

                        APIRestPayment.Models.POSTModels.PaymentPOSTModel paymodel = PaymentPOSTModelJson;
                        if (this.CheckValidityOFPOSTModel(paymodel, out ParseErrorMessage))
                        {
                            if (paymodel.TransactionType == TransactionTypes.Transfer)
                            {
                                return await HandleTransferRequest(paymodel, scopesGranted);
                            }
                            else
                            {
                                return Request.CreateResponse(HttpStatusCode.NotImplemented, new Models.QueryResponseModel
                                {
                                    meta = new Models.MetaModel
                                    {
                                        code = (int)HttpStatusCode.NotImplemented,
                                        errorMessage = "Other transactions are not supported",
                                    }
                                });
                            }
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
                    }
                    else
                    {
                        return Request.CreateResponse(HttpStatusCode.Unauthorized, new Models.QueryResponseModel
                        {
                            meta = new Models.MetaModel
                            {
                                code = (int)HttpStatusCode.Unauthorized,
                                errorMessage = "Application does not have permission to make payments",
                            }
                        });
                    }
                }).ConfigureAwait(false);
        }

        private async Task<HttpResponseMessage> HandleTransferRequest(Models.POSTModels.PaymentPOSTModel paymodel, IEnumerable<string> scopesGranted)
        {
            return await Task.Run(async () =>
             {
                 string HashPaymentCode = "";
                 if (CheckContainsPaymentCode(paymodel, out HashPaymentCode))
                 {
                     CASPaymentDTO.Domain.Account PayerAccount = accountHandler.Search(new CASPaymentDTO.Domain.Account { Accountnumber = paymodel.PayerAccountNumber }).Cast<CASPaymentDTO.Domain.Account>().FirstOrDefault();
                     string paymentErrorResult = "";
                     if (!AsyncContext.Run(() => this.CheckAccountAndPINCode(PayerAccount, paymodel.PaymentPIN)))
                     {
                         //PIN is incorrect
                         paymentErrorResult = "Error: Incorrect Pin";
                     }
                     else if (!AsyncContext.Run(() => this.CheckBalance(PayerAccount, (Decimal)paymodel.Amount)))
                     {
                         //Not Enough Money in Bank Account
                         paymentErrorResult = "Error: Not Enough Money";
                     }
                     else if (!AsyncContext.Run(() => this.CheckUpperLimit(PayerAccount, (Decimal)paymodel.Amount, Constants.TransactionTypes.Transfer)))
                     {
                         //Limit for purchase today is reached
                         paymentErrorResult = "Error: Upper Limit";
                     }
                     else
                     {
                         //Do payment job
                         CASPaymentDTO.Domain.Transactions payerTransaction = await this.CreatePayerTransaction(paymodel, PayerAccount);
                         string paymentStatus, trackingNumber, errorPayment;
                         this.PerformPayment(payerTransaction, out paymentStatus, out errorPayment, out trackingNumber);
                         Models.PaymentResultModel resultofTransferPayment = TheModelFactory.Create(paymentStatus, errorPayment, trackingNumber, paymodel.OrderNumber);
                         return Request.CreateResponse((resultofTransferPayment.ResultOfPayment == PaymentStatusTypes.Completed) ? HttpStatusCode.OK : HttpStatusCode.InternalServerError, new Models.QueryResponseModel
                         {
                             meta = new Models.MetaModel
                             {
                                 code = (int)((resultofTransferPayment.ResultOfPayment == PaymentStatusTypes.Completed) ? HttpStatusCode.OK : HttpStatusCode.InternalServerError)
                             },
                             data = resultofTransferPayment
                         });
                     }
                     HttpContext.Current = mainContext;
                     return Request.CreateResponse(HttpStatusCode.BadRequest, new Models.QueryResponseModel
                     {
                         meta = new Models.MetaModel
                         {
                             code = (int)HttpStatusCode.BadRequest
                         },
                         data = TheModelFactory.Create(PaymentStatusTypes.Canceled, paymentErrorResult, null, paymodel.OrderNumber)
                     });

                 }
                 else
                 {
                     if (scopesGranted.Contains(ScopeTypes.AllAccess))
                     {
                         paymodel.HashPaymentCode = HashPaymentCode;
                         HttpContext.Current = mainContext;
                         Models.POSTModels.TransferPaymentResultModel transferResultModel = TheModelFactory.Create(paymodel);
                         return Request.CreateResponse(HttpStatusCode.OK, new Models.QueryResponseModel
                         {
                             meta = new Models.MetaModel
                             {
                                 code = (int)HttpStatusCode.OK
                             },
                             data = transferResultModel
                         });
                     }
                     else
                     {
                         HttpContext.Current = mainContext;
                         return Request.CreateResponse(HttpStatusCode.BadRequest, new Models.QueryResponseModel
                         {
                             meta = new Models.MetaModel
                             {
                                 code = (int)HttpStatusCode.BadRequest,
                                 errorMessage = "Please provide body of your request with hash of payment code.",
                             }
                         });
                     }
                 }
             }
         ).ConfigureAwait(false);
        }

        private bool CheckContainsPaymentCode(Models.POSTModels.PaymentPOSTModel paymodel, out string HashPaymentCode)
        {
            CASPaymentDTO.Domain.Account PayeeAccount = accountHandler.Search(new CASPaymentDTO.Domain.Account { Accountnumber = paymodel.PayeeAccountNumber }).Cast<CASPaymentDTO.Domain.Account>().FirstOrDefault();
            if (!string.IsNullOrEmpty(paymodel.HashPaymentCode))
            {
                if (paymodel.HashPaymentCode == (PayeeAccount.Paymentcode).GetHashCode().ToString())
                {
                    HashPaymentCode = null;
                    return true;
                }
            }
            HashPaymentCode = (PayeeAccount.Paymentcode).GetHashCode().ToString();
            return false;
        }

        #endregion

        #region Checks

        
        [HttpPost]
        [ActionName("CheckAccountAndPIN")]
        [Route("check")]
        public async Task<HttpResponseMessage> PostCheck([FromBody]Models.POSTModels.OAuthServerCheckUserAccountPOSTModel OAuthServerCheckUserAccountPOSTModelJSON)
        {
            if (CurrentUserRoleType == RoleTypes.Administrator)
            {
                var identity = User.Identity as ClaimsIdentity;
                var scopesGranted = identity.Claims.Where(c => c.Type == ClaimNames.OAuthScope).Select(c => c.Value);
                if (scopesGranted.Contains(ScopeTypes.AllAccess))
                {
                    string ErrorMessage;
                    if (!CheckValidityOFPOSTModel(OAuthServerCheckUserAccountPOSTModelJSON, out ErrorMessage))
                    {
                        return Request.CreateResponse(HttpStatusCode.BadRequest, new Models.QueryResponseModel
                        {
                            meta = new Models.MetaModel
                            {
                                code = (int)HttpStatusCode.BadRequest,
                                errorMessage = ErrorMessage
                            },
                        });
                    }
                    CASPaymentDTO.Domain.Account PayerAccNo = accountHandler.Search(new CASPaymentDTO.Domain.Account { Accountnumber = OAuthServerCheckUserAccountPOSTModelJSON.PayerAccountNumber }).Cast<CASPaymentDTO.Domain.Account>().FirstOrDefault();
                    return Request.CreateResponse(HttpStatusCode.OK, new Models.QueryResponseModel
                    {
                        data = (await this.CheckAccountAndPINCode(PayerAccNo, OAuthServerCheckUserAccountPOSTModelJSON.PINCode)) ? true.ToString() : false.ToString(),
                    });
                }
            }
            return Request.CreateResponse(HttpStatusCode.Unauthorized, new Models.QueryResponseModel
            {
                meta = new Models.MetaModel
                {
                    code = (int)HttpStatusCode.Unauthorized,
                    errorMessage = "Access is denied."
                },
            });
        }
        private bool CheckValidityOFPOSTModel(Models.POSTModels.OAuthServerCheckUserAccountPOSTModel OAuthServerCheckUserAccountPOSTModel, out string ErrorMessage)
        {
            if (OAuthServerCheckUserAccountPOSTModel.PINCode == null || OAuthServerCheckUserAccountPOSTModel.PayerAccountNumber == null)
            {
                ErrorMessage = "Incomplete Data.";
                return false;
            }
            CASPaymentDTO.Domain.Account PayerAccNo = accountHandler.Search(new CASPaymentDTO.Domain.Account { Accountnumber = OAuthServerCheckUserAccountPOSTModel.PayerAccountNumber }).Cast<CASPaymentDTO.Domain.Account>().FirstOrDefault();
            if (object.Equals(PayerAccNo, default(CASPaymentDTO.Domain.Account)))
            {
                ErrorMessage = "Account Number is not valid";
                return false;
            }
            ErrorMessage = "";
            return true;
        }
        private bool CheckValidityOFPOSTModel(Models.POSTModels.PaymentPOSTModel paymentModel, out string ErrorMessage)
        {
            if (paymentModel.PayeeAccountNumber == null || paymentModel.TransactionType == null || paymentModel.Amount == null )
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
        // Not an action method.
        [NonAction]
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
            return (res == Microsoft.AspNet.Identity.PasswordVerificationResult.Success);
        }

        /// <summary>
        /// Compares the balance of the "PayerAccount" with "Amount" and returns a bool indicating whether amount could be taken from this account.
        /// </summary>
        /// <param name="PayerAccount">The Account Item of Payer</param>
        /// <param name="Amount">
        /// <typeparamref name="Decimal"/> Amount of payment</param>
        /// <returns>bool</returns>
        // Not an action method.
        [NonAction]
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
        // Not an action method.
        [NonAction]
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

        // Not an action method.
        [NonAction]
        public void PerformPayment(CASPaymentDTO.Domain.Transactions payerTransaction, out string paymentStatus, out string ErrorPayment, out string trackNumber)
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
            CASPaymentDTO.Domain.AccountDailyActivity accountDailyActivityForPayer = GetPayerTodayActivity(payerAccount, payerTransaction.TransactionTypeItem.NameEn, (Decimal)payerTransaction.Amount, out hasDailyActivity);


            CASPaymentDTO.Domain.PaymentStatus completePaymentStatus = paymentStatusHandler.Search(new CASPaymentDTO.Domain.PaymentStatus { NameEn = PaymentStatusTypes.Completed }).Cast<CASPaymentDTO.Domain.PaymentStatus>().FirstOrDefault();

            //Get TransactionFees
            //we use payee amount because it is absoloute and positive .... No other special reason !!!
            if (payerTransaction.TransactionTypeItem.NameEn != TransactionTypes.Fees)
            {
                float feesRate = (payeeTransaction.TransactionTypeItem.Comissionrate.HasValue && payeeTransaction.TransactionTypeItem.Comissionrate.Value > 0) ? payeeTransaction.TransactionTypeItem.Comissionrate.Value : default(float);
                if (!feesRate.Equals(default(float)))
                {
                    CASPaymentDTO.Domain.Transactions transactionfeesPayer = GetTransactionFees(payerTransaction, feesRate);
                    string transactionfeesStatus,transactionfeesError,transactionfeesTrackingNumber;
                    this.PerformPayment(transactionfeesPayer, out transactionfeesStatus, out transactionfeesError,out transactionfeesTrackingNumber);
                    if (transactionfeesStatus == PaymentStatusTypes.Canceled)
                    {
                        paymentStatus = PaymentStatusTypes.Canceled;
                        ErrorPayment = "Could not get transaction fees";
                        trackNumber = null;
                        return;
                    }
                }
            }
            //bool Completed = false;
            bool isTransactionComplete = false;
            string TransactionError = "";
            using (var session = WebApiApplication.SessionFactory.OpenSession())
            {
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
                        // return ChangeModelFactorySession(currentThreadContextSession.SessionFactory, Request).Create(PaymentStatusTypes.Completed, null, trackingNumber);
                        isTransactionComplete = true;
                        // return TheModelFactory.Create(PaymentStatusTypes.Completed, null, trackingNumber);
                    }
                    else
                    {
                        //TODO redirect user with Error parameters set
                        isTransactionComplete = false;
                        TransactionError = "Transaction failed";
                        //return TheModelFactory.Create(PaymentStatusTypes.Canceled, "Transaction failed", null);
                    }
                }
            }
            var pppp = HttpContext.Current;
            if (mainContext != null)
            {
                HttpContext.Current = mainContext;
                var sessionNew = WebApiApplication.SessionFactory.OpenSession();
                NHibernate.Context.CurrentSessionContext.Bind(sessionNew);
            }
            if (isTransactionComplete)
            {
                paymentStatus = PaymentStatusTypes.Completed;
                ErrorPayment = null;
                trackNumber = trackingNumber;
                //return TheModelFactory.Create(PaymentStatusTypes.Completed, null, trackingNumber);
            }
            else
            {
                paymentStatus = PaymentStatusTypes.Canceled;
                ErrorPayment = TransactionError;
                trackNumber = null;
                // return TheModelFactory.Create(PaymentStatusTypes.Canceled, TransactionError, null);
            }
        }

        private CASPaymentDTO.Domain.Transactions GetTransactionFees(CASPaymentDTO.Domain.Transactions mainTransaction, float rate)
        {
            Decimal transactionfeesAmount = (decimal)rate * mainTransaction.Amount.Value;
            Models.POSTModels.PaymentPOSTModel feesPaymodel = new Models.POSTModels.PaymentPOSTModel
            {
                Amount = transactionfeesAmount,
                Currency = PaymentSystemConstants.TransactionFeesAccountCurrency,
                //In this part we can determine who is going to pay the fees.... At the current time the payer is responsible for paying the fees.
                PayerAccountNumber = mainTransaction.DestinationAccountItem.Accountnumber,
                PayeeAccountNumber = PaymentSystemConstants.TransactionFeesAccountNumber,
                TransactionType = TransactionTypes.Fees,
                Description = "Transaction Fees: " + Math.Abs( transactionfeesAmount) + " " + PaymentSystemConstants.TransactionFeesAccountCurrency + " is deducted from " + mainTransaction.DestinationAccountItem.Accountnumber,
            };
            return CreatePayerTransaction(feesPaymodel, mainTransaction.DestinationAccountItem).Result;
        }

        private CASPaymentDTO.Domain.AccountDailyActivity GetPayerTodayActivity(CASPaymentDTO.Domain.Account payerAccount, string TransactionType, Decimal AmountToPay, out bool hasDailyActivity)
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

        private CASPaymentDTO.Domain.Transactions ReverseTransaction(CASPaymentDTO.Domain.Transactions mainTransaction)
        {
            CASPaymentDTO.Domain.Transactions reverseTransaction = new CASPaymentDTO.Domain.Transactions();
            reverseTransaction.Amount = (-1) * mainTransaction.Amount;
            reverseTransaction.CurrencyTypeItem = mainTransaction.CurrencyTypeItem;
            reverseTransaction.DestinationAccountItem = mainTransaction.SourceAccountItem;
            reverseTransaction.SourceAccountItem = mainTransaction.DestinationAccountItem;
            reverseTransaction.TransactionTypeItem = mainTransaction.TransactionTypeItem;
            reverseTransaction.Description = mainTransaction.Description;
            return reverseTransaction;
        }

        private string GenerateUniqueTrackingNumber(string PayeeAccountNumber, string PayerAccountNumber)
        {
            string trackingNumber = "";
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
