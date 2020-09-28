using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.ExceptionServices;

namespace Cogs.ActiveExpressions
{
    class ActiveInvocationExpression : ActiveExpression, IEquatable<ActiveInvocationExpression>
    {
        ActiveInvocationExpression(InvocationExpression invocationExpression, ActiveExpressionOptions? options, bool deferEvaluation) : base(invocationExpression.Type, ExpressionType.Invoke, options, deferEvaluation)
        {
            var activeArgumentsList = new List<ActiveExpression>();
            try
            {
                this.invocationExpression = invocationExpression;
                if (this.invocationExpression.Expression is LambdaExpression)
                {
                    foreach (var invocationExpressionArgument in this.invocationExpression.Arguments)
                    {
                        var activeArgument = Create(invocationExpressionArgument, options, deferEvaluation);
                        activeArgument.PropertyChanged += ActiveArgumentPropertyChanged;
                        activeArgumentsList.Add(activeArgument);
                    }
                    activeArguments = activeArgumentsList.ToImmutableArray();
                }
                CreateActiveExpression();
            }
            catch (Exception ex)
            {
                if (activeExpression is { })
                {
                    activeExpression.PropertyChanged -= ActiveExpressionPropertyChanged;
                    activeExpression.Dispose();
                }
                if (activeDelegateExpression is { })
                {
                    activeDelegateExpression.PropertyChanged -= ActiveDelegateExpressionPropertyChanged;
                    activeDelegateExpression.Dispose();
                }
                foreach (var activeArgument in activeArgumentsList)
                {
                    activeArgument.PropertyChanged -= ActiveArgumentPropertyChanged;
                    activeArgument.Dispose();
                }
                ExceptionDispatchInfo.Capture(ex).Throw();
                throw;
            }
        }

        readonly InvocationExpression invocationExpression;
        ActiveExpression? activeExpression;
        ActiveExpression? activeDelegateExpression;
        readonly IReadOnlyList<ActiveExpression>? activeArguments;
        int disposalCount;

        void ActiveArgumentPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (activeExpression is { })
            {
                activeExpression.PropertyChanged -= ActiveExpressionPropertyChanged;
                activeExpression.Dispose();
                activeExpression = null;
            }
            if (activeArguments.All(activeArgument => activeArgument.Fault is null))
                CreateActiveExpression();
            else if (!IsDeferringEvaluation)
                Evaluate();
        }

        void ActiveDelegateExpressionPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (activeExpression is { })
            {
                activeExpression.PropertyChanged -= ActiveExpressionPropertyChanged;
                activeExpression.Dispose();
                activeExpression = null;
            }
            CreateActiveExpression();
        }

        void ActiveExpressionPropertyChanged(object sender, PropertyChangedEventArgs e) => Evaluate();

        void CreateActiveExpression()
        {
            switch (invocationExpression.Expression)
            {
                case LambdaExpression lambdaExpression when activeArguments is { }:
                    activeExpression = Create(ReplaceParameters(lambdaExpression, activeArguments.Select(activeArgument => activeArgument.Value).ToArray()), options, IsDeferringEvaluation);
                    break;
                case Expression expression when typeof(Delegate).IsAssignableFrom(expression.Type):
                    var activeDelegateExpressionCreated = false;
                    if (activeDelegateExpression is null)
                    {
                        activeDelegateExpression = Create(expression, options, IsDeferringEvaluation);
                        activeDelegateExpressionCreated = true;
                    }
                    if (activeDelegateExpression.Value is Delegate @delegate)
                        activeExpression = Create(@delegate.Target is null ? Expression.Call(@delegate.Method, invocationExpression.Arguments) : Expression.Call(Expression.Constant(@delegate.Target), @delegate.Method, invocationExpression.Arguments), options, IsDeferringEvaluation);
                    if (activeDelegateExpressionCreated)
                        activeDelegateExpression.PropertyChanged += ActiveDelegateExpressionPropertyChanged;
                    break;
                default:
                    throw new NotSupportedException();
            }
            if (activeExpression is { })
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
                    activeExpression.PropertyChanged -= ActiveExpressionPropertyChanged;
                    activeExpression.Dispose();
                }
                if (activeDelegateExpression is { })
                {
                    activeDelegateExpression.PropertyChanged -= ActiveDelegateExpressionPropertyChanged;
                    activeDelegateExpression.Dispose();
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
        static readonly Dictionary<(InvocationExpression invocationExpression, ActiveExpressionOptions? options), ActiveInvocationExpression> instances = new Dictionary<(InvocationExpression invocationExpression, ActiveExpressionOptions? options), ActiveInvocationExpression>(new CachedInstancesKeyComparer<InvocationExpression>());

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
    }
}
