using Microsoft.CodeAnalysis;
// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.
using Orleans.Runtime.Host;
using OrleansGrains;
using ReachingTypeAnalysis.Analysis;
using ReachingTypeAnalysis.Roslyn;
using System;
using System.Diagnostics.Contracts;
using System.Net;
using System.Threading.Tasks;

namespace ReachingTypeAnalysis.Communication
{
	internal class OnDemandSyncDispatcher : SynchronousLocalDispatcher
    {
        public OnDemandSyncDispatcher(bool async = false)
            : base(async)
        {
        }
        /// <summary>
        /// This dispacher tries to find and load methods on the fly
        /// </summary>
        /// <param name="entityDesc"></param>
        /// <returns></returns>
        public async override Task<IEntity> GetEntity(IEntityDescriptor entityDesc)
        {
            IEntity entity = await base.GetEntity(entityDesc);
            if (entity == null)
            {
                MethodDescriptor methodDescriptor = GetMethodDescriptor(entityDesc);
                //try
                //{
                //    var codeProvider = await CodeProvider.GetAsync(methodDescriptor);
                //    var roslynMethod = codeProvider.FindMethod(methodDescriptor);
                //    var methodEntityGenerator = new MethodSyntaxProcessor(roslynMethod, codeProvider, this);
                //    entity = methodEntityGenerator.ParseMethod();
                //}
                //catch(Exception exception)
                //{
                //    //    // I need a Roslyn Method. I could to this with a solution but I cant without it
                //    //throw new NotImplementedException();
                //    var libraryMethodVisitor = new LibraryMethodProcessor(methodDescriptor, this);
                //    entity = libraryMethodVisitor.ParseLibraryMethod();
                //    base.RegisterEntity(entityDesc, entity);
                //}

                var codeProvider = await CodeProvider.GetAsync(methodDescriptor);

                if (codeProvider != null)
                {
                    var roslynMethod = codeProvider.FindMethod(methodDescriptor);
                    var methodEntityGenerator = new MethodSyntaxProcessor(roslynMethod, codeProvider, this);
                    entity = methodEntityGenerator.ParseMethod();
                }
                else
                {
                    var libraryMethodVisitor = new LibraryMethodProcessor(methodDescriptor, this);
                    entity = libraryMethodVisitor.ParseLibraryMethod();
                    base.RegisterEntity(entityDesc, entity);

                    // I need a Roslyn Method. I could to this with a solution but I cant without it
                    ///throw new NotImplementedException();
                    //var libraryMethodVisitor = new LibraryMethodProcessor(roslynMethod, this);
                    //entity = libraryMethodVisitor.ParseLibraryMethod();
                    //base.RegisterEntity(entityDesc, entity);
                }
            }

            return entity;
        }

        /// <summary>
        /// This method should not exists. Because the IEntityDescriptor should have a MethdodDescriptor 
        /// and Orleans entities should not appear here
        /// </summary>
        /// <param name="entityDesc"></param>
        /// <returns></returns>
        private MethodDescriptor GetMethodDescriptor(IEntityDescriptor entityDesc)
        {
            if(entityDesc is MethodEntityDescriptor)
            {
                return ((MethodEntityDescriptor)entityDesc).MethodDescriptor;
            }
            if (entityDesc is OrleansEntityDescriptor)
            {
                var grainDesc = (OrleansEntityDescriptor)entityDesc;
    			Contract.Assert(grainDesc != null);
	
                return grainDesc.MethodDescriptor;
            }
            throw new NotImplementedException("We shouldn't reach this place");
            return null;
        }
    }

    internal class OrleansHostWrapper : IDisposable
    {
        public bool Debug
        {
            get { return siloHost != null && siloHost.Debug; }
            set { siloHost.Debug = value; }
        }

        private SiloHost siloHost;

        public OrleansHostWrapper(string[] args)
        {
            ParseArguments(args);
            Init();
        }

        public bool Run()
        {
            bool ok = false;

            try
            {
                siloHost.InitializeOrleansSilo();

                ok = siloHost.StartOrleansSilo();

                if (ok)
                {
                    Console.WriteLine(string.Format("Successfully started Orleans silo '{0}' as a {1} node.", siloHost.Name, siloHost.Type));
                }
                else
                {
                    throw new SystemException(string.Format("Failed to start Orleans silo '{0}' as a {1} node.", siloHost.Name, siloHost.Type));
                }
            }
            catch (Exception exc)
            {
                siloHost.ReportStartupError(exc);
                var msg = string.Format("{0}:\n{1}\n{2}", exc.GetType().FullName, exc.Message, exc.StackTrace);
                Console.WriteLine(msg);
            }

            return ok;
        }

        public bool Stop()
        {
            bool ok = false;

            try
            {
                siloHost.StopOrleansSilo();

                Console.WriteLine(string.Format("Orleans silo '{0}' shutdown.", siloHost.Name));
            }
            catch (Exception exc)
            {
                siloHost.ReportStartupError(exc);
                var msg = string.Format("{0}:\n{1}\n{2}", exc.GetType().FullName, exc.Message, exc.StackTrace);
                Console.WriteLine(msg);
            }

            return ok;
        }

        private void Init()
        {
            siloHost.LoadOrleansConfig();
        }

        private bool ParseArguments(string[] args)
        {
            string deploymentId = null;

            string configFileName = "DevTestServerConfiguration.xml";
            string siloName = Dns.GetHostName(); // Default to machine name

            int argPos = 1;
            for (int i = 0; i < args.Length; i++)
            {
                string a = args[i];
                if (a.StartsWith("-") || a.StartsWith("/"))
                {
                    switch (a.ToLowerInvariant())
                    {
                        case "/?":
                        case "/help":
                        case "-?":
                        case "-help":
                            // Query usage help
                            return false;
                        default:
                            Console.WriteLine("Bad command line arguments supplied: " + a);
                            return false;
                    }
                }
                else if (a.Contains("="))
                {
                    string[] split = a.Split('=');
                    if (String.IsNullOrEmpty(split[1]))
                    {
                        Console.WriteLine("Bad command line arguments supplied: " + a);
                        return false;
                    }
                    switch (split[0].ToLowerInvariant())
                    {
                        case "deploymentid":
                            deploymentId = split[1];
                            break;
                        case "deploymentgroup":
                            // TODO: Remove this at some point in future
                            Console.WriteLine("Ignoring deprecated command line argument: " + a);
                            break;
                        default:
                            Console.WriteLine("Bad command line arguments supplied: " + a);
                            return false;
                    }
                }
                // unqualified arguments below
                else if (argPos == 1)
                {
                    siloName = a;
                    argPos++;
                }
                else if (argPos == 2)
                {
                    configFileName = a;
                    argPos++;
                }
                else
                {
                    // Too many command line arguments
                    Console.WriteLine("Too many command line arguments supplied: " + a);
                    return false;
                }
            }

            siloHost = new SiloHost(siloName);
            siloHost.ConfigFileName = configFileName;
            if (deploymentId != null)
                siloHost.DeploymentId = deploymentId;

            return true;
        }

        public void PrintUsage()
        {
            Console.WriteLine(
@"USAGE: 
    OrleansHost.exe [<siloName> [<configFile>]] [DeploymentId=<idString>] [/debug]
Where:
    <siloName>      - Name of this silo in the Config file list (optional)
    <configFile>    - Path to the Config file to use (optional)
    DeploymentId=<idString> 
                    - Which deployment group this host instance should run in (optional)
    /debug          - Turn on extra debug output during host startup (optional)");
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool dispose)
        {
            siloHost.Dispose();
            siloHost = null;
        }
    }
}
