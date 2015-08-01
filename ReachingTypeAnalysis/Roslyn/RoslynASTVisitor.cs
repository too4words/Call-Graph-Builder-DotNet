﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ReachingTypeAnalysis.Analysis;
using ReachingTypeAnalysis.Roslyn;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Threading.Tasks;

namespace ReachingTypeAnalysis
{

	/// <summary>
	/// This class is the base class for an analysis of Roslyn Methods
	/// Currenltly there are 2 subclasses: MethodSyntax and LibraryMethod
	/// </summary>
	internal abstract class GeneralRoslynMethodParser
    {
        // Analysis representation
        internal MethodInterfaceData MethodInterfaceData { get; set; }
        /// <summary>
        /// The next 3 properties are shortcuts for elements in MethodInterfaceData
        /// </summary>
        public VariableNode RetVar
        {
            get { return MethodInterfaceData.ReturnVariable; }
        }

        public IEnumerable<PropGraphNodeDescriptor> Parameters
        {
            get { return MethodInterfaceData.Parameters; }
        }

        public PropGraphNodeDescriptor ThisRef
        {
            get { return MethodInterfaceData.ThisRef; }
        }

        public StatementProcessor StatementProcessor { get; private set; }

        // Roslyn solution related
        public IMethodSymbol RoslynMethod { get; protected set; }
        // Communication
        //public IDispatcher Dispatcher { get; private set; }
        //private AMethod analysisMethod;

        //protected ICodeProvider codeProvider;

        public MethodDescriptor MethodDescriptor { get;  protected set; }

        public GeneralRoslynMethodParser(IMethodSymbol roslynMethod, MethodDescriptor methodDescriptor)
        {
			Contract.Assert(roslynMethod != null);
            this.RoslynMethod = roslynMethod;
            this.MethodDescriptor = methodDescriptor;
            this.MethodInterfaceData = CreateMethodInterfaceData(roslynMethod);
            //var codeProvider = ProjectCodeProvider.GetProjectProviderAndSyntaxAsync(this.MethodDescriptor).Result.Item1; 
            // The statement processor generates the Prpagagation Graph
            this.StatementProcessor = new StatementProcessor(this.MethodDescriptor, 
				                            this.RetVar, this.ThisRef, this.Parameters);
                                            //,
                                            //codeProvider);
            //this.codeProvider = codeProvider;
        }

        public GeneralRoslynMethodParser(IMethodSymbol roslynMethod) 
            : this(roslynMethod, Utils.CreateMethodDescriptor(roslynMethod))
        {
        }

        public GeneralRoslynMethodParser(MethodDescriptor methodDescriptor, 
                                            ProjectCodeProvider codeProvider)
        {
			Contract.Assert(methodDescriptor != null);
            this.RoslynMethod = codeProvider.FindMethod(methodDescriptor);
            this.MethodDescriptor = methodDescriptor;
            this.MethodInterfaceData = CreateMethodInterfaceData(this.RoslynMethod);
            // The statement processor generates the Prpagagation Graph
            this.StatementProcessor = new StatementProcessor(this.MethodDescriptor, 
				                            this.RetVar, this.ThisRef, this.Parameters);
                                            //,
                                            //codeProvider);
            //this.codeProvider = codeProvider;
        }
        public GeneralRoslynMethodParser(MethodDescriptor methodDescriptor)
        {
            Contract.Assert(methodDescriptor != null);
            this.MethodDescriptor = methodDescriptor;
            this.MethodInterfaceData = CreateMethodInterfaceDataForNonAnalyzableMethod(methodDescriptor);
            this.StatementProcessor = new StatementProcessor(this.MethodDescriptor,
                                            this.RetVar, this.ThisRef, this.Parameters);
            //,
            //                                null);

        }

        /// <summary>
        /// This is the propagation graph generated by the 
        /// </summary>
        internal PropagationGraph PropGraph
        {
            get { return this.StatementProcessor.PropagationGraph; }
        }

