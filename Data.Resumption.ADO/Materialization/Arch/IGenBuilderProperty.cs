using System.Reflection.Emit;

namespace Data.Resumption.ADO.Materialization
{
    /// <summary>
    /// Describes a property for code generation of an IBuilder.
    /// </summary>
    internal interface IGenBuilderProperty
    {
        /// <summary>
        /// Add the fields this properties needs to keep track of to <paramref name="type"/>, which is
        /// going to be an IBuilder.
        /// </summary>
        /// <param name="type"></param>
        void InstallFields(TypeBuilder type);

        /// <summary>
        /// Add the logic to process this property to the IBuilder's <c>ProcessRow()</c> method.
        /// Assume that a "this" reference to the IBuilder is currently on top of the stack.
        /// Leave it there when done.
        /// </summary>
        /// <param name="cxt"></param>
        void InstallProcessingLogic(GenInstanceMethodContext cxt);

        /// <summary>
        /// Add logic to push the value of this property onto the stack within the IBuilder's <c>Materialize()</c>
        /// method.
        /// </summary>
        /// <param name="cxt"></param>
        void InstallPushValue(GenInstanceMethodContext cxt);
    }
}
