namespace Cogs.ActiveExpressions;

/// <summary>
/// Instructs the Active Expressions system not to listen for property change notifications for the decorated property
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class DoNotListenForPropertyChangesAttribute :
    Attribute
{
}
