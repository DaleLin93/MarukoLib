namespace MarukoLib.Lang.Filters
{

    /// <summary>
    /// Exponentially Weighted Moving Average
    /// X(n) = (1 - lambda) * X(n-1) + lambda * Y(n)
    /// X(n) is n-th weighted moving average value and Y(n) is n-th observation value.
    /// </summary>
    public class EWMAFilter : IFilter<double>
    {

        private double _x = double.NaN;

        public EWMAFilter(double lambda) => Lambda = lambda;

        public EWMAFilter(double lambda, double initialValue) : this(lambda) => _x = initialValue;

        public double Lambda { get; }

        public double Apply(double y)
        {
            if (double.IsNaN(_x)) return _x = y;
            return _x = (1 - Lambda) * _x + Lambda * y;
        }

    }

}
