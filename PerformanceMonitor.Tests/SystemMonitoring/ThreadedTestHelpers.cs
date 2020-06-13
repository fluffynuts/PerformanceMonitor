using System;
using System.Threading;

namespace PerformanceMonitor.Tests.SystemMonitoring
{
    public static class ThreadedTestHelpers
    {
        public static void WaitFor(
            Func<bool> toRun,
            int sampleMs = 50,
            int maxWaitMs = 1000)
        {
            var started = DateTime.Now;
            while ((DateTime.Now - started).TotalMilliseconds < maxWaitMs)
            {
                if (toRun())
                {
                    return;
                }

                Thread.Sleep(sampleMs);
            }

            throw new TimeoutException("Timed out whilst waiting for condition");
        }
    }
}
