using System.Collections.Generic;

namespace Data.Resumption.ADO
{
    public class CommandResponse
    {
        public CommandResponse
            (int rowsAffected, IReadOnlyList<string> columnNames, IReadOnlyList<IReadOnlyList<object>> rows)
        {
            RowsAffected = rowsAffected;
            ColumnNames = columnNames;
            Rows = rows;
        }

        public int RowsAffected { get; }
        public IReadOnlyList<string> ColumnNames { get; }
        public IReadOnlyList<IReadOnlyList<object>> Rows { get; }
    }
}