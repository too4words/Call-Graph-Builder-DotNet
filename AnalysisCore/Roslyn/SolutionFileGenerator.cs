using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.IO;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;

namespace ReachingTypeAnalysis
{
    public class ProjectDescriptor 
    {
        internal string Name { get; set; }
        internal string ProjectGuid { get; set; }
        internal string AbsolutePath { get; set; }
        internal IEnumerable<string> Dependencies { get; set; }
        internal IEnumerable<string> Files { get; set; }
    }


    public class SolutionFileGenerator
    {
        	private static MetadataReference mscorlib;

		private static MetadataReference Mscorlib
		{
			get
			{
				if (mscorlib == null)
				{
					mscorlib = MetadataReference.CreateFromAssembly(typeof(object).Assembly);
				}

				return mscorlib;
			}
		}

        /// <summary>
        /// Create a simple solution that contains only one source file.
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
		public static Solution CreateSolution(string source)
		{
			var projectId = ProjectId.CreateNewId();
			var documentId = DocumentId.CreateNewId(projectId);

			var props = new Dictionary<string, string>();
			props["CheckForSystemRuntimeDependency"] = "true";
			var ws = MSBuildWorkspace.Create(props);
			var solution = ws.CurrentSolution
				.AddProject(projectId, TestConstants.ProjectName, TestConstants.ProjectAssemblyName, LanguageNames.CSharp)
				.AddMetadataReference(projectId, Mscorlib)
				.AddDocument(documentId, TestConstants.DocumentName, source, null, TestConstants.DocumentPath);

			return solution;
		}

