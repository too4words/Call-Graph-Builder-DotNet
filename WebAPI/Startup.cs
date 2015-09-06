using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(WebAPI.WebAPIStartup))]
namespace WebAPI
{
    public partial class WebAPIStartup
	{
        public void Configuration(IAppBuilder app) {
            ConfigureAuth(app);
        }
    }
}
