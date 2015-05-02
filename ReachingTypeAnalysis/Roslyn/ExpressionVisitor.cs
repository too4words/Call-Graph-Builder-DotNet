﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.Contracts;

namespace ReachingTypeAnalysis.Roslyn
{
	/// <summary>
	/// This interface represent the way we model an expression from the AST in term of the our analysis strcture
	/// The analysis use to kinds of information:
	///    - Types: Information about concrete types 
	///    - Nodes: an element in the Propagation Graph (where concrete types flow). It represent a local variable, parameter or field
	///             There are special nodes that represent method invocations (more about  this later)
	/// </summary>
    internal abstract class AnalysisExpression 
    {
        protected TypeDescriptor typeDescriptor;
        protected PropGraphNodeDescriptor aNode;

        internal SyntaxNodeOrToken Expression {get; private set;}
        internal ITypeSymbol Type { get; set; }

		public AnalysisExpression(SyntaxNodeOrToken expression, ITypeSymbol type)
        {
            this.Type = type;
            this.Expression = expression;
        }

		public virtual PropGraphNodeDescriptor GetAnalysisNode()
		{
			if (aNode == null)
			{
				this.aNode = CreateAnalysisNode();
			}

			return aNode;
		}

        protected virtual PropGraphNodeDescriptor CreateAnalysisNode()
        {
			throw new NotImplementedException();
        }

        public virtual TypeDescriptor GetAnalysisType()
        {
			if (typeDescriptor == null)
			{
				//aType = new ConcreteType(Type);
                typeDescriptor = new TypeDescriptor(this.Type);
			}
			return typeDescriptor;
        }

        public virtual void ProcessAssignment(PropGraphNodeDescriptor lhsAnalysisNode,  MethodSyntaxVisitor methodVisitor)
        {
            methodVisitor.RegisterAssignment(lhsAnalysisNode, this);
        }

        public virtual AnalysisExpression ProcessArgument(ArgumentSyntax argNode, ExpressionVisitor visitor)
        {
            return this;
        }
    }

    /// <summary>
    /// Represents a Constant. 
    /// </summary>
    internal class Constant : AnalysisExpression
    {
        internal Constant(SyntaxNodeOrToken expression, ITypeSymbol type)
            : base(expression, type)
        { }

        public override PropGraphNodeDescriptor GetAnalysisNode()
        {
            return null;
        }
    }

	/// <summary>
	/// Represents an allocation (i.e:, new statament)
	/// </summary>
	internal class Allocation : AnalysisExpression
	{
		internal Allocation(SyntaxNodeOrToken expression, ITypeSymbol type)
			: base(expression, type)
		{ }

		public override void ProcessAssignment(PropGraphNodeDescriptor lhsAnalysisNode, MethodSyntaxVisitor methodVisitor)
		{
			var allocType = this.GetAnalysisType();
			methodVisitor.RegisterNewExpressionAssignment(lhsAnalysisNode, allocType);
			//base.ProcessAssignment(lhsAnalysisNode, methodVisitor);
		}
	}

    /// <summary>
    /// Represents an identifier like a variable, parameter
    /// </summary>
    internal class Identifier : AnalysisExpression
    {
		internal ISymbol Symbol { get; set; }

		internal Identifier(SyntaxNodeOrToken expression, ITypeSymbol type, ISymbol symbol)
			: base(expression, type)
		{
			this.Symbol = symbol;
		}

        protected override PropGraphNodeDescriptor CreateAnalysisNode()
        {
			return new VariableNode(this.Symbol.Name, new TypeDescriptor(this.Type));
        }
    }

    internal class Parameter : Identifier
    {
        int position;
		internal Parameter(int position, SyntaxNodeOrToken expression, ITypeSymbol t, ISymbol s)
			: base(expression, t, s)
		{
			this.position = position;
		}

		protected override PropGraphNodeDescriptor CreateAnalysisNode()
		{
			return new ParameterNode(this.Expression.ToString(), position, new TypeDescriptor(this.Type));
		}
    }

	/// <summary>
	/// Represents a Field  or a Property
	/// </summary>
	internal class Field : Identifier
	{
		private string className;
		private string field;
		internal Field(SyntaxNodeOrToken expression, ITypeSymbol type, ISymbol symbol)
			: base(expression, type, symbol)
		{
			className = symbol.ContainingType.Name;
			field = symbol.Name;
		}
		protected override PropGraphNodeDescriptor CreateAnalysisNode()
		{
			return new FieldNode(this.className, this.field, new TypeDescriptor(this.Type));
		}
	}

