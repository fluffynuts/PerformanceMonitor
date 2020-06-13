using System;

namespace ServiceHost.SystemMonitoring
{
    public interface IAverage
    {
        double Average { get; }
        void AddSample(double value);
    }
    
    public class WeightedAverage : IAverage
    {
        public override string ToString()
        {
            return $"M: {Average:0.0}    A:{_alpha:0.0000}     S: {_samples:0}";
        }

        public double Average => _average ?? default(double);
        private double? _average;
        private readonly double _alpha;
        private int _samples;

        public WeightedAverage(
            double alpha)
        {
            if (alpha > 1)
            {
                throw new ArgumentException($"Alpha must be < 1 (received {alpha})", nameof(alpha));
            }

            _alpha = alpha;
        }

        public void AddSample(double value)
        {
            _samples++;
            _average = _average.HasValue
                ? (_alpha * value) + ((1D - _alpha) * Average)
                : value;
        }
    }
}
