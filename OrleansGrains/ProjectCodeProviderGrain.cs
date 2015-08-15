// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Diagnostics.Contracts;
using System.Threading.Tasks;
using Orleans;
using Orleans.Providers;
using OrleansInterfaces;
using ReachingTypeAnalysis.Roslyn;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using CodeGraphModel;

namespace ReachingTypeAnalysis.Analysis
{
    public interface IProjectState : IGrainState
    {
        string ProjectPath { get; set; }
        string AssemblyName { get; set; } 
        string Source { get; set; }
    }

    //[StorageProvider(ProviderName = "FileStore")]
    [StorageProvider(ProviderName = "MemoryStore")]
    public class ProjectCodeProviderGrain : Grain<IProjectState>, IProjectCodeProviderGrain
    {
        [NonSerialized]
        private IProjectCodeProvider projectCodeProvider;

        public override async Task OnActivateAsync()
        {
			Logger.Log(this.GetLogger(), "ProjectGrain", "OnActivate", "Enter");

			this.State.AssemblyName = this.GetPrimaryKeyString();

			if (this.State.ProjectPath != null)
			{
				this.projectCodeProvider = await OrleansProjectCodeProvider.CreateFromProjectAsync(this.GrainFactory, this.State.ProjectPath);
			}
            else
            {
                if (this.State.Source != null && this.State.AssemblyName != null)
                {
                    this.projectCodeProvider = await OrleansProjectCodeProvider.CreateFromSourceAsync(this.GrainFactory, this.State.Source, this.State.AssemblyName);                    
                }
                else
                {
                    if(this.State.AssemblyName.Equals("DUMMY"))
                    {
                        this.projectCodeProvider = new OrleansDummyProjectCodeProvider(this.GrainFactory);
                    }
                }
            }

			Logger.Log(this.GetLogger(), "ProjectGrain", "OnActivate", "Exit");            
        }

        public async Task SetProjectPath(string fullPath)
        {
			Logger.Log(this.GetLogger(), "ProjectGrain", "SetProjectPath", "Enter");
            this.State.ProjectPath = fullPath;
            this.projectCodeProvider = await OrleansProjectCodeProvider.CreateFromProjectAsync(this.GrainFactory, this.State.ProjectPath);
			await this.WriteStateAsync();
			Logger.Log(this.GetLogger(), "ProjectGrain", "SetProjectPath", "Exit");
        }

        public async Task SetProjectSourceCode(string source)
        {
            this.State.Source = source;
            // To do: Hack
            this.State.AssemblyName = "MyProject";
            this.projectCodeProvider = await OrleansProjectCodeProvider.CreateFromSourceAsync(this.GrainFactory, this.State.Source, this.State.AssemblyName);
			await this.WriteStateAsync();
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
			return this.projectCodeProvider.CreateMethodEntityAsync(methodDescriptor);
		}

		public Task<IEnumerable<FileResponse>> GetDocumentsAsync()
		{
			return this.projectCodeProvider.GetDocumentsAsync();
		}

		public Task<IEnumerable<CodeGraphModel.FileResponse>> GetDocumentEntitiesAsync(string filePath)
		{
			return this.projectCodeProvider.GetDocumentEntitiesAsync(filePath);
		}

		public Task<IMethodEntityWithPropagator> GetMethodEntityAsync(MethodDescriptor methodDescriptor)
		{
			return this.projectCodeProvider.GetMethodEntityAsync(methodDescriptor);
		}

		public Task RemoveMethodAsync(MethodDescriptor methodToUpdate)
		{
			throw new NotImplementedException();
		}
	}   
}
