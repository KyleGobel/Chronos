using System;

namespace Chronos
{
    public static class TypeExtensions
    {
        public static Type GetNullableType(Type type)
        {
            type = Nullable.GetUnderlyingType(type);
            if (type.IsValueType)
                return typeof (Nullable<>).MakeGenericType(type);
            else
                return type;
        }
    }
}