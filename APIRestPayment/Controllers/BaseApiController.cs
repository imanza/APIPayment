using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using APIRestPayment.Models;

namespace APIRestPayment.Controllers
{
    public class BaseApiController : ApiController
    {
        private ModelFactory _modelFactory;

        protected ModelFactory TheModelFactory
        {
            get
            {
                if (_modelFactory == null)
                {
                    _modelFactory = new ModelFactory(Request);
                }
                return _modelFactory;
            }
        }
    }
}