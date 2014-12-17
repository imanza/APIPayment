using APIRestPayment.Constants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Threading;
using System.Web.Http;
using System.Web.Http.Routing;

namespace APIRestPayment.Controllers
{
    //[Filters.GeneralAuthorization]
    [Authorize]
    public class AccountOwnershipController : BaseApiController
    {
        CASPaymentDAO.DataHandler.UsersDataHandler usersHandler = new CASPaymentDAO.DataHandler.UsersDataHandler(WebApiApplication.SessionFactory);

        #region Get
        
        public HttpResponseMessage Get(int usersId , int page = 0, int pageSize = 10)
        {
            //check whether the entity requesting this resource is admin. if not, then an unauthorized response is sent back.
            if (base.CurrentUserAccessType != DataAccessTypes.Administrator || 
               !((ClaimsIdentity) User.Identity).Claims.Where(c => c.Type == ClaimNames.OAuthScope).Select(c => c.Value).Contains(ScopeTypes.AllAccess))
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
                try
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
                    .Select(s => TheModelFactory.Create(s, DataAccessTypes.Administrator));
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
                catch (Exception e)
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new Models.QueryResponseModel
                    {
                        meta = new Models.MetaModel
                        {
                            code = (int)HttpStatusCode.BadRequest,
                            errorMessage = "The request has invalid arguments! --- "+e.Message 
                        }
                    });
                }
            }
        }

        #endregion
    }
}
