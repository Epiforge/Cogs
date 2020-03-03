using Cogs.Reflection;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;

namespace Cogs.ActiveExpressions
{
    /// <summary>
    /// Represents a visitor for expression trees that compares them to other expression trees
    /// </summary>
    public class ExpressionEqualityComparisonVisitor : ExpressionVisitor
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ExpressionEqualityComparisonVisitor"/> class using the specified <see cref="Expression"/> to serve as the basis for comparisons
        /// </summary>
        /// <param name="basis">The <see cref="Expression"/> to serve as the basis for comparisons</param>
        /// <param name="visit">The <see cref="Expression"/> against which to immediately perform a comparison</param>
        public ExpressionEqualityComparisonVisitor(Expression basis, Expression? visit = null) : base()
        {
            this.basis = basis;
            if (visit != null)
                Visit(visit);
        }

        readonly Expression basis;
        readonly Stack<ReadOnlyCollection<ParameterExpression>> basisParameters = new Stack<ReadOnlyCollection<ParameterExpression>>();
        readonly Stack<Expression> basisStack = new Stack<Expression>();
        readonly Stack<ReadOnlyCollection<ParameterExpression>> nodeParameters = new Stack<ReadOnlyCollection<ParameterExpression>>();
        Expression? nodeRoot;

        Expression? NotEqual(Expression? expression)
        {
            IsLastVisitedEqual = false;
            return expression;
        }

        T PeekBasis<T>() where T : Expression => (T)basisStack.Peek();

        void PopBasis() => basisStack.Pop();

        void PushBasis(IEnumerable<Expression> expressions)
        {
            foreach (var expression in expressions.Reverse())
                basisStack.Push(expression);
        }

        void PushBasis(params Expression[] expressions) => PushBasis((IEnumerable<Expression>)expressions);

        /// <summary>
        /// Dispatches the expression to one of the more specialized visit methods in this class
        /// </summary>
        /// <param name="node">The expression to visit</param>
        /// <returns><paramref name="node"/></returns>
        public override Expression? Visit(Expression node)
        {
            Expression? basis = null;
            Expression? result = null;
            var resultSet = false;
            try
            {
                if (nodeRoot == null)
                {
                    nodeRoot = node;
                    basisStack.Push(this.basis);
                    IsLastVisitedEqual = true;
                }
                if (!IsLastVisitedEqual)
                {
                    result = node;
                    resultSet = true;
                    PopBasis();
                }
                if (!resultSet)
                {
                    basis = PeekBasis<Expression>();
                    if (node != null)
                    {
                        if (basis == null)
                        {
                            result = NotEqual(node);
                            resultSet = true;
                        }
                        else if (basis.NodeType != node.NodeType || basis.Type != node.Type)
                        {
                            result = NotEqual(node);
                            resultSet = true;
                        }
                    }
                    else if (basis != null)
                    {
                        result = NotEqual(node);
                        resultSet = true;
                    }
                    if (!resultSet)
                        result = base.Visit(node);
                }
            }
            finally
            {
                if (node == nodeRoot && basis != null)
                {
                    var bodyStackRemaining = basisStack.Any();
                    nodeRoot = null;
                    nodeParameters.Clear();
                    basisStack.Clear();
                    basisParameters.Clear();
                    if (bodyStackRemaining)
                        result = NotEqual(node);
                }
            }
            return result;
        }

        /// <summary>
        /// Visits the children of the <see cref="BinaryExpression"/>
        /// </summary>
        /// <param name="node">The expression to visit</param>
        /// <returns><paramref name="node"/></returns>
        protected override Expression? VisitBinary(BinaryExpression node)
        {
            try
            {
                var body = PeekBasis<BinaryExpression>();
                if (body.IsLifted != node.IsLifted || body.IsLiftedToNull != node.IsLiftedToNull || body.Method != node.Method)
                    return NotEqual(node);
                PushBasis(body.Right);
                if (body.Conversion != null)
                    PushBasis(body.Conversion);
                PushBasis(body.Left);
                return base.VisitBinary(node);
            }
            finally
            {
                PopBasis();
            }
        }

        /// <summary>
        /// Visits the children of the <see cref="ConditionalExpression"/>
        /// </summary>
        /// <param name="node">The expression to visit</param>
        /// <returns><paramref name="node"/></returns>
        protected override Expression VisitConditional(ConditionalExpression node)
        {
            try
            {
                var body = PeekBasis<ConditionalExpression>();
                PushBasis(body.Test, body.IfTrue, body.IfFalse);
                return base.VisitConditional(node);
            }
            finally
            {
                PopBasis();
            }
        }

