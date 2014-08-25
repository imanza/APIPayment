using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Routing;

namespace APIRestPayment.Controllers
{
    public class UsersController : BaseApiController
    {
        CASPaymentDAO.DataHandler.UsersDataHandler userHandler = new CASPaymentDAO.DataHandler.UsersDataHandler(WebApiApplication.SessionFactory);

        public APIRestPayment.Models.UserModel GetUser(long id)
        {
            return TheModelFactory.Create(this.userHandler.GetEntity(id));
        }
        public Object Get(int page = 0, int pageSize = 2)
        {
            //redirection example
            //if (page == 0)
            //{
            //    var response = Request.CreateResponse(HttpStatusCode.Moved);
            //    string fullyQualifiedUrl = Request.RequestUri.GetLeftPart(UriPartial.Authority);
            //    //response.Headers.Location = new Uri(fullyQualifiedUrl);
            //    response.Headers.Location = new Uri("http://www.zaeemflowers.com");
            //    return response;
            //}
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
            return new Models.QueryResponseModel
            {
                TotalCount = totalCount,
                TotalPages = totalPages,
                PrevPageLink = prevLink,
                NextPageLink = nextLink,
                Data = resultInModel.ToList()
            };
        }
    }
}
