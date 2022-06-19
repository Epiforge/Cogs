namespace Cogs.Expressions;

/// <summary>
/// Provides extension methods for performing operations on expression trees
/// </summary>
public static class Extensions
{
    static MethodInfo SubstituteMethod(MethodInfo methodInfo, (MethodInfo replace, MethodInfo substitution)[] substitutions)
    {
        if (substitutions.FirstOrDefault(s => s.replace == methodInfo).substitution is { } substitution)
            return substitution;
        return methodInfo;
    }

    /// <summary>
    /// Subtitutes method calls in an expression tree with that of other methods
    /// </summary>
    /// <param name="expression">The expression tree</param>
    /// <param name="substitutions">The substitutions to perform</param>
    /// <exception cref="ArgumentException">One or more of the elements in <paramref name="substitutions"/> do not have matching signatures</exception>
    /// <exception cref="ArgumentNullException"><paramref name="expression"/> or <paramref name="substitutions"/> is <c>null</c></exception>
    public static Expression SubstituteMethods(this Expression expression, params (MethodInfo replace, MethodInfo substitution)[] substitutions)
    {
        if (expression is null)
            throw new ArgumentNullException(nameof(expression));
        if (substitutions is null)
            throw new ArgumentNullException(nameof(substitutions));
        if (substitutions.Length == 0)
            return expression;
        if (substitutions.Any(s => !new Type[] { s.replace.ReturnType }.Concat(s.replace.GetParameters().Select(p => p.ParameterType)).SequenceEqual(new Type[] { s.substitution.ReturnType }.Concat(s.substitution.GetParameters().Select(p => p.ParameterType)))))
            throw new ArgumentException("One or more substitutions do not have matching signatures", nameof(substitutions));
        return SubstituteMethodsImplementation(expression, substitutions);
    }

