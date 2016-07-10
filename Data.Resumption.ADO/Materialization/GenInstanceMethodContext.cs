using System.Collections.Generic;
using System.Reflection.Emit;
using Data.Resumption.ADO.Materialization;

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

    internal class GenProcessColumnMapContext : GenInstanceMethodContext
    {
        public GenProcessColumnMapContext(ILGenerator il, LocalBuilder @this) : base(il, @this)
        {
            ColumnMap = il.DeclareLocal(typeof(IColumnMap));
        }
        public LocalBuilder ColumnMap { get; }
    }

    internal class GenProcessRowContext : GenInstanceMethodContext
    {
        public GenProcessRowContext(ILGenerator il, LocalBuilder @this, Label skipSingularProperties) : base(il, @this)
        {
            SkipSingularProperties = skipSingularProperties;
            Row = il.DeclareLocal(typeof(object[]));
        }
        public LocalBuilder Row { get; }
        /// <summary>
        /// Label to skip to after all "singular" properties.
        /// </summary>
        public Label SkipSingularProperties { get; }
    }
}
