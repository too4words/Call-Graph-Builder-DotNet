using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.ServiceRuntime;
using Orleans.Runtime.Host;
using Orleans.Providers;
using Orleans.Runtime.Configuration;
using RedDog.Storage.Files;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure;
using System.IO;

namespace OrleansSilosInAzure
{
    public class WorkerRole : RoleEntryPoint
    {
        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private readonly ManualResetEvent runCompleteEvent = new ManualResetEvent(false);

        private AzureSilo orleansAzureSilo;
        private const string DATA_CONNECTION_STRING_KEY = "DataConnectionString";


        //private AppDomain hostDomain;
        //private static OrleansHostWrapper hostWrapper;


        public override void Run()
        {
			try
			{
				var config = new ClusterConfiguration();
                //config.StandardLoad();
				if (RoleEnvironment.IsEmulated)
				{
					config.LoadFromFile(@"OrleansLocalConfiguration.xml");
				}
				else
				{
					config.LoadFromFile(@"OrleansConfiguration.xml");
				}

					// First example of how to configure an existing provider
                //Example_ConfigureExistingStorageProvider(config);
                //Example_ConfigureNewStorageProvider(config);
                //Example_ConfigureNewBootstrapProvider(config);

				// It is IMPORTANT to start the silo not in OnStart but in Run.
				// Azure may not have the firewalls open yet (on the remote silos) at the OnStart phase.
				orleansAzureSilo = new AzureSilo();
				bool ok = orleansAzureSilo.Start(config);
				if (ok)
				{
					Trace.TraceInformation("OrleansAzureSilos-OnStart Orleans silo started ok=" + ok, "Information");

					orleansAzureSilo.Run(); // Call will block until silo is shutdown

					Trace.TraceInformation("OrleansSilosInAzure is running");
				}
				else
				{
					Trace.TraceError("Orleans Silo could not start");
                    SaveErrorToBlob("Orleans Silo could not start");
				}

			}
			catch(Exception exc)
			{
				while (exc is AggregateException) exc = exc.InnerException;
				Trace.TraceError("Error dutring initialization of WorkerRole {0}",exc.ToString());
                var excString = exc.ToString();

                SaveErrorToBlob(excString);
                
                throw exc;

			}
            //try
            //{
            //    this.RunAsync(this.cancellationTokenSource.Token).Wait();
            //}
            //finally
            //{
            //    this.runCompleteEvent.Set();
            //}
        }

        private void SaveErrorToBlob(string excString)
        {
            using (Stream s = GenerateStreamFromString(excString))
            {
                var container = CloudStorageAccount.Parse(CloudConfigurationManager.GetSetting("DataConnectionString"))
                            .CreateCloudBlobClient().GetContainerReference("errors");
                container.CreateIfNotExists();
                var reference = container.GetBlobReferenceFromServer(string.Format("error-{0}-{1}",
                        RoleEnvironment.CurrentRoleInstance.Id, DateTime.UtcNow.Ticks));
                reference.UploadFromStream(s);
            }
        }

        private Stream GenerateStreamFromString(string s)
        {
            MemoryStream stream = new MemoryStream();
            StreamWriter writer = new StreamWriter(stream);
            writer.Write(s);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }

