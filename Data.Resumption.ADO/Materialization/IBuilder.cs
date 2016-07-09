using System.Collections.Generic;

namespace Data.Resumption.ADO.Materialization
{
    /// <summary>
    /// A work-in-progress type T, which can be materialized once all relevant rows have been processed.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal interface IBuilder<out T>
    {
        void ProcessRow(IReadOnlyList<object> row, int index, int end);
        T Materialize();
    }
}