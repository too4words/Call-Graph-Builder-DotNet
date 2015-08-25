// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Diagnostics.Contracts;
using System.Threading.Tasks;
using Orleans;
using Orleans.Providers;
using OrleansInterfaces;
using ReachingTypeAnalysis;
using ReachingTypeAnalysis.Roslyn;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using CodeGraphModel;
using System.Diagnostics;
using Orleans.Concurrency;
using TestSources;

namespace ReachingTypeAnalysis.Analysis
{
    public interface IProjectState : IGrainState
    {
        string ProjectPath { get; set; }
        string AssemblyName { get; set; } 
        string Source { get; set; }
		string TestName { get; set; }
	}

    //[StorageProvider(ProviderName = "FileStore")]
    //[StorageProvider(ProviderName = "MemoryStore")]
	[StorageProvider(ProviderName = "AzureStore")]
	[Reentrant]
    public class ProjectCodeProviderGrain : Grain<IProjectState>, IProjectCodeProviderGrain
    {
        [NonSerialized]
        private IProjectCodeProvider projectCodeProvider;

        public override async Task OnActivateAsync()
        {
			Logger.OrleansLogger = this.GetLogger();
			Logger.LogVerbose(this.GetLogger(), "ProjectGrain", "OnActivate", "Enter");

			this.State.AssemblyName = this.GetPrimaryKeyString();

			if (!String.IsNullOrEmpty(this.State.ProjectPath))
			{
				this.projectCodeProvider = await OrleansProjectCodeProvider.CreateFromProjectAsync(this.GrainFactory, this.State.ProjectPath);
			}
			else if (!String.IsNullOrEmpty(this.State.Source) && !String.IsNullOrEmpty(this.State.AssemblyName))
            {
                    this.projectCodeProvider = await OrleansProjectCodeProvider.CreateFromSourceAsync(this.GrainFactory, this.State.Source, this.State.AssemblyName);                    
                }
			else if (!String.IsNullOrEmpty(this.State.TestName) && !String.IsNullOrEmpty(this.State.AssemblyName))
				{
				this.projectCodeProvider = await OrleansProjectCodeProvider.CreateFromTestAsync(this.GrainFactory, this.State.TestName, this.State.AssemblyName);
					}
			else if (this.State.AssemblyName.Equals("DUMMY"))
					{
							this.projectCodeProvider = new OrleansDummyProjectCodeProvider(this.GrainFactory);
						}

			Logger.LogVerbose(this.GetLogger(), "ProjectGrain", "OnActivate", "Exit");            
        }

        public async Task SetProjectPath(string fullPath)
        {
			Logger.LogVerbose(this.GetLogger(), "ProjectGrain", "SetProjectPath", "Enter");

            this.State.ProjectPath = fullPath;
            this.projectCodeProvider = await OrleansProjectCodeProvider.CreateFromProjectAsync(this.GrainFactory, this.State.ProjectPath);
			this.State.AssemblyName = null;
			this.State.Source = null;
			this.State.TestName = null;

			await this.WriteStateAsync();
			Logger.LogVerbose(this.GetLogger(), "ProjectGrain", "SetProjectPath", "Exit");
        }

        public async Task SetProjectSourceCode(string source)
        {
			Logger.LogVerbose(this.GetLogger(), "ProjectGrain", "SetProjectSource", "Enter");

            this.State.Source = source;
            this.State.AssemblyName = TestConstants.ProjectAssemblyName;
            this.projectCodeProvider = await OrleansProjectCodeProvider.CreateFromSourceAsync(this.GrainFactory, this.State.Source, this.State.AssemblyName);
			this.State.ProjectPath = null;
			this.State.TestName = null;

			await this.WriteStateAsync();
			Logger.LogVerbose(this.GetLogger(), "ProjectGrain", "SetProjectSource", "Exit");
        }

		public async Task SetProjectTest(string testName)
		{
			Logger.LogVerbose(this.GetLogger(), "ProjectGrain", "SetProjectTest", "Enter");

			this.State.TestName = testName;
			this.State.AssemblyName = TestConstants.ProjectAssemblyName;
			this.projectCodeProvider = await OrleansProjectCodeProvider.CreateFromTestAsync(this.GrainFactory, this.State.TestName, this.State.AssemblyName);
			this.State.ProjectPath = null;
			this.State.Source = null;

			await this.WriteStateAsync();
			Logger.LogVerbose(this.GetLogger(), "ProjectGrain", "SetProjectTest", "Exit");
		}

