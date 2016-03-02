// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using CodeGraphModel;

namespace ReachingTypeAnalysis
{
	public static class TestConstants
	{
		public const string ProjectName = "MyProject";
		public const string ProjectAssemblyName = "MyProject";
		public const string DocumentName = "MyFile.cs";
		public const string DocumentPath = @"C:\MyFile.cs";
		public const string SolutionPath = @"test.sln";
		public const string TestDirectory = "test";
		public const string TemporarySolutionDirectory = "temp";
		public const string TemporaryNamespace = "Temporary";
	}

	public static class AnalysisConstants
	{
		public const string StreamProvider = "SimpleMessageStreamProvider";
		public const string StreamNamespace = "EffectsStream";
		// {32B2336F-BDC9-4F75-AEBE-A97FE966E306}
		public const string StreamGuidFormat = "{{32B2336F-BDC9-4F75-AEBE-A97FE966{0:X4}}}";
		public const int StreamCount = 100;
		public const int DispatcherIdleThreshold = 30 * 1000; // milliseconds
		public const int DispatcherTimerPeriod = 5 * 1000; // milliseconds
		public const int WaitForTerminationDelay = 5 * 1000; // milliseconds
	}

	[Serializable]
	public enum AnalysisRootKind
	{
		MainMethods,
		TestMethods,
		PublicMethods,
		RootMethods,
        //Default = RootMethods
        Default = MainMethods
    }

	[Serializable]
	public enum EntityGrainStatus
	{
		Busy,
		Ready
	}

	[Serializable]
	public enum ModificationKind
	{
		MethodAdded, MethodRemoved, MethodUpdated
	}

	[Serializable]
	public class MethodCalleesInfo
	{
		public ISet<MethodDescriptor> ResolvedCallees { get; private set; }

		//public ISet<PropGraphNodeDescriptor> UnknownCallees { get; private set; }

		public bool HasUnknownCallees { get; private set; }

		//public MethodCalleesInfo(IEnumerable<MethodDescriptor> resolvedCallees, IEnumerable<PropGraphNodeDescriptor> unknownCallees)
		//{
		//	this.ResolvedCallees = new HashSet<MethodDescriptor>(resolvedCallees);
		//	this.UnknownCallees = new HashSet<PropGraphNodeDescriptor>(unknownCallees);
		//}

		public MethodCalleesInfo(IEnumerable<MethodDescriptor> resolvedCallees, bool hasUnknownCallees)
		{
			this.ResolvedCallees = new HashSet<MethodDescriptor>(resolvedCallees);
			this.HasUnknownCallees = hasUnknownCallees;
		}
	}

    [Serializable]
    public class MethodModification
    {
        public ModificationKind ModificationKind { get; private set; }
        public MethodDescriptor MethodDescriptor { get; private set; }

        public MethodModification(MethodDescriptor methodDescriptor, ModificationKind modificationKind)
        {
            this.MethodDescriptor = methodDescriptor;
            this.ModificationKind = modificationKind;
        }

        public override int GetHashCode()
        {
            return this.MethodDescriptor.GetHashCode() ^
                   this.ModificationKind.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            var other = obj as MethodModification;
            return other != null &&
                   this.MethodDescriptor.Equals(other.MethodDescriptor) &&
                   this.ModificationKind == other.ModificationKind;
        }

        public override string ToString()
        {
            return string.Format("{0}: {1}", this.ModificationKind, this.MethodDescriptor);
        }
    }

	[Serializable]
	public enum ParameterKind
	{
		In,
		Out,
		Ref
	}

	[Serializable]
	public class ParameterDescriptor
	{
		public ParameterKind Kind { get; protected set; }
		public TypeDescriptor Type { get; protected set; }

		public ParameterDescriptor(TypeDescriptor type, ParameterKind kind = ParameterKind.In)
		{
			this.Kind = kind;
			this.Type = type;
		}