        // Storage Provider is already configured in the OrleansConfiguration.xml as:
        // <Provider Type="Orleans.Storage.AzureTableStorage" Name="AzureStore" DataConnectionString="UseDevelopmentStorage=true" />
        // Below is an example of how to set the storage key in the ProviderConfiguration and how to add a new custom configuration property.
        private void Example_ConfigureExistingStorageProvider(ClusterConfiguration config)
        {
            IProviderConfiguration storageProvider = null;

            const string myProviderFullTypeName = "Orleans.Storage.AzureTableStorage"; // Alternatively, can be something like typeof(AzureTableStorage).FullName
            const string myProviderName = "AzureStore"; // what ever arbitrary name you want to give to your provider
            if (config.Globals.TryGetProviderConfiguration(myProviderFullTypeName, myProviderName, out storageProvider))
            {
                // provider configuration already exists, modify it.
                string connectionString = RoleEnvironment.GetConfigurationSettingValue(DATA_CONNECTION_STRING_KEY);
                storageProvider.SetProperty(DATA_CONNECTION_STRING_KEY, connectionString);
                storageProvider.SetProperty("MyCustomProperty1", "MyCustomPropertyValue1");
            }
            else
            {
                // provider configuration does not exists, add a new one.
                var properties = new Dictionary<string, string>();
                string connectionString = RoleEnvironment.GetConfigurationSettingValue(DATA_CONNECTION_STRING_KEY);
                properties.Add(DATA_CONNECTION_STRING_KEY, connectionString);
                properties.Add("MyCustomProperty2", "MyCustomPropertyValue2");

                config.Globals.RegisterStorageProvider(myProviderFullTypeName, myProviderName, properties);
            }

            // Alternatively, find all storage providers and modify them as necessary
            foreach (IProviderConfiguration providerConfig in config.Globals.GetAllProviderConfigurations())//storageConfiguration.Providers.Values.Where(provider => provider is ProviderConfiguration).Cast<ProviderConfiguration>())
            {
                if (providerConfig.Type.Equals(myProviderFullTypeName))
                {
                    string connectionString = RoleEnvironment.GetConfigurationSettingValue(DATA_CONNECTION_STRING_KEY);
                    providerConfig.SetProperty(DATA_CONNECTION_STRING_KEY, connectionString);
                    providerConfig.SetProperty("MyCustomProperty3", "MyCustomPropertyValue3");
                }
            }

            // Once silo starts you can see that it prints in the log:
            //   Providers:
            //      StorageProviders:
            //          Name=AzureStore, Type=Orleans.Storage.AzureTableStorage, Properties=[DataConnectionString, MyCustomProperty, MyCustomProperty1, MyCustomProperty3]
        }

        // Below is an example of how to define a full configuration for a new storage provider that is not already specified in the config file.
        private void Example_ConfigureNewStorageProvider(ClusterConfiguration config)
        {
            const string myProviderFullTypeName = "Orleans.Storage.AzureTableStorage"; // Alternatively, can be something like typeof(AzureTableStorage).FullName
            const string myProviderName = "MyNewAzureStoreProvider"; // what ever arbitrary name you want to give to your provider

            var properties = new Dictionary<string, string>();
            string connectionString = RoleEnvironment.GetConfigurationSettingValue(DATA_CONNECTION_STRING_KEY);
            properties.Add(DATA_CONNECTION_STRING_KEY, connectionString);
            properties.Add("MyCustomProperty4", "MyCustomPropertyValue4");

            config.Globals.RegisterStorageProvider(myProviderFullTypeName, myProviderName, properties);

            // Once silo starts you can see that it prints in the log:
            //  Providers:
            //      StorageProviders:
            //          Name=MyNewAzureStoreProvider, Type=Orleans.Storage.AzureTableStorage, Properties=[DataConnectionString, MyCustomProperty4]
        }

        // Below is an example of how to define a full configuration for a new Bootstrap provider that is not already specified in the config file.
        private void Example_ConfigureNewBootstrapProvider(ClusterConfiguration config)
        {
            //const string myProviderFullTypeName = "FullNameSpace.NewBootstrapProviderType"; // Alternatively, can be something like typeof(EventStoreInitBootstrapProvider).FullName
            //const string myProviderName = "MyNewBootstrapProvider"; // what ever arbitrary name you want to give to your provider
            //var properties = new Dictionary<string, string>();

            //config.Globals.RegisterBootstrapProvider(myProviderFullTypeName, myProviderName, properties);

            // The last line, config.Globals.RegisterBootstrapProvider, is commented out because the assembly with "FullNameSpace.NewBootstrapProviderType" is not added to the project,
            // this the silo will fail to load the new bootstrap provider upon startup.
            // !!!!!!!!!! Provider of type FullNameSpace.NewBootstrapProviderType name MyNewBootstrapProvider was not loaded.
            // Once you add your new provider to the project, uncommnet this line.

            // Once silo starts you can see that it prints in the log:
            // Providers:
            //      BootstrapProviders:
            //          Name=MyNewBootstrapProvider, Type=FullNameSpace.NewBootstrapProviderType, Properties=[]
        }

