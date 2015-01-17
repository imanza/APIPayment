using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using APIRestPayment.Models;
using APIRestPayment.Constants;
using System.Net.Http;
using System.Threading;
using System.Security.Claims;

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

        //protected ModelFactory ChangeModelFactorySession(NHibernate.ISessionFactory sessioFactory ,HttpRequestMessage request){
        //    _modelFactory = new ModelFactory(sessioFactory, request);
        //    return _modelFactory;
        //}

        protected RoleTypes CurrentUserRoleType
        {
            get
            {
                
                var identity = User.Identity as ClaimsIdentity;
                if (identity != null)
                {
                    string identityRole = identity.Claims.Where(c => c.Type == ClaimNames.Role).Select(c => c.Value).FirstOrDefault();
                    string gg = identity.RoleClaimType;
                    var IsAdministrator = (identityRole == RoleTypes.Administrator.ToString());
                    if (IsAdministrator) return RoleTypes.Administrator;
                }
                return RoleTypes.OrdinaryUser;
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