using System;
using System.Threading;
using System.Threading.Tasks;
using SharedCommunication.Contracts.RateLimiter;

namespace SharedCommunication.RateLimiter
{
    public class RingMemoryBufferRateLimiter : IRateLimiter
    {
        private readonly IRateLimiterDataAccessor _rateLimiterDataAccessor;
        private readonly IDateTime _dateTime;
        private readonly int _rateLimiterCapacity;
        private readonly TimeSpan _rateLimiterSpan;

        public RingMemoryBufferRateLimiter(
            IRateLimiterDataAccessor rateLimiterDataAccessor,
            IDateTime dateTime)
        {
            _rateLimiterDataAccessor = rateLimiterDataAccessor;
            _dateTime = dateTime;

            _rateLimiterCapacity = _rateLimiterDataAccessor.Capacity;
            _rateLimiterSpan = new TimeSpan(_rateLimiterDataAccessor.SpanTicks);
        }

        private void EnterSynchronization()
        {
            SpinWait.SpinUntil(_rateLimiterDataAccessor.TryEnterSynchronizedRegion);
        }

        //Depending on version of OS and .NET framework, the granularity of timer and timer events can 1-15ms (15ms being the usual)
        // this could lead to 'false wake-up' issues during contention (leading to resonated contention)
        private TimeSpan GetRandomSaltSpan()
        {
            return TimeSpan.FromMilliseconds(new Random().Next(20));
        }

        private bool ClaimSlotAndGetWaitingTime(TimeSpan maxAcceptableWaitingTime, out TimeSpan waitSpan)
        {
            DateTime utcNow;
            waitSpan = TimeSpan.Zero;
            bool isAcceptable = true;
            try
            {
                EnterSynchronization();
                utcNow = _dateTime.UtcNow;
                bool isFull = _rateLimiterDataAccessor.Size == _rateLimiterCapacity;

                if (isFull)
                {
                    DateTime oldestEventUtc =
                        new DateTime(_rateLimiterDataAccessor.OldestTimestampTicks);
                    waitSpan = _rateLimiterSpan - (utcNow - oldestEventUtc);
                    //prevent false wake ups by randomness
                    waitSpan = waitSpan <= TimeSpan.Zero ? TimeSpan.Zero : (waitSpan + GetRandomSaltSpan());
                }

                isAcceptable = waitSpan <= maxAcceptableWaitingTime;
                if (isAcceptable)
                {
                    _rateLimiterDataAccessor.CurrentTimestampTicks = (utcNow + waitSpan).Ticks;
                }
            }
            finally
            {
                _rateLimiterDataAccessor.ExitSynchronizedRegion();
            }

            return isAcceptable;
        }

        private static readonly Task<bool> _success = Task.FromResult(true);
        private static readonly Task<bool> _failure = Task.FromResult(false);
        public Task<bool> WaitTillNextFreeSlotAsync(TimeSpan maxAcceptableWaitingTime, CancellationToken cancellationToken = default)
        {
            TimeSpan waitSpan;
            if (!ClaimSlotAndGetWaitingTime(maxAcceptableWaitingTime, out waitSpan))
            {
                return _failure;
            }

            if (waitSpan <= TimeSpan.Zero)
                return _success;
            else
                return Task.Delay(waitSpan, cancellationToken).ContinueWith(t => !t.IsCanceled);
        }

        public bool BlockTillNextFreeSlot(TimeSpan maxAcceptableWaitingTime)
        {
            TimeSpan waitSpan;
            if (!ClaimSlotAndGetWaitingTime(maxAcceptableWaitingTime, out waitSpan))
            {
                return false;
            }

            if (waitSpan > TimeSpan.Zero)
                Thread.Sleep(waitSpan);

            return true;
        }
    }
}