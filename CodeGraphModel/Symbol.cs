//---------------------------------------------------------------------------------------------------------------------
// <copyright company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//---------------------------------------------------------------------------------------------------------------------

namespace CodeGraphModel
{
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    [DataContract]
    public class Symbol : Entity
    {
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

        public override string ToString()
        {
            return string.Format("[Name: {0}, Type: {1}, UId: {2}]", Name, Type, UId);
        }

        public override bool IsValid()
        {
            return this.Type == EntityType.Symbol;
        }
    }

    public class SymbolResponse
    {
        [DataMember]
        public string uid;

        [DataMember]
        public string name;

        [DataMember]
        public string type;

        [DataMember]
        public string fileUid;

        [DataMember]
        public int lineStart;

        [DataMember]
        public int lineEnd;

        [DataMember]
        public int columnStart;

        [DataMember]
        public int columnEnd;
    }
}