		public override int GetHashCode()
		{
			return this.Kind.GetHashCode() ^ this.Type.GetHashCode();
		}

		public override bool Equals(object obj)
		{
			var other = obj as ParameterDescriptor;
			if (other == null) return false;

			var kEq = this.Kind == other.Kind;
			var tEq = this.Type.Equals(other.Type);

			return kEq && tEq;
		}

		public bool EqualsIgnoringTypeArguments(object obj)
		{
			var other = obj as ParameterDescriptor;
			if (other == null) return false;

			var kEq = this.Kind == other.Kind;
			var tEq = this.Type.EqualsIgnoringTypeArguments(other.Type);

			return kEq && tEq;
		}

		public override string ToString()
		{
			return string.Format("[{0}] {1}", this.Kind, this.Type);
		}

		internal string Marshall()
		{
			var type = this.Type.Marshall();
			var kind = this.Kind.ToString().First();
            var result = string.Format("{0}{1}", kind, type);

			return result;
		}

		internal static ParameterDescriptor DeMarshall(string parameterString)
		{
			var kindChar = parameterString.First();
			ParameterKind kind;

			switch (kindChar)
			{
				case 'I': kind = ParameterKind.In; break;
				case 'O': kind = ParameterKind.Out; break;
				case 'R': kind = ParameterKind.Ref; break;
				default: throw new Exception("Unknown ParemeterKind");
			}

			var typeString = parameterString.Remove(0, 1);
			var type = TypeDescriptor.DeMarshall(typeString);
			var result = new ParameterDescriptor(type, kind);

			return result;
        }
	}

	/// <summary>
	/// This is a string represenation of a method designed to be 
	/// put as keys in Dictionaries and used for comparison.
	/// </summary>
	[Serializable]
    public class MethodDescriptor
    {
        protected string name;

        public TypeDescriptor ContainerType { get; protected set; }
        public string MethodName { get; protected set; }
		public int TypeParametersCount { get; protected set; }
        public IList<ParameterDescriptor> Parameters { get; protected set; }
        public TypeDescriptor ReturnType { get; protected set; }
        public bool IsStatic { get; protected set; }
		public bool IsVirtual { get; protected set; }

		public string ClassName
        {
            get { return this.ContainerType.ClassName; }
        }

        public string NamespaceName
        {
            get { return this.ContainerType.NamespaceName; }
        }

        public string Name
        {
            get
            {
                if (string.IsNullOrEmpty(name))
                {
                    var qualifiedName = new List<string>();

                    if (!string.IsNullOrEmpty(this.NamespaceName))
                    {
                        qualifiedName.Add(this.NamespaceName);
                    }

                    qualifiedName.Add(this.ClassName);
                    qualifiedName.Add(this.MethodName);
                    name = string.Join(".", qualifiedName);
                }

                return name;
            }
        }

        public TypeDescriptor ThisType
        {
            get { return !IsStatic ? ContainerType : null; }
        }

        // I include this just to simplify 
        public virtual MethodDescriptor BaseDescriptor
        {
            get { return this; }
            protected set { }
        }

        public bool IsAnonymousDescriptor { get; protected set; }

        public MethodDescriptor() : this("", "")
        {
        }

        public MethodDescriptor(TypeDescriptor typeDescriptor, string methodName,
                                    bool isStatic = false,
									bool isVirtual = false,
                                    IEnumerable<ParameterDescriptor> parameters = null,
									int typeParametersCount = 0,
                                    TypeDescriptor returnType = null)
        {
            this.ContainerType = typeDescriptor;
            //this.NamespaceName = typeDescriptor.Namespace;
            //this.ClassName = typeDescriptor.ClassName;
            this.MethodName = methodName;
            this.IsStatic = isStatic;
			this.IsVirtual = isVirtual;
            this.ReturnType = returnType;
            this.IsAnonymousDescriptor = false;
			this.TypeParametersCount = typeParametersCount;

			if (parameters != null)
			{
				this.Parameters = new List<ParameterDescriptor>(parameters);
			}
        }

