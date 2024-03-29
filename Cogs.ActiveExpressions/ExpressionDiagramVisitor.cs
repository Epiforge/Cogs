namespace Cogs.ActiveExpressions;

/// <summary>
/// Represents a visitor for expression trees that reduces them to a list of unique combination of elements for comparison and hash code generation
/// </summary>
public sealed class ExpressionDiagramVisitor :
    ExpressionVisitor
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ExpressionDiagramVisitor"/> class
    /// </summary>
    /// <param name="visit">The <see cref="Expression"/> for which to immediately reduce the elements</param>
    public ExpressionDiagramVisitor(Expression? visit = null)
    {
        if (visit is not null)
            Visit(visit);
    }

    readonly List<object?> elements = new();
    readonly Dictionary<ParameterExpression, (int set, int index)> parameters = new();
    int parameterSet = -1;

    /// <summary>
    /// Gets the list of unique combination of elements for comparison and hash code generation
    /// </summary>
    public IReadOnlyList<object?> Elements =>
        elements;

    /// <summary>
    /// Dispatches the expression to one of the more specialized visit methods in this class
    /// </summary>
    /// <param name="node">The expression to visit</param>
    /// <returns><paramref name="node"/></returns>
    public override Expression? Visit(Expression? node)
    {
        if (node is not null)
        {
            elements.Add(node.CanReduce);
            elements.Add(node.NodeType);
            elements.Add(node.Type);
        }
        return base.Visit(node);
    }

    /// <summary>
    /// Visits the children of the <see cref="BinaryExpression"/>
    /// </summary>
    /// <param name="node">The expression to visit</param>
    /// <returns><paramref name="node"/></returns>
    protected override Expression VisitBinary(BinaryExpression node)
    {
        if (node is null)
            throw new ArgumentNullException(nameof(node));
        elements.Add(node.IsLifted);
        elements.Add(node.IsLiftedToNull);
        elements.Add(node.Method);
        return base.VisitBinary(node);
    }

    /// <summary>
    /// Visits the children of the <see cref="BlockExpression"/>
    /// </summary>
    /// <param name="node">The expression to visit</param>
    /// <returns><paramref name="node"/></returns>
    [ExcludeFromCodeCoverage]
    protected override Expression VisitBlock(BlockExpression node)
    {
        if (node is null)
            throw new ArgumentNullException(nameof(node));
        elements.Add(node.Expressions.Count);
        elements.Add(node.Variables.Count);
        return base.VisitBlock(node);
    }

    /// <summary>
    /// Visits the children of the <see cref="CatchBlock"/>
    /// </summary>
    /// <param name="node">The expression to visit</param>
    /// <returns><paramref name="node"/></returns>
    [ExcludeFromCodeCoverage]
    protected override CatchBlock VisitCatchBlock(CatchBlock node)
    {
        if (node is null)
            throw new ArgumentNullException(nameof(node));
        elements.Add(node.GetType());
        elements.Add(node.Test);
        return base.VisitCatchBlock(node);
    }

    /// <summary>
    /// Visits the children of the <see cref="ConstantExpression"/>
    /// </summary>
    /// <param name="node">The expression to visit</param>
    /// <returns><paramref name="node"/></returns>
    protected override Expression VisitConstant(ConstantExpression node)
    {
        if (node is null)
            throw new ArgumentNullException(nameof(node));
        elements.Add(node.Value);
        return base.VisitConstant(node);
    }

    /// <summary>
    /// Visits the children of the <see cref="DebugInfoExpression"/>
    /// </summary>
    /// <param name="node">The expression to visit</param>
    /// <returns><paramref name="node"/></returns>
    [ExcludeFromCodeCoverage]
    protected override Expression VisitDebugInfo(DebugInfoExpression node)
    {
        if (node is null)
            throw new ArgumentNullException(nameof(node));
        elements.Add(node.Document);
        elements.Add(node.EndColumn);
        elements.Add(node.EndLine);
        elements.Add(node.IsClear);
        elements.Add(node.StartColumn);
        elements.Add(node.StartLine);
        return base.VisitDebugInfo(node);
    }

    /// <summary>
    /// Visits the children of the <see cref="DynamicExpression"/>
    /// </summary>
    /// <param name="node">The expression to visit</param>
    /// <returns><paramref name="node"/></returns>
    [ExcludeFromCodeCoverage]
    protected override Expression VisitDynamic(DynamicExpression node)
    {
        if (node is null)
            throw new ArgumentNullException(nameof(node));
        elements.Add(node.Binder);
        elements.Add(node.DelegateType);
        return base.VisitDynamic(node);
    }

    /// <summary>
    /// Visits the children of the <see cref="ElementInit"/>
    /// </summary>
    /// <param name="node">The expression to visit</param>
    /// <returns><paramref name="node"/></returns>
    [ExcludeFromCodeCoverage]
    protected override ElementInit VisitElementInit(ElementInit node)
    {
        if (node is null)
            throw new ArgumentNullException(nameof(node));
        elements.Add(node.GetType());
        elements.Add(node.AddMethod);
        return base.VisitElementInit(node);
    }

    /// <summary>
    /// Visits the children of the <see cref="GotoExpression"/>
    /// </summary>
    /// <param name="node">The expression to visit</param>
    /// <returns><paramref name="node"/></returns>
    [ExcludeFromCodeCoverage]
    protected override Expression VisitGoto(GotoExpression node)
    {
        if (node is null)
            throw new ArgumentNullException(nameof(node));
        elements.Add(node.Kind);
        return base.VisitGoto(node);
    }

    /// <summary>
    /// Visits the children of the <see cref="IndexExpression"/>
    /// </summary>
    /// <param name="node">The expression to visit</param>
    /// <returns><paramref name="node"/></returns>
    protected override Expression VisitIndex(IndexExpression node)
    {
        if (node is null)
            throw new ArgumentNullException(nameof(node));
        elements.Add(node.Indexer);
        return base.VisitIndex(node);
    }

    /// <summary>
    /// Visits the children of the <see cref="LabelTarget"/>
    /// </summary>
    /// <param name="node">The expression to visit</param>
    /// <returns><paramref name="node"/></returns>
    [ExcludeFromCodeCoverage]
    protected override LabelTarget VisitLabelTarget(LabelTarget node)
    {
        if (node is null)
            throw new ArgumentNullException(nameof(node));
        elements.Add(node.GetType());
        elements.Add(node.Name);
        return base.VisitLabelTarget(node);
    }

    /// <summary>
    /// Visits the children of the <see cref="Expression{T}"/>
    /// </summary>
    /// <param name="node">The expression to visit</param>
    /// <returns><paramref name="node"/></returns>
    [ExcludeFromCodeCoverage]
    protected override Expression VisitLambda<T>(Expression<T> node)
    {
        if (node is null)
            throw new ArgumentNullException(nameof(node));
        elements.Add(node.ReturnType);
        elements.Add(node.TailCall);
        ++parameterSet;
        var nodeParameters = node.Parameters;
        for (int i = 0, ii = nodeParameters.Count; i < ii; ++i)
            parameters.Add(nodeParameters[i], (parameterSet, i));
        return base.VisitLambda(node);
    }

    /// <summary>
    /// Visits the children of the <see cref="MemberExpression"/>
    /// </summary>
    /// <param name="node">The expression to visit</param>
    /// <returns><paramref name="node"/></returns>
    protected override Expression VisitMember(MemberExpression node)
    {
        if (node is null)
            throw new ArgumentNullException(nameof(node));
        elements.Add(node.Member);
        return base.VisitMember(node);
    }

    /// <summary>
    /// Visits the children of the <see cref="MemberAssignment"/>
    /// </summary>
    /// <param name="node">The expression to visit</param>
    /// <returns><paramref name="node"/></returns>
    [ExcludeFromCodeCoverage]
    protected override MemberAssignment VisitMemberAssignment(MemberAssignment node)
    {
        if (node is null)
            throw new ArgumentNullException(nameof(node));
        elements.Add(node.GetType());
        elements.Add(node.BindingType);
        elements.Add(node.Member);
        return base.VisitMemberAssignment(node);
    }

    /// <summary>
    /// Visits the children of the <see cref="MemberBinding"/>
    /// </summary>
    /// <param name="node">The expression to visit</param>
    /// <returns><paramref name="node"/></returns>
    [ExcludeFromCodeCoverage]
    protected override MemberBinding VisitMemberBinding(MemberBinding node)
    {
        if (node is null)
            throw new ArgumentNullException(nameof(node));
        elements.Add(node.GetType());
        elements.Add(node.BindingType);
        elements.Add(node.Member);
        return base.VisitMemberBinding(node);
    }

    /// <summary>
    /// Visits the children of the <see cref="MemberListBinding"/>
    /// </summary>
    /// <param name="node">The expression to visit</param>
    /// <returns><paramref name="node"/></returns>
    [ExcludeFromCodeCoverage]
    protected override MemberListBinding VisitMemberListBinding(MemberListBinding node)
    {
        if (node is null)
            throw new ArgumentNullException(nameof(node));
        elements.Add(node.GetType());
        elements.Add(node.BindingType);
        elements.Add(node.Member);
        return base.VisitMemberListBinding(node);
    }

    /// <summary>
    /// Visits the children of the <see cref="MemberMemberBinding"/>
    /// </summary>
    /// <param name="node">The expression to visit</param>
    /// <returns><paramref name="node"/></returns>
    [ExcludeFromCodeCoverage]
    protected override MemberMemberBinding VisitMemberMemberBinding(MemberMemberBinding node)
    {
        if (node is null)
            throw new ArgumentNullException(nameof(node));
        elements.Add(node.GetType());
        elements.Add(node.BindingType);
        elements.Add(node.Member);
        return base.VisitMemberMemberBinding(node);
    }

    /// <summary>
    /// Visits the children of the <see cref="MethodCallExpression"/>
    /// </summary>
    /// <param name="node">The expression to visit</param>
    /// <returns><paramref name="node"/></returns>
    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        if (node is null)
            throw new ArgumentNullException(nameof(node));
        elements.Add(node.Method);
        return base.VisitMethodCall(node);
    }

    /// <summary>
    /// Visits the children of the <see cref="NewExpression"/>
    /// </summary>
    /// <param name="node">The expression to visit</param>
    /// <returns><paramref name="node"/></returns>
    protected override Expression VisitNew(NewExpression node)
    {
        if (node is null)
            throw new ArgumentNullException(nameof(node));
        elements.Add(node.Constructor);
        elements.AddRange(node.Members ?? Enumerable.Empty<MemberInfo>());
        return base.VisitNew(node);
    }

    /// <summary>
    /// Visits the children of the <see cref="ParameterExpression"/>
    /// </summary>
    /// <param name="node">The expression to visit</param>
    /// <returns><paramref name="node"/></returns>
    protected override Expression VisitParameter(ParameterExpression node)
    {
        var (set, index) = parameters[node];
        elements.Add(set);
        elements.Add(index);
        return base.VisitParameter(node);
    }

    /// <summary>
    /// Visits the children of the <see cref="RuntimeVariablesExpression"/>
    /// </summary>
    /// <param name="node">The expression to visit</param>
    /// <returns><paramref name="node"/></returns>
    [ExcludeFromCodeCoverage]
    protected override Expression VisitRuntimeVariables(RuntimeVariablesExpression node)
    {
        if (node is null)
            throw new ArgumentNullException(nameof(node));
        elements.Add(node.Variables.Count);
        return base.VisitRuntimeVariables(node);
    }

    /// <summary>
    /// Visits the children of the <see cref="SwitchExpression"/>
    /// </summary>
    /// <param name="node">The expression to visit</param>
    /// <returns><paramref name="node"/></returns>
    [ExcludeFromCodeCoverage]
    protected override Expression VisitSwitch(SwitchExpression node)
    {
        if (node is null)
            throw new ArgumentNullException(nameof(node));
        elements.Add(node.Cases.Count);
        elements.Add(node.Comparison);
        return base.VisitSwitch(node);
    }

    /// <summary>
    /// Visits the children of the <see cref="SwitchCase"/>
    /// </summary>
    /// <param name="node">The expression to visit</param>
    /// <returns><paramref name="node"/></returns>
    [ExcludeFromCodeCoverage]
    protected override SwitchCase VisitSwitchCase(SwitchCase node)
    {
        if (node is null)
            throw new ArgumentNullException(nameof(node));
        elements.Add(node.GetType());
        elements.Add(node.TestValues.Count);
        return base.VisitSwitchCase(node);
    }

    /// <summary>
    /// Visits the children of the <see cref="TryExpression"/>
    /// </summary>
    /// <param name="node">The expression to visit</param>
    /// <returns><paramref name="node"/></returns>
    [ExcludeFromCodeCoverage]
    protected override Expression VisitTry(TryExpression node)
    {
        if (node is null)
            throw new ArgumentNullException(nameof(node));
        elements.Add(node.Handlers.Count);
        return base.VisitTry(node);
    }

    /// <summary>
    /// Visits the children of the <see cref="TypeBinaryExpression"/>
    /// </summary>
    /// <param name="node">The expression to visit</param>
    /// <returns><paramref name="node"/></returns>
    protected override Expression VisitTypeBinary(TypeBinaryExpression node)
    {
        if (node is null)
            throw new ArgumentNullException(nameof(node));
        elements.Add(node.TypeOperand);
        return base.VisitTypeBinary(node);
    }

    /// <summary>
    /// Visits the children of the <see cref="UnaryExpression"/>
    /// </summary>
    /// <param name="node">The expression to visit</param>
    /// <returns><paramref name="node"/></returns>
    protected override Expression VisitUnary(UnaryExpression node)
    {
        if (node is null)
            throw new ArgumentNullException(nameof(node));
        elements.Add(node.IsLifted);
        elements.Add(node.IsLiftedToNull);
        elements.Add(node.Method);
        return base.VisitUnary(node);
    }
}
