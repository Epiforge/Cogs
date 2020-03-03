using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq.Expressions;

namespace Cogs.ActiveExpressions
{
    class ActiveConditionalExpression : ActiveExpression, IEquatable<ActiveConditionalExpression>
    {
        ActiveConditionalExpression(Type type, ActiveExpression test, ActiveExpression ifTrue, ActiveExpression ifFalse, ActiveExpressionOptions? options, bool deferEvaluation) : base(type, ExpressionType.Conditional, options, deferEvaluation)
        {
            this.test = test;
            this.test.PropertyChanged += TestPropertyChanged;
            this.ifTrue = ifTrue;
            this.ifTrue.PropertyChanged += IfTruePropertyChanged;
            this.ifFalse = ifFalse;
            this.ifFalse.PropertyChanged += IfFalsePropertyChanged;
            EvaluateIfNotDeferred();
        }

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
                    instances.Remove((test, ifTrue, ifFalse, options));
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

        public override bool Equals(object obj) => obj is ActiveConditionalExpression other && Equals(other);

        public bool Equals(ActiveConditionalExpression other) => ifFalse.Equals(other.ifFalse) && ifTrue.Equals(other.ifTrue) && test.Equals(other.test) && Equals(options, other.options);

        protected override void Evaluate()
        {
            var testFault = test.Fault;
            if (testFault != null)
                Fault = testFault;
            else if (test.Value is bool testBool ? testBool : false)
            {
                var ifTrueFault = ifTrue.Fault;
                if (ifTrueFault != null)
                    Fault = ifTrueFault;
                else
                    Value = ifTrue.Value;
            }
            else
            {
                var ifFalseFault = ifFalse.Fault;
                if (ifFalseFault != null)
                    Fault = ifFalseFault;
                else
                    Value = ifFalse.Value;
            }
        }

        public override int GetHashCode() => HashCode.Combine(typeof(ActiveConditionalExpression), ifFalse, ifTrue, test, options);

        void IfFalsePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (test.Fault == null && !(test.Value is bool testBool ? testBool : false))
            {
                if (e.PropertyName == nameof(Fault))
                    Fault = ifFalse.Fault;
                else if (e.PropertyName == nameof(Value) && ifFalse.Fault == null)
                    Value = ifFalse.Value;
            }
        }

        void IfTruePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (test.Fault == null && (test.Value is bool testBool ? testBool : false))
            {
                if (e.PropertyName == nameof(Fault))
                    Fault = ifTrue.Fault;
                else if (e.PropertyName == nameof(Value) && ifTrue.Fault == null)
                    Value = ifTrue.Value;
            }
        }

        void TestPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Fault))
                Fault = test.Fault;
            else if (e.PropertyName == nameof(Value) && test.Fault == null)
            {
                if (test.Value is bool testBool ? testBool : false)
                {
                    var ifTrueFault = ifTrue.Fault;
                    if (ifTrueFault != null)
                        Fault = ifTrueFault;
                    else
                        Value = ifTrue.Value;
                }
                else
                {
                    var ifFalseFault = ifFalse.Fault;
                    if (ifFalseFault != null)
                        Fault = ifFalseFault;
                    else
                        Value = ifFalse.Value;
                }
            }
        }

        public override string ToString() => $"({test} ? {ifTrue} : {ifFalse}) {ToStringSuffix}";

        static readonly object instanceManagementLock = new object();
        static readonly Dictionary<(ActiveExpression test, ActiveExpression ifTrue, ActiveExpression ifFalse, ActiveExpressionOptions? options), ActiveConditionalExpression> instances = new Dictionary<(ActiveExpression test, ActiveExpression ifTrue, ActiveExpression ifFalse, ActiveExpressionOptions? options), ActiveConditionalExpression>();

        public static ActiveConditionalExpression Create(ConditionalExpression conditionalExpression, ActiveExpressionOptions? options, bool deferEvaluation)
        {
            var test = Create(conditionalExpression.Test, options, deferEvaluation);
            var ifTrue = Create(conditionalExpression.IfTrue, options, true);
            var ifFalse = Create(conditionalExpression.IfFalse, options, true);
            var key = (test, ifTrue, ifFalse, options);
            lock (instanceManagementLock)
            {
                if (!instances.TryGetValue(key, out var activeConditionalExpression))
                {
                    activeConditionalExpression = new ActiveConditionalExpression(conditionalExpression.Type, test, ifTrue, ifFalse, options, deferEvaluation);
                    instances.Add(key, activeConditionalExpression);
                }
                ++activeConditionalExpression.disposalCount;
                return activeConditionalExpression;
            }
        }

        public static bool operator ==(ActiveConditionalExpression? a, ActiveConditionalExpression? b) => EqualityComparer<ActiveConditionalExpression?>.Default.Equals(a, b);

        public static bool operator !=(ActiveConditionalExpression? a, ActiveConditionalExpression? b) => !EqualityComparer<ActiveConditionalExpression?>.Default.Equals(a, b);
    }
}
