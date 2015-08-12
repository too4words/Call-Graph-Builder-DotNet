//---------------------------------------------------------------------------------------------------------------------
// <copyright company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//---------------------------------------------------------------------------------------------------------------------

using System;

namespace CodeGraphModel
{
	[Serializable]
	public struct Range
    {
        public int startLineNumber;
        public int startColumn;
        public int endLineNumber;
        public int endColumn;
    }

	[Serializable]
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

	[Serializable]
	public class DeclarationAnnotation : Annotation
    {
        public string depth;
        public string glyph;
    }

	[Serializable]
	public class ReferenceAnnotation : Annotation
    {
		public string declarationId;
		public string declFile;
    }

	[Serializable]
    public class SymbolReference
    {
        public string refType;
        public string tref;
        public Range trange;
        public string preview;
    }

	[Serializable]
	public class SymbolReferenceCount
    {
        public string label;
        public string fullName;
        public int count;
    }
}
