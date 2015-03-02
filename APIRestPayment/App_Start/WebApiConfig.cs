using APIRestPayment.App_Start.MessageHandlers;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Formatting;
using System.Web.Http;
using WebApiThrottle;

namespace APIRestPayment
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            //config.MessageHandlers.Add(new MACMessageHandler());
            //config.MessageHandlers.Add(new AjaxRequestMessageHandler());            

            //to make the json response camelCase
            //config.Formatters.JsonFormatter.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();

            //force Https
            //FilterConfig.RegisterHttpFilters(config.Filters);

            //////// to generate json response
            config.Formatters.Clear();
            config.Formatters.Add(new System.Net.Http.Formatting.JsonMediaTypeFormatter());

            ////solve self referencing problem
            config.Formatters.JsonFormatter.SerializerSettings.Re‌ferenceLoopHandling = ReferenceLoopHandling.Ignore;

            //omit null values globally in response
            config.Formatters.JsonFormatter.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;

            //Limit Requests by their IP and Client-Key 
            config.MessageHandlers.Add(new ThrottlingHandler()
            {
                Policy = new ThrottlePolicy(perMinute: 40, perHour: 2000)
                {
                    IpThrottling = true,
                    ClientThrottling = true,
                    EndpointThrottling = true,
                    //TODO
                    IpWhitelist = new List<string> { "::1", "127.0.0.1", Constants.Paths.OAuthServerPath },
                },
                Repository = new CacheRepository()
            });

            ////Enable Cross Origin Request
            //config.EnableCors();
            


            ////Enable Attribute Routing
            config.MapHttpAttributeRoutes();



            //config.Routes.MapHttpRoute(
            //name: "AccountOwnership",
            //routeTemplate: "api/users/{usersId}/accounts/{accountId}",
            //defaults: new { controller = "AccountOwnership", accountId = RouteParameter.Optional }
            //);

            //config.Routes.MapHttpRoute(
            //name: "AccountTransactions",
            //routeTemplate: "api/accounts/{accountsId}/{inout}/payments/{paymentsId}",
            //defaults: new { controller = "AccountTransactions", inout = RouteParameter.Optional, paymentsId = RouteParameter.Optional }
            //);
            
            

            //config.Routes.MapHttpRoute(
            //name: "Jalda",
            //routeTemplate: "api/jalda/{contractID}",
            //defaults: new { controller = "Jalda", contractID = RouteParameter.Optional }
            //);

            //config.Routes.MapHttpRoute(
            //name: "Payments",
            //routeTemplate: "api/payments/{id}",
            //defaults: new { controller = "payments", id = RouteParameter.Optional }
            //);
            

            //config.Routes.MapHttpRoute(
            //name: "Users",
            //routeTemplate: "api/users/{id}",
            //defaults: new { controller = "users", id = RouteParameter.Optional }
            //);

            //config.Routes.MapHttpRoute(
            //name: "Accounts",
            //routeTemplate: "api/accounts/{id}",
            //defaults: new { controller = "accounts", id = RouteParameter.Optional }
            //);

            //config.Routes.MapHttpRoute(
            //name: "PaymentsCheck",
            //routeTemplate: "api/{controller}/{action}",
            //defaults: new { controller = "payments", action = "check"  }
            //);


            config.Routes.MapHttpRoute(
            name: "Apiwithaction",
            routeTemplate: "api/{controller}/{action}/{id}",
            defaults: new { id = RouteParameter.Optional }
            );

            config.Routes.MapHttpRoute(
            name: "DefaultApi",
            routeTemplate: "api/{controller}/{id}",
            defaults: new { id = RouteParameter.Optional }
            );
            // Uncomment the following line of code to enable query support for actions with an IQueryable or IQueryable<T> return type.
            // To avoid processing unexpected or malicious queries, use the validation settings on QueryableAttribute to validate incoming queries.
            // For more information, visit http://go.microsoft.com/fwlink/?LinkId=279712.
            //config.EnableQuerySupport();

            // To disable tracing in your application, please comment out or remove the following line of code
            // For more information, refer to: http://www.asp.net/web-api

            config.EnableSystemDiagnosticsTracing();
        }
    }
}
