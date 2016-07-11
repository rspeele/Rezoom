using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Data.Resumption.ADO.Materialization
{
    public static class PrimitiveConverter
    {
        public static string ToString(object obj) => obj?.ToString();

        public static sbyte ToInt8(object obj) => Convert.ToSByte(obj);
        public static sbyte? ToNullableInt8(object obj) => obj == null ? (sbyte?)null : Convert.ToSByte(obj);
        public static short ToInt16(object obj) => Convert.ToInt16(obj);
        public static short? ToNullableInt16(object obj) => obj == null ? (short?)null : Convert.ToInt16(obj);
        public static int ToInt32(object obj) => Convert.ToInt32(obj);
        public static int? ToNullableInt32(object obj) => obj == null ? (int?)null : Convert.ToInt32(obj);
        public static long ToInt64(object obj) => Convert.ToInt64(obj);
        public static long? ToNullableInt64(object obj) => obj == null ? (long?)null : Convert.ToInt64(obj);

        public static byte ToUInt8(object obj) => Convert.ToByte(obj);
        public static byte? ToNullableUInt8(object obj) => obj == null ? (byte?)null : Convert.ToByte(obj);
        public static ushort ToUInt16(object obj) => Convert.ToUInt16(obj);
        public static ushort? ToNullableUInt16(object obj) => obj == null ? (ushort?)null : Convert.ToUInt16(obj);
        public static uint ToUInt32(object obj) => Convert.ToUInt32(obj);
        public static uint? ToNullableUInt32(object obj) => obj == null ? (uint?)null : Convert.ToUInt32(obj);
        public static ulong ToUInt64(object obj) => Convert.ToUInt64(obj);
        public static ulong? ToNullableUInt64(object obj) => obj == null ? (ulong?)null : Convert.ToUInt64(obj);

        public static Guid ToGuid(object obj)
        {
            if (obj is Guid) return (Guid)obj;
            return Guid.Parse(ToString(obj));
        }
        public static Guid? ToNullableGuid(object obj) => obj == null ? (Guid?)null : ToGuid(obj);

        public static DateTime ToDateTime(object obj) => Convert.ToDateTime(obj);
        public static DateTime? ToNullableDateTime(object obj) => obj == null ? (DateTime?)null : ToDateTime(obj);

        private static readonly Dictionary<Type, MethodInfo> Converters =
            typeof(PrimitiveConverter).GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Where(m =>
                {
                    var pars = m.GetParameters();
                    return pars.Length == 1 && pars[0].ParameterType == typeof(object);
                })
                .ToDictionary(m => m.ReturnType);

        public static bool IsPrimitive(Type targetType) => Converters.ContainsKey(targetType);

        public static MethodInfo ToType(Type targetType)
        {
            MethodInfo converter;
            if (Converters.TryGetValue(targetType, out converter)) return converter;
            throw new NotSupportedException($"Can't convert to {targetType}");
        }
    }
}