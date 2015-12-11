using System;
using System.Net;
using Orleans.Runtime.Host;

namespace ConsoleServer
{
    internal class OrleansHostWrapper : IDisposable
    {
		private SiloHost siloHost;

        public OrleansHostWrapper()
        {
        }

		public bool Debug
		{
			get { return siloHost != null && siloHost.Debug; }
			set { siloHost.Debug = value; }
		}

		public void Init()
		{
			var configFileName = "OrleansConfigurationForTesting.xml";
			var siloName = Dns.GetHostName(); // Default to machine name

			siloHost = new SiloHost(siloName);
			siloHost.ConfigFileName = configFileName;
			siloHost.LoadOrleansConfig();
		}

        public bool Run()
        {
            var ok = false;

            try
            {
                siloHost.InitializeOrleansSilo();
                ok = siloHost.StartOrleansSilo();

                if (ok)
                {
                    Console.WriteLine("OrleansHostWrapper", "Run", "Successfully started Orleans silo '{0}' as a {1} node.", siloHost.Name, siloHost.Type);
                }
                else
                {
					var message = string.Format("Failed to start Orleans silo '{0}' as a {1} node.", siloHost.Name, siloHost.Type);
					throw new SystemException(message);
                }
            }
            catch (Exception ex)
            {
                siloHost.ReportStartupError(ex);
				Console.WriteLine("OrleansHostWrapper", "Run", "{0}:\n{1}\n{2}", ex.GetType().FullName, ex.Message, ex.StackTrace);
            }

            return ok;
        }

        public bool Stop()
        {
            var ok = false;

            try
            {
                siloHost.StopOrleansSilo();

				Console.WriteLine("OrleansHostWrapper", "Stop", "Orleans silo '{0}' shutdown.", siloHost.Name);
            }
            catch (Exception ex)
            {
                siloHost.ReportStartupError(ex);
				Console.WriteLine("OrleansHostWrapper", "Stop", "{0}:\n{1}\n{2}", ex.GetType().FullName, ex.Message, ex.StackTrace);
            }

            return ok;
        }

        public void Dispose()
        {
            this.Dispose(true);
        }

        protected virtual void Dispose(bool dispose)
        {
            siloHost.Dispose();
            siloHost = null;
        }
    }
}