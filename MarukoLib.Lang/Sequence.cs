using JetBrains.Annotations;
using System;

namespace MarukoLib.Lang
{

    public interface ISequence<out T>
    {

        T Next();

        void Reset();

    }

    public class Sequence<T> : ISequence<T>
    {

        [NotNull] private readonly Func<T> _nextFunc;

        [CanBeNull] private readonly Action _resetFunc;

        public Sequence([NotNull] Func<T> nextFunc, [CanBeNull] Action resetAction)
        {
            _nextFunc = nextFunc ?? throw new ArgumentNullException(nameof(nextFunc));
            _resetFunc = resetAction;
        }

        public T Next() => _nextFunc();

        public void Reset() => _resetFunc?.Invoke();

    }

    public interface IBoolSequence : ISequence<bool> { }

    public interface IRandomBoolSequence : IBoolSequence
    {

        double TargetRate { get; }

    }

    public sealed class RandomBools : IRandomBoolSequence
    {

        private readonly Random _random;

        public RandomBools() : this(0.5) { }

        public RandomBools(double targetRate)
        {
            _random = new Random();
            TargetRate = targetRate;
        }

        public RandomBools(int seed, double targetRate)
        {
            _random = new Random(seed);
            TargetRate = targetRate;
        }

        public double TargetRate { get; }

        public bool Next() => _random.NextDouble() < TargetRate;

        public void Reset() { }

    }

}
