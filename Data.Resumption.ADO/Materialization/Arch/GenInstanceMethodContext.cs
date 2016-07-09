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
}