    static Expression SubstituteMethodsImplementation(this Expression expression, (MethodInfo replace, MethodInfo substitution)[] substitutions) =>
        expression switch
        {
            BinaryExpression binaryExpression when binaryExpression.Conversion is { } binaryExpressionConversion => Expression.MakeBinary
            (
                binaryExpression.NodeType,
                SubstituteMethodsImplementation(binaryExpression.Left, substitutions),
                SubstituteMethodsImplementation(binaryExpression.Right, substitutions),
                binaryExpression.IsLiftedToNull,
                SubstituteMethod(binaryExpression.Method, substitutions),
                (LambdaExpression)SubstituteMethodsImplementation(binaryExpressionConversion, substitutions)
            ),
            BinaryExpression binaryExpression when binaryExpression.Method is { } binaryExpressionMethod => Expression.MakeBinary
            (
                binaryExpression.NodeType,
                SubstituteMethodsImplementation(binaryExpression.Left, substitutions),
                SubstituteMethodsImplementation(binaryExpression.Right, substitutions),
                binaryExpression.IsLiftedToNull,
                SubstituteMethod(binaryExpressionMethod, substitutions)
            ),
            BinaryExpression binaryExpression => Expression.MakeBinary
            (
                binaryExpression.NodeType,
                SubstituteMethodsImplementation(binaryExpression.Left, substitutions),
                SubstituteMethodsImplementation(binaryExpression.Right, substitutions)
            ),
            BlockExpression blockExpression => Expression.Block
            (
                blockExpression.Type,
                blockExpression.Variables.Select(e => (ParameterExpression)SubstituteMethodsImplementation(e, substitutions)),
                blockExpression.Expressions.Select(e => SubstituteMethodsImplementation(e, substitutions))
            ),
            ConditionalExpression conditionalExpression => Expression.Condition
            (
                SubstituteMethodsImplementation(conditionalExpression.Test, substitutions),
                SubstituteMethodsImplementation(conditionalExpression.IfTrue, substitutions),
                SubstituteMethodsImplementation(conditionalExpression.IfFalse, substitutions),
                conditionalExpression.Type
            ),
            ConstantExpression constantExpression => Expression.Constant
            (
                constantExpression.Value is Expression constantExpressionValue
                ?
                SubstituteMethodsImplementation(constantExpressionValue, substitutions)
                :
                constantExpression.Value
            ),
            DynamicExpression dynamicExpression => Expression.Dynamic
            (
                dynamicExpression.Binder,
                dynamicExpression.Type,
                dynamicExpression.Arguments.Select(e => SubstituteMethodsImplementation(e, substitutions))
            ),
            GotoExpression gotoExpression when gotoExpression.Value is { } gotoExpressionValue => Expression.Goto
            (
                gotoExpression.Target,
                SubstituteMethodsImplementation(gotoExpressionValue, substitutions),
                gotoExpression.Type
            ),
            GotoExpression gotoExpression => Expression.Goto
            (
                gotoExpression.Target,
                gotoExpression.Type
            ),
            IndexExpression indexExpression => Expression.MakeIndex
            (
                SubstituteMethodsImplementation(indexExpression.Object, substitutions),
                indexExpression.Indexer,
                indexExpression.Arguments.Select(e => SubstituteMethodsImplementation(e, substitutions))
            ),
            InvocationExpression invocationExpression => Expression.Invoke
            (
                SubstituteMethodsImplementation(invocationExpression.Expression, substitutions),
                invocationExpression.Arguments.Select(e => SubstituteMethodsImplementation(e, substitutions))
            ),
            LabelExpression labelExpression when labelExpression.DefaultValue is { } labelExpressionDefaultValue => Expression.Label
            (
                labelExpression.Target,
                SubstituteMethodsImplementation(labelExpressionDefaultValue, substitutions)
            ),
            LambdaExpression lambdaExpression => Expression.Lambda
            (
                SubstituteMethodsImplementation(lambdaExpression.Body, substitutions),
                lambdaExpression.Name,
                lambdaExpression.TailCall,
                lambdaExpression.Parameters.Select(e => (ParameterExpression)SubstituteMethodsImplementation(e, substitutions))
            ),
            ListInitExpression listInitExpression => Expression.ListInit
            (
                (NewExpression)SubstituteMethodsImplementation(listInitExpression.NewExpression, substitutions),
                listInitExpression.Initializers.Select(i => Expression.ElementInit(SubstituteMethod(i.AddMethod, substitutions), i.Arguments.Select(e => SubstituteMethodsImplementation(e, substitutions))))
            ),
            LoopExpression loopExpression => Expression.Loop
            (
                SubstituteMethodsImplementation(loopExpression.Body, substitutions),
                loopExpression.BreakLabel,
                loopExpression.ContinueLabel
            ),
            MemberExpression memberExpression => Expression.MakeMemberAccess
            (
                SubstituteMethodsImplementation(memberExpression.Expression, substitutions),
                memberExpression.Member
            ),
            MemberInitExpression memberInitExpression => Expression.MemberInit
            (
                (NewExpression)SubstituteMethodsImplementation(memberInitExpression.NewExpression, substitutions),
                memberInitExpression.Bindings
            ),
            MethodCallExpression methodCallExpression when methodCallExpression.Object is { } methodCallExpressionObject => Expression.Call
            (
                SubstituteMethodsImplementation(methodCallExpressionObject, substitutions),
                SubstituteMethod(methodCallExpression.Method, substitutions),
                methodCallExpression.Arguments.Select(e => SubstituteMethodsImplementation(e, substitutions))
            ),
            MethodCallExpression methodCallExpression => Expression.Call
            (
                SubstituteMethod(methodCallExpression.Method, substitutions),
                methodCallExpression.Arguments.Select(e => SubstituteMethodsImplementation(e, substitutions))
            ),
            NewArrayExpression newArrayExpression when newArrayExpression.NodeType == ExpressionType.NewArrayBounds => Expression.NewArrayBounds
            (
                newArrayExpression.Type.IsArray ? newArrayExpression.Type.GetElementType() : newArrayExpression.Type,
                newArrayExpression.Expressions.Select(e => SubstituteMethodsImplementation(e, substitutions))
            ),
            NewArrayExpression newArrayExpression => Expression.NewArrayInit
            (
                newArrayExpression.Type.IsArray ? newArrayExpression.Type.GetElementType() : newArrayExpression.Type,
                newArrayExpression.Expressions.Select(e => SubstituteMethodsImplementation(e, substitutions))
            ),
            NewExpression newExpression => Expression.New
            (
                newExpression.Constructor,
                newExpression.Arguments.Select(e => SubstituteMethodsImplementation(e, substitutions)),
                newExpression.Members
            ),
            SwitchExpression switchExpression when switchExpression.Comparison is { } switchExpressionComparison => Expression.Switch
            (
                switchExpression.Type,
                SubstituteMethodsImplementation(switchExpression.SwitchValue, substitutions),
                SubstituteMethodsImplementation(switchExpression.DefaultBody, substitutions),
                SubstituteMethod(switchExpressionComparison, substitutions),
                switchExpression.Cases.Select(c => Expression.SwitchCase(SubstituteMethodsImplementation(c.Body, substitutions), c.TestValues.Select(e => SubstituteMethodsImplementation(e, substitutions))))
            ),
            SwitchExpression switchExpression when switchExpression.DefaultBody is { } switchExpressionDefaultBody => Expression.Switch
            (
                SubstituteMethodsImplementation(switchExpression.SwitchValue, substitutions),
                SubstituteMethodsImplementation(switchExpressionDefaultBody, substitutions),
                switchExpression.Cases.Select(c => Expression.SwitchCase(SubstituteMethodsImplementation(c.Body, substitutions), c.TestValues.Select(e => SubstituteMethodsImplementation(e, substitutions)))).ToArray()
            ),
            SwitchExpression switchExpression => Expression.Switch
            (
                SubstituteMethodsImplementation(switchExpression.SwitchValue, substitutions),
                switchExpression.Cases.Select(c => Expression.SwitchCase(SubstituteMethodsImplementation(c.Body, substitutions), c.TestValues.Select(e => SubstituteMethodsImplementation(e, substitutions)))).ToArray()
            ),
            TryExpression tryExpression => Expression.MakeTry
            (
                tryExpression.Type,
                SubstituteMethodsImplementation(tryExpression.Body, substitutions),
                SubstituteMethodsImplementation(tryExpression.Finally, substitutions),
                SubstituteMethodsImplementation(tryExpression.Fault, substitutions),
                tryExpression.Handlers.Select(h => Expression.MakeCatchBlock(h.Test, (ParameterExpression)SubstituteMethodsImplementation(h.Variable, substitutions), SubstituteMethodsImplementation(h.Body, substitutions), SubstituteMethodsImplementation(h.Filter, substitutions)))
            ),
            TypeBinaryExpression typeBinaryExpression when typeBinaryExpression.NodeType == ExpressionType.TypeIs => Expression.TypeIs
            (
                SubstituteMethodsImplementation(typeBinaryExpression.Expression, substitutions),
                typeBinaryExpression.TypeOperand
            ),
            TypeBinaryExpression typeBinaryExpression => Expression.TypeEqual
            (
                SubstituteMethodsImplementation(typeBinaryExpression.Expression, substitutions),
                typeBinaryExpression.TypeOperand
            ),
            UnaryExpression unaryExpression when unaryExpression.Method is { } unaryExpressionMethod => Expression.MakeUnary
            (
                unaryExpression.NodeType,
                SubstituteMethodsImplementation(unaryExpression.Operand, substitutions),
                unaryExpression.Type,
                SubstituteMethod(unaryExpressionMethod, substitutions)
            ),
            UnaryExpression unaryExpression => Expression.MakeUnary
            (
                unaryExpression.NodeType,
                SubstituteMethodsImplementation(unaryExpression.Operand, substitutions),
                unaryExpression.Type
            ),
            _ => expression,
        };
}
