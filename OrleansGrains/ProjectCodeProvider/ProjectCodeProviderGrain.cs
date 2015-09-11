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
			await StatsHelper.RegisterActivation("ProjectCodeProviderGrain", this.GrainFactory);

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
			public override Task OnDeactivateAsync()
		{
			return StatsHelper.RegisterDeactivation("ProjectCodeProviderGrain", this.GrainFactory); 
		}

        public async Task SetProjectPathAsync(string fullPath)
        {
			await StatsHelper.RegisterMsg("ProjectGrain::SetProjectPath", this.GrainFactory);

			Logger.LogVerbose(this.GetLogger(), "ProjectGrain", "SetProjectPath", "Enter");

            this.State.ProjectPath = fullPath;
            this.projectCodeProvider = await OrleansProjectCodeProvider.CreateFromProjectAsync(this.GrainFactory, this.State.ProjectPath);
            this.State.AssemblyName = null;
            this.State.Source = null;
            this.State.TestName = null;

            await this.WriteStateAsync();
            Logger.LogVerbose(this.GetLogger(), "ProjectGrain", "SetProjectPath", "Exit");
        }

        public async Task SetProjectSourceAsync(string source)
        {
			await StatsHelper.RegisterMsg("ProjectGrain::SetProjectSource", this.GrainFactory);

			Logger.LogVerbose(this.GetLogger(), "ProjectGrain", "SetProjectSource", "Enter");

            this.State.Source = source;
            this.State.AssemblyName = TestConstants.ProjectAssemblyName;
            this.projectCodeProvider = await OrleansProjectCodeProvider.CreateFromSourceAsync(this.GrainFactory, this.State.Source, this.State.AssemblyName);
            this.State.ProjectPath = null;
            this.State.TestName = null;

            await this.WriteStateAsync();
            Logger.LogVerbose(this.GetLogger(), "ProjectGrain", "SetProjectSource", "Exit");
        }

        public async Task SetProjectFromTestAsync(string testName)
        {
			await StatsHelper.RegisterMsg("ProjectGrain::SetProjectFromTest", this.GrainFactory);

			Logger.LogVerbose(this.GetLogger(), "ProjectGrain", "SetProjectFromTest", "Enter");

            this.State.TestName = testName;
            this.State.AssemblyName = TestConstants.ProjectAssemblyName;
            this.projectCodeProvider = await OrleansProjectCodeProvider.CreateFromTestAsync(this.GrainFactory, this.State.TestName, this.State.AssemblyName);
            this.State.ProjectPath = null;
            this.State.Source = null;

            await this.WriteStateAsync();
            Logger.LogVerbose(this.GetLogger(), "ProjectGrain", "SetProjectFromTest", "Exit");
        }

        public Task<bool> IsSubtypeAsync(TypeDescriptor typeDescriptor1, TypeDescriptor typeDescriptor2)
        {
			StatsHelper.RegisterMsg("ProjectGrain::IsSubtype", this.GrainFactory);

			return this.projectCodeProvider.IsSubtypeAsync(typeDescriptor1, typeDescriptor2);
        }

        public Task<MethodDescriptor> FindMethodImplementationAsync(MethodDescriptor methodDescriptor, TypeDescriptor typeDescriptor)
        {
			StatsHelper.RegisterMsg("ProjectGrain::FindMethodImplementation", this.GrainFactory);

			//var methodImplementationDescriptor = this.projectCodeProvider.FindMethodImplementation(methodDescriptor, typeDescriptor);
			//return Task.FromResult<MethodDescriptor>(methodImplementationDescriptor);
			return this.projectCodeProvider.FindMethodImplementationAsync(methodDescriptor, typeDescriptor);
        }

        public Task<IEnumerable<MethodDescriptor>> GetRootsAsync()
        {
			StatsHelper.RegisterMsg("ProjectGrain::GetRoots", this.GrainFactory);

			return this.projectCodeProvider.GetRootsAsync();
        }

		public Task<IEnumerable<MethodDescriptor>> GetReachableMethodsAsync()
		{
			StatsHelper.RegisterMsg("ProjectGrain::GetReachableMethods", this.GrainFactory);

			return this.projectCodeProvider.GetReachableMethodsAsync();
		}

		public Task<IEntity> CreateMethodEntityAsync(MethodDescriptor methodDescriptor)
        {
			StatsHelper.RegisterMsg("ProjectGrain::CreateMethodEntity", this.GrainFactory);

			Logger.LogVerbose(this.GetLogger(), "ProjectGrain", "CreateMethodEntity", "Enter");

			var timer = new Stopwatch();
            timer.Start();

            var result = this.projectCodeProvider.CreateMethodEntityAsync(methodDescriptor);

            timer.Stop();
            Logger.LogWarning(this.GetLogger(), "ProjectGrain", "CreateMethodEntity", "Exit; took;{0};ms;{1};ticks", timer.ElapsedMilliseconds, timer.ElapsedTicks);
            return result;
        }

        public Task<IEnumerable<FileResponse>> GetDocumentsAsync()
        {
			StatsHelper.RegisterMsg("ProjectGrain::GetDocuments", this.GrainFactory);

			return this.projectCodeProvider.GetDocumentsAsync();
        }

        public Task<IEnumerable<FileResponse>> GetDocumentEntitiesAsync(string documentPath)
        {
			StatsHelper.RegisterMsg("ProjectGrain::GetDocumentEntities", this.GrainFactory);

			return this.projectCodeProvider.GetDocumentEntitiesAsync(documentPath);
        }

		public Task<CodeGraphModel.SymbolReference> GetDeclarationInfoAsync(MethodDescriptor methodDescriptor)
		{
			StatsHelper.RegisterMsg("ProjectGrain::GetDeclarationInfo", this.GrainFactory);

			return this.projectCodeProvider.GetDeclarationInfoAsync(methodDescriptor);
		}

		public Task<CodeGraphModel.SymbolReference> GetInvocationInfoAsync(CallContext callContext)
		{
			StatsHelper.RegisterMsg("ProjectGrain::GetInvocationInfo", this.GrainFactory);

			return this.projectCodeProvider.GetInvocationInfoAsync(callContext);
		}

        public Task<IMethodEntityWithPropagator> GetMethodEntityAsync(MethodDescriptor methodDescriptor)
        {
			StatsHelper.RegisterMsg("ProjectGrain::GetMethodEntity", this.GrainFactory);

			return this.projectCodeProvider.GetMethodEntityAsync(methodDescriptor);
        }

        public Task<PropagationEffects> RemoveMethodAsync(MethodDescriptor methodToUpdate)
        {
			StatsHelper.RegisterMsg("ProjectGrain::RemoveMethod", this.GrainFactory);

			return this.projectCodeProvider.RemoveMethodAsync(methodToUpdate);
        }

        public async Task ReplaceDocumentSourceAsync(string source, string documentPath)
        {
			await StatsHelper.RegisterMsg("ProjectGrain::ReplaceDocumentSource", this.GrainFactory);

			this.State.Source = source;
            await this.WriteStateAsync();

            await this.projectCodeProvider.ReplaceDocumentSourceAsync(source, documentPath);
        }

        public Task ReplaceDocumentAsync(string documentPath, string newDocumentPath = null)
        {
			StatsHelper.RegisterMsg("ProjectGrain::ReplaceDocument", this.GrainFactory);

			return this.projectCodeProvider.ReplaceDocumentAsync(documentPath);
        }

        public Task<IEnumerable<MethodModification>> GetModificationsAsync(IEnumerable<string> modifiedDocuments)
        {
			StatsHelper.RegisterMsg("ProjectGrain::GetModifications", this.GrainFactory);

			return this.projectCodeProvider.GetModificationsAsync(modifiedDocuments);
        }

        public Task ReloadAsync()
        {
			StatsHelper.RegisterMsg("ProjectGrain::Reload", this.GrainFactory);

			return this.projectCodeProvider.ReloadAsync();
        }

        /// <summary>
        /// Deactivates the grain and all method entity grains it has created
        /// </summary>
        /// <returns></returns>
        public async Task ForceDeactivationAsync()
        {
			//await StatsHelper.RegisterMsg("ProjectGrain::ForceDeactivation", this.GrainFactory);
			/// TODO: Change interface by OrleansCodeProvider but we need to fix the Dummy provider

			if (this.projectCodeProvider is OrleansProjectCodeProvider)
            {
                var orleansProvider = this.projectCodeProvider as OrleansProjectCodeProvider;
                await orleansProvider.ForceDeactivationOfMethodEntitiesAsync();
            }

            await this.ClearStateAsync();

            //this.State.ProjectPath = null;
            //this.State.Source = null;
            //this.State.TestName = null;
            //this.State.AssemblyName = null;
            //await this.WriteStateAsync();

            this.DeactivateOnIdle();
        }

		public Task<IEnumerable<MethodDescriptor>> GetPublicMethodsAsync()
		{
			StatsHelper.RegisterMsg("ProjectGrain::GetPublicMethods", this.GrainFactory);

			return Task.FromResult(new HashSet<MethodDescriptor>().AsEnumerable());
		}

		public Task<PropagationEffects> AddMethodAsync(MethodDescriptor methodToAdd)
		{
			StatsHelper.RegisterMsg("ProjectGrain::AddMethod", this.GrainFactory);

			return this.projectCodeProvider.AddMethodAsync(methodToAdd);
		}

		public Task<IEnumerable<TypeDescriptor>> GetCompatibleInstantiatedTypesAsync(TypeDescriptor type)
		{
			StatsHelper.RegisterMsg("ProjectGrain::GetCompatibleInstantiatedTypes", this.GrainFactory);

			return this.projectCodeProvider.GetCompatibleInstantiatedTypesAsync(type);
		}
	}
}
