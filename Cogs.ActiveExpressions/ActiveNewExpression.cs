using Cogs.Collections;
using Cogs.Reflection;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.ExceptionServices;

namespace Cogs.ActiveExpressions
{
    class ActiveNewExpression : ActiveExpression, IEquatable<ActiveNewExpression>
    {
        ActiveNewExpression(NewExpression newExpression, ActiveExpressionOptions? options, bool deferEvaluation) : base(newExpression.Type, newExpression.NodeType, options, deferEvaluation)
        {
            var argumentsList = new List<ActiveExpression>();
            try
            {
                this.newExpression = newExpression;
                constructor = this.newExpression.Constructor;
                fastConstructor = FastConstructorInfo.Get(constructor);
                foreach (var newExpressionArgument in this.newExpression.Arguments)
                {
                    var argument = Create(newExpressionArgument, options, deferEvaluation);
                    argument.PropertyChanged += ArgumentPropertyChanged;
                    argumentsList.Add(argument);
                }
                arguments = new EquatableList<ActiveExpression>(argumentsList);
                constructorParameterTypes = new EquatableList<Type>(arguments.Select(argument => argument.Type).ToList());
                EvaluateIfNotDeferred();
            }
            catch (Exception ex)
            {
                DisposeValueIfNecessaryAndPossible();
                foreach (var argument in argumentsList)
                {
                    argument.PropertyChanged -= ArgumentPropertyChanged;
                    argument.Dispose();
                }
                ExceptionDispatchInfo.Capture(ex).Throw();
                throw;
            }
        }

        readonly EquatableList<ActiveExpression> arguments;
        readonly ConstructorInfo constructor;
        readonly EquatableList<Type> constructorParameterTypes;
        int disposalCount;
        readonly FastConstructorInfo fastConstructor;
        readonly NewExpression newExpression;

        void ArgumentPropertyChanged(object sender, PropertyChangedEventArgs e) => Evaluate();

        protected override bool Dispose(bool disposing)
        {
            var result = false;
            lock (instanceManagementLock)
                if (--disposalCount == 0)
                {
                    instances.Remove((newExpression, options));
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

        public override bool Equals(object? obj) => obj is ActiveNewExpression other && Equals(other);

        public bool Equals(ActiveNewExpression other) => Type == other.Type && arguments == other.arguments && Equals(options, other.options);

        protected override void Evaluate()
        {
            try
            {
                var argumentFault = arguments.Select(argument => argument.Fault).Where(fault => fault is { }).FirstOrDefault();
                if (argumentFault is { })
                    Fault = argumentFault;
                else
                    Value = fastConstructor.Invoke(arguments.Select(argument => argument.Value).ToArray());
            }
            catch (Exception ex)
            {
                Fault = ex;
            }
        }

        public override int GetHashCode() => HashCode.Combine(typeof(ActiveNewExpression), Type, arguments, options);

        protected override bool GetShouldValueBeDisposed() => ApplicableOptions.IsConstructedTypeDisposed(Type, constructorParameterTypes);

        public override string ToString() => $"new {Type.FullName}({string.Join(", ", arguments.Select(argument => $"{argument}"))}) {ToStringSuffix}";

        static readonly Dictionary<(NewExpression newExpression, ActiveExpressionOptions? options), ActiveNewExpression> instances = new Dictionary<(NewExpression newExpression, ActiveExpressionOptions? options), ActiveNewExpression>(new CachedInstancesKeyComparer<NewExpression>());
        static readonly object instanceManagementLock = new object();

        public static ActiveNewExpression Create(NewExpression newExpression, ActiveExpressionOptions? options, bool deferEvaluation)
        {
            var key = (newExpression, options);
            lock (instanceManagementLock)
            {
                if (!instances.TryGetValue(key, out var activeNewExpression))
                {
                    activeNewExpression = new ActiveNewExpression(newExpression, options, deferEvaluation);
                    instances.Add(key, activeNewExpression);
                }
                ++activeNewExpression.disposalCount;
                return activeNewExpression;
            }
        }

        public static bool operator ==(ActiveNewExpression a, ActiveNewExpression b) => a.Equals(b);

        [ExcludeFromCodeCoverage]
        public static bool operator !=(ActiveNewExpression a, ActiveNewExpression b) => !(a == b);
    }
}
