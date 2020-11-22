using Cogs.Collections;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.ExceptionServices;

namespace Cogs.ActiveExpressions
{
    class ActiveNewArrayInitExpression : ActiveExpression, IEquatable<ActiveNewArrayInitExpression>
    {
        ActiveNewArrayInitExpression(NewArrayExpression newArrayExpression, ActiveExpressionOptions? options, CachedInstancesKey<NewArrayExpression> instancesKey, bool deferEvaluation) : base(newArrayExpression.Type, newArrayExpression.NodeType, options, deferEvaluation)
        {
            var initializersList = new List<ActiveExpression>();
            try
            {
                this.instancesKey = instancesKey;
                elementType = newArrayExpression.Type.GetElementType();
                foreach (var newArrayExpressionInitializer in newArrayExpression.Expressions)
                {
                    var initializer = Create(newArrayExpressionInitializer, options, deferEvaluation);
                    initializer.PropertyChanged += InitializerPropertyChanged;
                    initializersList.Add(initializer);
                }
                initializers = new EquatableList<ActiveExpression>(initializersList);
                EvaluateIfNotDeferred();
            }
            catch (Exception ex)
            {
                foreach (var initializer in initializersList)
                {
                    initializer.PropertyChanged -= InitializerPropertyChanged;
                    initializer.Dispose();
                }
                ExceptionDispatchInfo.Capture(ex).Throw();
                throw;
            }
        }

        int disposalCount;
        readonly Type elementType;
        readonly EquatableList<ActiveExpression> initializers;
        readonly CachedInstancesKey<NewArrayExpression> instancesKey;

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
                foreach (var initializer in initializers)
                {
                    initializer.PropertyChanged -= InitializerPropertyChanged;
                    initializer.Dispose();
                }
            }
            return result;
        }

        public override bool Equals(object? obj) => obj is ActiveNewArrayInitExpression other && Equals(other);

        public bool Equals(ActiveNewArrayInitExpression other) => elementType == other.elementType && initializers == other.initializers && Equals(options, other.options);

        protected override void Evaluate()
        {
            var initializerFault = initializers.Select(initializer => initializer.Fault).Where(fault => fault is { }).FirstOrDefault();
            if (initializerFault is { })
                Fault = initializerFault;
            else
            {
                var array = Array.CreateInstance(elementType, initializers.Count);
                for (var i = 0; i < initializers.Count; ++i)
                    array.SetValue(initializers[i].Value, i);
                Value = array;
            }
        }

        public override int GetHashCode() => HashCode.Combine(typeof(ActiveNewArrayInitExpression), elementType, initializers, options);

        void InitializerPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e) => Evaluate();

        public override string ToString() => $"new {elementType.FullName}[] {{{string.Join(", ", initializers.Select(initializer => $"{initializer}"))}}} {ToStringSuffix}";

        static readonly object instanceManagementLock = new object();
        static readonly Dictionary<CachedInstancesKey<NewArrayExpression>, ActiveNewArrayInitExpression> instances = new Dictionary<CachedInstancesKey<NewArrayExpression>, ActiveNewArrayInitExpression>(new CachedInstancesKeyComparer<NewArrayExpression>());

        public static ActiveNewArrayInitExpression Create(NewArrayExpression newArrayExpression, ActiveExpressionOptions? options, bool deferEvaluation)
        {
            var key = new CachedInstancesKey<NewArrayExpression>(newArrayExpression, options);
            lock (instanceManagementLock)
            {
                if (!instances.TryGetValue(key, out var activenewArrayInitExpression))
                {
                    activenewArrayInitExpression = new ActiveNewArrayInitExpression(newArrayExpression, options, key, deferEvaluation);
                    instances.Add(key, activenewArrayInitExpression);
                }
                ++activenewArrayInitExpression.disposalCount;
                return activenewArrayInitExpression;
            }
        }

        public static bool operator ==(ActiveNewArrayInitExpression a, ActiveNewArrayInitExpression b) => a.Equals(b);

        [ExcludeFromCodeCoverage]
        public static bool operator !=(ActiveNewArrayInitExpression a, ActiveNewArrayInitExpression b) => !(a == b);
    }
}
