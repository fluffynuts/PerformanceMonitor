using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using Codeo.Core.Logging;
using NExpect;
using NSubstitute;
using NUnit.Framework;
using ServiceHost.SystemMonitoring;
using static PeanutButter.RandomGenerators.RandomValueGen;
using static NExpect.Expectations;
namespace PerformanceMonitor.Tests.SystemMonitoring
{
    [TestFixture]
    public class CpuUsageSamplerTests
    {
        // difficult to accurately test, but we can test some behaviors
        [Test]
        public void AfterStarted_ShouldEmitSamplesPerSecondPerWindowInOrder()
        {
            // Arrange
            var config = GetRandom<ISystemStatsConfig>();
            var logger = Substitute.For<IGenericLogger>();
            var captured = new List<SamplerEventArgs<CpuUsageSampleResult[]>>();
            using (var sampler = new CpuUsageSampler(config, logger))
            {
                // Act
                sampler.OnSample += (s, a) => captured.Add(a);
                sampler.Start();
                Thread.Sleep(500);
                // Assert
            }
            Expect(captured).To.Contain.At.Least(1)
                .Item();
            var first = captured.First();
            Expect(first.Sample[0].SampleSeconds)
                .To.Equal(config.ShortTermCpuWindowSeconds);
            Expect(first.Sample[1].SampleSeconds)
                .To.Equal(config.MediumTermCpuWindowSeconds);
            Expect(first.Sample[2].SampleSeconds)
                .To.Equal(config.LongTermCpuWindowSeconds);
            Expect(logger.ReceivedCalls())
                .To.Be.Empty();
        }

        [Test]
        public void ShouldLogErrors()
        {
            // Arrange
            var config = GetRandom<ISystemStatsConfig>();
            var logger = Substitute.For<IGenericLogger>();
            using (var sampler = new CpuUsageSampler(config, logger))
            {
                // nefariously rip out the _totalCpu field
                var fieldInfo = sampler.GetType().GetField(
                    "_totalCpu", BindingFlags.Instance | BindingFlags.NonPublic
                    );
                Expect(fieldInfo).Not.To.Be.Null("Where's the _totalCpu private field?");
                // Act
                var gotASample = false;
                sampler.OnSample += (o, e) =>
                {
                    fieldInfo.SetValue(sampler, null);
                    gotASample = true;
                };
                sampler.Start();
                while (!gotASample)
                {
                    Thread.Sleep(100);
                }
                // wait for another (failed) sampling
                Thread.Sleep(1200);

                // Assert
                Expect(logger).To.Have.Received(1)
                    .Log<CpuUsageSampler>(
                        LogType.Error,
                        "Error whilst attempting CPU sampling",
                        Arg.Any<NullReferenceException>());
            }
        }
    }
}