        public Task<bool> IsSubtypeAsync(TypeDescriptor typeDescriptor1, TypeDescriptor typeDescriptor2)
        {
            Contract.Assert(this.projectCodeProvider != null);
            return this.projectCodeProvider.IsSubtypeAsync(typeDescriptor1, typeDescriptor2);
        }

        public Task<MethodDescriptor> FindMethodImplementationAsync(MethodDescriptor methodDescriptor, TypeDescriptor typeDescriptor)
        {
			Contract.Assert(this.projectCodeProvider != null);
            //var methodImplementationDescriptor = this.projectCodeProvider.FindMethodImplementation(methodDescriptor, typeDescriptor);
            //return Task.FromResult<MethodDescriptor>(methodImplementationDescriptor);
            return this.projectCodeProvider.FindMethodImplementationAsync(methodDescriptor, typeDescriptor);
        }

        public Task<IEnumerable<MethodDescriptor>> GetRootsAsync()
        {
            return this.projectCodeProvider.GetRootsAsync();
        }

		public Task<IEntity> CreateMethodEntityAsync(MethodDescriptor methodDescriptor)
		{
			Contract.Assert(this.projectCodeProvider != null);
			Logger.LogVerbose(this.GetLogger(), "ProjectGrain", "SetProjectPath", "Enter");
			Stopwatch timer = new Stopwatch();
			timer.Start();
			
			var result = this.projectCodeProvider.CreateMethodEntityAsync(methodDescriptor);
			
			timer.Stop();
			Logger.LogVerbose(this.GetLogger(), "ProjectGrain", "SetProjectPath", "Exit. Took: {0}", timer.ElapsedMilliseconds);
			return result;
		}

		public Task<IEnumerable<FileResponse>> GetDocumentsAsync()
		{
			return this.projectCodeProvider.GetDocumentsAsync();
		}

		public Task<IEnumerable<FileResponse>> GetDocumentEntitiesAsync(string documentPath)
		{
			return this.projectCodeProvider.GetDocumentEntitiesAsync(documentPath);
		}

		public Task<IMethodEntityWithPropagator> GetMethodEntityAsync(MethodDescriptor methodDescriptor)
		{
			return this.projectCodeProvider.GetMethodEntityAsync(methodDescriptor);
		}

		public Task<PropagationEffects> RemoveMethodAsync(MethodDescriptor methodToUpdate)
		{
			return this.projectCodeProvider.RemoveMethodAsync(methodToUpdate);
		}

		public async Task ReplaceDocumentSourceAsync(string source, string documentPath)
		{
			this.State.Source = source;
			await this.WriteStateAsync();

			await this.projectCodeProvider.ReplaceDocumentSourceAsync(source, documentPath);
		}

		public Task ReplaceDocumentAsync(string documentPath, string newDocumentPath = null)
		{
			return this.projectCodeProvider.ReplaceDocumentAsync(documentPath);
		}

		public Task<IEnumerable<MethodModification>> GetModificationsAsync(IEnumerable<string> modifiedDocuments)
		{
			return this.projectCodeProvider.GetModificationsAsync(modifiedDocuments);
		}

		public Task ReloadAsync()
		{
			return this.projectCodeProvider.ReloadAsync();
		}

        /// <summary>
        /// Deactivates the grain and all method entity grains it has created
        /// </summary>
        /// <returns></returns>
        public async Task ForceDeactivationAsync()
        {
            /// TODO: Change interface by OrleansCodeProvider but we need to fix the Dummy provider

            if (this.projectCodeProvider is OrleansProjectCodeProvider)
            {
                var orleansProvider = this.projectCodeProvider as OrleansProjectCodeProvider;
                await orleansProvider.ForceDeactivationOfMethodEntitiesAsync();
            }

			await this.ClearStateAsync();

            //this.State.Etag = null;
            this.State.ProjectPath = null;
            this.State.Source = null;
			this.State.TestName = null;
			this.State.AssemblyName = null;
            await this.WriteStateAsync();
            
            this.DeactivateOnIdle();
        }
	}   
}