        public MethodDescriptor(string namespaceName, string className, string methodName,
                                    bool isStatic = false,
									bool isVirtual = false,
									IEnumerable<ParameterDescriptor> parameters = null,
									int typeParametersCount = 0,
                                    TypeDescriptor returnType = null)
        {
            this.ContainerType = new TypeDescriptor(namespaceName, className, isReferenceType: true);
            //this.NamespaceName = namespaceName;
            //this.ClassName = className;
            this.MethodName = methodName;
            this.IsStatic = isStatic;
			this.IsVirtual = isVirtual;
			this.ReturnType = returnType;
            this.IsAnonymousDescriptor = false;
			this.TypeParametersCount = typeParametersCount;

            if (parameters != null)
            {
                this.Parameters = new List<ParameterDescriptor>(parameters);
            }
        }

        public MethodDescriptor(string className, string methodName, bool isStatic = false)
            : this("", className, methodName, isStatic)
        {
            //this.Parameters = new List<TypeDescriptor>();
        }

        //public bool SameAsMethodSymbol(IMethodSymbol method)
        //{
        //    if (method == null)
        //    {
        //        return false;
        //    }
        //    else
        //    {
        //        bool result = method.Name.Equals(this.MethodName);
        //        result = result && method.ContainingType.Name.Equals(this.ClassName);
        //        result = result & (this.NamespaceName == null || method.ContainingNamespace.Name.Equals(this.NamespaceName));

        //        return result;
        //    }
        //}

        public MethodDescriptor(MethodDescriptor original)
        {
            this.ContainerType = original.ContainerType;
            //this.ClassName = original.ClassName;
            //this.NamespaceName = original.NamespaceName;
            this.MethodName = original.MethodName;
            this.Parameters = original.Parameters;
			this.TypeParametersCount = original.TypeParametersCount;
            this.ReturnType = original.ReturnType;
            this.IsStatic = original.IsStatic;
			this.IsVirtual = original.IsVirtual;
            // this.ThisType = original.ThisType;
        }

        public override bool Equals(object obj)
        {
            var md = obj as MethodDescriptor;
            if (md == null) return false;

            //var nEq = this.NamespaceName == "" || md.NamespaceName == "" || this.NamespaceName.Equals(md.NamespaceName);
            //var cEq = this.ClassName.Equals(md.ClassName);
            var tEq = this.ContainerType.Equals(md.ContainerType);
            var mEq = this.MethodName.Equals(md.MethodName);
            var staticEq = this.IsStatic == md.IsStatic;
            var pEq = this.Parameters == null || md.Parameters == null || this.Parameters.SequenceEqual(md.Parameters);
			var tpEq = this.TypeParametersCount == md.TypeParametersCount;

            return /*nEq && cEq*/ tEq && mEq && staticEq && pEq && tpEq;
        }

		public bool EqualsIgnoringTypeArguments(object obj)
		{
			var md = obj as MethodDescriptor;
			if (md == null) return false;

			//var nEq = this.NamespaceName == "" || md.NamespaceName == "" || this.NamespaceName.Equals(md.NamespaceName);
			//var cEq = this.ClassName.Equals(md.ClassName);
			var tEq = this.ContainerType.EqualsIgnoringTypeArguments(md.ContainerType);
			var mEq = this.MethodName.Equals(md.MethodName);
			var staticEq = this.IsStatic == md.IsStatic;
			//var pEq = this.Parameters == null || md.Parameters == null || this.Parameters.SequenceEqual(md.Parameters);
			var pEq = this.Parameters == null || md.Parameters == null || this.Parameters.Count == md.Parameters.Count;
			var tpEq = this.TypeParametersCount == md.TypeParametersCount;

			if (pEq)
			{
				for (var i = 0; i < this.Parameters.Count; ++i)
				{
					var thisParam = this.Parameters[i];
					var mdParam = md.Parameters[i];

					if (thisParam.Type.Kind == SerializableTypeKind.TypeParameter)
					{
						var typeArgIndex = this.ContainerType.TypeArguments.IndexOf(thisParam.Type);

						if (typeArgIndex > -1)
						{
							var typeArg = md.ContainerType.TypeArguments[typeArgIndex];

							pEq = pEq && typeArg.EqualsIgnoringTypeArguments(mdParam.Type);
						}
					}
					else
					{
						pEq = pEq && thisParam.EqualsIgnoringTypeArguments(mdParam);
					}
				}
			}

			return /*nEq && cEq*/ tEq && mEq && staticEq && pEq && tpEq;
		}

