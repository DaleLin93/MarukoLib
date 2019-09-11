using System;

namespace MarukoLib.Lang
{

    public enum PreferredDescriptionType
    {
        Short, Long
    }

    public interface IDescribable
    {

        string Describe(PreferredDescriptionType type);

    }

    public struct Describable : IDescribable
    {

        private readonly string _short, _long;

        public Describable(string description) : this(description, description) { }

        public Describable(string shortDescription, string longDescription)
        {
            _short = shortDescription;
            _long = longDescription;
        }

        public static implicit operator Describable(string rhs) => new Describable(rhs);

        public string Describe(PreferredDescriptionType type)
        {
            switch (type)
            {
                case PreferredDescriptionType.Short:
                    return _short;
                case PreferredDescriptionType.Long:
                    return _long;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }

    }

    public static class DescribableExt
    {

        public static string GetShortDescription(this IDescribable describable) => describable.Describe(PreferredDescriptionType.Short);

        public static string GetLongDescription(this IDescribable describable) => describable.Describe(PreferredDescriptionType.Long);

    }

}