        public IEnumerable<TypeDescriptor> InstantiatedTypes
        {
            get { return this.StatementProcessor.InstantiatedTypes; }
        }

        public abstract MethodEntity ParseMethod();

		/// <summary>
		/// Generates analysis values for the method signature 
		/// In particular, the nodes for the propagation graph for parameters, retvalue y "this" 
		/// </summary>
		/// <param name="symbol"></param>
		/// <returns></returns>
		internal virtual MethodInterfaceData CreateMethodInterfaceData(IMethodSymbol methodSymbol)
		{
            Contract.Assert(methodSymbol != null);

			ReturnNode retVar = null;
			VariableNode thisRef = null;
			IList<ParameterNode> parameters;
			var inputs = new Dictionary<string, PropGraphNodeDescriptor>();
			var outputs = new Dictionary<string, PropGraphNodeDescriptor>();
			if (!methodSymbol.ReturnsVoid && Utils.IsTypeForAnalysis(methodSymbol.ReturnType))
			{
				retVar = new ReturnNode(Utils.CreateTypeDescriptor(methodSymbol.ReturnType));
				outputs["retVar"] = retVar;
			}
			if (!methodSymbol.IsStatic)
			{
				thisRef = new ThisNode(Utils.CreateTypeDescriptor(methodSymbol.ReceiverType));
			}
			parameters = new List<ParameterNode>();
			for (int i = 0; i < methodSymbol.Parameters.Count(); i++)
			{
				var p = methodSymbol.Parameters[i];
				var parameterNode = new ParameterNode(methodSymbol.Parameters[i].Name, i, Utils.CreateTypeDescriptor(p.Type));
				parameters.Add(parameterNode);
				if (p.RefKind == RefKind.Ref || p.RefKind == RefKind.Out)
				{
					outputs[p.Name] = parameterNode;
				}
				inputs[p.Name] = parameterNode;
			}

			var methodInterfaceData = new MethodInterfaceData()
			{
				ReturnVariable = retVar,
				ThisRef = thisRef,
				Parameters = parameters,
				InputData = inputs,
				OutputData = outputs
			};
			return methodInterfaceData;
		}

        internal virtual MethodInterfaceData CreateMethodInterfaceDataForNonAnalyzableMethod(MethodDescriptor methodDescriptor)
        {
            Contract.Assert(methodDescriptor != null);

            ReturnNode retVar = null;
            VariableNode thisRef = null;
            IList<ParameterNode> parameters;

            var inputs = new Dictionary<string, PropGraphNodeDescriptor>();
            var outputs = new Dictionary<string, PropGraphNodeDescriptor>();
            
            if (methodDescriptor.ReturnType!=null && Utils.IsTypeForAnalysis(methodDescriptor.ReturnType))
            {
                retVar = new ReturnNode(methodDescriptor.ReturnType);
                outputs["retVar"] = retVar;
            }
            if (!methodDescriptor.IsStatic)
            {
                thisRef = new ThisNode(methodDescriptor.ThisType);
            }
            parameters = new List<ParameterNode>();
            if (methodDescriptor.Parameters != null)
            {
                for (int i = 0; i < methodDescriptor.Parameters.Count(); i++)
                {
                    var parameterName = "P_" + i;
                    var parameterNode = new ParameterNode(parameterName, i, methodDescriptor.Parameters[i]);
                    parameters.Add(parameterNode);
                    //if (p.RefKind == RefKind.Ref || p.RefKind == RefKind.Out)
                    {
                        outputs[parameterName] = parameterNode;
                    }
                    inputs[parameterName] = parameterNode;
                }
            }

            var methodInterfaceData = new MethodInterfaceData()
            {
                ReturnVariable = retVar,
                ThisRef = thisRef,
                Parameters = parameters,
                InputData = inputs,
                OutputData = outputs
            };
            return methodInterfaceData;
        }


