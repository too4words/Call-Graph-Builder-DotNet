// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReachingTypeAnalysis.Roslyn;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ReachingTypeAnalysis
{
	[TestClass]
	public class IncrementalDiffTests
	{
		[TestMethod]
		[TestCategory("IncrementalDiff")]
		public async Task TestDocumentsDiff()
		{
			#region original source code
			var source = @"
using System;
class Program
{
    public static void MethodToRemove()
    {
		Console.WriteLine(1);
    }

    public static void MethodToUpdate()
    {
		MethodToRemove();
    }

    public static void Main()
    {
		MethodToUpdate();
    }
}";
			#endregion

			#region modified source code
			var newSource = @"
using System;
class Program
{
	public static void MethodToAdd()
    {
		Console.WriteLine(2);
    }

    public static void MethodToUpdate()
    {
		MethodToAdd();
    }

    public static void Main()
    {
		MethodToUpdate();
    }
}";
			#endregion

			Document document;
			Document newDocument;

			GetDocumentsFromSource(source, newSource, out document, out newDocument);

			var documentDiff = new DocumentDiff();
			var modifications = await documentDiff.GetDifferencesAsync(document, newDocument);

			var remove = new MethodModification(new MethodDescriptor("Program", "MethodToRemove", true), ModificationKind.MethodRemoved);
			var update = new MethodModification(new MethodDescriptor("Program", "MethodToUpdate", true), ModificationKind.MethodUpdated);
			var add = new MethodModification(new MethodDescriptor("Program", "MethodToAdd", true), ModificationKind.MethodAdded);

			Assert.IsTrue(modifications.Contains(remove));
			Assert.IsTrue(modifications.Contains(update));
			Assert.IsTrue(modifications.Contains(add));
		}

		private static void GetDocumentsFromSource(string oldSource, string newSource, out Document oldDocument, out Document newDocument)
		{
			var solution = SolutionFileGenerator.CreateSolution(oldSource);
			var project = solution.Projects.Single();
			oldDocument = project.Documents.Single();

			project = project.RemoveDocument(oldDocument.Id);
			newDocument = project.AddDocument(oldDocument.Name, newSource, null, oldDocument.FilePath);
			//project = newDocument.Project;
			//solution = project.Solution;
		}
	}
}
