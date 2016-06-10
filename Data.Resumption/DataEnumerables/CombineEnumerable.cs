using System;

namespace Data.Resumption.DataEnumerables
{
    /// <summary>
    /// Represents the concatenation of two data enumerables.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal class CombineEnumerable<T> : IDataEnumerable<T>
    {
        private readonly IDataEnumerable<T> _first;
        private readonly Func<IDataEnumerable<T>> _second;

        public CombineEnumerable(IDataEnumerable<T> first, Func<IDataEnumerable<T>> second)
        {
            _first = first;
            _second = second;
        }

        public IDataTask<DataTaskYield<T>?> Yield()
            => _first.Yield().SelectMany(firstYield =>
                firstYield.HasValue
                    ? DataTask.Return<DataTaskYield<T>?>(new DataTaskYield<T>
                        ( firstYield.Value.Value
                        , new CombineEnumerable<T>(firstYield.Value.Remaining, _second)
                        ))
                    : _second().Yield());
    }
}
