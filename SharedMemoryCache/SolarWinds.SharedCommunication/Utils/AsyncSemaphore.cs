using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SolarWinds.SharedCommunication.Contracts.Utils;

namespace SolarWinds.SharedCommunication.Utils
{
    public class AsyncSemaphore : IAsyncSemaphore
    {
        //Semaphore doesn't enforce the ownership - so we can release it from different thread than acquiring
        // so it's perfect candidate for async wrapping.
        //On the other hand it means that when left abandoned acquired (e.g. process crash), it won't get autoreleased
        //So we need to make sureit gets released in all execution paths (including exceptional)
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
                IDisposable lockContext = new LockContext(this._sp);
                if (!tcs.TrySetResult(lockContext))
                {
                    //this can happen if we get singalled by threadpool (wait finished), but at the same time the cancellation kicked in
                    // in this case we need to make sure the lock is released
                    lockContext.Dispose();
                }
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
