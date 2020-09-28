using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Runtime.ExceptionServices;

namespace Cogs.ActiveExpressions
{
    class ActiveConditionalExpression : ActiveExpression, IEquatable<ActiveConditionalExpression>
    {
        ActiveConditionalExpression(ConditionalExpression conditionalExpression, ActiveExpressionOptions? options, bool deferEvaluation) : base(conditionalExpression.Type, conditionalExpression.NodeType, options, deferEvaluation)
        {
            try
            {
                this.conditionalExpression = conditionalExpression;
                test = Create(conditionalExpression.Test, options, deferEvaluation);
                test.PropertyChanged += TestPropertyChanged;
                ifTrue = Create(conditionalExpression.IfTrue, options, true);
                ifTrue.PropertyChanged += IfTruePropertyChanged;
                ifFalse = Create(conditionalExpression.IfFalse, options, true);
                ifFalse.PropertyChanged += IfFalsePropertyChanged;
                EvaluateIfNotDeferred();
            }
            catch (Exception ex)
            {
                if (test is { })
                {
                    test.PropertyChanged -= TestPropertyChanged;
                    test.Dispose();
                }
                if (ifTrue is { })
                {
                    ifTrue.PropertyChanged -= IfTruePropertyChanged;
                    ifTrue.Dispose();
                }
                if (ifFalse is { })
                {
                    ifFalse.PropertyChanged -= IfFalsePropertyChanged;
                    ifFalse.Dispose();
                }
                ExceptionDispatchInfo.Capture(ex).Throw();
                throw;
            }
        }

        readonly ConditionalExpression conditionalExpression;
        int disposalCount;
        readonly ActiveExpression ifFalse;
        readonly ActiveExpression ifTrue;
        readonly ActiveExpression test;

        protected override bool Dispose(bool disposing)
        {
            var result = false;
            lock (instanceManagementLock)
                if (--disposalCount == 0)
                {
                    instances.Remove((conditionalExpression, options));
                    result = true;
                }
            if (result)
            {
                test.PropertyChanged -= TestPropertyChanged;
                test.Dispose();
                ifTrue.PropertyChanged -= IfTruePropertyChanged;
                ifTrue.Dispose();
                ifFalse.PropertyChanged -= IfFalsePropertyChanged;
                ifFalse.Dispose();
            }
            return result;
        }

        public override bool Equals(object? obj) => obj is ActiveConditionalExpression other && Equals(other);

        public bool Equals(ActiveConditionalExpression other) => ifFalse == other.ifFalse && ifTrue == other.ifTrue && test == other.test && Equals(options, other.options);

        protected override void Evaluate()
        {
            var testFault = test.Fault;
            if (testFault is { })
                Fault = testFault;
            else if (test.Value is bool testBool && testBool)
            {
                var ifTrueFault = ifTrue.Fault;
                if (ifTrueFault is { })
                    Fault = ifTrueFault;
                else
                    Value = ifTrue.Value;
            }
            else
            {
                var ifFalseFault = ifFalse.Fault;
                if (ifFalseFault is { })
                    Fault = ifFalseFault;
                else
                    Value = ifFalse.Value;
            }
        }

        public override int GetHashCode() => HashCode.Combine(typeof(ActiveConditionalExpression), ifFalse, ifTrue, test, options);

        void IfFalsePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (test.Fault is null && !(test.Value is bool testBool && testBool))
            {
                if (e.PropertyName == nameof(Fault))
                    Fault = ifFalse.Fault;
                else if (e.PropertyName == nameof(Value) && ifFalse.Fault is null)
                    Value = ifFalse.Value;
            }
        }

        void IfTruePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (test.Fault is null && (test.Value is bool testBool && testBool))
            {
                if (e.PropertyName == nameof(Fault))
                    Fault = ifTrue.Fault;
                else if (e.PropertyName == nameof(Value) && ifTrue.Fault is null)
                    Value = ifTrue.Value;
            }
        }

        void TestPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Fault))
                Fault = test.Fault;
            else if (e.PropertyName == nameof(Value) && test.Fault is null)
            {
                if (test.Value is bool testBool && testBool)
                {
                    var ifTrueFault = ifTrue.Fault;
                    if (ifTrueFault is { })
                        Fault = ifTrueFault;
                    else
                        Value = ifTrue.Value;
                }
                else
                {
                    var ifFalseFault = ifFalse.Fault;
                    if (ifFalseFault is { })
                        Fault = ifFalseFault;
                    else
                        Value = ifFalse.Value;
                }
            }
        }

        public override string ToString() => $"({test} ? {ifTrue} : {ifFalse}) {ToStringSuffix}";

        static readonly object instanceManagementLock = new object();
        static readonly Dictionary<(ConditionalExpression conditionalExpression, ActiveExpressionOptions? options), ActiveConditionalExpression> instances = new Dictionary<(ConditionalExpression conditionalExpression, ActiveExpressionOptions? options), ActiveConditionalExpression>(new CachedInstancesKeyComparer<ConditionalExpression>());

        public static ActiveConditionalExpression Create(ConditionalExpression conditionalExpression, ActiveExpressionOptions? options, bool deferEvaluation)
        {
            var key = (conditionalExpression, options);
            lock (instanceManagementLock)
            {
                if (!instances.TryGetValue(key, out var activeConditionalExpression))
                {
                    activeConditionalExpression = new ActiveConditionalExpression(conditionalExpression, options, deferEvaluation);
                    instances.Add(key, activeConditionalExpression);
                }
                ++activeConditionalExpression.disposalCount;
                return activeConditionalExpression;
            }
        }

        public static bool operator ==(ActiveConditionalExpression a, ActiveConditionalExpression b) => a.Equals(b);

        [ExcludeFromCodeCoverage]
        public static bool operator !=(ActiveConditionalExpression a, ActiveConditionalExpression b) => !(a == b);
    }
}
