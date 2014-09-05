using APIRestPayment.Constants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Web.Http;
using System.Web.Http.Routing;

namespace APIRestPayment.Controllers
{
    [Filters.GeneralAuthorization]
    public class AccountsController : BaseApiController
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
                    // My Own Logic To check the owner of the account
                    //////////////////////////////
                    var routeData = Request.GetRouteData();
                    var resourceID = routeData.Values["id"] as string;
                    if (resourceID != null)
                    {
                        long currentUserId;
                        if (Int64.TryParse(Thread.CurrentPrincipal.Identity.Name, out currentUserId))
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


        #region Get

        public HttpResponseMessage GetAccount(long id)
        {
            try
            {
                CASPaymentDTO.Domain.Account searchedAccount = this.accountHandler.GetEntity(id);
                return Request.CreateResponse(HttpStatusCode.OK,new Models.QueryResponseModel{
                   meta = new Models.MetaModel{
                       code = (int)HttpStatusCode.OK
                   },
                   data =  TheModelFactory.Create(searchedAccount , CurrentUserAccessType) , 
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
        public HttpResponseMessage Get(int page = 0, int pageSize = 10)
        {

            IList<CASPaymentDTO.Domain.Account> result;
            if (CurrentUserAccessType != DataAccessTypes.Administrator)
            {
                long currentUserId;
                if (Int64.TryParse(Thread.CurrentPrincipal.Identity.Name, out currentUserId)) result = userHandler.GetEntity(currentUserId).AccountS;
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
            else
            {
                result = this.accountHandler.SelectAll().Cast<CASPaymentDTO.Domain.Account>().ToList();
            }
            //////////////////////////////////////////////////
            var totalCount = result.Count();
            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
            var urlHelper = new UrlHelper(Request);
            var prevLink = page > 0 ? urlHelper.Link("Accounts", new { page = page - 1 }) : null;
            var nextLink = page < totalPages - 1 ? urlHelper.Link("Accounts", new { page = page + 1 }) : null;
            ///////////////////////////////////////////////////
            var resultInModel = result
            .Skip(pageSize * page)
            .Take(pageSize)
            .ToList()
            .Select(s => TheModelFactory.Create(s, (CurrentUserAccessType == DataAccessTypes.Administrator) ? DataAccessTypes.Administrator : DataAccessTypes.Owner));
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

        #endregion
    }
}