		//private static bool CompareParameters(IList<TypeDescriptor> params1, IList<TypeDescriptor> params2)
		//{
		//    if(params1.Count()!=params2.Count()) return false;
		//    for(var i = 0; i< params1.Count(); i++)
		//    {
		//        if(!params1[i].Equals(params2[i]))
		//            return false;
		//    }
		//    return true;
		//}

		public override int GetHashCode()
        {
            //return NamespaceName.GetHashCode() + ClassName.GetHashCode() + MethodName.GetHashCode();
            return this.ContainerType.GetHashCode() + this.MethodName.GetHashCode();
        }

        public override string ToString()
        {
            return this.Name;
        }

        public virtual string Marshall()
        {
            var result = new StringBuilder();

            //result.Append(this.NamespaceName);
            //result.Append("+");
            //result.Append(this.ClassName);
            result.Append(this.ContainerType.Marshall());
            result.Append("-");
            result.Append(this.MethodName);
            result.Append("-");
            result.Append(this.IsStatic);
			result.Append("-");
			result.Append(this.IsVirtual);
			result.Append("-");
			result.Append(this.TypeParametersCount);
			result.Append("-");

			if (this.Parameters != null && this.Parameters.Count > 0)
            {				
				result.Append(this.Parameters.Count);

                foreach (var parameterType in this.Parameters)
                {
                    result.Append("-");
                    result.Append(parameterType.Marshall());
                }
            }
			else
			{
				result.Append(0);
			}

            return result.ToString();
        }

        public static MethodDescriptor DeMarsall(string md)
        {
            var anonymousMD = "";
            var i = md.IndexOf(':');

            if (i >= 0)
            {
                anonymousMD = md.Substring(0, i);
                md = md.Substring(i + 1);
            }

            var methodDescriptor = ParseMethodDescriptor(md);

            if (anonymousMD.Length > 0)
            {
                var anonymousMDContent = ParseMethodDescriptor(anonymousMD);
                return new AnonymousMethodDescriptor(methodDescriptor, anonymousMDContent);
            }

            return methodDescriptor;
        }

		private static MethodDescriptor ParseMethodDescriptor(string md)
		{
			var tokens = md.Split('-');
			var containerType = TypeDescriptor.DeMarshall(tokens[0]);
			//var namespaceName = tokens[0];
			//var className = tokens[1];
			var methodName = tokens[1];
			var isStatic = Convert.ToBoolean(tokens[2]);
			var isVirtual = Convert.ToBoolean(tokens[3]);
			var typeParametersCount = Convert.ToInt32(tokens[4]);
			var parametersCount = Convert.ToInt32(tokens[5]);
			var parameters = new List<ParameterDescriptor>();

			for (var i = 0; i < parametersCount; ++i)
			{
				var parameterString = tokens[6 + i];
				var parameterDescriptor = ParameterDescriptor.DeMarshall(parameterString);

				parameters.Add(parameterDescriptor);
			}

			var methodDescriptor = new MethodDescriptor(containerType, methodName, isStatic, isVirtual, parameters, typeParametersCount);
			return methodDescriptor;
		}
    }