        internal IDictionary<SyntaxNodeOrToken, PropGraphNodeDescriptor> expressionNodeCache = new Dictionary<SyntaxNodeOrToken, PropGraphNodeDescriptor>();
        //internal ANode CreateNodeForExpression(SyntaxNode ex)
        //{
        //    var type = semanticModel.GetTypeInfo(ex).Type;
        //    return CreateNodeForExpression(type, ex);
        //}
        //internal ANode CreateNodeForExpression(ITypeSymbol type, SyntaxNode ex)
        //{
        //    ANode node;
        //    if (!expressionNodeCache.TryGetValue(ex, out node))
        //    {
        //        ISymbol s = semanticModel.GetSymbolInfo(ex).Symbol;
        //        node = ANode.Define(type, ex, s);
        //        expressionNodeCache[ex] = node;
        //    }
        //    return node;
        //}
        //internal ANode CreateNodeForExpression(ITypeSymbol type, SyntaxNodeOrToken ex, ISymbol s)
        //{
        //    ANode node = null;
        //    if (Utils.IsTypeForAnalysis(type))
        //    {
        //        if (!expressionNodeCache.TryGetValue(ex, out node))
        //        {
        //            node = ANode.Define(type, ex, s);
        //            expressionNodeCache[ex] = node;
        //        }
        //    }
        //    return node;
        //}

        //private AnalysisNode CreateNodeForParameter(ITypeSymbol type, IParameterSymbol parameterSymbol,int i)
        //{
        //    var paramExp = SyntaxFactory.ParseExpression("parameter" + i);
        //    if (parameterSymbol != null)
        //    {
        //        paramExp = SyntaxFactory.IdentifierName(parameterSymbol.Name);
        //        /// parameterSymbol.DeclaringSyntaxReferences.First();
        //    }
			
        //    return new Parameter(type, paramExp, parameterSymbol);
        //}
    }

    /// <summary>
    /// This is the visitor for one method
    /// It creates an entity for the method that includes a statment procesor. 
    /// It models the propagation graph for the intraprocedural analysis
    /// </summary>
    internal class MethodParser : GeneralRoslynMethodParser
    {
        private BaseMethodDeclarationSyntax methodNode;
        private SemanticModel model;
        //private SyntaxTree Tree;

        public MethodParser(SemanticModel model, SyntaxTree tree, IMethodSymbol method)
            : base(method)
        {            
            this.model = model;
            //this.Tree = tree;
            var root = tree.GetRoot();
            var visitor = new MethodFinder(method, model);
            visitor.Visit(root);
            this.methodNode = visitor.Result;
            Contract.Assert(this.methodNode != null);

            // Ben: this is just a test to make the AST simpler. Disregard this :-)
            //method = MethodSimpifier.SimplifyASTForMethod(ref methodNode, ref semanticModel);
        }

        public MethodParser(SemanticModel model, ProjectCodeProvider codeProvider, SyntaxTree tree, MethodDescriptor methodDescriptor)
            : base(methodDescriptor, codeProvider)
        {
            this.model = model;
            var pair = ProjectCodeProvider.FindMethodSyntaxAsync(codeProvider.Compilation.GetSemanticModel(tree), tree, methodDescriptor).Result;
            this.methodNode = pair.Item1;
            //this.Tree = tree;
            
            // Ben: this is just a test to make the AST simpler. Disregard this :-)
            //method = MethodSimpifier.SimplifyASTForMethod(ref methodNode, ref semanticModel);
        }

        public override MethodEntity ParseMethod()
        {
            Contract.Assert(this.methodNode != null);
            var propGraphGenerator = new MethodSyntaxVisitor(this.model, this.RoslynMethod, this);
            propGraphGenerator.Visit(this.methodNode);

            var descriptor = new MethodEntityDescriptor(propGraphGenerator.MethodDescriptor);  //EntityFactory.Create(this.MethodDescriptor, this.Dispatcher);
            var methodEntity = new MethodEntity(propGraphGenerator.MethodDescriptor,
                                                                    propGraphGenerator.MethodInterfaceData,
                                                                    propGraphGenerator.PropGraph, descriptor,
                                                                    propGraphGenerator.InstantiatedTypes,
                                                                    propGraphGenerator.StatementProcessor.AnonymousMethods);
            return methodEntity;
        }
    }

