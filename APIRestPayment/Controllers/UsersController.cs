using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Routing;

namespace APIRestPayment.Controllers
{
    [Filters.GeneralAuthorization]
    public class UsersController : BaseApiController
    {
        CASPaymentDAO.DataHandler.UsersDataHandler userHandler = new CASPaymentDAO.DataHandler.UsersDataHandler(WebApiApplication.SessionFactory);

        public HttpResponseMessage GetUser(long id)
        {
            try
            {
                CASPaymentDTO.Domain.Users searchedUser = this.userHandler.GetEntity(id);
                return Request.CreateResponse(HttpStatusCode.OK, TheModelFactory.Create(searchedUser));
            }
            catch (NHibernate.ObjectNotFoundException)
            {
                return Request.CreateResponse(HttpStatusCode.NotFound , "Item not Found");
            }
               
                
        }
        public HttpResponseMessage Get(int page = 0, int pageSize = 2)
        {
            
            IList<CASPaymentDTO.Domain.Users> result = this.userHandler.SelectAll().Cast<CASPaymentDTO.Domain.Users>().ToList();

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
            .Select(s => TheModelFactory.Create(s));
            ////////////////////////////////////////////////////
            return Request.CreateResponse(HttpStatusCode.OK, new Models.QueryResponseModel
            {
                TotalCount = totalCount,
                TotalPages = totalPages,
                PrevPageLink = prevLink,
                NextPageLink = nextLink,
                Data = resultInModel.ToList()
            });
        }
    }
}
