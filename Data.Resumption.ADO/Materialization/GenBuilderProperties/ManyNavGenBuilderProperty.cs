using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace Data.Resumption.ADO.Materialization.GenBuilderProperties
{
    internal class ManyNavGenBuilderProperty : IGenBuilderProperty
    {
        private readonly string _fieldName;

        private readonly Type _entityType;
        private readonly Type _entityReaderType;
        private readonly Type _keyType;
        private readonly string _keyFieldName;
        private readonly Type _dictionaryType;

        public bool Singular => false;

        private FieldBuilder _dict;
        private FieldBuilder _columnMap;
        private FieldBuilder _keyColumnIndex;

        public ManyNavGenBuilderProperty(string fieldName, Type entityType)
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
            _columnMap = type.DefineField("_dr_cmap_" + _fieldName, typeof(ColumnMap), FieldAttributes.Private);
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
            var skip = il.DefineLabel();
            il.Emit(OpCodes.Dup); // this, this
            il.Emit(OpCodes.Dup); // this, this, this
            // Get submap for this nav property
            il.Emit(OpCodes.Dup); // this, this, this, this
            il.Emit(OpCodes.Ldloc, cxt.ColumnMap); // this, this, this, this, colmap
            il.Emit(OpCodes.Dup);
            il.Emit(OpCodes.Brfalse_S, skip);
            il.Emit(OpCodes.Ldstr, _fieldName); // this, this, this, this colmap, fieldname
            il.Emit(OpCodes.Callvirt, typeof(ColumnMap).GetMethod(nameof(ColumnMap.SubMap)));
            // this, this, this, this, submap
            il.Emit(OpCodes.Dup);
            il.Emit(OpCodes.Brfalse_S, skip);
            // Set column map field to submap
            il.Emit(OpCodes.Stfld, _columnMap); // this, this, this
            il.Emit(OpCodes.Ldfld, _columnMap); // this, this, submap
            // Get key column index from submap
            il.Emit(OpCodes.Ldstr, _keyFieldName); // this, this, submap, keyfield
            il.Emit(OpCodes.Callvirt, typeof(ColumnMap).GetMethod(nameof(ColumnMap.ColumnIndex)));
            // this, this, keyindex
            il.Emit(OpCodes.Stfld, _keyColumnIndex);
            var done = il.DefineLabel();
            il.Emit(OpCodes.Br_S, done);
            il.MarkLabel(skip);
            // this, this, this, this, submap
            il.Emit(OpCodes.Pop);
            il.Emit(OpCodes.Pop);
            il.Emit(OpCodes.Pop);
            il.Emit(OpCodes.Pop);
            il.MarkLabel(done);
        }

        public void InstallProcessingLogic(GenProcessRowContext cxt)
        {
            var il = cxt.IL;
            var skip = il.DefineLabel();
            var subProcess = il.DefineLabel();
            var keyRaw = il.DeclareLocal(typeof(object));
            var key = il.DeclareLocal(_keyType);
            var entReader = il.DeclareLocal(_entityReaderType);
            il.Emit(OpCodes.Dup); // this, this
            il.Emit(OpCodes.Ldfld, _columnMap); // this, cmap
            il.Emit(OpCodes.Brfalse, skip); // skip if we have no column map (for recursive case)

            // get the key value from the row
            il.Emit(OpCodes.Ldloc, cxt.Row); // this, row
            il.Emit(OpCodes.Ldloc, cxt.This); // this, row, this
            il.Emit(OpCodes.Ldfld, _keyColumnIndex); // this, row, index
            il.Emit(OpCodes.Ldelem_Ref); // this, rval
            // store it in a local
            il.Emit(OpCodes.Dup); // this, rval, rval
            il.Emit(OpCodes.Stloc, keyRaw); // this, rval
            // if our id is null, bail
            il.Emit(OpCodes.Brfalse, skip);
            {
                // stack clean (this at top)
                il.Emit(OpCodes.Dup); // this, this
                il.Emit(OpCodes.Ldfld, _dict); // this, dict
                il.Emit(OpCodes.Ldloc, keyRaw); // this, dict, rval
                il.Emit(OpCodes.Call, PrimitiveConverter.ToType(_keyType)); // this, dict, key
                il.Emit(OpCodes.Dup); // this, dict, key, key
                il.Emit(OpCodes.Stloc, key); // this, dict, key
                il.Emit(OpCodes.Ldloca, entReader); // this, dict, key, &reader
                il.Emit(OpCodes.Call, _dictionaryType.GetMethod(nameof(Dictionary<object, object>.TryGetValue)));
                // this, gotv
                // if we've got one, skip to sub-processing the row
                il.Emit(OpCodes.Brtrue, subProcess);
                {
                    // otherwise, make one
                    // stack clean (this at top)
                    var templateStaticType = typeof(RowReaderTemplate<>).MakeGenericType(_entityType);
                    var templateType = typeof(IRowReaderTemplate<>).MakeGenericType(_entityType);
                    il.Emit(OpCodes.Ldsfld, templateStaticType.GetField
                        (nameof(RowReaderTemplate<object>.Template)));
                    // this, template
                    il.Emit(OpCodes.Callvirt, templateType.GetMethod
                        (nameof(IRowReaderTemplate<object>.CreateReader)));
                    // this, newreader
                    il.Emit(OpCodes.Dup);
                    // this, newreader, newreader
                    il.Emit(OpCodes.Stloc, entReader);
                    // this, newreader

                    // process column map
                    il.Emit(OpCodes.Ldloc, cxt.This); // this, newreader, this
                    il.Emit(OpCodes.Ldfld, _columnMap); // this, newreader, columnmap
                    il.Emit(OpCodes.Callvirt, _entityReaderType.GetMethod
                        (nameof(IRowReader<object>.ProcessColumnMap)));

                    // save in dictionary
                    // stack clean (this at top)
                    il.Emit(OpCodes.Dup); // this, this
                    il.Emit(OpCodes.Ldfld, _dict); // this, dict
                    il.Emit(OpCodes.Ldloc, key); // this, dict, key
                    il.Emit(OpCodes.Ldloc, entReader); // this, dict, key, reader
                    il.Emit(OpCodes.Call, _dictionaryType.GetMethod
                        (nameof(Dictionary<object, object>.Add)));
                    // stack clean (this at top)
                }
                il.MarkLabel(subProcess);
                // have the entity reader process the row
                il.Emit(OpCodes.Ldloc, entReader); // this, reader
                il.Emit(OpCodes.Ldloc, cxt.Row); // this, reader, row
                il.Emit(OpCodes.Callvirt, _entityReaderType.GetMethod(nameof(IRowReader<object>.ProcessRow)));
                // this
            }
            il.MarkLabel(skip);
        }

        public void InstallPushValue(GenInstanceMethodContext cxt)
        {
            var il = cxt.IL;

            il.Emit(OpCodes.Ldloc, cxt.This); // this
            il.Emit(OpCodes.Ldfld, _dict); // dict
            var values = _dictionaryType.GetProperty(nameof(Dictionary<object, object>.Values)).GetGetMethod();
            il.Emit(OpCodes.Callvirt, values); // values

            var converter = typeof(NavConverter<>).MakeGenericType(_entityType);
            var toArray = converter.GetMethod(nameof(NavConverter<object>.ToArray));
            il.Emit(OpCodes.Call, toArray); // arr
        }
    }
}
