using Cogs.Disposal;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Cogs.ActiveQuery
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
        /// <param name="operationFault">The current operation fault</param>
        /// <param name="elementFaultChangeNotifier">The <see cref="INotifyElementFaultChanges"/> for the underlying data from which the value is aggregated</param>
        /// <param name="onDispose">The action to take when the <see cref="ActiveValue{TValue}"/> is disposed</param>
        public ActiveValue([AllowNull] TValue value, Exception? operationFault = null, INotifyElementFaultChanges? elementFaultChangeNotifier = null, Action? onDispose = null)
        {
            this.value = value;
            this.operationFault = operationFault;
            this.elementFaultChangeNotifier = elementFaultChangeNotifier;
            InitializeFaultNotification();
            this.onDispose = onDispose;
        }

        readonly INotifyElementFaultChanges? elementFaultChangeNotifier;
        readonly Action? onDispose;
        Exception? operationFault;
        [AllowNull, MaybeNull] TValue value;

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

        /// <summary>
        /// Gets the exception that occured the most recent time the query updated
        /// </summary>
        public Exception? OperationFault
        {
            get => operationFault;
            protected internal set => SetBackedProperty(ref operationFault, in value);
        }

        /// <summary>
        /// Gets the value from the most recent time the query updated
        /// </summary>
        [AllowNull, MaybeNull]
        public TValue Value
        {
            get => value;
            protected internal set => SetBackedProperty(ref this.value! /* this could be null, but it won't matter if it is */, in value! /* this could be null, but it won't matter if it is */);
        }
    }
}
