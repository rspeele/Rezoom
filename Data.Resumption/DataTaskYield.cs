using System;

namespace Data.Resumption
{
    public struct DataTaskYield<T>
    {
        private readonly T _value;
        public DataTaskYield(T value)
        {
            _value = value;
            HasValue = true;
        }
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