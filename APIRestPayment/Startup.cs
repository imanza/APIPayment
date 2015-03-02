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
using System.Web.Http;
using Newtonsoft.Json;
using System.Web.Routing;
using System.Web.Optimization;
using WebApiThrottle;
using System.Collections.Generic;

[assembly: OwinStartup(typeof(APIRestPayment.Startup))]
namespace APIRestPayment
{
    public class Startup
    {
        public static int yyy = 2;
        private CASPaymentDAO.DataHandler.ApplicationDataHandler applicationHandler;
        private CASPaymentDAO.DataHandler.AccessTokenDataHandler accessTokenHandler;
        private CASPaymentDAO.DataHandler.AuthorizationCodeDataHandler authorizationCodeHandler;
        private CASPaymentDAO.DataHandler.UsersDataHandler usersHandler;

        public void Configuration(IAppBuilder app)
        {
            HttpConfiguration config = new HttpConfiguration();

            
            ///default settings
            AreaRegistration.RegisterAllAreas();

            //WebApiConfig.Register(config); 
            GlobalConfiguration.Configure(WebApiConfig.Register);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            //RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);

            ConfigureOAuth(app);
            app.UseCors(Microsoft.Owin.Cors.CorsOptions.AllowAll);
            app.UseWebApi(config);
            app.UseCookieAuthentication(new CookieAuthenticationOptions
            {
                AuthenticationType = "Application",
                AuthenticationMode = AuthenticationMode.Passive,
                LoginPath = new PathString("/Member/Login"),
                LogoutPath = new PathString("/Member/Logout"),
            });
            
            yyy++;

        }


        private void ConfigureOAuth(IAppBuilder app)
        {
            //Token Consumption
            app.UseOAuthBearerAuthentication(new OAuthBearerAuthenticationOptions()
            {
                AuthenticationType = "Bearer",
                Realm = "EHM",
                AuthenticationMode = AuthenticationMode.Active
                
            });
        }
    }
}
