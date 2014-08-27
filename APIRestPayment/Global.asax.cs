﻿using Newtonsoft.Json;
using NHibernate;
using NHibernate.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;

namespace APIRestPayment
{
    // Note: For instructions on enabling IIS6 or IIS7 classic mode, 
    // visit http://go.microsoft.com/?LinkId=9394801

    public class WebApiApplication : System.Web.HttpApplication
    {
        private static object syncRoot = new Object();

        public static ISessionFactory SessionFactory
        {
            get;
            private set;
        }

        protected void Application_Start(object sender, EventArgs e)
        {   
            
            //force Https

            
            //////// to generate json response
            GlobalConfiguration.Configuration.Formatters.Clear();
            GlobalConfiguration.Configuration.Formatters.Add(new System.Net.Http.Formatting.JsonMediaTypeFormatter());

            ////solve self referencing problem
            GlobalConfiguration.Configuration.Formatters.JsonFormatter.SerializerSettings.Re‌ferenceLoopHandling = ReferenceLoopHandling.Ignore;

            //omit null values globally in response
            GlobalConfiguration.Configuration.Formatters.JsonFormatter.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
            
            ///default settings
            AreaRegistration.RegisterAllAreas();
                        
            WebApiConfig.Register(GlobalConfiguration.Configuration); 
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
                         
            /// congigure NHibernate
            var nhConfig = new NHibernate.Cfg.Configuration().Configure();
            SessionFactory = nhConfig.BuildSessionFactory();  
        }

        protected void Session_Start(object sender, EventArgs e)
        {

        }

        protected void Application_BeginRequest(object sender, EventArgs e)
        {
            lock (syncRoot)
            {
                var session = SessionFactory.OpenSession();
                CurrentSessionContext.Bind(session);
            }
        }

        protected void Application_AuthenticateRequest(object sender, EventArgs e)
        {

        }

        protected void Application_Error(object sender, EventArgs e)
        {

        }

        protected void Session_End(object sender, EventArgs e)
        {

        }

        protected void Application_End(object sender, EventArgs e)
        {

        }

        protected void Application_EndRequest(object sender, EventArgs e)
        {
            var session = CurrentSessionContext.Unbind(SessionFactory);
            session.Dispose();
        }
        public static void RegisterRoute(RouteCollection routes)
        {
            routes.MapPageRoute("LoginID", "login", "~/PaymentPages/SalesValidation");
        }
    }
}