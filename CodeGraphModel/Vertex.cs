
namespace CodeGraphModel
{
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    [DataContract]
    public class Vertex : Entity
    {
        #region From File

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

        #endregion

        #region From Symbol

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string SymbolType { get; set; }

        [DataMember]
        public bool IsExternal { get; set; }

        [DataMember]
        public int LineStart { get; set; }

        [DataMember]
        public int LineEnd { get; set; }

        [DataMember]
        public int ColumnStart { get; set; }

        [DataMember]
        public int ColumnEnd { get; set; }

        [DataMember]
        public string FileUId { get; set; }

        [DataMember]
        public List<SymbolReference> References { get; set; }

        #endregion

    }
}
