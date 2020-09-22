namespace SolarWinds.SharedCommunication.RateLimiter
{
    public interface IRateLimiterDataAccessor
    {
        bool TryEnterSynchronizedRegion();
        void ExitSynchronizedRegion();
        int Capacity { get; }
        int Size { get; }
        long SpanTicks { get; }
        long OldestTimestampTicks { get; }
        long CurrentTimestampTicks { get; set; }

    }
}