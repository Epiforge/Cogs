This library includes utilities for Windows Presentation Foundation, including:

* `ActionCommand` - a command that can be manipulated by its caller
* `ControlAssist` - provides attached dependency properties to enhance the functionality of controls (e.g. `AdditionalInputBindings`)
* `Screen` - represents a display device or multiple display devices on a single system
* `WindowAssist` - provides attached dependency properties to enhance the functionality of windows (e.g. `AutoActivation`, `BlurBehind`, `IsBlurredBehind`, `IsCaption`, `SendSystemCommand`, `SetDefaultWindowStyleOnSystemCommands`, `ShowSystemMenu`)

Also includes extension methods for visuals:

* `GetVisualAncestor` - gets the first ancestor of a reference in the Visual Tree, or <c>null</c> if none could be found
* `GetVisualDescendent` - gets the first member of a Visual Tree descending from a reference, or <c>null</c> if none could be found

Also includes extension methods for windows:

* `IsInSafePosition` - gets whether the specified window is completely contained within the closest working area
* `SafeguardPosition` - moves the specified window the minimum amount to be completely contained within the closest working area

Also includes behaviors:

* `ComboBoxDataVirtualization` & `ListBoxDataVirtualization` - sets the items source of a combo box or list box (including list views), respectively, to a collection that loads elements as they are needed for display and keeps selected elements loaded (requires .NET Core 3.1 or later)
* `DelayedFocus` - focuses an element after a specified delay
* `DeselectAllOnEmptySpaceClicked` - feselects all items when empty space in a list view is clicked
* `OpenNavigateUri` - opens the `Hyperlink`'s `NavigateUri` when it is clicked
* `PasswordBindingTarget` - allows binding to `PasswordBox.Password`

Also includes controls:

* `UrlAwareTextBlock` - provides a lightweight control for displaying small amounts of flow content which finds URLs and makes them clickable hyperlinks

Also includes input gestures:

* `MouseWheelDownGesture` - defines a mouse wheel down gesture that can be used to invoke a command
* `MouseWheelUpGesture` - defines a mouse wheel up gesture that can be used to invoke a command

Also includes validation rules:

* `InvalidCharactersValidationRule` - provides a way to create a rule in order to check that user input does not contain any invalid characters
* `StringNotEmptyValidationRule` - provides a way to create a rule in order to check that user input is not an empty string
* `ValidFileNameValidationRule` - provides a way to create a rule in order to check that user input does not contain any invalid file name characters
* `ValidPathValidationRule` - provides a way to create a rule in order to check that user input does not contain any invalid file system path characters

Also includes a wide array of value converters. Please see a package explorer for details.