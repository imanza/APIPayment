using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Routing;

namespace APIRestPayment.Controllers
{
    public class AccountsController : BaseApiController
    {
        CASPaymentDAO.DataHandler.AccountDataHandler accountHandler = new CASPaymentDAO.DataHandler.AccountDataHandler(WebApiApplication.SessionFactory);

        public APIRestPayment.Models.AccountModel GetAccount(long id)
        {
            return TheModelFactory.Create(this.accountHandler.GetEntity(id));
        }
        public Object Get(int page = 0, int pageSize = 10)
        {

            IList<CASPaymentDTO.Domain.Account> result = this.accountHandler.SelectAll().Cast<CASPaymentDTO.Domain.Account>().ToList();

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