    internal class LambdaMethodParser : GeneralRoslynMethodParser
    {
        private SimpleLambdaExpressionSyntax lambdaExpression;
        private SemanticModel model;

        public LambdaMethodParser(SemanticModel model, SimpleLambdaExpressionSyntax node, IMethodSymbol method, MethodDescriptor methodDescriptor)
            : base(method, methodDescriptor)
        {
            this.model = model;
            this.lambdaExpression = node;
            this.MethodDescriptor = methodDescriptor;
        }

        public override MethodEntity ParseMethod()
        {
			Contract.Assert(this.lambdaExpression != null);
			var propGraphGenerator = new MethodSyntaxVisitor(this.model, this.RoslynMethod, this);
            propGraphGenerator.Visit(this.lambdaExpression);

            var descriptor = new MethodEntityDescriptor(propGraphGenerator.MethodDescriptor);  //EntityFactory.Create(this.MethodDescriptor, this.Dispatcher);
            var methodEntity = new MethodEntity(propGraphGenerator.MethodDescriptor,
                                                                    propGraphGenerator.MethodInterfaceData,
                                                                    propGraphGenerator.PropGraph, descriptor,
                                                                    propGraphGenerator.InstantiatedTypes,
                                                                    propGraphGenerator.StatementProcessor.AnonymousMethods);
            return methodEntity;
        }
    }

    internal class MethodSyntaxVisitor : CSharpSyntaxVisitor<object>
    {
        protected IMethodSymbol roslynMethod;
        internal int InvocationPosition { get; set; }

        public MethodDescriptor MethodDescriptor { get; private set; }
        internal MethodInterfaceData MethodInterfaceData { get; private set; }

        internal VariableNode RetVar
        {
            get { return MethodInterfaceData.ReturnVariable; }
        }

        internal PropGraphNodeDescriptor ThisRef
        {
            get { return MethodInterfaceData.ThisRef; }
        }

        internal IEnumerable<PropGraphNodeDescriptor> Parameters
        {
            get { return MethodInterfaceData.Parameters; }
        }

        // var mw1 = new MethodWorker<Expr, Type, String>("m1", rv1, thisRefm1, paramsm1);
        //MethodWorker<ANode, typeDescriptor, IMethodSymbol> worker;

        private SemanticModel model;
        //private BaseMethodDeclarationSyntax methodNode;
        private ExpressionVisitor expressionsVisitor;
        private GeneralRoslynMethodParser roslynMethodProcessor;

        internal StatementProcessor StatementProcessor
        {
            get { return this.roslynMethodProcessor.StatementProcessor; }
        }
        internal PropagationGraph PropGraph
        {
            get { return this.StatementProcessor.PropagationGraph; }
        }

        public IEnumerable<TypeDescriptor> InstantiatedTypes
        {
            get { return this.StatementProcessor.InstantiatedTypes; }
        }

        public MethodSyntaxVisitor(SemanticModel model,
                                    IMethodSymbol roslynMethod,
                                    GeneralRoslynMethodParser roslynMethodProcessor)
        {
            //this.methodNode = methodSyntaxNode;
            this.model = model;
            //this.roslynMethod = roslynMethod;
            this.roslynMethodProcessor = roslynMethodProcessor;

            this.MethodInterfaceData = roslynMethodProcessor.MethodInterfaceData;
            this.MethodDescriptor = roslynMethodProcessor.MethodDescriptor;
            this.roslynMethod = roslynMethod; //  model.GetDeclaredSymbol(this.methodNode);

            this.expressionsVisitor = new ExpressionVisitor(this.model, this.StatementProcessor, this);
            this.InvocationPosition = 0;
        }

		public override object VisitCompilationUnit(CompilationUnitSyntax node)
        {
            foreach (var member in node.Members)
            {
                this.Visit(member);
            }
            return null;
        }

