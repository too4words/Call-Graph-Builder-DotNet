using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(WebRole1.WebRole1Startup))]
namespace WebRole1
{
    public partial class WebRole1Startup
	{
        public void Configuration(IAppBuilder app) {
            ConfigureAuth(app);
        }
    }
}
