using System;

namespace Data.Resumption
{
    /// <summary>
    /// Represents the result of iterating a <see cref="IDataEnumerator{T}"/>.
    /// May either be the end of the sequence (if <see cref="HasValue"/> is false) or
    /// an element yielded from the sequence (if <see cref="HasValue"/> is true).
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public struct DataTaskYield<T>
    {
        private readonly T _value;
        public DataTaskYield(T value)
        {
            _value = value;
            HasValue = true;
        }

        /// <summary>
        /// Whether this represents an elemented yielded from the sequence (true)
        /// or the end of the sequence (false).
        /// </summary>
        public bool HasValue { get; }
        /// <summary>
        /// The value yielded from this iteration.
        /// </summary>
        public T Value
        {
            get
            {
                if (!HasValue) throw new InvalidOperationException("Cannot obtain a value from the end of a sequence");
                return _value;
            }
        }
    }
}