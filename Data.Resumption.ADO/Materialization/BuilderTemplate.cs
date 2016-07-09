using System;

namespace Data.Resumption.ADO.Materialization
{
    internal static class BuilderTemplate
    {
        public static object CreateInstance(Type targetType, Type builderTemplateType)
        {
            // TODO: dynamically generate a type that implements IBuilderTemplate<T>.
            throw new NotImplementedException();
        }
    }
    internal static class BuilderTemplate<T>
    {
        public static readonly IBuilderTemplate<T> Instance =
            (IBuilderTemplate<T>)BuilderTemplate.CreateInstance(typeof(T), typeof(IBuilderTemplate<T>));
    }
}