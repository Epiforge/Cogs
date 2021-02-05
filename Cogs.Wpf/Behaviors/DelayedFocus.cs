using Microsoft.Xaml.Behaviors;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace Cogs.Wpf.Behaviors
{
    /// <summary>
    /// Focuses an element after a specified delay
    /// </summary>
    public class DelayedFocus : Behavior<UIElement>
    {
        Dispatcher? dispatcher;

        /// <summary>
        /// Gets/sets the delay before the focus operation occurs
        /// </summary>
        public TimeSpan? Delay { get; set; }

        async void DelayCallback(object? state)
        {
            await Task.Delay(Delay ?? TimeSpan.Zero).ConfigureAwait(false);
            if (dispatcher is not null)
                await dispatcher.InvokeAsync(FocusCallback).Task.ConfigureAwait(false);
        }

        void FocusCallback() => AssociatedObject.Focus();

        /// <summary>
        /// Called after the behavior is attached to an <see cref="Behavior{UIElement}.AssociatedObject"/>
        /// </summary>
        protected override void OnAttached()
        {
            base.OnAttached();
            dispatcher = AssociatedObject.Dispatcher;
            ThreadPool.QueueUserWorkItem(DelayCallback);
        }
    }
}
