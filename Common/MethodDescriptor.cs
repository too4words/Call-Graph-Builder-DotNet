// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.
using Microsoft.CodeAnalysis;
using System;
using System.Diagnostics.Contracts;

namespace ReachingTypeAnalysis
{
	/// <summary>
	/// This is a string represenation of a method designed to be 
	/// put as keys in Dictionaries and used for comparison.
	/// </summary>
	public class MethodDescriptor 
    {
        public string ClassName {get ; private set;}
        public string MethodName { get; private set; }
        public string NamespaceName { get; private set; }

        private string name;

        public string Name
        {
            get {
                if (this.name == null)
                {
                    this.name =  ClassName + "." + MethodName;
                    if (this.NamespaceName != string.Empty)
                    {
                        this.name = NamespaceName + "." + this.name;
                    }
                }
                return name; 
            }
        }

		public MethodDescriptor(string classname, string methodName)
		{
			this.NamespaceName = "";
			this.ClassName = classname;
			this.MethodName = methodName;
			this.name = classname + "." + methodName;
		}

        public MethodDescriptor(string namespaceName, string classname, string methodName)
        {
            this.NamespaceName = namespaceName;
            this.ClassName = classname;
            this.MethodName = methodName;
        }

        public MethodDescriptor(IMethodSymbol method)
        {
            Contract.Assert(method != null);

            this.MethodName = method.Name;
            this.ClassName = method.ContainingType.Name;
            this.NamespaceName = method.ContainingNamespace.Name;
        }

        public bool SameAsMethodSymbol(IMethodSymbol method)
        {
            if (method == null)
            {
                return false;
            }
            else
            {
                bool result = method.Name.Equals(this.MethodName);
                result = result && method.ContainingType.Name.Equals(this.ClassName);
                result = result & (this.NamespaceName == null || method.ContainingNamespace.Name.Equals(this.NamespaceName));

                return result;
            }
        }

        public override bool Equals(object obj)
        {
            var md = obj as MethodDescriptor;
            bool nEq = (this.NamespaceName == "" || md.NamespaceName == "") || this.NamespaceName.Equals(md.NamespaceName);
            bool cEq = this.ClassName.Equals(md.ClassName);
            bool mEq = this.MethodName.Equals(md.MethodName);

            return nEq && cEq && mEq;
        }

        public override int GetHashCode()
        {
            return NamespaceName.GetHashCode() + ClassName.GetHashCode() + MethodName.GetHashCode();
        }

        public override string ToString()
        {
            return this.Name;
        }
    }

    [Serializable]
    public class TypeDescriptor 
    {
        public string TypeName { get; private set; }
        public TypeDescriptor(ITypeSymbol type)
        {
            this.TypeName = type.ToDisplayString();
        }

        public override int GetHashCode()
        {
            return this.TypeName.GetHashCode();
        }

        public override string ToString()
        {
            return this.TypeName.ToString();
        }
    }

	[Serializable]
	public class VariableDescriptor
	{

		public string VariableName { get; private set; }
		public VariableDescriptor(ISymbol variable)
		{
			this.VariableName = variable.ToDisplayString();
		}

		public VariableDescriptor(object lhs)
		{

		}
	}

	[Serializable]
	public class LocationDescriptor {
		public Location Location { get; private set; }

		public LocationDescriptor(Location location) {
			Contract.Assert(location != null);

			this.Location = location;
		}
		public override string ToString()
		{
			return this.Location.ToString();
		}
		public override bool Equals(object obj)
		{
			return this.Location.Equals(obj);
		}
		public override int GetHashCode()
		{
			return this.Location.GetHashCode();
		}
	}
}