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
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Table;

namespace OrleansSilosInAzure
{
    public class WorkerRole : RoleEntryPoint
    {
        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private readonly ManualResetEvent runCompleteEvent = new ManualResetEvent(false);

        private AzureSilo orleansAzureSilo;
        private const string DATA_CONNECTION_STRING_KEY = "DataConnectionString";
        private int instances;

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

				var ipAddr = RoleEnvironment.CurrentRoleInstance.InstanceEndpoints["OrleansSiloEndPoint"].IPEndpoint.Address.ToString();
				Environment.SetEnvironmentVariable("MyIPAddr", ipAddr);

				var instancesCount = RoleEnvironment.CurrentRoleInstance.Role.Instances.Count;
				Environment.SetEnvironmentVariable("MyInstancesCount", instancesCount.ToString());

				this.instances = instancesCount;

				// TODO: Delete Orleans Tables
				// To avoid double delete, check for existence
				//CloudStorageAccount storageAccount = CloudStorageAccount.Parse(CloudConfigurationManager.GetSetting("DataConnectionString"));
					//table = tableClient.GetTableReference("OrleansGrainState");
					//table.DeleteIfExists();
				// Create the table client.
				//CloudTableClient tableClient = storageAccount.CreateCloudTableClient();

				//CloudTable table = tableClient.GetTableReference("OrleansSiloStatistics");
				//table.DeleteIfExists();

				//CloudTable  table = tableClient.GetTableReference("OrleansClientStatistics");
				//table.DeleteIfExists();

				
				// It is IMPORTANT to start the silo not in OnStart but in Run.
				// Azure may not have the firewalls open yet (on the remote silos) at the OnStart phase.
				orleansAzureSilo = new AzureSilo();
				bool ok = orleansAzureSilo.Start(config);
				if (ok)
				{
					Trace.TraceInformation("OrleansAzureSilos-OnStart Orleans silo started ok=" + ok, "Information");
                    Trace.TraceInformation("OrleansSilosInAzure is running");

                    orleansAzureSilo.Run(); // Call will block until silo is shutdown

                    Trace.TraceInformation("OrleansSilosInAzure stop running");
                    WriteToTempFile("OrleansSilosInAzure stop running");

                    //SaveErrorToBlob("Orleans Silo stops!");
                }
                else
				{
					Trace.TraceError("Orleans Silo could not start");
                    WriteToTempFile("Orleans Silo could not start");
                    //SaveErrorToBlob("Orleans Silo could not start");
                }

			}
			catch (Exception ex)
			{
				while (ex is AggregateException) ex = ex.InnerException;
				Trace.TraceError("Error dutring initialization of WorkerRole {0}",ex.ToString());
                var excString = ex.ToString();
                WriteToTempFile(excString);
                //SaveErrorToBlob(excString);                
                throw ex;
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
            WriteToTempFile(excString);

            var errorFile = string.Format("error-{0}-{1}", RoleEnvironment.CurrentRoleInstance.Id, DateTime.UtcNow.Ticks);

            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(CloudConfigurationManager.GetSetting("DataConnectionString"));
            // Create the blob client.
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

            // Retrieve reference to a previously created container.
            CloudBlobContainer container = blobClient.GetContainerReference("errors");

            // Retrieve reference to a blob named "myblob".
            CloudBlockBlob blockBlob = container.GetBlockBlobReference(errorFile);

            using (Stream s = GenerateStreamFromString(excString))
            {
                blockBlob.UploadFromStream(s);

            }
        }

        private static void WriteToTempFile(string excString)
        {
            var errorFile = string.Format("error-{0}-{1}", RoleEnvironment.CurrentRoleInstance.Id, DateTime.UtcNow.Ticks);

            using (System.IO.StreamWriter file = new System.IO.StreamWriter(@"C:\Temp\"+errorFile+".txt"))
            {
                file.WriteLine("Logging:" + excString);
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

        public override bool OnStart()
        {
			

			if (!RoleEnvironment.IsEmulated)
			{
				
				try
				{
					CloudStorageAccount storageAccount = CloudStorageAccount.Parse(CloudConfigurationManager.GetSetting("DataConnectionString"));
					var key = storageAccount.Credentials.ExportBase64EncodedKey();
					var storageAccountName = storageAccount.Credentials.AccountName;
					// Mount a drive.
					FilesMappedDrive.Mount("Y:", @"\\"+storageAccountName+@".file.core.windows.net\solutions", storageAccountName,key);
                    // Dg Subsription: orleansstoragedg 0up2Sc/EYfYVeP0Hueim/bUSh63Jqdt/LCQTA0jPKX+KNtSNh1LnJdB0ODD3OnTVXMbqe+NQRZkE0mGuXpgi4Q==
                    //FilesMappedDrive.Mount("Y:", @"\\orleansstorage2.file.core.windows.net\solutions", "orleansstorage2",
                    //	"ilzOub7LFk5zQ7drJFkfoxdwN1rritlSWAJ9Vl35g/TG4rZWxCXWNTJV20vZLTL/D2LK065cG8AozDg8CGOKQQ==");
                }
                catch (Exception exc)
				{
					while (exc is AggregateException) exc = exc.InnerException;
					Trace.TraceError("Error trying to mount Azure File {0}", exc.ToString());
                    WriteToTempFile(exc.ToString());
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
            //ServicePointManager.DefaultConnectionLimit = 12;

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
            foreach (RoleEnvironmentConfigurationSettingChange settingChange in e.Changes.Where(x => x is RoleEnvironmentTopologyChange))
            {
                e.Cancel = true;
                return;
            }

   //         foreach (RoleEnvironmentConfigurationSettingChange settingChange in e.Changes.Where(x => x is RoleEnvironmentConfigurationSettingChange))
			//{
			//	switch (settingChange.ConfigurationSettingName)
			//	{
   //                 case "Startup.ExternalTasksUrl":
   //                     Trace.TraceWarning("The specified configuration changes can't be made on a running instance. Recycling...");
   //                     e.Cancel = true;
   //                     return;
   //                 case "Startup.VsInstallDir":
   //                     Trace.TraceWarning("The specified configuration changes can't be made on a running instance. Recycling...");
   //                     e.Cancel = true;
   //                     return;
   //             }
			//}
		}


        public override void OnStop()
        {
            Trace.TraceInformation("OrleansSilosInAzure is stopping");
            
            this.cancellationTokenSource.Cancel();
            this.runCompleteEvent.WaitOne();

            orleansAzureSilo.Stop();
            WriteToTempFile("OrleansSilosInAzure has stopped");

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
