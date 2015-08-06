//---------------------------------------------------------------------------------------------------------------------
// <copyright company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//---------------------------------------------------------------------------------------------------------------------

namespace CodeGraphModel
{
    using System.Runtime.Serialization;

    [DataContract]
    public class Entity
    {
        [DataMember]
        public string UId { get; set; }

        [DataMember]
        public string Type { get; set; }

        public virtual bool IsValid()
        {
            // All types of entities is an Entity.
            return true;
        }
    }
}
