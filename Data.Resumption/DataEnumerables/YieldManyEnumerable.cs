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
        private readonly List<T> _values;
        private readonly int _index;

        private YieldManyEnumerable(List<T> values, int index)
        {
            _values = values;
            _index = index;
        }

        public YieldManyEnumerable(IEnumerable<T> values) : this(values.ToList(), 0) { }

        public IDataTask<DataTaskYield<T>?> Yield()
        {
            if (_values.Count <= _index)
            {
                return DataTask.Return<DataTaskYield<T>?>(null);
            }
            return DataTask.Return<DataTaskYield<T>?>
                (new DataTaskYield<T>(_values[_index], new YieldManyEnumerable<T>(_values, _index + 1)));
        }
    }
}
