using Cogs.Collections;
using Cogs.Reflection;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

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

        ActiveNewExpression(Type type, ConstructorInfo constructor, EquatableList<ActiveExpression> arguments, ActiveExpressionOptions? options, bool deferEvaluation) : base(type, ExpressionType.New, options, deferEvaluation)
        {
            this.constructor = constructor;
            fastConstructor = FastConstructorInfo.Get(this.constructor);
            this.arguments = arguments;
            constructorParameterTypes = new EquatableList<Type>(this.arguments.Select(argument => argument.Type).ToList());
            foreach (var argument in this.arguments)
                argument.PropertyChanged += ArgumentPropertyChanged;
            EvaluateIfNotDeferred();
        }

        readonly EquatableList<ActiveExpression> arguments;
        readonly ConstructorInfo? constructor;
        readonly EquatableList<Type> constructorParameterTypes;
        int disposalCount;
        readonly FastConstructorInfo? fastConstructor;

        void ArgumentPropertyChanged(object sender, PropertyChangedEventArgs e) => Evaluate();

        protected override bool Dispose(bool disposing)
        {
            var result = false;
            lock (instanceManagementLock)
                if (--disposalCount == 0)
                {
                    if (constructor is null)
                        typeInstances.Remove((Type, arguments, options));
                    else
                        constructorInstances.Remove((Type, constructor, arguments, options));
                    result = true;
                }
            if (result)
            {
                DisposeValueIfNecessaryAndPossible();
                foreach (var argument in arguments)
                {
                    argument.PropertyChanged -= ArgumentPropertyChanged;
                    argument.Dispose();
                }
            }
            return result;
        }

        public override bool Equals(object obj) => obj is ActiveNewExpression other && Equals(other);

        public bool Equals(ActiveNewExpression other) => Type == other.Type && arguments == other.arguments && Equals(options, other.options);

        protected override void Evaluate()
        {
            try
            {
                var argumentFault = arguments.Select(argument => argument.Fault).Where(fault => fault is { }).FirstOrDefault();
                if (argumentFault is { })
                    Fault = argumentFault;
                else if (fastConstructor is { })
                    Value = fastConstructor.Invoke(arguments.Select(argument => argument.Value).ToArray());
                else
                    Value = Activator.CreateInstance(Type, arguments.Select(argument => argument.Value).ToArray());
            }
            catch (Exception ex)
            {
                Fault = ex;
            }
        }

        public override int GetHashCode() => HashCode.Combine(typeof(ActiveNewExpression), Type, arguments, options);

        protected override bool GetShouldValueBeDisposed() => ApplicableOptions.IsConstructedTypeDisposed(Type, constructorParameterTypes);

        public override string ToString() => $"new {Type.FullName}({string.Join(", ", arguments.Select(argument => $"{argument}"))}) {ToStringSuffix}";

        static readonly Dictionary<(Type type, ConstructorInfo constructor, EquatableList<ActiveExpression> arguments, ActiveExpressionOptions? options), ActiveNewExpression> constructorInstances = new Dictionary<(Type type, ConstructorInfo constructor, EquatableList<ActiveExpression> arguments, ActiveExpressionOptions? options), ActiveNewExpression>();
        static readonly object instanceManagementLock = new object();
        static readonly Dictionary<(Type type, EquatableList<ActiveExpression> arguments, ActiveExpressionOptions? options), ActiveNewExpression> typeInstances = new Dictionary<(Type type, EquatableList<ActiveExpression> arguments, ActiveExpressionOptions? options), ActiveNewExpression>();

        public static ActiveNewExpression Create(NewExpression newExpression, ActiveExpressionOptions? options, bool deferEvaluation)
        {
            var type = newExpression.Type;
            var arguments = new EquatableList<ActiveExpression>(newExpression.Arguments.Select(argument => Create(argument, options, deferEvaluation)).ToList());
            if (newExpression.Constructor is ConstructorInfo constructor)
            {
                var key = (type, constructor, arguments, options);
                lock (instanceManagementLock)
                {
                    if (!constructorInstances.TryGetValue(key, out var activeNewExpression))
                    {
                        activeNewExpression = new ActiveNewExpression(type, constructor, arguments, options, deferEvaluation);
                        constructorInstances.Add(key, activeNewExpression);
                    }
                    ++activeNewExpression.disposalCount;
                    return activeNewExpression;
                }
            }
            else
            {
                var key = (type, arguments, options);
                lock (instanceManagementLock)
                {
                    if (!typeInstances.TryGetValue(key, out var activeNewExpression))
                    {
                        activeNewExpression = new ActiveNewExpression(type, arguments, options, deferEvaluation);
                        typeInstances.Add(key, activeNewExpression);
                    }
                    ++activeNewExpression.disposalCount;
                    return activeNewExpression;
                }
            }
        }

        public static bool operator ==(ActiveNewExpression a, ActiveNewExpression b) => a.Equals(b);

        public static bool operator !=(ActiveNewExpression a, ActiveNewExpression b) => !(a == b);
    }
}
