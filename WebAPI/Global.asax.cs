using Microsoft.WindowsAzure.ServiceRuntime;
using Orleans;
using Orleans.Runtime.Host;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using System.Web.Security;
using System.Web.SessionState;

namespace WebAPI
{
	public class WebApiApplication : HttpApplication
    {
        void Application_Start(object sender, EventArgs e)
        {
			// Code that runs on application startup
			AreaRegistration.RegisterAllAreas();
			GlobalConfiguration.Configure(WebApiConfig.Register);
			RouteConfig.RegisterRoutes(RouteTable.Routes);

			InitOrleansClient();
        }

		private void InitOrleansClient()
		{
			var ipAddr = RoleEnvironment.CurrentRoleInstance.InstanceEndpoints["EndPoint1"].IPEndpoint.Address.ToString();
			Environment.SetEnvironmentVariable("MyIPAddr", ipAddr);
            Environment.SetEnvironmentVariable("ISORLEANSCLIENT", "True");

            // No longer needed. The emulator handles the same configuration
            //if (!RoleEnvironment.IsEmulated || true)
            //{
				if (!AzureClient.IsInitialized)
				{
					FileInfo clientConfigFile = AzureConfigUtils.ClientConfigFileLocation;
					if (!clientConfigFile.Exists)
					{
						throw new FileNotFoundException(string.Format("Cannot find Orleans client config file for initialization at {0}", clientConfigFile.FullName), clientConfigFile.FullName);
					}
					AzureClient.Initialize(clientConfigFile);
				}
            //}
            //else
            //{
            //    GrainClient.Initialize(Server.MapPath(@"~/LocalConfiguration.xml"));
            //}
		}
    }
}