        public override bool OnStart()
        {
			if (!RoleEnvironment.IsEmulated)
			{
				try
				{
					// Mount a drive.
					FilesMappedDrive.Mount("Y:", @"\\orleansstorage2.file.core.windows.net\solutions", "orleansstorage2",
						"ilzOub7LFk5zQ7drJFkfoxdwN1rritlSWAJ9Vl35g/TG4rZWxCXWNTJV20vZLTL/D2LK065cG8AozDg8CGOKQQ==");
				}
				catch(Exception exc)
				{
					while (exc is AggregateException) exc = exc.InnerException;
					Trace.TraceError("Error trying to mount Azure File {0}", exc.ToString());
				}

			}
			// Unmount a drive.
			//FilesMappedDrive.Unmount("P:");

			// Mount a drive for a CloudFileShare.
			//CloudFileShare share = CloudStorageAccount.Parse(CloudConfigurationManager.GetSetting("StorageConnectionString"))
			//	.CreateCloudFileClient()
			//	.GetShareReference("reports");
			//share.Mount("P:");

			// List drives mapped to an Azure Files share.
			//foreach (var mappedDrive in FilesMappedDrive.GetMountedShares())
			//{
			//	Trace.WriteLine(String.Format("{0} - {1}", mappedDrive.DriveLetter, mappedDrive.Path));
			//}
 

            // Set the maximum number of concurrent connections
            ServicePointManager.DefaultConnectionLimit = 12;

            // For information on handling configuration changes
            // see the MSDN topic at http://go.microsoft.com/fwlink/?LinkId=166357.

			RoleEnvironment.Changing += RoleEnvironmentOnChanging;

            bool result = base.OnStart();

            Trace.TraceInformation("OrleansSilosInAzure has been started");

            return result;
        }


		/// <summary>
		/// This event is called after configuration changes have been submited to Windows Azure but before they have been applied in this instance
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="RoleEnvironmentChangingEventArgs" /> instance containing the event data.</param>
		private void RoleEnvironmentOnChanging(object sender, RoleEnvironmentChangingEventArgs e)
		{
			// Implements the changes after restarting the role instance
			//foreach (RoleEnvironmentConfigurationSettingChange settingChange in e.Changes.Where(x => x is RoleEnvironmentConfigurationSettingChange))
			//{
			//	switch (settingChange.ConfigurationSettingName)
			//	{
			//		case "Startup.ExternalTasksUrl":
			//			Trace.TraceWarning("The specified configuration changes can't be made on a running instance. Recycling...");
			//			e.Cancel = true;
			//			return;
			//		case "Startup.VsInstallDir":
			//			Trace.TraceWarning("The specified configuration changes can't be made on a running instance. Recycling...");
			//			e.Cancel = true;
			//			return;
			//	}
			//}
		}


        public override void OnStop()
        {
            Trace.TraceInformation("OrleansSilosInAzure is stopping");

            this.cancellationTokenSource.Cancel();
            this.runCompleteEvent.WaitOne();

            base.OnStop();

            Trace.TraceInformation("OrleansSilosInAzure has stopped");
        }

        private async Task RunAsync(CancellationToken cancellationToken)
        {
            // TODO: Replace the following with your own logic.
            while (!cancellationToken.IsCancellationRequested)
            {
                Trace.TraceInformation("Working");
                await Task.Delay(1000);
            }
        }

        #region Deprecated
        //private void Initialize()
        //{
        //    Console.WriteLine("Initializing Orleans silo...");

        //    var applicationPath = Environment.CurrentDirectory;

