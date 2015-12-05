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
	//public class ProjectState
	{
        /*public*/ string ProjectPath { get; set; }
        /*public*/ string AssemblyName { get; set; }
        /*public*/ string Source { get; set; }
		/*public*/ string TestName { get; set; }
    }

    //[StorageProvider(ProviderName = "FileStore")]
    //[StorageProvider(ProviderName = "MemoryStore")]
    [StorageProvider(ProviderName = "AzureStore")]
    [Reentrant]
	public class ProjectCodeProviderGrain : Grain<IProjectState>, IProjectCodeProviderGrain
	//public class ProjectCodeProviderGrain : Grain, IProjectCodeProviderGrain
    {
		[NonSerialized]
        private IProjectCodeProvider projectCodeProvider;
		[NonSerialized]
		private ObserverSubscriptionManager<IEntityGrainObserverNotifications> observers;

		//private ProjectState State;

		//private Task WriteStateAsync()
		//{
		//	return TaskDone.Done;
		//}

		//private Task ClearStateAsync()
		//{
		//	return TaskDone.Done;
		//}

		public override async Task OnActivateAsync()
        {
			//this.State = new ProjectState();

			await StatsHelper.RegisterActivation("ProjectCodeProviderGrain", this.GrainFactory);

			Logger.OrleansLogger = this.GetLogger();
            Logger.LogInfo(this.GetLogger(), "ProjectGrain", "OnActivate", "Enter");

            // Logger.LogWarning(this.GetLogger(), "ProjectGrain", "OnActivate", "Entering Project: {0}", this.GetPrimaryKeyString());

            this.observers = new ObserverSubscriptionManager<IEntityGrainObserverNotifications>();
            this.State.AssemblyName = this.GetPrimaryKeyString();

			//Task.Run(async () =>
			//await Task.Factory.StartNew(async () =>
			//{
				try
				{
					this.RaiseStateChangedEvent(EntityGrainStatus.Busy);

					if (!string.IsNullOrEmpty(this.State.ProjectPath))
					{
						this.projectCodeProvider = await OrleansProjectCodeProvider.CreateFromProjectAsync(this.GrainFactory, this.State.ProjectPath);
					}
					else if (!string.IsNullOrEmpty(this.State.Source) && !String.IsNullOrEmpty(this.State.AssemblyName))
					{
						this.projectCodeProvider = await OrleansProjectCodeProvider.CreateFromSourceAsync(this.GrainFactory, this.State.Source, this.State.AssemblyName);
					}
					else if (!string.IsNullOrEmpty(this.State.TestName) && !String.IsNullOrEmpty(this.State.AssemblyName))
					{
						this.projectCodeProvider = await OrleansProjectCodeProvider.CreateFromTestAsync(this.GrainFactory, this.State.TestName, this.State.AssemblyName);
					}
					else if (this.State.AssemblyName.Equals("DUMMY"))
					{
						this.projectCodeProvider = new OrleansDummyProjectCodeProvider(this.GrainFactory);

						await this.WriteStateAsync();
					}

					this.RaiseStateChangedEvent(EntityGrainStatus.Ready);
				}
				catch (Exception ex)
				{
					var inner = ex;
					while (inner is AggregateException) inner = inner.InnerException;

					Logger.LogError(this.GetLogger(), "ProjectGrain", "OnActivate", "Error:\n{0}\nInner:\n{1}", ex, inner);
					throw ex;
				}
			//});

            Logger.LogInfo(this.GetLogger(), "ProjectGrain", "OnActivate", "Exit");
        }

		public override Task OnDeactivateAsync()
		{
			return StatsHelper.RegisterDeactivation("ProjectCodeProviderGrain", this.GrainFactory); 
		}

		#region IObservableEntityGrain

		public Task AddObserverAsync(IEntityGrainObserverNotifications observer)
		{
			// TODO: Hack to avoid subscribing the solution grain observer twice
			if (this.observers.Count == 0)
			{
				this.observers.Subscribe(observer);
			}

			return TaskDone.Done;
		}

		public Task RemoveObserverAsync(IEntityGrainObserverNotifications observer)
		{
			this.observers.Unsubscribe(observer);
			return TaskDone.Done;
		}

		private void RaiseStateChangedEvent(EntityGrainStatus newState)
	    {
			this.observers.Notify(observer => observer.OnStatusChanged(this.AsReference<IProjectCodeProviderGrain>(), newState));
	    }

		#endregion

		public async Task SetProjectPathAsync(string fullPath)
        {
			//await StatsHelper.RegisterMsg("ProjectGrain::SetProjectPath", this.GrainFactory);
			await StatsHelper.RegisterMsg("ProjectGrain::SetProjectPath:" + fullPath, this.GrainFactory);

			Logger.LogInfo(this.GetLogger(), "ProjectGrain", "SetProjectPath", "Enter:"+fullPath);

            this.State.ProjectPath = fullPath;            
            this.State.AssemblyName = null;
            this.State.Source = null;
            this.State.TestName = null;

            await this.WriteStateAsync();

			//Task.Run(async () =>
			//await Task.Factory.StartNew(async () =>
			//{
				try
				{
					this.RaiseStateChangedEvent(EntityGrainStatus.Busy);

					this.projectCodeProvider = await OrleansProjectCodeProvider.CreateFromProjectAsync(this.GrainFactory, this.State.ProjectPath);

					this.RaiseStateChangedEvent(EntityGrainStatus.Ready);
				}
				catch (Exception ex)
				{
					var inner = ex;
					while (inner is AggregateException) inner = inner.InnerException;

					Logger.LogError(this.GetLogger(), "ProjectGrain", "SetProjectPath", "Error:\n{0}\nInner:\n{1}", ex, inner);
					throw ex;
				}
			//});
			
            Logger.LogInfo(this.GetLogger(), "ProjectGrain", "SetProjectPath", "Exit");
        }

        public async Task SetProjectSourceAsync(string source)
        {
			await StatsHelper.RegisterMsg("ProjectGrain::SetProjectSource", this.GrainFactory);

			Logger.LogVerbose(this.GetLogger(), "ProjectGrain", "SetProjectSource", "Enter");

            this.State.Source = source;
            this.State.AssemblyName = TestConstants.ProjectAssemblyName;
            this.State.ProjectPath = null;
            this.State.TestName = null;

            await this.WriteStateAsync();

			//Task.Run(async () =>
			//await Task.Factory.StartNew(async () =>
			//{
				try
				{
					this.RaiseStateChangedEvent(EntityGrainStatus.Busy);

					this.projectCodeProvider = await OrleansProjectCodeProvider.CreateFromSourceAsync(this.GrainFactory, this.State.Source, this.State.AssemblyName);

					this.RaiseStateChangedEvent(EntityGrainStatus.Ready);
				}
				catch (Exception ex)
				{
					var inner = ex;
					while (inner is AggregateException) inner = inner.InnerException;

					Logger.LogError(this.GetLogger(), "ProjectGrain", "SetProjectSource", "Error:\n{0}\nInner:\n{1}", ex, inner);
					throw ex;
				}
			//});

            Logger.LogVerbose(this.GetLogger(), "ProjectGrain", "SetProjectSource", "Exit");
        }

        public async Task SetProjectFromTestAsync(string testName)
        {
			await StatsHelper.RegisterMsg("ProjectGrain::SetProjectFromTest", this.GrainFactory);

			Logger.LogVerbose(this.GetLogger(), "ProjectGrain", "SetProjectFromTest", "Enter");

            this.State.TestName = testName;
            this.State.AssemblyName = TestConstants.ProjectAssemblyName;
            this.State.ProjectPath = null;
            this.State.Source = null;

            await this.WriteStateAsync();

			//Task.Run(async () =>
			//await Task.Factory.StartNew(async () =>
			//{
				try
				{
					this.RaiseStateChangedEvent(EntityGrainStatus.Busy);

					this.projectCodeProvider = await OrleansProjectCodeProvider.CreateFromTestAsync(this.GrainFactory, this.State.TestName, this.State.AssemblyName);

					this.RaiseStateChangedEvent(EntityGrainStatus.Ready);
				}
				catch (Exception ex)
				{
					var inner = ex;
					while (inner is AggregateException) inner = inner.InnerException;

					Logger.LogError(this.GetLogger(), "ProjectGrain", "SetProjectFromTest", "Error:\n{0}\nInner:\n{1}", ex, inner);
					throw ex;
				}
			//});

            Logger.LogVerbose(this.GetLogger(), "ProjectGrain", "SetProjectFromTest", "Exit");
        }

        public Task<bool> IsSubtypeAsync(TypeDescriptor typeDescriptor1, TypeDescriptor typeDescriptor2)
        {
			StatsHelper.RegisterMsg("ProjectGrain::IsSubtype", this.GrainFactory);

			//if (GrainClient.IsInitialized)
			//{
			//	Logger.LogWarning(GrainClient.Logger, "ProjectGrain", "IsSubtypeAsync", "type1={0}, type2={1}", typeDescriptor1, typeDescriptor2);
			//}

			//Console.WriteLine("ProjectGrain::IsSubtypeAsync type1={0}, type2={1}", typeDescriptor1, typeDescriptor2);

			return this.projectCodeProvider.IsSubtypeAsync(typeDescriptor1, typeDescriptor2);
        }

        public Task<MethodDescriptor> FindMethodImplementationAsync(MethodDescriptor methodDescriptor, TypeDescriptor typeDescriptor)
        {
			StatsHelper.RegisterMsg("ProjectGrain::FindMethodImplementation", this.GrainFactory);

			//var methodImplementationDescriptor = this.projectCodeProvider.FindMethodImplementation(methodDescriptor, typeDescriptor);
			//return Task.FromResult<MethodDescriptor>(methodImplementationDescriptor);
			return this.projectCodeProvider.FindMethodImplementationAsync(methodDescriptor, typeDescriptor);
        }

        public Task<IEnumerable<MethodDescriptor>> GetRootsAsync(AnalysisRootKind rootKind = AnalysisRootKind.Default)
        {
			StatsHelper.RegisterMsg("ProjectGrain::GetRoots", this.GrainFactory);

			return this.projectCodeProvider.GetRootsAsync(rootKind);
        }

		public Task<IEnumerable<MethodDescriptor>> GetReachableMethodsAsync()
		{
			StatsHelper.RegisterMsg("ProjectGrain::GetReachableMethods", this.GrainFactory);

			return this.projectCodeProvider.GetReachableMethodsAsync();
		}

        public Task<int> GetReachableMethodsCountAsync()
        {
            StatsHelper.RegisterMsg("ProjectGrain::GetReachableMethods", this.GrainFactory);

            return this.projectCodeProvider.GetReachableMethodsCountAsync();
        }

        public Task<IEntity> CreateMethodEntityAsync(MethodDescriptor methodDescriptor)
        {
			StatsHelper.RegisterMsg("ProjectGrain::CreateMethodEntity", this.GrainFactory);

			Logger.LogVerbose(this.GetLogger(), "ProjectGrain", "CreateMethodEntity", "Enter");

			var timer = new Stopwatch();
            timer.Start();

            var result = this.projectCodeProvider.CreateMethodEntityAsync(methodDescriptor);

            timer.Stop();
            Logger.LogInfo(this.GetLogger(), "ProjectGrain", "CreateMethodEntity:"+methodDescriptor, "Exit; took;{0};ms;{1};ticks", timer.ElapsedMilliseconds, timer.ElapsedTicks);
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

        public async Task<IMethodEntityWithPropagator> GetMethodEntityAsync(MethodDescriptor methodDescriptor)
        {
			await StatsHelper.RegisterMsg("ProjectGrain::GetMethodEntity"+":"+methodDescriptor, this.GrainFactory);

			var methodEntity = await this.projectCodeProvider.GetMethodEntityAsync(methodDescriptor);

            // Force Activation
            var isReachable = await this.projectCodeProvider.IsReachableAsync(methodDescriptor);

            if (!isReachable)
            {
                await methodEntity.IsInitializedAsync();
            }

            return methodEntity;
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
			else if (this.projectCodeProvider is OrleansDummyProjectCodeProvider)
			{
				var orleansProvider = this.projectCodeProvider as OrleansDummyProjectCodeProvider;
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

		public Task<MethodDescriptor> GetRandomMethodAsync()
		{
			//StatsHelper.RegisterMsg("ProjectGrain::GetRandomMethodAsync", this.GrainFactory);

			return this.projectCodeProvider.GetRandomMethodAsync();
		}

		public Task<bool> IsReachableAsync(MethodDescriptor methodDescriptor)
		{
			//StatsHelper.RegisterMsg("ProjectGrain::IsReachable", this.GrainFactory);

			return this.projectCodeProvider.IsReachableAsync(methodDescriptor);
		}
	}
}
