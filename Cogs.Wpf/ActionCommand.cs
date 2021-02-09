using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace Cogs.Wpf
{
    /// <summary>
    /// A command that can be manipulated by its caller
    /// </summary>
    public class ActionCommand : ICommand, INotifyPropertyChanged, INotifyPropertyChanging
    {
        /// <summary>
        /// Initializes a new instance of <see cref="ActionCommand"/>
        /// </summary>
        /// <param name="executeAction">The action to invoke when the command is executed</param>
        /// <param name="executable"><c>true</c> if the command starts as executable; otherwise, <c>false</c></param>
        public ActionCommand(Action executeAction, bool executable)
        {
            this.executeAction = executeAction;
            this.executable = executable;
        }

        bool executable;
        readonly Action executeAction;

        /// <summary>
        /// Gets whether the command is executable
        /// </summary>
        public bool Executable
        {
            get => executable;
            set
            {
                if (executable != value)
                {
                    OnPropertyChanging();
                    executable = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Occurs when changes occur that affect whether or not the command should execute
        /// </summary>
        public event EventHandler? CanExecuteChanged;

        /// <summary>
        /// Occurs when a property value changes
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Occurs when a property value is changing
        /// </summary>
        public event PropertyChangingEventHandler? PropertyChanging;

        /// <summary>
        /// Defines the method that determines whether the command can execute in its current state
        /// </summary>
        /// <param name="parameter">Data used by the command</param>
        public bool CanExecute(object? parameter) => executable;

        /// <summary>
        /// Defines the method to be called when the command is invoked
        /// </summary>
        /// <param name="parameter">Data used by the command</param>
        public void Execute(object? parameter) => executeAction();

        /// <summary>
        /// Raises the <see cref="CanExecuteChanged"/> event
        /// </summary>
        /// <param name="e">The event data</param>
        protected virtual void OnCanExecuteChanged(EventArgs e) => CanExecuteChanged?.Invoke(this, e);

        /// <summary>
        /// Raises the <see cref="CanExecuteChanged"/> event
        /// </summary>
        protected void OnCanExecuteChanged() => OnCanExecuteChanged(new EventArgs());

        /// <summary>
        /// Raises the <see cref="PropertyChanged"/> event
        /// </summary>
		/// <param name="e">The arguments of the event</param>
        /// <exception cref="ArgumentNullException"><paramref name="e"/> is null</exception>
        protected virtual void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            if (e is null)
                throw new ArgumentNullException(nameof(e));
            PropertyChanged?.Invoke(this, e);
        }

        /// <summary>
        /// Notifies that a property changed
        /// </summary>
        /// <param name="propertyName">The name of the property that changed</param>
		/// <exception cref="ArgumentNullException"><paramref name="propertyName"/> is null</exception>
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            if (propertyName is null)
                throw new ArgumentNullException(nameof(propertyName));
            OnPropertyChanged(new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Raises the <see cref="PropertyChanging"/> event
        /// </summary>
		/// <param name="e">The arguments of the event</param>
        /// <exception cref="ArgumentNullException"><paramref name="e"/> is null</exception>
        protected virtual void OnPropertyChanging(PropertyChangingEventArgs e)
        {
            if (e is null)
                throw new ArgumentNullException(nameof(e));
            PropertyChanging?.Invoke(this, e);
        }

        /// <summary>
        /// Notifies that a property is changing
        /// </summary>
		/// <param name="propertyName">The name of the property that is changing</param>
        /// <exception cref="ArgumentNullException"><paramref name="propertyName"/> is null</exception>
        protected void OnPropertyChanging([CallerMemberName] string? propertyName = null)
        {
            if (propertyName is null)
                throw new ArgumentNullException(nameof(propertyName));
            OnPropertyChanging(new PropertyChangingEventArgs(propertyName));
        }
    }
}
