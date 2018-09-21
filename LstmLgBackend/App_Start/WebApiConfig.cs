using LstmLgBackend.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;
using System.Web.Http.ExceptionHandling;

namespace LstmLgBackend
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            // Web API configuration and services
            config.Services.Replace(typeof(IExceptionLogger), new UnhandledExceptionLogger());

            // Web API routes
            config.MapHttpAttributeRoutes();

            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );
        }
    }

    public class UnhandledExceptionLogger : ExceptionLogger
    {
        public override void Log(ExceptionLoggerContext context)
        {
            LstmLgBackendContext db = new LstmLgBackendContext();
            var path = System.Web.Hosting.HostingEnvironment.MapPath("~");
            var log_str = context.Exception.ToString();
            Log log = new Log();
            log.log = log_str;
            log.timestamp = System.DateTime.Now;
            db.Logs.Add(log);
            db.SaveChanges();

            //Do whatever logging you need to do here.
        }
    }


}
