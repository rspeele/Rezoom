using System.Collections.Generic;
using System.Linq;

namespace Data.Resumption.DataEnumerables
{
    /// <summary>
    /// Represents the yielding of a multiple values from a list within a data enumerable.
    /// </summary>
    /// <remarks>
    /// This is slightly more efficient than chaining together individual yields.
    /// </remarks>
    /// <typeparam name="T"></typeparam>
    internal class YieldManyEnumerable<T> : IDataEnumerable<T>
    {
        private readonly IEnumerable<T> _values;

        public YieldManyEnumerable(IEnumerable<T> values)
        {
            _values = values;
        }

        private class YieldManyEnumerator : IDataEnumerator<T>
        {
            private readonly IEnumerator<T> _enumerator;

            public YieldManyEnumerator(IEnumerator<T> enumerator)
            {
                _enumerator = enumerator;
            }

            public DataTask<DataTaskYield<T>> MoveNext()
                => DataTask.Return(_enumerator.MoveNext()
                    ? new DataTaskYield<T>(_enumerator.Current)
                    : new DataTaskYield<T>());

            public void Dispose() => _enumerator.Dispose();
        }

        public IDataEnumerator<T> GetEnumerator() => new YieldManyEnumerator(_values.GetEnumerator());
    }
}
