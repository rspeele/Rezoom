using System;

namespace Data.Resumption.DataEnumerables
{
    /// <summary>
    /// Represents a data enumerable wrapped with an exception handler.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal class TryCatchEnumerable<T> : IDataEnumerable<T>
    {
        private readonly IDataEnumerable<T> _wrapped;
        private readonly Func<Exception, IDataEnumerable<T>> _exceptionHandler;

        public TryCatchEnumerable
            (IDataEnumerable<T> wrapped, Func<Exception, IDataEnumerable<T>> exceptionHandler)
        {
            _wrapped = wrapped;
            _exceptionHandler = exceptionHandler;
        }

        public IDataTask<DataTaskYield<T>?> Yield()
        {
            IDataTask<DataTaskYield<T>?> wrapYieldTask;
            try
            {
                wrapYieldTask = _wrapped.Yield();
            }
            catch (Exception ex)
            {
                return _exceptionHandler(ex).Yield();
            }
            return wrapYieldTask
                .Select(wrapYield =>
                    wrapYield.MapRemaining(remaining =>
                        new TryCatchEnumerable<T>(remaining, _exceptionHandler)))
                .TryCatch(ex => _exceptionHandler(ex).Yield());
        }
    }
}
