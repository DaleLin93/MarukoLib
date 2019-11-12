using System;
using System.Collections.Generic;
using System.Linq;

namespace MarukoLib.Lang
{

    public static class TypeUtils
    {

        public static T Default<T>() => (T)Default(typeof(T));

        public static object Default(Type type) => type.IsClass ? null : Activator.CreateInstance(type);

        public static object InitClassOrStruct(this Type type) => Activator.CreateInstance(type);

        public static bool IsInstanceOfTypeOrNull(this Type type, object value) => type.IsInstanceOfType(value) || (type.IsClass || type.IsNullableType()) && value == null;

        public static string GetFriendlyName(this Type type)
        {
            var friendlyName = type.Name;
            if (type.IsGenericType)             
            {
                var iBacktick = friendlyName.IndexOf('`');
                if (iBacktick > 0)
                    friendlyName = friendlyName.Remove(iBacktick);
                friendlyName += "<";
                var typeParameters = type.GetGenericArguments();
                for (var i = 0; i < typeParameters.Length; ++i)
                {
                    var typeParamName = GetFriendlyName(typeParameters[i]);
                    friendlyName += (i == 0 ? typeParamName : "," + typeParamName);
                }
                friendlyName += ">";
            }
            return friendlyName;
        }

        [Obsolete]
        public static TOut CastOrConvert<TIn, TOut>(this TIn input, Func<TIn, TOut> func) where TOut : TIn => input is TOut tout ? tout : func(input);

        public static TSubclass CastOrConvertToSubType<TFrom, TSubclass>(this TFrom input, Func<TFrom, TSubclass> convertFunc) 
            where TSubclass : TFrom => input is TSubclass tout ? tout : convertFunc(input);

        public static bool IsNullableType(this Type type) => Nullable.GetUnderlyingType(type) != null;

        public static bool IsNullableType(this Type type, out Type underlyingType) => (underlyingType = Nullable.GetUnderlyingType(type)) != null;

        public static IEnumerable<Type> GetGenericTypes(this Type type, Type genericType, string genericArgumentName)
        {
            if (!genericType.IsGenericTypeDefinition) throw new ArgumentException("'genericType' is not a generic type definition");
            var genericArguments = genericType.GetGenericArguments();
            for (var i = 0; i < genericArguments.Length; i++)
                if (Equals(genericArgumentName, genericArguments[i].Name))
                    return GetGenericTypes(type, genericType, i);
            throw new ArgumentException($"generic argument not found, by name: '{genericArgumentName}'");
        }

        public static Type GetGenericType(this Type type, Type genericType, int genericArgumentIndex = 0)
        {
            var types = GetGenericTypes(type, genericType, genericArgumentIndex).ToArray();
            if (types.Length != 1) throw new ArgumentException("multiple definitions");
            return types[0];
        }

        public static IEnumerable<Type> GetGenericTypes(this Type type, Type genericType, int genericArgumentIndex = 0)
        {
            if (!genericType.IsGenericTypeDefinition) throw new ArgumentException("'genericType' is not a generic type definition");
            if (type == genericType)
            {
                yield return null;
                yield break;
            }
            foreach (var typePath in GetTypePaths(type, genericType))
            {
                var argumentIndex = genericArgumentIndex;
                Type outType = null;
                do
                {
                    var currentType = typePath.Pop();
                    if (!currentType.IsGenericType) break;
                    var argument = currentType.GetGenericArguments()[argumentIndex];
                    if (argument.IsGenericParameter)
                    {
                        if (typePath.IsEmpty()) break;
                        var child = typePath.Peek();
                        var childArgs = child.GetGenericTypeDefinition().GetGenericArguments();
                        var newArgIndex = -1;
                        for (var i = 0; i < childArgs.Length; i++)
                            if (childArgs[i] == argument)
                            {
                                newArgIndex = i;
                                break;
                            }
                        if (newArgIndex < 0) break;
                    }
                    else
                    {
                        outType = argument;
                        break;
                    }
                } while (typePath.Any());
                yield return outType;
            }
        }

        public static IEnumerable<Stack<Type>> GetTypePaths(this Type type, Type targetType)
        {
            if (targetType.IsAssignableFrom(type)) throw new ArgumentException();
            bool IsMatched(Type t)
            {
                if (t == targetType)
                    return true;
                if (targetType.IsGenericTypeDefinition && t.IsGenericType)
                    return t.GetGenericTypeDefinition() == targetType;
                return false;
            }
            if (IsMatched(type))
            {
                yield return new Stack<Type>(new [] { type });
                yield break;
            }
            const int initialIndex = -1;
            var stack = new Stack<Tuple<Type, Type[], int>>(); // <Type, Children, Previous Index>
            Type[] GetChildren(Type t, bool processInterface)
            {
                var baseType = t.BaseType;
                if (!targetType.IsInterface || !processInterface)
                    return baseType == null ? EmptyArray<Type>.Instance : new[] {baseType};
                var interfaces = t.GetInterfaces();
                if (baseType == null) return interfaces;
                var children = new Type[1 + interfaces.Length];
                children[0] = baseType;
                Array.Copy(interfaces, 0, children, 1, interfaces.Length);
                return children;
            }
            stack.Push(new Tuple<Type, Type[], int>(type, GetChildren(type, true), initialIndex));
            do
            {
                var node = stack.Pop();
                var index = node.Item3 + 1;
                if (index >= node.Item2.Length) continue;
                stack.Push(new Tuple<Type, Type[], int>(node.Item1, node.Item2, index));
                var currentType = node.Item2[index];
                if (IsMatched(currentType))
                {
                    var path = new Stack<Type>(stack.Select(tuple => tuple.Item1).Reverse());
                    path.Push(currentType);
                    yield return path;
                    if (!type.IsInterface) continue;
                }
                var children = GetChildren(currentType, false);
                if (children.IsNotEmpty()) stack.Push(new Tuple<Type, Type[], int>(currentType, children, initialIndex));
            } while (stack.Any());
        }

    }

}
