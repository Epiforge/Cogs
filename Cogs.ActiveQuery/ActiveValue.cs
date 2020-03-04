using Cogs.Disposal;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Gear.ActiveQuery
{
    /// <summary>
    /// Represents the scalar result of an active query
    /// </summary>
    /// <typeparam name="TValue">The type of the scalar result</typeparam>
    public class ActiveValue<TValue> : SyncDisposable, IActiveValue<TValue>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ActiveValue{TValue}"/> class
        /// </summary>
        /// <param name="value">The current value</param>
        /// <param name="operationFault">An action that will set the <see cref="OperationFault"/> property of the <see cref="ActiveValue{TValue}"/></param>
        /// <param name="elementFaultChangeNotifier">The <see cref="INotifyElementFaultChanges"/> for the underlying data from which the value is aggregated</param>
        public ActiveValue([MaybeNull] TValue value, Exception? operationFault = null, INotifyElementFaultChanges? elementFaultChangeNotifier = null)
        {
            this.value = value;
            this.operationFault = operationFault;
            this.elementFaultChangeNotifier = elementFaultChangeNotifier;
            InitializeFaultNotification();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ActiveValue{TValue}"/> class
        /// </summary>
        /// <param name="value">The current value</param>
        /// <param name="setValue">An action that will set the <see cref="Value"/> property of the <see cref="ActiveValue{TValue}"/></param>
        /// <param name="operationFault">The current operation fault</param>
        /// <param name="elementFaultChangeNotifier">The <see cref="INotifyElementFaultChanges"/> for the underlying data from which the value is aggregated</param>
        /// <param name="onDispose">The action to take when the <see cref="ActiveValue{TValue}"/> is disposed</param>
        public ActiveValue([MaybeNull] TValue value, out Action<TValue> setValue, Exception? operationFault = null, INotifyElementFaultChanges? elementFaultChangeNotifier = null, Action? onDispose = null) : this(value, operationFault, elementFaultChangeNotifier)
        {
            setValue = SetValue;
            this.onDispose = onDispose;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ActiveValue{TValue}"/> class
        /// </summary>
        /// <param name="value">The current value</param>
        /// <param name="setValue">An action that will set the <see cref="Value"/> property of the <see cref="ActiveValue{TValue}"/></param>
        /// <param name="setOperationFault">An action that will set the <see cref="OperationFault"/> property of the <see cref="ActiveValue{TValue}"/></param>
        /// <param name="elementFaultChangeNotifier">The <see cref="INotifyElementFaultChanges"/> for the underlying data from which the value is aggregated</param>
        /// <param name="onDispose">The action to take when the <see cref="ActiveValue{TValue}"/> is disposed</param>
        public ActiveValue([MaybeNull] TValue value, out Action<TValue> setValue, out Action<Exception?> setOperationFault, INotifyElementFaultChanges? elementFaultChangeNotifier = null, Action? onDispose = null) : this(value, out setValue, null, elementFaultChangeNotifier, onDispose) =>
            setOperationFault = SetOperationFault;

        /// <summary>
        /// Initializes a new instance of the <see cref="ActiveValue{TValue}"/> class
        /// </summary>
        /// <param name="value">The current value</param>
        /// <param name="setValue">An action that will set the <see cref="Value"/> property of the <see cref="ActiveValue{TValue}"/></param>
        /// <param name="operationFault">The current operation fault</param>
        /// <param name="setOperationFault">An action that will set the <see cref="OperationFault"/> property of the <see cref="ActiveValue{TValue}"/></param>
        /// <param name="elementFaultChangeNotifier">The <see cref="INotifyElementFaultChanges"/> for the underlying data from which the value is aggregated</param>
        /// <param name="onDispose">The action to take when the <see cref="ActiveValue{TValue}"/> is disposed</param>
        public ActiveValue([MaybeNull] TValue value, out Action<TValue> setValue, Exception? operationFault, out Action<Exception?> setOperationFault, INotifyElementFaultChanges? elementFaultChangeNotifier = null, Action? onDispose = null) : this(value, out setValue, operationFault, elementFaultChangeNotifier, onDispose) =>
            setOperationFault = SetOperationFault;

        readonly INotifyElementFaultChanges? elementFaultChangeNotifier;
        readonly Action? onDispose;
        Exception? operationFault;
        [MaybeNull] TValue value;

        /// <summary>
        /// Occurs when the fault for an element has changed
        /// </summary>
        public event EventHandler<ElementFaultChangeEventArgs>? ElementFaultChanged;

        /// <summary>
        /// Occurs when the fault for an element is changing
        /// </summary>
        public event EventHandler<ElementFaultChangeEventArgs>? ElementFaultChanging;

        /// <summary>
        /// Frees, releases, or resets unmanaged resources
        /// </summary>
        /// <param name="disposing"><c>false</c> if invoked by the finalizer because the object is being garbage collected; otherwise, <c>true</c></param>
        protected override bool Dispose(bool disposing)
        {
            onDispose?.Invoke();
            if (elementFaultChangeNotifier is { })
            {
                elementFaultChangeNotifier.ElementFaultChanged -= ElementFaultChangeNotifierElementFaultChanged;
                elementFaultChangeNotifier.ElementFaultChanging -= ElementFaultChangeNotifierElementFaultChanging;
            }
            return true;
        }

        void ElementFaultChangeNotifierElementFaultChanged(object sender, ElementFaultChangeEventArgs e) => ElementFaultChanged?.Invoke(this, e);

        void ElementFaultChangeNotifierElementFaultChanging(object sender, ElementFaultChangeEventArgs e) => ElementFaultChanging?.Invoke(this, e);

        /// <summary>
        /// Gets a list of all faulted elements
        /// </summary>
        /// <returns>The list</returns>
        public IReadOnlyList<(object? element, Exception? fault)> GetElementFaults() => elementFaultChangeNotifier?.GetElementFaults() ?? Enumerable.Empty<(object? element, Exception? fault)>().ToImmutableArray();

        void InitializeFaultNotification()
        {
            if (elementFaultChangeNotifier is { })
            {
                elementFaultChangeNotifier.ElementFaultChanged += ElementFaultChangeNotifierElementFaultChanged;
                elementFaultChangeNotifier.ElementFaultChanging += ElementFaultChangeNotifierElementFaultChanging;
            }
        }

        void SetOperationFault(Exception? operationFault) => OperationFault = operationFault;

        void SetValue([MaybeNull] TValue value) => Value = value;

        /// <summary>
        /// Gets the exception that occured the most recent time the query updated
        /// </summary>
        public Exception? OperationFault
        {
            get => operationFault;
            private set => SetBackedProperty(ref operationFault, in value);
        }

        /// <summary>
        /// Gets the value from the most recent time the query updated
        /// </summary>
        [MaybeNull]
        public TValue Value
        {
            get => value;
            private set => SetBackedProperty(ref this.value, in value);
        }
    }
}
