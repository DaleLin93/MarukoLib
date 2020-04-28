using System.Linq;
using MarukoLib.Lang.Collections;
using MarukoLib.Lang.Concurrent;

namespace MarukoLib.Lang.Filters
{

    public class MaFilter : IFilter<double>
    {

        private readonly IFiringControl _reCalcControl = new PeriodicFiringControl(100);

        private readonly CircularFifoBuffer<double> _buffer;

        private double _sum;

        public MaFilter(int windowSize) => _buffer = new CircularFifoBuffer<double>(windowSize);

        public double Apply(double input)
        {
            if (input.IsReal())
            {
                _sum += input;
                if (_buffer.Push(input, out var pop))
                    _sum = _reCalcControl.Check() ? _buffer.Sum() : _sum - pop;
            }
            return _sum / _buffer.Count;
        }

    }

}