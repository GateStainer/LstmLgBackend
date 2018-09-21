using Hangfire;
using Microsoft.Owin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;

namespace LstmLgBackend
{
    public class WebApiApplication : System.Web.HttpApplication
    {
        
        protected void Application_Start()
        {
            System.Web.Http.GlobalConfiguration.Configuration.Formatters.JsonFormatter.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
            System.Web.Http.GlobalConfiguration.Configuration.Formatters.Remove(System.Web.Http.GlobalConfiguration.Configuration.Formatters.XmlFormatter);
            AreaRegistration.RegisterAllAreas();
            System.Web.Http.GlobalConfiguration.Configure(WebApiConfig.Register);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);

            //Add cntk path
            string pathValue = Environment.GetEnvironmentVariable("PATH");
            string domainBaseDir = AppDomain.CurrentDomain.BaseDirectory;
            string cntkPath = domainBaseDir + @"bin\";
            pathValue += ";" + cntkPath;
            Environment.SetEnvironmentVariable("PATH", pathValue);

            HangFireInit.Application_Start();
        }
        protected void Application_End()
        {
            HangFireInit.Application_End();
        }

    }

    public class HangFireInit
    {
        private static BackgroundJobServer _backgroundJobServer;

        public static void Application_Start()
        {
            Hangfire.GlobalConfiguration.Configuration.UseSqlServerStorage("LstmLgBackendContext");

            var options = new BackgroundJobServerOptions
            {
                WorkerCount = Environment.ProcessorCount * 10
            };

            _backgroundJobServer = new BackgroundJobServer(options);

            var recurringJobManager = new RecurringJobManager();
        }

        public static void Application_End()
        {
            _backgroundJobServer.Dispose();
        }
    }
}
