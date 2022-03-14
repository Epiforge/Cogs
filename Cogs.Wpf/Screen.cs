namespace Cogs.Wpf;

/// <summary>
/// Represents a display device or multiple display devices on a single system
/// </summary>
public class Screen : IEquatable<Screen>
{
    readonly System.Windows.Forms.Screen formsScreen;

    Screen(System.Windows.Forms.Screen formsScreen) =>
        this.formsScreen = formsScreen;

    /// <summary>
    /// Gets the number of bits of memory, associated with one pixel of data
    /// </summary>
    public int BitsPerPixel =>
        formsScreen.BitsPerPixel;

    /// <summary>
    /// Gets the bounds of the display
    /// </summary>
    public Rect Bounds =>
        GetDeviceIndependentRect(formsScreen.Bounds);

    /// <summary>
    /// Gets the device name associated with a display
    /// </summary>
    public string DeviceName =>
        formsScreen.DeviceName;

    /// <summary>
    /// Gets a value indicating whether a particular display is the primary device
    /// </summary>
    public bool Primary =>
        formsScreen.Primary;

    /// <summary>
    /// Gets the working area of the display
    /// (the working area is the desktop area of the display, excluding taskbars, docked windows, and docked tool bars)
    /// </summary>
    public Rect WorkingArea =>
        GetDeviceIndependentRect(formsScreen.WorkingArea);

    /// <summary>
    /// Determines whether the specified object is equal to the current object
    /// </summary>
    /// <param name="obj">The object to compare with the current object</param>
    /// <returns><c>true</c> if the specified object is equal to the current object; otherwise, <c>false</c></returns>
    public override bool Equals(object? obj) =>
        Equals(obj as Screen);

    /// <summary>
    /// Indicates whether the current object is equal to another object of the same type
    /// </summary>
    /// <param name="other">An object to compare with this object</param>
    /// <returns><c>true</c> if the current object is equal to the other parameter; otherwise, <c>false</c></returns>
    public bool Equals(Screen? other) =>
        other is not null && formsScreen == other.formsScreen;

    /// <summary>
    /// Serves as the hash function
    /// </summary>
    /// <returns>A hash code for the current object</returns>
    public override int GetHashCode() =>
        formsScreen.GetHashCode();

    /// <summary>
    /// Returns a string that represents the current object
    /// </summary>
    /// <returns>A string that represents the current object</returns>
    public override string ToString()
    {
        var bounds = Bounds;
        var workingArea = WorkingArea;
        return $"S[B[X{bounds.X}Y{bounds.Y}W{bounds.Width}H{bounds.Height}]W[X{workingArea.X}Y{workingArea.Y}W{workingArea.Width}H{workingArea.Height}]]";
    }

    /// <summary>
    /// Gets an array of all displays on the system
    /// </summary>
    public static IReadOnlyList<Screen> AllScreens =>
        System.Windows.Forms.Screen.AllScreens.Select(s => new Screen(s)).ToImmutableArray();

    /// <summary>
    /// Gets the primary display
    /// </summary>
    public static Screen PrimaryScreen =>
        new(System.Windows.Forms.Screen.PrimaryScreen);

    /// <summary>
    /// Gets the system PPI longitudinal ratio
    /// </summary>
    public static double XRatio =>
        SystemParameters.PrimaryScreenWidth / System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width;

    /// <summary>
    /// Gets the system PPI latitudinal ratio
    /// </summary>
    public static double YRatio =>
        SystemParameters.PrimaryScreenHeight / System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height;

    /// <summary>
    /// Retrieves a <see cref="Screen"/> for the display that contains the largest portion of the specified framework element
    /// </summary>
    /// <param name="frameworkElement">A <see cref="FrameworkElement"/> for which to retrieve a <see cref="Screen"/></param>
    public static Screen? FromFrameworkElement(FrameworkElement frameworkElement)
    {
        if (frameworkElement is null)
            throw new ArgumentNullException(nameof(frameworkElement));
        var frameworkElementPosition = frameworkElement.PointToScreen(new Point(0, 0));
        var formsScreen = System.Windows.Forms.Screen.FromRectangle(GetDeviceDependentRectangle(new Rect(frameworkElementPosition.X, frameworkElementPosition.Y, frameworkElement.ActualWidth, frameworkElement.ActualHeight)));
        return formsScreen is not null ? new Screen(formsScreen) : null;
    }

    /// <summary>
    /// Retrieves a <see cref="Screen"/> for the display that contains the largest portion of the object referred to by the specified handle
    /// </summary>
    /// <param name="hwnd">The window handle for which to retrieve the <see cref="Screen"/></param>
    public static Screen FromHandle(IntPtr hwnd) =>
        new(System.Windows.Forms.Screen.FromHandle(hwnd));

    /// <summary>
    /// Retrieves a <see cref="Screen"/> for the display that contains the specified point
    /// </summary>
    /// <param name="point">A <see cref="Point"/> that specifies the location for which to retrieve a <see cref="Screen"/></param>
    public static Screen FromPoint(Point point) =>
        new(System.Windows.Forms.Screen.FromPoint(GetDeviceDependentPoint(point)));

    /// <summary>
    /// Retrieves a <see cref="Screen"/> for the display that contains the largest portion of the rectangle
    /// </summary>
    /// <param name="rect">A <see cref="Rect"/> that specifies the area for which to retrieve the display</param>
    public static Screen FromRect(Rect rect) =>
        new(System.Windows.Forms.Screen.FromRectangle(GetDeviceDependentRectangle(rect)));

