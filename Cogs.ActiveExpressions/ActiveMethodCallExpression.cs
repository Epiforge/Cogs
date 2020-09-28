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
    class ActiveMethodCallExpression : ActiveExpression, IEquatable<ActiveMethodCallExpression>
    {
        ActiveMethodCallExpression(MethodCallExpression methodCallExpression, ActiveExpressionOptions? options, bool deferEvaluation) : base(methodCallExpression.Type, methodCallExpression.NodeType, options, deferEvaluation)
        {
            var argumentsList = new List<ActiveExpression>();
            try
            {
                this.methodCallExpression = methodCallExpression;
                method = this.methodCallExpression.Method;
                fastMethod = FastMethodInfo.Get(method);
                if (methodCallExpression.Object is { })
                {
                    @object = Create(this.methodCallExpression.Object, options, deferEvaluation);
                    @object.PropertyChanged += ObjectPropertyChanged;
                }
                foreach (var methodCallExpressionArgument in this.methodCallExpression.Arguments)
                {
                    var argument = Create(methodCallExpressionArgument, options, deferEvaluation);
                    argument.PropertyChanged += ArgumentPropertyChanged;
                    argumentsList.Add(argument);
                }
                arguments = new EquatableList<ActiveExpression>(argumentsList);
                EvaluateIfNotDeferred();
            }
            catch (Exception ex)
            {
                DisposeValueIfNecessaryAndPossible();
                if (@object is { })
                {
                    @object.PropertyChanged -= ObjectPropertyChanged;
                    @object.Dispose();
                }
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
        int disposalCount;
        readonly FastMethodInfo fastMethod;
        readonly MethodInfo method;
        readonly MethodCallExpression methodCallExpression;
        readonly ActiveExpression? @object;

        void ArgumentPropertyChanged(object sender, PropertyChangedEventArgs e) => Evaluate();

        protected override bool Dispose(bool disposing)
        {
            var result = false;
            lock (instanceManagementLock)
                if (--disposalCount == 0)
                {
                    instances.Remove((methodCallExpression, options));
                    result = true;
                }
            if (result)
            {
                DisposeValueIfNecessaryAndPossible();
                if (@object is { })
                {
                    @object.PropertyChanged -= ObjectPropertyChanged;
                    @object.Dispose();
                }
                foreach (var argument in arguments)
                {
                    argument.PropertyChanged -= ArgumentPropertyChanged;
                    argument.Dispose();
                }
            }
            return result;
        }

        public override bool Equals(object? obj) => obj is ActiveMethodCallExpression other && Equals(other);

        public bool Equals(ActiveMethodCallExpression other) => arguments == other.arguments && method == other.method && Equals(@object, other.@object) && Equals(options, other.options);

        protected override void Evaluate()
        {
            try
            {
                var objectFault = @object?.Fault;
                var argumentFault = arguments.Select(argument => argument.Fault).Where(fault => fault is { }).FirstOrDefault();
                if (objectFault is { })
                    Fault = objectFault;
                else if (argumentFault is { })
                    Fault = argumentFault;
                else
                    Value = fastMethod.Invoke(@object?.Value, arguments.Select(argument => argument.Value).ToArray());
            }
            catch (Exception ex)
            {
                Fault = ex;
            }
        }

        public override int GetHashCode() => HashCode.Combine(typeof(ActiveMethodCallExpression), arguments, method, @object, options);

        protected override bool GetShouldValueBeDisposed() => ApplicableOptions.IsMethodReturnValueDisposed(method);

        void ObjectPropertyChanged(object sender, PropertyChangedEventArgs e) => Evaluate();

        public override string ToString() => $"{@object?.ToString() ?? method.DeclaringType.FullName}.{method.Name}({string.Join(", ", arguments.Select(argument => $"{argument}"))}) {ToStringSuffix}";

        static readonly object instanceManagementLock = new object();
        static readonly Dictionary<(MethodCallExpression methodCallExpression, ActiveExpressionOptions? options), ActiveMethodCallExpression> instances = new Dictionary<(MethodCallExpression methodCallExpression, ActiveExpressionOptions? options), ActiveMethodCallExpression>(new CachedInstancesKeyComparer<MethodCallExpression>());

        public static ActiveMethodCallExpression Create(MethodCallExpression methodCallExpression, ActiveExpressionOptions? options, bool deferEvaluation)
        {
            var key = (methodCallExpression, options);
            lock (instanceManagementLock)
            {
                if (!instances.TryGetValue(key, out var activeMethodCallExpression))
                {
                    activeMethodCallExpression = new ActiveMethodCallExpression(methodCallExpression, options, deferEvaluation);
                    instances.Add(key, activeMethodCallExpression);
                }
                ++activeMethodCallExpression.disposalCount;
                return activeMethodCallExpression;
            }
        }

        public static bool operator ==(ActiveMethodCallExpression a, ActiveMethodCallExpression b) => a.Equals(b);

        [ExcludeFromCodeCoverage]
        public static bool operator !=(ActiveMethodCallExpression a, ActiveMethodCallExpression b) => !(a == b);
    }
}
