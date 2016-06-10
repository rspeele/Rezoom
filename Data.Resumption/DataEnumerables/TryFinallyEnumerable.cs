using System;

namespace Data.Resumption.DataEnumerables
{
    /// <summary>
    /// Represents a data enumerable wrapped with a `finally` block.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal class TryFinallyEnumerable<T> : IDataEnumerable<T>
    {
        private readonly IDataEnumerable<T> _wrapped;
        private readonly Action _onExit;

        public TryFinallyEnumerable(IDataEnumerable<T> wrapped, Action onExit)
        {
            _wrapped = wrapped;
            _onExit = onExit;
        }

        public IDataTask<DataTaskYield<T>?> Yield()
        {
            IDataTask<DataTaskYield<T>?> wrapYieldTask;
            try
            {
                wrapYieldTask = _wrapped.Yield();
            }
            catch
            {
                _onExit();
                throw;
            }
            return wrapYieldTask
                .Select(wrapYield =>
                {
                    // if we get to the end of the sequence safely, we still need to run our
                    // finally block.
                    if (wrapYield == null)
                    {
                        _onExit();
                        return null;
                    }
                    // otherwise we just keep chaining the finally forward
                    return wrapYield.MapRemaining
                        (remaining =>
                            new TryFinallyEnumerable<T>(remaining, _onExit));
                })
                .TryCatch(ex => { _onExit(); throw ex; });
        }
    }
}
