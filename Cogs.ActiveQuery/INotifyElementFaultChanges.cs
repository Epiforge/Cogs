using System;
using System.Collections.Generic;

namespace Gear.ActiveQuery
{
    /// <summary>
    /// Notifies listeners when the fault of an element in a sequence changes
    /// </summary>
    public interface INotifyElementFaultChanges
    {
        /// <summary>
        /// Occurs when the fault for an element has changed
        /// </summary>
        event EventHandler<ElementFaultChangeEventArgs>? ElementFaultChanged;

        /// <summary>
        /// Occurs when the fault for an element is changing
        /// </summary>
        event EventHandler<ElementFaultChangeEventArgs>? ElementFaultChanging;

        /// <summary>
        /// Gets a list of all faulted elements
        /// </summary>
        /// <returns>The list</returns>
        IReadOnlyList<(object? element, Exception? fault)> GetElementFaults();
    }
}