    internal class Method : Identifier
    {
        public IMethodSymbol RoslynMethod { get; private set; }
        public bool IsDelegate { get; private set; }
		internal Method(SyntaxNodeOrToken expression, ITypeSymbol t, IMethodSymbol symbol)
			: base(expression, symbol.ReturnType, symbol)
		{
			IsDelegate = false;
			RoslynMethod = symbol as IMethodSymbol;
		}
    }

    internal class Property : Identifier
    {
        public IMethodSymbol RoslynMethod { get; private set; }
        public bool IsDelegate { get; private set; }
        internal Property(SyntaxNodeOrToken expression, ITypeSymbol typeSymbol, ISymbol symbol)
            : base(expression, typeSymbol, symbol)
        {
            IsDelegate = false;
            var properySymbol = symbol as IPropertySymbol;
            RoslynMethod = properySymbol.GetMethod;
        }

        public override AnalysisExpression ProcessArgument(ArgumentSyntax argNode, ExpressionVisitor visitor)
        {
            return  visitor.AnalyzeProperty(argNode.Expression, visitor.ThisRef , null, this);
        }
    }

    /// <summary>
    /// Represent a Field, Property, or method access
    /// </summary>
    internal class MemberAccess : AnalysisExpression
    {
        internal Identifier NameExpresion { get; private set; }
		internal AnalysisExpression ReferenceExpresion { get; private set; }
		internal ISymbol Field { get; private set; }
		internal SyntaxNodeOrToken FieldSyntax { get; private set; }

        internal MemberAccess(MemberAccessExpressionSyntax ex, AnalysisExpression r, ExpressionSyntax rSyntax,
            Identifier field, SyntaxNodeOrToken fSyntax, ITypeSymbol type)
            : base(ex.Name, type)
        { 
            this.Field = field.Symbol;
            this.FieldSyntax = fSyntax;
            this.NameExpresion = field;
            this.ReferenceExpresion = r;
        }

		protected override PropGraphNodeDescriptor CreateAnalysisNode()
		{
			if (this.Field.Kind == SymbolKind.Property)
			{
				return new AnalysisCallNode(
					new TypeDescriptor(this.Type), 
					new LocationDescriptor(this.Expression.GetLocation()));
			}
			else
			{
				return new FieldNode(
					this.Field.ContainingType.Name,
					this.Field.Name,
					new TypeDescriptor(this.Type));
			}
		}

        public override void ProcessAssignment(PropGraphNodeDescriptor lhsAnalysisNode, MethodSyntaxVisitor methodVisitor)
        {
            // Check for delegate invocation (ie. x = s.Delegate())
            if (this.NameExpresion is Method)
            {
                var delegateMethod = (this.NameExpresion as Method).RoslynMethod;
                methodVisitor.RegisterAssignment(lhsAnalysisNode, this.ReferenceExpresion);
                methodVisitor.RegisterDelegate(lhsAnalysisNode, delegateMethod);
            }
            else
            {
                methodVisitor.RegisterAssignment(lhsAnalysisNode, this);
            }
        }
        public override AnalysisExpression ProcessArgument(ArgumentSyntax argNode, ExpressionVisitor visitor)
        {
            if (this.NameExpresion is Property)
            {
                return visitor.AnalyzeProperty(argNode.Expression, null,  this.ReferenceExpresion, this.NameExpresion as Property);
            }
            // To-do Support delegates like s.Delegate here!
            if(this.NameExpresion is Method)
            { }
            return base.ProcessArgument(argNode, visitor);
        }
    }

	/// <summary>
	/// DIEGO: I would like to remove this
	/// Represent a non supported expression
	/// </summary>
	internal class UnsupportedExpression : AnalysisExpression
	{
		SyntaxNodeOrToken expression;
		internal UnsupportedExpression(SyntaxNodeOrToken ex, ITypeSymbol type, ISymbol s)
			: base(ex, type)
		{
			var descriptor = new TypeDescriptor(type);
			this.aNode = new UnsupportedNode(descriptor);
			this.typeDescriptor = descriptor;
		}
	}

