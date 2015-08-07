//---------------------------------------------------------------------------------------------------------------------
// <copyright company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//---------------------------------------------------------------------------------------------------------------------

namespace CodeGraphModel
{
    using System.Collections.Generic;
    using System.Runtime.Serialization;

	public class BuildInfo
	{
		public string TeamProjectName { get; set; }
		public string RepositoryName { get; set; }
		public string BranchName { get; set; }
		public string VersionName { get; set; }
		public string BuildInfoFileName { get; set; }

		public BuildInfo()
		{
			this.TeamProjectName = "SYSTEM_TEAMPROJECT";
			this.RepositoryName = "BUILD_REPOSITORY_NAME";
			this.BranchName = "BUILD_SOURCEBRANCH";
			this.VersionName = "BUILD_SOURCEVERSION";
			this.BuildInfoFileName = "build.json";
		}
	}

	/// <summary>
	/// Data type for File entity. File is the type used in graph database while FileReponse is the type used by REST API.
	/// Consider to merge these two types into one in the future.
	/// </summary>
	[DataContract]
    public class File : Entity
    {
        [DataMember]
        public string FilePath { get; set; }

        [DataMember]
        public string GitBranch { get; set; }

        [DataMember]
        public string GitRepository { get; set; }

        [DataMember]
        public List<DeclarationAnnotation> DeclAnnotations { get; set; }

        [DataMember]
        public List<ReferenceAnnotation> RefAnnotations { get; set; }

        public override string ToString()
        {
            return string.Format("{0},{1}", FilePath, UId);
        }

        public override bool IsValid()
        {
            return this.Type == EntityType.File;
        }
    }

    [DataContract]
    public class FileResponse
    {
        [DataMember]
        public string uid;

		[DataMember]
		public string assemblyname;

		[DataMember]
        public string filepath;

        [DataMember]
        public string repository;

        [DataMember]
        public string version;

        [DataMember]
        public List<DeclarationAnnotation> declarationAnnotation;

        [DataMember]
        public List<ReferenceAnnotation> referenceAnnotation;
    }
}
