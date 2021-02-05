using Cogs.Components;
using System;
using System.Windows.Input;

namespace Cogs.Wpf
{
    /// <summary>
    /// A command that can be manipulated by its caller
    /// </summary>
    public class ActionCommand : PropertyChangeNotifier, ICommand
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
                if (SetBackedProperty(ref executable, in value))
                    OnCanExecuteChanged();
            }
        }

        /// <summary>
        /// Occurs when changes occur that affect whether or not the command should execute
        /// </summary>
        public event EventHandler? CanExecuteChanged;

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
    }
}
