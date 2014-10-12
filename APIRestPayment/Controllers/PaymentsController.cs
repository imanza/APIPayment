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
    [Filters.GeneralAuthorization]
    public class PaymentsController : BaseApiController
    {
        CASPaymentDAO.DataHandler.TransactionsDataHandler transactionHandler = new CASPaymentDAO.DataHandler.TransactionsDataHandler(WebApiApplication.SessionFactory);

        #region GET

        public HttpResponseMessage GetPayment(long id)
        {
            try
            {
                CASPaymentDTO.Domain.Transactions searchedTransaction = this.transactionHandler.GetEntity(id);
                return Request.CreateResponse(HttpStatusCode.OK, new Models.QueryResponseModel
                {
                    meta = new Models.MetaModel
                    {
                        code = (int)HttpStatusCode.OK
                    },
                    data = TheModelFactory.Create(searchedTransaction)
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


        public HttpResponseMessage Post([FromBody] APIRestPayment.Models.POSTModels.PaymentPOSTModel paymentPOSTModel)
        {
           
                string ParseErrorMessage; 
                CASPaymentDTO.Domain.Transactions transactionPAYEE = TheModelFactory.Parse(paymentPOSTModel , out ParseErrorMessage);

                if (transactionPAYEE == null)
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new Models.QueryResponseModel
                    {
                        meta = new Models.MetaModel
                        {
                            code = (int)HttpStatusCode.BadRequest,
                            errorMessage = ParseErrorMessage,
                        }
                    });
                }

                //if (CheckValidityOFPOST(transactionPAYEE))
                //{

                //}
                

                var response = Request.CreateResponse(HttpStatusCode.Moved /*, new Object[]{paymentPOSTModel , transactionPAYEE} */);
                string fullyQualifiedUrl = Request.RequestUri.GetLeftPart(UriPartial.Authority);
                response.Headers.Location = new Uri(fullyQualifiedUrl +"/Home/sales");
                //response.Headers.Location = new Uri( , UriKind.Relative);
                return response;
            

            }

            private bool CheckValidityOFPOST(CASPaymentDTO.Domain.Transactions transactionPAYEE)
            {

                return true;
            }

        #endregion


    }
}
