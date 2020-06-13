using System.IO;
using System.Linq;
using NSubstitute;
using PeanutButter.RandomGenerators;
using ServiceHost.SystemMonitoring;

namespace PerformanceMonitor.Tests.SystemMonitoring
{
    public class SystemStatsConfigBuilder : GenericBuilder<SystemStatsConfigBuilder, ISystemStatsConfig>
    {
        public override ISystemStatsConfig ConstructEntity()
        {
            return Substitute.For<ISystemStatsConfig>();
        }

        public override SystemStatsConfigBuilder WithRandomProps()
        {
            var allDrives = DriveInfo.GetDrives();
            var primary = allDrives.First();
            var secondary = allDrives.Skip(1).FirstOrDefault() ?? primary;
            // probably normally 5 / 30 / 300 in prod
            return WithProp(o => o.ShortTermCpuWindowSeconds.Returns(RandomValueGen.GetRandomInt(1, 5)))
                .WithProp(o => o.MediumTermCpuWindowSeconds.Returns(RandomValueGen.GetRandomInt(10, 20)))
                .WithProp(o => o.LongTermCpuWindowSeconds.Returns(RandomValueGen.GetRandomInt(3, 60)))
                .WithProp(o => o.OSDrive.Returns(primary.Name.Substring(0, 1)))
                .WithProp(o => o.DataDrive.Returns(secondary.Name.Substring(0, 1)));
        }
    }
}
