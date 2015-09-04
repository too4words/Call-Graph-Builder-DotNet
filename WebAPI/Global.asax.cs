﻿using Microsoft.WindowsAzure.ServiceRuntime;
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
    public class Global : HttpApplication
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
			if (!RoleEnvironment.IsEmulated)
			{
				if (!AzureClient.IsInitialized)
				{
					FileInfo clientConfigFile = AzureConfigUtils.ClientConfigFileLocation;
					if (!clientConfigFile.Exists)
					{
						throw new FileNotFoundException(string.Format("Cannot find Orleans client config file for initialization at {0}", clientConfigFile.FullName), clientConfigFile.FullName);
					}
					AzureClient.Initialize(clientConfigFile);
				}
			}
			else
			{
				GrainClient.Initialize(Server.MapPath(@"~/LocalConfiguration.xml"));
			}
		}
    }
}