using System;

namespace MarukoLib.Lang
{

    public class PseudoRandom : IRandomBoolSequence
    {

        private readonly object _lock = new object();

        private readonly Random _r;

        private readonly double _target, _nonTarget;

        private double _targetCount, _nonTargetCount;

        public PseudoRandom(double targetRate) : this(targetRate, (int)DateTimeUtils.CurrentTimeTicks) { }

        public PseudoRandom(double targetRate, int seed)
        {
            TargetRate = targetRate;
            _r = new Random(seed);
            if (targetRate > 0.5)
            {
                _target = 1 / (1 - targetRate) - 1;
                _nonTarget = 1;
            }
            else
            {
                _target = 1;
                _nonTarget = 1 / targetRate - 1;
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
                var modifiedTargetRate = (_target - _targetCount) / (_target - _targetCount + _nonTarget - _nonTargetCount);
                var target = _r.NextDouble() < modifiedTargetRate;
                if (target)
                    _targetCount++;
                else
                    _nonTargetCount++;
                if (_targetCount >= _target && _nonTargetCount >= _nonTarget)
                {
                    _targetCount -= _target;
                    _nonTargetCount -= _nonTarget;
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
