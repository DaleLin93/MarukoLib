using System;

namespace MarukoLib.Lang
{

    public class PseudoRandom : IRandomBoolSequence
    {

        private readonly object _lock = new object();

        private readonly Random _r;

        private readonly double Target;

        private readonly double NonTarget;

        private double _targetCount;

        private double _nonTargetCount;

        public PseudoRandom(double targetRate) : this(targetRate, (int)DateTimeUtils.CurrentTimeTicks) { }

        public PseudoRandom(double targetRate, int seed)
        {
            TargetRate = targetRate;
            _r = new Random(seed);
            if (targetRate > 0.5)
            {
                Target = 1 / (1 - targetRate) - 1;
                NonTarget = 1;
            }
            else
            {
                Target = 1;
                NonTarget = 1 / targetRate - 1;
            }
        }

        public double TargetRate { get; }

        public bool Next()
        {
            if (TargetRate <= 0)
                return false;
            else if (TargetRate >= 1)
                return true;
            lock (_lock)
            {
                var modifiedTargetRate = (Target - _targetCount) / (Target - _targetCount + NonTarget - _nonTargetCount);
                bool target = _r.NextDouble() < modifiedTargetRate;
                if (target)
                    _targetCount++;
                else
                    _nonTargetCount++;
                if (_targetCount >= Target && _nonTargetCount >= NonTarget)
                {
                    _targetCount -= Target;
                    _nonTargetCount -= NonTarget;
                }
                return target;
            }
        }

        public void Reset()
        {
            lock (_lock)
            {
                _targetCount = 0;
                _nonTargetCount = 0;
            }
        }

    }
}