    /// <summary>
    /// Represent a method invocation
    /// </summary>
    internal class Call : AnalysisExpression
    {
        /// <summary>
        ///  It doesn't contain information but is used to connect the arguments to the call
        ///  Essentially is a type reach this node, we need to propagate info to the callee
        /// </summary>
        public AnalysisCallNode CallNode { get; private set; }
        
		/// <summary>
        /// A node in the PropGraph that represent the result returned in the invocation
        /// </summary>
        public VariableNode ReturnedVariableNode { get; private set; }

        public IMethodSymbol RoslynMethod { get; private set; }
        
        //public AnalysisMethod Method { get; private set; }
        public MethodDescriptor Method { get; private set; }

        internal Call(ExpressionSyntax node, ITypeSymbol type, IMethodSymbol method, 
                    AnalysisCallNode callNode, VariableNode returnedVariableNode)
            : base(node, type)
        {
            this.RoslynMethod = method;
            this.Method = new MethodDescriptor(method);
            this.CallNode = callNode;
            this.ReturnedVariableNode = returnedVariableNode;
        }

        public override PropGraphNodeDescriptor GetAnalysisNode()
        {
            return this.CallNode;
        }
        public override void ProcessAssignment(PropGraphNodeDescriptor lhsAnalysisNode, MethodSyntaxVisitor methodVisitor)
        {
            Contract.Requires(lhsAnalysisNode is VariableNode);
            methodVisitor.RegisterCallLHS((VariableNode)lhsAnalysisNode, this);
        }
        public override AnalysisExpression ProcessArgument(ArgumentSyntax argNode, ExpressionVisitor visitor)
        {
            return visitor.Visit(argNode.Expression);
            // return visitor.AnalyzeInvocation(argNode.Expression as InvocationExpressionSyntax); 
        }
    }
    internal class DelegateCall : Call
    {
        internal DelegateCall(ExpressionSyntax node, ITypeSymbol type, 
                             IMethodSymbol method, AnalysisCallNode analysisCallNode, VariableNode returnedVariableNode) : 
			base(node, type, method, analysisCallNode,returnedVariableNode)
        {
            //this.cn = cn;
        }
        public override PropGraphNodeDescriptor GetAnalysisNode()
        {
            return this.CallNode;
        }
    }

    internal class ExpressionVisitor : CSharpSyntaxVisitor<AnalysisExpression>
    {
        private CodeProvider codeProvider;
        private StatementProcessor statementProcessor;
        private MethodSyntaxVisitor roslynMethodVisitor;
        
        // This mapping is used for the temporary variables used in nested calls 
        private IDictionary<ExpressionSyntax, PropGraphNodeDescriptor> tempLH = new Dictionary<ExpressionSyntax, PropGraphNodeDescriptor>();

        internal PropGraphNodeDescriptor ThisRef
        {
            get { return roslynMethodVisitor.ThisRef; }
        }

        internal ExpressionVisitor(CodeProvider codeProvider, 
            StatementProcessor stProcessor, 
            MethodSyntaxVisitor roslynMethodVisitor)
        {
            Contract.Requires(codeProvider != null);

            this.codeProvider = codeProvider;
            this.statementProcessor = stProcessor;
            this.roslynMethodVisitor = roslynMethodVisitor;
        }

        public override AnalysisExpression DefaultVisit(SyntaxNode node)
        {
            //return null;
            return base.DefaultVisit(node);
        }
        public override AnalysisExpression VisitParenthesizedExpression(ParenthesizedExpressionSyntax node)
        {
            return Visit(node.Expression);
            //return base.VisitParenthesizedExpression(node);
        }

		public override AnalysisExpression VisitLiteralExpression(LiteralExpressionSyntax node)
		{
			var symbol = this.codeProvider.SemanticModel.GetSymbolInfo(node).Symbol;
			var type = this.codeProvider.SemanticModel.GetTypeInfo(node).Type;
			if (type != null && Utils.IsTypeForAnalysis(type))
			{
				return new Constant(node, type);
			}
			else
			{
				return null;
			}
		}

        public override AnalysisExpression VisitConditionalExpression(ConditionalExpressionSyntax node)
        {
            return Visit(node.Condition);
        }

