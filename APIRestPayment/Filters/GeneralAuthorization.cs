using NHibernate;
using NHibernate.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http.Filters;

namespace APIRestPayment.Filters
{
    public class GeneralAuthorization : AuthorizationFilterAttribute
    {
        private bool lastTimeUnauthenticated = false;
        private ISessionFactory auxilarySessionFactory;
        private CASPaymentDAO.DataHandler.SecretDataHandler secretHandler = new CASPaymentDAO.DataHandler.SecretDataHandler(WebApiApplication.SessionFactory);
        private CASPaymentDAO.DataHandler.UsersDataHandler userHandler = new CASPaymentDAO.DataHandler.UsersDataHandler(WebApiApplication.SessionFactory);
        public override void OnAuthorization(System.Web.Http.Controllers.HttpActionContext actionContext)
        {
            //Case that user is authenticated using forms authentication
            //so no need to check header for basic authentication.
            //if (Thread.CurrentPrincipal.Identity.IsAuthenticated)
            //{
            //    return;
            //}

            var authHeader = actionContext.Request.Headers.Authorization;

            if (authHeader != null)
            { 
                if (authHeader.Scheme.Equals("basic", StringComparison.OrdinalIgnoreCase) &&
                !String.IsNullOrWhiteSpace(authHeader.Parameter))
                {
                    var credArray = GetCredentials(authHeader);
                    var userEmail = credArray[0];
                    var password = credArray[1];

                    if (IsResourceOwner(userEmail, actionContext))
                    {
                        Task.Run(async () =>
                        {
                        

                        Controllers.MemberController memberController = new Controllers.MemberController();


                        if (await memberController.CheckCredentialsValidity(userEmail, password, false))
                        {
                            this.lastTimeUnauthenticated = false;
                            return;
                        }
                        });

                        //You can use Websecurity or asp.net memebrship provider to login, for
                        //for he sake of keeping example simple, we used out own login functionality
                        //CASPaymentDTO.Domain.Users userQuery;
                        //if (this.CheckLogin(userEmail, password , out userQuery))
                        //{
                        //    //String[] userRolesArray = new String[userQuery.UsersRolesS.Count];
                        //    //for (int i = 0; i < userQuery.UsersRolesS.Count; i++)
                        //    //{
                        //    //    userRolesArray[i] = userQuery.UsersRolesS[i].RolesItem.RoleName;
                        //    //}
                        //    //var identity = new GenericIdentity(userQuery.Id.ToString() , "Application");
                        //    //var principal = new GenericPrincipal(identity, userRolesArray);
                        //    //Thread.CurrentPrincipal = principal;
                        //    //if (HttpContext.Current != null)HttpContext.Current.User = principal;

                        //    //var currentPrincipal = new GenericPrincipal(new GenericIdentity(userEmail) , null);
                        //    //Thread.CurrentPrincipal = currentPrincipal;
                        //    //return
                        //}
                    }
                }
            }
            HandleUnauthorizedRequest(actionContext);
        }

        private string[] GetCredentials(System.Net.Http.Headers.AuthenticationHeaderValue authHeader)
        {

            //Base 64 encoded string
            var rawCred = authHeader.Parameter;
            var encoding = Encoding.GetEncoding("iso-8859-1");
            var cred = encoding.GetString(Convert.FromBase64String(rawCred));

            var credArray = cred.Split(':');

            return credArray;
        }

        private bool IsResourceOwner(string userEmail, System.Web.Http.Controllers.HttpActionContext actionContext)
        {
            var routeData = actionContext.Request.GetRouteData();

            var resourceID = routeData.Values["id"] as string;
            long Longid;
            if (Int64.TryParse(resourceID, out Longid))
            {
                CASPaymentDTO.Domain.Users userWithID = userHandler.GetEntity(Longid);
            }
            //TODO check if the request is authorized for the user
            // you can check it for different routes maybe

            ////var resourceUserName = routeData.Values["userName"] as string;

            ////if (resourceUserName == userName)
            ////{
            ////    return true;
            ////}
            //return false;
            return true;
        }

        private void HandleUnauthorizedRequest(System.Web.Http.Controllers.HttpActionContext actionContext)
        {
            this.lastTimeUnauthenticated = true;
            actionContext.Response = actionContext.Request.CreateResponse(HttpStatusCode.Unauthorized);

            actionContext.Response.Headers.Add("WWW-Authenticate",
            "Basic Scheme='APIRestPayment' location='" + Constants.Paths.LoginPath + "'");

        }

        private void CreateSessions()
        {
            /// congigure NHibernate
            var nhConfig = new NHibernate.Cfg.Configuration().Configure();
            auxilarySessionFactory = nhConfig.BuildSessionFactory();

            var session = auxilarySessionFactory.OpenSession();
            CurrentSessionContext.Bind(session);
        }

        private bool CheckLogin(string userEmail, string password, out CASPaymentDTO.Domain.Users userEntity)
        {
            NHibernate.ISession session;
            bool sessionCreated = false;

            if (lastTimeUnauthenticated)
            {
                this.CreateSessions();
                userHandler = new CASPaymentDAO.DataHandler.UsersDataHandler(this.auxilarySessionFactory);
                sessionCreated = true;
            }
            CASPaymentDTO.Domain.Users person = (userHandler.SelectAll().Cast<CASPaymentDTO.Domain.Users>().Where(s => s.Email == userEmail).FirstOrDefault());
            if (person != null)
            {
                // TODO correct the password check
                //if (person.Password == password.GetHashCode())
                if (person.Password.ToString() == password)
                {
                    if (sessionCreated) WebApiApplication.ChangeSession(this.auxilarySessionFactory);
                    userEntity = person;
                    return true;
                }
            }
            if (sessionCreated)
            {
                session = CurrentSessionContext.Unbind(auxilarySessionFactory);
                session.Close();
                session.Dispose();
            }
            userEntity = null;
            return false;
        }
    }
}