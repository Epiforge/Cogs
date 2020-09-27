using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;

namespace Cogs.ActiveExpressions
{
    class ActiveInvocationExpression : ActiveExpression, IEquatable<ActiveInvocationExpression>
    {
        ActiveInvocationExpression(InvocationExpression invocationExpression, ActiveExpressionOptions? options, bool deferEvaluation) : base(invocationExpression.Type, ExpressionType.Invoke, options, deferEvaluation)
        {
            this.invocationExpression = invocationExpression;
            if (this.invocationExpression.Expression is LambdaExpression)
                activeArguments = invocationExpression.Arguments.Select(argument => Create(argument, options, deferEvaluation)).ToImmutableArray();
            CreateActiveExpression();
            if (activeArguments is { })
                foreach (var activeArgument in activeArguments)
                    activeArgument.PropertyChanged += ActiveArgumentPropertyChanged;
        }

        readonly InvocationExpression invocationExpression;
        ActiveExpression? activeExpression;
        readonly IReadOnlyList<ActiveExpression>? activeArguments;
        int disposalCount;

        void ActiveExpressionPropertyChanged(object sender, PropertyChangedEventArgs e) => Evaluate();

        void ActiveArgumentPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (activeExpression is { })
            {
                activeExpression.PropertyChanged -= ActiveExpressionPropertyChanged;
                activeExpression.Dispose();
                activeExpression = null;
            }
            if (activeExpression is null)
            {
                if (activeArguments.All(activeArgument => activeArgument.Fault is null))
                    CreateActiveExpression();
                else if (!IsDeferringEvaluation)
                    Evaluate();
            }
        }

        void CreateActiveExpression()
        {
            activeExpression = invocationExpression.Expression switch
            {
                LambdaExpression lambdaExpression when activeArguments is { } => Create(ReplaceParameters(lambdaExpression, activeArguments.Select(activeArgument => activeArgument.Value).ToArray()), options, IsDeferringEvaluation),
                ConstantExpression constantExpression when constantExpression.Value is Delegate @delegate => Create(@delegate.Target is null ? Expression.Call(@delegate.Method, invocationExpression.Arguments) : Expression.Call(Expression.Constant(@delegate.Target), @delegate.Method, invocationExpression.Arguments), options, IsDeferringEvaluation),
                _ => throw new NotSupportedException()
            };
            activeExpression.PropertyChanged += ActiveExpressionPropertyChanged;
            EvaluateIfNotDeferred();
        }

        protected override bool Dispose(bool disposing)
        {
            var result = false;
            lock (instanceManagementLock)
                if (--disposalCount == 0)
                {
                    instances.Remove((invocationExpression, options));
                    result = true;
                }
            if (result)
            {
                if (activeExpression is { })
                {
                    activeExpression.PropertyChanged += ActiveExpressionPropertyChanged;
                    activeExpression.Dispose();
                }
                if (activeArguments is { })
                    foreach (var activeArgument in activeArguments)
                    {
                        activeArgument.PropertyChanged -= ActiveArgumentPropertyChanged;
                        activeArgument.Dispose();
                    }
            }
            return result;
        }

        public override bool Equals(object? obj) => obj is ActiveInvocationExpression other && Equals(other);

        public bool Equals(ActiveInvocationExpression other) => ExpressionEqualityComparer.Default.Equals(invocationExpression, other.invocationExpression) && Equals(options, other.options);

        protected override void Evaluate()
        {
            if (activeExpression is { } && activeExpression.Fault is { } activeExpressionFault)
                Fault = activeExpressionFault;
            else if (activeArguments is { } && activeArguments.Select(activeArgument => activeArgument.Fault).Where(fault => fault is { }).FirstOrDefault() is { } activeArgumentFault)
                Fault = activeArgumentFault;
            else if (activeExpression is { })
                Value = activeExpression.Value;
        }

        public override int GetHashCode() => HashCode.Combine(typeof(ActiveInvocationExpression), ExpressionEqualityComparer.Default.GetHashCode(invocationExpression), options);

        public override string ToString() => $"λ({(activeExpression is { } ? (object)activeExpression : invocationExpression)})";

        static readonly object instanceManagementLock = new object();
        static readonly Dictionary<(InvocationExpression invocationExpression, ActiveExpressionOptions? options), ActiveInvocationExpression> instances = new Dictionary<(InvocationExpression invocationExpression, ActiveExpressionOptions? options), ActiveInvocationExpression>(new InstancesEqualityComparer());

        public static ActiveInvocationExpression Create(InvocationExpression invocationExpression, ActiveExpressionOptions? options, bool deferEvaluation)
        {
            var key = (invocationExpression, options);
            lock (instanceManagementLock)
            {
                if (!instances.TryGetValue(key, out var activeInvocationExpression))
                {
                    activeInvocationExpression = new ActiveInvocationExpression(invocationExpression, options, deferEvaluation);
                    instances.Add(key, activeInvocationExpression);
                }
                ++activeInvocationExpression.disposalCount;
                return activeInvocationExpression;
            }
        }

        public static bool operator ==(ActiveInvocationExpression a, ActiveInvocationExpression b) => a.Equals(b);

        [ExcludeFromCodeCoverage]
        public static bool operator !=(ActiveInvocationExpression a, ActiveInvocationExpression b) => !(a == b);

        class InstancesEqualityComparer : IEqualityComparer<(InvocationExpression invocationExpression, ActiveExpressionOptions? options)>
        {
            public bool Equals((InvocationExpression invocationExpression, ActiveExpressionOptions? options) x, (InvocationExpression invocationExpression, ActiveExpressionOptions? options) y) =>
                ExpressionEqualityComparer.Default.Equals(x.invocationExpression, y.invocationExpression) && ((x.options is null && y.options is null) || (x.options is { } && y.options is { } && x.options.Equals(y.options)));

            public int GetHashCode((InvocationExpression invocationExpression, ActiveExpressionOptions? options) obj) =>
                HashCode.Combine(ExpressionEqualityComparer.Default.GetHashCode(obj.invocationExpression), obj.options);
        }
    }
}