        /// <summary>
        /// Creates a project file out of the descriptor and returns its body.
        /// The result result can be saved to a .csproj file.
        /// </summary>
        /// <param name="project"></param>
        /// <returns></returns>
        public static string CreateProjectFile(ProjectDescriptor project)
        {
            var result = new StringBuilder(
    @"<?xml version=""1.0"" encoding=""utf-8""?>
  <Project ToolsVersion=""12.0"" DefaultTargets=""Build"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
  <Import Project=""$(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Microsoft.Common.props"" Condition=""Exists('$(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Microsoft.Common.props')"" />
  <PropertyGroup>
    <Configuration Condition="" '$(Configuration)' == '' "">Debug</Configuration>
    <Platform Condition="" '$(Platform)' == '' "">AnyCPU</Platform>
    <ProjectGuid>{");
            result.Append(project.ProjectGuid);
            result.Append(
    @"}</ProjectGuid>
    <OutputType>Exe</OutputType>
    <AppDesignerFolder>Properties</AppDesignerFolder>
    <RootNamespace>ConsoleApplication1</RootNamespace>
    <AssemblyName>ConsoleApplication1</AssemblyName>
    <TargetFrameworkVersion>v4.5</TargetFrameworkVersion>
    <FileAlignment>512</FileAlignment>
  </PropertyGroup>
  <PropertyGroup Condition="" '$(Configuration)|$(Platform)' == 'Debug|AnyCPU' "">
    <PlatformTarget>AnyCPU</PlatformTarget>
    <DebugSymbols>true</DebugSymbols>
    <DebugType>full</DebugType>
    <Optimize>false</Optimize>
    <OutputPath>bin\Debug\</OutputPath>
    <DefineConstants>DEBUG;TRACE</DefineConstants>
    <ErrorReport>prompt</ErrorReport>
    <WarningLevel>4</WarningLevel>
  </PropertyGroup>
  <PropertyGroup Condition="" '$(Configuration)|$(Platform)' == 'Release|AnyCPU' "">
    <PlatformTarget>AnyCPU</PlatformTarget>
    <DebugType>pdbonly</DebugType>
    <Optimize>true</Optimize>
    <OutputPath>bin\Release\</OutputPath>
    <DefineConstants>TRACE</DefineConstants>
    <ErrorReport>prompt</ErrorReport>
    <WarningLevel>4</WarningLevel>
  </PropertyGroup>
  <ItemGroup>
    <Reference Include=""System"" />
    <Reference Include=""System.Core"" />
    <Reference Include=""System.Xml.Linq"" />
    <Reference Include=""System.Data.DataSetExtensions"" />
    <Reference Include=""Microsoft.CSharp"" />
    <Reference Include=""System.Data"" />
    <Reference Include=""System.Xml"" />
  </ItemGroup>
  <ItemGroup>");
            result.Append("\n");
            foreach (var file in project.Files)
            {
                result.AppendFormat("    <Compile Include=\"{0}\" />\n", file);
            }
            result.Append(
    @"</ItemGroup>
  <Import Project=""$(MSBuildToolsPath)\Microsoft.CSharp.targets"" />
  <!-- To modify your build process, add your task inside one of the targets below and uncomment it. 
       Other similar extension points exist, see Microsoft.Common.targets.
  <Target Name=""BeforeBuild"">
  </Target>
  <Target Name=""AfterBuild"">
  </Target>
  -->
</Project>");

            var str = result.ToString();
            return str;
        }

        private static readonly string[] configs = new string[]
        {
            "Debug|Any CPU.ActiveCfg",
            "Debug|Any CPU.Build.0",
            "Release|Any CPU.ActiveCfg",
            "Release|Any CPU.Build.0",
        };

        public static string GenerateSolutionText(IEnumerable<ProjectDescriptor> projects)
        {
            StringBuilder result = new StringBuilder(
    @"
Microsoft Visual Studio Solution File, Format Version 12.00
Global
    GlobalSection(SolutionConfigurationPlatforms) = preSolution
        Release|Any CPU = Release|Any CPU
        Release|Win32 = Release|Win32
        Other|Any CPU = Other|Any CPU
        Other|Win32 = Other|Win32
    EndGlobalSection
EndGlobal
");
            var solutionGuid = Guid.NewGuid().ToString();
            //Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = 
            //    "ReachingTypeAnalysis", "ReachingTypeAnalysis\ReachingTypeAnalysis.csproj", "{31E6307F-30AF-46D4-B9B5-195C54408204}"
            //EndProject
            foreach (var project in projects)
            {
                result.Append("Project(\"{");
                result.Append(solutionGuid);
                result.Append("\"}");
                result.AppendFormat("\") = \"{0}\", \"{1}\", \"",
                    project.Name, project.AbsolutePath);
                result.Append("{");
                result.Append(project.ProjectGuid);
                result.Append("}\"\n");
                result.Append("EndProject\n");
            }

            result.Append(@"
GlobalSection(SolutionConfigurationPlatforms) = preSolution
    Debug|Any CPU = Debug|Any CPU
    Release|Any CPU = Release|Any CPU
EndGlobalSection
GlobalSection(ProjectConfigurationPlatforms) = postSolution");
            foreach (var project in projects)
            {
                foreach (var config in configs)
                {
                    result.Append("{");
                    result.AppendFormat("{0}", project.ProjectGuid);
                    result.Append("}.{");
                    result.Append(config);
                    result.AppendFormat("= {0}\n", config);
                }
            }
            result.Append(@"EndGlobalSection
GlobalSection(SolutionProperties) = preSolution
    HideSolutionNode = FALSE
EndGlobalSection");

            var str = result.ToString();
            Trace.TraceInformation("sln file:");
            Trace.TraceInformation(str);

            return str;
        }

        /// <summary>
        /// Write the entire solution structure to disk.
        /// </summary>
        /// <param name="solutionPath">Where to save the sln file</param>
        /// <param name="projects">Projects to include</param>
        public static Solution GenerateSolutionWithProjects(string solutionPath, IEnumerable<ProjectDescriptor> projects)
        {
            Contract.Assert(solutionPath != null);
            var text = GenerateSolutionText(projects);
            Contract.Assert(text != null);

            foreach (var project in projects)
            {
                var csprojText = CreateProjectFile(project);
                Contract.Assert(csprojText != null);
                Trace.TraceInformation("Writing project file to to {0}", project.AbsolutePath);
                File.WriteAllText(project.AbsolutePath, csprojText);
            }

            File.WriteAllText(solutionPath, text);

            return Utils.ReadSolution(solutionPath);
        }
    }
}