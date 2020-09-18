using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SharedCommunication.Contracts.Utils;

namespace SharedCommunication.Utils
{
    public class AsyncSemaphore : IAsyncSemaphore
    {
        private readonly Semaphore _sp;

        public AsyncSemaphore(Semaphore sp)
        {
            _sp = sp;
        }

        private class WaitInfo
        {
            public IDisposable Handle { get; set; }
        }

        public Task WaitAsync(CancellationToken token = default)
        {
            return LockAsync(token);
        }

        public void Release()
        {
            _sp.Release();
        }

        public Task<IDisposable> LockAsync(CancellationToken token = default)
        {
            TaskCompletionSource<IDisposable> tcs = new TaskCompletionSource<IDisposable>();
            if (token.IsCancellationRequested)
            {
                tcs.TrySetCanceled();
                return tcs.Task;
            }
            WaitInfo waitInfo = new WaitInfo();

            var reg = ThreadPool.RegisterWaitForSingleObject(_sp, (state, timedout) =>
            {
                tcs.TrySetResult(new LockContext(this._sp));
                waitInfo.Handle?.Dispose();
            }, null, Timeout.InfiniteTimeSpan, true);
            //here is a small space for race condition - token getting cancelled right after the task getting registered and finished
            // and before registering the cancellation token. In such a case, the token would remain registered and upon cancellation (if any)
            // the unregistration of wait handle from TP would not succeed anyway - as TP registration is no more valid
            waitInfo.Handle = token.Register(() =>
            {
                reg.Unregister(null);
                tcs.TrySetCanceled();
            });
            return tcs.Task;
        }

        public void Dispose()
        {
            _sp.Dispose();
        }

        private class LockContext : IDisposable
        {
            private readonly Semaphore _sp;

            public LockContext(Semaphore sp)
            {
                _sp = sp;
            }

            public void Dispose()
            {
                _sp?.Release();
            }
        }
    }
}