    /// <summary>
    /// Retrieves the bounds of the display that contains the specified point
    /// </summary>
    /// <param name="point">A <see cref="Point"/> that specifies the coordinates for which to retrieve the display bounds</param>
    /// <returns>
    /// A <see cref="Rect"/> that specifies the bounds of the display that contains the specified point
    /// (in multiple display environments where no display contains the specified point, the display closest to the point is returned)
    /// </returns>
    public static Rect GetBounds(Point point) =>
        GetDeviceIndependentRect(System.Windows.Forms.Screen.GetBounds(GetDeviceDependentPoint(point)));

    /// <summary>
    /// Retrieves the bounds of the display that contains the largest portion of the specified rectangle
    /// </summary>
    /// <param name="rect">A <see cref="Rect"/> that specifies the area for which to retrieve the display bounds</param>
    /// <returns>
    /// A <see cref="Rect"/> that specifies the bounds of the display that contains the specified rectangle
    /// (in multiple display environments where no monitor contains the specified rectangle, the monitor closest to the rectangle is returned)
    /// </returns>
    public static Rect GetBounds(Rect rect) =>
        GetDeviceIndependentRect(System.Windows.Forms.Screen.GetBounds(GetDeviceDependentRectangle(rect)));

    /// <summary>
    /// Retrieves the bounds of the display that contains the largest portion of the specified framework element
    /// </summary>
    /// <param name="frameworkElement">The <see cref="FrameworkElement"/> for which to retrieve the display bounds</param>
    /// <returns>
    /// A <see cref="Rect"/> that specifies the bounds of the display that contains the specified framework element
    /// (in multiple display environments where no display contains the specified framework element, the display closest to the framework element is returned)
    /// </returns>
    public static Rect GetBounds(FrameworkElement frameworkElement)
    {
        if (frameworkElement is null)
            throw new ArgumentNullException(nameof(frameworkElement));
        var frameworkElementPosition = frameworkElement.PointToScreen(new Point(0, 0));
        return GetDeviceIndependentRect(System.Windows.Forms.Screen.GetBounds(GetDeviceDependentRectangle(new Rect(frameworkElementPosition.X, frameworkElementPosition.Y, frameworkElement.ActualWidth, frameworkElement.ActualHeight))));
    }

    static System.Drawing.Point GetDeviceDependentPoint(Point point) =>
        new((int)(point.X / XRatio), (int)(point.Y / YRatio));

    static System.Drawing.Rectangle GetDeviceDependentRectangle(Rect rect)
    {
        var xRatio = XRatio;
        var yRatio = YRatio;
        return new System.Drawing.Rectangle((int)(rect.X / xRatio), (int)(rect.Y / yRatio), (int)(rect.Width / xRatio), (int)(rect.Height / yRatio));
    }

    static Rect GetDeviceIndependentRect(System.Drawing.Rectangle formsRectangle)
    {
        var xRatio = XRatio;
        var yRatio = YRatio;
        return new Rect(formsRectangle.Left * xRatio, formsRectangle.Top * yRatio, formsRectangle.Width * xRatio, formsRectangle.Height * yRatio);
    }

    /// <summary>
    /// Retrieves the working area closest to the specified point
    /// (the working area is the desktop area of the display, excluding taskbars, docked windows, and docked tool bars)
    /// </summary>
    /// <param name="point">A <see cref="Point"/> that specifies the coordinates for which to retrieve the working area</param>
    /// <returns>
    /// A <see cref="Rect"/> that specifies the working area
    /// (in multiple display environments where no display contains the specified point, the display closest to the point is returned)
    /// </returns>
    public static Rect GetWorkingArea(Point point) =>
        GetDeviceIndependentRect(System.Windows.Forms.Screen.GetWorkingArea(GetDeviceDependentPoint(point)));

    /// <summary>
    /// Retrieves the working area for the display that contains the largest portion of the specified rectangle
    /// (the working area is the desktop area of the display, excluding taskbars, docked windows, and docked tool bars)
    /// </summary>
    /// <param name="rect">The <see cref="Rect"/> that specifies the area for which to retrieve the working area</param>
    /// <returns>
    /// A <see cref="Rect"/> that specifies the working area
    /// (in multiple display environments where no display contains the specified rectangle, the display closest to the rectangle is returned)
    /// </returns>
    public static Rect GetWorkingArea(Rect rect) =>
        GetDeviceIndependentRect(System.Windows.Forms.Screen.GetWorkingArea(GetDeviceDependentRectangle(rect)));

    /// <summary>
    /// Retrieves the working area for the display that contains the largest region of the specified framework element
    /// (the working area is the desktop area of the display, excluding taskbars, docked windows, and docked tool bars)
    /// </summary>
    /// <param name="frameworkElement">The <see cref="FrameworkElement"/> for which to retrieve the working area</param>
    /// <returns>
    /// A <see cref="Rect"/> that specifies the working area
    /// (in multiple display environments where no display contains the specified framework element, the display closest to the framework element is returned)
    /// </returns>
    public static Rect GetWorkingArea(FrameworkElement frameworkElement)
    {
        if (frameworkElement is null)
            throw new ArgumentNullException(nameof(frameworkElement));
        Point frameworkElementPosition;
        try
        {
            frameworkElementPosition = frameworkElement.PointToScreen(new Point(0, 0));
        }
        catch (InvalidOperationException)
        {
            return Rect.Empty;
        }
        catch (NullReferenceException)
        {
            return Rect.Empty;
        }
        return GetDeviceIndependentRect(System.Windows.Forms.Screen.GetWorkingArea(new System.Drawing.Rectangle((int)frameworkElementPosition.X, (int)frameworkElementPosition.Y, (int)(frameworkElement.ActualWidth / XRatio), (int)(frameworkElement.ActualHeight / YRatio))));
    }
}
