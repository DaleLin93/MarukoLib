using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace MarukoLib.Lang
{
    public static class ReflectUtils
    {

        public static T GetProperty<T>(this object obj, string propertyName, T defaultVal = default)
        {
            try
            {
                return (T)obj.GetType().GetProperty("DisplayName")?.GetValue(obj);
            }
            catch (Exception)
            {
                return defaultVal;
            }
        }

        public static ICollection<T> ReadStaticFields<T>(this Type type, bool recursively = true) => ReadFields<T>(type, null, recursively);

        public static ICollection<T> ReadFields<T>(this Type type, object obj = null, bool recursively = true)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            var values = new LinkedList<T>();
            do
            {
                foreach (var field in type.GetRuntimeFields())
                {
                    if (obj == null && !field.IsStatic) continue;
                    if (typeof(T).IsAssignableFrom(field.FieldType))
                    {
                        var value = field.GetValue(obj);
                        if (value != null)
                            values.AddLast((T)value);
                    }
                    else if (typeof(T).IsAssignableFrom(field.FieldType.GetGenericTypes(typeof(IEnumerable<>), 0).FirstOrDefault()))
                    {
                        foreach (var value in (IEnumerable<T>)field.GetValue(obj))
                            if (value != null)
                                values.AddLast(value);
                    }
                    else if (typeof(T).IsAssignableFrom(field.FieldType.GetGenericTypes(typeof(IDictionary<,>), 1).FirstOrDefault()))
                    {
                        foreach (var value in (IEnumerable<T>)field.GetValue(obj))
                            if (value != null)
                                values.AddLast(value);
                    }
                }
            } while (recursively && (type = type.BaseType) != null);
            return values;
        }

    }
}
