using Microsoft.Xaml.Behaviors;
using System.Windows;
using System.Windows.Controls;

namespace Cogs.Wpf.Behaviors
{
    /// <summary>
    /// Allows binding to <see cref="PasswordBox.Password"/>
    /// </summary>
    public class PasswordBindingTarget : Behavior<PasswordBox>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PasswordBindingTarget"/> class
        /// </summary>
        public PasswordBindingTarget()
        {
            dependencyPropertyChangeSource = false;
            passwordBoxChangeSource = false;
        }

        bool dependencyPropertyChangeSource;
        bool passwordBoxChangeSource;

        void PasswordBoxPasswordChanged(object sender, RoutedEventArgs e)
        {
            if (!dependencyPropertyChangeSource)
            {
                passwordBoxChangeSource = true;
                SetCurrentValue(PasswordProperty, AssociatedObject.Password);
                passwordBoxChangeSource = false;
            }
        }

        /// <summary>
        /// Called after the behavior is attached to an <see cref="Behavior{PasswordBox}.AssociatedObject"/>
        /// </summary>
        protected override void OnAttached()
        {
            dependencyPropertyChangeSource = true;
            AssociatedObject.Password = (string)GetValue(PasswordProperty);
            dependencyPropertyChangeSource = false;
            AssociatedObject.PasswordChanged += PasswordBoxPasswordChanged;
        }

        /// <summary>
        /// Called when the behavior is being detached from its <see cref="Behavior{PasswordBox}.AssociatedObject"/>, but before it has actually occurred
        /// </summary>
        protected override void OnDetaching()
        {
            AssociatedObject.PasswordChanged -= PasswordBoxPasswordChanged;
            dependencyPropertyChangeSource = true;
            AssociatedObject.Password = null;
            dependencyPropertyChangeSource = false;
        }

        /// <summary>
        /// Identifies the Password dependency property
        /// </summary>
        public static readonly DependencyProperty PasswordProperty = DependencyProperty.Register("Password", typeof(string), typeof(PasswordBindingTarget), new PropertyMetadata(null, OnPasswordChanged));

        static void OnPasswordChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is PasswordBindingTarget passwordBindingTarget && !passwordBindingTarget.passwordBoxChangeSource)
            {
                passwordBindingTarget.dependencyPropertyChangeSource = true;
                passwordBindingTarget.AssociatedObject.Password = e.NewValue as string;
                passwordBindingTarget.dependencyPropertyChangeSource = false;
            }
        }
    }
}
