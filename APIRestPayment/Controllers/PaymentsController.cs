using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using APIRestPayment;
using System.Web.Http.Routing;

namespace APIRestPayment.Controllers
{
    public class PaymentsController : BaseApiController
    {
        CASPaymentDAO.DataHandler.TransactionsDataHandler transactionHandler = new CASPaymentDAO.DataHandler.TransactionsDataHandler(WebApiApplication.SessionFactory);
        
        public HttpResponseMessage GetPayment(long id)
        {
            try
            {
                CASPaymentDTO.Domain.Transactions searchedTransaction = this.transactionHandler.GetEntity(id);
                return Request.CreateResponse(HttpStatusCode.OK, TheModelFactory.Create(searchedTransaction));
            }
            catch (NHibernate.ObjectNotFoundException)
            {
                return Request.CreateResponse(HttpStatusCode.NotFound, "Item not Found");
            } 
        }
        public HttpResponseMessage Get(int page=0 , int pageSize = 10)
        {
            IList<CASPaymentDTO.Domain.Transactions> result = this.transactionHandler.SelectAll().Cast<CASPaymentDTO.Domain.Transactions>().ToList();
            //////////////////////////////////////////////////
            var totalCount = result.Count();
            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
            var urlHelper = new UrlHelper(Request);
            var prevLink = page > 0 ? urlHelper.Link("Payments", new { page = page - 1 }) : null;
            var nextLink = page < totalPages - 1 ? urlHelper.Link("Payments", new { page = page + 1 }) : null;
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
