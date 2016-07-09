using System.Reflection.Emit;

namespace Data.Resumption.ADO.Materialization
{
    /// <summary>
    /// Describes a property for code generation of an IBuilder.
    /// </summary>
    internal interface IGenBuilderProperty
    {
        /// <summary>
        /// If true, this property has only one value for an instance of the target type,
        /// and that value will appear on the first row that has a value for *any* singular property.
        /// </summary>
        /// <remarks>
        /// This is assumed to be true for all properties that aren't lists, arrays, or other collection types.
        /// </remarks>
        bool Singular { get; }

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
        void InstallProcessingLogic(GenProcessRowContext cxt);

        /// <summary>
        /// Add logic to push the value of this property onto the stack within the IBuilder's <c>Materialize()</c>
        /// method.
        /// </summary>
        /// <param name="cxt"></param>
        void InstallPushValue(GenInstanceMethodContext cxt);
    }
}
