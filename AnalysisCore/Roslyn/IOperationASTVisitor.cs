using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnalysisCore.Roslyn
{
    //public class IOperationASTVisitor
    //{
    //    #region Properties
    //    ITraceConsumer _traceConsumer;
    //    IAliasingSolver _aliasingSolver;
    //    IDependencyGraph _dependencyGraph;
    //    ISemanticModelsContainer _semanticModelsContainer;
    //    IControlManagement _controlManagement;
    //    Dictionary<Stmt, string> _statementsToNodes;
    //    List<Stmt> _executedStatements;
    //    InstrumentationResult _instrumentationResult;
    //    public Term _thisObject { get; set; }
    //    public Term _returnObject { get; set; }
    //    public string _returnVertex { get; set; }
    //    public InvocationData _lastNoInstrumentedInvocation { get; set; }
    //    OperationKind[] _conditionalStatements = { OperationKind.IfStatement, OperationKind.LoopStatement, OperationKind.SwitchStatement };
    //    TraceType[] _trazasDeSalida = { TraceType.ExitStaticMethod, TraceType.ExitMethod, TraceType.ExitStaticConstructor, TraceType.ExitConstructor };
    //    #endregion

    //    #region Constructor
    //    public ProcessConsumer(
    //        ITraceConsumer traceConsumer,
    //        IAliasingSolver aliasingSolver,
    //        IDependencyGraph dependencyGraph,
    //        ISemanticModelsContainer semanticModelsContainer,
    //        InstrumentationResult instrumentationResult,
    //        List<Stmt> executedStatements)
    //    {
    //        _traceConsumer = traceConsumer;
    //        _aliasingSolver = aliasingSolver;
    //        _dependencyGraph = dependencyGraph;
    //        _semanticModelsContainer = semanticModelsContainer;
    //        _instrumentationResult = instrumentationResult;
    //        _executedStatements = executedStatements;
    //        _controlManagement = new ControlManagement();
    //        _statementsToNodes = new Dictionary<Stmt, string>();
    //    }
    //    #endregion

    //    #region Public
    //    public string Process(Term term = null, List<Term> argumentList = null, Term @this = null)
    //    {
    //        var enterMethodStatement = _traceConsumer.GetNextStatement();
    //        var syntaxNode = enterMethodStatement.CSharpSyntaxNode;
    //        _thisObject = @this;
    //        _returnObject = term;
    //        switch (enterMethodStatement.TraceType)
    //        {
    //            case TraceType.EnterMethod:
    //                if (syntaxNode is MethodDeclarationSyntax)
    //                    _aliasingSolver.EnterMethodAndBind(argumentList, ((MethodDeclarationSyntax)syntaxNode).ParameterList.Parameters, _dependencyGraph, _instrumentationResult, @this);
    //                else
    //                    _aliasingSolver.EnterMethodAndBind(new List<Term>() { }, new SeparatedSyntaxList<ParameterSyntax>(), _dependencyGraph, _instrumentationResult, @this);
    //                break;
    //            case TraceType.EnterStaticMethod:
    //                if (argumentList != null)
    //                    _aliasingSolver.EnterStaticMethodAndBind(argumentList, ((MethodDeclarationSyntax)syntaxNode).ParameterList.Parameters, _dependencyGraph, _instrumentationResult);
    //                else
    //                    _aliasingSolver.EnterStaticMethodAndBind(new List<Term>() { }, new SeparatedSyntaxList<ParameterSyntax>(), _dependencyGraph, _instrumentationResult);
    //                break;
    //            case TraceType.EnterConstructor:
    //                var parameterList = (syntaxNode is ConstructorDeclarationSyntax) ? ((ConstructorDeclarationSyntax)syntaxNode).ParameterList.Parameters : new SeparatedSyntaxList<ParameterSyntax>();
    //                _aliasingSolver.EnterConstructorAndBind(argumentList, parameterList, _dependencyGraph, _instrumentationResult);
    //                break;
    //            case TraceType.EnterStaticConstructor:
    //                _aliasingSolver.EnterConstructorAndBind(new List<Term>() { }, new SeparatedSyntaxList<ParameterSyntax>(), _dependencyGraph, _instrumentationResult);
    //                break;
    //        }

    //        BodyConsume();
    //        return _returnVertex;
    //    }
    //    #endregion

    //    #region Private
    //    void BodyConsume()
    //    {
    //        while (_traceConsumer.HasNext() && _returnVertex == null)
    //        {
    //            var currentStatement = _traceConsumer.ObserveNextStatement();
    //            switch (currentStatement.TraceType)
    //            {
    //                case TraceType.EnterCondition:
    //                    {
    //                        currentStatement = _traceConsumer.GetNextStatement();
    //                        _controlManagement.EnterCondition(currentStatement);
    //                        break;
    //                    }
    //                case TraceType.Break:
    //                    {
    //                        currentStatement = _traceConsumer.GetNextStatement();
    //                        _controlManagement.Break();
    //                        break;
    //                    }
    //                case TraceType.ExitCondition:
    //                    {
    //                        currentStatement = _traceConsumer.GetNextStatement();
    //                        _controlManagement.ExitCondition();
    //                        break;
    //                    }
    //                case TraceType.EnterStaticMethod:
    //                case TraceType.EnterMethod:
    //                case TraceType.EnterStaticConstructor:
    //                case TraceType.EnterConstructor:
    //                    {
    //                        // CALLBACKS
    //                        var parametersCount = ((MethodDeclarationSyntax)currentStatement.CSharpSyntaxNode).ParameterList.Parameters.Count();
    //                        if (_lastNoInstrumentedInvocation != null)
    //                        {
    //                            var verticesDependientes = _aliasingSolver.LastDef_Get(_lastNoInstrumentedInvocation.ResultTerm, _dependencyGraph, _instrumentationResult);
    //                            string result = ConsumeInvocation(null, Enumerable.Repeat(_lastNoInstrumentedInvocation.ResultTerm, parametersCount).ToList(), _lastNoInstrumentedInvocation.ResultTerm);
    //                            if (result != null)
    //                                foreach (var verticeDependiente in verticesDependientes)
    //                                    _dependencyGraph.AddEdge(verticeDependiente, result);
    //                        }
    //                        else
    //                            ConsumeInvocation();
    //                        break;
    //                    }
    //                case TraceType.ExitStaticMethod:
    //                case TraceType.ExitMethod:
    //                case TraceType.ExitStaticConstructor:
    //                case TraceType.ExitConstructor:
    //                    {
    //                        currentStatement = _traceConsumer.GetNextStatement();
    //                        ExitMethod(currentStatement);
    //                        return;
    //                    }
    //                default:
    //                    {
    //                        currentStatement = _traceConsumer.GetNextStatement();
    //                        StatementConsume(currentStatement);
    //                        break;
    //                    }
    //            }
    //        }
    //    }

    //    public void ExitMethod(Stmt currentStatement)
    //    {
    //        // Actualizamos la estructura de variables (scope)
    //        // TODO:
    //        switch (currentStatement.TraceType)
    //        {
    //            case TraceType.ExitStaticMethod:
    //                _aliasingSolver.ExitMethodAndUnbind(_returnObject);
    //                break;
    //            case TraceType.ExitMethod:
    //                _aliasingSolver.ExitMethodAndUnbind(_returnObject);
    //                break;
    //            case TraceType.ExitStaticConstructor:
    //                _aliasingSolver.ExitMethodAndUnbind(null);
    //                break;
    //            case TraceType.ExitConstructor:
    //                _aliasingSolver.ExitMethodAndUnbind(_returnObject);
    //                break;
    //            default:
    //                _aliasingSolver.ExitMethodAndUnbind(_returnObject);
    //                break;
    //        }
    //    }

    //    void StatementConsume(Stmt currentStatement)
    //    {
    //        _executedStatements.Add(currentStatement);
    //        _sliceCriteriaReached = _traceConsumer.SliceCriteriaReached();
    //        _originalStatement = currentStatement;
    //        var syntaxNode = (CSharpSyntaxNode)(currentStatement.CSharpSyntaxNode);
    //        var semanticModel = _semanticModelsContainer.GetBySyntaxNode(syntaxNode);
    //        _operation = semanticModel.GetOperation(syntaxNode);
    //        CleanProperties();

    //        if (_operation == null && (syntaxNode is FieldDeclarationSyntax || syntaxNode is VariableDeclaratorSyntax))
    //        {
    //            #region Fields declaration
    //            _definedVariable =
    //                (syntaxNode is FieldDeclarationSyntax ?
    //            Create(semanticModel, ((FieldDeclarationSyntax)syntaxNode).Declaration.Variables.First()) :
    //            Create(semanticModel, ((VariableDeclaratorSyntax)syntaxNode)));

    //            _operation = currentStatement.CSharpSyntaxNode is FieldDeclarationSyntax ?
    //                semanticModel.GetOperation(((FieldDeclarationSyntax)currentStatement.CSharpSyntaxNode).Declaration.Variables.First().Initializer.Value) :
    //                semanticModel.GetOperation(((VariableDeclaratorSyntax)currentStatement.CSharpSyntaxNode).Initializer.Value);

    //            Visit(_operation);

    //            if (_isAllocating)
    //                _aliasingSolver.Alloc(_definedVariable);
    //            var vertice = AddVertex();

    //            _aliasingSolver.LastDef_Set(_definedVariable, vertice);

    //            // TODO: esta mal acá
    //            foreach (var property in _properties)
    //            {
    //                var completeTerm = _definedVariable.AddingField(property.Item1.ToString());
    //                _aliasingSolver.LastDef_Set(completeTerm, vertice);
    //                if (!property.Item2.IsScalar)
    //                    _aliasingSolver.Assign(completeTerm, property.Item2);
    //            }

    //            if (!_isAllocating && _invocationData == null &&
    //                _isAssignament && (!_definedVariable.IsScalar || !_usedVariables.First().IsScalar))
    //                _aliasingSolver.Assign(_definedVariable, _usedVariables.First());

    //            #endregion
    //        }
    //        else
    //            Visit(_operation);
    //    }
    //    #endregion

    //    #region Visitor
    //    #region Properties
    //    Stmt _originalStatement;
    //    IOperation _operation;
    //    InvocationData _invocationData;
    //    bool _sliceCriteriaReached;
    //    List<Term> _usedVariables;
    //    Term _definedVariable;
    //    bool _isAssignament;
    //    bool _isAllocating;
    //    string _invocationVertex;
    //    List<Tuple<Term, Term>> _properties;

    //    void CleanProperties()
    //    {
    //        _usedVariables = new List<Term>();
    //        _invocationData = null;
    //        _isAssignament = false;
    //        _isAllocating = false;
    //        _invocationVertex = null;
    //        _definedVariable = null;
    //        _properties = new List<Tuple<Term, Term>>();
    //    }
    //    #endregion

    //    #region Visits
    //    Term Visit(IOperation operation)
    //    {
    //        if (operation == null)
    //            return null;
    //        if (operation.Kind == OperationKind.ExpressionStatement)
    //            Visit(((IExpressionStatement)operation).Expression);
    //        if (operation.Kind == OperationKind.ConversionExpression)
    //            Visit(((IConversionExpression)operation).Operand);
    //        if (operation.Kind == OperationKind.EmptyStatement)
    //            VisitEmptyStatement(operation);
    //        if (operation.Kind == OperationKind.AssignmentExpression)
    //            VisitAssignmentExpression((IAssignmentExpression)operation);
    //        if (operation.Kind == OperationKind.IncrementExpression)
    //            VisitIncrementExpression((IIncrementExpression)operation);
    //        if (operation.Kind == OperationKind.BinaryOperatorExpression)
    //            VisitBinaryOperatorExpression((IBinaryOperatorExpression)operation);
    //        if (operation.Kind == OperationKind.IfStatement)
    //            VisitIfStatement((IIfStatement)operation);
    //        if (operation.Kind == OperationKind.LoopStatement && ((ILoopStatement)operation).LoopKind != LoopKind.ForEach)
    //            VisitForWhileUntilLoopStatement((IForWhileUntilLoopStatement)operation);
    //        if (operation.Kind == OperationKind.LoopStatement && ((ILoopStatement)operation).LoopKind == LoopKind.ForEach)
    //            VisitForEachLoopStatement((IForEachLoopStatement)operation);
    //        if (operation.Kind == OperationKind.SwitchStatement)
    //            VisitSwitchStatement((ISwitchStatement)operation);
    //        if (operation.Kind == OperationKind.VariableDeclarationStatement)
    //            VisitVariableDeclarationStatement((IVariableDeclarationStatement)operation);
    //        if (operation.Kind == OperationKind.TypeOperationExpression)
    //            VisitTypeOperationExpression((ITypeOperationExpression)operation);
    //        if (operation.Kind == OperationKind.ArrayCreationExpression)
    //            VisitArrayCreationExpression((IArrayCreationExpression)operation);
    //        if (operation.Kind == OperationKind.None && operation.Syntax is AnonymousObjectCreationExpressionSyntax)
    //            VisitAnonymousObjectCreationExpressionSyntax((AnonymousObjectCreationExpressionSyntax)operation.Syntax);
    //        if (operation.Kind == OperationKind.None && operation.Syntax is InitializerExpressionSyntax)
    //            VisitInitializerExpressionSyntax((InitializerExpressionSyntax)operation.Syntax);
    //        if (operation.Kind == OperationKind.None && operation.Syntax is ElementAccessExpressionSyntax)
    //            VisitElementAccessExpressionSyntax((ElementAccessExpressionSyntax)operation.Syntax);
    //        if (operation.Kind == OperationKind.NullCoalescingExpression)
    //            VisitNullCoalescingExpression((INullCoalescingExpression)operation);
    //        if (operation.Kind == OperationKind.ReturnStatement)
    //            VisitReturnStatement((IReturnStatement)operation);
    //        if (operation.Kind == OperationKind.InvocationExpression)
    //            VisitInvocationExpression((IInvocationExpression)operation);
    //        if (operation.Kind == OperationKind.ObjectCreationExpression)
    //            VisitObjectCreationExpression((IObjectCreationExpression)operation);
    //        if (operation.Kind == OperationKind.LocalReferenceExpression)
    //            VisitLocalReferenceExpression((ILocalReferenceExpression)operation);
    //        if (operation.Kind == OperationKind.FieldReferenceExpression)
    //            VisitFieldReferenceExpression((IFieldReferenceExpression)operation);
    //        if (operation.Kind == OperationKind.PropertyReferenceExpression)
    //            VisitPropertyReferenceExpression((IPropertyReferenceExpression)operation);
    //        if (operation.Kind == OperationKind.ParameterReferenceExpression)
    //            VisitParameterReferenceExpression((IParameterReferenceExpression)operation);
    //        if (operation.Kind == OperationKind.ArrayElementReferenceExpression)
    //            VisitArrayElementReferenceExpression((IArrayElementReferenceExpression)operation);
    //        if (operation.Kind == OperationKind.InstanceReferenceExpression)
    //            return VisitInstanceReferenceExpression((IInstanceReferenceExpression)operation);
    //        if (operation.Kind == OperationKind.LambdaExpression)
    //            return VisitLambdaExpression((ILambdaExpression)operation);
    //        if (operation.Kind == OperationKind.LiteralExpression)
    //            return VisitLiteralExpression((ILiteralExpression)operation);

    //        return null;
    //    }

    //    void VisitEmptyStatement(IOperation operation)
    //    {
    //        AddVertex();
    //    }

    //    void VisitAssignmentExpression(IAssignmentExpression operation)
    //    {
    //        _definedVariable = Create(operation.Target);
    //        Visit(operation.Value);

    //        var vertex = AddVertex();
    //        _aliasingSolver.LastDef_Set(_definedVariable, vertex);

    //        foreach (var property in _properties)
    //        {
    //            var completeTerm = _definedVariable.AddingField(property.Item1.ToString());
    //            _aliasingSolver.LastDef_Set(completeTerm, vertex);
    //            if (!property.Item2.IsScalar)
    //                _aliasingSolver.Assign(completeTerm, property.Item2);
    //        }

    //        if (_invocationData == null &&
    //            _isAssignament && (!_definedVariable.IsScalar || !_usedVariables.First().IsScalar))
    //            _aliasingSolver.Assign(_definedVariable, _usedVariables.First());
    //    }

    //    void VisitIncrementExpression(IIncrementExpression operation)
    //    {
    //        var term = Create(operation.Target);
    //        var vertex = AddVertex();
    //        _aliasingSolver.LastDef_Set(term, vertex);
    //    }

    //    void VisitBinaryOperatorExpression(IBinaryOperatorExpression operation)
    //    {
    //        Visit(operation.Left);
    //        Visit(operation.Right);
    //    }

    //    void VisitIfStatement(IIfStatement operation)
    //    {
    //        Visit(((IIfStatement)operation).Condition);
    //        var vertex = AddVertex();
    //        UpdateStatementsDictionary(vertex);
    //    }

    //    void VisitForWhileUntilLoopStatement(IForWhileUntilLoopStatement operation)
    //    {
    //        Visit(((IForWhileUntilLoopStatement)operation).Condition);
    //        var vertex = AddVertex();
    //        UpdateStatementsDictionary(vertex);
    //    }

    //    void VisitForEachLoopStatement(IForEachLoopStatement operation)
    //    {
    //        _isAssignament = true;
    //        var term = Create(operation.Collection);
    //        _usedVariables.Add(term);

    //        _definedVariable = Create(operation.IterationVariable);
    //        Term resultParam = null;
    //        if (!_definedVariable.IsScalar)
    //        {
    //            _aliasingSolver.Alloc(_definedVariable);
    //            resultParam = _definedVariable;
    //        }

    //        var vertex = AddVertex();
    //        _aliasingSolver.LastDef_Set(_definedVariable, vertex);

    //        var havocArgs = new Term[] { term };
    //        _aliasingSolver.Havoc(havocArgs, resultParam, vertex);
    //        foreach (var argTerm in havocArgs)
    //        {
    //            var listSigma = Copy(argTerm, argTerm.ToString() + "._any_");
    //            _aliasingSolver.LastDef_Get(listSigma, _dependencyGraph, _instrumentationResult).ToList().ForEach(x => _dependencyGraph.AddEdge(vertex, x));
    //        }

    //        UpdateStatementsDictionary(vertex);
    //    }

    //    void VisitSwitchStatement(ISwitchStatement operation)
    //    {
    //        Visit(operation.Value);
    //        var vertex = AddVertex();
    //        UpdateStatementsDictionary(vertex);
    //    }

    //    void VisitVariableDeclarationStatement(IVariableDeclarationStatement operation)
    //    {
    //        var definedVariables = operation.Variables.Select(x => Create(x)).ToList();
    //        if (definedVariables.Count == 1)
    //            _definedVariable = definedVariables.First();

    //        operation.Variables.ToList().ForEach(x => Visit(x.InitialValue));

    //        var vertice = AddVertex();

    //        foreach (var variableDefinida in definedVariables)
    //            _aliasingSolver.LastDef_Set(variableDefinida, vertice);

    //        // TODO: esta mal acá
    //        foreach (var property in _properties)
    //        {
    //            var completeTerm = _definedVariable.AddingField(property.Item1.ToString());
    //            _aliasingSolver.LastDef_Set(completeTerm, vertice);
    //            if (!property.Item2.IsScalar)
    //                _aliasingSolver.Assign(completeTerm, property.Item2);
    //        }

    //        if (_invocationData == null &&
    //            _isAssignament && (!definedVariables.First().IsScalar || !_usedVariables.First().IsScalar))
    //            _aliasingSolver.Assign(definedVariables.First(), _usedVariables.First());
    //    }

    //    void VisitTypeOperationExpression(ITypeOperationExpression operation)
    //    {
    //        if (_definedVariable != null)
    //            _aliasingSolver.Alloc(_definedVariable);
    //    }

    //    void VisitArrayCreationExpression(IArrayCreationExpression operation)
    //    {
    //        if (operation.Syntax is ArrayCreationExpressionSyntax && ((ArrayCreationExpressionSyntax)operation.Syntax).Initializer != null)
    //            ((ArrayCreationExpressionSyntax)operation.Syntax).Initializer.Expressions.ToList().ForEach(x => Visit(_semanticModelsContainer.GetBySyntaxNode(x).GetOperation(x)));
    //        if (operation.Syntax is ImplicitArrayCreationExpressionSyntax && ((ImplicitArrayCreationExpressionSyntax)operation.Syntax).Initializer != null)
    //            ((ImplicitArrayCreationExpressionSyntax)operation.Syntax).Initializer.Expressions.ToList().ForEach(x => Visit(_semanticModelsContainer.GetBySyntaxNode(x).GetOperation(x)));

    //        var ejesDependientes = _usedVariables.Select(x => _aliasingSolver.LastDef_Get(x, _dependencyGraph, _instrumentationResult)).SelectMany(x => x).ToList();
    //        var controlVertex = _controlManagement.GetControlStmt();
    //        if (controlVertex != null)
    //            ejesDependientes.Add(_statementsToNodes[controlVertex]);

    //        var vertice = _dependencyGraph.AddVertex(this._originalStatement, new HashSet<string>(ejesDependientes));
    //        if (_sliceCriteriaReached)
    //            _dependencyGraph.SliceCriteriaReached = true;

    //        Term havocTerm = null;
    //        if (!_definedVariable.IsScalar)
    //        {
    //            _aliasingSolver.Alloc(_definedVariable);
    //            havocTerm = _definedVariable;
    //        }
    //        else
    //            havocTerm = _aliasingSolver.FreshVar();
    //        _aliasingSolver.Havoc(new List<Term>(), havocTerm, vertice);

    //        _invocationData = new InvocationData(null, null, operation, havocTerm);

    //        _invocationVertex = vertice;
    //    }

    //    void VisitAnonymousObjectCreationExpressionSyntax(AnonymousObjectCreationExpressionSyntax operation)
    //    {
    //        if (_definedVariable != null)
    //            _aliasingSolver.Alloc(_definedVariable);

    //        var properties = operation.Initializers;

    //        foreach (var property in properties)
    //        {
    //            var p_semanticModel = _semanticModelsContainer.GetBySyntaxNode(property.Expression);
    //            var p_operation = p_semanticModel.GetOperation(property.Expression);

    //            var newTerm = CreateArgument(p_operation);
    //            _usedVariables.Add(newTerm);
    //            _properties.Add(new Tuple<Term, Term>(new Term(property.NameEquals.Name.Identifier.ValueText), newTerm));
    //        }
    //    }

    //    void VisitInitializerExpressionSyntax(InitializerExpressionSyntax operation)
    //    {
    //        _isAllocating = true;
    //        if (operation.Expressions != null)
    //            operation.Expressions.ToList().ForEach(x => Visit(_semanticModelsContainer.GetBySyntaxNode(x).GetOperation(x)));
    //    }

    //    void VisitElementAccessExpressionSyntax(ElementAccessExpressionSyntax operation)
    //    {
    //        var p_semanticModel = _semanticModelsContainer.GetBySyntaxNode(operation.Expression);
    //        var p_operation = p_semanticModel.GetOperation(operation.Expression);
    //        var recTerm = Create(p_operation);

    //        var terms = new List<Term>() { recTerm };
    //        operation.ArgumentList.Arguments.ToList().ForEach(x => terms.Add(Create(p_semanticModel.GetOperation(x.Expression))));

    //        string spanString = ToStringUtils.SpanStringFromSyntaxNode(operation);
    //        var newTerm = new Term(spanString) { UseNode = operation };
    //        newTerm.IsScalar = !p_semanticModel.GetTypeInfo(operation.Expression).ConvertedType.AllInterfaces.First().TypeArguments.First().CustomIsReferenceType();
    //        newTerm.IsGlobal = recTerm.IsGlobal;
    //        Stmt statement = StmtUtil.StmtFromSyntaxNode(operation, _instrumentationResult);
    //        var ejesDependientes = terms.Select(x => _aliasingSolver.LastDef_Get(x, _dependencyGraph, _instrumentationResult)).SelectMany(x => x).ToList();
    //        var vertex = _dependencyGraph.AddVertex(statement, new HashSet<string>(ejesDependientes));

    //        _aliasingSolver.LastDef_Set(newTerm, vertex);

    //        if (!newTerm.IsScalar)
    //            _aliasingSolver.Alloc(newTerm);

    //        var havocArgs = terms.Where(x => x != null && !x.IsScalar).ToList();
    //        _aliasingSolver.Havoc(havocArgs, recTerm, vertex);
    //        foreach (var argTerm in havocArgs)
    //        {
    //            var listSigma = Copy(argTerm, argTerm.ToString() + "._any_");
    //            _aliasingSolver.LastDef_Get(listSigma, _dependencyGraph, _instrumentationResult).ToList().ForEach(x => _dependencyGraph.AddEdge(vertex, x));
    //        }

    //        _invocationVertex = vertex;
    //    }

    //    void VisitNullCoalescingExpression(INullCoalescingExpression operation)
    //    {
    //        // TODO: El Primary va siempre. Lo que no va siempre es lo 2do. Decisión conservadora
    //        Visit(operation.Primary);
    //        Visit(operation.Secondary);
    //    }

    //    void VisitReturnStatement(IReturnStatement operation)
    //    {
    //        var _returnTerm = Create(((IReturnStatement)operation).Returned);
    //        if (_returnTerm != null)
    //            _usedVariables.Add(_returnTerm);

    //        var vertice = AddVertex();

    //        _returnVertex = vertice;
    //        if (_returnTerm != null && !_returnTerm.IsScalar)
    //            _aliasingSolver.AssignRV(_returnTerm);
    //        ExitMethod(this._originalStatement);
    //    }

    //    void VisitInvocationExpression(IInvocationExpression operation)
    //    {
    //        var thisTerm = Create(operation.Instance);
    //        var argumentos = operation.ArgumentsInParameterOrder.Select(x => CreateArgument(x.Value)).ToList();

    //        var ejesDependientes = new List<string>();
    //        var controlVertex = _controlManagement.GetControlStmt();
    //        if (controlVertex != null)
    //            ejesDependientes.Add(_statementsToNodes[controlVertex]);

    //        if (thisTerm != null && (!operation.IsInSource()))
    //            ejesDependientes.AddRange(_aliasingSolver.LastDef_Get(thisTerm, _dependencyGraph, _instrumentationResult));
    //        if (!operation.IsInSource())
    //            ejesDependientes.AddRange(argumentos.Where(x => x != null)
    //                .SelectMany(x => _aliasingSolver.LastDef_Get(x, _dependencyGraph, _instrumentationResult)));

    //        var vertice = _dependencyGraph.AddVertex(_originalStatement, new HashSet<string>(ejesDependientes));
    //        if (_sliceCriteriaReached)
    //            _dependencyGraph.SliceCriteriaReached = true;

    //        Term resultParam = null;
    //        if (operation.IsInSource())
    //        {
    //            // El bind term es a donde se asignan los resultados no escalares en la llamadas a las funciones
    //            Term bindTerm = _definedVariable != null && !_definedVariable.IsScalar ? _definedVariable : null;

    //            var retorno = ConsumeInvocation(bindTerm, argumentos, thisTerm ?? _thisObject);
    //            // Si hubo un return, agregamos un eje al mismo
    //            if (retorno != null)
    //                _dependencyGraph.AddEdge(vertice, retorno);
    //        }
    //        else
    //        {
    //            _isAllocating = operation.ResultType.CustomIsReferenceType();
    //            if (operation.TargetMethod.MethodKind != MethodKind.DelegateInvoke)
    //            {
    //                var havocArgs = argumentos.Where(x => x != null && !x.IsScalar).ToList();

    //                if (thisTerm != null)
    //                    havocArgs.Add(thisTerm);
    //                if (_definedVariable != null && !_definedVariable.IsScalar)
    //                    resultParam = _definedVariable;
    //                else
    //                    resultParam = _aliasingSolver.FreshVar();
    //                _aliasingSolver.Havoc(havocArgs, resultParam, vertice);
    //                foreach (var argTerm in havocArgs)
    //                {
    //                    var listSigma = Copy(argTerm, argTerm.ToString() + "._any_");
    //                    _aliasingSolver.LastDef_Get(listSigma, _dependencyGraph, _instrumentationResult).ToList().ForEach(x => _dependencyGraph.AddEdge(vertice, x));
    //                }
    //            }
    //        }

    //        _invocationData = new InvocationData(thisTerm, argumentos, operation, resultParam);
    //        if (!_operation.IsInSource())
    //            _lastNoInstrumentedInvocation = _invocationData;

    //        _invocationVertex = vertice;
    //    }

    //    void VisitObjectCreationExpression(IObjectCreationExpression operation)
    //    {
    //        if (((ObjectCreationExpressionSyntax)operation.Syntax).Initializer != null)
    //            ((ObjectCreationExpressionSyntax)operation.Syntax).Initializer.Expressions
    //                .Where(x => !(x is AssignmentExpressionSyntax)).ToList()
    //                .ForEach(x => Visit(_semanticModelsContainer.GetBySyntaxNode(x).GetOperation(x)));

    //        if (operation.IsInSource() && _traceConsumer.ObserveNextStatement().TraceType == TraceType.EnterStaticConstructor)
    //            ConsumeInvocation();

    //        var ejesDependientes = _usedVariables.Select(x => _aliasingSolver.LastDef_Get(x, _dependencyGraph, _instrumentationResult)).SelectMany(x => x).ToList();
    //        var controlVertex = _controlManagement.GetControlStmt();
    //        if (controlVertex != null)
    //            ejesDependientes.Add(_statementsToNodes[controlVertex]);

    //        var argumentos = operation.ConstructorArguments.Select(x => CreateArgument(x.Value)).ToList();

    //        if (!operation.IsInSource())
    //            ejesDependientes.AddRange(argumentos.Where(x => x != null)
    //                .SelectMany(x => _aliasingSolver.LastDef_Get(x, _dependencyGraph, _instrumentationResult)));

    //        var vertice = _dependencyGraph.AddVertex(this._originalStatement, new HashSet<string>(ejesDependientes));
    //        if (_sliceCriteriaReached)
    //            _dependencyGraph.SliceCriteriaReached = true;

    //        Term havocTerm = null;
    //        if (operation.IsInSource())
    //        {
    //            // El bind term es a donde se asignan los resultados no escalares en la llamadas a las funciones
    //            var bindTerm = _definedVariable != null && !_definedVariable.IsScalar ? _definedVariable : null;

    //            var retorno = ConsumeInvocation(bindTerm, argumentos, null);
    //            // Si hubo un return, agregamos un eje al mismo
    //            if (retorno != null)
    //                _dependencyGraph.AddEdge(vertice, retorno);
    //        }
    //        else
    //        {
    //            var havocParams = argumentos.Where(x => x != null && !x.IsScalar).ToList();
    //            if (!_definedVariable.IsScalar)
    //            {
    //                _aliasingSolver.Alloc(_definedVariable);
    //                havocTerm = _definedVariable;
    //            }
    //            else
    //                havocTerm = _aliasingSolver.FreshVar();
    //            _aliasingSolver.Havoc(havocParams, havocTerm, vertice);
    //            foreach (var argTerm in havocParams)
    //            {
    //                var listSigma = Copy(argTerm, argTerm.ToString() + "._any_");
    //                _aliasingSolver.LastDef_Get(listSigma, _dependencyGraph, _instrumentationResult).ToList().ForEach(x => _dependencyGraph.AddEdge(vertice, x));
    //            }
    //        }

    //        if (((ObjectCreationExpressionSyntax)operation.Syntax).Initializer != null)
    //        {
    //            var expressions = ((ObjectCreationExpressionSyntax)operation.Syntax).Initializer.Expressions.Where(x => x is AssignmentExpressionSyntax);

    //            foreach (var expression in expressions)
    //            {
    //                var ioperation_expression = _semanticModelsContainer.GetBySyntaxNode(expression).GetOperation(expression);
    //                var left = new Term(((AssignmentExpressionSyntax)expression).Left.ToString());
    //                var right = CreateArgument(((IAssignmentExpression)ioperation_expression).Value);
    //                _usedVariables.Add(right);
    //                _properties.Add(new Tuple<Term, Term>(left, right));
    //            }
    //        }

    //        _invocationData = new InvocationData(null, argumentos, operation, havocTerm);
    //        if (!operation.IsInSource())
    //            _lastNoInstrumentedInvocation = _invocationData;

    //        _invocationVertex = vertice;
    //    }

    //    void VisitLocalReferenceExpression(ILocalReferenceExpression operation)
    //    {
    //        _isAssignament = true;
    //        var newTerm = new Term(operation.Local.Name, false);
    //        newTerm.IsScalar = !(operation.Local.Type.CustomIsReferenceType());
    //        newTerm.IsGlobal = false;
    //        _usedVariables.Add(newTerm);
    //    }

    //    void VisitFieldReferenceExpression(IFieldReferenceExpression operation)
    //    {
    //        _isAssignament = true;
    //        _usedVariables.Add(Create(operation));
    //    }

    //    void VisitPropertyReferenceExpression(IPropertyReferenceExpression operation)
    //    {
    //        _isAssignament = true;
    //        _usedVariables.Add(Create(operation));
    //    }

    //    void VisitParameterReferenceExpression(IParameterReferenceExpression operation)
    //    {
    //        _isAssignament = true;
    //        var newTerm = new Term(operation.Parameter.Name, false);
    //        newTerm.IsScalar = !(operation.Parameter.Type.CustomIsReferenceType());
    //        newTerm.IsGlobal = false;
    //        _usedVariables.Add(newTerm);
    //    }

    //    void VisitArrayElementReferenceExpression(IArrayElementReferenceExpression operation)
    //    {
    //        _isAssignament = true;
    //        _usedVariables.Add(Create(operation));
    //    }

    //    Term VisitInstanceReferenceExpression(IInstanceReferenceExpression propertyReferenceExpression)
    //    {
    //        var newTerm = new Term("this", false);
    //        newTerm.IsScalar = false;
    //        return newTerm;
    //    }

    //    Term VisitLambdaExpression(ILambdaExpression operation)
    //    {
    //        string spanString = ToStringUtils.SpanStringFromSyntaxNode((LambdaExpressionSyntax)operation.Syntax);
    //        var newTerm = new Term(spanString) { UseNode = (LambdaExpressionSyntax)operation.Syntax };
    //        newTerm.IsScalar = true;
    //        newTerm.IsGlobal = false;
    //        Stmt statement = StmtUtil.StmtFromSyntaxNode((LambdaExpressionSyntax)operation.Syntax, _instrumentationResult);
    //        var vertex = _dependencyGraph.AddVertex(statement, new HashSet<string>());

    //        _aliasingSolver.LastDef_Set(newTerm, vertex);

    //        var bodyStatement = operation.Body.Statements.First();
    //        var recTerm = Create(bodyStatement);
    //        if (recTerm != null)
    //        {
    //            var ejesDependientes = _aliasingSolver.LastDef_Get(recTerm, _dependencyGraph, _instrumentationResult);
    //            foreach (var ejeDependiente in ejesDependientes)
    //                _dependencyGraph.AddEdge(vertex, ejeDependiente);
    //        }

    //        return newTerm;
    //    }

    //    Term VisitLiteralExpression(ILiteralExpression literalExpression)
    //    {
    //        string spanString = ToStringUtils.SpanStringFromSyntaxNode((CSharpSyntaxNode)literalExpression.Syntax);
    //        var newTerm = new Term(spanString) { UseNode = (CSharpSyntaxNode)literalExpression.Syntax };
    //        newTerm.IsScalar = true;
    //        newTerm.IsGlobal = false;
    //        Stmt statement = StmtUtil.StmtFromSyntaxNode((CSharpSyntaxNode)literalExpression.Syntax, _instrumentationResult);
    //        var vertex = _dependencyGraph.AddVertex(statement, new HashSet<string>());

    //        _aliasingSolver.LastDef_Set(newTerm, vertex);
    //        return newTerm;
    //    }
    //    #endregion

    //    #region TermVisitor
    //    public Term Create(IVariable variable)
    //    {
    //        if (variable == null)
    //            return null;

    //        var newTerm = new Term(variable.Variable.Name, false);
    //        newTerm.IsGlobal = variable.Variable.IsStatic;
    //        newTerm.IsScalar = !(variable.Variable.Type.CustomIsReferenceType());

    //        return newTerm;
    //    }

    //    public static Term Create(SemanticModel semanticModel, VariableDeclaratorSyntax syntaxNode)
    //    {
    //        var isGlobal = ((FieldDeclarationSyntax)syntaxNode.Parent.Parent).Modifiers.Any(x => x.ToString() == "static" || x.ToString() == "const");
    //        Term newTerm = null;
    //        if (!isGlobal)
    //        {
    //            var fields = new List<Field>();
    //            fields.Add(new Field("this", true, false));
    //            fields.Add(new Field(syntaxNode.Identifier.ValueText, false, false));
    //            newTerm = new Term(fields, false);
    //        }
    //        else
    //            newTerm = new Term(semanticModel.GetDeclaredSymbol(syntaxNode).ToString());

    //        var typeInfo = semanticModel.GetTypeInfo(syntaxNode.Initializer.Value).ConvertedType;
    //        newTerm.IsScalar = !typeInfo.CustomIsReferenceType();
    //        newTerm.IsGlobal = isGlobal;
    //        return newTerm;
    //    }

    //    public Term Create(IOperation operation)
    //    {
    //        if (operation == null)
    //            return null;
    //        if (operation is IConversionExpression)
    //            return Create(((IConversionExpression)operation).Operand);
    //        if (operation is IReturnStatement)
    //            return Create(((IReturnStatement)operation).Returned);
    //        if (operation is ILocalReferenceExpression)
    //            return CreateLocalReferenceExpression((ILocalReferenceExpression)operation);
    //        if (operation is IFieldReferenceExpression)
    //            return CreateFieldReferenceExpression((IFieldReferenceExpression)operation);
    //        if (operation is IPropertyReferenceExpression)
    //            return CreatePropertyReferenceExpression((IPropertyReferenceExpression)operation);
    //        if (operation is IInstanceReferenceExpression)
    //            return VisitInstanceReferenceExpression((IInstanceReferenceExpression)operation);
    //        if (operation is IParameterReferenceExpression)
    //            return CreateParameterReferenceExpression((IParameterReferenceExpression)operation);
    //        if (operation is IInvocationExpression)
    //            return CreateInvocationExpression((IInvocationExpression)operation);
    //        if (operation is IObjectCreationExpression)
    //            return CreateObjectCreationExpression((IObjectCreationExpression)operation);
    //        if (operation is IBinaryOperatorExpression)
    //            return CreateBinaryOperatorExpression((IBinaryOperatorExpression)operation);
    //        if (operation is ILiteralExpression)
    //            return VisitLiteralExpression((ILiteralExpression)operation);
    //        if (operation is IArrayCreationExpression)
    //            return CreateArrayCreationExpression((IArrayCreationExpression)operation);
    //        if (operation is IArrayElementReferenceExpression)
    //            return CreateArrayElementReferenceExpression((IArrayElementReferenceExpression)operation);
    //        if (operation is ILambdaExpression)
    //            return VisitLambdaExpression((ILambdaExpression)operation);

    //        return null;
    //    }

    //    public Term Create(ILocalSymbol localSymbol)
    //    {
    //        var newTerm = new Term(localSymbol.Name, false);
    //        newTerm.IsScalar = !localSymbol.Type.CustomIsReferenceType();
    //        newTerm.IsGlobal = false;
    //        return newTerm;
    //    }

    //    public Term CreateArgument(IOperation operation)
    //    {
    //        Term newTerm = null;

    //        var realTerm = Create(operation);
    //        if (realTerm == null)
    //            return newTerm;

    //        string spanString = ToStringUtils.SpanStringFromSyntaxNode((CSharpSyntaxNode)operation.Syntax);
    //        newTerm = new Term(spanString) { UseNode = (CSharpSyntaxNode)operation.Syntax };
    //        newTerm.IsScalar = realTerm.IsScalar;
    //        newTerm.IsGlobal = realTerm.IsGlobal;
    //        if (!newTerm.IsScalar)
    //            _aliasingSolver.Assign(newTerm, realTerm);

    //        Stmt statement = StmtUtil.StmtFromSyntaxNode((CSharpSyntaxNode)operation.Syntax, _instrumentationResult);
    //        var ejesDependientes = _aliasingSolver.LastDef_Get(realTerm, _dependencyGraph, _instrumentationResult);
    //        var vertex = _dependencyGraph.AddVertex(statement, ejesDependientes);

    //        _aliasingSolver.LastDef_Set(newTerm, vertex);

    //        return newTerm;
    //    }

    //    Term CreateLocalReferenceExpression(ILocalReferenceExpression localReferenceExpression)
    //    {
    //        var newTerm = new Term(localReferenceExpression.Local.Name, false);
    //        newTerm.IsScalar = !(localReferenceExpression.Local.Type.CustomIsReferenceType());
    //        newTerm.IsGlobal = false;
    //        return newTerm;
    //    }

    //    Term CreateFieldReferenceExpression(IFieldReferenceExpression fieldReferenceExpression)
    //    {
    //        Term newTerm = null;
    //        Term recTerm = null;
    //        if (fieldReferenceExpression.Instance != null)
    //            recTerm = Create(fieldReferenceExpression.Instance);
    //        if (fieldReferenceExpression.Instance == null || recTerm == null)
    //            newTerm = new Term(fieldReferenceExpression.Field.ToString());
    //        else
    //            newTerm = new Term(fieldReferenceExpression.Field.Name);
    //        newTerm.IsGlobal = fieldReferenceExpression.Field.IsStatic;
    //        if (recTerm != null)
    //            newTerm = recTerm.AddingField(newTerm.ToString());
    //        newTerm.IsScalar = !(fieldReferenceExpression.Field.Type.CustomIsReferenceType());

    //        return newTerm;
    //    }

    //    Term CreatePropertyReferenceExpression(IPropertyReferenceExpression propertyReferenceExpression)
    //    {
    //        if (HasAccesor(propertyReferenceExpression.Property.Name))
    //        {
    //            Term recTerm = Create(propertyReferenceExpression.Instance);

    //            string spanString = ToStringUtils.SpanStringFromSyntaxNode((CSharpSyntaxNode)propertyReferenceExpression.Syntax);
    //            Term newTerm = new Term(spanString) { UseNode = (CSharpSyntaxNode)propertyReferenceExpression.Syntax };
    //            newTerm.IsGlobal = false;
    //            newTerm.IsScalar = !((IPropertyReferenceExpression)propertyReferenceExpression).ResultType.CustomIsReferenceType();
    //            Stmt statement = StmtUtil.StmtFromSyntaxNode((CSharpSyntaxNode)propertyReferenceExpression.Syntax, _instrumentationResult);

    //            var vertex = _dependencyGraph.AddVertex(statement, new HashSet<string>());
    //            _aliasingSolver.LastDef_Set(newTerm, vertex);

    //            var retorno = ConsumeInvocation(newTerm, null, recTerm);
    //            // JUNTAR EL RETORNO CON EL VÉRTICE NUEVO
    //            if (retorno != null)
    //                _dependencyGraph.AddEdge(vertex, retorno);

    //            return newTerm;
    //        }
    //        else
    //        {
    //            Term newTerm = new Term(propertyReferenceExpression.Property.Name);
    //            newTerm.IsGlobal = false;

    //            if (propertyReferenceExpression.Instance != null && propertyReferenceExpression.Instance.ResultType.Name.Equals("Nullable"))
    //            {
    //                return Create(propertyReferenceExpression.Instance);
    //            }
    //            if (propertyReferenceExpression.Instance != null)
    //            {
    //                var recTerm = Create(propertyReferenceExpression.Instance);
    //                if (recTerm == null && (propertyReferenceExpression.Instance.Kind == OperationKind.None && propertyReferenceExpression.Instance.Syntax is IdentifierNameSyntax))
    //                    recTerm = new Term(propertyReferenceExpression.Instance.ResultType.ToString()) { IsGlobal = true, IsScalar = false };
    //                if (recTerm != null)
    //                    newTerm = recTerm.AddingField(newTerm.ToString());
    //            }
    //            newTerm.IsScalar = !(propertyReferenceExpression.Property.Type.CustomIsReferenceType());

    //            return newTerm;
    //        }
    //    }

    //    Term CreateParameterReferenceExpression(IParameterReferenceExpression parameterReferenceExpression)
    //    {
    //        var newTerm = new Term(parameterReferenceExpression.Parameter.Name, false);
    //        newTerm.IsScalar = !(parameterReferenceExpression.Parameter.Type.CustomIsReferenceType());
    //        newTerm.IsGlobal = false;
    //        return newTerm;
    //    }

    //    Term CreateInvocationExpression(IInvocationExpression invocationExpression)
    //    {
    //        Term recTerm = Create(invocationExpression.Instance);
    //        var argumentos = invocationExpression.ArgumentsInParameterOrder
    //                            .Select(x => CreateArgument(x.Value)).Where(x => x != null).ToList();

    //        string spanString = ToStringUtils.SpanStringFromSyntaxNode((CSharpSyntaxNode)invocationExpression.Syntax);
    //        Term newTerm = new Term(spanString) { UseNode = (CSharpSyntaxNode)invocationExpression.Syntax };
    //        newTerm.IsScalar = !((IInvocationExpression)invocationExpression).ResultType.CustomIsReferenceType();
    //        newTerm.IsGlobal = false; // invocationExpression.IsStatic(); los términos que creamos no pueden ser globales

    //        if (!newTerm.IsScalar)
    //            _aliasingSolver.Alloc(newTerm);

    //        Stmt statement = StmtUtil.StmtFromSyntaxNode((CSharpSyntaxNode)invocationExpression.Syntax, _instrumentationResult);

    //        var ejesDependientes = new List<string>();
    //        if (recTerm != null && (!invocationExpression.IsInSource()))
    //            ejesDependientes.AddRange(_aliasingSolver.LastDef_Get(recTerm, _dependencyGraph, _instrumentationResult));
    //        if (!invocationExpression.IsInSource())
    //            ejesDependientes.AddRange(argumentos.Where(x => x != null).SelectMany(x => _aliasingSolver.LastDef_Get(x, _dependencyGraph, _instrumentationResult)));
    //        var vertex = _dependencyGraph.AddVertex(statement, new HashSet<string>(ejesDependientes));
    //        _aliasingSolver.LastDef_Set(newTerm, vertex);

    //        // TODO: Revisar...
    //        if (invocationExpression.IsInSource())
    //        {
    //            var retorno = ConsumeInvocation(newTerm, argumentos, recTerm);
    //            // JUNTAR EL RETORNO CON EL VÉRTICE NUEVO
    //            if (retorno != null)
    //                _dependencyGraph.AddEdge(vertex, retorno);
    //        }
    //        else
    //        {
    //            if (invocationExpression.TargetMethod.MethodKind != MethodKind.DelegateInvoke)
    //            {
    //                var havocArgs = argumentos.Where(x => x != null && !x.IsScalar).ToList();
    //                if (recTerm != null)
    //                    havocArgs.Add(recTerm);
    //                Term resultParam = null;
    //                if (newTerm != null && !newTerm.IsScalar)
    //                    resultParam = newTerm;
    //                else
    //                    resultParam = _aliasingSolver.FreshVar();
    //                _aliasingSolver.Havoc(havocArgs, resultParam, vertex);
    //                foreach (var argTerm in havocArgs)
    //                {
    //                    var listSigma = Copy(argTerm, argTerm.ToString() + "._any_");
    //                    _aliasingSolver.LastDef_Get(listSigma, _dependencyGraph, _instrumentationResult).ToList().ForEach(x => _dependencyGraph.AddEdge(vertex, x));
    //                }

    //                _lastNoInstrumentedInvocation = new InvocationData(recTerm, argumentos, invocationExpression, resultParam);
    //            }
    //        }

    //        return newTerm;
    //    }

    //    Term CreateBinaryOperatorExpression(IBinaryOperatorExpression binaryOperatorExpression)
    //    {
    //        string spanString = ToStringUtils.SpanStringFromSyntaxNode((CSharpSyntaxNode)binaryOperatorExpression.Syntax);
    //        Term newTerm = new Term(spanString) { UseNode = (CSharpSyntaxNode)binaryOperatorExpression.Syntax };
    //        newTerm.IsScalar = true;
    //        newTerm.IsGlobal = false;

    //        Stmt statement = StmtUtil.StmtFromSyntaxNode((CSharpSyntaxNode)binaryOperatorExpression.Syntax, _instrumentationResult);

    //        Term recTermLeft = Create(binaryOperatorExpression.Left);
    //        Term recTermRight = Create(binaryOperatorExpression.Right);
    //        var ejesDependientes = new List<string>();
    //        if (recTermLeft != null)
    //            ejesDependientes.AddRange(_aliasingSolver.LastDef_Get(recTermLeft, _dependencyGraph, _instrumentationResult));
    //        if (recTermRight != null)
    //            ejesDependientes.AddRange(_aliasingSolver.LastDef_Get(recTermRight, _dependencyGraph, _instrumentationResult));

    //        var vertex = _dependencyGraph.AddVertex(statement, new HashSet<string>(ejesDependientes));
    //        _aliasingSolver.LastDef_Set(newTerm, vertex);

    //        return newTerm;
    //    }

    //    Term CreateObjectCreationExpression(IObjectCreationExpression objectCreationExpression)
    //    {
    //        if (objectCreationExpression.IsInSource() &&
    //            _traceConsumer.ObserveNextStatement().TraceType == TraceType.EnterStaticConstructor)
    //            ConsumeInvocation();

    //        string spanString = ToStringUtils.SpanStringFromSyntaxNode((CSharpSyntaxNode)objectCreationExpression.Syntax);
    //        Term newTerm = new Term(spanString) { UseNode = (CSharpSyntaxNode)objectCreationExpression.Syntax };
    //        newTerm.IsScalar = false;
    //        newTerm.IsGlobal = false;
    //        Stmt statement = StmtUtil.StmtFromSyntaxNode((CSharpSyntaxNode)objectCreationExpression.Syntax, _instrumentationResult);

    //        var argumentos = objectCreationExpression.ConstructorArguments.Select(x => CreateArgument(x.Value)).ToList();

    //        var ejesDependientes = new List<string>();
    //        if (!objectCreationExpression.IsInSource())
    //            ejesDependientes.AddRange(argumentos.Where(x => x != null)
    //                .SelectMany(x => _aliasingSolver.LastDef_Get(x, _dependencyGraph, _instrumentationResult)));

    //        var vertex = _dependencyGraph.AddVertex(statement, new HashSet<string>(ejesDependientes));
    //        _aliasingSolver.LastDef_Set(newTerm, vertex);

    //        if (objectCreationExpression.IsInSource())
    //        {
    //            var retorno = ConsumeInvocation(newTerm, argumentos, null);
    //            // JUNTAR EL RETORNO CON EL VÉRTICE NUEVO
    //            if (retorno != null)
    //                _dependencyGraph.AddEdge(vertex, retorno);
    //        }
    //        else
    //        {
    //            var havocParams = argumentos.Where(x => x != null && !x.IsScalar).ToList();
    //            Term havocTerm = null;
    //            if (!newTerm.IsScalar)
    //            {
    //                _aliasingSolver.Alloc(newTerm);
    //                havocTerm = newTerm;
    //            }
    //            else
    //                havocTerm = _aliasingSolver.FreshVar();
    //            _aliasingSolver.Havoc(havocParams, havocTerm, vertex);
    //            foreach (var argTerm in havocParams)
    //            {
    //                var listSigma = Copy(argTerm, argTerm.ToString() + "._any_");
    //                _aliasingSolver.LastDef_Get(listSigma, _dependencyGraph, _instrumentationResult).ToList().ForEach(x => _dependencyGraph.AddEdge(vertex, x));
    //            }

    //            _lastNoInstrumentedInvocation = new InvocationData(null, argumentos, objectCreationExpression, havocTerm);
    //        }

    //        return newTerm;
    //    }

    //    Term CreateArrayCreationExpression(IArrayCreationExpression arrayCreationExpression)
    //    {
    //        var dependentTerms = new List<Term>();

    //        if (arrayCreationExpression.Syntax is ArrayCreationExpressionSyntax && ((ArrayCreationExpressionSyntax)arrayCreationExpression.Syntax).Initializer != null)
    //            ((ArrayCreationExpressionSyntax)arrayCreationExpression.Syntax).Initializer.Expressions.ToList().ForEach(x => dependentTerms.Add(Create(_semanticModelsContainer.GetBySyntaxNode(x).GetOperation(x))));
    //        if (arrayCreationExpression.Syntax is ImplicitArrayCreationExpressionSyntax && ((ImplicitArrayCreationExpressionSyntax)arrayCreationExpression.Syntax).Initializer != null)
    //            ((ImplicitArrayCreationExpressionSyntax)arrayCreationExpression.Syntax).Initializer.Expressions.ToList().ForEach(x => dependentTerms.Add(Create(_semanticModelsContainer.GetBySyntaxNode(x).GetOperation(x))));

    //        string spanString = ToStringUtils.SpanStringFromSyntaxNode((CSharpSyntaxNode)arrayCreationExpression.Syntax);
    //        Term newTerm = new Term(spanString) { UseNode = (CSharpSyntaxNode)arrayCreationExpression.Syntax };
    //        newTerm.IsScalar = false;
    //        newTerm.IsGlobal = false;
    //        Stmt statement = StmtUtil.StmtFromSyntaxNode((CSharpSyntaxNode)arrayCreationExpression.Syntax, _instrumentationResult);

    //        _aliasingSolver.Alloc(newTerm);

    //        var ejesDependientes = dependentTerms.Where(x => x != null).Select(x => _aliasingSolver.LastDef_Get(x, _dependencyGraph, _instrumentationResult)).SelectMany(x => x).ToList();

    //        var vertex = _dependencyGraph.AddVertex(statement, new HashSet<string>(ejesDependientes));
    //        _aliasingSolver.LastDef_Set(newTerm, vertex);

    //        return newTerm;
    //    }

    //    Term CreateArrayElementReferenceExpression(IArrayElementReferenceExpression arrayElementReferenceExpression)
    //    {
    //        var recTerm = Create(arrayElementReferenceExpression.ArrayReference);
    //        var terms = new List<Term>() { recTerm };
    //        arrayElementReferenceExpression.Indices.ToList().ForEach(x => terms.Add(Create(x)));

    //        string spanString = ToStringUtils.SpanStringFromSyntaxNode((CSharpSyntaxNode)arrayElementReferenceExpression.Syntax);
    //        var newTerm = new Term(spanString) { UseNode = (CSharpSyntaxNode)arrayElementReferenceExpression.Syntax };
    //        newTerm.IsScalar = !arrayElementReferenceExpression.ResultType.CustomIsReferenceType();
    //        newTerm.IsGlobal = recTerm.IsGlobal;
    //        Stmt statement = StmtUtil.StmtFromSyntaxNode((CSharpSyntaxNode)arrayElementReferenceExpression.Syntax, _instrumentationResult);
    //        var ejesDependientes = terms.Select(x => _aliasingSolver.LastDef_Get(x, _dependencyGraph, _instrumentationResult)).SelectMany(x => x).ToList();
    //        var vertex = _dependencyGraph.AddVertex(statement, new HashSet<string>(ejesDependientes));

    //        _aliasingSolver.LastDef_Set(newTerm, vertex);

    //        if (!newTerm.IsScalar)
    //            _aliasingSolver.Alloc(newTerm);

    //        var havocArgs = terms.Where(x => x != null && !x.IsScalar).ToList();
    //        _aliasingSolver.Havoc(havocArgs, recTerm, vertex);
    //        foreach (var argTerm in havocArgs)
    //        {
    //            var listSigma = Copy(argTerm, argTerm.ToString() + "._any_");
    //            _aliasingSolver.LastDef_Get(listSigma, _dependencyGraph, _instrumentationResult).ToList().ForEach(x => _dependencyGraph.AddEdge(vertex, x));
    //        }

    //        return newTerm;
    //    }
    //    #endregion

    //    #region Helpers
    //    string AddVertex()
    //    {
    //        var ejesDependientes = _usedVariables.Select(x => _aliasingSolver.LastDef_Get(x, _dependencyGraph, _instrumentationResult)).SelectMany(x => x).ToList();
    //        var controlVertex = _controlManagement.GetControlStmt();
    //        if (controlVertex != null)
    //            ejesDependientes.Add(_statementsToNodes[controlVertex]);
    //        if (_invocationVertex != null)
    //            ejesDependientes.Add(_invocationVertex);

    //        var vertice = _dependencyGraph.AddVertex(this._originalStatement, new HashSet<string>(ejesDependientes));
    //        if (_sliceCriteriaReached)
    //            _dependencyGraph.SliceCriteriaReached = true;

    //        return vertice;
    //    }

    //    void UpdateStatementsDictionary(string vertex)
    //    {
    //        string lastValue;
    //        if (!_statementsToNodes.TryGetValue(_originalStatement, out lastValue))
    //            _statementsToNodes.Add(_originalStatement, vertex);
    //        else
    //            _statementsToNodes[_originalStatement] = vertex;
    //    }

    //    string ConsumeInvocation(Term term = null, List<Term> argumentList = null, Term @this = null)
    //    {
    //        var nuevoProcesador = new ProcessConsumer(_traceConsumer, _aliasingSolver, _dependencyGraph, _semanticModelsContainer, _instrumentationResult, _executedStatements);
    //        return nuevoProcesador.Process(term, argumentList, @this);
    //    }

    //    bool HasAccesor(string Name)
    //    {
    //        var nextStatement = _traceConsumer.ObserveNextStatement();
    //        var syntaxNode = nextStatement.CSharpSyntaxNode;
    //        return (nextStatement.TraceType == TraceType.EnterMethod
    //            && syntaxNode is AccessorDeclarationSyntax
    //            && (((PropertyDeclarationSyntax)((CSharpSyntaxNode)syntaxNode).Parent.Parent).Identifier.ValueText.Equals(Name)));
    //    }

    //    Term Copy(Term term, string id)
    //    {
    //        var newTerm = new Term(id);
    //        newTerm.IsGlobal = term.IsGlobal;
    //        newTerm.IsScalar = term.IsScalar;
    //        return newTerm;
    //    }
    //    #endregion
    //    #endregion
    //}

}
