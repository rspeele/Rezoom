namespace Data.Resumption.ADO.Materialization
{
    internal interface IColumnMap
    {
        int ColumnIndex(string propertyName);
        IColumnMap SubMap(string propertyName);
    }
}
