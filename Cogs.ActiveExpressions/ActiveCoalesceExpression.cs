using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

namespace Cogs.ActiveExpressions
{
    class ActiveCoalesceExpression : ActiveBinaryExpression, IEquatable<ActiveCoalesceExpression>
    {
        public ActiveCoalesceExpression(Type type, ActiveExpression left, ActiveExpression right, LambdaExpression conversion, ActiveExpressionOptions? options, bool deferEvaluation) : base(type, ExpressionType.Coalesce, left, right, false, null, options, deferEvaluation, false, false)
        {
            if (conversion is { })
            {
                var key = (convertFrom: conversion.Parameters[0].Type, convertTo: conversion.Body.Type);
                lock (conversionDelegateManagementLock)
                {
                    if (!conversionDelegates.TryGetValue(key, out var conversionDelegate))
                    {
                        var parameter = Expression.Parameter(typeof(object));
                        conversionDelegate = Expression.Lambda<UnaryOperationDelegate>(Expression.Convert(Expression.Invoke(conversion, Expression.Convert(parameter, key.convertFrom)), typeof(object)), parameter).Compile();
                        conversionDelegates.Add(key, conversionDelegate);
                    }
                    this.conversionDelegate = conversionDelegate;
                }
            }
            EvaluateIfNotDeferred();
        }

        readonly UnaryOperationDelegate? conversionDelegate;

        public override bool Equals(object obj) => obj is ActiveCoalesceExpression other && Equals(other);

        public bool Equals(ActiveCoalesceExpression other) => left.Equals(other.left) && right.Equals(other.right) && Equals(options, other.options);

        [SuppressMessage("Design", "CA1031:Do not catch general exception types")]
        protected override void Evaluate()
        {
            try
            {
                var leftFault = left.Fault;
                if (leftFault is { })
                    Fault = leftFault;
                else
                {
                    var leftValue = left.Value;
                    if (leftValue is { })
                        Value = conversionDelegate is null ? leftValue : conversionDelegate(leftValue);
                    else
                    {
                        var rightFault = right.Fault;
                        if (rightFault is { })
                            Fault = rightFault;
                        else
                            Value = right.Value;
                    }
                }
            }
            catch (Exception ex)
            {
                Fault = ex;
            }
        }

        public override int GetHashCode() => HashCode.Combine(typeof(ActiveCoalesceExpression), left, right, options);

        public override string ToString() => $"({left} ?? {right}) {ToStringSuffix}";

        static readonly object conversionDelegateManagementLock = new object();
        static readonly Dictionary<(Type convertFrom, Type convertTo), UnaryOperationDelegate> conversionDelegates = new Dictionary<(Type convertFrom, Type convertTo), UnaryOperationDelegate>();

        public static bool operator ==(ActiveCoalesceExpression? a, ActiveCoalesceExpression? b) => EqualityComparer<ActiveCoalesceExpression?>.Default.Equals(a, b);

        public static bool operator !=(ActiveCoalesceExpression? a, ActiveCoalesceExpression? b) => !EqualityComparer<ActiveCoalesceExpression?>.Default.Equals(a, b);
    }
}
