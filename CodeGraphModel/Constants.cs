//---------------------------------------------------------------------------------------------------------------------
// <copyright company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//---------------------------------------------------------------------------------------------------------------------

namespace CodeGraphModel
{
    using System;

    public static class LocationIds
    {
        public const string EntitiesResourceIdString = "07eba98c-cc27-4717-b1c1-c1d54f445d2f";
        public static readonly Guid EntitiesResourceId = new Guid(EntitiesResourceIdString);

        public const string AnnotationResourceIdString = "37394cc5-406e-4185-b502-26308df0c67f";
        public static readonly Guid AnnotationResourceId = new Guid(AnnotationResourceIdString);

        public const string ReferencesResourceIdString = "4d356b5b-b646-4d7b-bc0a-a4b2fe9dbdd5";
        public static readonly Guid ReferencesResourceId = new Guid(ReferencesResourceIdString);

        public static readonly Guid MonacoAnnotationResourceId = new Guid("1ef8d8e0-43c7-433b-a7d0-0e46698282d6");
        public static readonly Guid MonacoReferencesResourceId = new Guid("3dd47cc9-fc74-40f1-b18b-ec259bf01c69");
    }

    public static class WebConstants
    {
        public const string AreaId = "3CAC5542-DC40-4AD2-B33E-59D13332537B";
        internal const string InstanceType = "00000021-0000-8888-8000-000000000000";
        public const string Area = "";
        public const string AreaName = "graph";
        public const string EntitiesResourceName = "entities";
        public const string AnnotationsResourceName = "annotations";
        public const string ReferencesResourceName = "references";
    }

    public static class EntityType
    {
        public const string Assembly = "Assembly";
        public const string File = "File";
        public const string Symbol = "Symbol";
        public const string CompoundSymbol = "CompoundSymbol";
    }

    public static class SymbolType
    {
        public const string Class = "Class";
        public const string Interface = "Interface";
        public const string Struct = "Struct";
        public const string Method = "Method";
    }
}
