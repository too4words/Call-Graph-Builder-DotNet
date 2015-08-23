using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ReachingTypeAnalysis.Roslyn
{
	public class DocumentDiff
	{
		#region class MethodUpdateInfo

		private class MethodUpdateInfo
		{
			public MethodDescriptor MethodDescriptor { get; private set; }
			public BaseMethodDeclarationSyntax NewDeclarationNode { get; private set; }
			public BaseMethodDeclarationSyntax OldDeclarationNode { get; private set; }

			public MethodUpdateInfo(MethodDescriptor methodDescriptor, BaseMethodDeclarationSyntax oldDeclarationNode, BaseMethodDeclarationSyntax newDeclarationNode)
			{
				this.MethodDescriptor = methodDescriptor;
				this.OldDeclarationNode = oldDeclarationNode;
				this.NewDeclarationNode = newDeclarationNode;
			}
		}

		#endregion

		public async Task<IEnumerable<MethodModification>> GetDifferencesAsync(Document oldDocument, Document newDocument)
		{
			var cancellationTokenSource = new CancellationTokenSource();
			var oldDeclaredMethods = await GetDeclaredMethodsAsync(oldDocument, cancellationTokenSource.Token);
			var newDeclaredMethods = await GetDeclaredMethodsAsync(newDocument, cancellationTokenSource.Token);

			//// More async version
			//var oldDeclaredMethodsTask = GetDeclaredMethodsAsync(this.oldDocument, cancellationTokenSource.Token);
			//var newDeclaredMethodsTask = GetDeclaredMethodsAsync(this.newDocument, cancellationTokenSource.Token);

			//await Task.WhenAll(oldDeclaredMethodsTask, newDeclaredMethodsTask);
			//var oldDeclaredMethods = oldDeclaredMethodsTask.Result;
			//var newDeclaredMethods = newDeclaredMethodsTask.Result;

			var result = new List<MethodModification>();
			var methodsAdded = newDeclaredMethods.Except(oldDeclaredMethods);
			var methodsRemoved = oldDeclaredMethods.Except(newDeclaredMethods);
			var methodsUpdated = from oldMethod in oldDeclaredMethods
								 from newMethod in newDeclaredMethods
								 where oldMethod.Equals(newMethod)
								 select new MethodUpdateInfo(newMethod.MethodDescriptor,
															 oldMethod.DeclarationNode,
															 newMethod.DeclarationNode);

			foreach (var methodInfo in methodsAdded)
			{
				var modification = new MethodModification(methodInfo.MethodDescriptor, ModificationKind.MethodAdded);
				result.Add(modification);
            }

			foreach (var methodInfo in methodsRemoved)
			{
				var modification = new MethodModification(methodInfo.MethodDescriptor, ModificationKind.MethodRemoved);
				result.Add(modification);
			}

			// Compare the body of each potentially updated method to see if it was acutally updated or not
			foreach (var methodInfo in methodsUpdated)
			{
				var oldMethodBody = methodInfo.OldDeclarationNode.ToString();
				var newMethodBody = methodInfo.NewDeclarationNode.ToString();

				// TODO: Hack! We should compare the method bodies better!
				if (oldMethodBody != newMethodBody)
				{
					var modification = new MethodModification(methodInfo.MethodDescriptor, ModificationKind.MethodUpdated);
					result.Add(modification);
				}
			}

			return result;
		}

		private static async Task<IEnumerable<MethodParserInfo>> GetDeclaredMethodsAsync(Document document, CancellationToken cancellationToken)
		{
			var root = await document.GetSyntaxRootAsync();
			var semanticModel = await document.GetSemanticModelAsync(cancellationToken);			
			var visitor = new MethodFinder(semanticModel);

			visitor.Visit(root);
			return visitor.DeclaredMethods;
        }
	}
}
