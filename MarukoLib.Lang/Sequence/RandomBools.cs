using System;

namespace MarukoLib.Lang.Sequence
{

    public interface IRandomBools : ISequence<bool>
    {

        decimal TargetRate { get; }

    }

    public sealed class RandomBools : IRandomBools
    {

        private readonly Random _random;

        public RandomBools(decimal targetRate = (decimal)0.5)
        {
            _random = new Random();
            TargetRate = targetRate;
        }

        public RandomBools(int seed, decimal targetRate)
        {
            _random = new Random(seed);
            TargetRate = targetRate;
        }

        public decimal TargetRate { get; }

        public bool Next() => (decimal) _random.NextDouble() < TargetRate;

        public void Reset() { }

    }

    public class PseudoRandomBools : PseduoRandomSequence<bool>, IRandomBools
    {

        public PseudoRandomBools() : this((decimal) 0.5) { }

        public PseudoRandomBools(decimal targetRate) : this(targetRate, (int)DateTimeUtils.CurrentTimeTicks) { }

        public PseudoRandomBools(decimal targetRate, int seed) : base(seed, new RandomTarget<bool>(true, targetRate), new RandomTarget<bool>(false, 1 - targetRate))
            => TargetRate = targetRate;

        public decimal TargetRate { get; }

    }

}
