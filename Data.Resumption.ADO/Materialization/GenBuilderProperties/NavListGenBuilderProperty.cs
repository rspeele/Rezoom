using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace Data.Resumption.ADO.Materialization.GenBuilderProperties
{
    internal class NavListGenBuilderProperty : IGenBuilderProperty
    {
        private readonly string _fieldName;

        private readonly Type _entityType;
        private readonly Type _keyType;
        private readonly Type _dictionaryType;

        public bool Singular => false;

        private FieldBuilder _dict;

        public NavListGenBuilderProperty(string fieldName, Type entityType)
        {
            _fieldName = fieldName;
            _entityType = entityType;
            _keyType = typeof(int); // TODO figure out from field type
            _dictionaryType = typeof(Dictionary<,>).MakeGenericType(_keyType, _entityType);
        }

        public void InstallFields(TypeBuilder type, ILGenerator constructor)
        {
            _dict = type.DefineField("_dr_dict_" + _fieldName, _dictionaryType, FieldAttributes.Private);
            var cons = _dictionaryType.GetConstructor(Type.EmptyTypes);
            if (cons == null) throw new Exception("Unexpected lack of default constructor on dictionary type");

            constructor.Emit(OpCodes.Dup); // dup "this"
            constructor.Emit(OpCodes.Newobj, cons); // new dictionary
            constructor.Emit(OpCodes.Stfld, _dict); // assign field
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
