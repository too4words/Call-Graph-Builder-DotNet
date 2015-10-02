using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Diagnostics;
using Microsoft.WindowsAzure.ServiceRuntime;
using System.Diagnostics;

namespace WebAPI
{
    public class WebRole : RoleEntryPoint
    {
        public override bool OnStart()
        {
            Trace.WriteLine("WebAPI-OnStart");

			// For information on handling configuration changes
			// see the MSDN topic at http://go.microsoft.com/fwlink/?LinkId=166357.
			RoleEnvironment.Changing += RoleEnvironmentChanging;

			var ok = base.OnStart();

			Trace.WriteLine("WebAPI-OnStart completed with OK=" + ok);

			return ok;
        }

        public override void OnStop()
        {
            Trace.WriteLine("WebAPI-OnStop");
            base.OnStop();
        }

        public override void Run()
        {
            Trace.WriteLine("WebAPI-Run");
            try
            {
                base.Run();
            }
            catch (Exception exc)
            {
                Trace.WriteLine("Run() failed with " + exc.ToString());
            }
        }

        private void RoleEnvironmentChanging(object sender, RoleEnvironmentChangingEventArgs e)
        {
            foreach (RoleEnvironmentConfigurationSettingChange settingChange in e.Changes.Where(x => x is RoleEnvironmentTopologyChange))
            {
                e.Cancel = true;
                return;
            }
            // If a configuration setting is changing
            //if (e.Changes.Any(change => change is RoleEnvironmentConfigurationSettingChange))
            //{
            //    // Set e.Cancel to true to restart this role instance
            //    e.Cancel = true;
            //}
        }
    }
}
