// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.IO;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using System.Linq;
using System.IO.Compression;

namespace ReachingTypeAnalysis
{
    internal enum ProjectType {
        Library,
        Executable,
    }

    public class ProjectDescriptor
    {
        internal string Name { get; set; }
        internal string ProjectGuid { get; set; }
        internal string AbsolutePath { get; set; }
        internal IEnumerable<ProjectDescriptor> Dependencies { get; set; }
        internal IEnumerable<string> Files { get; set; }
        internal ProjectType Type { get; set; }
    }


    /// <summary>
    /// Support for artificially generating solutions, both in memory and on disk. 
    /// </summary>

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
    <OutputType>");
            result.AppendFormat(@"{0}</OutputType>
    <AppDesignerFolder>Properties</AppDesignerFolder>
    ", project.Type == ProjectType.Library ? "Library" : "Exe");
            result.AppendFormat(
@"<RootNamespace>{0}</RootNamespace>
    <AssemblyName>{0}{1}</AssemblyName>", TestConstants.TemporaryNamespace, project.Name);
                result.Append(
@"<TargetFrameworkVersion>v4.5</TargetFrameworkVersion>
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
            result.Append("</ItemGroup>\n");
            if (project.Dependencies.Count() > 0)
            {
                result.Append("<ItemGroup>\n");
                foreach (var dependency in project.Dependencies)
                {
                    // skip ourselves to avoid cycles
                    if (!dependency.Name.Equals(project.Name))
                    {
                        result.AppendFormat(@"    <ProjectReference Include = ""{0}"">",
                            Path.GetDirectoryName(dependency.AbsolutePath).Equals(Path.GetDirectoryName(project.AbsolutePath)) ?
                            Path.GetFileName(dependency.AbsolutePath) : dependency.AbsolutePath
                            );
                        result.Append("\n");
                        result.AppendFormat(@"        <Project>{0}</Project>", "{" + dependency.ProjectGuid + "}");
                        result.Append("\n");
                        result.AppendFormat(@"        <Name>{0}</Name>", dependency.Name);
                        result.Append("\n");
                        result.Append("    </ProjectReference>");
                        result.Append("\n");
                    }
                }
                result.Append("</ItemGroup>\n");
            }
            result.Append(
@"<Import Project=""$(MSBuildToolsPath)\Microsoft.CSharp.targets"" />
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
                result.Append("}");
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
                    result.Append("}.");
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
        /// <param name="solutionFileName">Where to save the sln file</param>
        /// <param name="projects">Projects to include</param>
        public static Solution GenerateSolutionWithProjects(string solutionFileName, IEnumerable<ProjectDescriptor> projects, string baseDirectory = null, bool clean = true)
        {
            Contract.Assert(solutionFileName != null);
            Contract.Assert(!solutionFileName.Contains(Path.PathSeparator));
            if (baseDirectory == null)
            {
                baseDirectory = Directory.GetCurrentDirectory();
            }

            var text = GenerateSolutionText(projects);
            Contract.Assert(text != null);
            if (clean)
            {
                if (Directory.Exists(Path.Combine(baseDirectory, TestConstants.TestDirectory)))
                {
                    Directory.Delete(Path.Combine(baseDirectory, TestConstants.TestDirectory), true);
                }
            }

            if (!Directory.Exists(Path.Combine(baseDirectory, TestConstants.TestDirectory)))
            {
                Directory.CreateDirectory(Path.Combine(baseDirectory, TestConstants.TestDirectory));
            }

            foreach (var project in projects)
            {
                var csprojText = CreateProjectFile(project);
                Contract.Assert(csprojText != null);
                var csProjFile = Path.Combine(baseDirectory, project.AbsolutePath);
                Trace.TraceInformation("Writing project file to to {0}", csProjFile);
                File.WriteAllText(csProjFile, csprojText);
            }

            File.WriteAllText(Path.Combine(baseDirectory, solutionFileName), text);

            return Utils.ReadSolution(solutionFileName);
        }

        /// <summary>
        /// Creates a directory structure for a solution and zips it up. 
        /// </summary>
        /// <param name="solutionFileName"></param>
        /// <param name="projects"></param>
        /// <returns>Path to the zip file</returns>
        public static string GenerateSolutionWithProjectsAsAZip(string solutionFileName, IEnumerable<ProjectDescriptor> projects, bool deleteTemp = true)
        {
            var solution = GenerateSolutionWithProjects(solutionFileName, projects, "temp", false);
            Contract.Assert(Directory.Exists("temp"));
            Contract.Assert(Directory.Exists(Path.Combine("temp", TestConstants.TestDirectory)));

            var destinationFile = System.IO.Path.GetTempPath() + Guid.NewGuid().ToString() + ".zip";

            ZipFile.CreateFromDirectory("temp", destinationFile);
            Trace.TraceInformation("Wrote the archive to {0}", destinationFile);
            if (deleteTemp)
            {
                Directory.Delete("temp", true);
            }

            return destinationFile;
        }
    }
}