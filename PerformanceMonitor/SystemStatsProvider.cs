using System;
using System.Threading;
using System.Threading.Tasks;
using Codeo.Core.Logging;

namespace ServiceHost.SystemMonitoring
{
    public interface ISystemStatsProvider : IDisposable
    {
        ISystemStats FetchSnapshot();
    }

    public class SystemStatsProvider : ISystemStatsProvider
    {
        private ICpuUsageSampler _cpuSampler;
        private IDiskUsageSampler _diskUsageSampler;
        private readonly IGenericLogger _logger;

        private CpuUsageSampleResult[] _lastCpuResults = null;
        private DiskUsageSampleResult[] _lastDiskResults = null;

        private SemaphoreSlim _lock = new SemaphoreSlim(1);

        public SystemStatsProvider(ICpuUsageSampler cpuSampler,
            IDiskUsageSampler diskUsageSampler,
            IGenericLogger logger)
        {
            _cpuSampler = cpuSampler;
            _diskUsageSampler = diskUsageSampler;
            _logger = logger;
            Start();
        }

        public ISystemStats FetchSnapshot()
        {
            return null;
        }

        private void Start()
        {
            _cpuSampler.OnSample += OnCpuSample;
            _diskUsageSampler.OnSample += OnDiskSample;

            _cpuSampler.Start();
            _diskUsageSampler.Start();
        }

        private void OnDiskSample(object sender, SamplerEventArgs<DiskUsageSampleResult[]> e)
        {
        }

        private void OnCpuSample(object sender, SamplerEventArgs<CpuUsageSampleResult[]> e)
        {

        }

        public void Dispose()
        {
            Stop();
        }

        private void Stop()
        {
            var cpuSampler = _cpuSampler;
            var diskSampler = _diskUsageSampler;
            _cpuSampler = null;
            _diskUsageSampler = null;
            Task.WaitAll(
                TryDispose(cpuSampler, "CPU sampler"),
                TryDispose(diskSampler, "Disk usage sampler")
            );
        }

        private Task TryDispose(
            IDisposable disposable,
            string label)
        {
            return Task.Run(() =>
            {
                try
                {
                    disposable?.Dispose();
                }
                catch (Exception ex)
                {
                    _logger.Log<SystemStatsProvider>(
                        LogType.Error,
                        $"Unable to dispose {label}",
                        ex
                    );
                }
            });
        }
    }
}
