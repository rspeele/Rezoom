using System;

namespace Data.Resumption.DataEnumerables
{
    /// <summary>
    /// Represents monadic binding of data enumerables (like SelectMany).
    /// </summary>
    /// <typeparam name="TIn"></typeparam>
    /// <typeparam name="TOut"></typeparam>
    internal class BindEnumerable<TIn, TOut> : IDataEnumerable<TOut>
    {
        private readonly IDataEnumerable<TIn> _inputs;
        private readonly Func<TIn, IDataEnumerable<TOut>> _selector;

        public BindEnumerable(IDataEnumerable<TIn> inputs, Func<TIn, IDataEnumerable<TOut>> selector)
        {
            _inputs = inputs;
            _selector = selector;
        }

        private class BindEnumerator : IDataEnumerator<TOut>
        {
            private readonly IDataEnumerator<TIn> _inputs;
            private readonly Func<TIn, IDataEnumerable<TOut>> _selector;
            private IDataEnumerator<TOut> _currentSubSelect;

            public BindEnumerator(IDataEnumerator<TIn> inputs, Func<TIn, IDataEnumerable<TOut>> selector)
            {
                _inputs = inputs;
                _selector = selector;
            }
            
            private DataTask<DataTaskYield<TOut>> MoveNextInside(IDataEnumerator<TOut> subSelect)
                => subSelect.MoveNext()
                    .Bind(yielded =>
                    {
                        if (yielded.HasValue) return DataTask.Return(yielded);
                        subSelect.Dispose();
                        _currentSubSelect = null;
                        return MoveNextOutside();
                    });

            private DataTask<DataTaskYield<TOut>> MoveNextOutside()
                => _inputs.MoveNext()
                    .Bind(yielded =>
                    {
                        if (!yielded.HasValue)
                        {
                            return DataTask.Return(new DataTaskYield<TOut>());
                        }
                        var subSelection = _selector(yielded.Value);
                        _currentSubSelect = subSelection.GetEnumerator();
                        return MoveNextInside(_currentSubSelect);
                    });

            public DataTask<DataTaskYield<TOut>> MoveNext()
                => _currentSubSelect == null
                    ? MoveNextOutside()
                    : MoveNextInside(_currentSubSelect);

            public void Dispose()
            {
                try
                {
                    _currentSubSelect?.Dispose();
                }
                finally 
                {
                    _inputs.Dispose();
                }
            }
        }

        public IDataEnumerator<TOut> GetEnumerator()
            => new BindEnumerator(_inputs.GetEnumerator(), _selector);
    }
}
