﻿using NHibernate.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Routing;

namespace APIRestPayment.Controllers
{
    public class AccountTransactionsController : BaseApiController
    {
        CASPaymentDAO.DataHandler.AccountDataHandler accountHandler = new CASPaymentDAO.DataHandler.AccountDataHandler(WebApiApplication.SessionFactory);
        public Object Get(int accountsId, [FromUri] string inout, int page = 0, int pageSize = 10)
        {
            CASPaymentDTO.Domain.Account SpecificAccount = accountHandler.GetEntity(accountsId);
            IList<CASPaymentDTO.Domain.Transactions> result = new List<CASPaymentDTO.Domain.Transactions>();
            if(inout=="in")
                foreach(CASPaymentDTO.Domain.Transactions dest in SpecificAccount.DestinationTransactionsS )
                    result.Add(dest);
            else if (inout == "out")
                foreach (CASPaymentDTO.Domain.Transactions src in SpecificAccount.SourceTransactionsS)
                    result.Add(src);
            else
            {
                result = SpecificAccount.SourceTransactionsS;
                result.Concat<CASPaymentDTO.Domain.Transactions>(SpecificAccount.DestinationTransactionsS);
            }
            result.OrderBy(x => x.Executiondatetime);
            //////////////////////////////////////////////////
            var totalCount = result.Count();
            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
            var urlHelper = new UrlHelper(Request);
            var prevLink = page > 0 ? urlHelper.Link("AccountTransactions", new { page = page - 1 }) : null;
            var nextLink = page < totalPages - 1 ? urlHelper.Link("AccountTransactions", new { page = page + 1 }) : null;
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