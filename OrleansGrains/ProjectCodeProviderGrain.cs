﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.

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
        string FullPath { get; set; }
        string Name { get; set; } 
        string SourceCode { get; set; }
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

			if (this.State.FullPath != null)
			{
				this.projectCodeProvider = await ProjectCodeProvider.ProjectCodeProviderAsync(this.State.FullPath);
			}
            else
            {
                if (this.State.SourceCode != null)
                {
                    var solution = Utils.CreateSolution(this.State.SourceCode);
                    Contract.Assert(this.State.Name != null);
                    this.projectCodeProvider = await ProjectCodeProvider.ProjectCodeProviderByNameAsync(solution,this.State.Name);                    
                }
                else
                {
                    if(this.GetPrimaryKeyString().Equals("DUMMY"))
                    {
                        this.projectCodeProvider = new DummyCodeProvider();
                    }
                }
            }
			Logger.Log(this.GetLogger(), "ProjectGrain", "OnActivate", "Exit");
            
        }

        public async Task SetProjectPath(string fullPath)
        {
			Logger.Log(this.GetLogger(), "ProjectGrain", "SetProjectPath", "Enter");
            this.State.FullPath = fullPath;
            this.projectCodeProvider = await ProjectCodeProvider.ProjectCodeProviderAsync(this.State.FullPath);
            await this.WriteStateAsync();
			Logger.Log(this.GetLogger(), "ProjectGrain", "SetProjectPath", "Exit");
            return;
        }

        public async Task SetProjectSourceCode(string source)
        {
            this.State.SourceCode = source;
            var solution = Utils.CreateSolution(source);
            // To do: Hack
            this.State.Name = "MyProject";
            this.projectCodeProvider = await ProjectCodeProvider.ProjectCodeProviderByNameAsync(solution, this.State.Name);                    
            await this.WriteStateAsync();
            return;
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
    }   
}