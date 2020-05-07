using System.Threading;

namespace Cogs.Threading
{
    /// <summary>
    /// Represents an object the operations of which occur on a specific <see cref="System.Threading.SynchronizationContext"/>
    /// </summary>
    public interface ISynchronized
    {
        /// <summary>
        /// Gets the <see cref="System.Threading.SynchronizationContext"/> on which this object's operations occur
        /// </summary>
        SynchronizationContext? SynchronizationContext { get; }
    }
}