    [Serializable]
    public class AnonymousMethodDescriptor : MethodDescriptor
    {
        public override MethodDescriptor BaseDescriptor { get; protected set; }

        public AnonymousMethodDescriptor(MethodDescriptor baseMethodDescriptor, MethodDescriptor anonymousMethodDescriptor)
            : base(anonymousMethodDescriptor)
        {
            this.BaseDescriptor = baseMethodDescriptor;
            this.MethodName = "Anonymous";
            this.IsAnonymousDescriptor = true;
        }

        public override bool Equals(object obj)
        {
			var other = obj as AnonymousMethodDescriptor;
            return other != null && this.BaseDescriptor.Equals(other.BaseDescriptor) && this.MethodName.Equals(other.MethodName);
        }

        public override int GetHashCode()
        {
            return this.BaseDescriptor.GetHashCode() + this.MethodName.GetHashCode();
        }

        public override string Marshall()
        {
            return base.Marshall() + ":" + this.BaseDescriptor.Marshall();
        }
    }

    [Serializable]
    public enum SerializableTypeKind
    {
        Class,
        Interface,
        Delegate,
        TypeParameter,
        Array,
        Struct,
        Module,
        Enum,
        Pointer,
        Dynamic,
        Error,
        Submission,
        Unknown,
        Undefined
    }

    [Serializable]
    public class TypeDescriptor
    {
        public bool IsReferenceType { get; private set; }
        public SerializableTypeKind Kind { get; private set; }
        public bool IsConcreteType { get; private set; }
        public string NamespaceName { get; private set; }
        public string ClassName { get; private set; }
        public string AssemblyName { get; private set; }
		public int TypeParametersCount { get; private set; }
		public IList<TypeDescriptor> TypeArguments { get; private set; }

		public TypeDescriptor(string namespaceName, string className, string assemblyName = TestConstants.ProjectAssemblyName, int typeParametersCount = 0, IEnumerable<TypeDescriptor> typeArguments = null, bool isReferenceType = true, SerializableTypeKind kind = SerializableTypeKind.Undefined, bool isConcrete = true)
        {
            this.NamespaceName = namespaceName;
            this.ClassName = className;
            this.IsReferenceType = isReferenceType;
            this.Kind = kind;
            this.IsConcreteType = isConcrete;
            this.AssemblyName = assemblyName;
			this.TypeParametersCount = typeParametersCount;

			if (typeArguments != null)
			{
				this.TypeArguments = new List<TypeDescriptor>(typeArguments);
			}
		}

        public TypeDescriptor(TypeDescriptor typeDescriptor, bool isConcrete = true)
        {
            this.NamespaceName = typeDescriptor.NamespaceName;
            this.ClassName = typeDescriptor.ClassName;
            this.IsReferenceType = typeDescriptor.IsReferenceType;
            this.Kind = typeDescriptor.Kind;
            this.AssemblyName = typeDescriptor.AssemblyName;
			this.TypeParametersCount = typeDescriptor.TypeParametersCount;
			this.TypeArguments = typeDescriptor.TypeArguments;
			this.IsConcreteType = isConcrete;
        }

        public string TypeName
        {
            get
            {
				var result = new StringBuilder();

				result.Append(this.ClassName);

				if (this.TypeArguments != null && this.TypeArguments.Count > 0)
				{
					var arguments = string.Join(",", this.TypeArguments.Select(arg => arg.TypeName));
					result.AppendFormat("<{0}>", arguments);
				}

                return result.ToString();
            }
        }

		public string MetadataTypeName
		{
			get
			{
				var result = new StringBuilder();

				result.Append(this.ClassName);

				if (this.TypeParametersCount > 0)
				{
					result.AppendFormat("`{0}", this.TypeParametersCount);
				}

				return result.ToString();
			}
		}

