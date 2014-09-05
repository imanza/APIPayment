using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using APIRestPayment.Models;
using APIRestPayment.Constants;
using System.Net.Http;
using System.Threading;

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

        protected RoleTypes CurrentUserRoleType
        {
            get
            {
                var IsAdministrator = Thread.CurrentPrincipal.IsInRole(RoleTypes.Administrator.ToString());
                if (IsAdministrator) return RoleTypes.Administrator;
                else return RoleTypes.OrdinaryUser;
            }
        }

        public virtual DataAccessTypes CurrentUserAccessType
        {
            get
            {
                if (CurrentUserRoleType == RoleTypes.Administrator) return DataAccessTypes.Administrator;
                else return DataAccessTypes.Anonymous;
            }
        }
    }
}