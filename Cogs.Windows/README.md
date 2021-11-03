This library includes utilities for interoperation with Microsoft Windows, including:

* `Activation` - provides information relating to Windows Activation
* `ConsoleAssist` - provides methods for interacting with consoles
* `Cursor` - wraps Win32 API methods dealing with the cursor
* `Shell` - wraps methods of the WScript.Shell COM object (specifically useful for invoking its `CreateShortcut` function)
* `Theme` - represents the current Windows theme

Also provides extension methods for dealing with processes, including:

* `CloseMainWindowAsync` - close the main window of the specified process
* `GetParentProcess` - gets the parent process of the specified process