        public override AnalysisExpression VisitAssignmentExpression(AssignmentExpressionSyntax node)
        {
			//var lhsExp = Visit(node.Left);
			//var rhsExp = Visit(node.Right);
			if (Utils.IsTypeForAnalysis(this.codeProvider.SemanticModel, node.Left) && Utils.IsTypeForAnalysis(this.codeProvider.SemanticModel, node.Right))
			{
				var lhs = Visit(node.Left);
				var rhs = Visit(node.Right);

				if (lhs == null || rhs == null)
				{
					return CreateUnsupportedExpression(node.Right);
				}

				this.statementProcessor.RegisterLocalVariable(lhs.GetAnalysisNode());
				//this.roslynMethodVisitor.RegisterVariable(lhs.GetSynTaxExpression(),
				//                                          lhs.GetAnalysisType().RoslynType,
				//                                          lhs.GetRoslynSymbol());
				rhs.ProcessAssignment(lhs.GetAnalysisNode(), this.roslynMethodVisitor);

				//if (rhs is Allocation)
				//{
				//    // Get the type of the allocation 
				//    var allocType = rhs.GetAnalysisType();
				//    statementProcessor.RegisterNewExpressionAssignment(lhs.GetAnalysisNode(), allocType);
				//}
				//else
				//{
				//    rhs.ProcessAssignment(lhs.GetAnalysisNode(), this.roslynMethodVisitor);
				//    statementProcessor.RegisterAssignment(lhs.GetAnalysisNode(), rhs.GetAnalysisNode());
				//}
				return rhs;
			}
			else
			{
				return CreateUnsupportedExpression(node.Right);
			}
        }

		public override AnalysisExpression VisitIdentifierName(IdentifierNameSyntax node)
		{
			var symbol = this.codeProvider.SemanticModel.GetSymbolInfo(node).Symbol;
			var type = GetTypeSymbol(node);
			switch (symbol.Kind)
			{
				case SymbolKind.Field:
					return new Field(node, type, symbol);
					// Do you need to add a thisRef? 
					// In the current version no because a node is C.f
				case SymbolKind.Method:
					return new Method(node, type, (IMethodSymbol)symbol);
				case SymbolKind.Property:
					return new Property(node, type, symbol);
				case SymbolKind.Local:
					return new Identifier(node, type, symbol);
				case SymbolKind.Parameter:
					// I need to return the already created parameter node
					int i = 0;
					foreach (var parameterNode in this.statementProcessor.ParameterNodes)
					{
						Contract.Assert(parameterNode != null);
						if (parameterNode.Name.Equals(symbol.Name))
						{
							return new Parameter(i, node, type, symbol);
						}
						i++;
					}
					Contract.Assert(false, "Can't find parameter by name " + symbol.Name);
					return null;
				case SymbolKind.NamedType:
					// Todo create a NamedType type?
					return new Identifier(node, type, symbol);
				//case SymbolKind.Namespace:
				//	break;
				default:
					return new Identifier(node, type, symbol);
			}
			throw new System.ArgumentException();
		}

        #region allocation
        /// <summary>
        /// Process a new statement. This consist in two opetrations:
        /// 1) allocation 
        /// 2) constructor invocation. 
        /// We register the invocationa and return the concrete type from the allocation
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public override AnalysisExpression VisitObjectCreationExpression(ObjectCreationExpressionSyntax node)
        {
            this.roslynMethodVisitor.InvocationOrder++;

            var allocAnalysisExpression = ProcessConstructor(node,node.ArgumentList);
            return allocAnalysisExpression;
        }

        private Allocation ProcessConstructor(ExpressionSyntax node, ArgumentListSyntax argumentListSyntax)
        {
            var type = this.codeProvider.SemanticModel.GetTypeInfo(node);
            var symbol = this.codeProvider.SemanticModel.GetSymbolInfo(node).Symbol;
            // Create an allocation expression
            var allocAnalysisExpression = new Allocation(node, type.Type);
            // Process the constructor as a call
            if (symbol != null)
            {
                IMethodSymbol roslynMethod = symbol as IMethodSymbol;
                // Process parameters
                var callNode = new AnalysisCallNode(
                    new TypeDescriptor(roslynMethod.ReturnType),
                    new LocationDescriptor(this.roslynMethodVisitor.InvocationOrder)); //node.GetLocation()

                var methodDescriptor = new MethodDescriptor(roslynMethod);
                Contract.Assert(!roslynMethod.IsStatic);
                Contract.Assert(roslynMethod.MethodKind == MethodKind.Constructor);
                var args = ProcessArguments(argumentListSyntax, roslynMethod.Parameters);
                statementProcessor.RegisterConstructorCall(methodDescriptor, args, null, callNode);
            }
            return allocAnalysisExpression;
        }

