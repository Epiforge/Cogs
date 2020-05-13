using Cogs.Collections;
using Cogs.Disposal;
using Cogs.Reflection;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Cogs.ActiveExpressions
{
    class ActiveIndexExpression : ActiveExpression, IEquatable<ActiveIndexExpression>
    {
        ActiveIndexExpression(Type type, ActiveExpression @object, PropertyInfo indexer, EquatableList<ActiveExpression> arguments, ActiveExpressionOptions? options, bool deferEvaluation) : base(type, ExpressionType.Index, options, deferEvaluation)
        {
            this.indexer = indexer;
            getMethod = this.indexer.GetMethod;
            fastGetter = FastMethodInfo.Get(getMethod);
            this.@object = @object;
            this.@object.PropertyChanged += ObjectPropertyChanged;
            this.arguments = arguments;
            foreach (var argument in this.arguments)
                argument.PropertyChanged += ArgumentPropertyChanged;
            EvaluateIfNotDeferred();
        }

        readonly EquatableList<ActiveExpression> arguments;
        int disposalCount;
        readonly FastMethodInfo fastGetter;
        readonly MethodInfo getMethod;
        readonly PropertyInfo indexer;
        readonly ActiveExpression @object;
        object? objectValue;

        void ArgumentPropertyChanged(object sender, PropertyChangedEventArgs e) => Evaluate();

        protected override bool Dispose(bool disposing)
        {
            lock (instanceManagementLock)
                if (--disposalCount == 0)
                {
                    instances.Remove((@object, indexer, arguments, options));
                    return true;
                }
            return false;
        }

        void DisposeValueIfNecessary()
        {
            if (ApplicableOptions.IsMethodReturnValueDisposed(getMethod))
                DisposeValueIfPossible();
        }

        public override bool Equals(object obj) => obj is ActiveIndexExpression other && Equals(other);

        public bool Equals(ActiveIndexExpression other) => arguments.Equals(other.arguments) && indexer.Equals(other.indexer) && @object.Equals(other.@object) && Equals(options, other.options);

        [SuppressMessage("Design", "CA1031:Do not catch general exception types")]
        protected override void Evaluate()
        {
            try
            {
                DisposeValueIfNecessary();
                var objectFault = @object.Fault;
                var argumentFault = arguments.Select(argument => argument.Fault).Where(fault => fault is { }).FirstOrDefault();
                if (objectFault is { })
                    Fault = objectFault;
                else if (argumentFault is { })
                    Fault = argumentFault;
                else
                {
                    var newObjectValue = @object.Value;
                    if (newObjectValue != objectValue)
                    {
                        UnsubscribeFromObjectValueNotifications();
                        objectValue = newObjectValue;
                        SubscribeToObjectValueNotifications();
                    }
                    Value = fastGetter.Invoke(objectValue, arguments.Select(argument => argument.Value).ToArray());
                }
            }
            catch (Exception ex)
            {
                Fault = ex;
            }
        }

        public override int GetHashCode() => HashCode.Combine(typeof(ActiveIndexExpression), arguments, indexer, @object, options);

        void ObjectPropertyChanged(object sender, PropertyChangedEventArgs e) => Evaluate();

        void ObjectValueCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    {
                        if (e.NewStartingIndex >= 0 && (e.NewItems?.Count ?? 0) > 0 && arguments.Count == 1 && arguments[0].Value is int index && e.NewStartingIndex <= index)
                            Evaluate();
                    }
                    break;
                case NotifyCollectionChangedAction.Move:
                    {
                        var movingCount = Math.Max(e.OldItems?.Count ?? 0, e.NewItems?.Count ?? 0);
                        if (e.OldStartingIndex >= 0 && e.NewStartingIndex >= 0 && movingCount > 0 && arguments.Count == 1 && arguments[0].Value is int index && ((index >= e.OldStartingIndex && index < e.OldStartingIndex + movingCount) || (index >= e.NewStartingIndex && index < e.NewStartingIndex + movingCount)))
                            Evaluate();
                    }
                    break;
                case NotifyCollectionChangedAction.Remove:
                    {
                        if (e.OldStartingIndex >= 0 && (e.OldItems?.Count ?? 0) > 0 && arguments.Count == 1 && arguments[0].Value is int index && e.OldStartingIndex <= index)
                            Evaluate();
                    }
                    break;
                case NotifyCollectionChangedAction.Replace:
                    {
                        if (arguments.Count == 1 && arguments[0].Value is int index)
                        {
                            var oldCount = e.OldItems?.Count ?? 0;
                            var newCount = e.NewItems?.Count ?? 0;
                            if ((oldCount != newCount && (e.OldStartingIndex >= 0 || e.NewStartingIndex >= 0) && index >= Math.Min(Math.Max(e.OldStartingIndex, 0), Math.Max(e.NewStartingIndex, 0))) || (e.OldStartingIndex >= 0 && index >= e.OldStartingIndex && index < e.OldStartingIndex + oldCount) || (e.NewStartingIndex >= 0 && index >= e.NewStartingIndex && index < e.NewStartingIndex + newCount))
                                Evaluate();
                        }
                    }
                    break;
                case NotifyCollectionChangedAction.Reset:
                    Evaluate();
                    break;
            }
        }

        void ObjectValueDictionaryChanged(object sender, NotifyDictionaryChangedEventArgs<object?, object?> e)
        {
            if (e.Action == NotifyDictionaryChangedAction.Reset)
                Evaluate();
            else if (arguments.Count == 1)
            {
                var removed = false;
                var key = arguments[0].Value;
                if (key is { })
                {
                    removed = e.OldItems?.Any(kv => key.Equals(kv.Key)) ?? false;
                    var keyValuePair = e.NewItems?.FirstOrDefault(kv => key.Equals(kv.Key)) ?? default;
                    if (keyValuePair.Key is { })
                    {
                        removed = false;
                        Value = keyValuePair.Value;
                    }
                }
                if (removed)
                    Fault = new KeyNotFoundException($"Key '{key}' was removed");
            }
        }

        void ObjectValuePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == indexer.Name)
                Evaluate();
        }

        protected override void OnDisposed(DisposalNotificationEventArgs e)
        {
            DisposeValueIfNecessary();
            UnsubscribeFromObjectValueNotifications();
            @object.PropertyChanged -= ObjectPropertyChanged;
            @object.Dispose();
            foreach (var argument in arguments)
            {
                argument.PropertyChanged -= ArgumentPropertyChanged;
                argument.Dispose();
            }
            base.OnDisposed(e);
        }

        void SubscribeToObjectValueNotifications()
        {
            if (objectValue is INotifyDictionaryChanged dictionaryChangedNotifier)
                dictionaryChangedNotifier.DictionaryChanged += ObjectValueDictionaryChanged;
            else if (objectValue is INotifyCollectionChanged collectionChangedNotifier)
                collectionChangedNotifier.CollectionChanged += ObjectValueCollectionChanged;
            if (objectValue is INotifyPropertyChanged propertyChangedNotifier)
                propertyChangedNotifier.PropertyChanged += ObjectValuePropertyChanged;
        }

        public override string ToString() => $"{@object}{string.Join(string.Empty, arguments.Select(argument => $"[{argument}]"))} {ToStringSuffix}";

        void UnsubscribeFromObjectValueNotifications()
        {
            if (objectValue is INotifyDictionaryChanged dictionaryChangedNotifier)
                dictionaryChangedNotifier.DictionaryChanged -= ObjectValueDictionaryChanged;
            else if (objectValue is INotifyCollectionChanged collectionChangedNotifier)
                collectionChangedNotifier.CollectionChanged -= ObjectValueCollectionChanged;
            if (objectValue is INotifyPropertyChanged propertyChangedNotifier)
                propertyChangedNotifier.PropertyChanged -= ObjectValuePropertyChanged;
        }

        static readonly object instanceManagementLock = new object();
        static readonly Dictionary<(ActiveExpression @object, PropertyInfo indexer, EquatableList<ActiveExpression> arguments, ActiveExpressionOptions? options), ActiveIndexExpression> instances = new Dictionary<(ActiveExpression @object, PropertyInfo indexer, EquatableList<ActiveExpression> arguments, ActiveExpressionOptions? options), ActiveIndexExpression>();

        public static ActiveIndexExpression Create(IndexExpression indexExpression, ActiveExpressionOptions? options, bool deferEvaluation)
        {
            var @object = Create(indexExpression.Object, options, deferEvaluation);
            var indexer = indexExpression.Indexer;
            var arguments = new EquatableList<ActiveExpression>(indexExpression.Arguments.Select(argument => Create(argument, options, deferEvaluation)).ToList());
            var key = (@object, indexer, arguments, options);
            lock (instanceManagementLock)
            {
                if (!instances.TryGetValue(key, out var activeIndexExpression))
                {
                    activeIndexExpression = new ActiveIndexExpression(indexExpression.Type, @object, indexer, arguments, options, deferEvaluation);
                    instances.Add(key, activeIndexExpression);
                }
                ++activeIndexExpression.disposalCount;
                return activeIndexExpression;
            }
        }

        public static bool operator ==(ActiveIndexExpression? a, ActiveIndexExpression? b) => EqualityComparer<ActiveIndexExpression?>.Default.Equals(a, b);

        public static bool operator !=(ActiveIndexExpression? a, ActiveIndexExpression? b) => !EqualityComparer<ActiveIndexExpression?>.Default.Equals(a, b);
    }
}
