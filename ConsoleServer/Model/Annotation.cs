//---------------------------------------------------------------------------------------------------------------------
// <copyright company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//---------------------------------------------------------------------------------------------------------------------

namespace Microsoft.VisualStudio.Services.WebApi
{
    public struct Range
    {
        public int startLineNumber;
        public int startColumn;
        public int endLineNumber;
        public int endColumn;
    }

    public abstract class Annotation
    {
        public string symbolId;
        public string symbolType;
        public string refType;
        public string declAssembly;
        public string label;
        public string hover;
        public Range range;
    }

    public class DeclarationAnnotation : Annotation
    {
        public string depth;
        public string glyph;
    }

    public class ReferenceAnnotation : Annotation
    {
        public string declFile;
    }

    public struct SymbolReference
    {
        public string refType;
        public string tref;
        public Range trange;
        public string preview;
    }

    public class SymbolReferenceCount
    {
        public string label;
        public string fullName;
        public int count;
    }
}