        private IList<PropGraphNodeDescriptor> ProcessArguments(ArgumentListSyntax argumentListSyntax, 
			ImmutableArray<IParameterSymbol> parameters)
        {
            var args = new List<PropGraphNodeDescriptor>();
            for (int i = 0; i < parameters.Length; i++)
            {
                if (argumentListSyntax != null && argumentListSyntax.Arguments != null 
					&& argumentListSyntax.Arguments.Count > i)
                {
                    PropGraphNodeDescriptor anode = null;
                    var a_prime = this.Visit(argumentListSyntax.Arguments[i]);
                    if (a_prime != null)
                    {
                        if (a_prime is Call) // || a_prime is Property)
                        {
                            var callExp = a_prime as Call;
                            anode = callExp.ReturnedVariableNode;
                        }
                        else
                        {
                            anode = a_prime.GetAnalysisNode();
                        }
                    }
                    args.Add(anode);
                }
                else args.Add(null);
            }
            Contract.Assert(args.Count == parameters.Length);
            return args;
        }

        /// <summary>
        /// TO DO
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        /// 
        public override AnalysisExpression VisitSimpleLambdaExpression(SimpleLambdaExpressionSyntax node)
        {
            return null;
        }

        public override AnalysisExpression VisitExpressionStatement(ExpressionStatementSyntax node)
        {
            return Visit(node.Expression);
        }

        public override AnalysisExpression VisitAnonymousObjectCreationExpression(AnonymousObjectCreationExpressionSyntax node)
        {
            return base.VisitAnonymousObjectCreationExpression(node);
        }
        /// <summary>
        /// TO DO
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public override AnalysisExpression VisitArrayCreationExpression(ArrayCreationExpressionSyntax node)
        {
            return ProcessConstructor(node,null);
        }
        #endregion

        /// <summary>
        /// TO DO
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public override AnalysisExpression VisitAnonymousMethodExpression(AnonymousMethodExpressionSyntax node)
        {
            return base.VisitAnonymousMethodExpression(node);
        }

        public override AnalysisExpression VisitArgument(ArgumentSyntax node)
        {
            var analysisExpression = Visit(node.Expression);
            if (analysisExpression != null)
            {
                analysisExpression.ProcessArgument(node, this);
            }
            //if(analysisExpression is Property)
            //{
            //    Property property = analysisExpression as Property;
            //    analysisExpression = AnalyzeProperty(node.Expression, this.ThisRef, null, property);
            //}
            //else if(analysisExpression is Call)
            //{
            //    analysisExpression = AnalyzeInvocation(node.Expression as InvocationExpressionSyntax);
            //}

            return analysisExpression;
        }

        public override AnalysisExpression VisitBinaryExpression(BinaryExpressionSyntax node)
        {
            var lhs = Visit(node.Left);
            var rhs = Visit(node.Right);

            return CreateUnsupportedExpression(node);
        }

		/// <summary>
		/// Process an invocation.
		/// </summary>
		/// <param name="node"></param>
		/// <returns></returns>
		public override AnalysisExpression VisitInvocationExpression(InvocationExpressionSyntax node)
		{
            this.roslynMethodVisitor.InvocationOrder++;

			// CHECK
			var callExpression = node.Expression;

			// in Roslyn rc1 they use Kind() instead of CSharpKind()...
			//if (callExpression.Kind() == (SyntaxKind.SimpleMemberAccessExpression) ||
			//callExpression.Kind() == (SyntaxKind.IdentifierName))

			if (callExpression.Kind() == SyntaxKind.SimpleMemberAccessExpression ||
				callExpression.Kind() == SyntaxKind.IdentifierName)
			{
				return AnalyzeInvocation(node);
			}
			return CreateUnsupportedExpression(node);
		}

