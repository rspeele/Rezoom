using System;
using System.Runtime.Serialization;

namespace Data.Resumption
{
    /// <summary>
    /// An exception that occurs when a data task is aborted for reasons beyond its control.
    /// </summary>
    /// <remarks>
    /// In particular, this happens when two tasks are interleaved, and one encounters an exception which it does not handle,
    /// but the other succeeds. The successful task will be given this exception so that it can clean up resources via its `finally` blocks.
    /// </remarks>
    public class DataTaskAbortException : Exception
    {
        internal DataTaskAbortException()
            : this("This data task has been aborted due to an unhandled exception in a concurrently applied task")
        {
        }

        internal DataTaskAbortException(string message) : base(message)
        {
        }

        internal DataTaskAbortException(string message, Exception innerException) : base(message, innerException)
        {
        }

        internal DataTaskAbortException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
