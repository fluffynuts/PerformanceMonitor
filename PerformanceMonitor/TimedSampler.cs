namespace ServiceHost.SystemMonitoring
{
    public interface ITimedSampler : IAverage
    {
        int Seconds { get; }
    }

    public class TimedSampler : WeightedAverage, ITimedSampler
    {
        public int Seconds { get; }

        public TimedSampler(int seconds) : this(seconds, 1)
        {
        }

        public TimedSampler(int seconds, int secondsPerSample)
            : base(1D / (seconds * secondsPerSample))
        {
            Seconds = seconds;
        }
    }
}
