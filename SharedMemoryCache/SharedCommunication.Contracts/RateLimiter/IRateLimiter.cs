using System;
using System.Threading;
using System.Threading.Tasks;

namespace SharedCommunication.Contracts.RateLimiter
{
    public interface IRateLimiter
    {
        Task<bool> WaitTillNextFreeSlotAsync(TimeSpan maxAcceptableWaitingTime, CancellationToken cancellationToken = default);
        bool BlockTillNextFreeSlot(TimeSpan maxAcceptableWaitingTime);

    }
}