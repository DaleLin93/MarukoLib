using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using JetBrains.Annotations;
using MarukoLib.Lang;
using MarukoLib.UI;

namespace MarukoLib.Parametrization
{

    public static class TypeConverters
    {

        public static readonly ITypeConverter<Color, uint> SdColor2UInt =
            TypeConverter<Color, uint>.Of(sdColor => sdColor.ToUIntArgb(), uintColor => uintColor.ToSdColor());

        public static readonly ITypeConverter<string, Uri> String2AbsoluteUri =
            TypeConverter<string, Uri>.Of(str => new Uri(str, UriKind.Absolute), uri => uri.ToString());

        private static readonly IReadOnlyCollection<ITypeConverter> Converters;

        static TypeConverters()
        {
            Converters = (from fieldInfo in typeof(TypeConverters).GetFields()
                    where typeof(ITypeConverter).IsAssignableFrom(fieldInfo.FieldType)
                    select (ITypeConverter)fieldInfo.GetValue(null))
                .ToList();
        }

        public static bool IsBasicType([NotNull] this Type type)
        {
            if (type.IsNullableType(out var underlyingType)) type = underlyingType;
            return type.IsPrimitive || type.IsEnum || type == typeof(string);
        }

        public static ITypeConverter<T1, T2> CreateBiDirectionConverter<T1, T2>(IEnumerable<Tuple<T1, T2>> tuples,
            out IReadOnlyDictionary<T1, T2> dictionary1, out IReadOnlyDictionary<T2, T1> dictionary2)
        {
            var dict1 = new Dictionary<T1, T2>();
            var dict2 = new Dictionary<T2, T1>();
            dictionary1 = dict1;
            dictionary2 = dict2;
            foreach (var tuple in tuples)
            {
                dict1.Add(tuple.Item1, tuple.Item2);
                dict2.Add(tuple.Item2, tuple.Item1);
            }
            return TypeConverter<T1, T2>.Of(t1 => dict1[t1], t2 => dict2[t2]);
        }

        public static ITypeConverter<string, T> CreateNamedConverter<T>(IEnumerable<T> values, out IReadOnlyDictionary<string, T> dictionary) where T : INamed 
            => CreateNamedConverter(values, t => t.Name, out dictionary);

        public static ITypeConverter<string, T> CreateNamedConverter<T>(IEnumerable<T> values, Func<T, string> nameFunc, 
            out IReadOnlyDictionary<string, T> dictionary) 
        {
            var dict = new Dictionary<string, T>();
            dictionary = dict;
            foreach (var value in values) dict[nameFunc(value)] = value;
            return TypeConverter<string, T>.Of(str => dict[str], nameFunc);
        }

        public static ITypeConverter<T1, T2> GetConverter<T1, T2>()
        {
            if (!FindConverter<T1, T2>(out var converter))
                throw new Exception();
            return converter;
        }

        public static bool FindConverter<T1, T2>(out ITypeConverter<T1, T2> converter)
        {
            Type t1 = typeof(T1), t2 = typeof(T2);
            if (FindConverter(t1, t2, out var rawConverter))
            {
                converter = rawConverter.As<T1, T2>();
                return true;
            }
            converter = null;
            return false;
        }

        public static bool FindConverter(Type t1, Type t2, out ITypeConverter converter)
        {
            if (t1 == t2)
            {
                converter = TypeConverter.Identity(t1, t2);
                return true;
            }
            if (t1.IsBasicType() && t2.IsBasicType())
            {
                converter = TypeConverter.SystemConvert(t1, t2);
                return true;
            }
            foreach (var current in Converters)
            {
                if (current.IsExactlyMatch(t1, t2))
                {
                    converter = current;
                    return true;
                }
                if (current.IsExactlyMatch(t2, t1))
                {
                    converter = current.Inverse();
                    return true;
                }
            }
            converter = default;
            return false;
        }
        
    }

}
