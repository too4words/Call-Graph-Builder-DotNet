using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(WebAPI.Startup))]
namespace WebAPI
{
    public partial class Startup {
        public void Configuration(IAppBuilder app) {
            ConfigureAuth(app);
        }
    }
}