		internal AnalysisExpression AnalyzeInvocation(InvocationExpressionSyntax node)
		{
			VariableNode lh = null;
			Call call = null;
			// Analyze if it is actually a chain of invocations
			// in Roslyn rc1 they use Kind() instead of CSharpKind...
			if (node.Expression.Kind() == (SyntaxKind.SimpleMemberAccessExpression))
			{
				var simpleAccessMemberExpression = node.Expression as MemberAccessExpressionSyntax;
				// Analyze the expresion that can be an invocation or an access path
				var analysisExpression = Visit(simpleAccessMemberExpression.Expression);
				if (analysisExpression is Call)
				{
					// In the case of a nested call we use an artificial var as the left value
					// of an invocation (TempRV = call...)
					// That TempRV can be used as a parameter (e.g. the receiver) in the next call
					call = analysisExpression as Call;
					lh = call.ReturnedVariableNode;
				}
				// This is a Property and then a call (e.g, Prop.Method())
				else if (analysisExpression is Property)
				{
					call = AnalyzeProperty(simpleAccessMemberExpression, this.ThisRef, null,
												analysisExpression as Property);
					if (call != null)
					{
						lh = call.ReturnedVariableNode;
					}
				}
				else
				{
					// Diego: TO-DO
					// This is for Identifier of MemberAccess 
					// These cases are handled later by TrytToGetReceiver 
					// It will be nice to handle here to avoid revisiting the node
					// And we already have here almost all the information
				}
			}
			else
			{
				// Idem previous TO-DO:  handled later by TrytToGetReceiver
				// This case is for static calls or virtual call with implicit "this"
			}
			return RegisterCall(node, lh);
		}

        /// <summary>
        /// Register an invocation of the form: a.call(....)
        /// The variable tempReceiver is used when the call includes a nested call
        /// e.g : call1(...).call2(...), tempReceiver represents the result of call1() 
        /// </summary>
        /// <param name="node"></param>
        /// <param name="args"></param>
        /// <param name="tempReceiver"></param>
        /// <returns></returns>
        private AnalysisExpression RegisterCall(InvocationExpressionSyntax node, VariableNode tempReceiver)
        {
            VariableNode lh = null;
            AnalysisExpression result = null;

            var callSymbolinfo = this.codeProvider.SemanticModel.GetSymbolInfo(node);
            var methodSymbol = callSymbolinfo.Symbol;
            // This shouldn't happend but if it fails to get the method, at least tries to get a candidate
            if (methodSymbol == null && callSymbolinfo.CandidateSymbols.Length > 0)
            {
                methodSymbol = callSymbolinfo.CandidateSymbols[0];
            }

			if (methodSymbol != null)
			{
				IMethodSymbol methodInvokedSymbol = methodSymbol as IMethodSymbol;
				var methodDescriptor = new MethodDescriptor(methodInvokedSymbol);
				lh = CreateAndRegisterTemporaryLHVar(node, methodInvokedSymbol);

				var args = ProcessArguments(node.ArgumentList, methodInvokedSymbol.Parameters);

				// Delegate? 
				if (methodInvokedSymbol.MethodKind == MethodKind.DelegateInvoke)
				{
					// Get the delegate variable (SHOULD use a cache!)
					var delegateVarNode = roslynMethodVisitor.RegisterVariable(node.Expression);
					var callNode = new AnalysisCallNode(
						new TypeDescriptor(methodInvokedSymbol.ReturnType),
                        new LocationDescriptor(this.roslynMethodVisitor.InvocationOrder)); //node.GetLocation()

                    statementProcessor.RegisterStaticDelegateCall(methodDescriptor, args, lh, (DelegateVariableNode)delegateVarNode, callNode);
                    result = new DelegateCall(node, methodInvokedSymbol.ReturnType, methodInvokedSymbol, callNode, lh);
				}
				// Normal invocation    
				else
				{
					var callNode = new AnalysisCallNode(
						new TypeDescriptor(methodInvokedSymbol.ReturnType),
                        new LocationDescriptor(this.roslynMethodVisitor.InvocationOrder)); //node.GetLocation()

					VariableNode receiverArg = null;
					if (!methodInvokedSymbol.IsStatic)
					{
                        // Diego: Check types!
						var receiver = TryToGetReceiver(node, methodInvokedSymbol);
                        Contract.Assert(receiver is VariableNode);

						// Try to get receiver, when this is not given by the previous nested call
						receiverArg = tempReceiver == null ? (VariableNode)receiver : tempReceiver;
						if (receiverArg != null)
						{
							if (receiverArg != receiver)
							{
								statementProcessor.RegisterAssignment(receiverArg, (VariableNode)receiver);
							}
							// Register the invocation in the PropGraph
							statementProcessor.RegisterVirtualCall(methodDescriptor, receiverArg, args, lh, callNode);
						}
						else // To-DO FIX: This is not correct because is not static (in can be a lamba o query or reduced expression)
						{
							statementProcessor.RegisterStaticCall(methodDescriptor, args, lh, callNode);
						}
					}
					else
					{
						statementProcessor.RegisterStaticCall(methodDescriptor, args, lh, callNode);
					}

					result = new Call(node, methodInvokedSymbol.ReturnType, methodInvokedSymbol, callNode, lh);
				}
			}
			else
			{

			}
            return result;
        }