        /// <summary>
        /// Visits the <see cref="ConstantExpression"/>
        /// </summary>
        /// <param name="node">The expression to visit</param>
        /// <returns><paramref name="node"/></returns>
        protected override Expression? VisitConstant(ConstantExpression node)
        {
            try
            {
                if (!FastEqualityComparer.Get(node.Type).Equals(PeekBasis<ConstantExpression>().Value, node.Value))
                    return NotEqual(node);
                return base.VisitConstant(node);
            }
            finally
            {
                PopBasis();
            }
        }

        /// <summary>
        /// Visits the children of the <see cref="IndexExpression"/>
        /// </summary>
        /// <param name="node">The expression to visit</param>
        /// <returns><paramref name="node"/></returns>
        protected override Expression? VisitIndex(IndexExpression node)
        {
            try
            {
                var body = PeekBasis<IndexExpression>();
                if (body.Indexer != node.Indexer)
                    return NotEqual(node);
                PushBasis(body.Arguments);
                PushBasis(body.Object);
                return base.VisitIndex(node);
            }
            finally
            {
                PopBasis();
            }
        }

        /// <summary>
        /// Visits the children of the <see cref="Expression{T}"/>
        /// </summary>
        /// <param name="node">The expression to visit</param>
        /// <returns><paramref name="node"/></returns>
        protected override Expression VisitLambda<T>(Expression<T> node)
        {
            try
            {
                var basis = PeekBasis<Expression<T>>();
                PushBasis(basis.Parameters);
                PushBasis(basis.Body);
                basisParameters.Push(basis.Parameters);
                nodeParameters.Push(node.Parameters);
                return base.VisitLambda(node);
            }
            finally
            {
                PopBasis();
                basisParameters.Pop();
                nodeParameters.Pop();
            }
        }

        /// <summary>
        /// Visits the children of the <see cref="MemberExpression"/>
        /// </summary>
        /// <param name="node">The expression to visit</param>
        /// <returns><paramref name="node"/></returns>
        protected override Expression? VisitMember(MemberExpression node)
        {
            try
            {
                var body = PeekBasis<MemberExpression>();
                if (body.Member != node.Member)
                    return NotEqual(node);
                PushBasis(body.Expression);
                return base.VisitMember(node);
            }
            finally
            {
                PopBasis();
            }
        }

        /// <summary>
        /// Visits the children of the <see cref="MethodCallExpression"/>
        /// </summary>
        /// <param name="node">The expression to visit</param>
        /// <returns><paramref name="node"/></returns>
        protected override Expression? VisitMethodCall(MethodCallExpression node)
        {
            try
            {
                var body = PeekBasis<MethodCallExpression>();
                if (body.Method != node.Method)
                    return NotEqual(node);
                PushBasis(body.Arguments);
                PushBasis(body.Object);
                return base.VisitMethodCall(node);
            }
            finally
            {
                PopBasis();
            }
        }

        /// <summary>
        /// Visits the children of the <see cref="NewExpression"/>
        /// </summary>
        /// <param name="node">The expression to visit</param>
        /// <returns><paramref name="node"/></returns>
        protected override Expression? VisitNew(NewExpression node)
        {
            try
            {
                var body = PeekBasis<NewExpression>();
                if (body.Constructor != node.Constructor)
                    return NotEqual(node);
                PushBasis(body.Arguments);
                return base.VisitNew(node);
            }
            finally
            {
                PopBasis();
            }
        }

        /// <summary>
        /// Visits the children of the <see cref="ParameterExpression"/>
        /// </summary>
        /// <param name="node">The expression to visit</param>
        /// <returns><paramref name="node"/></returns>
        protected override Expression? VisitParameter(ParameterExpression node)
        {
            try
            {
                var body = PeekBasis<ParameterExpression>();
                if (basisParameters.Peek().IndexOf(body) != nodeParameters.Peek().IndexOf(node))
                    return NotEqual(node);
                return base.VisitParameter(node);
            }
            finally
            {
                PopBasis();
            }
        }

        /// <summary>
        /// Visits the children of the <see cref="UnaryExpression"/>
        /// </summary>
        /// <param name="node">The expression to visit</param>
        /// <returns><paramref name="node"/></returns>
        protected override Expression? VisitUnary(UnaryExpression node)
        {
            try
            {
                var body = PeekBasis<UnaryExpression>();
                if (body.IsLifted != node.IsLifted || body.IsLiftedToNull != node.IsLiftedToNull || body.Method != node.Method)
                    return NotEqual(node);
                PushBasis(body.Operand);
                return base.VisitUnary(node);
            }
            finally
            {
                PopBasis();
            }
        }

        /// <summary>
        /// Gets whether the last expression visited is equivalent to the basis expression
        /// </summary>
        public bool IsLastVisitedEqual { get; private set; }
    }
}
