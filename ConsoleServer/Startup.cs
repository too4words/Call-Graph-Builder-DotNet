using Owin;
using System.Web.Http; 

namespace ConsoleServer
{
    public class Startup
    {
        // This code configures Web API. The Startup class is specified as a type
        // parameter in the WebApp.Start method.
        public void Configuration(IAppBuilder appBuilder)
        {
            // Configure Web API for self-host. 
            var config = new HttpConfiguration();

            config.MapHttpAttributeRoutes();

            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );

            appBuilder.Use((context,next) =>
            {
                context.Response.Headers.Add("Access-Control-Allow-Origin", new[] { "*" }); // alt: http://localhost:14054
                return next.Invoke();
            });

            appBuilder.UseWebApi(config);
        }
    } 

}
