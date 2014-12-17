using APIRestPayment.Constants;
using NHibernate.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Web.Http;
using System.Web.Http.Routing;

namespace APIRestPayment.Controllers
{
    [Authorize]
    public class AccountTransactionsController : BaseApiController
    {
        CASPaymentDAO.DataHandler.AccountDataHandler accountHandler = new CASPaymentDAO.DataHandler.AccountDataHandler(WebApiApplication.SessionFactory);
        CASPaymentDAO.DataHandler.UsersDataHandler userHandler = new CASPaymentDAO.DataHandler.UsersDataHandler(WebApiApplication.SessionFactory);

        #region Access

        public override DataAccessTypes CurrentUserAccessType
        {
            get
            {
                if (base.CurrentUserAccessType == DataAccessTypes.Administrator) return DataAccessTypes.Administrator;
                else
                {
                    var routeData = Request.GetRouteData();
                    var resourceID = routeData.Values["accountsId"] as string;

                    var identity = User.Identity as ClaimsIdentity;
                    if (identity != null)
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
                    /////////////////////////////
                    return DataAccessTypes.Anonymous;
                }
            }
        }

        #endregion

        #region GET
        public HttpResponseMessage Get(long accountsId, [FromUri] string inout, int page = 0, int pageSize = 10, string startDate = "", string endDate = "")
        {
            try
            {
                if (CurrentUserAccessType == DataAccessTypes.Anonymous)
                {
                    return Request.CreateResponse(HttpStatusCode.Unauthorized, new Models.QueryResponseModel
                    {
                        meta = new Models.MetaModel
                        {
                            code = (int)HttpStatusCode.Unauthorized,
                            errorMessage = "You are not allowed to do this action!"
                        },
                    });
                }
                else
                {
                    var identity = User.Identity as ClaimsIdentity;
                    if (identity != null)
                    {
                        var scopesGranted = identity.Claims.Where(c => c.Type == ClaimNames.OAuthScope).Select(c => c.Value);
                        if (!scopesGranted.Contains(ScopeTypes.Report) && !scopesGranted.Contains(ScopeTypes.AllAccess))
                        {
                            return Request.CreateResponse(HttpStatusCode.Unauthorized, new Models.QueryResponseModel
                            {
                                meta = new Models.MetaModel
                                {
                                    code = (int)HttpStatusCode.Unauthorized,
                                    errorMessage = "Not enough permisions granted to do this action!"
                                },
                            });
                        }
                        else
                        {
                            CASPaymentDTO.Domain.Account SpecificAccount = accountHandler.GetEntity(accountsId);
                            IList<CASPaymentDTO.Domain.Transactions> result = new List<CASPaymentDTO.Domain.Transactions>();

                            result = SpecificAccount.DestinationTransactionsS;
                            ///////////////////////////////////////////////////

                            bool isStartDateParamNull = string.IsNullOrEmpty(startDate);
                            bool isEndDateParamNull = string.IsNullOrEmpty(endDate);
                            if (!(isStartDateParamNull && isEndDateParamNull))
                            {
                                result = result.Where(x =>
                                    (
                                        ((inout == "in") && x.Amount > 0) ||
                                        ((inout == "out") && x.Amount < 0) ||
                                        (inout == "all")
                                    ) &&
                                    (isStartDateParamNull || (!isStartDateParamNull && x.Executiondatetime >= DateFuncs.ToGregorianDate(DateFuncs.ConvertStringToDate(startDate)))) &&
                                    (isEndDateParamNull || (!isEndDateParamNull && x.Executiondatetime <= DateFuncs.ToGregorianDate(DateFuncs.ConvertStringToDate(endDate))))
                                    ).ToList();
                            }
                            result = result.OrderBy(x => x.Executiondatetime).OrderBy(x => x.Id).ToList();
                            //////////////////////////////////////////////////
                            var totalCount = result.Count();
                            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
                            var urlHelper = new UrlHelper(Request);
                            var prevLink = page > 0 ? urlHelper.Link("AccountTransactions", new { page = page - 1 }) : null;
                            var nextLink = page < totalPages - 1 ? urlHelper.Link("AccountTransactions", new { page = page + 1 }) : null;
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
                    }
                    else
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
                }
            }
            catch (Exception e)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, new Models.QueryResponseModel
                {
                    meta = new Models.MetaModel
                    {
                        code = (int)HttpStatusCode.BadRequest,
                        errorMessage = "The request has invalid arguments! --- " + e.Message
                    }
                });
            }
        }

        #endregion
    }
}
