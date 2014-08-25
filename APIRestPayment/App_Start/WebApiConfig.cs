using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Formatting;
using System.Web.Http;

namespace APIRestPayment
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {


            config.Routes.MapHttpRoute(
            name: "Payments",
            routeTemplate: "api/payments/{id}",
            defaults: new { controller = "payments", id = RouteParameter.Optional }
            );

            config.Routes.MapHttpRoute(
            name: "Users",
            routeTemplate: "api/users/{id}",
            defaults: new { controller = "users", id = RouteParameter.Optional }
            );
             
            config.Routes.MapHttpRoute(
            name: "Accounts",
            routeTemplate: "api/accounts/{id}",
            defaults: new { controller = "accounts", id = RouteParameter.Optional }
            );

            config.Routes.MapHttpRoute(
            name: "AccountOwnership",
            routeTemplate: "api/users/{usersId}/accounts/{accountId}",
            defaults: new { controller = "AccountOwnership", accountId = RouteParameter.Optional }
            );

            config.Routes.MapHttpRoute(
            name: "AccountTransactions",
            routeTemplate: "api/accounts/{accountsId}/{inout}/payments/{paymentsId}",
            defaults: new { controller = "AccountTransactions",inout = RouteParameter.Optional, paymentsId = RouteParameter.Optional }
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
