using Microsoft.Owin;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.Infrastructure;
using Microsoft.Owin.Security.OAuth;
using Owin;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Security.Principal;
using System.Web.Mvc;
using System.Threading;
using Microsoft.Owin.Security;

namespace APIRestPayment
{
    public partial class Startup
    {

        private CASPaymentDAO.DataHandler.ApplicationDataHandler applicationHandler;
        private CASPaymentDAO.DataHandler.AccessTokenDataHandler accessTokenHandler;
        private CASPaymentDAO.DataHandler.AuthorizationCodeDataHandler authorizationCodeHandler;
        private CASPaymentDAO.DataHandler.UsersDataHandler usersHandler;

        // For more information on configuring authentication, please visit http://go.microsoft.com/fwlink/?LinkId=301864
        [Authorize]
        public void ConfigureAuth(IAppBuilder app)
        {

            //Enable CORS
            app.UseCors(Microsoft.Owin.Cors.CorsOptions.AllowAll);


            //// Enable the application to use a cookie to store information for the signed in user
            // Enable the Application Sign In Cookie.
            app.UseCookieAuthentication(new CookieAuthenticationOptions
            {
                AuthenticationType = "Application",
                AuthenticationMode = AuthenticationMode.Passive,
                LoginPath = new PathString("/Member/Login"),
                LogoutPath = new PathString("/Member/Logout"),
            });

            //// Enable the External Sign In Cookie.
            app.SetDefaultSignInAsAuthenticationType("Application");
            //app.UseCookieAuthentication(new CookieAuthenticationOptions
            //{
            //    AuthenticationType = "External",
            //    AuthenticationMode = AuthenticationMode.Passive,
            //    CookieName = CookieAuthenticationDefaults.CookiePrefix + "External",
            //    ExpireTimeSpan = TimeSpan.FromMinutes(5),
            //});





            #region ServerOAuth

            app.UseOAuthBearerAuthentication(new Microsoft.Owin.Security.OAuth.OAuthBearerAuthenticationOptions
                ()
                {
                    AuthenticationMode = Microsoft.Owin.Security.AuthenticationMode.Active,
                    AuthenticationType = "Bearer",
                    Realm = "EHM" //anything
                });

            var options = new Microsoft.Owin.Security.OAuth.OAuthAuthorizationServerOptions();
            options.TokenEndpointPath = new PathString("/Oauth/Token");
            options.AuthorizeEndpointPath = new PathString("/Oauth/Authorize");
            options.AllowInsecureHttp = true; //Don't do that!! always be on secure scheme, this is set to "true" for demo purposes
            options.ApplicationCanDisplayErrors = true;
            options.AccessTokenExpireTimeSpan = TimeSpan.FromMinutes(30);
            options.AuthorizationCodeExpireTimeSpan = TimeSpan.FromMinutes(30);
            var provider = new OAuthAuthorizationServerProvider();

            provider.OnValidateClientRedirectUri = Controllers.OauthController.ValidateRedirectUri;
            provider.OnValidateAuthorizeRequest =Controllers.OauthController.ValidateRequest;
            //provider.OnValidateAuthorizeRequest = (context) =>
            //    {
            //        return Task.Run(() =>
            //        {
            //            //Authorization validation here
            //            //Somewhere in the request you should create the identity and sign in with it, I put it here, it could be a page on your app?
            //            context.Validated();
            //        });
            //    };

            //provider.OnAuthorizeEndpoint = (context) =>
            //{
            //    return Task.Run(() =>
            //    {
            //        var authentication = context.OwinContext.Authentication;
            //        var ticket = authentication.AuthenticateAsync("Application").Result;
            //        var identity = ticket != null ? ticket.Identity : null;
            //        if (identity == null)
            //        {
            //            authentication.Challenge("Application");
            //        }
                    
            //        //ClaimsIdentity claimsIdentity = new ClaimsIdentity("Bearer");
            //        //context.OwinContext.Authentication.SignIn(claimsIdentity);

            //        //identity.AddClaim(new Claim(ClaimTypes.Country , "Iran"));
            //            //This is the last chance to alter the request, you can either end it here using RequestCompleted and start resonding, 
            //            //or you can let it go through to the subsequent middleware, 
            //            //except that you have to make sure the response returns a 200, otherwise the whole thing will not work
            //            context.RequestCompleted();
            //            var str = context.Options.AccessTokenFormat;

            //    });
            //};


            provider.OnValidateClientAuthentication = (context) =>
            {
                string clientId;
                string clientSecret;
                return Task.Run(() =>
                    {
                        if (context.TryGetBasicCredentials(out clientId, out clientSecret) ||
                            context.TryGetFormCredentials(out clientId, out clientSecret))
                        {
                            CASPaymentDTO.Domain.Application applicationPOCO = (this.applicationHandler.Search(new CASPaymentDTO.Domain.Application() { ClientID = clientId })).Cast<CASPaymentDTO.Domain.Application>().FirstOrDefault();
                            if (!object.Equals(applicationPOCO, default(CASPaymentDTO.Domain.Application)))
                            {
                                //TODO check if we must use Secret hash() instead of clientSecret.
                                if (clientId == applicationPOCO.ClientID && clientSecret == applicationPOCO.Secrethash)
                                {
                                    context.Validated(clientId);
                                }
                            }
                        }
                    });
                //return Task.Run(() =>
                //{
                //    //Client validation here
                //    context.Validated(context.ClientId);
                //});
            };

            provider.OnGrantResourceOwnerCredentials = (context) =>
            {
                var identity = new ClaimsIdentity(new GenericIdentity(context.UserName, OAuthDefaults.AuthenticationType), context.Scope.Select(x => new Claim("urn:oauth:scope", x)));
                context.Validated(identity);

                return Task.FromResult(0);
            };

            provider.OnGrantClientCredentials = (context) =>
            {
                var identity = new ClaimsIdentity(new GenericIdentity(context.ClientId, OAuthDefaults.AuthenticationType), context.Scope.Select(x => new Claim("urn:oauth:scope", x)));
                context.Validated(identity);
                return Task.FromResult(0);
            };

            options.Provider = provider;

            options.AuthorizationCodeProvider = new AuthenticationTokenProvider
            {
                //OnCreate = CreateAuthorizationCode,
                //OnReceive = ReceiveAuthorizationCode,
                OnCreate = Controllers.OauthController.CreateAuthorizationCode,
                OnReceive = Controllers.OauthController.ReceiveAuthorizationCode,
            };


            options.AccessTokenProvider = new AuthenticationTokenProvider
            {
                OnCreate = CreateAccessToken,
                OnReceive = ReceiveAccessToken,
            };
            options.RefreshTokenProvider = new AuthenticationTokenProvider
            {
                OnCreate = CreateRefreshToken,
                OnReceive = ReceiveRefreshToken,
            };
            app.UseOAuthBearerTokens(options);

            #endregion

            app.UseGoogleAuthentication();
        }

