namespace Data.Resumption.ADO.Materialization
{
    public interface IRowReaderTemplate<out T>
    {
        IRowReader<T> CreateReader();
    }
    public interface IRowReader<out T>
    {
        void ProcessColumnMap(ColumnMap map);
        void ProcessRow(object[] row);
        T ToEntity();
    }

    public static class RowReaderTemplate<T>
    {
        private static IRowReaderTemplate<T> GetReader() => DelayedRowReaderTemplate<T>.Template;
        public static readonly IRowReaderTemplate<T> Template = GetReader();
    }
    public static class DelayedRowReaderTemplate<T>
    {
        private class DelayReaderTemplate : IRowReaderTemplate<T>
        {
            public IRowReaderTemplate<T> Instance;
            public IRowReader<T> CreateReader() => Instance.CreateReader();
        }
        public static IRowReaderTemplate<T> Template = new DelayReaderTemplate();

        static DelayedRowReaderTemplate()
        {
            var delayed = (DelayReaderTemplate)Template;
            Template = delayed.Instance = (IRowReaderTemplate<T>)
                RowReaderTemplateGenerator.GenerateReaderTemplate(typeof(T));
        }
    }
}