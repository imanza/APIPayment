using Microsoft.Owin.Security.Infrastructure;
using Microsoft.Owin.Security.OAuth;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace APIRestPayment.Controllers
{
    public class OauthController : Controller
    {
        private CASPaymentDAO.DataHandler.ApplicationDataHandler appHandler = new CASPaymentDAO.DataHandler.ApplicationDataHandler(WebApiApplication.SessionFactory);

        #region Code Behind
        
        public ActionResult Authorize()
        {

            if (Response.StatusCode != 200)
            {
                return View("AuthorizeError");
            }

            var authentication = HttpContext.GetOwinContext().Authentication;
            var ticket = authentication.AuthenticateAsync("Application").Result;
            var identity = ticket != null ? ticket.Identity : null;
            if (identity == null)
            {
                //return Redirect("http://"+Request.Url.Authority + "/Account/Login");
                authentication.Challenge("Application");
                return new HttpUnauthorizedResult();
            }

            var scopes = (Request.QueryString.Get("scope") ?? "").Split(' ');

            if (Request.HttpMethod == "POST")
            {
                if (!string.IsNullOrEmpty(Request.Form.Get("submit.Grant")))
                {
                    identity = new ClaimsIdentity(identity.Claims, "Bearer", identity.NameClaimType, identity.RoleClaimType);
                    foreach (var scope in scopes)
                    {
                        identity.AddClaim(new Claim("urn:oauth:scope", scope));
                    }
                    authentication.SignIn(identity);
                }
                if (!string.IsNullOrEmpty(Request.Form.Get("submit.Login")))
                {
                    authentication.SignOut("Application");
                    authentication.Challenge("Application");
                    return new HttpUnauthorizedResult();
                }
            }

            return View();
        }

        //
        // GET: /Oauth/
        public ActionResult Index()
        {
            return View();
        }

            #endregion


        #region Expressions And Delegates

        public static Task ValidateRequest(OAuthValidateAuthorizeRequestContext context)
        {
            return Task.Run(() =>
            {
                //Authorization validation here
                //Somewhere in the request you should create the identity and sign in with it, I put it here, it could be a page on your app?
                context.Validated();
            });
        }

        public static Task ValidateRedirectUri(OAuthValidateClientRedirectUriContext context)
        {

            CASPaymentDAO.DataHandler.ApplicationDataHandler applicationHandler = new CASPaymentDAO.DataHandler.ApplicationDataHandler(WebApiApplication.SessionFactory);
            return Task.Run(() =>
            {
                CASPaymentDTO.Domain.Application applicationPOCO = applicationHandler.Search(new CASPaymentDTO.Domain.Application() { ClientID = context.ClientId }).Cast<CASPaymentDTO.Domain.Application>().FirstOrDefault();
                if (!object.Equals(applicationPOCO, default(CASPaymentDTO.Domain.Application)))
                {
                    if (context.RedirectUri == applicationPOCO.ReturnUri)
                    {
                        context.Validated(applicationPOCO.ReturnUri);
                    }
                }
            });
        }

        public static Task ValidateAuthorizeEndpoint(OAuthAuthorizeEndpointContext context)
        {
            return Task.Run(() =>
            {
                var authentication = context.OwinContext.Authentication;
                var ticket = authentication.AuthenticateAsync("Application").Result;
                var identity = ticket != null ? ticket.Identity : null;
                if (identity == null)
                {
                    authentication.Challenge("Application");
                }
                ClaimsIdentity claimsIdentity = new ClaimsIdentity("Bearer");
                context.OwinContext.Authentication.SignIn(claimsIdentity);

                //This is the last chance to alter the request, you can either end it here using RequestCompleted and start resonding, 
                //or you can let it go through to the subsequent middleware, 
                //except that you have to make sure the response returns a 200, otherwise the whole thing will not work
                context.RequestCompleted();
                var str = context.Options.AccessTokenFormat;

            });
        }

        public static void CreateAuthorizationCode(AuthenticationTokenCreateContext context)
        {
            CASPaymentDAO.DataHandler.AuthorizationCodeDataHandler authorizationCodeHandler = new CASPaymentDAO.DataHandler.AuthorizationCodeDataHandler(WebApiApplication.SessionFactory);
            CASPaymentDAO.DataHandler.UsersDataHandler usersHandler = new CASPaymentDAO.DataHandler.UsersDataHandler(WebApiApplication.SessionFactory);            
            CASPaymentDAO.DataHandler.ApplicationDataHandler applicationHandler = new CASPaymentDAO.DataHandler.ApplicationDataHandler(WebApiApplication.SessionFactory);
            //create a token
            context.SetToken(Guid.NewGuid().ToString("n") + Guid.NewGuid().ToString("n") + Thread.CurrentPrincipal.Identity.Name);
            //_authenticationCodes[context.Token] = context.SerializeTicket();
            CASPaymentDTO.Domain.AuthorizationCode authorizationCode = new CASPaymentDTO.Domain.AuthorizationCode();

            if (context.Ticket.Identity.Name != null)
            {
                CASPaymentDTO.Domain.Users user = usersHandler.Search(new CASPaymentDTO.Domain.Users { Email = context.Ticket.Identity.Name }).Cast<CASPaymentDTO.Domain.Users>().FirstOrDefault();
                if (!object.Equals(user, default(CASPaymentDTO.Domain.Users)))
                {
                    authorizationCode.UsersItem = user;
                }
            }

            if (context.Ticket.Properties.Dictionary["client_id"] != null)
            {
                CASPaymentDTO.Domain.Application application = applicationHandler.Search(new CASPaymentDTO.Domain.Application { ClientID = context.Ticket.Properties.Dictionary["client_id"] }).Cast<CASPaymentDTO.Domain.Application>().FirstOrDefault();
                if (!object.Equals(application, default(CASPaymentDTO.Domain.Application)))
                {
                    authorizationCode.ApplicationItem = application;
                }
            }
            //remember to search ticket with token Item
            authorizationCode.Tokencontent = context.Token;
            authorizationCode.Ticketcontent = context.SerializeTicket();
            authorizationCode.Used = false;
            authorizationCodeHandler.Save(authorizationCode);

        }


        public static void ReceiveAuthorizationCode(AuthenticationTokenReceiveContext context)
        {
            CASPaymentDAO.DataHandler.AuthorizationCodeDataHandler authorizationCodeHandler = new CASPaymentDAO.DataHandler.AuthorizationCodeDataHandler(WebApiApplication.SessionFactory);
            CASPaymentDTO.Domain.AuthorizationCode authorizationCode = authorizationCodeHandler.Search(new CASPaymentDTO.Domain.AuthorizationCode { Tokencontent = context.Token }).Cast<CASPaymentDTO.Domain.AuthorizationCode>().FirstOrDefault();
            if (!object.Equals(authorizationCode, default(CASPaymentDTO.Domain.AuthorizationCode)))
            {
                if (!authorizationCode.Used)
                {
                    authorizationCode.Used = true;
                    authorizationCodeHandler.Update(authorizationCode);
                    context.DeserializeTicket(authorizationCode.Ticketcontent);
                }
            }
        }


        #endregion

	}
}