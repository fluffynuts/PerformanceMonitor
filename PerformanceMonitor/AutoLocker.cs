using System;
using System.Threading;

namespace ServiceHost.SystemMonitoring
{
    public class AutoLocker: IDisposable
    {
        private SemaphoreSlim _semaphoreSlim;

        public AutoLocker(SemaphoreSlim semaphoreSlim)
        {
            _semaphoreSlim = semaphoreSlim;
            _semaphoreSlim.Wait();
        }

        public void Dispose()
        {
            _semaphoreSlim?.Release();
            _semaphoreSlim = null;
        }
    }
}