		public string QualifiedTypeName
		{
			get
			{
				var result = new StringBuilder();

				if (!string.IsNullOrEmpty(this.NamespaceName))
				{
					result.AppendFormat("{0}.", this.NamespaceName);
				}

				result.Append(this.TypeName);
				return result.ToString();
			}
		}

		public string QualifiedMetadataTypeName
		{
			get
			{
				var result = new StringBuilder();

				if (!string.IsNullOrEmpty(this.NamespaceName))
				{
					result.AppendFormat("{0}.", this.NamespaceName);
				}

				result.Append(this.MetadataTypeName);
				return result.ToString();
			}
		}

		public string FullTypeName
        {
            get { return string.Format("{0}: {1}", this.AssemblyName, this.QualifiedTypeName); }
        }

		public string FullMetadataTypeName
		{
			get { return string.Format("{0}: {1}", this.AssemblyName, this.QualifiedMetadataTypeName); }
		}

		// TODO: Fix the equals, but we need to resolve the default values
		public override bool Equals(object obj)
        {
            var td = (TypeDescriptor)obj;
            var eqKind = td.Kind.Equals(SerializableTypeKind.Undefined) ||
                          this.Kind.Equals(SerializableTypeKind.Undefined) ||
                          this.Kind.Equals(td.Kind);
            var eqRef = this.IsReferenceType == td.IsReferenceType;
            var eqConcrete = this.IsConcreteType == td.IsConcreteType;
			var eqTPC = this.TypeParametersCount == td.TypeParametersCount;
			var eqTA = this.TypeArguments == null || td.TypeArguments == null || this.TypeArguments.SequenceEqual(td.TypeArguments);

			return this.FullTypeName.Equals(td.FullTypeName)
                    //       && eqRef && eqConcrete
                    && eqKind && eqTPC && eqTA;
        }

		public bool EqualsIgnoringTypeArguments(object obj)
		{
			var td = (TypeDescriptor)obj;
			var eqKind = this.Kind.Equals(SerializableTypeKind.Undefined) ||
						  td.Kind.Equals(SerializableTypeKind.Undefined) ||
                          this.Kind.Equals(td.Kind);
			var eqRef = this.IsReferenceType == td.IsReferenceType;
			var eqConcrete = this.IsConcreteType == td.IsConcreteType;
			var eqTPC = this.TypeParametersCount == td.TypeParametersCount;

			return this.FullMetadataTypeName.Equals(td.FullMetadataTypeName)
					//       && eqRef && eqConcrete
					&& eqKind && eqTPC;
		}

		// TODO: Fix the equals, but we need to resolve the default values
		public override int GetHashCode()
        {
            return this.FullTypeName.GetHashCode()
                + (this.Kind.Equals(SerializableTypeKind.Undefined) ? 0 : this.Kind.GetHashCode());
        }

        public override string ToString()
        {
            return this.FullTypeName.ToString();
        }

        public bool IsDelegate
        {
            get { return this.Kind.Equals(SerializableTypeKind.Delegate); }
        }

        internal string Marshall()
        {
            var result = new StringBuilder();

            result.Append(this.AssemblyName);
            result.Append("=");
            result.Append(this.NamespaceName);
            result.Append("=");
            result.Append(this.ClassName);
			result.Append("=");
			result.Append(this.TypeParametersCount);
			result.Append("=");

			if (this.TypeArguments != null && this.TypeArguments.Count > 0)
			{
				result.Append(this.TypeArguments.Count);				

				foreach (var argument in this.TypeArguments)
				{
					result.Append("=");
					result.Append(argument.Marshall());
				}
			}
			else
			{
				result.Append(0);
			}

			return result.ToString();
        }

        internal static TypeDescriptor DeMarshall(string typeString)
        {
			var index = 0;
			var tokens = typeString.Split('=');
			var typeDescriptor = DeMarshall(tokens, ref index);
			return typeDescriptor;
        }

