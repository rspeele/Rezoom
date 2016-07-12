using System;
using System.Reflection;
using System.Reflection.Emit;

namespace Data.Resumption.ADO.Materialization.GenBuilderProperties
{
    internal class SingleNavGenBuilderProperty : IGenBuilderProperty
    {
        private readonly string _fieldName;
        private readonly Type _entityType;
        private readonly Type _entityReaderType;
        private readonly string _keyFieldName;
        private readonly Type _keyType;

        // even though this entity is singular, we might need to process multiple rows to populate it
        // so to be on the safe side, we say we're not singular.
        // can we do better by figuring out whether the target type is going to be all singular?
        public bool Singular => false;

        private FieldBuilder _reader;
        private FieldBuilder _keyColumnIndex;

        public SingleNavGenBuilderProperty(string fieldName, Type entityType)
        {
            _fieldName = fieldName;
            _entityType = entityType;
            _entityReaderType = typeof(IRowReader<>).MakeGenericType(_entityType);
        }

        public void InstallFields(TypeBuilder type, ILGenerator constructor)
        {
            _reader = type.DefineField("_dr_single_" + _fieldName, _entityReaderType, FieldAttributes.Private);
            _keyColumnIndex = type.DefineField("_dr_kcol_" + _fieldName, typeof(int), FieldAttributes.Private);
        }

        public void InstallProcessingLogic(GenProcessColumnMapContext cxt)
        {
            throw new System.NotImplementedException();
        }

        public void InstallProcessingLogic(GenProcessRowContext cxt)
        {
            throw new System.NotImplementedException();
        }

        public void InstallPushValue(GenInstanceMethodContext cxt)
        {
            throw new System.NotImplementedException();
        }
    }
}