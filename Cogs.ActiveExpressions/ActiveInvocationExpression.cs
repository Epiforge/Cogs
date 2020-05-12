using Cogs.Collections;
using Cogs.Reflection;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;

namespace Cogs.ActiveExpressions
{
    class ActiveInvocationExpression : ActiveExpression, IEquatable<ActiveInvocationExpression>
    {
        ActiveInvocationExpression(Type type, ActiveExpression expression, EquatableList<ActiveExpression> arguments, ActiveExpressionOptions? options, bool deferEvaluation) : base(type, ExpressionType.Invoke, options, deferEvaluation)
        {
            this.expression = expression;
            this.expression.PropertyChanged += ExpressionPropertyChanged;
            this.arguments = arguments;
            foreach (var argument in this.arguments)
                argument.PropertyChanged += ArgumentPropertyChanged;
            EvaluateIfNotDeferred();
        }

        readonly EquatableList<ActiveExpression> arguments;
        int disposalCount;
        readonly ActiveExpression expression;
        FastMethodInfo? fastMethod;

        void ArgumentPropertyChanged(object sender, PropertyChangedEventArgs e) => Evaluate();

        protected override bool Dispose(bool disposing)
        {
            var result = false;
            lock (instanceManagementLock)
                if (--disposalCount == 0)
                {
                    instances.Remove((expression, arguments, options));
                    result = true;
                }
            if (result)
            {
                expression.PropertyChanged -= ExpressionPropertyChanged;
                expression.Dispose();
                foreach (var argument in arguments)
                {
                    argument.PropertyChanged -= ArgumentPropertyChanged;
                    argument.Dispose();
                }
                DisposeValueIfNecessary();
            }
            return result;
        }

        void DisposeValueIfNecessary()
        {
            if (fastMethod is { } && ApplicableOptions.IsMethodReturnValueDisposed(fastMethod.MethodInfo))
                DisposeValueIfPossible();
        }

        public override bool Equals(object obj) => obj is ActiveInvocationExpression other && Equals(other);

        public bool Equals(ActiveInvocationExpression other) => expression.Equals(other.expression) && arguments.Equals(arguments);

        [SuppressMessage("Design", "CA1031:Do not catch general exception types")]
        protected override void Evaluate()
        {
            try
            {
                DisposeValueIfNecessary();
                var expressionFault = expression.Fault;
                var argumentFault = arguments.Select(argument => argument.Fault).Where(fault => fault is { }).FirstOrDefault();
                if (expressionFault is { })
                    Fault = expressionFault;
                else if (argumentFault is { })
                    Fault = argumentFault;
                else if (expression.Value is Delegate @delegate)
                {
                    if (fastMethod?.MethodInfo != @delegate.Method)
                        fastMethod = FastMethodInfo.Get(@delegate.Method);
                    Value = fastMethod.Invoke(@delegate.Target, arguments.Select(argument => argument.Value).ToArray());
                }
                else
                {
                    fastMethod = null;
                    Fault = new Exception("Unable to invoke expression's value");
                }
            }
            catch (Exception ex)
            {
                Fault = ex;
            }
        }

        void ExpressionPropertyChanged(object sender, PropertyChangedEventArgs e) => Evaluate();

        public override int GetHashCode() => HashCode.Combine(typeof(ActiveInvocationExpression), expression, arguments);

        public override string ToString() => $"{expression}({string.Join(", ", arguments.Select(argument => $"{argument}"))}) {ToStringSuffix}";

        static readonly object instanceManagementLock = new object();
        static readonly Dictionary<(ActiveExpression expression, EquatableList<ActiveExpression> arguments, ActiveExpressionOptions? options), ActiveInvocationExpression> instances = new Dictionary<(ActiveExpression expression, EquatableList<ActiveExpression> arguments, ActiveExpressionOptions? options), ActiveInvocationExpression>();

        public static ActiveInvocationExpression Create(InvocationExpression invocationExpression, ActiveExpressionOptions? options, bool deferEvaluation)
        {
            var expression = Create(invocationExpression.Expression, options, deferEvaluation);
            var arguments = new EquatableList<ActiveExpression>(invocationExpression.Arguments.Select(argument => Create(argument, options, deferEvaluation)).ToList());
            var key = (expression, arguments, options);
            lock (instanceManagementLock)
            {
                if (!instances.TryGetValue(key, out var activeInvocationExpression))
                {
                    activeInvocationExpression = new ActiveInvocationExpression(invocationExpression.Type, expression, arguments, options, deferEvaluation);
                    instances.Add(key, activeInvocationExpression);
                }
                ++activeInvocationExpression.disposalCount;
                return activeInvocationExpression;
            }
        }

        public static bool operator ==(ActiveInvocationExpression a, ActiveInvocationExpression b) => a.Equals(b);

        public static bool operator !=(ActiveInvocationExpression a, ActiveInvocationExpression b) => !(a == b);
    }
}
