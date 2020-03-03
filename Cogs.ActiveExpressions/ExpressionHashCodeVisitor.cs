using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;

namespace Cogs.ActiveExpressions
{
    /// <summary>
    /// Represents a visitor for expression trees that generates hash codes for them
    /// </summary>
    public class ExpressionHashCodeVisitor : ExpressionVisitor
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ExpressionHashCodeVisitor"/> class
        /// </summary>
        /// <param name="visit">The <see cref="Expression"/> for which to immediately generate a hash code</param>
        public ExpressionHashCodeVisitor(Expression? visit = null) : base()
        {
            if (visit != null)
                Visit(visit);
        }

        List<object?>? hashElements;
        readonly Stack<ReadOnlyCollection<ParameterExpression>> nodeParameters = new Stack<ReadOnlyCollection<ParameterExpression>>();

        void AddHashElements(params object?[] elements) => hashElements?.AddRange(elements);

        /// <summary>
        /// Dispatches the expression to one of the more specialized visit methods in this class
        /// </summary>
        /// <param name="node">The expression to visit</param>
        /// <returns><paramref name="node"/></returns>
        public override Expression Visit(Expression node)
        {
            var atRoot = hashElements == null;
            if (atRoot)
                hashElements = new List<object?>();
            AddHashElements(node?.NodeType, node?.Type);
            var result = base.Visit(node);
            if (atRoot)
            {
                var hashCode = HashCode.Combine(typeof(ExpressionHashCodeVisitor), hashElements.FirstOrDefault());
                foreach (var element in hashElements.Skip(1))
                    hashCode = HashCode.Combine(hashCode, element);
                LastVisitedHashCode = hashCode;
                hashElements = null;
            }
            return result;
        }

        /// <summary>
        /// Visits the children of the <see cref="BinaryExpression"/>
        /// </summary>
        /// <param name="node">The expression to visit</param>
        /// <returns><paramref name="node"/></returns>
        protected override Expression VisitBinary(BinaryExpression node)
        {
            AddHashElements(node.IsLifted, node.IsLiftedToNull, node.Method);
            return base.VisitBinary(node);
        }

        /// <summary>
        /// Visits the <see cref="ConstantExpression"/>
        /// </summary>
        /// <param name="node">The expression to visit</param>
        /// <returns><paramref name="node"/></returns>
        protected override Expression VisitConstant(ConstantExpression node)
        {
            AddHashElements(node.Value);
            return base.VisitConstant(node);
        }

        /// <summary>
        /// Visits the children of the <see cref="IndexExpression"/>
        /// </summary>
        /// <param name="node">The expression to visit</param>
        /// <returns><paramref name="node"/></returns>
        protected override Expression VisitIndex(IndexExpression node)
        {
            AddHashElements(node.Indexer);
            return base.VisitIndex(node);
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
                AddHashElements(node.ReturnType, node.TailCall);
                nodeParameters.Push(node.Parameters);
                return base.VisitLambda(node);
            }
            finally
            {
                nodeParameters.Pop();
            }
        }

        /// <summary>
        /// Visits the children of the <see cref="MemberExpression"/>
        /// </summary>
        /// <param name="node">The expression to visit</param>
        /// <returns><paramref name="node"/></returns>
        protected override Expression VisitMember(MemberExpression node)
        {
            AddHashElements(node.Member);
            return base.VisitMember(node);
        }

        /// <summary>
        /// Visits the children of the <see cref="MethodCallExpression"/>
        /// </summary>
        /// <param name="node">The expression to visit</param>
        /// <returns><paramref name="node"/></returns>
        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            AddHashElements(node.Method);
            return base.VisitMethodCall(node);
        }

        /// <summary>
        /// Visits the children of the <see cref="NewExpression"/>
        /// </summary>
        /// <param name="node">The expression to visit</param>
        /// <returns><paramref name="node"/></returns>
        protected override Expression VisitNew(NewExpression node)
        {
            AddHashElements(node.Constructor);
            return base.VisitNew(node);
        }

        /// <summary>
        /// Visits the children of the <see cref="ParameterExpression"/>
        /// </summary>
        /// <param name="node">The expression to visit</param>
        /// <returns><paramref name="node"/></returns>
        protected override Expression VisitParameter(ParameterExpression node)
        {
            AddHashElements(nodeParameters.Peek().IndexOf(node));
            return base.VisitParameter(node);
        }

        /// <summary>
        /// Visits the children of the <see cref="UnaryExpression"/>
        /// </summary>
        /// <param name="node">The expression to visit</param>
        /// <returns><paramref name="node"/></returns>
        protected override Expression VisitUnary(UnaryExpression node)
        {
            AddHashElements(node.IsLifted, node.IsLiftedToNull, node.Method);
            return base.VisitUnary(node);
        }

        /// <summary>
        /// Gets the hash code generated for the last expression visited
        /// </summary>
        public int LastVisitedHashCode { get; private set; }
    }
}
