using System;
using System.Threading;
using System.Threading.Tasks;

namespace SharedCommunication.Contracts.Utils
{
    public interface IAsyncSemaphore : IDisposable
    {
        Task WaitAsync(CancellationToken token = default);
        Task<IDisposable> LockAsync(CancellationToken token = default);
        void Release();
    }
}