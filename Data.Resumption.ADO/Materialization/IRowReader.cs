namespace Data.Resumption.ADO.Materialization
{
    internal interface IRowReaderTemplate<out T>
    {
        IRowReader<T> CreateReader();
    }
    internal interface IRowReader<out T>
    {
        void ProcessColumnMap(IColumnMap map);
        void ProcessRow(object[] row);
        T ToEntity();
    }

    internal static class RowReaderTemplate<T>
    {
        private static IRowReaderTemplate<T> GetReader()
        {
            DelayedRowReaderTemplate<T>.Commit();
            return DelayedRowReaderTemplate<T>.Template;
        }
        public static readonly IRowReaderTemplate<T> Reader = GetReader();
    }
    internal static class DelayedRowReaderTemplate<T>
    {
        private class DelayReaderTemplate : IRowReaderTemplate<T>
        {
            public IRowReaderTemplate<T> Instance;
            public IRowReader<T> CreateReader() => Instance.CreateReader();
        }
        public static IRowReaderTemplate<T> Template = new DelayReaderTemplate();

        public static void Commit()
        {
            var delayed = (DelayReaderTemplate)Template;
            Template = delayed.Instance = (IRowReaderTemplate<T>)
                RowReaderTemplateGenerator.GenerateReaderTemplate(typeof(T));
        }
    }
}