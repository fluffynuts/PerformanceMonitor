using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Codeo.Core.Extenders;
using Codeo.Core.Logging;

namespace ServiceHost.SystemMonitoring
{
    public class CpuUsageSampleResult
    {
        public int SampleSeconds { get; }
        public float AverageValue { get; }

        public CpuUsageSampleResult(
            int sampleSeconds,
            float averageValue)
        {
            SampleSeconds = sampleSeconds;
            AverageValue = averageValue;
        }
    }

    public interface IEmitter<T>
    {
        EventHandler<SamplerEventArgs<T>> OnSample { get; set; }
    }

    public abstract class SampleEmitter<T> : IEmitter<T>
    {
        public EventHandler<SamplerEventArgs<T>> OnSample { get; set; }

        protected void Emit(T data)
        {
            OnSample?.Invoke(
                this,
                SamplerEventArgs.For(data)
            );
        }
    }

    public interface ISampler<T> : IDisposable, IEmitter<T>
    {
        void Start();
        void Stop();
    }

    public abstract class Sampler<T> : SampleEmitter<T>, ISampler<T>
    {
        private Thread _thread;
        private bool _running;
        private DateTime _lastSampled;
        private object _lock = new object();

        public void Dispose()
        {
            Stop();
        }

        public void Start()
        {
            lock (_lock)
            {
                if (_thread != null)
                {
                    return;
                }

                _running = true;
                Initialize();
                _thread = new Thread(StartSampling);
                _thread.Start();
            }
        }

        private void StartSampling()
        {
            while (_running)
            {
                var before = DateTime.Now;
                var delta = DateTime.Now - _lastSampled;
                if (delta.TotalMilliseconds >= 1000)
                {
                    try
                    {
                        Emit(Sample());
                    }
                    catch (Exception ex)
                    {
                        try
                        {
                            OnSampleError(ex);
                        }
                        catch (Exception errorHandlerEx)
                        {
                            // don't allow a faulty error handler to break this thread
                            Trace.WriteLine(
                                $"{GetType()} sampler does not handle error {ex}: {errorHandlerEx}"
                            );
                        }
                    }
                    finally
                    {
                        _lastSampled = DateTime.Now;
                    }
                }

                var runTime = (int) (DateTime.Now - before).TotalMilliseconds;
                var toSleep = 100 - runTime;
                if (toSleep > 0)
                {
                    Thread.Sleep(toSleep);
                }
            }
        }

        public void Stop()
        {
            lock (_lock)
            {
                _running = false;
                _thread?.Join();
                _thread = null;
            }
        }

        protected abstract T Sample();
        protected abstract void Initialize();
        protected abstract void OnSampleError(Exception ex);
    }

    public interface ICpuUsageSampler : ISampler<CpuUsageSampleResult[]>
    {
    }

    public abstract class SamplerEventArgs : EventArgs
    {
        public static SamplerEventArgs<T> For<T>(T sample)
        {
            return new SamplerEventArgs<T>(sample);
        }
    }

    public class SamplerEventArgs<T> : SamplerEventArgs
    {
        public T Sample { get; }

        public SamplerEventArgs(T sample)
        {
            Sample = sample;
        }
    }

    public class CpuUsageSampler
        : Sampler<CpuUsageSampleResult[]>,
          ICpuUsageSampler
    {
        private readonly IGenericLogger _logger;
        private PerformanceCounter _totalCpu;
        private readonly IEnumerable<TimedSampler> _samplers;

        public CpuUsageSampler(
            ISystemStatsConfig config,
            IGenericLogger logger)
        {
            _logger = logger;
            var windows = new[]
            {
                config.ShortTermCpuWindowSeconds,
                config.MediumTermCpuWindowSeconds,
                config.LongTermCpuWindowSeconds
            };
            _samplers = windows.Select(i => new TimedSampler(i)).ToArray();
        }

        protected override CpuUsageSampleResult[] Sample()
        {
            var current = _totalCpu.NextValue();
            foreach (var sampler in _samplers)
            {
                sampler.AddSample(current);
            }

            return _samplers.Select(
                s => new CpuUsageSampleResult(s.Seconds, (float) s.Average)
            ).ToArray();
        }

        protected override void Initialize()
        {
            _totalCpu = new PerformanceCounter("Processor", "% Processor Time", "_Total");
        }

        protected override void OnSampleError(Exception ex)
        {
            _logger.Log<CpuUsageSampler>(
                LogType.Error,
                "Error whilst attempting CPU sampling",
                ex);
        }
    }

    public interface ISystemStatsConfig
    {
        int ShortTermCpuWindowSeconds { get; }
        int MediumTermCpuWindowSeconds { get; }
        int LongTermCpuWindowSeconds { get; }

        string OSDrive { get; }
        string DataDrive { get; }
    }

    public interface ISystemStats
    {
        DateTime? CpuLastUpdated { get; }
        DateTime? DiskLastUpdated { get; }

        float ShortTermCpuUsage { get; }
        float MediumTermCpuUsage { get; }
        float LongTermCpuUsage { get; }

        long DataDriveFreeBytes { get; }
        float DataDriveFreePercentage { get; }

        long OSDriveFreeBytes { get; }
        float OSDriveFreePercentage { get; }
    }

    public class SystemStats : ISystemStats
    {
        public DateTime? CpuLastUpdated { get; }
        public DateTime? DiskLastUpdated { get; }
        public float ShortTermCpuUsage { get; }
        public float MediumTermCpuUsage { get; }
        public float LongTermCpuUsage { get; }

        public long DataDriveFreeBytes { get; }
        public float DataDriveFreePercentage { get; }
        public long OSDriveFreeBytes { get; }
        public float OSDriveFreePercentage { get; }

        public SystemStats(
            DateTime? cpuLastUpdated,
            CpuUsageSampleResult shortTermCpuUsage,
            CpuUsageSampleResult mediumTermCpuUsage,
            CpuUsageSampleResult longTermCpuUsage,
            DateTime? diskLastUpdated,
            DiskUsageSampleResult osDriveUsage,
            DiskUsageSampleResult dataDriveUsage
        )
        {
            CpuLastUpdated = cpuLastUpdated;
            DiskLastUpdated = diskLastUpdated;

            ShortTermCpuUsage = shortTermCpuUsage?.AverageValue ?? 0;
            MediumTermCpuUsage = mediumTermCpuUsage?.AverageValue ?? 0;
            LongTermCpuUsage = longTermCpuUsage?.AverageValue ?? 0;

            DataDriveFreeBytes = dataDriveUsage?.FreeBytes ?? 0;
            DataDriveFreePercentage = dataDriveUsage?.FreePercentage ?? 0;

            OSDriveFreeBytes = osDriveUsage?.FreeBytes ?? 0;
            OSDriveFreePercentage = osDriveUsage?.FreePercentage ?? 0;
        }
    }
}
