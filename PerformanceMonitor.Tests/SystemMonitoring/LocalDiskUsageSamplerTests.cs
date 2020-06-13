using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Codeo.Core.Logging;
using NExpect;
using NSubstitute;
using NUnit.Framework;
using ServiceHost.SystemMonitoring;
using static NExpect.Expectations;
using static PeanutButter.RandomGenerators.RandomValueGen;

namespace PerformanceMonitor.Tests.SystemMonitoring
{
    [TestFixture]
    public class LocalDiskUsageSamplerTests
    {
        [Test]
        public void AfterStarted_ShouldEmitSamplesPerSecondPerWindowInOrder()
        {
            // Arrange
            var config = GetRandom<ISystemStatsConfig>();
            var logger = Substitute.For<IGenericLogger>();
            var captured = new List<SamplerEventArgs<DiskUsageSampleResult[]>>();
            using (var sampler = new LocalFixedDiskUsageSampler(config, logger))
            {
                sampler.OnSample += (s, a) => captured.Add(a);
                // Act
                sampler.Start();
                Thread.Sleep(500);
                // Assert
            }

            Expect(logger.ReceivedCalls())
                .To.Be.Empty();
            Expect(captured).To.Contain.At.Least(1)
                .Item();
            var first = captured.First();
            Expect(first.Sample[0].Device)
                .To.Equal(config.OSDrive);
            Expect(first.Sample[1].Device)
                .To.Equal(config.DataDrive);
        }
    }
}
