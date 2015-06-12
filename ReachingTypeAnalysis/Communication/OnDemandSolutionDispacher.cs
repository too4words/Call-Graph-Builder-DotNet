// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.
using Orleans.Runtime.Host;
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
        public async override Task<IEntity> GetEntityAsync(IEntityDescriptor entityDesc)
        {
            IEntity entity = await base.GetEntityAsync(entityDesc);
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

                var pair = await ProjectCodeProvider.GetAsync(methodDescriptor);
                var codeProvider = pair.Item1;
                var tree = pair.Item2;

                if (codeProvider != null)
                {
                    var roslynMethod = codeProvider.FindMethod(methodDescriptor);
                    var model = codeProvider.Compilation.GetSemanticModel(tree);
                    var methodEntityGenerator = new MethodSyntaxProcessor(model, tree, roslynMethod, this);
                    entity = methodEntityGenerator.ParseMethod();
                }
                else
                {
                    var libraryMethodVisitor = new LibraryMethodProcessor(methodDescriptor, this);
                    entity = libraryMethodVisitor.ParseLibraryMethod();

                    // I need a Roslyn Method. I could to this with a solution but I cant without it
                    ///throw new NotImplementedException();
                    //var libraryMethodVisitor = new LibraryMethodProcessor(roslynMethod, this);
                    //entity = libraryMethodVisitor.ParseLibraryMethod();
                    //base.RegisterEntity(entityDesc, entity);
                }
                base.RegisterEntity(entityDesc, entity);
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
        }
    }
}
