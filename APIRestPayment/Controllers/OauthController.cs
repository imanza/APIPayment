using Microsoft.Owin.Security.OAuth;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace APIRestPayment.Controllers
{
    public class OauthController : Controller
    {
        public ActionResult Authorize()
        {

            #region Code Behind

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

        #endregion

	}
}