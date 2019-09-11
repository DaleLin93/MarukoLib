using System.Collections.Generic;

namespace MarukoLib.Lang
{
    public class Pair<T>
    {

        public Pair(T left, T right)
        {
            Left = left;
            Right = right;
        }

        public T Left { get; }

        public T Right { get; }

        public Pair<T> Swap() => new Pair<T>(Right, Left);

        public bool Equals(Pair<T> other) => EqualityComparer<T>.Default.Equals(Left, other.Left) && EqualityComparer<T>.Default.Equals(Right, other.Right);

        public override bool Equals(object obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((Pair<T>) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (EqualityComparer<T>.Default.GetHashCode(Left) * 397) ^ EqualityComparer<T>.Default.GetHashCode(Right);
            }
        }

        public override string ToString() => $"{nameof(Left)}: {Left}, {nameof(Right)}: {Right}";

    }

    public class Pair : Pair<object>
    {

        public Pair(object left, object right) : base(left, right) { }

        public new Pair Swap() => new Pair(Right, Left);

    }

}
