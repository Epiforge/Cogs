using System.Windows.Input;

namespace Cogs.Wpf.Input
{
    /// <summary>
    /// Defines a mouse wheel up gesture that can be used to invoke a command
    /// </summary>
    public class MouseWheelUpGesture : MouseGesture
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MouseWheelUpGesture"/> class
        /// </summary>
        public MouseWheelUpGesture()
            : base(MouseAction.WheelClick)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MouseWheelUpGesture"/> class using the specified <see cref="ModifierKeys"/>
        /// </summary>
        /// <param name="modifierKeys">The modifiers associated with this gesture</param>
        public MouseWheelUpGesture(ModifierKeys modifierKeys)
            : base(MouseAction.WheelClick, modifierKeys)
        {
        }

        /// <summary>
        /// Determines whether <see cref="MouseWheelUpGesture"/> matches the input associated with the specified <see cref="InputEventArgs"/> object
        /// </summary>
        /// <param name="targetElement">The target</param>
        /// <param name="inputEventArgs">The input event data to compare with this gesture</param>
        public override bool Matches(object targetElement, InputEventArgs inputEventArgs) =>
            base.Matches(targetElement, inputEventArgs) &&
            inputEventArgs is MouseWheelEventArgs mouseWheelEventArgs &&
            mouseWheelEventArgs.Delta > 0;
    }
}