        public override object VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
			if (node.ExpressionBody != null)
			{
				Visit(node.ExpressionBody);
			}

			if (node.Body != null)
			{
				Visit(node.Body);
			}

            return null;
        }

		public override object VisitArrowExpressionClause(ArrowExpressionClauseSyntax node)
		{
			var analysisExpression = expressionsVisitor.Visit(node.Expression);
			this.ProcessReturnAnalysisExpression(analysisExpression);
			return null;
		}

		public override object VisitSimpleLambdaExpression(SimpleLambdaExpressionSyntax node)
		{
			Visit(node.Body);
			return null;
		}

        public override object DefaultVisit(SyntaxNode node)
        {
            return base.DefaultVisit(node);
        }

		#region Statements

		public override object VisitBlock(BlockSyntax node)
        {
            foreach (var st in node.Statements)
            {
                this.Visit(st);
            }
            return null;
        }

		public override object VisitForEachStatement(ForEachStatementSyntax node)
		{
			var symbol = this.model.GetDeclaredSymbol(node);
			var type = symbol.Type;

			// This is the declaration of the iterarion variable
			var lhs = new Identifier(node.Identifier, type, symbol);
			var rhs = expressionsVisitor.Visit(node.Expression);
			this.RegisterAssignment(lhs, rhs);

			// This is the body of the ForEach
			Visit(node.Statement);

			return null;
			//return base.VisitForEachStatement(node);
		}

		public override object VisitSwitchStatement(SwitchStatementSyntax node)
        {
            expressionsVisitor.Visit(node.Expression);
            foreach (var section in node.Sections)
            {
                Visit(section);
            }
            return null;
        }

        public override object VisitSwitchSection(SwitchSectionSyntax node)
        {
            foreach (var statement in node.Statements)
            {
                Visit(statement);
            }

            return null;
        }

        public override object VisitLocalDeclarationStatement(LocalDeclarationStatementSyntax node)
        {
            AnalyzeDeclaration(node.Declaration);
            return null;
        }
        /// <summary>
        /// This method analyze a set of local variable declarations, i.e. C c = expression, D d = exp2,...;
        /// </summary>
        /// <param name="decl"></param>
        private void AnalyzeDeclaration(VariableDeclarationSyntax decl)
        {
            foreach (var v in decl.Variables)
            {
				var declaredVariableSymbol = (ILocalSymbol)this.model.GetDeclaredSymbol(v);
                // Register the variable in the PropGraph
                // Note it returns a Node, not an analysis expression
                var lhsAnalysisNode = RegisterVariable(v.Identifier, declaredVariableSymbol);

                if (v.Initializer != null)
                {
                    // This is the rhs of the assigment
                    // We first visit the expression because it may conatain invocations
                    var rhsAnalysisExpression = expressionsVisitor.Visit(v.Initializer.Value);
                    if (rhsAnalysisExpression != null)
                    {
                        rhsAnalysisExpression.ProcessAssignment(lhsAnalysisNode, this);
                    }
                }
            }
        }

        public override object VisitElseClause(ElseClauseSyntax node)
        {
            return Visit(node.Statement);
        }
        public override object VisitIfStatement(IfStatementSyntax node)
        {
            expressionsVisitor.Visit(node.Condition);

            Visit(node.Statement);
            Visit(node.Else);

            return null;
        }
        /// <summary>
        /// INCOMPLETE!
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public override object VisitForStatement(ForStatementSyntax node)
        {
            // Should do the same as LocalDeclarations
            AnalyzeDeclaration(node.Declaration);
            Visit(node.Statement);

			if (node.Condition != null)
			{
				expressionsVisitor.Visit(node.Condition);
			}

			foreach (var inc in node.Incrementors)
			{
				expressionsVisitor.Visit(inc);
			}

            return null;
        }
        public override object VisitExpressionStatement(ExpressionStatementSyntax node)
        {
            expressionsVisitor.Visit(node.Expression);
            return null;
        }

