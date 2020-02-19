﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        public static decimal SumOfWeight(params RandomTarget<T>[] targets) => SumOfWeight((IReadOnlyCollection<RandomTarget<T>>)targets);

        public static decimal SumOfWeight(IReadOnlyCollection<RandomTarget<T>> targets) =>
            (from target in targets let rate = Math.Abs(target.Weight) where !(rate <= 0) select target.Weight).Sum();

        public static RandomTarget<T>[] Normalize(params RandomTarget<T>[] targets) => Normalize((IReadOnlyCollection<RandomTarget<T>>)targets);

        public static RandomTarget<T>[] Normalize(IReadOnlyCollection<RandomTarget<T>> targets)
        {
            var sum = SumOfWeight(targets);
            return targets.Select(target => new RandomTarget<T>(target.Value, target.Weight / sum)).ToArray();
        }

        public T Value { get; }

        public decimal Weight { get; }

        public override string ToString() => $"RandomTarget{{{nameof(Value)}={Value}, {nameof(Weight)}={Weight}}}";

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
            var normalizedTargets = RandomTarget<T>.Normalize(targets);
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
            _r = new Random(seed);
            var normalizedTargets = RandomTarget<T>.Normalize(targets);
            var minRate = normalizedTargets.Length == 0 ? 0 : normalizedTargets.Min(target => Math.Abs(target.Weight));
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
            Debug.Assert(choices > 0);
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