        #region AuthorizationCode Provider

        private void ReceiveAuthorizationCode(AuthenticationTokenReceiveContext context)
        {
            authorizationCodeHandler = new CASPaymentDAO.DataHandler.AuthorizationCodeDataHandler(WebApiApplication.SessionFactory);
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

        private void CreateAuthorizationCode(AuthenticationTokenCreateContext context)
        {
            usersHandler = new CASPaymentDAO.DataHandler.UsersDataHandler(WebApiApplication.SessionFactory);
            authorizationCodeHandler = new CASPaymentDAO.DataHandler.AuthorizationCodeDataHandler(WebApiApplication.SessionFactory);
            applicationHandler = new CASPaymentDAO.DataHandler.ApplicationDataHandler(WebApiApplication.SessionFactory);
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

        #endregion

        #region AccessToken Provider

        private void ReceiveAccessToken(AuthenticationTokenReceiveContext obj)
        {
            //This is called when a client is requesting with Authorization header and passing the token, like this "Authorization: Bearer jdksjkld"
            throw new NotImplementedException();
        }

        private void CreateAccessToken(AuthenticationTokenCreateContext context)
        {
            usersHandler = new CASPaymentDAO.DataHandler.UsersDataHandler(WebApiApplication.SessionFactory);
            applicationHandler = new CASPaymentDAO.DataHandler.ApplicationDataHandler(WebApiApplication.SessionFactory);

            //create a token
            context.SetToken(Guid.NewGuid().ToString("n") + Guid.NewGuid().ToString("n") + Thread.CurrentPrincipal.Identity.Name);

            CASPaymentDTO.Domain.AccessToken accessToken;

            accessToken = context.OwinContext.Get<CASPaymentDTO.Domain.AccessToken>("RefreshAccessTokenContent");
            if (object.Equals(accessToken, default(CASPaymentDTO.Domain.AccessToken)))
            {
                accessToken = new CASPaymentDTO.Domain.AccessToken();
                if (context.Ticket.Identity != null)
                {
                    CASPaymentDTO.Domain.Users user = usersHandler.Search(new CASPaymentDTO.Domain.Users { Email = context.Ticket.Identity.Name }).Cast<CASPaymentDTO.Domain.Users>().FirstOrDefault();
                    if (!object.Equals(user, default(CASPaymentDTO.Domain.Users)))
                    {
                        accessToken.UsersItem = user;
                    }
                }
                if (context.Ticket.Properties.Dictionary["client_id"] != null)
                {
                    CASPaymentDTO.Domain.Application application = applicationHandler.Search(new CASPaymentDTO.Domain.Application { ClientID = context.Ticket.Properties.Dictionary["client_id"] }).Cast<CASPaymentDTO.Domain.Application>().FirstOrDefault();
                    if (!object.Equals(application, default(CASPaymentDTO.Domain.Application)))
                    {
                        accessToken.ApplicationItem = application;
                    }
                }
            }
            //TODO Read the expiry timeSpan from DB
            accessToken.Expirydate = DateTime.UtcNow.Add(TimeSpan.FromMinutes(30));
            accessToken.TokenContent = context.Token;
            accessToken.IsRevoked = false;
            context.OwinContext.Set<CASPaymentDTO.Domain.AccessToken>("AccessTokenContent", accessToken);
        }


        #endregion

        #region RefreshToken Provider

        private void ReceiveRefreshToken(AuthenticationTokenReceiveContext context)
        {
            accessTokenHandler = new CASPaymentDAO.DataHandler.AccessTokenDataHandler(WebApiApplication.SessionFactory);
            CASPaymentDTO.Domain.AccessToken accessToken = accessTokenHandler.Search(new CASPaymentDTO.Domain.AccessToken { RefreshToken = context.Token }).Cast<CASPaymentDTO.Domain.AccessToken>().FirstOrDefault();
            if (!object.Equals(accessToken, default(CASPaymentDTO.Domain.AccessToken)))
            {
                if (!accessToken.IsRevoked)
                {
                    context.DeserializeTicket(context.Token);
                    context.OwinContext.Set<CASPaymentDTO.Domain.AccessToken>("RefreshAccessTokenContent", accessToken);
                }
            }
        }

        private void CreateRefreshToken(AuthenticationTokenCreateContext context)
        {
            accessTokenHandler = new CASPaymentDAO.DataHandler.AccessTokenDataHandler(WebApiApplication.SessionFactory);
            //Expiration time in seconds
            int expire = 3 * 60 * 60;
            context.Ticket.Properties.ExpiresUtc = new DateTimeOffset(DateTime.UtcNow.AddSeconds(expire));
            context.SetToken(context.SerializeTicket());
            CASPaymentDTO.Domain.AccessToken accessToken = context.OwinContext.Get<CASPaymentDTO.Domain.AccessToken>("AccessTokenContent");
            if (!object.Equals(accessToken, default(CASPaymentDTO.Domain.AccessToken)))
            {
                accessToken.RefreshToken = context.Token;

                if (context.OwinContext.Environment.ContainsKey("RefreshAccessTokenContent"))
                {
                    context.OwinContext.Environment.Remove("RefreshAccessTokenContent");
                    accessTokenHandler.Update(accessToken);
                }
                else
                {
                    accessTokenHandler.Save(accessToken);
                }
                context.OwinContext.Environment.Remove("AccessTokenContent");
            }
        }

        #endregion
    }
}