        public override object VisitReturnStatement(ReturnStatementSyntax node)
        {
			var analysisExpression = expressionsVisitor.Visit(node.Expression);
			this.ProcessReturnAnalysisExpression(analysisExpression);
            return null;
        }

		private void ProcessReturnAnalysisExpression(AnalysisExpression analysisExpression)
		{
			if (this.RetVar != null && analysisExpression != null)
			{
				analysisExpression.ProcessAssignment(this.RetVar, this);
			}
		}

        #endregion

        public override object VisitConstructorDeclaration(ConstructorDeclarationSyntax node)
        {
            VisitBlock(node.Body);
            return null;
        }

        public override object VisitDestructorDeclaration(DestructorDeclarationSyntax node)
        {
            VisitBlock(node.Body);
            return null;
        }

        public override object VisitAccessorDeclaration(AccessorDeclarationSyntax node)
        {
            if (node.Body != null)
            {
                VisitBlock(node.Body);
            }
            return null;
        }

        public override object VisitConversionOperatorDeclaration(ConversionOperatorDeclarationSyntax node)
        {
            VisitBlock(node.Body);
            return null;
        }

        public override object VisitOperatorDeclaration(OperatorDeclarationSyntax node)
        {
            VisitBlock(node.Body);
            return null;
        }

        #region Helpers to connect with statement processor
        internal VariableNode RegisterVariable(ExpressionSyntax v)
        {
            var tVar = this.model.GetTypeInfo(v);
            var s = this.model.GetSymbolInfo(v).Symbol;
            var lhs = RegisterVariable(v, tVar.Type, s);
            //var lhs = RegisterVariable(v, tVar.Type,s);
            return lhs;
        }

        internal VariableNode RegisterVariable(SyntaxNodeOrToken v, ILocalSymbol s)
        {
            return RegisterVariable(v, s.Type, s);
        }

        internal VariableNode RegisterVariable(SyntaxNodeOrToken v, ITypeSymbol type, ISymbol s)
        {
            VariableNode lhs;
            if (type.TypeKind.Equals(TypeKind.Delegate))
            {
				lhs = new DelegateVariableNode(v.ToString(), Utils.CreateTypeDescriptor(type));
            }
            else
            {
                lhs = new VariableNode(v.ToString(), Utils.CreateTypeDescriptor(type));
                //lhs = ANode.Define(t, v);
            }
            if (lhs != null) this.StatementProcessor.RegisterLocalVariable(lhs);
            return lhs;
        }

        internal void RegisterAssignment(AnalysisExpression lhs, AnalysisExpression rhs)
        {
            Contract.Assert(lhs is Field || lhs is Identifier);
            if (lhs != null)
            {
                this.RegisterAssignment(lhs.GetAnalysisNode(), rhs);
            }
        }

        internal void RegisterAssignment(PropGraphNodeDescriptor lhsNode, AnalysisExpression rhs)
        {
            if (lhsNode != null && rhs != null && Utils.IsTypeForAnalysis(rhs.GetAnalysisType()))
            {
                this.StatementProcessor.RegisterAssignment(lhsNode, rhs.GetAnalysisNode());
            }
        }

        internal void RegisterNewExpressionAssignment(VariableNode lhsNode, TypeDescriptor typeDescriptor)
        {
            this.StatementProcessor.RegisterNewExpressionAssignment(lhsNode, typeDescriptor);
        }

        internal void RegisterCallLHS(VariableNode lhsNode, AnalysisExpression rhsExpression)
        {
            Contract.Assert(rhsExpression.GetAnalysisNode() is AnalysisCallNode);

            var callNode = (AnalysisCallNode)rhsExpression.GetAnalysisNode();
            this.StatementProcessor.RegisterCallLHS(callNode, lhsNode);
        }

        internal void RegisterDelegate(VariableNode lhsNode, MethodDescriptor delegateMethodDescriptor)
        {
            Contract.Assert(lhsNode is DelegateVariableNode);
            this.StatementProcessor.RegisterDelegateAssignment(
                (DelegateVariableNode)lhsNode, delegateMethodDescriptor);
        }

