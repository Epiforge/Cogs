namespace Cogs.Components;

/// <summary>
/// Provides a mechanism for notifying about property changes
/// </summary>
public abstract class PropertyChangeNotifier : INotifyPropertyChanged, INotifyPropertyChanging
{
    /// <summary>
    /// Occurs when a property value changes
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Occurs when a property value is changing
    /// </summary>
    public event PropertyChangingEventHandler? PropertyChanging;

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

    /// <summary>
    /// Compares a property's backing field and a new value for inequality, and when they are unequal, raises the <see cref="PropertyChanging"/> event, sets the backing field to the new value, and then raises the <see cref="PropertyChanged"/> event
    /// </summary>
    /// <typeparam name="TValue">The type of the property</typeparam>
    /// <param name="backingField">A reference to the backing field of the property</param>
    /// <param name="value">The new value</param>
    /// <param name="propertyName">The name of the property</param>
    /// <returns>true if <paramref name="backingField"/> was unequal to <paramref name="value"/>; otherwise, false</returns>
    [SuppressMessage("Code Analysis", "CA1045: Do not pass types by reference", Justification = "To 'correct' this would defeat the purpose of the method")]
    protected bool SetBackedProperty<TValue>(ref TValue backingField, TValue value, [CallerMemberName] string? propertyName = null)
    {
        if (!EqualityComparer<TValue>.Default.Equals(backingField, value))
        {
            OnPropertyChanging(propertyName);
            backingField = value;
            OnPropertyChanged(propertyName);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Compares a property's backing field and a new value for inequality, and when they are unequal, raises the <see cref="PropertyChanging"/> event, sets the backing field to the new value, and then raises the <see cref="PropertyChanged"/> event
    /// </summary>
    /// <typeparam name="TValue">The type of the property</typeparam>
    /// <param name="backingField">A reference to the backing field of the property</param>
    /// <param name="value">The new value</param>
    /// <param name="propertyName">The name of the property</param>
    /// <returns>true if <paramref name="backingField"/> was unequal to <paramref name="value"/>; otherwise, false</returns>
    [SuppressMessage("Code Analysis", "CA1045: Do not pass types by reference", Justification = "To 'correct' this would defeat the purpose of the method")]
    protected bool SetBackedProperty<TValue>(ref TValue backingField, in TValue value, [CallerMemberName] string? propertyName = null)
    {
        if (!EqualityComparer<TValue>.Default.Equals(backingField, value))
        {
            OnPropertyChanging(propertyName);
            backingField = value;
            OnPropertyChanged(propertyName);
            return true;
        }
        return false;
    }
}