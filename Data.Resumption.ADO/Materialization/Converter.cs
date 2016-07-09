using System;
using System.Reflection;

namespace Data.Resumption.ADO.Materialization
{
    internal static class Converter
    {
        public static MethodInfo ToType(Type targetType)
        {
            var convert = typeof(Convert);
            if (targetType == typeof(int))
            {
                return convert.GetMethod(nameof(Convert.ToInt32), new[] { typeof(object) });
            }
            throw new NotImplementedException();
        }
    }
}