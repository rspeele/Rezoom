namespace Data.Resumption
{
    public struct SingleOrMany<T>
    {
        public SingleOrMany(T singleton)
        {
            Single = singleton;
            Many = null;
        }
        public SingleOrMany(T[] many)
        {
            Single = default(T);
            Many = many;
        }
        public T Single { get; }
        public T[] Many { get; }
    }
}