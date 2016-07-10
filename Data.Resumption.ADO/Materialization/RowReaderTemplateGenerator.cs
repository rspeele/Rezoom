using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Data.Resumption.ADO.Materialization.GenBuilders;

namespace Data.Resumption.ADO.Materialization
{
    internal static class RowReaderTemplateGenerator
    {
        private static void ImplementRowReader(TypeBuilder builder, Type targetType)
        {
            // TODO different builder type depending on the target
            var gen = new PropertyAssignmentGenBuilder(targetType);
            var consIL = builder.DefineDefaultConstructor(MethodAttributes.Public).GetILGenerator();
            consIL.Emit(OpCodes.Ldarg_0);
            builder.AddInterfaceImplementation(typeof(IRowReader<>).MakeGenericType(targetType));

            GenInstanceMethodContext toEntityContext;
            {
                var toEntity = builder.DefineMethod
                    (nameof(IRowReader<object>.ToEntity), MethodAttributes.Public);
                toEntity.SetParameters();
                toEntity.SetReturnType(targetType);
                var il = toEntity.GetILGenerator();
                var thisLocal = il.DeclareLocal(builder);
                il.Emit(OpCodes.Ldarg_0); // load this
                il.Emit(OpCodes.Stloc, thisLocal);
                toEntityContext = new GenInstanceMethodContext(il, thisLocal);
            }

            GenProcessColumnMapContext columnContext;
            {
                var processColumnMap = builder.DefineMethod
                    (nameof(IRowReader<object>.ProcessColumnMap), MethodAttributes.Public);
                processColumnMap.SetParameters(typeof(ColumnMap));
                var il = processColumnMap.GetILGenerator();
                var thisLocal = il.DeclareLocal(builder);
                il.Emit(OpCodes.Ldarg_0); // load this
                il.Emit(OpCodes.Dup);
                il.Emit(OpCodes.Stloc, thisLocal);
                columnContext = new GenProcessColumnMapContext(il, thisLocal);
                il.Emit(OpCodes.Ldarg_1); // load column map
                il.Emit(OpCodes.Stloc, columnContext.ColumnMap);
            }

            GenProcessRowContext rowContext;
            {
                var processRow = builder.DefineMethod
                    (nameof(IRowReader<object>.ProcessRow), MethodAttributes.Public);
                processRow.SetParameters(typeof(object[]));
                var il = processRow.GetILGenerator();
                var thisLocal = il.DeclareLocal(builder);
                il.Emit(OpCodes.Ldarg_0); // load this
                il.Emit(OpCodes.Dup);
                il.Emit(OpCodes.Stloc, thisLocal);
                rowContext = new GenProcessRowContext(il, thisLocal);
                il.Emit(OpCodes.Ldarg_1); // load row
                il.Emit(OpCodes.Stloc, rowContext.Row);
            }

            foreach (var prop in gen.Properties)
            {
                prop.InstallFields(builder, consIL);
                prop.InstallProcessingLogic(columnContext);
                prop.InstallPushValue(toEntityContext);
            }
            foreach (var prop in gen.Properties.OrderByDescending(p => p.Singular))
            {
                if (!prop.Singular) rowContext.IL.MarkLabel(rowContext.SkipSingularProperties);
                prop.InstallProcessingLogic(rowContext);
            }
            gen.InstallConstructor(toEntityContext.IL);
            consIL.Emit(OpCodes.Pop); // pop this
            columnContext.IL.Emit(OpCodes.Pop); // pop this
            rowContext.IL.Emit(OpCodes.Pop); // pop this
            toEntityContext.IL.Emit(OpCodes.Ret); // return constructed object
        }

        private static void ImplementRowReaderTemplate(TypeBuilder builder, Type targetType, Type readerType)
        {
            var cons = readerType.GetConstructor(Type.EmptyTypes);
            if (cons == null) throw new Exception("No default constructor for reader");
            builder.DefineDefaultConstructor(MethodAttributes.Public);
            builder.AddInterfaceImplementation(typeof(IRowReaderTemplate<>).MakeGenericType(targetType));
            var creator = builder.DefineMethod(nameof(IRowReaderTemplate<object>.CreateReader), MethodAttributes.Public);
            creator.SetParameters();
            creator.SetReturnType(typeof(IRowReader<>).MakeGenericType(targetType));
            var il = creator.GetILGenerator();
            il.Emit(OpCodes.Newobj, cons);
            il.Emit(OpCodes.Ret);
        }

        public static object GenerateReaderTemplate(Type targetType)
        {
            // create a dynamic assembly to house our dynamic type
            var assembly = new AssemblyName($"Readers.{targetType.FullName}");
            var appDomain = System.Threading.Thread.GetDomain();
            var assemblyBuilder = appDomain.DefineDynamicAssembly(assembly, AssemblyBuilderAccess.Run);
            var moduleBuilder = assemblyBuilder.DefineDynamicModule(assembly.Name);

            // create the dynamic IRowReader<T> type
            var reader = moduleBuilder.DefineType
                ( $"{targetType.Name}Reader"
                , TypeAttributes.Public | TypeAttributes.AutoClass | TypeAttributes.AnsiClass
                , typeof(object)
                );
            ImplementRowReader(reader, targetType);

            var template = moduleBuilder.DefineType
                ($"{targetType.Name}ReaderTemplate"
                , TypeAttributes.Public | TypeAttributes.AutoClass | TypeAttributes.AnsiClass
                , typeof(object)
                );
            ImplementRowReaderTemplate(template, targetType, reader.CreateType());
            var templateType = template.CreateType();
            return Activator.CreateInstance(templateType);
        }
    }
}