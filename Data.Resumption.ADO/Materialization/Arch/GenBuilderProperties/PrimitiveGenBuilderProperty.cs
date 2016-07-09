using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime;

namespace Data.Resumption.ADO.Materialization.GenBuilderProperties
{
    /// <summary>
    /// Implements IGenBuilderProperty for a primitive field -- for example, one of type int, string, or Guid.
    /// </summary>
    internal class PrimitiveGenBuilderProperty : IGenBuilderProperty
    {
        private readonly string _fieldName;
        private readonly Type _fieldType;

        /// <summary>
        /// The field that stores the value for this property.
        /// </summary>
        private FieldBuilder _value;
        /// <summary>
        /// The boolean field that stores whether or not we've loaded the value for this property yet.
        /// </summary>
        private FieldBuilder _seen;

        public PrimitiveGenBuilderProperty(string fieldName, Type fieldType)
        {
            _fieldName = fieldName;
            _fieldType = fieldType;
        }

        public void InstallFields(TypeBuilder type)
        {
            _value = type.DefineField(_fieldName, _fieldType, FieldAttributes.Private);
            _seen = type.DefineField("__seen_" + _fieldName, typeof(bool), FieldAttributes.Private);
        }

        public void InstallProcessingLogic(GenInstanceMethodContext cxt)
        {
            var il = cxt.IL;
            var skip = il.DefineLabel();

            // First check if we can skip...
            il.Emit(OpCodes.Dup);
            il.Emit(OpCodes.Ldfld, _seen);
            il.Emit(OpCodes.Brtrue_S, skip);
            {
                // If not, attempt to load the value.
                il.Emit
            }
            // Here's where we skip to if we already had a value.
            il.MarkLabel(skip);
        }

        public void InstallPushValue(GenInstanceMethodContext cxt)
        {
            var il = cxt.IL;
            il.Emit(OpCodes.Ldloc, cxt.This);
            il.Emit(OpCodes.Ldfld, _value);
        }
    }
}