        //public ANode RegisterExpression(ExpressionSyntax lhs)
        //{
        //    // We use the declared typed for Nodes abstraction
        //    var declaredTypeLHS = semanticModel.GetTypeInfo(lhs).Type;

        //    //var node = new MyNodes(declaredTypeLHS,lhs.GetLocation());
        //    var node = ANode.Define(declaredTypeLHS, lhs);
        //    if(node!=null) statementProcessor.RegisterLocalVar(node);
        //    return node;
        //}

        internal IDictionary<SyntaxNodeOrToken, PropGraphNodeDescriptor> expressionNodeCache = new Dictionary<SyntaxNodeOrToken, PropGraphNodeDescriptor>();
     
        #endregion
    }

	/// <summary>
	/// This is use to traverse a complete project and create a processor for each one.
	/// Another way is to do it on demand. I'm trying both options.
	/// </summary>
	internal class AllMethodsVisitor : CSharpSyntaxWalker
    {
        private IDispatcher dispatcher;
		private SemanticModel model;
        private SyntaxTree tree;

        internal AllMethodsVisitor(SemanticModel model, SyntaxTree tree, IDispatcher dispatcher)
        {
            this.model = model;
            this.tree = tree;
            this.dispatcher = dispatcher;
        }

        public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            var method = this.model.GetDeclaredSymbol(node);
            var processor = new MethodParser(this.model, this.tree, method);
            processor.ParseMethod();
        }

        public override void VisitConstructorDeclaration(ConstructorDeclarationSyntax node)
        {
            var method = this.model.GetDeclaredSymbol(node);
			var processor = new MethodParser(this.model, this.tree, method);
            processor.ParseMethod();
        }

		internal async Task Run(SyntaxTree tree)
		{
			var root = await tree.GetRootAsync();

			this.Visit(root);
		}
    }

	internal class MethodFinder : CSharpSyntaxWalker
	{
		private MethodDescriptor MethodDescriptor;
		private SemanticModel SemanticModel;
		internal BaseMethodDeclarationSyntax Result { get; private set; }

		internal MethodFinder(MethodDescriptor descriptor, SemanticModel semanticModel)
		{
			this.MethodDescriptor = descriptor;
			this.SemanticModel = semanticModel;
		}

		internal MethodFinder(IMethodSymbol symbol, SemanticModel semanticModel)
		{
			Contract.Assert(symbol != null);
			Contract.Assert(semanticModel != null);

			this.MethodDescriptor = Utils.CreateMethodDescriptor(symbol);
			this.SemanticModel = semanticModel;
		}
		//public override object VisitCompilationUnit(CompilationUnitSyntax node)
		//{
		//    foreach (var member in node.Members)
		//    {
		//        Visit(member);
		//    }
		//    return null;
		//}
		//public override object VisitClassDeclaration(ClassDeclarationSyntax node)
		//{
		//    foreach (var member in node.Members)
		//    {
		//        Visit(member);
		//    }

		//    return null;
		//}

		public override void Visit(SyntaxNode syntax)
		{
			var kind = syntax.Kind();
            switch (kind)
			{
				case SyntaxKind.MethodDeclaration:
					{
						var node = (MethodDeclarationSyntax)syntax;
						var symbol = this.SemanticModel.GetDeclaredSymbol(node);
						var thisDescriptor = Utils.CreateMethodDescriptor(symbol);
						if (thisDescriptor.Equals(this.MethodDescriptor))
						{
							// found it!
							this.Result = node;
						}
						break;
					}
				case SyntaxKind.ConstructorDeclaration:
					{
						var node = (ConstructorDeclarationSyntax)syntax;
						var symbol = this.SemanticModel.GetDeclaredSymbol(node);
						var thisDescriptor = Utils.CreateMethodDescriptor(symbol);
						if (thisDescriptor.Equals(this.MethodDescriptor))
						{
							// found it!
							this.Result = node;
						}
						break;
					}
			}

			base.Visit(syntax);
		}	
	}
}