		private static TypeDescriptor DeMarshall(string[] tokens, ref int index)
		{
			var assemblyName = tokens[index++];
			var namespaceName = tokens[index++];
			var className = tokens[index++];
			var typeParametersCount = Convert.ToInt32(tokens[index++]);
			var typeArgumentsCount = Convert.ToInt32(tokens[index++]);
			var typeArguments = new List<TypeDescriptor>();

			for (var i = 0; i < typeArgumentsCount; ++i)
			{
				var argument = DeMarshall(tokens, ref index);
				typeArguments.Add(argument);
			}

			var typeDescriptor = new TypeDescriptor(namespaceName, className, assemblyName, typeArgumentsCount, typeArguments, true, SerializableTypeKind.Undefined, true);
			return typeDescriptor;
		}

	}

	[Serializable]
    public class LocationDescriptor
    {
        public string FilePath { get; private set; }
        public Range Range { get; private set; }
        //public Location Location { get; private set; }
        public int InMethodOrder { get; private set; }
        //public LocationDescriptor(Location location)
        //{
        //    Contract.Assert(location != null);
        //    this.Location = location;
        //    InMethodOrder = 0; // need to search for the statement # in that location
        //}

        public int Location
        {
            get { return InMethodOrder; }
        }
        public LocationDescriptor(int inMethodOrder, Range range, string filePath)
        {
            this.InMethodOrder = inMethodOrder;
            this.Range = range;
            this.FilePath = filePath;
        }

        public override string ToString()
        {
            return this.Location.ToString();
        }
        public override bool Equals(object obj)
        {
            LocationDescriptor locRef = (LocationDescriptor)obj;
            return this.Location.Equals(locRef.Location);
        }
        public override int GetHashCode()
        {
            return this.Location.GetHashCode();
        }
    }

    [Serializable]
    public class AnalysisCallNodeAdditionalInfo
    {
        public string DisplayString { get; private set; }
        public string StaticMethodDeclarationPath { get; private set; }
        public MethodDescriptor StaticMethodDescriptor { get; private set; }

        public AnalysisCallNodeAdditionalInfo(MethodDescriptor staticMethodDescriptor, string staticMethodDeclarationPath, string displayString)
        {
            this.StaticMethodDescriptor = staticMethodDescriptor;
            this.StaticMethodDeclarationPath = staticMethodDeclarationPath;
            this.DisplayString = displayString;
        }
    }

    /// <summary>
    /// This is essentially a node in the propagation graph 
    /// It represents either a variable, field, parameter or even a method invocation (that are specials)
    /// Currently they conrtain information about the corresponding Roslyn symbol they represent 
    /// But the idea is to get rid of roslyn into (maybe keeping only the syntax expression they denote)
    /// </summary> 
    [Serializable]
    public abstract class PropGraphNodeDescriptor
    {
        public string Name { get; private set; }
        public TypeDescriptor Type { get; private set; }

        protected PropGraphNodeDescriptor(string name, TypeDescriptor declaredType)
        {
            this.Type = declaredType;
            this.Name = name;
        }

        public override bool Equals(object obj)
        {
            var analysisNode = (obj as PropGraphNodeDescriptor);
            return (analysisNode != null && analysisNode.Name.Equals(this.Name)
                    && analysisNode.Type.Equals(this.Type));
        }

        public override int GetHashCode()
        {
            return this.Name.GetHashCode() + this.Type.GetHashCode();
        }

        public override string ToString()
        {
            return string.Format("{0}: {1}", this.Name, this.Type);
            //string tString = "Dummy";
            //if (this.DeclaredType != null)
            //    tString = this.DeclaredType.TypeName;
            //string lString = "";
            //if (this.LocationReference != null)
            //    lString = "@" + this.LocationReference.ToString();
            //string eString = "";
            //if (this.Expression != null)
            //    eString = this.Expression.ToString();
            //string eSymbol = "";
            //if (this.Symbol != null && (this.Symbol.Kind == SymbolKind.Field))
            //    eSymbol = this.Symbol.ContainingType.Name.ToString() + ".";

            //return eSymbol + eString + lString + ":" + tString;
        }

