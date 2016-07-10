using System;
using Data.Resumption.ADO.Materialization.GenBuilderProperties;

namespace Data.Resumption.ADO.Materialization
{
    internal static class GenBuilderProperty
    {
        public static IGenBuilderProperty GetProperty(string name, Type propertyType)
        {
            // TODO support non-primitive property types
            return new PrimitiveGenBuilderProperty(name, propertyType);
        }
    }
}
