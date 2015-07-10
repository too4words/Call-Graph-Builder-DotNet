﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Diagnostics.Contracts;
using System.Threading.Tasks;
using Orleans;
using Orleans.Providers;
using OrleansInterfaces;
using ReachingTypeAnalysis.Roslyn;

namespace ReachingTypeAnalysis.Analysis
{
    public interface IProjectState : IGrainState
    {
        string FullPath { get; set; }
        string Name { get; set; }
 
        string SourceCode { get; set; }

    }

    [StorageProvider(ProviderName = "TestStore")]
    public class ProjectCodeProviderGrain : Grain<IProjectState>, IProjectCodeProviderGrain
    {
        [NonSerialized]
        private ProjectCodeProvider projectCodeProvider;

        public override async Task OnActivateAsync()
        {
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
            }

        }

        public async Task SetProjectPath(string fullPath)
        {
            this.State.FullPath = fullPath;
            this.projectCodeProvider = await ProjectCodeProvider.ProjectCodeProviderAsync(this.State.FullPath);
            await this.State.WriteStateAsync();
            return;
        }

        public async Task SetProjectSourceCode(string source)
        {
            this.State.SourceCode = source;
            var solution = Utils.CreateSolution(source);
            // To do: Hack
            this.State.Name = "MyProject";
            this.projectCodeProvider = await ProjectCodeProvider.ProjectCodeProviderByNameAsync(solution, this.State.Name);                    
            await this.State.WriteStateAsync();
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
            var methodImplementationDescriptor = this.projectCodeProvider.FindMethodImplementation(methodDescriptor, typeDescriptor);
            return Task.FromResult<MethodDescriptor>(methodImplementationDescriptor);
        }

		public async Task<IEntity> CreateMethodEntityAsync(MethodDescriptor methodDescriptor)
		{
			Contract.Assert(this.projectCodeProvider != null);
			var methodEntity = await this.projectCodeProvider.CreateMethodEntityAsync(methodDescriptor);
			return methodEntity;
		}
    }
    [Serializable]
    internal class ProjectGrainWrapper : ICodeProvider
    {
        private IProjectCodeProviderGrain projectGrain;
        internal static async Task<ICodeProvider> CreateProjectGrainWrapperAsync(MethodDescriptor methodDescriptor)
        {
            ISolutionGrain solutionGrain = SolutionGrainFactory.GetGrain("Solution");
            var codeProviderGrain = await solutionGrain.GetCodeProviderAsync(methodDescriptor);

            if(codeProviderGrain!=null)
            {
                return new ProjectGrainWrapper(codeProviderGrain);
            }
            return null;
        }
        internal ProjectGrainWrapper(IProjectCodeProviderGrain grain)
        {
            projectGrain = grain;
        }
        public Task<bool> IsSubtypeAsync(TypeDescriptor typeDescriptor1, TypeDescriptor typeDescriptor2)
        {
            return projectGrain.IsSubtypeAsync(typeDescriptor1,typeDescriptor2);
        }
        public Task<MethodDescriptor> FindMethodImplementationAsync(MethodDescriptor methodDescriptor, TypeDescriptor typeDescriptor)
        {
            return projectGrain.FindMethodImplementationAsync(methodDescriptor, typeDescriptor);
        }

        public bool IsSubtype(TypeDescriptor typeDescriptor1, TypeDescriptor typeDescriptor2)
        {
            throw new NotImplementedException();
            //return IsSubtypeAsync(typeDescriptor1,typeDescriptor2).Result;
        }
        public MethodDescriptor FindMethodImplementation(MethodDescriptor methodDescriptor, TypeDescriptor typeDescriptor)
        {
            throw new NotImplementedException();
            //return FindMethodImplementationAsync(methodDescriptor,typeDescriptor).Result;
        }
    }
}
