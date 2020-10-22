using System;

namespace Cogs.Threading
{
    /// <summary>
    /// Represents errors that occur when the application attempts to escalate a lock
    /// </summary>
    public class LockEscalationException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LockEscalationException"/> class
        /// </summary>
        public LockEscalationException() : base("cannot escalate lock")
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LockEscalationException"/> class with a specified error
        /// </summary>
        /// <param name="message">The message that describes the error</param>
        public LockEscalationException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LockEscalationException"/> class with a specified error message and a reference to the inner exception that is the cause of this exception
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception</param>
        /// <param name="innerException">The exception that is the cause of the current exception, or a null reference if no inner exception is specified</param>
        public LockEscalationException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