        //public static AnalysisNode Define(TypeDescriptor declaredType)
        //{
        //	if (Utils.IsTypeForAnalysis(declaredType))
        //	{
        //		return new AnalysisNode(declaredType);
        //	}
        //	else
        //	{
        //		return null;
        //	}
        //}

        //internal SyntaxNodeOrToken Expression { get; private set; }
        //internal VariableDescriptor Symbol { get; private set; }

    }

    [Serializable]
    public class VariableNode : PropGraphNodeDescriptor
    {
        public VariableNode(string name, TypeDescriptor declaredType) :
            base(name, declaredType)
        {

        }
    }

    [Serializable]
    public class ParameterNode : VariableNode
    {
        public int Position { get; private set; }
        public ParameterNode(string name, int position, TypeDescriptor declaredType) :
            base(name, declaredType)
        {
            this.Position = position;
        }
        public override string ToString()
        {
            return base.ToString() + "_" + Position.ToString();
        }
    }

    [Serializable]
    public class ThisNode : VariableNode
    {
        public ThisNode(TypeDescriptor declaredType) :
            base("this", declaredType)
        {

        }
    }

    [Serializable]
    public class UnsupportedNode : PropGraphNodeDescriptor
    {
        public UnsupportedNode(TypeDescriptor declaredType) :
            base("unsupported", declaredType)
        {

        }
    }

    [Serializable]
    public class ReturnNode : VariableNode
    {
        public ReturnNode(TypeDescriptor declaredType) :
            base("return", declaredType)
        {
        }
    }


    [Serializable]
    public class FieldNode : VariableNode
    {
        public string ClassName { get; private set; }
        public string Field { get; private set; }
        public FieldNode(string className, string fieldName, TypeDescriptor declaredType)
            : base(string.Format("{0}.{1}", className, fieldName), declaredType)
        {
            this.ClassName = className;
            this.Field = fieldName;
        }
    }

    [Serializable]
    public class DelegateVariableNode : VariableNode
    {
        public DelegateVariableNode(string name, TypeDescriptor declaredType)
            : base(name, declaredType)
        { }
        public override string ToString()
        {
            return "delegate: " + base.ToString();
        }
    }

    [Serializable]
    public class PropertyVariableNode : VariableNode
    {
        AnalysisCallNode ProperyMethod { get; set; }
        public PropertyVariableNode(string name, TypeDescriptor declaredType,
            AnalysisCallNode propertyMethod)
            : base(name, declaredType)
        {
            this.ProperyMethod = propertyMethod;
        }
        public override string ToString()
        {
            return "property: " + base.ToString();
        }
    }

    [Serializable]
    public class AnalysisCallNode : PropGraphNodeDescriptor
    {
        public AnalysisCallNodeAdditionalInfo AdditionalInfo { get; private set; }
        public LocationDescriptor LocationDescriptor { get; private set; }
        public int InMethodPosition { get; private set; }
        public AnalysisCallNode(string methodName, TypeDescriptor declaredType, LocationDescriptor location, AnalysisCallNodeAdditionalInfo additionalInfo)
            : base(methodName, declaredType)
        {
            this.LocationDescriptor = location;
            this.InMethodPosition = location.InMethodOrder;
            this.AdditionalInfo = additionalInfo;
        }

        /// <summary>
        /// The idea is to use the same hash and equals than ANode but also locations
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            AnalysisCallNode analysisCallNode = (obj as AnalysisCallNode);
            return base.Equals(obj)
                    && analysisCallNode != null && base.Equals(analysisCallNode)
                    && analysisCallNode.LocationDescriptor.Equals(this.LocationDescriptor);
        }
        public override int GetHashCode()
        {
            return base.GetHashCode() + this.LocationDescriptor.GetHashCode();
        }
    }
}