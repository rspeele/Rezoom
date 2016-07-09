using System.Collections.Generic;
using System.Reflection.Emit;

namespace Data.Resumption.ADO.Materialization
{
    internal class GenInstanceMethodContext
    {
        public GenInstanceMethodContext(ILGenerator il, LocalBuilder @this)
        {
            IL = il;
            This = @this;
        }
        public LocalBuilder This { get; }
        public ILGenerator IL { get; }
    }

    internal class GenProcessRowContext : GenInstanceMethodContext
    {
        public GenProcessRowContext(ILGenerator il, LocalBuilder @this, Label skipSingularProperties) : base(il, @this)
        {
            SkipSingularProperties = skipSingularProperties;
            ColumnMap = il.DeclareLocal(typeof(IColumnMap));
            Row = il.DeclareLocal(typeof(object[]));
        }
        public LocalBuilder ColumnMap { get; }
        public LocalBuilder Row { get; }
        /// <summary>
        /// Label to skip to after all "singular" properties.
        /// </summary>
        public Label SkipSingularProperties { get; }
    }
}
