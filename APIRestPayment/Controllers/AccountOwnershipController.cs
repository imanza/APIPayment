using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Routing;

namespace APIRestPayment.Controllers
{
    public class AccountOwnershipController : BaseApiController
    {
        CASPaymentDAO.DataHandler.UsersDataHandler usersHandler = new CASPaymentDAO.DataHandler.UsersDataHandler(WebApiApplication.SessionFactory);
        public Object Get(int usersId , int page = 0, int pageSize = 10)
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