        private VariableNode CreateAndRegisterTemporaryLHVar(ExpressionSyntax node, IMethodSymbol roslynMethod)
        {
            VariableNode tempRV = null;
            //var tmpExp = SyntaxFactory.ParseName("T_" + tempLH.Count);
            if (!roslynMethod.ReturnsVoid && Utils.IsTypeForAnalysis(roslynMethod.ReturnType))
            {
                // Creates the node that will hold the value returned by the call
                tempRV = new VariableNode("T_" + tempLH.Count, new TypeDescriptor(roslynMethod.ReturnType));
                //tempRV = (VariableNode)this.roslynMethodVisitor.CreateNodeForExpression(
                //    roslynMethod.ReturnType,
                //    tmpExp, 
                //    roslynMethod.ContainingType);

                tempLH[node] = tempRV;
                statementProcessor.RegisterLocalVariable(tempRV);
            }
            return tempRV;
        }

        public override AnalysisExpression VisitThisExpression(ThisExpressionSyntax node)
        {
            if (this.ThisRef != null)
            {
                var symbol = this.codeProvider.SemanticModel.GetSymbolInfo(node).Symbol;
				var type = GetTypeSymbol(node);
                // If we find I real "this" we replace with the fake one
                // I did this to keep the same node 
                return new Identifier(node, type, symbol);
            }
            else {
                return null;
            }
        }

        private PropGraphNodeDescriptor TryToGetReceiver(ExpressionSyntax node, IMethodSymbol ms)
        {
			PropGraphNodeDescriptor receiverArg = null;
            //if(node is ObjectCreationExpressionSyntax)
            //{
            //    var objExp = node as ObjectCreationExpressionSyntax;
            //    receiverArg = thisRef; 
            //}
            if (node is InvocationExpressionSyntax)
            {
                var callExp = node as InvocationExpressionSyntax;
                // in Roslyn rc1 they use Kind() instead of CSharpKind...
                switch (callExp.Expression.Kind())
                {
                    case SyntaxKind.SimpleMemberAccessExpression:
                        var sma = callExp.Expression as MemberAccessExpressionSyntax;
                        if(!tempLH.TryGetValue(sma.Expression, out receiverArg))
                        {
                            var anaExp = Visit(sma.Expression);
                            if (anaExp != null)
                            {
                                receiverArg = (PropGraphNodeDescriptor)anaExp.GetAnalysisNode();
                            }
                            else
                            {
                                return null;
                            }
                        }                        
                        break;
                    /// Identifier means a method without "this?'
                    case SyntaxKind.IdentifierName:
                        receiverArg = this.ThisRef;
                        break;
                    default:
                        // CHECK!!!!
                        var anaExp2 = Visit(callExp.Expression);
                        receiverArg = anaExp2.GetAnalysisNode();
                        break;
                }
            }
            return receiverArg;
        }

        public override AnalysisExpression VisitCastExpression(CastExpressionSyntax node)
        {
            // TODO: should you not be filtering the types that come out of this to only let the ones that match the cast??
            return Visit(node.Expression);
        }

        public override AnalysisExpression VisitConstructorInitializer(ConstructorInitializerSyntax node)
        {
            return base.VisitConstructorInitializer(node);
        }

        public override AnalysisExpression VisitElementAccessExpression(ElementAccessExpressionSyntax node)
        {
            // This is for args[i]. I'm just taking args!
            return Visit(node.Expression);
        }

