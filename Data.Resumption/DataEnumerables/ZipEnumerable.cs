using System;

namespace Data.Resumption.DataEnumerables
{
    /// <summary>
    /// Represents a pair of data enumerable zipped together with an asynchronous function
    /// to combine elements from the sequences.
    /// </summary>
    /// <typeparam name="TLeft"></typeparam>
    /// <typeparam name="TRight"></typeparam>
    /// <typeparam name="TOut"></typeparam>
    internal class ZipEnumerable<TLeft, TRight, TOut> : IDataEnumerable<TOut>
    {
        private readonly IDataEnumerable<TLeft> _left;
        private readonly IDataEnumerable<TRight> _right;
        private readonly Func<TLeft, TRight, IDataTask<TOut>> _zipper;

        public ZipEnumerable
            ( IDataEnumerable<TLeft> left
            , IDataEnumerable<TRight> right
            , Func<TLeft, TRight, IDataTask<TOut>> zipper
            )
        {
            _left = left;
            _right = right;
            _zipper = zipper;
        }

        private class ZipEnumerator : IDataEnumerator<TOut>
        {
            private readonly IDataEnumerator<TLeft> _left;
            private readonly IDataEnumerator<TRight> _right;
            private readonly Func<TLeft, TRight, IDataTask<TOut>> _zipper;

            public ZipEnumerator
                ( IDataEnumerator<TLeft> left
                , IDataEnumerator<TRight> right
                , Func<TLeft, TRight, IDataTask<TOut>> zipper
                )
            {
                _left = left;
                _right = right;
                _zipper = zipper;
            }

            public IDataTask<DataTaskYield<TOut>> MoveNext()
                => _left.MoveNext()
                    .Zip(_right.MoveNext(), (ly, ry) => new { ly, ry })
                    .Bind(pair =>
                        pair.ly.HasValue && pair.ry.HasValue
                            ? _zipper(pair.ly.Value, pair.ry.Value).Select(o => new DataTaskYield<TOut>(o))
                            : DataTask.Return(new DataTaskYield<TOut>()));

            public void Dispose()
            {
                try
                {
                    _left.Dispose();
                }
                finally
                {
                    _right.Dispose();
                }
            }
        }

        public IDataEnumerator<TOut> GetEnumerator()
        {
            var leftEnumerator = _left.GetEnumerator();
            IDataEnumerator<TRight> rightEnumerator;
            try
            {
                rightEnumerator = _right.GetEnumerator();
            }
            catch
            {
                leftEnumerator.Dispose();
                throw;
            }
            return new ZipEnumerator(leftEnumerator, rightEnumerator, _zipper);
        }
    }
}
