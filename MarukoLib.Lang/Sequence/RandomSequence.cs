using System;
using System.Collections.Generic;
using System.Linq;

namespace MarukoLib.Lang.Sequence
{

    public struct RandomTarget<T>
    {

        public RandomTarget(T value) : this(value, (decimal) 1) { }

        public RandomTarget(T value, double weight) : this(value, (decimal) weight) { }

        public RandomTarget(T value, decimal weight)
        {
            Value = value;
            Weight = weight;
        }

        public T Value { get; }

        public decimal Weight { get; }

        public override string ToString() => $"RandomTarget{{{nameof(Value)}={Value}, {nameof(Weight)}={Weight}}}";

    }

    public static class RandomTargetExt
    {

        public static decimal SumOfWeight<T>(params RandomTarget<T>[] targets) 
            => SumOfWeight((IReadOnlyCollection<RandomTarget<T>>)targets);

        public static decimal SumOfWeight<T>(this IReadOnlyCollection<RandomTarget<T>> targets) 
            => (from target in targets let rate = Math.Abs(target.Weight) where rate > 0 select rate).Sum();

        public static RandomTarget<T>[] Normalize<T>(params RandomTarget<T>[] targets) 
            => Normalize((IReadOnlyCollection<RandomTarget<T>>)targets);

        public static RandomTarget<T>[] Normalize<T>(this IReadOnlyCollection<RandomTarget<T>> targets) 
            => (from target in targets let weight = target.Weight where weight != 0 select target)
                .ToArray();

    }

    public class RandomSequence<T> : ISequence<T>
    {

        private readonly Random _r;

        private readonly T[] _targetValues;

        private readonly decimal[] _targetBounds;

        public RandomSequence(params RandomTarget<T>[] targets) : this((int)DateTimeUtils.CurrentTimeTicks, targets) { }

        public RandomSequence(int seed, params RandomTarget<T>[] targets)
        {
            _r = new Random(seed);
            var normalizedTargets = targets.Normalize();
            TargetCount = normalizedTargets.Length;
            _targetValues = new T[TargetCount];
            _targetBounds = new decimal[TargetCount];
            for (var i = 0; i < TargetCount; i++)
            {
                var target = normalizedTargets[i];
                _targetValues[i] = target.Value;
                _targetBounds[i] = (i == 0 ? 0 : _targetBounds[i - 1]) + Math.Abs(target.Weight);
            }
            if (TargetCount > 0) _targetBounds[TargetCount - 1] = decimal.MaxValue;
        }

        public int TargetCount { get; }

        public T Next()
        {
            var rnd = (decimal) _r.NextDouble();
            for (var i = 0; i < TargetCount; i++)
                if (rnd < _targetBounds[i]) 
                    return _targetValues[i];
            return default;
        }

        public void Reset() { }

    }

    public class PseduoRandomSequence<T> : ISequence<T>
    {

        public const decimal DefaultRandomness = 1;

        private readonly Random _r;

        private readonly T[] _targetValues;

        private readonly decimal[] _targetRoundCounts, _targetCounts;

        public PseduoRandomSequence(params RandomTarget<T>[] targets) : this(null, DefaultRandomness, targets) { }

        public PseduoRandomSequence(int? roundSize, params RandomTarget<T>[] targets) : this(roundSize, DefaultRandomness, targets) { }

        public PseduoRandomSequence(int? roundSize, decimal randomness, params RandomTarget<T>[] targets) : this((int)DateTimeUtils.CurrentTimeTicks, roundSize, randomness, targets) { }

        public PseduoRandomSequence(int seed, int? roundSize, decimal randomness, params RandomTarget<T>[] targets)
        {
            if (roundSize != null && roundSize <= 0) throw new ArgumentException($"{nameof(roundSize)} must be positive");
            if (randomness <= 0) throw new ArgumentException($"{nameof(randomness)} must be positive");
            _r = new Random(seed);
            var normalizedTargets = targets.Normalize();
            if (normalizedTargets.IsEmpty()) throw new ArgumentException($"No valid {targets}");
            var minRate = normalizedTargets.Min(target => Math.Abs(target.Weight));
            var actualRoundSize = roundSize ?? 1 / minRate;
            TargetCount = normalizedTargets.Length;
            _targetValues = new T[TargetCount];
            _targetRoundCounts = new decimal[TargetCount];
            _targetCounts = new decimal[TargetCount];
            for (var i = 0; i < TargetCount; i++)
            {
                var target = normalizedTargets[i];
                _targetValues[i] = target.Value;
                _targetRoundCounts[i] = Math.Abs(target.Weight) * actualRoundSize * randomness;
                System.Diagnostics.Debug.Assert(_targetRoundCounts[i] > 0);
            }
        }

        public int TargetCount { get; }

        public T Next()
        {
            if (TargetCount <= 0) return default;
            var rnd = (decimal) _r.NextDouble();
            var choices = 0;
            decimal roundSum = 0;
            for (var i = 0; i < TargetCount; i++)
            {
                var remainingCount = _targetRoundCounts[i] - _targetCounts[i];
                if (remainingCount <= 0) continue;
                roundSum += remainingCount;
                choices++;
            }
            System.Diagnostics.Debug.Assert(choices > 0);
            decimal accumulated = 0;
            var lastIndex = -1;
            for (var i = 0; i < TargetCount; i++)
            {
                var remainingCount = _targetRoundCounts[i] - _targetCounts[i];
                if (remainingCount <= 0) continue;
                lastIndex = i;
                var rate = remainingCount / roundSum;
                accumulated += rate;
                if (rnd < accumulated) break;
            }
            _targetCounts[lastIndex] += 1;
            if (choices == 1 && _targetCounts[lastIndex] >= _targetRoundCounts[lastIndex])
                for (var i = 0; i < TargetCount; i++)
                    _targetCounts[i] -= _targetRoundCounts[i];
            return _targetValues[lastIndex];
        }

        public void Reset()
        {
            for (var i = 0; i < _targetCounts.Length; i++) 
                _targetCounts[i] = 0;
        }

    }

}
