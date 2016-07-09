namespace Data.Resumption.ADO.Materialization
{
    public interface IColumnMap
    {
        int ColumnIndex(string propertyName);
        IColumnMap SubMap(string propertyName);
    }
}
