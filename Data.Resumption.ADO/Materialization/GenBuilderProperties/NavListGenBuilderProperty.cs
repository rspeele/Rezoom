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
        private readonly Type _entityReaderType;
        private readonly Type _keyType;
        private readonly string _keyFieldName;
        private readonly Type _dictionaryType;

        public bool Singular => false;

        private FieldBuilder _dict;
        private FieldBuilder _keyColumnIndex;

        public NavListGenBuilderProperty(string fieldName, Type entityType)
        {
            _fieldName = fieldName;
            _entityType = entityType;
            _entityReaderType = typeof(IRowReader<>).MakeGenericType(entityType);
            _keyType = typeof(int); // TODO figure out from entity type
            _keyFieldName = "id"; // TODO figure out from entity type
            _dictionaryType = typeof(Dictionary<,>).MakeGenericType(_keyType, _entityReaderType);
        }

        public void InstallFields(TypeBuilder type, ILGenerator constructor)
        {
            _keyColumnIndex = type.DefineField("_dr_col_" + _fieldName, typeof(int), FieldAttributes.Private);
            _dict = type.DefineField("_dr_dict_" + _fieldName, _dictionaryType, FieldAttributes.Private);
            var cons = _dictionaryType.GetConstructor(Type.EmptyTypes);
            if (cons == null) throw new Exception("Unexpected lack of default constructor on dictionary type");

            constructor.Emit(OpCodes.Dup); // dup "this"
            constructor.Emit(OpCodes.Newobj, cons); // new dictionary
            constructor.Emit(OpCodes.Stfld, _dict); // assign field
        }

        public void InstallProcessingLogic(GenProcessColumnMapContext cxt)
        {
            var il = cxt.IL;
            il.Emit(OpCodes.Dup); // dup this
            // Get submap for this nav property
            il.Emit(OpCodes.Dup);
            il.Emit(OpCodes.Ldloc, cxt.ColumnMap);
            il.Emit(OpCodes.Ldstr, _fieldName);
            il.Emit(OpCodes.Callvirt, typeof(IColumnMap).GetMethod(nameof(IColumnMap.SubMap)));
            // Get column index from submap
            il.Emit(OpCodes.Ldstr, _keyFieldName);
            il.Emit(OpCodes.Callvirt, typeof(IColumnMap).GetMethod(nameof(IColumnMap.ColumnIndex)));
            il.Emit(OpCodes.Stfld, _keyColumnIndex);
        }

        public void InstallProcessingLogic(GenProcessRowContext cxt)
        {
            var il = cxt.IL;
            var skip = il.DefineLabel();
            var subProcess = il.DefineLabel();
            var key = il.DeclareLocal(_keyType);
            var entReader = il.DeclareLocal(_entityReaderType);
            // get the key value from the row
            il.Emit(OpCodes.Ldloc, cxt.Row);
            il.Emit(OpCodes.Ldloc, cxt.This);
            il.Emit(OpCodes.Ldfld, _keyColumnIndex);
            il.Emit(OpCodes.Ldelem_Ref);
            // store it in a local
            il.Emit(OpCodes.Dup);
            il.Emit(OpCodes.Stloc, key);
            // if our id is null, bail
            il.Emit(OpCodes.Brfalse, skip);
            {
                // stack clean (this at top)
                il.Emit(OpCodes.Dup); // dup this
                il.Emit(OpCodes.Ldfld, _dict);
                il.Emit(OpCodes.Ldloc, key);
                il.Emit(OpCodes.Ldloca, entReader);
                il.Emit(OpCodes.Call, _dictionaryType.GetMethod(nameof(Dictionary<object, object>.TryGetValue)));
                // if we've got one, skip to sub-processing the row
                il.Emit(OpCodes.Brtrue, subProcess);
                {
                    // otherwise, make one
                    // stack clean (this at top)
                    var templateStaticType = typeof(DelayedRowReaderTemplate<>).MakeGenericType(_entityType);
                    var templateType = typeof(IRowReaderTemplate<>).MakeGenericType(_entityType);
                    il.Emit(OpCodes.Ldsfld, templateStaticType.GetField(nameof(DelayedRowReaderTemplate<object>.Template)));
                    il.Emit(OpCodes.Callvirt, templateType.GetMethod(nameof(IRowReaderTemplate<object>.CreateReader)));
                    il.Emit(OpCodes.Stloc, entReader);
                    // save in dictionary
                    // stack clean (this at top)
                    il.Emit(OpCodes.Dup);
                    il.Emit(OpCodes.Ldfld, _dict);
                    il.Emit(OpCodes.Ldloc, key);
                    il.Emit(OpCodes.Ldloc, entReader);
                    il.Emit(OpCodes.Call, _dictionaryType.GetMethod(nameof(Dictionary<object, object>.Add)));
                    // stack clean (this at top)
                }
                il.MarkLabel(subProcess);
                // have the entity reader process the row
                il.Emit(OpCodes.Ldloc, entReader);
                il.Emit(OpCodes.Ldloc, cxt.Row);
                il.Emit(OpCodes.Callvirt, _entityReaderType.GetMethod(nameof(IRowReader<object>.ProcessRow)));
            }
            il.MarkLabel(skip);
        }

        public void InstallPushValue(GenInstanceMethodContext cxt)
        {
            throw new System.NotImplementedException();
        }
    }
}