        public override AnalysisExpression VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
        {
            AnalysisExpression res = null;
            PropGraphNodeDescriptor receiverArg = null;
            // A MemberExpression looks like reference.Name, reference can be a path and Name is an identifier of a Field or Delegate
            var nameExpresssion = Visit(node.Name);
            var symbol = this.codeProvider.SemanticModel.GetSymbolInfo(node.Name).Symbol;
            
            //if (nameExpresssion == null)
            //{
            //    // If is a namespace, that means that the reference expression is the prexif of the namespace
            //    if (symbol.Kind == SymbolKind.Namespace)
            //    {
            //        return CreateUnsupportedExpression(node);
            //    }
            //}

			// If it is a type, the reference is a namespace
            if (symbol.Kind == SymbolKind.NamedType)
			{
				return CreateUnsupportedExpression(node);
			}
            var referenceExpression = Visit(node.Expression);
 
            if (referenceExpression != null)
            {                
                if (nameExpresssion is Property)
                {
                    res =  AnalyzeProperty(node, receiverArg, referenceExpression, (Property)nameExpresssion);
                }
                //else if (nameExpresssion is Method)
                //{
                //    // Is it a delegate because is not an invocation
                //    var methodExpression = nameExpresssion as Method;
                //    var delegateMethod = methodExpression.RoslynMethod;
                //    //this.StatementProcessor.RegisterDelegateAssignment(lhsAnalysisNode, new AMethod(delegateMethod));
                //}
                return new MemberAccess(node, referenceExpression, node.Expression,
                                        (Identifier)nameExpresssion, node.Name, 
                                        nameExpresssion.Type);
            }
            return CreateUnsupportedExpression(node);
        }

        /// <summary>
        /// Obtain and register the method call determined by the property
        /// </summary>
        /// <param name="node"></param>
        /// <param name="receiverArg"></param>
        /// <param name="referenceAnalysisExpression"></param>
        /// <param name="property"></param>
        /// <returns></returns>
        internal Call AnalyzeProperty(ExpressionSyntax node,
					 PropGraphNodeDescriptor receiverArg, 
					 AnalysisExpression referenceAnalysisExpression, 
					 Property property)
        {
            var roslynMethod = property.RoslynMethod;
            // we treat this as an invocation
            var callNode = new AnalysisCallNode(
				new TypeDescriptor(roslynMethod.ReturnType),
                new LocationDescriptor(this.roslynMethodVisitor.InvocationOrder)); //node.GetLocation()

            var methodDescriptor = new MethodDescriptor(roslynMethod);

            if (receiverArg == null)
            {
                if (referenceAnalysisExpression != null)
                {
                    receiverArg = referenceAnalysisExpression.GetAnalysisNode();
                }
            }

            var lhs = CreateAndRegisterTemporaryLHVar(node, roslynMethod);
            var args = new List<PropGraphNodeDescriptor>();
            statementProcessor.RegisterPropertyCall(methodDescriptor, receiverArg, args, lhs, callNode);

            return new Call(node, roslynMethod.ReturnType, roslynMethod, callNode, lhs);
        }
        
        public override AnalysisExpression VisitBaseExpression(BaseExpressionSyntax node)
        {
            // I treat this like a this reference...
            var symbol = this.codeProvider.SemanticModel.GetSymbolInfo(node).Symbol;
            //return new Identifier(node, this.ThisRef.DeclaredType, symbol);
            return new Identifier(node, symbol.ContainingType, symbol);
        }

        private ITypeSymbol GetTypeSymbol(SyntaxNode node)
        {
            var typeInfo = this.codeProvider.SemanticModel.GetTypeInfo(node);
            var type = typeInfo.Type;
            return type;
        }

        private ITypeSymbol GetTypeSymbol(ExpressionSyntax node)
        {
            var typeInfo = this.codeProvider.SemanticModel.GetTypeInfo(node);
            var type = typeInfo.Type;
            return type;
        }
        /// <summary>
        /// Creates a node for expressions we can not deal
        /// Diego: Maybe it'd be better to return null for the VTA analysis
        /// For the Declared Type propagation we can return the declared type
        /// </summary>
        /// <param name="exp"></param>
        /// <returns></returns>
        private AnalysisExpression CreateUnsupportedExpression(ExpressionSyntax exp)
        {
            var type = GetTypeSymbol(exp);
            var symbol = this.codeProvider.SemanticModel.GetSymbolInfo(exp).Symbol;
            if(Utils.IsTypeForAnalysis(type))
                return new UnsupportedExpression(exp, type, symbol);
            return null;
        }
    }
}
