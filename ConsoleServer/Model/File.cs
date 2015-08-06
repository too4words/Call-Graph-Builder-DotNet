//---------------------------------------------------------------------------------------------------------------------
// <copyright company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//---------------------------------------------------------------------------------------------------------------------

namespace Microsoft.VisualStudio.Services.WebApi
{
    using System.Collections.Generic;
    using System.Runtime.Serialization;

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
