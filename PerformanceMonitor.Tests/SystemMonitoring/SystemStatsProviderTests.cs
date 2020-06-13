using System;
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
    public class SystemStatsProviderTests
    {
        [Test]
        public void ShouldImplement_ISystemStatsProvider()
        {
            // Arrange
            // Act
            Expect(typeof(SystemStatsProvider))
                .To.Implement<ISystemStatsProvider>();
            // Assert
        }

        [TestFixture]
        public class Construction
        {
            [Test]
            public void ShouldStartCpuSampler()
            {
                // Arrange
                var cpuSampler = Substitute.For<ICpuUsageSampler>();
                // Act
                using (Create(cpuSampler))
                {
                    // Assert
                    Expect(cpuSampler)
                        .To.Have.Received(1)
                        .Start();
                }
            }

            [Test]
            public void ShouldStartDiskSampler()
            {
                // Arrange
                var cpuSampler = Substitute.For<ICpuUsageSampler>();
                var diskSampler = Substitute.For<IDiskUsageSampler>();
                // Act
                using (Create(cpuSampler, diskSampler))
                {
                    // Assert
                    Expect(diskSampler)
                        .To.Have.Received(1)
                        .Start();
                }
            }
        }

        [TestFixture]
        public class FetchSnapshot
        {
            [Test]
            [Explicit("WIP: I'm not using Raise.Event properly yet")]
            public void ShouldIncludeCpuSamples()
            {
                // Arrange
                var cpuSampler = Substitute.For<ICpuUsageSampler>();
                var expected = GetRandomArray<CpuUsageSampleResult>();
                // Act
                using (var sut = Create(cpuSampler))
                {
                    var beforeSample = DateTime.Now;
                    cpuSampler.OnSample += Raise.EventWith(
                        cpuSampler,
                        SamplerEventArgs.For(expected)
                    );
                    var afterSample = DateTime.Now;
                    var result = sut.FetchSnapshot();
                    // Assert
                    Expect(result.CpuLastUpdated)
                        .To.Be
                        .Greater.Than(beforeSample)
                        .And
                        .Less.Than(afterSample);
                }
            }
        }

        [TestFixture]
        public class Disposal
        {
            [Test]
            public void ShouldDisposeCpuSampler()
            {
                // Arrange
                var cpuSampler = Substitute.For<ICpuUsageSampler>();
                var diskSampler = Substitute.For<IDiskUsageSampler>();
                // Act
                using (Create(cpuSampler, diskSampler))
                {
                }

                // Assert
                Expect(cpuSampler)
                    .To.Have.Received(1)
                    .Dispose();
            }

            [Test]
            public void ShouldDisposeDiskSampler()
            {
                // Arrange
                var cpuSampler = Substitute.For<ICpuUsageSampler>();
                var diskSampler = Substitute.For<IDiskUsageSampler>();
                // Act
                using (Create(cpuSampler, diskSampler))
                {
                }

                // Assert
                Expect(diskSampler)
                    .To.Have.Received(1)
                    .Dispose();
            }

            [Test]
            public void ShouldLogErrorWithCpuSamplerDisposal()
            {
                // Arrange
                var cpuSampler = Substitute.For<ICpuUsageSampler>();
                var expected = new InvalidOperationException(GetRandomString(1));
                cpuSampler.When(o => o.Dispose())
                    .Do(ci => throw expected);
                var diskSampler = Substitute.For<IDiskUsageSampler>();
                var logger = Substitute.For<IGenericLogger>();
                // Act
                using (Create(cpuSampler, diskSampler, logger))
                {
                }

                // Assert
                Expect(logger)
                    .To.Have.Received(1)
                    .Log<SystemStatsProvider>(
                        LogType.Error,
                        "Unable to dispose CPU sampler",
                        expected
                    );
            }

            [Test]
            public void ShouldLogErrorWithDiskSamplerDisposal()
            {
                // Arrange
                var cpuSampler = Substitute.For<ICpuUsageSampler>();
                var expected = new InvalidOperationException(GetRandomString(1));
                var diskSampler = Substitute.For<IDiskUsageSampler>();
                diskSampler.When(o => o.Dispose())
                    .Do(ci => throw expected);
                var logger = Substitute.For<IGenericLogger>();
                // Act
                using (Create(cpuSampler, diskSampler, logger))
                {
                }

                // Assert
                Expect(logger)
                    .To.Have.Received(1)
                    .Log<SystemStatsProvider>(
                        LogType.Error,
                        "Unable to dispose Disk usage sampler",
                        expected
                    );
            }
        }

        private static ISystemStatsProvider Create(
            ICpuUsageSampler cpuUsageSampler = null,
            IDiskUsageSampler diskUsageSampler = null,
            IGenericLogger logger = null
        )
        {
            return new SystemStatsProvider(
                cpuUsageSampler ?? Substitute.For<ICpuUsageSampler>(),
                diskUsageSampler ?? Substitute.For<IDiskUsageSampler>(),
                logger ?? Substitute.For<IGenericLogger>()
            );
        }
    }

    [TestFixture]
    public class AutoLockerTests
    {
        // PB.Utils has an auto-locker -- but I don't want to
        //  introduce the dependency to the main project
        //  -> I've re-created the bare minimum auto-locker
        //  -> This should be used where re-entrant locking
        //     is not allowed or just not trusted to be safe
        //     since .net lock() statements are re-entrant

        [Test]
        public void WhenNoError_ShouldLockUntilDisposed()
        {
            // Arrange
            var semaphore = new SemaphoreSlim(1);
            using (new ServiceHost.SystemMonitoring.AutoLocker(semaphore))
            {
                // Act
                Expect(semaphore.CurrentCount)
                    .To.Equal(0);
                // Assert
            }

            Expect(semaphore.CurrentCount)
                .To.Equal(1);
        }

        [Test]
        public void WhenCodeThrows_ShouldStillLockUntilDisposed()
        {
            // Arrange
            var semaphore = new SemaphoreSlim(1);
            var exploded = false;
            try
            {
                using (new ServiceHost.SystemMonitoring.AutoLocker(semaphore))
                {
                    Expect(semaphore.CurrentCount)
                        .To.Equal(0);
                    // Act
                    throw new Exception("DIE");
                    // Assert
                }
            }
            catch
            {
                exploded = true;
                Expect(semaphore.CurrentCount)
                    .To.Equal(1);
            }

            Expect(exploded).To.Be.True("Should have exploded");
        }
    }
}
