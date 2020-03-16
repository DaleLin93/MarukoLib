using System.Linq;
using MarukoLib.Lang.Collections;

namespace MarukoLib.Lang.Filters
{

    public class MAFilter : IFilter<double>
    {

        private readonly CircularFifoBuffer<double> _buffer;

        public MAFilter(int windowSize) => _buffer = new CircularFifoBuffer<double>(windowSize);

        public double Apply(double input)
        {
            _buffer.Add(input);
            return _buffer.Sum() / _buffer.Count;
        }

    }

}