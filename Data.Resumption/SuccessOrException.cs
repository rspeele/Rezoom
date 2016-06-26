using System;
using System.Runtime.ExceptionServices;

namespace Data.Resumption
{
    /// <summary>
    /// Represents the response to an <see cref="IDataRequest"/>.
    /// As its name implies, may either be success with an object carrying the retrieved data,
    /// or contain an exception that was thrown while attempting to retrieve the data.
    /// </summary>
    public struct SuccessOrException
    {
        private readonly object _success;
        private readonly Exception _exception;
        public SuccessOrException(object success)
        {
            _success = success;
            _exception = null;
        }

        public SuccessOrException(Exception exception)
        {
            if (exception == null)
            {
                throw new ArgumentNullException(nameof(exception));
            }
            _success = null;
            _exception = exception;
        }

        /// <summary>
        /// The data retrieved by the <see cref="IDataRequest"/>.
        /// It is not valid to access this property unless <see cref="HasSuccess"/> is true.
        /// </summary>
        public object Success
        {
            get
            {
                if (_exception != null)
                {
                    // rethrow with original stack trace
                    ExceptionDispatchInfo.Capture(_exception).Throw();
                }
                return _success;
            }
        }

        /// <summary>
        /// The exception thrown by the <see cref="IDataRequest"/>.
        /// It is not valid to access this property unless <see cref="HasSuccess"/> is false.
        /// </summary>
        public Exception Exception
        {
            get
            {
                if (_exception == null)
                {
                    throw new InvalidOperationException
                        ("Request operation succeeded unexpectedly");
                }
                return _exception;
            }
        }

        /// <summary>
        /// Whether the <see cref="IDataRequest"/> succeeded (and therefore has a <see cref="Success"/> value).
        /// </summary>
        public bool HasSuccess => _exception == null;

        public override string ToString()
            => HasSuccess ? $"Success: {Success}" : $"Exception: {_exception?.Message}";
    }
}