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

        private class TryCatchEnumerator : IDataEnumerator<T>
        {
            private IDataEnumerator<T> _wrapped;
            private Func<Exception, IDataEnumerable<T>> _exceptionHandler;

            public TryCatchEnumerator
                (IDataEnumerator<T> wrapped, Func<Exception, IDataEnumerable<T>> exceptionHandler)
            {
                _wrapped = wrapped;
                _exceptionHandler = exceptionHandler;
            }

            private IDataTask<DataTaskYield<T>> HandleException(Exception ex)
            {
                _wrapped = _exceptionHandler(ex).GetEnumerator();
                _exceptionHandler = null;
                return MoveNext();
            }

            public IDataTask<DataTaskYield<T>> MoveNext()
            {
                if (_exceptionHandler == null) return _wrapped.MoveNext();
                try
                {
                    return _wrapped.MoveNext().TryCatch(HandleException);
                }
                catch (Exception ex)
                {
                    return HandleException(ex);
                }
            }

            public void Dispose() => _wrapped.Dispose();
        }

        public IDataEnumerator<T> GetEnumerator()
            => new TryCatchEnumerator(_wrapped.GetEnumerator(), _exceptionHandler);
    }
}
