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
        ActiveMethodCallExpression(MethodCallExpression methodCallExpression, ActiveExpressionOptions? options, CachedInstancesKey<MethodCallExpression> instancesKey, bool deferEvaluation) : base(methodCallExpression.Type, methodCallExpression.NodeType, options, deferEvaluation)
        {
            var argumentsList = new List<ActiveExpression>();
            try
            {
                this.instancesKey = instancesKey;
                method = methodCallExpression.Method;
                fastMethod = FastMethodInfo.Get(method);
                if (methodCallExpression.Object is not null)
                {
                    @object = Create(methodCallExpression.Object, options, deferEvaluation);
                    @object.PropertyChanged += ObjectPropertyChanged;
                }
                foreach (var methodCallExpressionArgument in methodCallExpression.Arguments)
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
                if (@object is not null)
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
        readonly CachedInstancesKey<MethodCallExpression> instancesKey;
        readonly MethodInfo method;
        readonly ActiveExpression? @object;

        void ArgumentPropertyChanged(object sender, PropertyChangedEventArgs e) => Evaluate();

        protected override bool Dispose(bool disposing)
        {
            var result = false;
            lock (instanceManagementLock)
                if (--disposalCount == 0)
                {
                    instances.Remove(instancesKey);
                    result = true;
                }
            if (result)
            {
                DisposeValueIfNecessaryAndPossible();
                if (@object is not null)
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
                var argumentFault = arguments.Select(argument => argument.Fault).Where(fault => fault is not null).FirstOrDefault();
                if (objectFault is not null)
                    Fault = objectFault;
                else if (argumentFault is not null)
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
        static readonly Dictionary<CachedInstancesKey<MethodCallExpression>, ActiveMethodCallExpression> instances = new Dictionary<CachedInstancesKey<MethodCallExpression>, ActiveMethodCallExpression>(new CachedInstancesKeyComparer<MethodCallExpression>());

        public static ActiveMethodCallExpression Create(MethodCallExpression methodCallExpression, ActiveExpressionOptions? options, bool deferEvaluation)
        {
            var key = new CachedInstancesKey<MethodCallExpression>(methodCallExpression, options);
            lock (instanceManagementLock)
            {
                if (!instances.TryGetValue(key, out var activeMethodCallExpression))
                {
                    activeMethodCallExpression = new ActiveMethodCallExpression(methodCallExpression, options, key, deferEvaluation);
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
