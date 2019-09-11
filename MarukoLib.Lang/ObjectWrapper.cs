using System;

namespace MarukoLib.Lang
{

    public interface IReadableObjectWrapper
    {

        object Value { get; }

    }

    public interface IWritableObjectWrapper
    {

        object Value { set; }

    }

    // ReSharper disable once PossibleInterfaceMemberAmbiguity
    public interface IObjectWrapper : IReadableObjectWrapper, IWritableObjectWrapper
    {

        bool ReadOnly { get; }

    }

    public class ObjectWrapper<T> : IObjectWrapper
    {

        private T _value;

        public ObjectWrapper() : this(default) { }

        public ObjectWrapper(T value, bool @readonly = false)
        {
            Value = value;
            ReadOnly = @readonly;
        } 

        public bool ReadOnly { get; }

        public T Value
        {
            get => _value;
            set
            {
                if (ReadOnly) throw new NotSupportedException("cannot set value to readonly wrapper");
                _value = value;
            }
        }

        object IReadableObjectWrapper.Value => Value;

        object IWritableObjectWrapper.Value { set => Value = (T)value; }

    }

}
