using APIRestPayment.Constants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Threading;
using System.Transactions;
using System.Web;
using System.Web.Http;
using System.Web.Http.Routing;

namespace APIRestPayment.Controllers
{
    //[Filters.GeneralAuthorization]
    [Authorize]
    [RoutePrefix("api/accounts")]
    public class AccountsController : BaseApiController
    {
        #region Handlers
        
        CASPaymentDAO.DataHandler.AccountDataHandler accountHandler = new CASPaymentDAO.DataHandler.AccountDataHandler(WebApiApplication.SessionFactory);
        CASPaymentDAO.DataHandler.UsersDataHandler userHandler = new CASPaymentDAO.DataHandler.UsersDataHandler(WebApiApplication.SessionFactory);
        CASPaymentDAO.DataHandler.SettingDataDataHandler settingDataHandler = new CASPaymentDAO.DataHandler.SettingDataDataHandler(WebApiApplication.SessionFactory);
        
        #endregion


        #region Access

        public override DataAccessTypes CurrentUserAccessType
        {
            get
            {
                if (base.CurrentUserAccessType == DataAccessTypes.Administrator) return DataAccessTypes.Administrator;
                else
                {
                    var routeData = Request.GetRouteData();
                    var resourceID = routeData.Values["id"] as string;
                    
                    var identity = User.Identity as ClaimsIdentity;
                    if (identity != null)
                    {
                        var scopesGranted = identity.Claims.Where(c => c.Type == ClaimNames.OAuthScope).Select(c => c.Value);
                        if (scopesGranted.Contains(ScopeTypes.Profile) || scopesGranted.Contains(ScopeTypes.AllAccess))
                        {
                            var currentuserIdstring = identity.Claims.Where(c => c.Type == ClaimNames.NameID).Select(c => c.Value).FirstOrDefault();
                            long currentUserId;
                            if (Int64.TryParse(currentuserIdstring, out currentUserId))
                            {
                                CASPaymentDTO.Domain.Users currentUser = userHandler.GetEntity(currentUserId);
                                foreach (CASPaymentDTO.Domain.Account ac in currentUser.AccountS)
                                {
                                    if (ac.Id.ToString() == resourceID)
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
        }

        #endregion

        #region Get
        [Route("{id:long}", Name = "GetAccount")]
        public HttpResponseMessage GetAccount(long id)
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
            var scopesGranted = identity.Claims.Where(c => c.Type == ClaimNames.OAuthScope).Select(c => c.Value);
            if (scopesGranted.Contains(ScopeTypes.ManageAccounts) || scopesGranted.Contains(ScopeTypes.AllAccess))
            {
                try
                {
                    CASPaymentDTO.Domain.Account searchedAccount = this.accountHandler.GetEntity(id);
                    if (searchedAccount != null)
                    {
                        return Request.CreateResponse(HttpStatusCode.OK, new Models.QueryResponseModel
                        {
                            meta = new Models.MetaModel
                            {
                                code = (int)HttpStatusCode.OK
                            },
                            data = TheModelFactory.Create(searchedAccount, CurrentUserAccessType),
                        });
                    }
                    else
                    {
                        return Request.CreateResponse(HttpStatusCode.NotFound, new Models.QueryResponseModel
                        {
                            meta = new Models.MetaModel
                            {
                                code = (int)HttpStatusCode.NotFound,
                                errorMessage = "Account was not found"
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
                //User does not have permission to view accounts
                return Request.CreateResponse(HttpStatusCode.Unauthorized, new Models.QueryResponseModel
                {
                    meta = new Models.MetaModel
                    {
                        code = (int)HttpStatusCode.Unauthorized,
                        errorMessage = "Not enough permisions granted to view accounts!"
                    },
                });
            }
        }


        [Route( Name = "GetAllAccounts")]
        public HttpResponseMessage Get(int page = 0, int pageSize = 10)
        {

            IList<CASPaymentDTO.Domain.Account> result;
            if (base.CurrentUserAccessType != DataAccessTypes.Administrator)
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
                //if (Int64.TryParse(Thread.CurrentPrincipal.Identity.Name, out currentUserId)) result = userHandler.GetEntity(currentUserId).AccountS;
                else
                {
                    var scopesGranted = identity.Claims.Where(c => c.Type == ClaimNames.OAuthScope).Select(c => c.Value);
                    if (scopesGranted.Contains(ScopeTypes.ManageAccounts) || scopesGranted.Contains(ScopeTypes.AllAccess))
                    {
                        string currentUserId = identity.Claims.Where(c => c.Type == ClaimNames.NameID).Select(c => c.Value).FirstOrDefault();
                        long currentuserIdLong;
                        if (long.TryParse(currentUserId, out currentuserIdLong)) result = userHandler.GetEntity(currentuserIdLong).AccountS.Where(x => x.State ==0).ToList();
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
                        //User does not have permission to view accounts
                        return Request.CreateResponse(HttpStatusCode.Unauthorized, new Models.QueryResponseModel
                        {
                            meta = new Models.MetaModel
                            {
                                code = (int)HttpStatusCode.Unauthorized,
                                errorMessage = "Not enough permisions granted to view accounts!"
                            },
                        });
                    }
                }
            }
            else
            {
                result = this.accountHandler.SelectAll().Cast<CASPaymentDTO.Domain.Account>().ToList();
            }
            //////////////////////////////////////////////////
            var totalCount = result.Count();
            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
            var urlHelper = new UrlHelper(Request);
            var prevLink = page > 0 ? urlHelper.Link("GetAllAccounts", new { page = page - 1 }) : null;
            var nextLink = page < totalPages - 1 ? urlHelper.Link("GetAllAccounts", new { page = page + 1 }) : null;
            ///////////////////////////////////////////////////
            var resultInModel = result
            .Skip(pageSize * page)
            .Take(pageSize)
            .ToList()
            .Select(s => TheModelFactory.Create(s, (base.CurrentUserAccessType == DataAccessTypes.Administrator) ? DataAccessTypes.Administrator : DataAccessTypes.Owner));
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

        #endregion

        #region POST
        [Route(Name="CreateAccount")]
        [HttpPost]
        /// <summary>
        /// This method tries to save a new account based on the sites regulations. 
        /// </summary>
        /// <param name="accountPOSTModel">The requested account to be saved. This parameter must contain 
        /// 1- The type of account.
        /// 2- The currency name of the account.
        /// 3- The access to other users to see the account number in restricted mode.
        /// 4- The Id of the user wich this account belongs to.</param>
        /// <returns>Response contains the account properties including the Id related to this account.</returns>
        public HttpResponseMessage Post([FromBody] APIRestPayment.Models.POSTModels.AccountPOSTModel accountPOSTModel)
        {
            var identity = User.Identity as ClaimsIdentity;
            if (identity != null)
            {
                var scopesGranted = identity.Claims.Where(c => c.Type == ClaimNames.OAuthScope).Select(c => c.Value);
                if (scopesGranted.Contains(ScopeTypes.ManageAccounts) || scopesGranted.Contains(ScopeTypes.AllAccess))
                {
                    try
                    {
                        string ParseErrorMessage;
                        CASPaymentDTO.Domain.Account account = TheModelFactory.Parse(accountPOSTModel, out ParseErrorMessage);

                        if (account == null)
                        {
                            //Account POST model could not be parsed or contains invalid data
                            return Request.CreateResponse(HttpStatusCode.BadRequest, new Models.QueryResponseModel
                            {
                                meta = new Models.MetaModel
                                {
                                    code = (int)HttpStatusCode.BadRequest,
                                    errorMessage = ParseErrorMessage,
                                }
                            });
                        }
                        //////////////////
                        //check the validity of this post. Prevent other user from creating accounts for others.                
                        if (CheckValidityofPost(account))
                        {
                            //Complete unassigned data in account

                            account.IsActive = true;
                            account.Accountnumber = this.GenerateAccountNumber(account);
                            account.Paymentcode = this.GeneratePaymentCode(account.Accountnumber);
                            account.Balance = 0;
                            account.Dateofopening = DateTime.Now;

                            /////////////////
                            ///Here we start a transaction to save the account
                            try
                            {
                                accountHandler.Save(account);
                                //Return account with details including its Id
                                account = accountHandler.Search(account).Cast<CASPaymentDTO.Domain.Account>().First();
                                if (account != null)
                                {
                                    var result = Request.CreateResponse(HttpStatusCode.Created, new Models.QueryResponseModel
                                    {
                                        meta = new Models.MetaModel
                                        {
                                            code = (int)HttpStatusCode.Created,
                                        },
                                        data = this.TheModelFactory.Create(account, DataAccessTypes.Owner),
                                    });
                                    return result;
                                }


                            }
                            catch (TransactionAbortedException ex)
                            {
                                return Request.CreateResponse(HttpStatusCode.BadRequest, new Models.QueryResponseModel
                                        {
                                            meta = new Models.MetaModel
                                            {
                                                code = (int)HttpStatusCode.BadRequest,
                                                errorMessage = "Could not save the account.\n" + ex
                                            },
                                        });
                            }
                            catch (ApplicationException ex)
                            {
                                return Request.CreateResponse(HttpStatusCode.BadRequest, new Models.QueryResponseModel
                                {
                                    meta = new Models.MetaModel
                                    {
                                        code = (int)HttpStatusCode.BadRequest,
                                        errorMessage = "Some problems happend while saving the account. Account not saved!\n" + ex
                                    },
                                });
                            }
                        }
                        else
                        {
                            //Attempted to Create an account for other users
                            return Request.CreateResponse(HttpStatusCode.Unauthorized, new Models.QueryResponseModel
                            {
                                meta = new Models.MetaModel
                                {
                                    code = (int)HttpStatusCode.Unauthorized,
                                    errorMessage = "You are not allowed to create accounts for other users!"
                                },
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        //Error while saving account or any other unpredicted error happend
                        return Request.CreateResponse(HttpStatusCode.BadRequest, new Models.QueryResponseModel
                        {
                            meta = new Models.MetaModel
                            {
                                code = (int)HttpStatusCode.BadRequest,
                                errorMessage = "The account cannot be saved:\n" + ex
                            },
                        });
                    }
                }
                else
                {
                    //User does not have permission to create account
                    return Request.CreateResponse(HttpStatusCode.Unauthorized, new Models.QueryResponseModel
                    {
                        meta = new Models.MetaModel
                        {
                            code = (int)HttpStatusCode.Unauthorized,
                            errorMessage = "Not enough permisions granted to do this action!"
                        },
                    });
                }
            }
            return Request.CreateResponse(HttpStatusCode.BadRequest, new Models.QueryResponseModel
            {
                meta = new Models.MetaModel
                {
                    code = (int)HttpStatusCode.BadRequest,
                    errorMessage = "Account not saved due to problems\n"
                },
            });
        }
#endregion

        #region check and generation of account related numbers
        private bool CheckValidityofPost (CASPaymentDTO.Domain.Account account)
        {
            
            
            var identity = User.Identity as ClaimsIdentity;
            if (identity != null)
            {
                var currentuserIdstring = identity.Claims.Where(c => c.Type == ClaimTypes.NameIdentifier).Select(c => c.Value).FirstOrDefault();
                long currentUserId;
                if (Int64.TryParse(currentuserIdstring, out currentUserId))
                {
                    string upperLimitAccountSTRING= settingDataHandler.Search(new CASPaymentDTO.Domain.SettingData { Name = "MaxAccountCount" }).Cast<CASPaymentDTO.Domain.SettingData>().FirstOrDefault().Value;
                    int upperLimitAccountINT;
                    CASPaymentDTO.Domain.Users currentUser = userHandler.GetEntity(currentUserId);
                    if (currentUserId != account.UsersItem.Id) return false;
                    //TODO set the upper limit for number of accounts. Now this amount is 5 
                    else if ((int.TryParse(upperLimitAccountSTRING ,out upperLimitAccountINT)) && account.UsersItem.AccountS.Count >= upperLimitAccountINT) return false;
                    else return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }

        }
        /// <summary>
        /// Generates a unique Account Number regarding the type of account - user Id - the index of previous accounts
        /// </summary>
        /// <param name="ac">The Account to be created with defined account type - userId</param>
        /// <returns>
        /// string account number
        /// The format of account number is:
        /// AccountTypeCode(3 char) + UserId(8 char) + The index of the account among users accounts(1 char)
        /// Totally 12 caracters.
        /// </returns>
        private string GenerateAccountNumber(CASPaymentDTO.Domain.Account ac)
        {
            string accountTypeCode = ac.AccountTypeItem.Code, userCode = ac.UsersItem.Id.ToString(), previousAccountCount = (ac.UsersItem.AccountS.Count + 1).ToString();
            accountTypeCode = (accountTypeCode.Length < 3) ? accountTypeCode.PadLeft(3, '0') : accountTypeCode;
            userCode = userCode.PadLeft(8, '0');
            return accountTypeCode + userCode + previousAccountCount;
        }

        private string GeneratePaymentCode(string AccountNumber)
        {
            // TODO generate a payment code for the account
            return "PC" + ((Guid.NewGuid().ToString("n") + AccountNumber).GetHashCode().ToString()).Substring(0,8);
        }

        #endregion
    }
}
