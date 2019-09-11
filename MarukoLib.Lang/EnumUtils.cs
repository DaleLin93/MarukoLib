using System;

namespace MarukoLib.Lang
{
    public static class EnumUtils
    {

        public static T[] GetEnumValues<T>() 
        {
            if (!typeof(Enum).IsAssignableFrom(typeof(T)))
                throw new ArgumentException("type is not enum");
            return (T[])typeof(T).GetEnumValues();
        }

    }
}
