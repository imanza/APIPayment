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
    public class AccountOwnershipController : BaseApiController
    {
        CASPaymentDAO.DataHandler.UsersDataHandler usersHandler = new CASPaymentDAO.DataHandler.UsersDataHandler(WebApiApplication.SessionFactory);


        #region Access

        public override DataAccessTypes CurrentUserAccessType
        {
            get
            {
                if (base.CurrentUserAccessType == DataAccessTypes.Administrator) return DataAccessTypes.Administrator;
                else
                {
                    // TODO My Own Logic To check the owner
                    //////////////////////////////
                    var routeData = Request.GetRouteData();
                    var resourceID = routeData.Values["usersId"] as string;
                    if (Thread.CurrentPrincipal.Identity.Name == resourceID)
                    {
                        return DataAccessTypes.Owner;
                    }
                    /////////////////////////////
                    return DataAccessTypes.Anonymous;
                }
            }
        }

        #endregion

        #region Get

        
        public HttpResponseMessage Get(int usersId , int page = 0, int pageSize = 10)
        {
            CASPaymentDTO.Domain.Users SpecificUser = usersHandler.GetEntity(usersId);
            IList<CASPaymentDTO.Domain.Account> result = SpecificUser.AccountS;
            result.OrderBy(x => x.Accountnumber);
            //////////////////////////////////////////////////
            var totalCount = result.Count();
            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
            var urlHelper = new UrlHelper(Request);
            var prevLink = page > 0 ? urlHelper.Link("AccountOwnership", new { page = page - 1 }) : null;
            var nextLink = page < totalPages - 1 ? urlHelper.Link("AccountOwnership", new { page = page + 1 }) : null;
            ///////////////////////////////////////////////////
            var resultInModel = result
            .Skip(pageSize * page)
            .Take(pageSize)
            .ToList()
            .Select(s => TheModelFactory.Create(s ,  (CurrentUserAccessType == DataAccessTypes.Administrator) ? DataAccessTypes.Administrator : DataAccessTypes.Owner));
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
    }
}
