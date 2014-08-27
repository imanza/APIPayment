using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Web;

using System.Web.Http.Filters;

namespace APIRestPayment.Filters
{
    public class GeneralAuthorization : AuthorizationFilterAttribute
    {
        private CASPaymentDAO.DataHandler.SecretDataHandler secretHandler = new CASPaymentDAO.DataHandler.SecretDataHandler(WebApiApplication.SessionFactory);
        private CASPaymentDAO.DataHandler.UsersDataHandler userHandler = new CASPaymentDAO.DataHandler.UsersDataHandler(WebApiApplication.SessionFactory);
        public override void OnAuthorization(System.Web.Http.Controllers.HttpActionContext actionContext)
        {
            //Case that user is authenticated using forms authentication
            //so no need to check header for basic authentication.
            if (Thread.CurrentPrincipal.Identity.IsAuthenticated)
            {
                return;
            }

            var authHeader = actionContext.Request.Headers.Authorization;

            if (authHeader != null)
            {
                if (authHeader.Scheme.Equals("basic", StringComparison.OrdinalIgnoreCase) &&
                !String.IsNullOrWhiteSpace(authHeader.Parameter))
                {
                    var credArray = GetCredentials(authHeader);
                    var userName = credArray[0];
                    var password = credArray[1];

                    if (IsResourceOwner(userName, actionContext))
                    {
                        //You can use Websecurity or asp.net memebrship provider to login, for
                        //for he sake of keeping example simple, we used out own login functionality
                        if (this.CheckLogin(userName, password))
                        {
                            var currentPrincipal = new GenericPrincipal(new GenericIdentity(userName) , null);
                            Thread.CurrentPrincipal = currentPrincipal;
                            return;
                        }
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
            actionContext.Response = actionContext.Request.CreateResponse(HttpStatusCode.Unauthorized);

            actionContext.Response.Headers.Add("WWW-Authenticate",
            "Basic Scheme='payment' location='"+Constants.Paths.LoginPath+"'");

        }

        private bool CheckLogin(string userEmail, string password)
        {
            CASPaymentDTO.Domain.Users person = (userHandler.SelectAll().Cast<CASPaymentDTO.Domain.Users>().Where( s => s.Email == userEmail).FirstOrDefault());
            if (person != null)
            {
                //if (person.Password == password.GetHashCode())
                if (person.Password.ToString() == password)
                {
                    return true;
                }
            }
            return false;
        }
    }
}