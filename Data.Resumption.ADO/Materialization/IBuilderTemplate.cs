using System.Collections.Generic;

namespace Data.Resumption.ADO.Materialization
{
    internal interface IBuilderTemplate<out T>
    {
        /// <summary>
        /// Get a builder object that can be used to process rows of data, given the arrangement of column names.
        /// </summary>
        /// <param name="header"></param>
        /// <returns></returns>
        IBuilder<T> CreateBuilder(IReadOnlyList<string> header);
    }
}
