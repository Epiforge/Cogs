using Cogs.Disposal;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Cogs.ActiveQuery
{
    class MergedElementFaultChangeNotifier : SyncDisposable, INotifyElementFaultChanges
    {
        public MergedElementFaultChangeNotifier(IEnumerable<INotifyElementFaultChanges> elementFaultChangeNotifiers)
        {
            this.elementFaultChangeNotifiers = elementFaultChangeNotifiers;
            foreach (var elementFaultChangeNotifier in this.elementFaultChangeNotifiers.Where(elementFaultChangeNotifier => elementFaultChangeNotifier is { }))
            {
                elementFaultChangeNotifier.ElementFaultChanged += ElementFaultChangeNotifierElementFaultChanged;
                elementFaultChangeNotifier.ElementFaultChanging += ElementFaultChangeNotifierElementFaultChanging;
            }
        }

        public MergedElementFaultChangeNotifier(params INotifyElementFaultChanges[] elementFaultChangeNotifiers) : this((IEnumerable<INotifyElementFaultChanges>)elementFaultChangeNotifiers)
        {
        }

        readonly IEnumerable<INotifyElementFaultChanges> elementFaultChangeNotifiers;

        public event EventHandler<ElementFaultChangeEventArgs>? ElementFaultChanged;
        public event EventHandler<ElementFaultChangeEventArgs>? ElementFaultChanging;

        protected override bool Dispose(bool disposing)
        {
            foreach (var elementFaultChangeNotifier in elementFaultChangeNotifiers.Where(elementFaultChangeNotifier => elementFaultChangeNotifier is { }))
            {
                elementFaultChangeNotifier.ElementFaultChanged -= ElementFaultChangeNotifierElementFaultChanged;
                elementFaultChangeNotifier.ElementFaultChanging -= ElementFaultChangeNotifierElementFaultChanging;
            }
            return true;
        }

        void ElementFaultChangeNotifierElementFaultChanged(object sender, ElementFaultChangeEventArgs e) => ElementFaultChanged?.Invoke(sender, e);

        void ElementFaultChangeNotifierElementFaultChanging(object sender, ElementFaultChangeEventArgs e) => ElementFaultChanging?.Invoke(sender, e);

        public IReadOnlyList<(object? element, Exception? fault)> GetElementFaults() =>
            elementFaultChangeNotifiers.SelectMany(elementFaultChangeNotifier => elementFaultChangeNotifier?.GetElementFaults() ?? Enumerable.Empty<(object? element, Exception? fault)>()).ToImmutableArray();
    }
}