        //    var appDomainSetup = new AppDomainSetup
        //    {
        //        AppDomainInitializer = InitSilo,
        //        ApplicationBase = applicationPath,
        //        ApplicationName = "CallGraphGeneration",
        //        AppDomainInitializerArguments = new string[] { },
        //        ConfigurationFile = "CallGraphGeneration.exe.config"
        //    };

        //    // set up the Orleans silo
        //    hostDomain = AppDomain.CreateDomain("OrleansHost", null, appDomainSetup);

        //    var xmlConfig = "ClientConfigurationForTesting.xml";
        //    Contract.Assert(File.Exists(xmlConfig), "Can't find " + xmlConfig);

        //    GrainClient.Initialize(xmlConfig);
        //    Console.WriteLine("Orleans silo initialized successfully");
        //}
        //        private void Cleanup()
        //{

        //    hostDomain.DoCallBack(ShutdownSilo);
        //}

        //private static void InitSilo(string[] args)
        //{
        //    hostWrapper = new OrleansHostWrapper();
        //    hostWrapper.Init();
        //    var ok = hostWrapper.Run();

        //    if (!ok)
        //    {
        //        Console.WriteLine("Failed to initialize Orleans silo");
        //    }
        //}

        //private static void ShutdownSilo()
        //{
        //    if (hostWrapper != null)
        //    {
        //        hostWrapper.Dispose();
        //        GC.SuppressFinalize(hostWrapper);
        //    }
        //}
        #endregion 
    }
    
    #region Deprecated
    //internal class OrleansHostWrapper : IDisposable
    //{
    //    private SiloHost siloHost;

    //    public OrleansHostWrapper()
    //    {
    //    }

    //    public bool Debug
    //    {
    //        get { return siloHost != null && siloHost.Debug; }
    //        set { siloHost.Debug = value; }
    //    }

    //    public void Init()
    //    {
    //        var configFileName = "OrleansConfigurationForTesting.xml";
    //        var siloName = Dns.GetHostName(); // Default to machine name

    //        siloHost = new SiloHost(siloName);
    //        siloHost.ConfigFileName = configFileName;
    //        siloHost.LoadOrleansConfig();
    //    }

    //    public bool Run()
    //    {
    //        var ok = false;

    //        try
    //        {
    //            siloHost.InitializeOrleansSilo();
    //            ok = siloHost.StartOrleansSilo();

    //            if (ok)
    //            {
    //                Console.WriteLine("OrleansHostWrapper", "Run", "Successfully started Orleans silo '{0}' as a {1} node.", siloHost.Name, siloHost.Type);
    //            }
    //            else
    //            {
    //                var message = string.Format("Failed to start Orleans silo '{0}' as a {1} node.", siloHost.Name, siloHost.Type);
    //                throw new SystemException(message);
    //            }
    //        }
    //        catch (Exception ex)
    //        {
    //            siloHost.ReportStartupError(ex);
    //            Console.WriteLine("OrleansHostWrapper", "Run", "{0}:\n{1}\n{2}", ex.GetType().FullName, ex.Message, ex.StackTrace);
    //        }

    //        return ok;
    //    }

    //    public bool Stop()
    //    {
    //        var ok = false;

    //        try
    //        {
    //            siloHost.StopOrleansSilo();

    //            Console.WriteLine("OrleansHostWrapper", "Stop", "Orleans silo '{0}' shutdown.", siloHost.Name);
    //        }
    //        catch (Exception ex)
    //        {
    //            siloHost.ReportStartupError(ex);
    //            Console.WriteLine("OrleansHostWrapper", "Stop", "{0}:\n{1}\n{2}", ex.GetType().FullName, ex.Message, ex.StackTrace);
    //        }

    //        return ok;
    //    }

    //    public void Dispose()
    //    {
    //        this.Dispose(true);
    //    }

    //    protected virtual void Dispose(bool dispose)
    //    {
    //        siloHost.Dispose();
    //        siloHost = null;
    //    }
    //}
    #endregion


}
