using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Cogs.ActiveExpressions
{
    /// <summary>
    /// Represents a visitor for expression trees that reduces them to a list of unique combination of elements for comparison and hash code generation
    /// </summary>
    public class ExpressionDiagramVisitor : ExpressionVisitor
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ExpressionDiagramVisitor"/> class
        /// </summary>
        /// <param name="visit">The <see cref="Expression"/> for which to immediately reduce the elements</param>
        public ExpressionDiagramVisitor(Expression? visit = null)
        {
            if (visit is { })
                Visit(visit);
        }

        readonly List<object?> elements = new List<object?>();
        readonly Dictionary<ParameterExpression, (int set, int index)> parameters = new Dictionary<ParameterExpression, (int set, int index)>();
        int parameterSet = -1;

        /// <summary>
        /// Gets the list of unique combination of elements for comparison and hash code generation
        /// </summary>
        public IReadOnlyList<object?> Elements => elements;

        /// <summary>
        /// Dispatches the expression to one of the more specialized visit methods in this class
        /// </summary>
        /// <param name="node">The expression to visit</param>
        /// <returns><paramref name="node"/></returns>
        public override Expression? Visit(Expression? node)
        {
            if (node is { })
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
        protected override Expression VisitBlock(BlockExpression node)
        {
            elements.Add(node.Expressions.Count);
            elements.Add(node.Variables.Count);
            return base.VisitBlock(node);
        }

        /// <summary>
        /// Visits the children of the <see cref="CatchBlock"/>
        /// </summary>
        /// <param name="node">The expression to visit</param>
        /// <returns><paramref name="node"/></returns>
        protected override CatchBlock VisitCatchBlock(CatchBlock node)
        {
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
            elements.Add(node.Value);
            return base.VisitConstant(node);
        }

        /// <summary>
        /// Visits the children of the <see cref="DebugInfoExpression"/>
        /// </summary>
        /// <param name="node">The expression to visit</param>
        /// <returns><paramref name="node"/></returns>
        protected override Expression VisitDebugInfo(DebugInfoExpression node)
        {
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
        protected override Expression VisitDynamic(DynamicExpression node)
        {
            elements.Add(node.Binder);
            elements.Add(node.DelegateType);
            return base.VisitDynamic(node);
        }

        /// <summary>
        /// Visits the children of the <see cref="ElementInit"/>
        /// </summary>
        /// <param name="node">The expression to visit</param>
        /// <returns><paramref name="node"/></returns>
        protected override ElementInit VisitElementInit(ElementInit node)
        {
            elements.Add(node.GetType());
            elements.Add(node.AddMethod);
            return base.VisitElementInit(node);
        }

        /// <summary>
        /// Visits the children of the <see cref="GotoExpression"/>
        /// </summary>
        /// <param name="node">The expression to visit</param>
        /// <returns><paramref name="node"/></returns>
        protected override Expression VisitGoto(GotoExpression node)
        {
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
            elements.Add(node.Indexer);
            return base.VisitIndex(node);
        }

        /// <summary>
        /// Visits the children of the <see cref="LabelTarget"/>
        /// </summary>
        /// <param name="node">The expression to visit</param>
        /// <returns><paramref name="node"/></returns>
        protected override LabelTarget VisitLabelTarget(LabelTarget node)
        {
            elements.Add(node.GetType());
            elements.Add(node.Name);
            return base.VisitLabelTarget(node);
        }

        /// <summary>
        /// Visits the children of the <see cref="Expression{T}"/>
        /// </summary>
        /// <param name="node">The expression to visit</param>
        /// <returns><paramref name="node"/></returns>
        protected override Expression VisitLambda<T>(Expression<T> node)
        {
            elements.Add(node.ReturnType);
            elements.Add(node.TailCall);
            ++parameterSet;
            var index = -1;
            foreach (var parameter in node.Parameters)
                parameters.Add(parameter, (parameterSet, ++index));
            return base.VisitLambda(node);
        }

        /// <summary>
        /// Visits the children of the <see cref="MemberExpression"/>
        /// </summary>
        /// <param name="node">The expression to visit</param>
        /// <returns><paramref name="node"/></returns>
        protected override Expression VisitMember(MemberExpression node)
        {
            elements.Add(node.Member);
            return base.VisitMember(node);
        }

        /// <summary>
        /// Visits the children of the <see cref="MemberAssignment"/>
        /// </summary>
        /// <param name="node">The expression to visit</param>
        /// <returns><paramref name="node"/></returns>
        protected override MemberAssignment VisitMemberAssignment(MemberAssignment node)
        {
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
        protected override MemberBinding VisitMemberBinding(MemberBinding node)
        {
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
        protected override MemberListBinding VisitMemberListBinding(MemberListBinding node)
        {
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
        protected override MemberMemberBinding VisitMemberMemberBinding(MemberMemberBinding node)
        {
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
        protected override Expression VisitRuntimeVariables(RuntimeVariablesExpression node)
        {
            elements.Add(node.Variables.Count);
            return base.VisitRuntimeVariables(node);
        }

        /// <summary>
        /// Visits the children of the <see cref="SwitchExpression"/>
        /// </summary>
        /// <param name="node">The expression to visit</param>
        /// <returns><paramref name="node"/></returns>
        protected override Expression VisitSwitch(SwitchExpression node)
        {
            elements.Add(node.Cases.Count);
            elements.Add(node.Comparison);
            return base.VisitSwitch(node);
        }

        /// <summary>
        /// Visits the children of the <see cref="SwitchCase"/>
        /// </summary>
        /// <param name="node">The expression to visit</param>
        /// <returns><paramref name="node"/></returns>
        protected override SwitchCase VisitSwitchCase(SwitchCase node)
        {
            elements.Add(node.GetType());
            elements.Add(node.TestValues.Count);
            return base.VisitSwitchCase(node);
        }

        /// <summary>
        /// Visits the children of the <see cref="TryExpression"/>
        /// </summary>
        /// <param name="node">The expression to visit</param>
        /// <returns><paramref name="node"/></returns>
        protected override Expression VisitTry(TryExpression node)
        {
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
            elements.Add(node.IsLifted);
            elements.Add(node.IsLiftedToNull);
            elements.Add(node.Method);
            return base.VisitUnary(node);
        }
    }
}
