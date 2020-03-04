using Cogs.Collections;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;

namespace Cogs.ActiveExpressions
{
    class ActiveNewExpression : ActiveExpression, IEquatable<ActiveNewExpression>
    {
        ActiveNewExpression(Type type, EquatableList<ActiveExpression> arguments, ActiveExpressionOptions? options, bool deferEvaluation) : base(type, ExpressionType.New, options, deferEvaluation)
        {
            this.arguments = arguments;
            constructorParameterTypes = new EquatableList<Type>(this.arguments.Select(argument => argument.Type).ToList());
            foreach (var argument in this.arguments)
                argument.PropertyChanged += ArgumentPropertyChanged;
            EvaluateIfNotDeferred();
        }

        readonly EquatableList<ActiveExpression> arguments;
        readonly EquatableList<Type> constructorParameterTypes;
        int disposalCount;

        void ArgumentPropertyChanged(object sender, PropertyChangedEventArgs e) => Evaluate();

        protected override bool Dispose(bool disposing)
        {
            var result = false;
            lock (instanceManagementLock)
                if (--disposalCount == 0)
                {
                    instances.Remove((Type, arguments, options));
                    result = true;
                }
            if (result)
            {
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
            if (ApplicableOptions.IsConstructedTypeDisposed(Type, constructorParameterTypes) && TryGetUndeferredValue(out var value))
            {
                if (value is IDisposable disposable)
                    disposable.Dispose();
                else if (value is IAsyncDisposable asyncDisposable)
                    asyncDisposable.DisposeAsync().AsTask().Wait();
            }
        }

        public override bool Equals(object obj) => obj is ActiveNewExpression other && Equals(other);

        public bool Equals(ActiveNewExpression other) => Type.Equals(other.Type) && arguments.Equals(other.arguments) && Equals(options, other.options);

        [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Don't tell me what to catch in a general purpose method, bruh")]
        protected override void Evaluate()
        {
            try
            {
                DisposeValueIfNecessary();
                var argumentFault = arguments.Select(argument => argument.Fault).Where(fault => fault is { }).FirstOrDefault();
                if (argumentFault is { })
                    Fault = argumentFault;
                else
                    Value = Activator.CreateInstance(Type, arguments.Select(argument => argument.Value).ToArray());
            }
            catch (Exception ex)
            {
                Fault = ex;
            }
        }

        public override int GetHashCode() => HashCode.Combine(typeof(ActiveNewExpression), Type, arguments, options);

        public override string ToString() => $"new {Type.FullName}({string.Join(", ", arguments.Select(argument => $"{argument}"))}) {ToStringSuffix}";

        static readonly object instanceManagementLock = new object();
        static readonly Dictionary<(Type type, EquatableList<ActiveExpression> arguments, ActiveExpressionOptions? options), ActiveNewExpression> instances = new Dictionary<(Type type, EquatableList<ActiveExpression> arguments, ActiveExpressionOptions? options), ActiveNewExpression>();

        public static bool operator ==(ActiveNewExpression? a, ActiveNewExpression? b) => EqualityComparer<ActiveNewExpression?>.Default.Equals(a, b);

        public static bool operator !=(ActiveNewExpression? a, ActiveNewExpression? b) => !EqualityComparer<ActiveNewExpression?>.Default.Equals(a, b);

        public static ActiveNewExpression Create(NewExpression newExpression, ActiveExpressionOptions? options, bool deferEvaluation)
        {
            var type = newExpression.Type;
            var arguments = new EquatableList<ActiveExpression>(newExpression.Arguments.Select(argument => Create(argument, options, deferEvaluation)).ToList());
            var key = (type, arguments, options);
            lock (instanceManagementLock)
            {
                if (!instances.TryGetValue(key, out var activeNewExpression))
                {
                    activeNewExpression = new ActiveNewExpression(type, arguments, options, deferEvaluation);
                    instances.Add(key, activeNewExpression);
                }
                ++activeNewExpression.disposalCount;
                return activeNewExpression;
            }
        }
    }
}
