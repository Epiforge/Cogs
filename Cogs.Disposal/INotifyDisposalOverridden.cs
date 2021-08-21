namespace Cogs.Disposal;

/// <summary>
/// Notifies clients that the object's disposal has been overridden
/// </summary>
public interface INotifyDisposalOverridden
{
    /// <summary>
    /// Occurs when this object's disposal has been overridden
    /// </summary>
    event EventHandler<DisposalNotificationEventArgs>? DisposalOverridden;
}
