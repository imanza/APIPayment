using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Routing;
using APIRestPayment.Constants;
using System.Threading;
using System.Security.Claims;
using System.Web;

namespace APIRestPayment.Controllers
{
   // [Filters.GeneralAuthorization]
    [Authorize]
    public class UsersController : BaseApiController
    {
        CASPaymentDAO.DataHandler.UsersDataHandler userHandler = new CASPaymentDAO.DataHandler.UsersDataHandler(WebApiApplication.SessionFactory);

        #region Access

        public override DataAccessTypes CurrentUserAccessType
        {
            get
            {
                if( base.CurrentUserAccessType == DataAccessTypes.Administrator) return DataAccessTypes.Administrator;
                else
                {
                    var routeData = Request.GetRouteData();
                    var resourceID = routeData.Values["id"] as string;
                    //
                    //var authentication = System.Web.HttpContextExtensions.GetOwinContext(HttpContext.Current).Authentication;
                    //var ticket = authentication.AuthenticateAsync("Application").Result;
                    //var identity = User.Identity as ClaimsIdentity;
                    var identity = User.Identity as ClaimsIdentity;
                    if (identity != null)
                    {
                        var currentuserId = identity.Claims.Where(c => c.Type == ClaimTypes.NameIdentifier).Select(c => c.Value).FirstOrDefault();
                        if (currentuserId == resourceID)
                        {
                            return DataAccessTypes.Owner;
                        }
                    }
                    /////////////////////////////
                    return DataAccessTypes.Anonymous;
                }
            }
        }

        #endregion

        #region Get

        public HttpResponseMessage GetUser(long id)
        {
            
            try
            {
                CASPaymentDTO.Domain.Users searchedUser = this.userHandler.GetEntity(id);
                return Request.CreateResponse(HttpStatusCode.OK,new Models.QueryResponseModel{
                   meta = new Models.MetaModel{
                       code = (int)HttpStatusCode.OK
                   },
                   data = TheModelFactory.Create(searchedUser , CurrentUserAccessType),
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
        public HttpResponseMessage Get(int page = 0, int pageSize = 2)
        {
            IList<CASPaymentDTO.Domain.Users> result =new List<CASPaymentDTO.Domain.Users>();
            if (base.CurrentUserAccessType != DataAccessTypes.Administrator)
            {
                var authentication = System.Web.HttpContextExtensions.GetOwinContext(HttpContext.Current).Authentication;
                var ticket = authentication.AuthenticateAsync("Application").Result;
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
                    string currentUserIdstring = identity.Claims.Where(c => c.Type == ClaimTypes.NameIdentifier).Select(c => c.Value).FirstOrDefault();
                    long currentUserId;
                    if (long.TryParse(currentUserIdstring, out currentUserId))result.Add(userHandler.GetEntity(currentUserId));
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
            }
            else
            {
                result = this.userHandler.SelectAll().Cast<CASPaymentDTO.Domain.Users>().ToList();
            }
            //////////////////////////////////////////////////
            var totalCount = result.Count();
            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
            var urlHelper = new UrlHelper(Request);
            var prevLink = page > 0 ? urlHelper.Link("Users", new { page = page - 1 }) : null;
            var nextLink = page < totalPages - 1 ? urlHelper.Link("Users", new { page = page + 1 }) : null;
            ///////////////////////////////////////////////////
            var resultInModel = result
            .Skip(pageSize * page)
            .Take(pageSize)
            .ToList()
            .Select(s => TheModelFactory.Create(s , (base.CurrentUserAccessType==DataAccessTypes.Administrator) ? DataAccessTypes.Administrator : DataAccessTypes.Owner));
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
