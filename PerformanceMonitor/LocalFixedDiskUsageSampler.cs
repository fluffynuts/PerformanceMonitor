using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using Codeo.Core.Extenders;
using Codeo.Core.Logging;
using Dapper;
using MySql.Data.MySqlClient;

namespace ServiceHost.SystemMonitoring
{

    public interface IConnectionStringProvider
    {
        string GetMainConnectionString();
    }

    public class DiskUsageSampleResult
    {
        public string Device { get; }
        public long FreeBytes { get; }
        public float FreePercentage { get; }

        public DiskUsageSampleResult(
            string device,
            long freeBytes,
            float freePercentage)
        {
            Device = device;
            FreeBytes = freeBytes;
            FreePercentage = freePercentage;
        }
    }

    public interface IDiskUsageSampler : ISampler<DiskUsageSampleResult[]>
    {
    }

    public class LocalFixedDiskUsageSampler : Sampler<DiskUsageSampleResult[]>, IDiskUsageSampler
    {
        private readonly ISystemStatsConfig _config;
        private readonly IGenericLogger _logger;

        public LocalFixedDiskUsageSampler(
            ISystemStatsConfig config,
            IGenericLogger logger)
        {
            _config = config;
            _logger = logger;
        }

        protected override DiskUsageSampleResult[] Sample()
        {
            return new[]
            {
                new DriveInfo(_config.OSDrive?.FirstOrDefault().ToString() ?? "C"),
                new DriveInfo(_config.DataDrive?.FirstOrDefault().ToString() ?? "D")
            }.Select(info => new DiskUsageSampleResult(
                info.Name.FirstOrDefault().ToString(),
                info.AvailableFreeSpace,
                100 * (info.AvailableFreeSpace / info.TotalSize)
            )).ToArray();
        }

        protected override void Initialize()
        {
            // nothing to do for init
        }

        protected override void OnSampleError(Exception ex)
        {
            _logger.Log<LocalFixedDiskUsageSampler>(
                LogType.Error,
                "Error whilst attempting disk usage sampling",
                ex);
        }
    }

    public class StorageInfo
    {
        public string Device { get; set; }
        public long FreeBytes { get; set; }
        public long TotalBytes { get; set; }
    }

    public class ServerStats
    {
        public float[] Load { get; set; }
        public StorageInfo[] StorageInfo { get; set; }
    }

}
