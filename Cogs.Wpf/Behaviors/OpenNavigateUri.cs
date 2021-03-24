using Microsoft.Xaml.Behaviors;
using System.Diagnostics;
using System.Windows;
using System.Windows.Documents;

namespace Cogs.Wpf.Behaviors
{
    /// <summary>
    /// Opens the <see cref="Hyperlink"/>'s <see cref="Hyperlink.NavigateUri"/> when it is clicked
    /// </summary>
    public class OpenNavigateUri : Behavior<Hyperlink>
    {
        /// <summary>
        /// Called after the behavior is attached to an <see cref="Behavior{Hyperlink}.AssociatedObject"/>
        /// </summary>
        protected override void OnAttached() => AssociatedObject.Click += Click;

        /// <summary>
        /// Called when the behavior is being detached from its <see cref="Behavior{Hyperlink}.AssociatedObject"/>, but before it has actually occurred
        /// </summary>
        protected override void OnDetaching() => AssociatedObject.Click -= Click;

        static void Click(object sender, RoutedEventArgs e)
        {
            if (sender is Hyperlink hyperlink && hyperlink.NavigateUri is { } uri)
                Process.Start(new ProcessStartInfo(uri.ToString())
                {
                    Verb = "open",
                    UseShellExecute = true
                });
        }
    }
}
